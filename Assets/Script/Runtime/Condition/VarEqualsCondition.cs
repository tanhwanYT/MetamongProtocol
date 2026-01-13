using UnityEngine;

public class VarEqualsCondition : ICondition
{
    private string varName;
    private object expected;

    public VarEqualsCondition(string name, object value)
    {
        varName = name;
        expected = value;
    }

    public bool Evaluate(GameObject owner)
    {
        var vars = owner.GetComponent<RuntimeVariables>();
        if (vars == null) return false;

        var actual = vars.GetValue(varName);
        return Equals(actual, expected);
    }
}
