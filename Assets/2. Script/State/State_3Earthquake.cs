using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//3. 지진
namespace Yeon
{
    public class State_3Earthquake : StateBase
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
            return new State_4Playing();
        }
    }
}