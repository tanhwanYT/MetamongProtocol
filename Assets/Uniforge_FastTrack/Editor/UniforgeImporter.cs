using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Uniforge.FastTrack.Editor
{
    public static class UniforgeImporter
    {
        private static Dictionary<string, GameObject> PendingScriptAttachments = new Dictionary<string, GameObject>();
        private static Dictionary<string, GameObject> PrefabMappings = new Dictionary<string, GameObject>();

        // EditorPrefs key for persisting pending attachments across domain reload
        private const string PendingAttachmentsKey = "Uniforge_PendingAttachments";
        private const string PendingPrefabsKey = "Uniforge_PendingPrefabs";

        [MenuItem("Uniforge/Import Scene JSON (Fast Track)")]
        public static void ImportScene()
        {
            ImportFromJson(null);
        }

        public static async void ImportFromJson(string jsonOverride = null)
        {
            string json = jsonOverride;
            if (string.IsNullOrEmpty(json))
            {
                string path = EditorUtility.OpenFilePanel("Select Uniforge Scene JSON", "", "json");
                if (string.IsNullOrEmpty(path)) return;
                json = File.ReadAllText(path);
            }

            // Save JSON to StreamingAssets
            SaveJsonToStreamingAssets(json);

            try
            {
                // Try to detect if this is a single scene JSON (Frontend format)
                // Frontend sends: { sceneId, entities, tiles, assets }
                // Expected: { scenes: [{ entities, tiles, assets }], assets }
                JObject rawJson = JObject.Parse(json);
                
                GameDataJSON data;
                
                // Check if 'entities' exists at root level (Frontend single scene format)
                if (rawJson["entities"] != null && rawJson["scenes"] == null)
                {
                    Debug.Log("<color=cyan>[UniforgeImporter]</color> Detected Frontend single-scene format, converting...");
                    
                    // Convert to GameDataJSON format
                    var sceneData = new SceneJSON
                    {
                        id = rawJson["sceneId"]?.ToString() ?? "scene_0",
                        sceneId = rawJson["sceneId"]?.ToString(),
                        name = rawJson["name"]?.ToString() ?? "Scene",
                        entities = rawJson["entities"]?.ToObject<List<EntityJSON>>(),
                        tiles = rawJson["tiles"]?.ToObject<List<TileJSON>>(),
                        assets = rawJson["assets"]?.ToObject<List<AssetDetailJSON>>()
                    };
                    
                    data = new GameDataJSON
                    {
                        scenes = new List<SceneJSON> { sceneData },
                        assets = rawJson["assets"]?.ToObject<List<AssetDetailJSON>>()
                    };
                    
                    Debug.Log($"<color=cyan>[UniforgeImporter]</color> Converted: {sceneData.entities?.Count ?? 0} entities, {data.assets?.Count ?? 0} assets");
                }
                else
                {
                    // Standard GameDataJSON format
                    data = JsonConvert.DeserializeObject<GameDataJSON>(json);
                }
                
                if (data == null)
                {
                    Debug.LogError("[UniforgeImporter] JSON Parsing Failed.");
                    return;
                }
                
                ConfigureInputSystem();
                await ProcessGameData(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniforgeImporter] Import Failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static async Task ProcessGameData(GameDataJSON data)
        {
            Debug.Log($"<color=cyan>[UniforgeImporter]</color> === ProcessGameData Start ===");
            Debug.Log($"<color=cyan>[UniforgeImporter]</color> Scenes: {data.scenes?.Count ?? 0}, Assets: {data.assets?.Count ?? 0}");
            if (data.assets != null)
            {
                foreach (var a in data.assets)
                {
                    Debug.Log($"<color=gray>[UniforgeImporter]</color> Asset: id={a.id}, name={a.name}, tag={a.tag}");
                }
            }

            GameObject root = new GameObject($"Uniforge_Scene_{DateTime.Now:HHmmss}");

            // Ensure UniforgeRuntime exists for PrefabRegistry, AudioManager, etc.
            var existingRuntime = GameObject.Find("[UniforgeRuntime]");
            if (existingRuntime == null)
            {
                var runtimeGo = new GameObject("[UniforgeRuntime]");
                var runtime = runtimeGo.AddComponent<Uniforge.FastTrack.Runtime.UniforgeRuntime>();
                runtimeGo.AddComponent<Uniforge.FastTrack.Runtime.AudioManager>();
                runtimeGo.AddComponent<Uniforge.FastTrack.Runtime.ParticleManager>();
                runtimeGo.AddComponent<Uniforge.FastTrack.Runtime.PrefabRegistry>();
                runtimeGo.AddComponent<Uniforge.FastTrack.Runtime.ProjectileManager>();
                Debug.Log("<color=green>[UniforgeImporter]</color> Created UniforgeRuntime");
            }
            
            // Map Asset IDs/Names to URLs
            Dictionary<string, string> assetMap = new Dictionary<string, string>();
            if (data.assets != null)
            {
                foreach (var asset in data.assets)
                {
                    if (!string.IsNullOrEmpty(asset.id))
                        assetMap[asset.id] = asset.url;
                    if (!string.IsNullOrEmpty(asset.name) && !assetMap.ContainsKey(asset.name))
                        assetMap.Add(asset.name, asset.url);
                }
            }

            // Sprite Cache (Key: URL, Value: Internal Sprite Asset)
            Dictionary<string, Sprite> textureCache = new Dictionary<string, Sprite>();

            // [Optimization] Pre-download all unique URLs in parallel
            if (data.assets != null && data.assets.Count > 0)
            {
                var uniqueUrls = data.assets.Select(a => a.url).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
                Debug.Log($"<color=yellow>[UniforgeImporter]</color> Pre-downloading {uniqueUrls.Count} unique textures in parallel...");
                
                EditorUtility.DisplayProgressBar("Uniforge Import", $"Downloading {uniqueUrls.Count} textures...", 0f);
                
                var downloadTasks = uniqueUrls.Select(url => DownloadTexture(url)).ToArray();
                var results = await Task.WhenAll(downloadTasks);
                
                for (int i = 0; i < uniqueUrls.Count; i++)
                {
                    if (results[i] != null)
                    {
                        textureCache[uniqueUrls[i]] = results[i];
                    }
                }
                
                Debug.Log($"<color=green>[UniforgeImporter]</color> Pre-download complete! Cached {textureCache.Count}/{uniqueUrls.Count} textures.");
            }

            // Scene Processing
            if (data.scenes == null || data.scenes.Count == 0) return;
            var scene = data.scenes[0];

            if (scene.entities != null)
            {
                int totalEntities = scene.entities.Count;
                int current = 0;

                foreach (var entity in scene.entities)
                {
                    current++;
                    EditorUtility.DisplayProgressBar("Uniforge Import", $"Processing {entity.name} ({current}/{totalEntities})", (float)current / totalEntities);

                    Debug.Log($"<color=cyan>[UniforgeImporter]</color> Entity: name={entity.name}, id={entity.id}, texture={entity.texture ?? "null"}");

                    GameObject go = new GameObject(entity.name);
                    go.transform.SetParent(root.transform);
                    
                    // Coordinate Conversion
                    float ppx = 100f; 
                    Vector3 pos = new Vector3(entity.x / ppx, -entity.y / ppx, 0);
                    go.transform.position = pos;
                    
                    // Rotation Fix for Polymorphic Type (float vs object)
                    float rot = 0f;
                    try {
                        if (entity.rotation is JObject jo) 
                            rot = jo["z"]?.Value<float>() ?? 0f;
                        else 
                            rot = Convert.ToSingle(entity.rotation);
                    } catch { rot = 0f; }
                    
                    go.transform.rotation = Quaternion.Euler(0, 0, rot);
                    go.transform.localScale = new Vector3(entity.scaleX, entity.scaleY, 1);
                    
                    // Camera Follow Setup
                    if (current == 1 && Camera.main != null)
                    {
                        Camera.main.transform.position = new Vector3(pos.x, pos.y, -10);
                    }

                    // Sprite Assignment
                    if (!string.IsNullOrEmpty(entity.texture))
                    {
                        Sprite sprite = null;
                        string url = assetMap.ContainsKey(entity.texture) ? assetMap[entity.texture] : entity.texture;

                        if (textureCache.ContainsKey(url))
                        {
                            sprite = textureCache[url];
                        }
                        else if (url.StartsWith("http") || url.StartsWith("data:"))
                        {
                            sprite = await DownloadTexture(url);
                            if (sprite != null) textureCache[url] = sprite;
                        }

                        if (sprite != null)
                        {
                            var sr = go.AddComponent<SpriteRenderer>();
                            sr.sprite = sprite;
                            sr.sortingOrder = 1;
                            
                            var collider = go.AddComponent<BoxCollider2D>();
                            collider.isTrigger = true; 
                            collider.size = sprite.bounds.size; 
                        }
                        else
                        {
                            // Warn but don't crash
                            // Debug.LogWarning($"Failed to resolve sprite for {entity.name}");
                        }
                    }

                    // Animator
                    Debug.Log($"<color=magenta>[UniforgeImporter]</color> Processing animation for entity '{entity.name}' (texture={entity.texture ?? "null"})");
                    var animController = AnimationGenerator.GenerateForEntity(entity, data.assets);
                    if (animController != null)
                    {
                        var animator = go.GetComponent<Animator>();
                        if (animator == null) animator = go.AddComponent<Animator>();
                        animator.runtimeAnimatorController = animController;
                        Debug.Log($"<color=green>[UniforgeImporter]</color> Assigned AnimatorController to '{entity.name}'");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=orange>[UniforgeImporter]</color> No AnimatorController created for '{entity.name}'");
                    }

                    // UniforgeEntity
                    var ufe = go.AddComponent<Uniforge.FastTrack.Runtime.UniforgeEntity>();
                    ufe.EntityId = entity.id;
                    ufe.Role = entity.role ?? "";
                    if (entity.tags != null) ufe.Tags = new List<string>(entity.tags);

                    // Script Generation
                    UniforgeScriptGenerator.Generate(entity);
                    PendingScriptAttachments[entity.id] = go;
                }
            }

            // Prefab Asset Processing
            // Process assets with tag "Prefab" and create prefab GameObjects
            if (data.assets != null)
            {
                Debug.Log($"<color=cyan>[UniforgeImporter]</color> Processing {data.assets.Count} assets for prefabs...");
                
                GameObject prefabRoot = new GameObject("Uniforge_Prefabs");
                prefabRoot.transform.SetParent(root.transform);
                // Keep active but move far away to hide from view
                prefabRoot.transform.position = new Vector3(-9999, -9999, 0);

                foreach (var asset in data.assets)
                {
                    Debug.Log($"<color=gray>[UniforgeImporter]</color> Asset: {asset.id}, tag={asset.tag ?? "null"}, metadata={(asset.metadata != null ? "exists" : "null")}");
                    
                    if (asset.tag != "Prefab" || asset.metadata == null) continue;
                    
                    Debug.Log($"<color=yellow>[UniforgeImporter]</color> Found Prefab asset: {asset.name} (ID: {asset.id})");

                    try
                    {
                        // Parse prefab data from metadata
                        // metadata could be JObject, Dictionary, or other types
                        JObject metadataJson = null;
                        if (asset.metadata is JObject jo)
                            metadataJson = jo;
                        else if (asset.metadata != null)
                            metadataJson = JObject.FromObject(asset.metadata);

                        if (metadataJson == null) continue;

                        var prefabJson = metadataJson["prefab"] as JObject;
                        if (prefabJson == null) continue;

                        // Create prefab entity
                        var prefabEntity = prefabJson.ToObject<EntityJSON>();
                        if (prefabEntity == null) continue;

                        // Override prefabEntity.id with asset.id for consistent referencing
                        // This ensures SpawnEntity can find the prefab using the asset ID
                        prefabEntity.id = asset.id;

                        Debug.Log($"<color=cyan>[UniforgeImporter]</color> Processing Prefab: {asset.name} (ID: {asset.id})");

                        // Create GameObject for prefab
                        GameObject prefabGo = new GameObject(asset.name ?? prefabEntity.name ?? "Prefab");
                        prefabGo.transform.SetParent(prefabRoot.transform);
                        prefabGo.transform.localPosition = Vector3.zero;

                        // Setup sprite
                        if (!string.IsNullOrEmpty(prefabEntity.texture))
                        {
                            string texUrl = assetMap.ContainsKey(prefabEntity.texture) ? assetMap[prefabEntity.texture] : prefabEntity.texture;
                            Sprite sprite = null;

                            if (textureCache.ContainsKey(texUrl))
                            {
                                sprite = textureCache[texUrl];
                            }
                            else if (texUrl.StartsWith("http") || texUrl.StartsWith("data:"))
                            {
                                sprite = await DownloadTexture(texUrl);
                                if (sprite != null) textureCache[texUrl] = sprite;
                            }

                            if (sprite != null)
                            {
                                var sr = prefabGo.AddComponent<SpriteRenderer>();
                                sr.sprite = sprite;
                                var collider = prefabGo.AddComponent<BoxCollider2D>();
                                collider.isTrigger = true;
                                collider.size = sprite.bounds.size;
                            }
                        }

                        // Generate Animations for Prefab
                        var animController = AnimationGenerator.GenerateForEntity(prefabEntity, data.assets);
                        if (animController != null)
                        {
                            var animator = prefabGo.GetComponent<Animator>();
                            if (animator == null) animator = prefabGo.AddComponent<Animator>();
                            animator.runtimeAnimatorController = animController;
                        }

                        // Add UniforgeEntity component
                        var ufe = prefabGo.AddComponent<Uniforge.FastTrack.Runtime.UniforgeEntity>();
                        // IMPORTANT: Use asset.id as EntityId because SpawnEntity uses sourceAssetId/prefabId
                        // which refers to the asset ID, not the internal prefab entity ID
                        ufe.EntityId = asset.id;
                        ufe.Role = prefabEntity.role ?? "neutral";
                        if (prefabEntity.tags != null) ufe.Tags = new List<string>(prefabEntity.tags);

                        // Generate script for prefab logic using prefabEntity (which has the logic)
                        UniforgeScriptGenerator.Generate(prefabEntity);
                        PendingScriptAttachments[asset.id] = prefabGo;

                        // Save as actual Unity Prefab asset
                        string prefabFolderPath = "Assets/Uniforge_FastTrack/Prefabs";
                        if (!Directory.Exists(prefabFolderPath))
                        {
                            Directory.CreateDirectory(prefabFolderPath);
                        }

                        string safeName = SanitizeFileName(asset.name ?? prefabEntity.name ?? asset.id);
                        string prefabPath = $"{prefabFolderPath}/{safeName}.prefab";

                        // Save GameObject as Prefab asset
                        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabGo, prefabPath);
                        if (savedPrefab != null)
                        {
                            Debug.Log($"<color=green>[UniforgeImporter]</color> Prefab saved: {prefabPath}");

                            // Store mapping for PrefabRegistry registration
                            PrefabMappings[asset.id] = savedPrefab;
                            if (!string.IsNullOrEmpty(asset.name))
                                PrefabMappings[asset.name] = savedPrefab;
                        }
                        else
                        {
                            Debug.LogWarning($"[UniforgeImporter] Failed to save prefab: {prefabPath}");
                        }

                        Debug.Log($"<color=green>[UniforgeImporter]</color> Prefab registered: {asset.id} -> {prefabGo.name}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[UniforgeImporter] Failed to process prefab {asset.id}: {e.Message}");
                    }
                }
            }

            // Tile Creation
            if (scene.tiles != null)
            {
                GameObject tileRoot = new GameObject("Uniforge_Tiles");
                tileRoot.transform.SetParent(root.transform);
                
                // Index Map for Tiles
                Dictionary<int, string> assetIndexMap = new Dictionary<int, string>();
                if (data.assets != null) {
                    foreach (var asset in data.assets)
                        if (!assetIndexMap.ContainsKey(asset.idx)) assetIndexMap.Add(asset.idx, asset.url);
                }

                float tileSize = 0.32f;

                foreach (var tile in scene.tiles)
                {
                    if (assetIndexMap.ContainsKey(tile.idx))
                    {
                        string url = assetIndexMap[tile.idx];
                        GameObject tObj = new GameObject($"Tile_{tile.x}_{tile.y}");
                        tObj.transform.SetParent(tileRoot.transform);
                        tObj.transform.position = new Vector3(tile.x * tileSize, -tile.y * tileSize, 0);

                        Sprite sprite = null;
                        if (textureCache.ContainsKey(url)) sprite = textureCache[url];
                        else {
                            sprite = await DownloadTexture(url);
                            if (sprite != null) textureCache[url] = sprite;
                        }

                        if (sprite != null)
                        {
                            var sr = tObj.AddComponent<SpriteRenderer>();
                            sr.sprite = sprite;
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // Register prefabs to PrefabRegistry (before domain reload)
            RegisterPrefabsToRegistry();

            // 도메인 리로드 전에 pending attachments를 EditorPrefs에 저장
            Debug.Log($"<color=yellow>[UniforgeImporter]</color> PendingScriptAttachments count: {PendingScriptAttachments.Count}");
            if (PendingScriptAttachments.Count > 0)
            {
                var entityIds = PendingScriptAttachments.Keys.ToList();
                Debug.Log($"<color=yellow>[UniforgeImporter]</color> Saving pending: {string.Join(", ", entityIds)}");
                SavePendingAttachments(entityIds);
            }
            else
            {
                Debug.LogWarning("[UniforgeImporter] No pending script attachments to save!");
            }

            Debug.Log("<color=green>[UniforgeImporter]</color> Import complete! Triggering asset refresh...");
            AssetDatabase.Refresh();
        }

        // --- Helper Methods ---

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";

            // Remove invalid characters for file names
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            // Also replace some problematic characters
            name = name.Replace(' ', '_').Replace('-', '_');

            return name;
        }

        /// <summary>
        /// Register prefabs to PrefabRegistry after import completes
        /// </summary>
        private static void RegisterPrefabsToRegistry()
        {
            if (PrefabMappings.Count == 0) return;

            // Find or create PrefabRegistry
            var registry = UnityEngine.Object.FindFirstObjectByType<Uniforge.FastTrack.Runtime.PrefabRegistry>();
            if (registry == null)
            {
                var runtimeGo = GameObject.Find("[UniforgeRuntime]");
                if (runtimeGo != null)
                {
                    registry = runtimeGo.GetComponent<Uniforge.FastTrack.Runtime.PrefabRegistry>();
                }
            }

            if (registry != null)
            {
                // Clear existing entries and add new ones
                registry.Prefabs.Clear();
                foreach (var kvp in PrefabMappings)
                {
                    registry.Prefabs.Add(new Uniforge.FastTrack.Runtime.PrefabEntry
                    {
                        Id = kvp.Key,
                        Prefab = kvp.Value
                    });
                    Debug.Log($"<color=green>[UniforgeImporter]</color> Registered to PrefabRegistry: {kvp.Key}");
                }
                EditorUtility.SetDirty(registry);
            }
            else
            {
                Debug.LogWarning("[UniforgeImporter] PrefabRegistry not found. Prefabs won't be registered.");
            }
        }

        private static async Task<Sprite> DownloadTexture(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl)) return null;

            // Simple Local/Direct check
            if (originalUrl.Contains("localhost") || originalUrl.Contains("127.0.0.1") || !originalUrl.Contains("/api/assets/"))
            {
                // Fallback or local
                return await DownloadTextureDirect(originalUrl);
            }

            // Proxy logic
            if (originalUrl.StartsWith("/")) originalUrl = "https://uniforge.kr" + originalUrl;
            
            // Bypass Data URI (handled in Direct)
            if (originalUrl.StartsWith("data:")) return await DownloadTextureDirect(originalUrl);

            string proxyUrl = $"https://images.weserv.nl/?url={Uri.EscapeDataString(originalUrl)}&output=png&n=-1";

            using (UnityWebRequest uwr = UnityWebRequest.Get(proxyUrl))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = 10;
                var op = uwr.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try {
                        return SaveAndLoadTexture(uwr.downloadHandler.data, originalUrl);
                    } catch { return await DownloadTextureDirect(originalUrl); }
                }
                else
                {
                    return await DownloadTextureDirect(originalUrl);
                }
            }
        }

        private static async Task<Sprite> DownloadTextureDirect(string url)
        {
            if (url.StartsWith("data:image"))
            {
                try {
                    string base64 = url.Substring(url.IndexOf(",") + 1);
                    byte[] bytes = Convert.FromBase64String(base64);
                    return SaveAndLoadTexture(bytes, "asset_" + url.GetHashCode() + ".webp");
                } catch { return null; }
            }

            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.redirectLimit = 10;
                uwr.timeout = 10;
                var op = uwr.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    return SaveAndLoadTexture(uwr.downloadHandler.data, url);
                }
                return null;
            }
        }

        private static Sprite SaveAndLoadTexture(byte[] data, string originalUrl)
        {
            string fileName = GetSanitizedFileName(originalUrl);
            string dirPath = "Assets/Uniforge_FastTrack/Textures";
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
            
            string filePath = Path.Combine(dirPath, fileName);
            
            // Convert any image format (WebP, JPG, etc.) to actual PNG bytes
            // Unity's Texture2D.LoadImage supports PNG, JPG, and some other formats
            Texture2D tempTexture = new Texture2D(2, 2);
            if (tempTexture.LoadImage(data))
            {
                // Re-encode as proper PNG
                byte[] pngData = tempTexture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
                UnityEngine.Object.DestroyImmediate(tempTexture);
            }
            else
            {
                // Fallback: save raw bytes and hope for the best
                Debug.LogWarning($"[UniforgeImporter] Failed to decode image, saving raw bytes: {originalUrl}");
                File.WriteAllBytes(filePath, data);
                UnityEngine.Object.DestroyImmediate(tempTexture);
            }
            
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null) {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            
            return AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
        }

        private static string GetSanitizedFileName(string url)
        {
            try
            {
                if (url.StartsWith("data:") || !url.StartsWith("http")) return "asset_" + Math.Abs(url.GetHashCode()) + ".png";
                
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                string fileName = Path.GetFileName(path);
                
                // Convert webp to png for Unity compatibility
                if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    fileName = Path.ChangeExtension(fileName, ".png");
                else if (!Path.HasExtension(fileName)) 
                    fileName += ".png";
                    
                return Uri.UnescapeDataString(fileName);
            }
            catch
            {
                return "asset_" + Math.Abs(url.GetHashCode()) + ".png";
            }
        }

        private static void SaveJsonToStreamingAssets(string json)
        {
            try
            {
                string streamingAssetsPath = Application.streamingAssetsPath;
                string uniforgeDataPath = Path.Combine(streamingAssetsPath, "UniforgeData");
                
                // Create directories if they don't exist
                if (!Directory.Exists(streamingAssetsPath))
                    Directory.CreateDirectory(streamingAssetsPath);
                if (!Directory.Exists(uniforgeDataPath))
                    Directory.CreateDirectory(uniforgeDataPath);
                
                // Generate filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"scene_{timestamp}.json";
                string filePath = Path.Combine(uniforgeDataPath, fileName);
                
                // Save JSON file
                File.WriteAllText(filePath, json);
                
                // Also save as "latest.json" for easy access
                string latestPath = Path.Combine(uniforgeDataPath, "latest.json");
                File.WriteAllText(latestPath, json);
                
                Debug.Log($"<color=green>[UniforgeImporter]</color> JSON saved to: {filePath}");
                Debug.Log($"<color=green>[UniforgeImporter]</color> Latest copy at: {latestPath}");
                
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniforgeImporter] Failed to save JSON to StreamingAssets: {ex.Message}");
            }
        }

        private static void ConfigureInputSystem()
        {
            #if UNITY_EDITOR
            try
            {
                var playerSettings = typeof(UnityEditor.PlayerSettings);
                var prop = playerSettings.GetProperty("activeInputHandler", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (prop != null)
                {
                    int val = (int)prop.GetValue(null);
                    if (val != 2) prop.SetValue(null, 2);
                }
            }
            catch {}
            #endif
        }
        
        // Callback Handler
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // After script compilation, attach generated scripts to all UniforgeEntity objects
            EditorApplication.delayCall += AttachScriptsToAllEntities;
        }

        private static void AttachScriptsToAllEntities()
        {
            var allEntities = GameObject.FindObjectsByType<Uniforge.FastTrack.Runtime.UniforgeEntity>(FindObjectsSortMode.None);
            if (allEntities.Length == 0) return;

            Debug.Log($"<color=cyan>[UniforgeImporter]</color> Checking {allEntities.Length} entities for script attachment...");
            
            int attachedCount = 0;
            foreach (var entity in allEntities)
            {
                if (string.IsNullOrEmpty(entity.EntityId)) continue;

                string className = $"Gen_{entity.EntityId.Replace("-", "_")}";
                
                try
                {
                    var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                    if (assembly == null) continue;

                    var type = assembly.GetType(className);
                    if (type == null) continue; // Script doesn't exist for this entity

                    // Check if already attached
                    if (entity.gameObject.GetComponent(type) != null) continue;

                    // Attach the script
                    entity.gameObject.AddComponent(type);
                    attachedCount++;
                    Debug.Log($"<color=green>[UniforgeImporter]</color> Attached {className} to {entity.gameObject.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UniforgeImporter] Failed to attach {className}: {ex.Message}");
                }
            }

            if (attachedCount > 0)
            {
                Debug.Log($"<color=green>[UniforgeImporter]</color> Total {attachedCount} scripts attached!");
            }
        }

        private static void AttachScriptsFromEditorPrefs()
        {
            if (!EditorPrefs.HasKey(PendingAttachmentsKey)) return;

            try
            {
                string json = EditorPrefs.GetString(PendingAttachmentsKey);
                var entityIds = JsonConvert.DeserializeObject<List<string>>(json);
                EditorPrefs.DeleteKey(PendingAttachmentsKey);

                if (entityIds == null || entityIds.Count == 0) return;

                Debug.Log($"<color=cyan>[UniforgeImporter]</color> 도메인 리로드 후 {entityIds.Count}개 스크립트 attach 시도");

                // 모든 UniforgeEntity 컴포넌트를 찾아서 매칭
                var allEntities = UnityEngine.Object.FindObjectsByType<Uniforge.FastTrack.Runtime.UniforgeEntity>(FindObjectsSortMode.None);
                int attachedCount = 0;

                foreach (var entityId in entityIds)
                {
                    // UniforgeEntity 컴포넌트로 직접 찾기 (더 안정적)
                    GameObject target = null;
                    foreach (var entity in allEntities)
                    {
                        if (entity.EntityId == entityId)
                        {
                            target = entity.gameObject;
                            break;
                        }
                    }

                    if (target != null)
                    {
                        UniforgeScriptGenerator.AttachScript(target, entityId);
                        attachedCount++;
                        Debug.Log($"<color=green>[UniforgeImporter]</color> 스크립트 attached: {entityId} -> {target.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[UniforgeImporter] GameObject를 찾을 수 없음: {entityId}");
                    }
                }

                Debug.Log($"<color=green>[UniforgeImporter]</color> 총 {attachedCount}/{entityIds.Count}개 스크립트 attach 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniforgeImporter] 스크립트 attach 실패: {e.Message}\n{e.StackTrace}");
                EditorPrefs.DeleteKey(PendingAttachmentsKey);
            }
        }

        private static GameObject FindEntityGameObject(string entityId, GameObject entitiesRoot, GameObject prefabsRoot)
        {
            // Entities에서 찾기
            if (entitiesRoot != null)
            {
                foreach (Transform child in entitiesRoot.transform)
                {
                    var entity = child.GetComponent<Uniforge.FastTrack.Runtime.UniforgeEntity>();
                    if (entity != null && entity.EntityId == entityId)
                        return child.gameObject;
                }
            }

            // Prefabs에서 찾기
            if (prefabsRoot != null)
            {
                foreach (Transform child in prefabsRoot.transform)
                {
                    var entity = child.GetComponent<Uniforge.FastTrack.Runtime.UniforgeEntity>();
                    if (entity != null && entity.EntityId == entityId)
                        return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 스크립트 생성 후 pending list를 EditorPrefs에 저장
        /// </summary>
        private static void SavePendingAttachments(List<string> entityIds)
        {
            if (entityIds == null || entityIds.Count == 0) return;
            string json = JsonConvert.SerializeObject(entityIds);
            EditorPrefs.SetString(PendingAttachmentsKey, json);
            Debug.Log($"<color=cyan>[UniforgeImporter]</color> {entityIds.Count}개 엔티티 pending 저장됨");
        }
    }
}
