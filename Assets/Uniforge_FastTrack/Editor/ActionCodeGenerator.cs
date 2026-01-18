using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Generates C# action code from UniForge action definitions.
    /// Handles Movement, Combat, Variables, Visual effects, etc.
    /// Updated to match UniForgeFrontend DefaultActions.ts
    /// </summary>
    public static class ActionCodeGenerator
    {
        /// <summary>
        /// Generates code for a single action.
        /// </summary>
        /// <param name="sb">StringBuilder to append code to</param>
        /// <param name="action">Action type name</param>
        /// <param name="p">Action parameters</param>
        /// <param name="indent">Current indentation</param>
        /// <param name="entity">Parent entity (for module references)</param>
        /// <param name="traverseGraphCallback">Callback for RunModule action to traverse graphs</param>
        public static void GenerateActionCode(
            StringBuilder sb,
            string action,
            Dictionary<string, object> p,
            string indent,
            EntityJSON entity,
            System.Action<StringBuilder, NodeJSON, ModuleJSON, string, EntityJSON> traverseGraphCallback = null)
        {
            if (string.IsNullOrEmpty(action)) return;
            p = p ?? new Dictionary<string, object>();

            switch (action)
            {
                // === Basic ===
                case "Log":
                    {
                        string msg = ParameterHelper.GetParamString(p, "message");
                        sb.AppendLine($"{indent}Debug.Log(\"{msg}\");");
                        break;
                    }

                // === Movement ===
                case "Move":
                    {
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 200f);
                        // Web speed is in pixels/sec, Unity uses units/sec. Divide by PPU (100)
                        float unitySpeed = speed / 100f;

                        // Check if direction is a dynamic ValueSource (variable, mouse, etc.)
                        if (ParameterHelper.IsDynamicValueSource(p, "direction"))
                        {
                            string dirCode = ParameterHelper.GetOperandCode(p, "direction");
                            sb.AppendLine($"{indent}{{");
                            sb.AppendLine($"{indent}    Vector2 moveDir = {dirCode};");
                            sb.AppendLine($"{indent}    // Direction is already in Unity coordinates");
                            sb.AppendLine($"{indent}    Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0).normalized;");
                            sb.AppendLine($"{indent}    _transform.Translate(dir3 * {unitySpeed}f * Time.deltaTime);");
                            sb.AppendLine($"{indent}}}");
                        }
                        else
                        {
                            var dir = ParameterHelper.GetParamVector2(p, "direction");
                            // Direction is already in Unity coordinates
                            sb.AppendLine($"{indent}_transform.Translate(new Vector3({dir.x}f, {dir.y}f, 0).normalized * {unitySpeed}f * Time.deltaTime);");
                        }
                        break;
                    }
                case "Rotate":
                    {
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 90f);
                        sb.AppendLine($"{indent}_transform.Rotate(0, 0, {speed}f * Time.deltaTime);");
                        break;
                    }
                case "ChaseTarget":
                    {
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        string targetRole = ParameterHelper.GetParamString(p, "targetRole");
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 80f) / 100f;

                        sb.AppendLine($"{indent}// ChaseTarget: targetId={targetId}, targetRole={targetRole}");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    Transform chaseTarget = null;");

                        if (!string.IsNullOrEmpty(targetId))
                        {
                            sb.AppendLine($"{indent}    var targetGo = GameObject.Find(\"{targetId}\");");
                            sb.AppendLine($"{indent}    if (targetGo != null) chaseTarget = targetGo.transform;");
                        }

                        if (!string.IsNullOrEmpty(targetRole))
                        {
                            sb.AppendLine($"{indent}    if (chaseTarget == null)");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        chaseTarget = UniforgeEntity.FindNearestWithRole(\"{targetRole}\", _transform.position)?.transform;");
                            sb.AppendLine($"{indent}    }}");
                        }

                        sb.AppendLine($"{indent}    if (chaseTarget != null)");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        Vector3 dir = (chaseTarget.position - _transform.position).normalized;");
                        sb.AppendLine($"{indent}        _transform.Translate(dir * {speed}f * Time.deltaTime);");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }
                case "MoveToward":
                    {
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 100f) / 100f;
                        float targetX = ParameterHelper.GetParamFloat(p, "x", 0) / 100f;
                        float targetY = ParameterHelper.GetParamFloat(p, "y", 0) / 100f;
                        sb.AppendLine($"{indent}// MoveToward: ({targetX * 100}, {targetY * 100}) -> Unity ({targetX}, {-targetY})");
                        sb.AppendLine($"{indent}Vector3 targetPos = new Vector3({targetX}f, {-targetY}f, 0);");
                        sb.AppendLine($"{indent}Vector3 direction = (targetPos - _transform.position).normalized;");
                        sb.AppendLine($"{indent}if (Vector3.Distance(_transform.position, targetPos) > 0.05f)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    _transform.Translate(direction * {speed}f * Time.deltaTime);");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                // === Jump (NEW - Physics based) ===
                case "Jump":
                    {
                        float jumpForce = ParameterHelper.GetParamFloat(p, "force", 500f) / 100f;
                        sb.AppendLine($"{indent}// Jump with physics");
                        sb.AppendLine($"{indent}if (_isGrounded)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    var rb = GetComponent<Rigidbody2D>();");
                        sb.AppendLine($"{indent}    if (rb != null)");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        rb.velocity = new Vector2(rb.velocity.x, 0);");
                        sb.AppendLine($"{indent}        rb.AddForce(Vector2.up * {jumpForce}f, ForceMode2D.Impulse);");
                        sb.AppendLine($"{indent}        _isGrounded = false;");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                // === Variables ===
                case "SetVar":
                    {
                        string varName = ParameterHelper.SanitizeName(ParameterHelper.GetParamString(p, "name"));
                        string operation = ParameterHelper.GetParamString(p, "operation", "Set");
                        var op1 = ParameterHelper.GetOperandCode(p, "operand1");
                        var op2 = ParameterHelper.GetOperandCode(p, "operand2");

                        switch (operation)
                        {
                            case "Set":
                                sb.AppendLine($"{indent}{varName} = {op1};");
                                break;
                            case "Add":
                                sb.AppendLine($"{indent}{varName} = {op1} + {op2};");
                                break;
                            case "Sub":
                                sb.AppendLine($"{indent}{varName} = {op1} - {op2};");
                                break;
                            case "Multiply":
                                sb.AppendLine($"{indent}{varName} = {op1} * {op2};");
                                break;
                            case "Divide":
                                sb.AppendLine($"{indent}{varName} = {op2} != 0 ? {op1} / {op2} : 0;");
                                break;
                            default:
                                sb.AppendLine($"{indent}{varName} = {op1};");
                                break;
                        }
                        break;
                    }
                case "IncrementVar":
                    {
                        string varName = ParameterHelper.SanitizeName(ParameterHelper.GetParamString(p, "name"));
                        float amount = ParameterHelper.GetParamFloat(p, "amount", 0);
                        if (amount == 0)
                            sb.AppendLine($"{indent}{varName} += Time.deltaTime;");
                        else
                            sb.AppendLine($"{indent}{varName} += {amount}f;");
                        break;
                    }

                // === Flow Control ===
                case "Wait":
                    {
                        float seconds = ParameterHelper.GetParamFloat(p, "seconds", 1f);
                        sb.AppendLine($"{indent}yield return new WaitForSeconds({seconds}f);");
                        break;
                    }
                case "Enable":
                    {
                        bool enabled = ParameterHelper.GetParamBool(p, "enabled", true);
                        sb.AppendLine($"{indent}gameObject.SetActive({enabled.ToString().ToLower()});");
                        break;
                    }
                case "ChangeScene":
                    {
                        string sceneName = ParameterHelper.GetParamString(p, "sceneName", "sceneId");
                        sb.AppendLine($"{indent}SceneManager.LoadScene(\"{sceneName}\");");
                        break;
                    }
                case "Destroy":
                    {
                        sb.AppendLine($"{indent}Destroy(gameObject);");
                        break;
                    }

                // === Combat ===
                case "TakeDamage":
                case "Heal":
                    {
                        float amount = ParameterHelper.GetParamFloat(p, "amount", 10f);
                        string sign = action == "Heal" ? "+" : "-";
                        sb.AppendLine($"{indent}// {action}: {amount}");
                        sb.AppendLine($"{indent}hp {sign}= {amount}f;");
                        if (action == "Heal")
                        {
                            sb.AppendLine($"{indent}if (hp > maxHp) hp = maxHp;");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}if (hp <= 0) {{ hp = 0; OnDeath(); }}");
                        }
                        break;
                    }
                case "Attack":
                    {
                        float range = ParameterHelper.GetParamFloat(p, "range", 100f) / 100f;
                        float damage = ParameterHelper.GetParamFloat(p, "damage", 10f);
                        float cooldown = ParameterHelper.GetParamFloat(p, "cooldown", 500f) / 1000f;
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        string targetRole = ParameterHelper.GetParamString(p, "targetRole");
                        string hitEffect = ParameterHelper.GetParamString(p, "hitEffect", "hit_spark");

                        sb.AppendLine($"{indent}// Attack: range={range * 100}, damage={damage}, cooldown={cooldown}s");
                        sb.AppendLine($"{indent}if (Time.time >= _lastAttackTime + {cooldown}f)");
                        sb.AppendLine($"{indent}{{");

                        if (!string.IsNullOrEmpty(targetId))
                        {
                            // Attack specific target
                            sb.AppendLine($"{indent}    var attackTarget = GameObject.Find(\"{targetId}\");");
                            sb.AppendLine($"{indent}    if (attackTarget != null && Vector3.Distance(_transform.position, attackTarget.transform.position) <= {range}f)");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        attackTarget.SendMessage(\"OnTakeDamage\", {damage}f, SendMessageOptions.DontRequireReceiver);");
                            if (!string.IsNullOrEmpty(hitEffect) && hitEffect != "none")
                            {
                                sb.AppendLine($"{indent}        ParticleManager.PlayStatic(\"{hitEffect}\", attackTarget.transform.position, 1f);");
                            }
                            sb.AppendLine($"{indent}        _lastAttackTime = Time.time;");
                            sb.AppendLine($"{indent}    }}");
                        }
                        else if (!string.IsNullOrEmpty(targetRole))
                        {
                            // Attack by role
                            sb.AppendLine($"{indent}    foreach (var entity in UniforgeEntity.RegisteredEntities)");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        if (entity.Role == \"{targetRole}\" && entity.gameObject != gameObject)");
                            sb.AppendLine($"{indent}        {{");
                            sb.AppendLine($"{indent}            float dist = Vector3.Distance(_transform.position, entity.transform.position);");
                            sb.AppendLine($"{indent}            if (dist <= {range}f)");
                            sb.AppendLine($"{indent}            {{");
                            sb.AppendLine($"{indent}                entity.SendMessage(\"OnTakeDamage\", {damage}f, SendMessageOptions.DontRequireReceiver);");
                            if (!string.IsNullOrEmpty(hitEffect) && hitEffect != "none")
                            {
                                sb.AppendLine($"{indent}                ParticleManager.PlayStatic(\"{hitEffect}\", entity.transform.position, 1f);");
                            }
                            sb.AppendLine($"{indent}                _lastAttackTime = Time.time;");
                            sb.AppendLine($"{indent}            }}");
                            sb.AppendLine($"{indent}        }}");
                            sb.AppendLine($"{indent}    }}");
                        }
                        else
                        {
                            // Attack all in range (original behavior)
                            sb.AppendLine($"{indent}    Debug.Log(\"[Attack] Executing attack with range={range}f\");");
                            sb.AppendLine($"{indent}    // Always show attack swing effect at player position");
                            sb.AppendLine($"{indent}    ParticleManager.PlayStatic(\"hit_spark\", _transform.position, 0.5f);");
                            sb.AppendLine($"{indent}    var hits = Physics2D.OverlapCircleAll(_transform.position, {range}f);");
                            sb.AppendLine($"{indent}    Debug.Log($\"[Attack] Found {{hits.Length}} targets in range\");");
                            sb.AppendLine($"{indent}    foreach (var hit in hits)");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        if (hit.gameObject != gameObject)");
                            sb.AppendLine($"{indent}        {{");
                            sb.AppendLine($"{indent}            Debug.Log($\"[Attack] Hit: {{hit.gameObject.name}}\");");
                            sb.AppendLine($"{indent}            hit.SendMessage(\"OnTakeDamage\", {damage}f, SendMessageOptions.DontRequireReceiver);");
                            if (!string.IsNullOrEmpty(hitEffect) && hitEffect != "none")
                            {
                                sb.AppendLine($"{indent}            ParticleManager.PlayStatic(\"{hitEffect}\", hit.transform.position, 1f);");
                            }
                            sb.AppendLine($"{indent}        }}");
                            sb.AppendLine($"{indent}    }}");
                            sb.AppendLine($"{indent}    _lastAttackTime = Time.time;");
                        }

                        sb.AppendLine($"{indent}}}");
                        break;
                    }
                case "FireProjectile":
                    {
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 500f) / 100f;
                        float damage = ParameterHelper.GetParamFloat(p, "damage", 10f);
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        string targetRole = ParameterHelper.GetParamString(p, "targetRole", "enemy");
                        float targetX = ParameterHelper.GetParamFloat(p, "targetX", float.NaN);
                        float targetY = ParameterHelper.GetParamFloat(p, "targetY", float.NaN);

                        sb.AppendLine($"{indent}// FireProjectile: speed={speed * 100}, damage={damage}");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    Vector3 fireDirection = Vector3.right;");

                        if (!float.IsNaN(targetX) && !float.IsNaN(targetY))
                        {
                            // Fire toward specific coordinates
                            sb.AppendLine($"{indent}    Vector3 targetPos = new Vector3({targetX / 100f}f, {-targetY / 100f}f, 0);");
                            sb.AppendLine($"{indent}    fireDirection = (targetPos - _transform.position).normalized;");
                        }
                        else if (!string.IsNullOrEmpty(targetId))
                        {
                            sb.AppendLine($"{indent}    var fireTarget = GameObject.Find(\"{targetId}\");");
                            sb.AppendLine($"{indent}    if (fireTarget != null) fireDirection = (fireTarget.transform.position - _transform.position).normalized;");
                        }
                        else if (!string.IsNullOrEmpty(targetRole))
                        {
                            sb.AppendLine($"{indent}    var nearestTarget = UniforgeEntity.FindNearestWithRole(\"{targetRole}\", _transform.position);");
                            sb.AppendLine($"{indent}    if (nearestTarget != null) fireDirection = (nearestTarget.transform.position - _transform.position).normalized;");
                        }

                        sb.AppendLine($"{indent}    ProjectileManager.FireDirectionStatic(_transform.position, fireDirection, {speed}f, {damage}f);");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                case "SpawnEntity":
                    {
                        // Support multiple parameter names from Frontend
                        // Frontend uses: prefabId, sourceAssetId, templateId
                        string templateId = ParameterHelper.GetParamString(p, "templateId");
                        if (string.IsNullOrEmpty(templateId))
                            templateId = ParameterHelper.GetParamString(p, "prefabId");
                        if (string.IsNullOrEmpty(templateId))
                            templateId = ParameterHelper.GetParamString(p, "sourceAssetId");

                        string sourceType = ParameterHelper.GetParamString(p, "sourceType", "texture");
                        string posMode = ParameterHelper.GetParamString(p, "positionMode", "relative");
                        float offsetX = ParameterHelper.GetParamFloat(p, "offsetX", 0) / 100f;
                        float offsetY = ParameterHelper.GetParamFloat(p, "offsetY", 0) / 100f;
                        float absX = ParameterHelper.GetParamFloat(p, "x", 0) / 100f;
                        float absY = ParameterHelper.GetParamFloat(p, "y", 0) / 100f;
                        string role = ParameterHelper.GetParamString(p, "role", "neutral");

                        // Handle initialVariables
                        var initialVars = ExtractInitialVariables(p);

                        sb.AppendLine($"{indent}// SpawnEntity: {templateId} (sourceType={sourceType})");
                        sb.AppendLine($"{indent}{{");

                        if (posMode == "absolute")
                        {
                            sb.AppendLine($"{indent}    var spawnedObj = PrefabRegistry.SpawnStatic(\"{templateId}\", new Vector3({absX}f, {-absY}f, 0));");
                        }
                        else if (templateId == "__self__")
                        {
                            sb.AppendLine($"{indent}    var spawnedObj = PrefabRegistry.SpawnSelfStatic(gameObject, _transform.position + new Vector3({offsetX}f, {-offsetY}f, 0));");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    var spawnedObj = PrefabRegistry.SpawnStatic(\"{templateId}\", _transform.position + new Vector3({offsetX}f, {-offsetY}f, 0));");
                        }

                        // Set role
                        if (!string.IsNullOrEmpty(role))
                        {
                            sb.AppendLine($"{indent}    if (spawnedObj != null)");
                            sb.AppendLine($"{indent}    {{");
                            sb.AppendLine($"{indent}        var entity = spawnedObj.GetComponent<UniforgeEntity>();");
                            sb.AppendLine($"{indent}        if (entity != null) entity.Role = \"{role}\";");

                            // Apply initial variables
                            if (!string.IsNullOrEmpty(initialVars))
                            {
                                sb.AppendLine($"{indent}        // Apply initial variables");
                                sb.AppendLine($"{indent}        {initialVars}");
                            }
                            sb.AppendLine($"{indent}    }}");
                        }
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                // === Disable Action (NEW) ===
                case "Disable":
                    {
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        if (!string.IsNullOrEmpty(targetId))
                        {
                            sb.AppendLine($"{indent}Debug.Log(\"[Action] Disable Target: {targetId}\");");
                            sb.AppendLine($"{indent}var disableTarget = GameObject.Find(\"{targetId}\");");
                            sb.AppendLine($"{indent}if (disableTarget != null) disableTarget.SetActive(false);");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}Debug.Log(\"[Action] Disable Self\");");
                            sb.AppendLine($"{indent}gameObject.SetActive(false);");
                        }
                        break;
                    }

                // === Visual ===
                case "PlayAnimation":
                    {
                        // Try multiple parameter name variations
                        string animName = ParameterHelper.GetParamString(p, "animationName");
                        if (string.IsNullOrEmpty(animName))
                            animName = ParameterHelper.GetParamString(p, "animation");
                        if (string.IsNullOrEmpty(animName))
                            animName = ParameterHelper.GetParamString(p, "name");
                        if (string.IsNullOrEmpty(animName))
                            animName = ParameterHelper.GetParamString(p, "anim");
                        if (string.IsNullOrEmpty(animName))
                            animName = ParameterHelper.GetParamString(p, "state");

                        if (string.IsNullOrEmpty(animName))
                        {
                            sb.AppendLine($"{indent}Debug.LogWarning(\"[Action] PlayAnimation: No animation name specified\");");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}if (_animator != null) {{ Debug.Log(\"[Action] PlayAnimation: {animName}\"); _animator.Play(\"{animName}\"); }}");
                            sb.AppendLine($"{indent}else {{ Debug.LogWarning(\"[Action] PlayAnimation Failed: Animator is null for {animName}\"); }}");
                        }
                        break;
                    }
                case "Pulse":
                    {
                        float speed = ParameterHelper.GetParamFloat(p, "speed", 2f);
                        float minScale = ParameterHelper.GetParamFloat(p, "minScale", 0.9f);
                        float maxScale = ParameterHelper.GetParamFloat(p, "maxScale", 1.1f);
                        sb.AppendLine($"{indent}float pulse = Mathf.Lerp({minScale}f, {maxScale}f, (Mathf.Sin(Time.time * {speed}f) + 1f) / 2f);");
                        sb.AppendLine($"{indent}_transform.localScale = new Vector3(pulse, pulse, 1f);");
                        break;
                    }
                case "PlayParticle":
                    {
                        string preset = ParameterHelper.GetParamString(p, "preset", "hit_spark");
                        float scale = ParameterHelper.GetParamFloat(p, "scale", 1f);
                        sb.AppendLine($"{indent}Debug.Log(\"[Action] PlayParticle: {preset}\");");
                        sb.AppendLine($"{indent}ParticleManager.PlayStatic(\"{preset}\", _transform.position, {scale}f);");
                        break;
                    }
                case "StartParticleEmitter":
                    {
                        string emitterId = ParameterHelper.GetParamString(p, "emitterId");
                        string particleSystemId = ParameterHelper.GetParamString(p, "particleSystemId", "preset");
                        float offsetX = ParameterHelper.GetParamFloat(p, "offsetX", 0) / 100f;
                        float offsetY = ParameterHelper.GetParamFloat(p, "offsetY", 0) / 100f;
                        bool attachToEntity = ParameterHelper.GetParamBool(p, "attachToEntity", true);

                        sb.AppendLine($"{indent}// StartParticleEmitter: {emitterId} (system: {particleSystemId})");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    var emitter = ParticleEmitterManager.StartEmitter(");
                        sb.AppendLine($"{indent}        \"{emitterId}\",");
                        sb.AppendLine($"{indent}        \"{particleSystemId}\",");
                        sb.AppendLine($"{indent}        {(attachToEntity ? "_transform" : "null")},");
                        sb.AppendLine($"{indent}        new Vector3({offsetX}f, {-offsetY}f, 0)");
                        sb.AppendLine($"{indent}    );");
                        sb.AppendLine($"{indent}    if (emitter != null) _activeEmitters[\"{emitterId}\"] = emitter;");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }
                case "StopParticleEmitter":
                    {
                        string emitterId = ParameterHelper.GetParamString(p, "emitterId");
                        bool destroy = ParameterHelper.GetParamBool(p, "destroy", false);

                        sb.AppendLine($"{indent}// StopParticleEmitter: {emitterId}");
                        sb.AppendLine($"{indent}if (_activeEmitters.TryGetValue(\"{emitterId}\", out var emitterToStop))");
                        sb.AppendLine($"{indent}{{");
                        if (destroy)
                        {
                            sb.AppendLine($"{indent}    Destroy(emitterToStop.gameObject);");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    var psToStop = emitterToStop.GetComponent<ParticleSystem>();");
                            sb.AppendLine($"{indent}    if (psToStop != null) psToStop.Stop();");
                        }
                        sb.AppendLine($"{indent}    _activeEmitters.Remove(\"{emitterId}\");");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                case "PlaySound":
                    {
                        string soundId = ParameterHelper.GetParamString(p, "soundId");
                        sb.AppendLine($"{indent}AudioManager.PlayStatic(\"{soundId}\");");
                        break;
                    }

                // === Events ===
                case "EmitEventSignal":
                    {
                        string signalKey = ParameterHelper.GetParamString(p, "signalKey");
                        sb.AppendLine($"{indent}EventBus.Emit(\"{signalKey}\");");
                        break;
                    }
                case "ClearSignal":
                    {
                        string key = ParameterHelper.GetParamString(p, "key");
                        sb.AppendLine($"{indent}EventBus.Clear(\"{key}\");");
                        break;
                    }
                case "ShowDialogue":
                    {
                        string text = ParameterHelper.GetParamString(p, "text");
                        sb.AppendLine($"{indent}DialogueManager.Show(\"{text}\");");
                        break;
                    }

                // === Modules ===
                case "RunModule":
                    {
                        string moduleId = ParameterHelper.GetParamString(p, "moduleId");
                        sb.AppendLine($"{indent}// RunModule: {moduleId}");

                        if (traverseGraphCallback != null && entity?.modules != null)
                        {
                            var targetModule = entity.modules.FirstOrDefault(m => m.id == moduleId);
                            if (targetModule != null)
                            {
                                var entry = targetModule.nodes?.FirstOrDefault(n => n.kind == "Entry");
                                if (entry != null)
                                {
                                    traverseGraphCallback(sb, entry, targetModule, indent, entity);
                                }
                            }
                            else
                            {
                                sb.AppendLine($"{indent}// Warning: Module {moduleId} not found");
                            }
                        }
                        break;
                    }

                // === Flow Control: If (NEW) ===
                case "If":
                    {
                        sb.AppendLine($"{indent}// Inline If Action");
                        var conditionObj = p.ContainsKey("condition") ? p["condition"] : null;
                        var thenActions = p.ContainsKey("then") ? p["then"] : null;
                        var elseActions = p.ContainsKey("else") ? p["else"] : null;

                        string conditionCode = GenerateInlineCondition(conditionObj);
                        sb.AppendLine($"{indent}if ({conditionCode})");
                        sb.AppendLine($"{indent}{{");

                        // Generate then actions
                        if (thenActions is JArray thenArr)
                        {
                            foreach (var actionObj in thenArr)
                            {
                                if (actionObj is JObject jo)
                                {
                                    string actionType = jo["type"]?.ToString() ?? "";
                                    var actionParams = jo.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                                    actionParams.Remove("type");
                                    GenerateActionCode(sb, actionType, actionParams, indent + "    ", entity, traverseGraphCallback);
                                }
                            }
                        }

                        sb.AppendLine($"{indent}}}");

                        // Generate else actions
                        if (elseActions is JArray elseArr && elseArr.Count > 0)
                        {
                            sb.AppendLine($"{indent}else");
                            sb.AppendLine($"{indent}{{");
                            foreach (var actionObj in elseArr)
                            {
                                if (actionObj is JObject jo)
                                {
                                    string actionType = jo["type"]?.ToString() ?? "";
                                    var actionParams = jo.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                                    actionParams.Remove("type");
                                    GenerateActionCode(sb, actionType, actionParams, indent + "    ", entity, traverseGraphCallback);
                                }
                            }
                            sb.AppendLine($"{indent}}}");
                        }
                        break;
                    }

                // === Signal Actions ===
                case "SetSignal":
                    {
                        string key = ParameterHelper.GetParamString(p, "key");
                        bool value = ParameterHelper.GetParamBool(p, "value", true);
                        sb.AppendLine($"{indent}_signalFlags[\"{key}\"] = {value.ToString().ToLower()};");
                        break;
                    }

                default:
                    sb.AppendLine($"{indent}// [WARNING] Unknown action: {action}");
                    sb.AppendLine($"{indent}Debug.LogWarning(\"[Uniforge] Unknown action: {action}\");");
                    break;
            }
        }

        /// <summary>
        /// Generates inline condition code for If action.
        /// </summary>
        private static string GenerateInlineCondition(object conditionObj)
        {
            if (conditionObj == null) return "true";

            if (conditionObj is JObject jo)
            {
                string condType = jo["type"]?.ToString() ?? "";
                var condParams = jo.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();

                // Create a ConditionJSON-like structure
                var cond = new ConditionJSON
                {
                    type = condType,
                    key = jo["key"]?.ToString(),
                    @params = condParams
                };

                return ConditionCodeGenerator.GenerateSingleCondition(cond);
            }

            return "true";
        }

        /// <summary>
        /// Extracts and generates code for initial variables in SpawnEntity.
        /// </summary>
        private static string ExtractInitialVariables(Dictionary<string, object> p)
        {
            if (!p.ContainsKey("initialVariables") || p["initialVariables"] == null)
                return "";

            var sb = new StringBuilder();
            var vars = p["initialVariables"];

            if (vars is JObject jo)
            {
                foreach (var prop in jo.Properties())
                {
                    string varName = prop.Name;
                    string varValue = ParameterHelper.FormatValue(prop.Value?.ToString());
                    sb.AppendLine($"// Set {varName} = {varValue}");
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Check if a list of actions contains any Wait action (requires Coroutine).
        /// </summary>
        public static bool HasWaitAction(List<ActionJSON> actions)
        {
            if (actions == null) return false;
            foreach (var action in actions)
            {
                if (action.type == "Wait") return true;

                // Check nested actions in If blocks
                var allParams = action.GetAllParams();
                if (action.type == "If")
                {
                    if (allParams.TryGetValue("thenActions", out var thenObj) && thenObj is JArray thenArr)
                    {
                        var thenActions = thenArr.ToObject<List<ActionJSON>>();
                        if (HasWaitAction(thenActions)) return true;
                    }
                    if (allParams.TryGetValue("elseActions", out var elseObj) && elseObj is JArray elseArr)
                    {
                        var elseActions = elseArr.ToObject<List<ActionJSON>>();
                        if (HasWaitAction(elseActions)) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a component contains Wait action.
        /// </summary>
        public static bool ComponentHasWait(ComponentJSON comp)
        {
            if (comp?.actions == null) return false;
            return HasWaitAction(comp.actions);
        }
    }
}
