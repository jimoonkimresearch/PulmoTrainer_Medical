using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//2. 정답화면
namespace Yeon
{
    public class State_2AnswerScene : StateBase
    {
        public override void Enter()
        {
            base.Enter();

            Debug.Log("2번 시작");
        }

        public override void Execute()
        {
            base.Execute();
            Debug.Log("2번 실행중...");
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("2번 끝");
        }

        public override StateBase GetNextState()
        {
            return new State_3Earthquake();
        }
    }
}