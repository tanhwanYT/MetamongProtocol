using Metamong.Runtime.Actions;
using UnityEngine;

public interface IAction
{
    void Execute(ActionContext ctx);
}
