using UnityEngine;
namespace Metamong.Shared.DTO
{
    [System.Serializable]
    public class ConditionDTO
    {
        public string type;

        // 공통 필드 (옵션)
        public string name;
        public float? value;
        public string op;
        public string key;
    }
}