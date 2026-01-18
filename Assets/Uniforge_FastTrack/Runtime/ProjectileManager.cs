using UnityEngine;

namespace Uniforge.FastTrack.Runtime
{
    public class ProjectileManager : MonoBehaviour
    {
        public GameObject SpawnProjectile(string prefabId, Vector3 position, Vector3 direction, float speed)
        {
            var projectile = PrefabRegistry.SpawnStatic(prefabId, position);
            if (projectile != null)
            {
                var rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = direction.normalized * speed;
                }
            }
            return projectile;
        }

        public static GameObject SpawnProjectileStatic(string prefabId, Vector3 position, Vector3 direction, float speed)
        {
            if (UniforgeRuntime.Instance?.Projectiles != null)
                return UniforgeRuntime.Instance.Projectiles.SpawnProjectile(prefabId, position, direction, speed);
            return null;
        }
    }
}
