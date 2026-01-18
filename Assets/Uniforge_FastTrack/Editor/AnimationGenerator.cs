using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Creates AnimatorControllers and AnimationClips from sprite sheet assets.
    /// Called during import to enable automatic animation playback.
    /// </summary>
    public static class AnimationGenerator
    {
        private static string GeneratedAnimPath = "Assets/Uniforge_FastTrack/Generated/Animations";

        /// <summary>
        /// Generate all animations for an entity based on its texture asset.
        /// Extracts required animation names from entity logic and ensures all states exist.
        /// </summary>
        public static RuntimeAnimatorController GenerateForEntity(EntityJSON entity, List<AssetDetailJSON> assets)
        {
            Debug.Log($"[AnimationGenerator] GenerateForEntity called for '{entity.name}' (id={entity.id}, texture={entity.texture})");

            if (string.IsNullOrEmpty(entity.texture))
            {
                Debug.Log($"[AnimationGenerator] Entity '{entity.name}' has no texture, skipping animation generation");
                return null;
            }

            var asset = assets?.FirstOrDefault(a => a.name == entity.texture || a.id == entity.texture);
            if (asset == null)
            {
                Debug.LogWarning($"[AnimationGenerator] Asset not found for texture '{entity.texture}' in entity '{entity.name}'. Available assets: {string.Join(", ", assets?.Select(a => a.name) ?? new string[0])}");
                return null;
            }

            // Check if entity implementation needs animation
            bool force = HasAnimationUsage(entity);
            Debug.Log($"[AnimationGenerator] Entity '{entity.name}' force={force} (has PlayAnimation usage)");

            // Extract all animation names used by PlayAnimation actions in entity logic
            var requiredAnimations = ExtractRequiredAnimations(entity);
            Debug.Log($"[AnimationGenerator] Entity '{entity.name}' requires animations: [{string.Join(", ", requiredAnimations)}]");

            // Force generation if there are required animations
            if (requiredAnimations.Count > 0)
            {
                force = true;
            }

            // Try with metadata first, then try heuristic
            var controller = GenerateFromAsset(asset, force, requiredAnimations);
            if (controller != null)
            {
                Debug.Log($"[AnimationGenerator] Successfully created controller for '{entity.name}'");
            }
            else
            {
                Debug.LogWarning($"[AnimationGenerator] Failed to create controller for '{entity.name}'");
            }
            return controller;
        }

        /// <summary>
        /// Extracts all animation names used by PlayAnimation actions in entity logic.
        /// </summary>
        public static HashSet<string> ExtractRequiredAnimations(EntityJSON entity)
        {
            var result = new HashSet<string>();

            // Extract from events
            if (entity.events != null)
            {
                foreach (var evt in entity.events)
                {
                    ExtractAnimationsFromActions(evt.actions, result);
                    ExtractAnimationsFromActions(evt.elseActions, result);

                    // Single action format
                    if (evt.action == "PlayAnimation" && evt.@params != null)
                    {
                        var animName = ParameterHelper.GetParamString(evt.@params, "animationName");
                        if (!string.IsNullOrEmpty(animName)) result.Add(animName);
                    }
                }
            }

            // Extract from logic items (Frontend format)
            if (entity.logic != null)
            {
                foreach (var item in entity.logic)
                {
                    ComponentJSON comp = item.component ?? new ComponentJSON
                    {
                        actions = item.actions
                    };
                    if (comp?.actions != null)
                    {
                        ExtractAnimationsFromActions(comp.actions, result);
                    }
                }
            }

            // Extract from components
            if (entity.components != null)
            {
                foreach (var comp in entity.components)
                {
                    ExtractAnimationsFromActions(comp.actions, result);
                    ExtractAnimationsFromActions(comp.elseActions, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts animation names from a list of actions, including nested If actions.
        /// </summary>
        private static void ExtractAnimationsFromActions(List<ActionJSON> actions, HashSet<string> result)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action.type == "PlayAnimation")
                {
                    var p = action.GetAllParams();
                    // Try multiple parameter name variations
                    var animName = ParameterHelper.GetParamString(p, "animationName");
                    if (string.IsNullOrEmpty(animName))
                        animName = ParameterHelper.GetParamString(p, "animation");
                    if (string.IsNullOrEmpty(animName))
                        animName = ParameterHelper.GetParamString(p, "name");
                    if (string.IsNullOrEmpty(animName))
                        animName = ParameterHelper.GetParamString(p, "anim");
                    if (string.IsNullOrEmpty(animName))
                        animName = ParameterHelper.GetParamString(p, "state");

                    if (!string.IsNullOrEmpty(animName))
                    {
                        result.Add(animName);
                        Debug.Log($"[AnimationGenerator] Found PlayAnimation: {animName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AnimationGenerator] PlayAnimation action found but no animation name in params: {string.Join(", ", p.Keys)}");
                    }
                }
                else if (action.type == "If")
                {
                    // Check nested actions in If blocks
                    var p = action.GetAllParams();
                    if (p.TryGetValue("then", out var thenObj) && thenObj is JArray thenArr)
                    {
                        var thenActions = thenArr.ToObject<List<ActionJSON>>();
                        ExtractAnimationsFromActions(thenActions, result);
                    }
                    if (p.TryGetValue("else", out var elseObj) && elseObj is JArray elseArr)
                    {
                        var elseActions = elseArr.ToObject<List<ActionJSON>>();
                        ExtractAnimationsFromActions(elseActions, result);
                    }
                }
            }
        }

        private static bool HasAnimationUsage(EntityJSON entity)
        {
            if (entity.logic == null && (entity.events == null || entity.events.Count == 0)) return false;
            
            // Quick string search for PlayAnimation action type
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
            return json.Contains("\"PlayAnimation\"") || json.Contains("'PlayAnimation'");
        }

        /// <summary>
        /// Convert metadata object to AssetMetadataJSON
        /// </summary>
        private static AssetMetadataJSON GetMetadata(object metadata)
        {
            if (metadata == null) return null;
            if (metadata is AssetMetadataJSON typed) return typed;
            if (metadata is JObject jo) return jo.ToObject<AssetMetadataJSON>();
            try { return JObject.FromObject(metadata).ToObject<AssetMetadataJSON>(); }
            catch { return null; }
        }

        /// <summary>
        /// Generate AnimatorController from asset metadata or heuristic detection.
        /// </summary>
        /// <param name="asset">Asset to generate animation for</param>
        /// <param name="force">Force generation even without metadata</param>
        /// <param name="requiredAnimations">Animation names required by PlayAnimation actions</param>
        public static RuntimeAnimatorController GenerateFromAsset(AssetDetailJSON asset, bool force = false, HashSet<string> requiredAnimations = null)
        {
            var meta = GetMetadata(asset.metadata);
            requiredAnimations = requiredAnimations ?? new HashSet<string>();

            Debug.Log($"[AnimationGenerator] GenerateFromAsset: {asset.name}, force={force}, requiredAnimations=[{string.Join(", ", requiredAnimations)}]");

            // Ensure directory exists
            if (!Directory.Exists(GeneratedAnimPath))
            {
                Directory.CreateDirectory(GeneratedAnimPath);
                AssetDatabase.Refresh();
            }

            string safeName = SanitizeName(asset.name);
            string controllerPath = $"{GeneratedAnimPath}/{safeName}_Controller.controller";

            // Check if already exists - but may need to update if required animations changed
            var existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existingController != null)
            {
                // Check if all required animations exist as states
                bool needsUpdate = false;
                var stateMachine = existingController.layers[0].stateMachine;
                var existingStates = new HashSet<string>(stateMachine.states.Select(s => s.state.name));

                Debug.Log($"[AnimationGenerator] Existing controller states: [{string.Join(", ", existingStates)}]");

                foreach (var animName in requiredAnimations)
                {
                    if (!existingStates.Contains(animName))
                    {
                        needsUpdate = true;
                        Debug.Log($"[AnimationGenerator] Controller missing required state: {animName}");
                    }
                }

                if (!needsUpdate && requiredAnimations.Count > 0)
                {
                    Debug.Log($"[AnimationGenerator] All required states exist, using existing controller");
                    return existingController;
                }
                else if (!needsUpdate)
                {
                    // No required animations specified - check if force is needed
                    if (!force)
                    {
                        return existingController;
                    }
                }

                // Delete existing controller to regenerate
                AssetDatabase.DeleteAsset(controllerPath);
                Debug.Log($"[AnimationGenerator] Regenerating controller for {asset.name}");
            }

            // Load sprites - try existing sliced sprites first
            var sprites = LoadSpritesFromAsset(asset);

            // If no sliced sprites and no metadata, try heuristic slicing
            if ((sprites == null || sprites.Length <= 1) && meta == null)
            {
                var heuristicResult = TryHeuristicSlicing(asset);
                if (heuristicResult.sprites != null && heuristicResult.sprites.Length > 1)
                {
                    sprites = heuristicResult.sprites;
                    meta = heuristicResult.metadata;
                    Debug.Log($"[AnimationGenerator] Heuristic slicing applied: {asset.name} -> {sprites.Length} frames");
                }
            }

            // If still no valid sprites or metadata, skip unless forced
            if ((sprites == null || sprites.Length == 0) && !force)
            {
                Debug.Log($"[AnimationGenerator] No sprites found for {asset.name}, skipping (force={force})");
                return null;
            }

            // Handle case where we have no sprites but need to create controller anyway (force=true)
            if (sprites == null || sprites.Length == 0)
            {
                Debug.Log($"[AnimationGenerator] No sprites but force=true, creating empty controller with required states");
                return CreateEmptyControllerWithStates(controllerPath, requiredAnimations);
            }

            if (meta == null)
            {
                // Create default metadata from sprite count
                meta = new AssetMetadataJSON
                {
                    frameCount = sprites.Length,
                    frameWidth = (int)sprites[0].rect.width,
                    frameHeight = (int)sprites[0].rect.height
                };
            }

            // Skip if only 1 frame and no explicit animations, unless forced
            if (!force && meta.frameCount <= 1 && (meta.animations == null || meta.animations.Count == 0))
            {
                Debug.Log($"[AnimationGenerator] Only 1 frame and no animations defined, skipping (force={force})");
                return null;
            }

            // If we have required animations but only 1 sprite, still create controller
            if (force && sprites.Length == 1 && requiredAnimations != null && requiredAnimations.Count > 0)
            {
                Debug.Log($"[AnimationGenerator] Only 1 sprite but required animations exist, creating controller with single-frame states");
            }

            // Create new controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Track created states and clips for reuse
            var createdStates = new Dictionary<string, AnimatorState>();
            AnimationClip defaultClip = null;

            // Create animations from metadata
            if (meta.animations != null && meta.animations.Count > 0)
            {
                bool isFirstState = true;
                foreach (var kvp in meta.animations)
                {
                    var clip = CreateAnimationClip(safeName, kvp.Key, kvp.Value, sprites);
                    if (clip != null)
                    {
                        var state = rootStateMachine.AddState(kvp.Key);
                        state.motion = clip;
                        createdStates[kvp.Key] = state;

                        if (isFirstState)
                        {
                            rootStateMachine.defaultState = state;
                            defaultClip = clip;
                            isFirstState = false;
                        }
                    }
                }
            }
            else
            {
                // Create default animation using all frames
                var defaultDef = new AnimationDefJSON
                {
                    startFrame = 0,
                    endFrame = meta.frameCount - 1,
                    frameRate = 12,
                    loop = true
                };
                var clip = CreateAnimationClip(safeName, "default", defaultDef, sprites);
                if (clip != null)
                {
                    var state = rootStateMachine.AddState("default");
                    state.motion = clip;
                    rootStateMachine.defaultState = state;
                    createdStates["default"] = state;
                    defaultClip = clip;
                }
            }

            // Add missing required animations as alias states using default clip
            // This ensures PlayAnimation("walk") works even if "walk" isn't in metadata
            if (requiredAnimations != null && requiredAnimations.Count > 0 && defaultClip != null)
            {
                foreach (var animName in requiredAnimations)
                {
                    if (!createdStates.ContainsKey(animName))
                    {
                        // Create a state that uses the default clip as fallback
                        var aliasState = rootStateMachine.AddState(animName);
                        aliasState.motion = defaultClip;
                        createdStates[animName] = aliasState;
                        Debug.Log($"[AnimationGenerator] Created alias state '{animName}' using default animation for {asset.name}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationGenerator] Created controller: {controllerPath}");

            return controller;
        }

        /// <summary>
        /// Try to detect horizontal strip and slice automatically.
        /// Returns sliced sprites and generated metadata.
        /// </summary>
        private static (Sprite[] sprites, AssetMetadataJSON metadata) TryHeuristicSlicing(AssetDetailJSON asset)
        {
            // Find the texture in project
            string[] guids = AssetDatabase.FindAssets($"t:Texture2D {asset.name}");
            if (guids.Length == 0) return (null, null);

            string texturePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) return (null, null);

            int width = texture.width;
            int height = texture.height;

            // Heuristic: horizontal strip detection
            // width > height AND width is divisible by height AND ratio > 1
            if (width > height && width % height == 0 && width / height > 1)
            {
                int frameCount = width / height;
                int frameWidth = height; // Square frames
                int frameHeight = height;

                // Modify texture import settings to slice as sprite sheet
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null && importer.spriteImportMode != SpriteImportMode.Multiple)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.filterMode = FilterMode.Point;

                    // Create sprite sheet metadata (using legacy API with warning suppression)
#pragma warning disable CS0618 // Type or member is obsolete
                    var spritesheet = new List<SpriteMetaData>();
                    for (int i = 0; i < frameCount; i++)
                    {
                        spritesheet.Add(new SpriteMetaData
                        {
                            name = $"{asset.name}_{i}",
                            rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                            pivot = new Vector2(0.5f, 0.5f),
                            alignment = (int)SpriteAlignment.Center
                        });
                    }
                    importer.spritesheet = spritesheet.ToArray();
#pragma warning restore CS0618

                    // Reimport
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh();

                    Debug.Log($"[AnimationGenerator] Sliced texture: {asset.name} into {frameCount} frames");
                }

                // Load the newly sliced sprites
                var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                    .OfType<Sprite>()
                    .OrderBy(s => s.name)
                    .ToArray();

                var metadata = new AssetMetadataJSON
                {
                    frameWidth = frameWidth,
                    frameHeight = frameHeight,
                    frameCount = frameCount
                };

                return (sprites, metadata);
            }

            return (null, null);
        }

        private static Sprite[] LoadSpritesFromAsset(AssetDetailJSON asset)
        {
            // Try to find already imported texture in project by name
            string[] guids = AssetDatabase.FindAssets($"t:Texture2D {asset.name}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .OrderBy(s => s.name)
                    .ToArray();

                if (sprites.Length > 0) return sprites;
            }

            // Fallback: Try to find by URL hash (files are saved as asset_XXXX.png)
            if (!string.IsNullOrEmpty(asset.url))
            {
                int urlHash = asset.url.GetHashCode();
                string expectedFileName = $"asset_{urlHash}";
                
                guids = AssetDatabase.FindAssets($"t:Texture2D {expectedFileName}");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                        .OfType<Sprite>()
                        .OrderBy(s => s.name)
                        .ToArray();

                    if (sprites.Length > 0)
                    {
                        Debug.Log($"[AnimationGenerator] Found sprite by URL hash: {path}");
                        return sprites;
                    }
                }
            }

            // Last resort: Search in Textures folder directly
            string texturesPath = "Assets/Uniforge_FastTrack/Textures";
            if (Directory.Exists(texturesPath))
            {
                var allTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath });
                foreach (var guid in allTextures)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                        .OfType<Sprite>()
                        .OrderBy(s => s.name)
                        .ToArray();

                    // Match by checking if URL hash is in filename
                    if (!string.IsNullOrEmpty(asset.url))
                    {
                        int urlHash = asset.url.GetHashCode();
                        if (path.Contains(urlHash.ToString()) && sprites.Length > 0)
                        {
                            Debug.Log($"[AnimationGenerator] Found sprite by direct search: {path}");
                            return sprites;
                        }
                    }
                }
            }

            return null;
        }

        private static AnimationClip CreateAnimationClip(string assetName, string animName, AnimationDefJSON def, Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return null;

            int start = Mathf.Clamp(def.startFrame, 0, sprites.Length - 1);
            int end = Mathf.Clamp(def.endFrame, start, sprites.Length - 1);
            int frameRate = def.frameRate > 0 ? def.frameRate : 12;

            var clip = new AnimationClip();
            clip.name = $"{assetName}_{animName}";
            clip.frameRate = frameRate;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = def.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Create keyframes
            var keyframes = new List<ObjectReferenceKeyframe>();
            float frameDuration = 1f / frameRate;

            for (int i = start; i <= end; i++)
            {
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = (i - start) * frameDuration,
                    value = sprites[i]
                });
            }

            // Create binding
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());

            // Save clip
            string clipPath = $"{GeneratedAnimPath}/{clip.name}.anim";
            AssetDatabase.CreateAsset(clip, clipPath);

            return clip;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            var result = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result.Append(c);
                else
                    result.Append('_');
            }
            return result.ToString();
        }

        /// <summary>
        /// Creates an empty AnimatorController with states for required animations.
        /// Used when sprites aren't available but we need animation states.
        /// </summary>
        private static RuntimeAnimatorController CreateEmptyControllerWithStates(string controllerPath, HashSet<string> requiredAnimations)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Create an empty animation clip for placeholder
            string clipPath = controllerPath.Replace(".controller", "_empty.anim");
            var emptyClip = new AnimationClip();
            emptyClip.name = "empty";
            emptyClip.frameRate = 12;
            AssetDatabase.CreateAsset(emptyClip, clipPath);

            bool isFirst = true;
            foreach (var animName in requiredAnimations)
            {
                var state = rootStateMachine.AddState(animName);
                state.motion = emptyClip;

                if (isFirst)
                {
                    rootStateMachine.defaultState = state;
                    isFirst = false;
                }
                Debug.Log($"[AnimationGenerator] Created empty state '{animName}'");
            }

            // If no required animations, create a default state
            if (requiredAnimations == null || requiredAnimations.Count == 0)
            {
                var defaultState = rootStateMachine.AddState("default");
                defaultState.motion = emptyClip;
                rootStateMachine.defaultState = defaultState;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationGenerator] Created empty controller with {requiredAnimations?.Count ?? 0} states: {controllerPath}");

            return controller;
        }

        /// <summary>
        /// Menu item to clear all generated animation controllers.
        /// Use this when animation states are out of sync.
        /// </summary>
        [UnityEditor.MenuItem("Uniforge/Clear Generated Animations")]
        public static void ClearGeneratedAnimations()
        {
            if (!Directory.Exists(GeneratedAnimPath))
            {
                Debug.Log("[AnimationGenerator] No generated animations folder found.");
                return;
            }

            // Delete all .controller and .anim files
            var controllerFiles = Directory.GetFiles(GeneratedAnimPath, "*.controller");
            var animFiles = Directory.GetFiles(GeneratedAnimPath, "*.anim");

            int count = 0;
            foreach (var file in controllerFiles)
            {
                AssetDatabase.DeleteAsset(file.Replace("\\", "/"));
                count++;
            }
            foreach (var file in animFiles)
            {
                AssetDatabase.DeleteAsset(file.Replace("\\", "/"));
                count++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[AnimationGenerator] Cleared {count} generated animation files. Re-import your game JSON to regenerate.");
        }
    }
}
