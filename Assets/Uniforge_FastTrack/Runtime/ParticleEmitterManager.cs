using UnityEngine;
using System.Collections.Generic;

namespace Uniforge.FastTrack
{
    /// <summary>
    /// Manages particle emitter instances at runtime.
    /// Provides static methods for starting/stopping emitters from generated code.
    /// </summary>
    public class ParticleEmitterManager : MonoBehaviour
    {
        private static ParticleEmitterManager _instance;
        private static Dictionary<string, GameObject> _particlePrefabs = new Dictionary<string, GameObject>();

        public static ParticleEmitterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[ParticleEmitterManager]");
                    _instance = go.AddComponent<ParticleEmitterManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Register a particle prefab for runtime use.
        /// </summary>
        public static void RegisterPrefab(string particleSystemId, GameObject prefab)
        {
            if (!string.IsNullOrEmpty(particleSystemId) && prefab != null)
            {
                _particlePrefabs[particleSystemId] = prefab;
            }
        }

        /// <summary>
        /// Start a particle emitter.
        /// </summary>
        /// <param name="emitterId">Unique ID for this emitter instance</param>
        /// <param name="particleSystemId">ID of the particle system definition</param>
        /// <param name="parent">Optional parent transform to attach to</param>
        /// <param name="offset">Position offset from parent (or world position if no parent)</param>
        /// <returns>Transform of the created emitter, or null if failed</returns>
        public static Transform StartEmitter(string emitterId, string particleSystemId, Transform parent, Vector3 offset)
        {
            // Try to find the particle prefab
            GameObject prefab = null;

            // First check registered prefabs
            if (_particlePrefabs.TryGetValue(particleSystemId, out prefab))
            {
                // Found in registry
            }
            else
            {
                // Try to load from Resources
                prefab = Resources.Load<GameObject>($"Particles/{particleSystemId}");
            }

            if (prefab == null)
            {
                // Try built-in presets
                prefab = GetBuiltInPreset(particleSystemId);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[ParticleEmitterManager] Particle system not found: {particleSystemId}");
                return null;
            }

            // Instantiate the particle system
            Vector3 position = parent != null ? parent.position + offset : offset;
            var instance = Instantiate(prefab, position, Quaternion.identity);
            instance.name = $"Emitter_{emitterId}";

            // Attach to parent if specified
            if (parent != null)
            {
                instance.transform.SetParent(parent);
                instance.transform.localPosition = offset;
            }

            // Start the particle system
            var ps = instance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            Debug.Log($"[ParticleEmitterManager] Started emitter: {emitterId} (system: {particleSystemId})");
            return instance.transform;
        }

        /// <summary>
        /// Start emitter at a specific world position.
        /// </summary>
        public static Transform StartEmitterAtPosition(string emitterId, string particleSystemId, Vector3 worldPosition)
        {
            return StartEmitter(emitterId, particleSystemId, null, worldPosition);
        }

        /// <summary>
        /// Get a built-in particle preset.
        /// </summary>
        private static GameObject GetBuiltInPreset(string presetName)
        {
            // Create simple built-in presets for common effects
            GameObject prefab = new GameObject($"BuiltIn_{presetName}");
            var ps = prefab.AddComponent<ParticleSystem>();
            var renderer = prefab.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;

            switch (presetName?.ToLower())
            {
                case "fire":
                    main.startLifetime = 0.8f;
                    main.startSpeed = 1f;
                    main.startSize = 0.3f;
                    main.startColor = new Color(1f, 0.5f, 0f, 1f);
                    main.gravityModifier = -0.5f;
                    emission.rateOverTime = 20f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 15f;
                    shape.radius = 0.1f;
                    ConfigureColorOverLifetime(ps, new Color(1f, 0.5f, 0f), new Color(1f, 0f, 0f, 0f));
                    break;

                case "smoke":
                    main.startLifetime = 2f;
                    main.startSpeed = 0.5f;
                    main.startSize = 0.2f;
                    main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    main.gravityModifier = -0.2f;
                    emission.rateOverTime = 10f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 20f;
                    ConfigureSizeOverLifetime(ps, 1f, 2f);
                    ConfigureColorOverLifetime(ps, new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(0.3f, 0.3f, 0.3f, 0f));
                    break;

                case "spark":
                case "sparks":
                    main.startLifetime = 0.5f;
                    main.startSpeed = 3f;
                    main.startSize = 0.1f;
                    main.startColor = new Color(1f, 1f, 0.5f, 1f);
                    main.gravityModifier = 1f;
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.1f;
                    ConfigureColorOverLifetime(ps, new Color(1f, 1f, 0.5f), new Color(1f, 0.5f, 0f, 0f));
                    main.loop = false;
                    break;

                case "dust":
                    main.startLifetime = 1.5f;
                    main.startSpeed = 0.3f;
                    main.startSize = 0.15f;
                    main.startColor = new Color(0.8f, 0.7f, 0.6f, 0.4f);
                    emission.rateOverTime = 15f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(1f, 0.1f, 1f);
                    ConfigureColorOverLifetime(ps, new Color(0.8f, 0.7f, 0.6f, 0.4f), new Color(0.8f, 0.7f, 0.6f, 0f));
                    break;

                case "rain":
                    main.startLifetime = 1f;
                    main.startSpeed = 5f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                    main.startColor = new Color(0.7f, 0.8f, 1f, 0.6f);
                    main.gravityModifier = 2f;
                    emission.rateOverTime = 100f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(10f, 0.1f, 1f);
                    shape.position = new Vector3(0f, 5f, 0f);
                    break;

                case "snow":
                    main.startLifetime = 5f;
                    main.startSpeed = 0.5f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                    main.startColor = Color.white;
                    main.gravityModifier = 0.1f;
                    emission.rateOverTime = 30f;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(10f, 0.1f, 1f);
                    shape.position = new Vector3(0f, 5f, 0f);
                    // Add some horizontal movement
                    var vel = ps.velocityOverLifetime;
                    vel.enabled = true;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    break;

                case "explosion":
                    main.startLifetime = 0.5f;
                    main.startSpeed = 5f;
                    main.startSize = 0.3f;
                    main.startColor = new Color(1f, 0.7f, 0.2f, 1f);
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.2f;
                    ConfigureColorOverLifetime(ps, new Color(1f, 0.7f, 0.2f), new Color(0.3f, 0.1f, 0f, 0f));
                    ConfigureSizeOverLifetime(ps, 1f, 0.2f);
                    main.loop = false;
                    break;

                case "hit_spark":
                case "impact":
                    main.startLifetime = 0.3f;
                    main.startSpeed = 2f;
                    main.startSize = 0.15f;
                    main.startColor = new Color(1f, 1f, 1f, 1f);
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.05f;
                    ConfigureColorOverLifetime(ps, Color.white, new Color(1f, 1f, 1f, 0f));
                    main.loop = false;
                    break;

                default:
                    // Default simple particle
                    main.startLifetime = 1f;
                    main.startSpeed = 1f;
                    main.startSize = 0.2f;
                    emission.rateOverTime = 10f;
                    Debug.LogWarning($"[ParticleEmitterManager] Using default preset for unknown: {presetName}");
                    break;
            }

            return prefab;
        }

        private static void ConfigureColorOverLifetime(ParticleSystem ps, Color startColor, Color endColor)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(endColor.a, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ConfigureSizeOverLifetime(ParticleSystem ps, float startScale, float endScale)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, startScale),
                new Keyframe(1f, endScale)
            ));
        }
    }
}
