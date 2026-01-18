using UnityEngine;
using System.Collections.Generic;

namespace Uniforge.FastTrack.Runtime
{
    public class UniforgeEntity : MonoBehaviour
    {
        public string EntityId;
        public string EntityName;
        public string Role = "neutral";
        public List<string> Tags = new List<string>();
    }
}
