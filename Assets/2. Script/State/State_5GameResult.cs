using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 5게임결과
namespace Yeon
{
    public class State_5GameResult : StateBase
    {
        public override void Enter()
        {
            base.Enter();
            StateMachine.ChangeState(GetNextState());
        }

        public override void Execute()
        {
            base.Execute();
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override StateBase GetNextState()
        {
            return null;
        }
    }
}