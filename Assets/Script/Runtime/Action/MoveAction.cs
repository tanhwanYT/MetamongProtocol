using UnityEngine;

namespace Metamong.Runtime.Actions
{
    public class MoveAction : IAction
    {
        private readonly float x;
        private readonly float y;
        private readonly float speed;

        public MoveAction(float x, float y, float speed)
        {
            this.x = x;
            this.y = y;
            this.speed = speed;
        }

        public void Execute(ActionContext ctx)
        {
            ctx.Owner.transform.position +=
                new Vector3(x, y, 0f) * speed * ctx.DeltaTime;
        }
    }
}
