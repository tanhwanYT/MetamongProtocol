using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Imports custom particle system definitions from JSON protocol.
    /// Converts ParticleSystemJSON to Unity ParticleSystem prefabs.
    /// </summary>
    public static class ParticleImporter
    {
        private static string GeneratedParticlePath = "Assets/Uniforge_FastTrack/Generated/Particles";

        // Cache of generated particle prefabs
        private static Dictionary<string, GameObject> _particlePrefabs = new Dictionary<string, GameObject>();

        /// <summary>
        /// Import all particle systems from game data.
        /// </summary>
        public static void ImportAll(GameDataJSON gameData)
        {
            if (gameData?.particles == null || gameData.particles.Count == 0) return;

            // Ensure directory exists
            if (!Directory.Exists(GeneratedParticlePath))
            {
                Directory.CreateDirectory(GeneratedParticlePath);
                AssetDatabase.Refresh();
            }

            foreach (var particleDef in gameData.particles)
            {
                ImportParticleSystem(particleDef);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ParticleImporter] Imported {gameData.particles.Count} particle systems");
        }

        /// <summary>
        /// Import a single particle system definition.
        /// </summary>
        public static GameObject ImportParticleSystem(ParticleSystemJSON def)
        {
            if (def == null || string.IsNullOrEmpty(def.id)) return null;

            string safeName = SanitizeName(def.name ?? def.id);
            string prefabPath = $"{GeneratedParticlePath}/{safeName}.prefab";

            // Check if prefab already exists
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                _particlePrefabs[def.id] = existingPrefab;
                return existingPrefab;
            }

            // Create new particle system GameObject
            var go = new GameObject(safeName);
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            // Configure particle system from JSON definition
            ConfigureParticleSystem(ps, renderer, def);

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            _particlePrefabs[def.id] = prefab;
            Debug.Log($"[ParticleImporter] Created particle prefab: {prefabPath}");

            return prefab;
        }

        /// <summary>
        /// Configure Unity ParticleSystem from JSON definition.
        /// </summary>
        private static void ConfigureParticleSystem(ParticleSystem ps, ParticleSystemRenderer renderer, ParticleSystemJSON def)
        {
            // Main module
            var main = ps.main;
            main.duration = def.duration > 0 ? def.duration : 5f;
            main.loop = def.loop;
            main.playOnAwake = def.playOnAwake;
            main.maxParticles = def.maxParticles;

            // Lifetime (convert from seconds, no change needed)
            if (def.lifetimeVariance > 0)
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(
                    Mathf.Max(0.1f, def.lifetime - def.lifetimeVariance),
                    def.lifetime + def.lifetimeVariance
                );
            }
            else
            {
                main.startLifetime = def.lifetime;
            }

            // Size (convert from pixels to Unity units, divide by PPU 100)
            float startSize = def.startSize / 100f;
            float sizeVar = def.sizeVariance / 100f;
            if (sizeVar > 0)
            {
                main.startSize = new ParticleSystem.MinMaxCurve(
                    Mathf.Max(0.01f, startSize - sizeVar),
                    startSize + sizeVar
                );
            }
            else
            {
                main.startSize = startSize;
            }

            // Speed (convert from pixels/sec to units/sec)
            float startSpeed = def.startSpeed / 100f;
            float speedVar = def.speedVariance / 100f;
            if (speedVar > 0)
            {
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    Mathf.Max(0f, startSpeed - speedVar),
                    startSpeed + speedVar
                );
            }
            else
            {
                main.startSpeed = startSpeed;
            }

            // Rotation
            float rotVar = def.rotationVariance;
            if (rotVar > 0)
            {
                main.startRotation = new ParticleSystem.MinMaxCurve(
                    (def.startRotation - rotVar) * Mathf.Deg2Rad,
                    (def.startRotation + rotVar) * Mathf.Deg2Rad
                );
            }
            else
            {
                main.startRotation = def.startRotation * Mathf.Deg2Rad;
            }

            // Color
            if (def.startColor != null)
            {
                main.startColor = ConvertColor(def.startColor);
            }

            // Gravity (convert from web coordinates: positive Y = down)
            main.gravityModifier = def.gravityY / 100f; // Simplified - Unity gravity is Y-axis

            // Emission module
            var emission = ps.emission;
            emission.rateOverTime = def.emissionRate;

            if (def.burstCount > 0)
            {
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, (short)def.burstCount, (short)def.burstCount, 1, def.burstInterval)
                });
            }

            // Shape module
            var shape = ps.shape;
            shape.enabled = true;

            switch (def.shape?.ToLower())
            {
                case "point":
                    shape.shapeType = ParticleSystemShapeType.Sprite;
                    shape.radius = 0.001f;
                    break;
                case "circle":
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = def.shapeRadius / 100f;
                    shape.radiusThickness = def.shapeEdgeOnly ? 0f : 1f;
                    break;
                case "rectangle":
                case "box":
                    shape.shapeType = ParticleSystemShapeType.Rectangle;
                    shape.scale = new Vector3(def.shapeWidth / 100f, def.shapeHeight / 100f, 1f);
                    break;
                case "line":
                    shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
                    shape.radius = def.shapeWidth / 100f;
                    break;
                default:
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = def.angleVariance;
                    shape.radius = 0.01f;
                    break;
            }

            // Rotation to match emission angle (0 = right, 90 = up in web coords)
            // Unity: 0 = up, 90 = left. Convert: Unity = 90 - Web
            shape.rotation = new Vector3(0, 0, 90 - def.angle);

            // Size over lifetime (if endSize differs from startSize)
            if (Mathf.Abs(def.endSize - def.startSize) > 0.01f)
            {
                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                float ratio = def.startSize > 0 ? def.endSize / def.startSize : 1f;
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(1f, ratio)
                ));
            }

            // Color over lifetime (alpha fade and color gradient)
            if (def.startAlpha != def.endAlpha || def.endColor != null)
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;

                Color startCol = def.startColor != null ? ConvertColor(def.startColor) : Color.white;
                Color endCol = def.endColor != null ? ConvertColor(def.endColor) : startCol;

                startCol.a = def.startAlpha;
                endCol.a = def.endAlpha;

                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(startCol, 0f),
                        new GradientColorKey(endCol, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(def.startAlpha, 0f),
                        new GradientAlphaKey(def.endAlpha, 1f)
                    }
                );
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            // Rotation over lifetime
            if (Mathf.Abs(def.rotationSpeed) > 0.01f)
            {
                var rotOverLifetime = ps.rotationOverLifetime;
                rotOverLifetime.enabled = true;
                rotOverLifetime.z = def.rotationSpeed * Mathf.Deg2Rad;
            }

            // Velocity over lifetime (for speed changes)
            if (Mathf.Abs(def.endSpeed - def.startSpeed) > 0.01f)
            {
                var velOverLifetime = ps.velocityOverLifetime;
                velOverLifetime.enabled = true;
                float ratio = def.startSpeed > 0 ? def.endSpeed / def.startSpeed : 1f;
                velOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(1f, ratio)
                ));
            }

            // Renderer settings
            renderer.sortingOrder = 100; // Render above most sprites

            // Blend mode
            switch (def.blendMode?.ToLower())
            {
                case "additive":
                    renderer.material = GetAdditiveMaterial();
                    break;
                case "multiply":
                    renderer.material = GetMultiplyMaterial();
                    break;
                default:
                    renderer.material = GetDefaultMaterial();
                    break;
            }

            // Texture
            if (!string.IsNullOrEmpty(def.texture))
            {
                var texSprite = FindSpriteByName(def.texture);
                if (texSprite != null)
                {
                    renderer.material.mainTexture = texSprite.texture;
                }
            }
        }

        /// <summary>
        /// Convert ParticleColorJSON to Unity Color.
        /// </summary>
        private static Color ConvertColor(ParticleColorJSON colorDef)
        {
            if (!string.IsNullOrEmpty(colorDef.hex))
            {
                if (ColorUtility.TryParseHtmlString(colorDef.hex, out Color c))
                {
                    c.a = colorDef.a;
                    return c;
                }
            }
            return new Color(colorDef.r / 255f, colorDef.g / 255f, colorDef.b / 255f, colorDef.a);
        }

        /// <summary>
        /// Get or create default particle material.
        /// </summary>
        private static Material GetDefaultMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedParticlePath}/ParticleDefault.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Sprites/Default"));
                mat.name = "ParticleDefault";
                AssetDatabase.CreateAsset(mat, $"{GeneratedParticlePath}/ParticleDefault.mat");
            }
            return mat;
        }

        /// <summary>
        /// Get or create additive particle material.
        /// </summary>
        private static Material GetAdditiveMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedParticlePath}/ParticleAdditive.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.name = "ParticleAdditive";
                mat.SetFloat("_Mode", 1); // Additive
                AssetDatabase.CreateAsset(mat, $"{GeneratedParticlePath}/ParticleAdditive.mat");
            }
            return mat;
        }

        /// <summary>
        /// Get or create multiply particle material.
        /// </summary>
        private static Material GetMultiplyMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedParticlePath}/ParticleMultiply.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.name = "ParticleMultiply";
                mat.SetFloat("_Mode", 3); // Multiply
                AssetDatabase.CreateAsset(mat, $"{GeneratedParticlePath}/ParticleMultiply.mat");
            }
            return mat;
        }

        /// <summary>
        /// Find sprite asset by name.
        /// </summary>
        private static Sprite FindSpriteByName(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:Sprite {name}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        /// <summary>
        /// Get particle prefab by ID.
        /// </summary>
        public static GameObject GetParticlePrefab(string particleId)
        {
            if (_particlePrefabs.TryGetValue(particleId, out var prefab))
                return prefab;

            // Try to find in generated folder
            var guids = AssetDatabase.FindAssets($"t:Prefab", new[] { GeneratedParticlePath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null && (p.name == particleId || path.Contains(particleId)))
                {
                    _particlePrefabs[particleId] = p;
                    return p;
                }
            }

            return null;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Particle";
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
    }
}
