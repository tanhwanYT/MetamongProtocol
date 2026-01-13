using UnityEngine.LightTransport;
using UnityEngine;
using System.Collections.Generic;

namespace Metamong.Runtime.Actions
{
    public class ActionContext
    {
        public GameObject Owner;
        public float DeltaTime;
        public IWorld World;
        public ISignalBus Signals;
    }
    public interface IWorld
    {
        GameObject GetEntity(string id);
        IEnumerable<GameObject> GetEntitiesInRange(GameObject origin, float range);
        GameObject Spawn(string prefabId, Vector3 position);
    }

    public interface ISignalBus
    {
        void Emit(string signal, object payload = null);
        bool Has(string signal);
        void Clear(string signal);
    }
}