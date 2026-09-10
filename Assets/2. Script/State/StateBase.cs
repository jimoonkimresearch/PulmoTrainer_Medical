using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yeon
{
    //StateBase : State들의 부모
    //StateBase는 기본틀로 Enter / Execute / Exit / OnClick / GetNextState 를 만들고 버츄얼붙이기
    public abstract class StateBase
    {
        /// <summary>
        /// 시작할때 필요한 기능들 (유니티에서의 Start)
        /// </summary>
        public virtual void Enter()
        {
        }

        /// <summary>
        /// 실행 중 매번 실행할 코드들 (유니티에서의 Update)
        /// </summary>
        public virtual void Execute()
        {
        }

        /// <summary>
        /// 종료 (유니티에서의 OnDestroy) 삭제되기직전에 실행
        /// </summary>
        public virtual void Exit()
        {
        }

        //TO DO: 어느버튼 눌렸는지 매개변수 넣은 버젼 메소드로 업그레이드하기
        public virtual void OnClick()
        {
        }

        public abstract StateBase GetNextState();
    }
}