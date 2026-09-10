using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandleSoundManager : MonoBehaviour
{
    public AudioSource backmusic;
    public AudioSource scoreBoard;
    public AudioSource breathe;
    public AudioSource perfectSound;

    // Start is called before the first frame update
    void Start()
    {
        PlayMusic();
    }

    // 배경음악을 멈출 수 있게 하는 코드 
    // 버튼을 눌렀을 때 컨버넌트가 어떻게 연결되는지 잘 모르겠음... 
    public void PlayMusic()
    {
        if (backmusic.isPlaying) return;
        backmusic.Play();
    }

    public void StopMusic()
    {
        backmusic.Stop();
    }

    public void ScoreBoardSound()
    {
        scoreBoard.Play();
    }

    public void OnPerfectGame()
    {
        perfectSound.Play();
    }

    public void OnBreatheSound()
    {
        breathe.Play();
    }
}
