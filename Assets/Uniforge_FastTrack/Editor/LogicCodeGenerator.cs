using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// New unified logic code generator.
    /// Directly processes Frontend LogicComponent format without intermediate conversion.
    /// </summary>
    public static class LogicCodeGenerator
    {
        /// <summary>
        /// Generate all logic code for an entity from its components array.
        /// This is the main entry point for the new system.
        /// </summary>
        public static void GenerateEntityLogic(StringBuilder sb, EntityJSON entity)
        {
            // Collect all Logic components
            var logicComponents = new List<ComponentJSON>();

            // Priority 1: Use components array directly (high fidelity)
            if (entity.components != null && entity.components.Count > 0)
            {
                logicComponents = entity.components.Where(c => c.type == "Logic").ToList();
                Debug.Log($"<color=cyan>[LogicCodeGenerator]</color> Using components array: {logicComponents.Count} logic components");
            }
            // Priority 2: Fallback to events array (legacy)
            else if (entity.events != null && entity.events.Count > 0)
            {
                logicComponents = ConvertEventsToComponents(entity.events);
                Debug.Log($"<color=yellow>[LogicCodeGenerator]</color> Fallback to events array: {logicComponents.Count} converted components");
            }

            if (logicComponents.Count == 0)
            {
                Debug.Log($"<color=gray>[LogicCodeGenerator]</color> No logic components for entity '{entity.name}'");
                return;
            }

            // Group by trigger type
            var triggerGroups = logicComponents.GroupBy(c => c.@event ?? "OnUpdate").ToList();

            // Track which Unity methods we've generated
            var generatedMethods = new HashSet<string>();

            foreach (var group in triggerGroups)
            {
                string trigger = group.Key;
                var components = group.ToList();

                Debug.Log($"<color=green>[LogicCodeGenerator]</color> Processing trigger '{trigger}' with {components.Count} components");

                switch (trigger)
                {
                    case "OnStart":
                        GenerateStartMethod(sb, components, entity);
                        generatedMethods.Add("Start");
                        break;

                    case "OnUpdate":
                        GenerateUpdateMethod(sb, components, entity, generatedMethods.Contains("Update"));
                        generatedMethods.Add("Update");
                        break;

                    case "OnKeyDown":
                    case "OnKeyUp":
                    case "OnKeyPress":
                        // Key events are handled in Update
                        if (!generatedMethods.Contains("Update"))
                        {
                            sb.AppendLine("    void Update()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        CheckGrounded();");
                            generatedMethods.Add("Update");
                        }
                        GenerateKeyEventCode(sb, trigger, components, entity);
                        break;

                    case "OnCollisionEnter":
                    case "OnCollision":
                        GenerateCollisionMethod(sb, "OnCollisionEnter2D", components, entity);
                        break;

                    case "OnTriggerEnter":
                        GenerateTriggerMethod(sb, "OnTriggerEnter2D", components, entity);
                        break;

                    case "OnDestroy":
                        GenerateDestroyMethod(sb, components, entity);
                        break;

                    case "OnTimer":
                        // Timer events are handled in Update
                        // Will be processed separately
                        break;

                    default:
                        sb.AppendLine($"    // TODO: Unhandled trigger type: {trigger}");
                        break;
                }
            }

            // Close Update method if we started it for key events
            if (generatedMethods.Contains("Update") && !generatedMethods.Contains("UpdateClosed"))
            {
                // Check if Update was opened but not closed
                // This is handled in GenerateUpdateMethod
            }
        }

        /// <summary>
        /// Convert legacy events array to components format
        /// </summary>
        private static List<ComponentJSON> ConvertEventsToComponents(List<EventJSON> events)
        {
            var components = new List<ComponentJSON>();

            foreach (var evt in events)
            {
                var comp = new ComponentJSON
                {
                    id = evt.id,
                    type = "Logic",
                    @event = evt.trigger,
                    eventParams = evt.triggerParams,
                    conditions = evt.conditions,
                    conditionLogic = evt.conditionLogic ?? "AND"
                };

                // Convert actions
                if (evt.actions != null && evt.actions.Count > 0)
                {
                    comp.actions = evt.actions;
                }
                else if (!string.IsNullOrEmpty(evt.action))
                {
                    // Single action format
                    comp.actions = new List<ActionJSON>
                    {
                        new ActionJSON
                        {
                            type = evt.action,
                            AdditionalData = evt.@params != null
                                ? new Dictionary<string, object>(evt.@params)
                                : new Dictionary<string, object>()
                        }
                    };
                }

                comp.elseActions = evt.elseActions;
                components.Add(comp);
            }

            return components;
        }

        private static void GenerateStartMethod(StringBuilder sb, List<ComponentJSON> components, EntityJSON entity)
        {
            sb.AppendLine("    void Start()");
            sb.AppendLine("    {");

            foreach (var comp in components)
            {
                GenerateComponentActions(sb, comp, "        ", entity);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateUpdateMethod(StringBuilder sb, List<ComponentJSON> components, EntityJSON entity, bool alreadyStarted)
        {
            if (!alreadyStarted)
            {
                sb.AppendLine("    void Update()");
                sb.AppendLine("    {");
                sb.AppendLine("        CheckGrounded();");
                sb.AppendLine();
            }

            foreach (var comp in components)
            {
                GenerateComponentActions(sb, comp, "        ", entity);
            }

            if (!alreadyStarted)
            {
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        private static void GenerateKeyEventCode(StringBuilder sb, string trigger, List<ComponentJSON> components, EntityJSON entity)
        {
            foreach (var comp in components)
            {
                string key = GetEventParamString(comp.eventParams, "key");
                string unityKey = ConvertWebKeyToUnity(key);

                string inputMethod = trigger switch
                {
                    "OnKeyDown" => "GetKeyDown",
                    "OnKeyUp" => "GetKeyUp",
                    "OnKeyPress" => "GetKey",
                    _ => "GetKeyDown"
                };

                sb.AppendLine($"        if (Input.{inputMethod}(KeyCode.{unityKey}))");
                sb.AppendLine($"        {{");
                GenerateComponentActions(sb, comp, "            ", entity);
                sb.AppendLine($"        }}");
            }
        }

        private static void GenerateCollisionMethod(StringBuilder sb, string methodName, List<ComponentJSON> components, EntityJSON entity)
        {
            sb.AppendLine($"    void {methodName}(Collision2D collision)");
            sb.AppendLine("    {");
            sb.AppendLine("        _isGrounded = true;");

            foreach (var comp in components)
            {
                string withTag = GetEventParamString(comp.eventParams, "withTag", "withRole");
                if (!string.IsNullOrEmpty(withTag))
                {
                    sb.AppendLine($"        var otherEntity = collision.gameObject.GetComponent<UniforgeEntity>();");
                    sb.AppendLine($"        if (otherEntity != null && otherEntity.Role == \"{withTag}\")");
                    sb.AppendLine($"        {{");
                    GenerateComponentActions(sb, comp, "            ", entity);
                    sb.AppendLine($"        }}");
                }
                else
                {
                    GenerateComponentActions(sb, comp, "        ", entity);
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateTriggerMethod(StringBuilder sb, string methodName, List<ComponentJSON> components, EntityJSON entity)
        {
            sb.AppendLine($"    void {methodName}(Collider2D other)");
            sb.AppendLine("    {");

            foreach (var comp in components)
            {
                string withTag = GetEventParamString(comp.eventParams, "withTag", "withRole");
                if (!string.IsNullOrEmpty(withTag))
                {
                    sb.AppendLine($"        var otherEntity = other.GetComponent<UniforgeEntity>();");
                    sb.AppendLine($"        if (otherEntity != null && otherEntity.Role == \"{withTag}\")");
                    sb.AppendLine($"        {{");
                    GenerateComponentActions(sb, comp, "            ", entity);
                    sb.AppendLine($"        }}");
                }
                else
                {
                    GenerateComponentActions(sb, comp, "        ", entity);
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateDestroyMethod(StringBuilder sb, List<ComponentJSON> components, EntityJSON entity)
        {
            sb.AppendLine("    void OnDestroy()");
            sb.AppendLine("    {");

            foreach (var comp in components)
            {
                GenerateComponentActions(sb, comp, "        ", entity);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generate code for a single component's conditions and actions
        /// </summary>
        private static void GenerateComponentActions(StringBuilder sb, ComponentJSON comp, string indent, EntityJSON entity)
        {
            bool hasConditions = comp.conditions != null && comp.conditions.Count > 0;
            string innerIndent = indent;

            // Generate conditions
            if (hasConditions)
            {
                string conditionCode = GenerateConditionsCode(comp.conditions, comp.conditionLogic);
                sb.AppendLine($"{indent}if ({conditionCode})");
                sb.AppendLine($"{indent}{{");
                innerIndent = indent + "    ";
            }

            // Generate actions
            if (comp.actions != null)
            {
                foreach (var action in comp.actions)
                {
                    GenerateActionCode(sb, action, innerIndent, entity);
                }
            }

            // Generate else actions
            if (hasConditions && comp.elseActions != null && comp.elseActions.Count > 0)
            {
                sb.AppendLine($"{indent}}}");
                sb.AppendLine($"{indent}else");
                sb.AppendLine($"{indent}{{");

                foreach (var action in comp.elseActions)
                {
                    GenerateActionCode(sb, action, innerIndent, entity);
                }
            }

            if (hasConditions)
            {
                sb.AppendLine($"{indent}}}");
            }
        }

        /// <summary>
        /// Generate condition expression code
        /// </summary>
        private static string GenerateConditionsCode(List<ConditionJSON> conditions, string logic)
        {
            if (conditions == null || conditions.Count == 0) return "true";

            var parts = new List<string>();
            foreach (var cond in conditions)
            {
                parts.Add(GenerateSingleCondition(cond));
            }

            string op = (logic?.ToUpper() == "OR") ? " || " : " && ";
            return string.Join(op, parts);
        }

        /// <summary>
        /// Generate code for a single condition
        /// </summary>
        private static string GenerateSingleCondition(ConditionJSON cond)
        {
            var p = cond.GetAllParams();

            switch (cond.type)
            {
                // Input conditions
                case "InputDown":
                case "InputHeld":
                case "InputKey":
                    {
                        string key = cond.key ?? GetParamString(p, "key");
                        string unityKey = ConvertWebKeyToUnity(key);
                        return $"Input.GetKey(KeyCode.{unityKey})";
                    }
                case "InputPressed":
                    {
                        string key = cond.key ?? GetParamString(p, "key");
                        string unityKey = ConvertWebKeyToUnity(key);
                        return $"Input.GetKeyDown(KeyCode.{unityKey})";
                    }
                case "InputUp":
                    {
                        string key = cond.key ?? GetParamString(p, "key");
                        string unityKey = ConvertWebKeyToUnity(key);
                        return $"Input.GetKeyUp(KeyCode.{unityKey})";
                    }

                // Variable conditions
                case "VarEquals":
                case "VariableEquals":
                    {
                        string varName = ParameterHelper.SanitizeName(GetParamString(p, "name", "variable"));
                        string value = GetParamString(p, "value");
                        return $"{varName} == {ParameterHelper.FormatValue(value)}";
                    }
                case "VarGreaterThan":
                    {
                        string varName = ParameterHelper.SanitizeName(GetParamString(p, "name", "variable"));
                        string value = GetParamString(p, "value");
                        return $"{varName} > {ParameterHelper.FormatValue(value)}";
                    }
                case "VarLessThan":
                    {
                        string varName = ParameterHelper.SanitizeName(GetParamString(p, "name", "variable"));
                        string value = GetParamString(p, "value");
                        return $"{varName} < {ParameterHelper.FormatValue(value)}";
                    }

                // Status conditions
                case "IsAlive":
                    return "hp > 0";
                case "IsGrounded":
                    return "_isGrounded";

                // Distance conditions
                case "InRange":
                    {
                        float range = GetParamFloat(p, "range", 100f) / 100f;
                        string targetRole = GetParamString(p, "targetRole");
                        if (!string.IsNullOrEmpty(targetRole))
                            return $"UniforgeEntity.GetDistanceToNearestWithRole(_transform, \"{targetRole}\") <= {range}f";
                        return $"UniforgeEntity.GetDistanceToNearest(_transform) <= {range}f";
                    }

                default:
                    Debug.LogWarning($"[LogicCodeGenerator] Unknown condition type: {cond.type}");
                    return "true";
            }
        }

        /// <summary>
        /// Generate code for a single action
        /// </summary>
        private static void GenerateActionCode(StringBuilder sb, ActionJSON action, string indent, EntityJSON entity)
        {
            if (action == null || string.IsNullOrEmpty(action.type))
            {
                sb.AppendLine($"{indent}// Warning: Empty action");
                return;
            }

            // Get all parameters (merged from @params and flat fields)
            var p = action.GetAllParams();

            sb.AppendLine($"{indent}// Action: {action.type}");

            switch (action.type)
            {
                case "Log":
                    {
                        string msg = GetParamString(p, "message");
                        sb.AppendLine($"{indent}Debug.Log(\"{msg}\");");
                        break;
                    }

                case "Move":
                    {
                        float speed = GetParamFloat(p, "speed", 200f) / 100f;
                        var dir = GetParamVector2(p, "direction");
                        sb.AppendLine($"{indent}_transform.Translate(new Vector3({dir.x}f, {-dir.y}f, 0).normalized * {speed}f * Time.deltaTime);");
                        break;
                    }

                case "Rotate":
                    {
                        float speed = GetParamFloat(p, "speed", 90f);
                        sb.AppendLine($"{indent}_transform.Rotate(0, 0, {speed}f * Time.deltaTime);");
                        break;
                    }

                case "Jump":
                    {
                        float force = GetParamFloat(p, "force", 500f) / 100f;
                        sb.AppendLine($"{indent}if (_isGrounded && _rigidbody != null)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0);");
                        sb.AppendLine($"{indent}    _rigidbody.AddForce(Vector2.up * {force}f, ForceMode2D.Impulse);");
                        sb.AppendLine($"{indent}    _isGrounded = false;");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                case "SetVar":
                    {
                        string varName = ParameterHelper.SanitizeName(GetParamString(p, "name"));
                        string operation = GetParamString(p, "operation", "Set");
                        string op1 = GetOperandCode(p, "operand1");
                        string op2 = GetOperandCode(p, "operand2");

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
                                sb.AppendLine($"{indent}{varName} = ({op2} != 0) ? {op1} / {op2} : 0;");
                                break;
                            default:
                                sb.AppendLine($"{indent}{varName} = {op1};");
                                break;
                        }
                        break;
                    }

                case "FireProjectile":
                    {
                        float speed = GetParamFloat(p, "speed", 500f) / 100f;
                        float damage = GetParamFloat(p, "damage", 10f);
                        string targetRole = GetParamString(p, "targetRole", "enemy");

                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    Vector3 fireDir = Vector3.right;");
                        if (!string.IsNullOrEmpty(targetRole))
                        {
                            sb.AppendLine($"{indent}    var target = UniforgeEntity.FindNearestWithRole(\"{targetRole}\", _transform.position);");
                            sb.AppendLine($"{indent}    if (target != null) fireDir = (target.transform.position - _transform.position).normalized;");
                        }
                        sb.AppendLine($"{indent}    ProjectileManager.FireDirectionStatic(_transform.position, fireDir, {speed}f, {damage}f);");
                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                case "SpawnEntity":
                    {
                        // Support multiple parameter names
                        string prefabId = GetParamString(p, "prefabId", "sourceAssetId", "templateId");
                        string posMode = GetParamString(p, "positionMode", "relative");
                        float offsetX = GetParamFloat(p, "offsetX", 0) / 100f;
                        float offsetY = GetParamFloat(p, "offsetY", 0) / 100f;

                        sb.AppendLine($"{indent}// SpawnEntity: {prefabId}");
                        sb.AppendLine($"{indent}{{");

                        if (string.IsNullOrEmpty(prefabId))
                        {
                            sb.AppendLine($"{indent}    Debug.LogWarning(\"SpawnEntity: No prefabId specified\");");
                        }
                        else if (posMode == "absolute")
                        {
                            float absX = GetParamFloat(p, "x", 0) / 100f;
                            float absY = GetParamFloat(p, "y", 0) / 100f;
                            sb.AppendLine($"{indent}    PrefabRegistry.SpawnStatic(\"{prefabId}\", new Vector3({absX}f, {-absY}f, 0));");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    PrefabRegistry.SpawnStatic(\"{prefabId}\", _transform.position + new Vector3({offsetX}f, {-offsetY}f, 0));");
                        }

                        sb.AppendLine($"{indent}}}");
                        break;
                    }

                case "Destroy":
                    {
                        string targetId = GetParamString(p, "targetId");
                        if (!string.IsNullOrEmpty(targetId))
                        {
                            sb.AppendLine($"{indent}var destroyTarget = GameObject.Find(\"{targetId}\");");
                            sb.AppendLine($"{indent}if (destroyTarget != null) Destroy(destroyTarget);");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}Destroy(gameObject);");
                        }
                        break;
                    }

                case "PlayAnimation":
                    {
                        string animName = GetParamString(p, "animationName", "name");
                        sb.AppendLine($"{indent}if (_animator != null) _animator.Play(\"{animName}\");");
                        break;
                    }

                case "PlayParticle":
                    {
                        string preset = GetParamString(p, "preset", "hit_spark");
                        float scale = GetParamFloat(p, "scale", 1f);
                        sb.AppendLine($"{indent}ParticleManager.PlayStatic(\"{preset}\", _transform.position, {scale}f);");
                        break;
                    }

                case "PlaySound":
                    {
                        string soundId = GetParamString(p, "soundId");
                        sb.AppendLine($"{indent}AudioManager.PlayStatic(\"{soundId}\");");
                        break;
                    }

                case "TakeDamage":
                    {
                        float amount = GetParamFloat(p, "amount", 10f);
                        sb.AppendLine($"{indent}hp -= {amount}f;");
                        sb.AppendLine($"{indent}if (hp <= 0) {{ hp = 0; OnDeath(); }}");
                        break;
                    }

                case "Heal":
                    {
                        float amount = GetParamFloat(p, "amount", 10f);
                        sb.AppendLine($"{indent}hp += {amount}f;");
                        sb.AppendLine($"{indent}if (hp > maxHp) hp = maxHp;");
                        break;
                    }

                case "ChangeScene":
                    {
                        string sceneName = GetParamString(p, "sceneName", "sceneId");
                        sb.AppendLine($"{indent}SceneManager.LoadScene(\"{sceneName}\");");
                        break;
                    }

                default:
                    sb.AppendLine($"{indent}// TODO: Unhandled action type: {action.type}");
                    break;
            }
        }

        #region Helper Methods

        private static string GetEventParamString(Dictionary<string, object> eventParams, params string[] keys)
        {
            if (eventParams == null) return "";
            foreach (var key in keys)
            {
                if (eventParams.ContainsKey(key) && eventParams[key] != null)
                    return eventParams[key].ToString();
            }
            return "";
        }

        private static string GetParamString(Dictionary<string, object> p, params string[] keys)
        {
            if (p == null) return "";
            foreach (var key in keys)
            {
                if (p.ContainsKey(key) && p[key] != null)
                    return p[key].ToString();
            }
            return "";
        }

        private static float GetParamFloat(Dictionary<string, object> p, string key, float defaultVal = 0)
        {
            if (p == null || !p.ContainsKey(key) || p[key] == null) return defaultVal;
            if (float.TryParse(p[key].ToString(), out float result))
                return result;
            return defaultVal;
        }

        private static Vector2 GetParamVector2(Dictionary<string, object> p, string key)
        {
            if (p == null || !p.ContainsKey(key) || p[key] == null)
                return Vector2.zero;

            var obj = p[key];
            if (obj is JObject jo)
            {
                // Handle nested value source: { type: "literal", value: { x, y } }
                if (jo["type"]?.ToString() == "literal" && jo["value"] is JObject valueObj)
                {
                    float vx = (float?)valueObj["x"] ?? 0;
                    float vy = (float?)valueObj["y"] ?? 0;
                    return new Vector2(vx, vy);
                }
                // Direct vector: { x, y }
                float x = (float?)jo["x"] ?? 0;
                float y = (float?)jo["y"] ?? 0;
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private static string GetOperandCode(Dictionary<string, object> p, string key)
        {
            if (p == null || !p.ContainsKey(key) || p[key] == null) return "0";

            var val = p[key];
            if (val is JObject jo)
            {
                string type = jo["type"]?.ToString() ?? "literal";

                switch (type)
                {
                    case "variable":
                        string varName = jo["name"]?.ToString() ?? "";
                        return ParameterHelper.SanitizeName(varName);

                    case "literal":
                        var litVal = jo["value"];
                        if (litVal is JObject vec && vec["x"] != null && vec["y"] != null)
                        {
                            float x = (float?)vec["x"] ?? 0;
                            float y = (float?)vec["y"] ?? 0;
                            return $"new Vector2({x}f, {y}f)";
                        }
                        return ParameterHelper.FormatValue(litVal?.ToString());

                    case "mouse":
                        string axis = jo["axis"]?.ToString();
                        string mode = jo["mode"]?.ToString() ?? "absolute";

                        if (string.IsNullOrEmpty(axis))
                        {
                            // Return Vector2
                            if (mode == "relative")
                                return "new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - _transform.position.x, " +
                                       "Camera.main.ScreenToWorldPoint(Input.mousePosition).y - _transform.position.y)";
                            else
                                return "new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, " +
                                       "Camera.main.ScreenToWorldPoint(Input.mousePosition).y)";
                        }
                        else
                        {
                            // Single axis
                            if (mode == "relative")
                                return axis == "x"
                                    ? "(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - _transform.position.x)"
                                    : "(Camera.main.ScreenToWorldPoint(Input.mousePosition).y - _transform.position.y)";
                            else
                                return axis == "x"
                                    ? "Camera.main.ScreenToWorldPoint(Input.mousePosition).x"
                                    : "Camera.main.ScreenToWorldPoint(Input.mousePosition).y";
                        }

                    default:
                        if (jo["x"] != null && jo["y"] != null)
                        {
                            float x = (float?)jo["x"] ?? 0;
                            float y = (float?)jo["y"] ?? 0;
                            return $"new Vector2({x}f, {y}f)";
                        }
                        break;
                }
            }

            return ParameterHelper.FormatValue(val.ToString());
        }

        public static string ConvertWebKeyToUnity(string webKey)
        {
            if (string.IsNullOrEmpty(webKey)) return "None";

            // Handle "Key" prefix (KeyA -> A)
            if (webKey.StartsWith("Key"))
                return webKey.Substring(3);

            switch (webKey)
            {
                case "ArrowUp": return "UpArrow";
                case "ArrowDown": return "DownArrow";
                case "ArrowLeft": return "LeftArrow";
                case "ArrowRight": return "RightArrow";
                case "Space": return "Space";
                case "Enter": return "Return";
                case "Escape": return "Escape";
                case "ShiftLeft":
                case "ShiftRight": return "LeftShift";
                case "ControlLeft":
                case "ControlRight": return "LeftControl";
                default:
                    if (webKey.StartsWith("Digit"))
                        return "Alpha" + webKey.Substring(5);
                    return webKey;
            }
        }

        #endregion
    }
}
