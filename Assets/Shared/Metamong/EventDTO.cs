using Newtonsoft.Json.Linq;
using System.Collections.Generic;
namespace Metamong.Shared.DTO
{
    [System.Serializable]
    public class EventDTO
    {
        public string id;

        // Event
        public string trigger;
        public JObject triggerParams;

        // Condition
        public string conditionLogic; // AND / OR
        public List<ConditionDTO> conditions;

        // Action
        public string action;
        public JObject @params;
    }
}