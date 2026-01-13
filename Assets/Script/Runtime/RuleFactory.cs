using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Metamong.Shared.DTO;
using Metamong.Runtime.Actions;

public static class RuleFactory
{
    // EventDTO RuntimeRule 변환
    public static RuntimeRule Create(EventDTO dto)
    {
        Debug.Log($"[RuleFactory] Create rule trigger={dto.trigger}");
        var rule = new RuntimeRule
        {
            trigger = ParseTrigger(dto.trigger),
            condition = CreateCondition(dto),
            action = CreateAction(dto)
        };

        Debug.Log($"[RuleFactory] -> trigger={rule.trigger}, " +
                  $"condition={(rule.condition != null)}, " +
                  $"action={(rule.action != null)}");

        return rule;

    }
    
    // trigger 변환
    static EventType ParseTrigger(string trigger)
    {
        Debug.Log($"[RuleFactory] ParseTrigger {trigger}");
        return Enum.Parse<EventType>(trigger);
    }

    // Condition 생성
    static ICondition CreateCondition(EventDTO dto)
    {
        if (dto.conditions == null || dto.conditions.Count == 0)
            return null;

        var c = dto.conditions[0]; // 지금은 1개만

        switch (c.type)
        {
            case "VarEquals":
                return new VarEqualsCondition(c.name, c.value);
        }

        Debug.LogWarning($"Unknown condition: {c.type}");
        return null;
    }

    // Action 생성
    static IAction CreateAction(EventDTO dto)
    {
        Debug.Log($"[RuleFactory] CreateAction {dto.action}");
        switch (dto.action)
        {
            case "SetVar":
                return new SetVariableAction(
                    dto.@params["name"]?.ToString(),
                    dto.@params["value"]?.ToObject<object>()
                );

            case "Move":
                return new MoveAction(
                    dto.@params["x"]?.ToObject<float>() ?? 0f,
                    dto.@params["y"]?.ToObject<float>() ?? 0f,
                    dto.@params["speed"]?.ToObject<float>() ?? 1f
                );

            case "Rotate":
                return new RotateAction(
                    dto.@params["speed"]?.ToObject<float>() ?? 90f
                );

            case "EmitEventSignal":
                return new EmitSignalAction(
                    dto.@params["signalKey"]?.ToString()
                );
        }

        Debug.LogWarning($"Unknown action: {dto.action}");
        return null;
    }
}
