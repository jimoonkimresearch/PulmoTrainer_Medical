using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreathTestSoundManager : MonoBehaviour
{
    public AudioSource backmusic;
    public AudioSource PopBalloon;
    public AudioSource breatheOut;
    public AudioSource breathIn;
    public AudioSource perfectSound;
    public AudioClip scoreBoard;

    private AudioSource thisAudio = null;

    // Start is called before the first frame update
    void Start()
    {
        PlayMusic();
        thisAudio = this.GetComponent<AudioSource>();
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

    public void PopBalloonSound()
    {
        PopBalloon.Play();
    }

    public void OnBreatheOutSound()
    {
        if(breatheOut.isPlaying==false)
            breatheOut.Play();
    }

    public void OnBreathInSound()
    {
        if (breathIn.isPlaying == false)
            breathIn.Play();
    }

    public void ScoreBoardSound()
    {
        thisAudio.clip = scoreBoard;
        thisAudio.Play();

    }

    public void OnPerfectGame()
    {
        perfectSound.Play();
    }
}
