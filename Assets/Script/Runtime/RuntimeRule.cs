using UnityEngine;

public class RuntimeRule
{
    public EventType trigger;
    public ICondition condition;
    public IAction action;
}
public enum EventType
{
    OnStart,
    OnUpdate,
    OnDestroy,
    OnSignalReceive,
    OnCollision
}
