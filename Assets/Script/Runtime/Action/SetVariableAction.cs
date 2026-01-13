using UnityEngine;

namespace Metamong.Runtime.Actions
{
    public class SetVariableAction : IAction
    {
        private string varName;
        private object value;

        public SetVariableAction(string name, object value)
        {
            this.varName = name;
            this.value = value;
        }

        public void Execute(ActionContext ctx)
        {
            var vars = ctx.Owner.GetComponent<RuntimeVariables>();
            if (vars == null) return;

            vars.SetValue(varName, value);
        }
    }
}