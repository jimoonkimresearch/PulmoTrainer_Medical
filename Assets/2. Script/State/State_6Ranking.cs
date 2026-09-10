using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//6 랭킹
namespace Yeon
{
    public class State_6Ranking : StateBase
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
            return new State_1WaitingRoom();
        }
    }
}