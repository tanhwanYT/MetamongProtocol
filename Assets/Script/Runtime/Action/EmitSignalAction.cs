using UnityEngine;

namespace Metamong.Runtime.Actions
{
    public class EmitSignalAction : IAction
    {
        private readonly string signal;

        public EmitSignalAction(string signal)
        {
            this.signal = signal;
        }

        public void Execute(ActionContext ctx)
        {
            if (ctx.Signals == null)
            {
                Debug.LogWarning("[EmitSignalAction] SignalBus not found");
                return;
            }

            ctx.Signals.Emit(signal);
        }
    }
}
