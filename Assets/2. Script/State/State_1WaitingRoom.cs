using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//1.대기실
namespace Yeon
{
    public class State_1WaitingRoom : StateBase
    {
        public override void Enter()
        {
            base.Enter();
            Debug.Log("1번 시작");
        }

        public override void Execute()
        {
            base.Execute();
            Debug.Log("1번 실행중...");
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("1번 종료");
        }

        public override void OnClick()
        {
            Debug.Log("OnClick (State_1WatingRoom)");
            StateMachine.ChangeState(GetNextState());
        }

        public override StateBase GetNextState()
        {
            return new State_2AnswerScene();
        }
    }
}