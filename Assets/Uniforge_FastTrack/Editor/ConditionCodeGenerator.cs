using System.Collections.Generic;
using System.Linq;

namespace Uniforge.FastTrack.Editor
{
    /// <summary>
    /// Generates C# condition code from UniForge condition definitions.
    /// Handles Variable checks, Input checks, Status checks, Distance checks, etc.
    /// Updated to match UniForgeFrontend DefaultConditions.ts
    /// </summary>
    public static class ConditionCodeGenerator
    {
        /// <summary>
        /// Generates combined condition expression from multiple conditions.
        /// </summary>
        public static string GenerateConditionsCode(List<ConditionJSON> conditions, string logic)
        {
            if (conditions == null || conditions.Count == 0) return "true";

            var parts = conditions.Select(c => GenerateSingleCondition(c)).ToList();
            string op = (logic?.ToUpper() == "OR") ? " || " : " && ";
            return string.Join(op, parts);
        }

        /// <summary>
        /// Generates a single condition expression.
        /// </summary>
        public static string GenerateSingleCondition(ConditionJSON cond)
        {
            var p = cond.GetAllParams();

            switch (cond.type)
            {
                // === Variable Conditions (with Frontend aliases) ===
                case "VariableEquals":
                case "IfVariableEquals":
                case "VarEquals":  // Frontend alias
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} == {ParameterHelper.FormatValue(value)}";
                    }
                case "VarNotEquals":
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} != {ParameterHelper.FormatValue(value)}";
                    }
                case "VariableGreaterThan":
                case "IfVariableGreaterThan":
                case "VarGreaterThan":  // Frontend alias
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} > {ParameterHelper.FormatValue(value)}";
                    }
                case "VariableLessThan":
                case "IfVariableLessThan":
                case "VarLessThan":  // Frontend alias
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} < {ParameterHelper.FormatValue(value)}";
                    }
                case "VarGreaterOrEqual":
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} >= {ParameterHelper.FormatValue(value)}";
                    }
                case "VarLessOrEqual":
                    {
                        string varName = ParameterHelper.GetParamString(p, "variable", "name");
                        string value = ParameterHelper.GetParamString(p, "value");
                        return $"{ParameterHelper.SanitizeName(varName)} <= {ParameterHelper.FormatValue(value)}";
                    }

                // === Status Conditions ===
                case "IsAlive":
                    return "hp > 0";
                case "HpBelow":
                    {
                        string value = ParameterHelper.GetParamString(p, "value", "0");
                        return $"hp < {ParameterHelper.FormatValue(value)}";
                    }
                case "HpAbove":
                    {
                        string value = ParameterHelper.GetParamString(p, "value", "0");
                        return $"hp > {ParameterHelper.FormatValue(value)}";
                    }
                case "RoleEquals":
                    {
                        string role = ParameterHelper.GetParamString(p, "role", "value");
                        return $"GetComponent<UniforgeEntity>()?.Role == \"{role}\"";
                    }

                // === Ground/Physics Conditions (NEW) ===
                case "IsGrounded":
                    {
                        // Use raycast to check if grounded
                        return "_isGrounded";
                    }

                // === Distance Conditions (NEW - Frontend InRange/OutOfRange) ===
                case "InRange":
                    {
                        float range = ParameterHelper.GetParamFloat(p, "range", 100f) / 100f;
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        string targetRole = ParameterHelper.GetParamString(p, "targetRole");

                        if (!string.IsNullOrEmpty(targetId))
                        {
                            return $"UniforgeEntity.GetDistanceToEntity(_transform, \"{targetId}\") <= {range}f";
                        }
                        else if (!string.IsNullOrEmpty(targetRole))
                        {
                            return $"UniforgeEntity.GetDistanceToNearestWithRole(_transform, \"{targetRole}\") <= {range}f";
                        }
                        return $"UniforgeEntity.GetDistanceToNearest(_transform) <= {range}f";
                    }
                case "OutOfRange":
                    {
                        float range = ParameterHelper.GetParamFloat(p, "range", 100f) / 100f;
                        string targetId = ParameterHelper.GetParamString(p, "targetId");
                        string targetRole = ParameterHelper.GetParamString(p, "targetRole");

                        if (!string.IsNullOrEmpty(targetId))
                        {
                            return $"UniforgeEntity.GetDistanceToEntity(_transform, \"{targetId}\") > {range}f";
                        }
                        else if (!string.IsNullOrEmpty(targetRole))
                        {
                            return $"UniforgeEntity.GetDistanceToNearestWithRole(_transform, \"{targetRole}\") > {range}f";
                        }
                        return $"UniforgeEntity.GetDistanceToNearest(_transform) > {range}f";
                    }

                // === Signal Conditions (NEW) ===
                case "SignalFlag":
                    {
                        string key = ParameterHelper.GetParamString(p, "key");
                        return $"_signalFlags.ContainsKey(\"{key}\") && _signalFlags[\"{key}\"]";
                    }

                // === Input Conditions ===
                case "InputDown":
                case "InputHeld":
                case "InputKey":  // Frontend alias
                    {
                        // Get key from condition object directly
                        string key = cond.key ?? "";
                        if (string.IsNullOrEmpty(key) && p.ContainsKey("key"))
                            key = p["key"]?.ToString() ?? "";

                        string unityKeyCode = ConvertWebKeyToUnity(key);
                        // InputDown = key pressed this frame (single trigger)
                        return $"Input.GetKeyDown(KeyCode.{unityKeyCode})";
                    }
                case "InputUp":
                    {
                        string key = cond.key ?? "";
                        if (string.IsNullOrEmpty(key) && p.ContainsKey("key"))
                            key = p["key"]?.ToString() ?? "";
                        string unityKeyCode = ConvertWebKeyToUnity(key);
                        return $"Input.GetKeyUp(KeyCode.{unityKeyCode})";
                    }
                case "InputPressed":  // Key just pressed this frame
                    {
                        string key = cond.key ?? "";
                        if (string.IsNullOrEmpty(key) && p.ContainsKey("key"))
                            key = p["key"]?.ToString() ?? "";
                        string unityKeyCode = ConvertWebKeyToUnity(key);
                        return $"Input.GetKeyDown(KeyCode.{unityKeyCode})";
                    }

                // === Direction Input Conditions (NEW - Frontend shortcuts) ===
                case "InputLeft":
                    return "Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)";
                case "InputRight":
                    return "Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)";
                case "InputMoveUp":  // Renamed to avoid conflict with InputUp (key release)
                    return "Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)";
                case "InputMoveDown":
                    return "Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)";
                case "InputJump":
                    return "Input.GetKey(KeyCode.Space)";

                // === Cooldown Conditions (NEW) ===
                case "CooldownReady":
                    {
                        string cooldownId = ParameterHelper.GetParamString(p, "cooldownId", "id");
                        return $"IsCooldownReady(\"{cooldownId}\")";
                    }

                default:
                    // Log warning for unknown condition type but return true to not break logic
                    UnityEngine.Debug.LogWarning($"[Uniforge] Unknown condition type: {cond.type}");
                    return "true";
            }
        }

        /// <summary>
        /// Converts web key codes (KeyA, ArrowUp) to Unity KeyCode names.
        /// </summary>
        public static string ConvertWebKeyToUnity(string webKey)
        {
            if (string.IsNullOrEmpty(webKey)) return "None";

            // Normalize input
            string key = webKey.Trim();

            // Handle "Key" prefix (KeyA -> A)
            if (key.StartsWith("Key") && key.Length == 4)
                return key.Substring(3);

            // Handle common key mappings
            switch (key)
            {
                // Arrow keys
                case "ArrowUp": case "Up": return "UpArrow";
                case "ArrowDown": case "Down": return "DownArrow";
                case "ArrowLeft": case "Left": return "LeftArrow";
                case "ArrowRight": case "Right": return "RightArrow";

                // Special keys
                case "Space": case " ": return "Space";
                case "Enter": case "Return": return "Return";
                case "Escape": case "Esc": return "Escape";
                case "Tab": return "Tab";
                case "Backspace": return "Backspace";
                case "Delete": return "Delete";
                case "Insert": return "Insert";
                case "Home": return "Home";
                case "End": return "End";
                case "PageUp": return "PageUp";
                case "PageDown": return "PageDown";

                // Modifier keys
                case "ShiftLeft": case "Shift": return "LeftShift";
                case "ShiftRight": return "RightShift";
                case "ControlLeft": case "Control": case "Ctrl": return "LeftControl";
                case "ControlRight": return "RightControl";
                case "AltLeft": case "Alt": return "LeftAlt";
                case "AltRight": return "RightAlt";

                // Function keys
                case "F1": case "F2": case "F3": case "F4":
                case "F5": case "F6": case "F7": case "F8":
                case "F9": case "F10": case "F11": case "F12":
                    return key;

                // Mouse buttons
                case "MouseLeft": case "Mouse0": return "Mouse0";
                case "MouseRight": case "Mouse1": return "Mouse1";
                case "MouseMiddle": case "Mouse2": return "Mouse2";

                default:
                    // Handle digit keys (Digit0 -> Alpha0)
                    if (key.StartsWith("Digit") && key.Length == 6)
                        return "Alpha" + key.Substring(5);

                    // Handle numpad keys (Numpad0 -> Keypad0)
                    if (key.StartsWith("Numpad"))
                        return "Keypad" + key.Substring(6);

                    // Single letter keys (a-z, A-Z)
                    if (key.Length == 1 && char.IsLetter(key[0]))
                        return key.ToUpper();

                    // Single digit (0-9)
                    if (key.Length == 1 && char.IsDigit(key[0]))
                        return "Alpha" + key;

                    // Validate that the key exists in Unity's KeyCode enum
                    if (System.Enum.TryParse<UnityEngine.KeyCode>(key, true, out _))
                        return key;

                    // Unknown key - log warning and return as-is
                    UnityEngine.Debug.LogWarning($"[Uniforge] Unknown key code: {webKey}, using as-is");
                    return key;
            }
        }
    }
}
