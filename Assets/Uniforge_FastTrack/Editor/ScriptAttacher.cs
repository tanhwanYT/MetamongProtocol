using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Handles attaching generated scripts to Unity GameObjects.
    /// </summary>
    public static class ScriptAttacher
    {
        private const string GeneratedFolderPath = "Assets/Uniforge_FastTrack/Generated";
        private const string PendingAttachmentsKey = "Uniforge_PendingAttachments";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.HasKey(PendingAttachmentsKey))
            {
                AttachScriptsFromJson();
                EditorPrefs.DeleteKey(PendingAttachmentsKey);
            }
        }

        public static void AttachScriptsToEntities()
        {
            // Set flag to run attachment after compilation
            EditorPrefs.SetBool(PendingAttachmentsKey, true);
            AssetDatabase.Refresh();
        }

        public static void AttachScriptsFromJson()
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "project.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning("[Uniforge] project.json not found for script attachment.");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            try
            {
                var gameData = Newtonsoft.Json.JsonConvert.DeserializeObject<GameDataJSON>(json);
                if (gameData?.scenes == null) return;

                foreach (var scene in gameData.scenes)
                {
                    if (scene.entities == null) continue;

                    foreach (var entity in scene.entities)
                    {
                        var go = GameObject.Find(entity.id); // Try finding by ID first check
                        if (go == null) 
                        {
                            // Try finding by name (fallback, less reliable if duplicates)
                            go = GameObject.Find(entity.name);
                        }

                        if (go != null)
                        {
                            // Check if script exists
                            string className = $"Gen_{entity.id.Replace("-", "_")}";
                            var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                            var type = assembly.GetType(className);

                            if (type != null)
                            {
                                if (go.GetComponent(type) == null)
                                {
                                    go.AddComponent(type);
                                    Debug.Log($"[Uniforge] Attached {className} to {go.name}");
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Uniforge] Script Attachment Failed: {ex.Message}");
            }
        }

        public static void AttachScript(GameObject go, string entityId)
        {
            if (go == null)
            {
                Debug.LogWarning($"[ScriptAttacher] GameObject is null for entityId: {entityId}");
                return;
            }
            if (string.IsNullOrEmpty(entityId))
            {
                Debug.LogWarning($"[ScriptAttacher] EntityId is null/empty for GameObject: {go.name}");
                return;
            }

            string className = $"Gen_{entityId.Replace("-", "_")}";

            try
            {
                // Try to load from Assembly-CSharp (Runtime scripts)
                var assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
                if (assembly == null)
                {
                    Debug.LogError($"[ScriptAttacher] Assembly-CSharp not loaded!");
                    return;
                }

                var type = assembly.GetType(className);

                if (type == null)
                {
                    Debug.LogWarning($"[ScriptAttacher] Class not found: {className}. Script might not be compiled yet.");
                    return;
                }

                if (go.GetComponent(type) == null)
                {
                    go.AddComponent(type);
                    Debug.Log($"<color=green>[ScriptAttacher]</color> Attached {className} to {go.name}");
                }
                else
                {
                    Debug.Log($"[ScriptAttacher] {className} already attached to {go.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScriptAttacher] Failed to attach {className}: {ex.Message}");
            }
        }
    }
}
