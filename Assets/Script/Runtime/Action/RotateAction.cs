using UnityEngine;

namespace Metamong.Runtime.Actions
{
    public class RotateAction : IAction
    {
        private readonly float speed;

        public RotateAction(float speed = 90f)
        {
            this.speed = speed;
        }

        public void Execute(ActionContext ctx)
        {
            // 2D 기준: Z축 회전
            ctx.Owner.transform.Rotate(
                0f,
                0f,
                speed * ctx.DeltaTime
            );
        }
    }
}
