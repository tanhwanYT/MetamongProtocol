using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Uniforge.FastTrack.Runtime
{
    public class PrefabRegistry : MonoBehaviour
    {
        [Header("Registered Prefabs")]
        public List<PrefabEntry> Prefabs = new List<PrefabEntry>();

        private Dictionary<string, GameObject> _registry = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _runtimeTemplates = new Dictionary<string, GameObject>();

        void Awake()
        {
            foreach (var entry in Prefabs)
            {
                if (entry.Prefab != null && !string.IsNullOrEmpty(entry.Id))
                {
                    _registry[entry.Id] = entry.Prefab;
                }
            }
        }

        public void RegisterTemplate(string id, GameObject template)
        {
            _runtimeTemplates[id] = template;
        }

        public GameObject Spawn(string templateId, Vector3 position, Quaternion rotation = default)
        {
            if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0 && rotation.w == 0)
                rotation = Quaternion.identity;

            GameObject prefab = GetPrefab(templateId);
            if (prefab == null)
            {
                Debug.LogWarning($"[PrefabRegistry] Template not found: {templateId}");
                return null;
            }

            GameObject spawned = Instantiate(prefab, position, rotation);
            AttachGeneratedScript(spawned, templateId);
            return spawned;
        }

        private void AttachGeneratedScript(GameObject go, string entityId)
        {
            if (go == null || string.IsNullOrEmpty(entityId)) return;

            string className = $"Gen_{entityId.Replace("-", "_")}";

            try
            {
                Assembly assembly = Assembly.Load("Assembly-CSharp");
                if (assembly == null) return;

                System.Type scriptType = assembly.GetType(className);
                if (scriptType == null)
                    scriptType = assembly.GetType($"Uniforge.FastTrack.Generated.{className}");

                if (scriptType != null && go.GetComponent(scriptType) == null)
                {
                    go.AddComponent(scriptType);
                    Debug.Log($"<color=green>[PrefabRegistry]</color> Attached {className} to {go.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PrefabRegistry] Failed to attach script: {ex.Message}");
            }
        }

        public static GameObject SpawnStatic(string templateId, Vector3 position)
        {
            if (UniforgeRuntime.Instance?.Prefabs != null)
                return UniforgeRuntime.Instance.Prefabs.Spawn(templateId, position);
            Debug.LogWarning($"[PrefabRegistry] Runtime not initialized. Cannot spawn: {templateId}");
            return null;
        }

        public static GameObject SpawnSelfStatic(GameObject self, Vector3 position)
        {
            if (UniforgeRuntime.Instance?.Prefabs != null)
                return UniforgeRuntime.Instance.Prefabs.SpawnSelf(self, position);
            return Instantiate(self, position, self.transform.rotation);
        }

        public GameObject SpawnSelf(GameObject self, Vector3 position)
        {
            return Instantiate(self, position, self.transform.rotation);
        }

        private GameObject GetPrefab(string templateId)
        {
            if (_runtimeTemplates.TryGetValue(templateId, out var runtimePrefab))
                return runtimePrefab;

            if (_registry.TryGetValue(templateId, out var prefab))
                return prefab;

            // Find by UniforgeEntity.EntityId
            var allEntities = FindObjectsByType<UniforgeEntity>(FindObjectsSortMode.None);
            foreach (var entity in allEntities)
            {
                if (entity.EntityId == templateId)
                {
                    _runtimeTemplates[templateId] = entity.gameObject;
                    return entity.gameObject;
                }
            }

            var found = GameObject.Find(templateId);
            if (found != null)
                _runtimeTemplates[templateId] = found;
            return found;
        }
    }

    [System.Serializable]
    public class PrefabEntry
    {
        public string Id;
        public GameObject Prefab;
    }
}
