using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yeon
{
    //현재상태를 저장하고 업데이트 해주는 클래스
    public class StateMachine
    {
        
        private static StateBase currentState = null; //현재상태 저장할 변수

        public static void OnClick()
        {
            Debug.Log("OnClick (StateMachine)");
            //if (currentState != null)
            //    currentState.OnClick();
            currentState?.OnClick(); // ?. 키워드 : 인스턴스 있으면 실행
        }

        //시작하자마자 체인지스테이트를 불러야한다
        public static void ChangeState(StateBase nextState) //상태 업데이트 함수
        {
            if (nextState == null)
                return;

            if (currentState != null)
            {
                if (currentState.GetType() == nextState.GetType())
                    return;
                currentState.Exit();
            }
            currentState = nextState;

            currentState.Enter();
        }

        public static void Execute()
        {
            if (currentState != null)
                currentState.Execute();
        }

        //다음 상태를 업데이트 해주는 ChangeState 메소드가 있으니까 일단 없어도될 것 같은 메소드
        public void NextState()
        {
            if (currentState == null)
                return;
            ChangeState(currentState.GetNextState());
        }
    }
}