using UnityEngine;
using System.Collections.Generic;
using Metamong.Runtime.Actions;

public class RuntimeEvents : MonoBehaviour
{
    private List<RuntimeRule> rules;
    private GameObject owner;

    // 런타임 시스템 참조
    private IWorld world;
    private ISignalBus signals;

    public void Initialize(
        List<RuntimeRule> rules,
        GameObject owner,
        IWorld world,
        ISignalBus signals
    )
    {
        this.rules = rules;
        this.owner = owner;
        this.world = world;
        this.signals = signals;

        Debug.Log($"[RuntimeEvents] Initialized rules={rules?.Count}");
    }

    void Start()
    {
        Debug.Log("[RuntimeEvents] Start()");
        Execute(EventType.OnStart);
    }

    void Update()
    {
        Execute(EventType.OnUpdate);
    }

    void OnDestroy()
    {
        Execute(EventType.OnDestroy);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Execute(EventType.OnCollision);
    }

    void Execute(EventType trigger)
    {
        if (rules == null) return;

        var ctx = new ActionContext
        {
            Owner = owner,
            DeltaTime = Time.deltaTime,
            World = world,
            Signals = signals
        };

        foreach (var rule in rules)
        {
            Debug.Log($"[RuntimeEvents] Check rule trigger={rule.trigger}");
            if (rule.trigger != trigger)
                continue;

            if (rule.condition == null || rule.condition.Evaluate(owner))
            {
                rule.action?.Execute(ctx);
            }
        }
    }
}
