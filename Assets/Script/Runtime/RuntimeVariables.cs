using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class RuntimeVariables : MonoBehaviour
{
    [SerializeField]
    private List<VariableSO> variables = new List<VariableSO>();

    public void AddVariable(VariableSO variable)
    {
        variables.Add(variable);
    }

    public VariableSO GetVariable(string id)
    {
        return variables.Find(v => v.id == id);
    }
    public object GetValue(string id)
    {
        var v = GetVariable(id);
        return v != null ? v.GetValue() : null;
    }

    public T GetValue<T>(string id)
    {
        object value = GetValue(id);

        if (value == null)
            return default;

        if (value is T t)
            return t;

        Debug.LogWarning($"Variable '{id}' is not of type {typeof(T)}");
        return default;
    }

    public void SetValue(string id, object value)
    {
        var v = GetVariable(id);
        if (v != null)
        {
            v.SetValue(value);
        }
        else
        {
            Debug.LogWarning($"Variable not found: {id}");
        }
    }
}
