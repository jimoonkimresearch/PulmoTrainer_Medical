using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketSoundManager: MonoBehaviour
{
    GameObject MusicStop;
    public AudioSource backmusic;
    public AudioSource explode;
    public AudioSource flying;
    public AudioSource scoreBoard;
    public AudioSource perfectSound;
    // 배경음악이 재생될 수 있도록 AudioSource 받아서 나오는 코드 
    void Awake()
    {
        //Button = GameObject.Find("BackgroundMusic");
        //backmusic = MusicStop.GetComponent<AudioSource>();
        //if (backmusic.isPlaying) return; // 배경음악이 재생되고 있으면 패스
        //else
        //{
        //    backmusic.Stop();
        //}
    }
    // 배경음악을 멈추는 정지 버튼 생성 코드 
    public void BackGroundMusicOff()
    {
        MusicStop = GameObject.Find("MusicStop");
        backmusic = MusicStop.GetComponent<AudioSource>();
        //if (backmusic.isPlaying) backmusic.Pause();
        //else backmusic.Stop();
    }

    private void Start()
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

    public void OnLaunchSound()
    {
        explode.Play();
        flying.Play();
    }

    public void ScoreBoardSound()
    {
        scoreBoard.Play();
    }

    public void OnPerfectGame()
    {
        perfectSound.gameObject.SetActive(true);
        perfectSound.Play();
    }
}
//https://ansohxxn.github.io/unity%20lesson%202/ch9-1/
