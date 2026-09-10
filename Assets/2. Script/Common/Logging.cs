using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

public class Logging : MonoBehaviour
{

    private string directory = null;
    private string path = "";
    //DirectoryInfo directoryInfo = null;
    FileStream fs = null;
    StreamWriter writer = null;

    int count = 1;

    //Singleton. 씬전환 시에도 로그 남기는 텍스트파일 유지위함.
    public static Logging instance = null;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        //path = pathForDocumentsFile("pressurelog.txt");
        //directoryInfo = new DirectoryInfo(path);
        directory = Application.persistentDataPath + "/Log";
        path = directory + "/log.txt";
        if (PlayerPrefs.GetInt("LogFileCount") != 1)
            count = PlayerPrefs.GetInt("LogFileCount");
        else
            count = 1;
    }

    //안드로이드 로그
    //
    //
    public void logPressure(string pressure)
    {
        //Debug.Log("logging");
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }


        //디렉토리에 파일이 없으면 만들고, 있으면 열어서 끝으로 이동.  
        if (!File.Exists(path))
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        }
        else
        {
            fs = new FileStream(path, FileMode.Append, FileAccess.Write);
        }
        // 파일이 65000줄 보다 길어 엑셀처리 힘들경우 기존 데이터 닫고 다시 만들기
        if (fs.Length > 65000)
        {
            fs.Flush();
            fs.Close();
            try
            {
                fs = new FileStream(directory + "/log" + count + ".txt", FileMode.CreateNew, FileAccess.Write);
            }
            catch(IOException e)
            {
                count++;
                PlayerPrefs.SetInt("LogFileCount", count);
                fs = new FileStream(directory + "/log" + count + ".txt", FileMode.CreateNew, FileAccess.Write);
            }
            path = directory + "/log" + count + ".txt";
        }

        writer = new StreamWriter(fs);

        string logfrm = DateTime.Now.ToString("yyyyMMdd, hh:mm:ss.fff") + ", ";
        //Debug.Log(pressure.GetType().Name);
        try
        {
            if (float.Parse(pressure) < -1f)
                logfrm += ("Inhaling," + pressure);
            else if (float.Parse(pressure) > 1f)
                logfrm += ("Exhaling," + pressure);
        }
        catch (Exception ex)
        {
            logfrm += (", " + pressure);
        }
        writer.WriteLine(logfrm);
        writer.Close();
        fs.Close();
    }

    /// <summary>
    /// ClearTime logging
    /// 디렉토리에 파일이 없으면 만들고, 있으면 열어서 끝으로 이동, 클리어타임을 기록하는 함수 
    /// 
    /// </summary>
    /// <param name="clearTime">클리어타임(매 프레임마다 증가)</param>
    public void logClearTime(string clearTime)
    {
        //디렉토리에 파일이 없으면 만들고, 있으면 열어서 끝으로 이동.  
        if (!File.Exists(path))
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        }
        else
        {
            fs = new FileStream(path, FileMode.Append, FileAccess.Write);
        }
        // 파일이 65000줄 보다 길어 엑셀처리 힘들경우 기존 데이터 닫고 다시 만들기
        if (fs.Length > 65000)
        {
            fs.Flush();
            fs.Close();
            try
            {
                fs = new FileStream(directory + "/log" + count + ".txt", FileMode.CreateNew, FileAccess.Write);
            }
            catch (IOException e)
            {
                count++;
                fs = new FileStream(directory + "/log" + count + ".txt", FileMode.CreateNew, FileAccess.Write);
            }
            path = directory + "/log" + count + ".txt";
        }

        StreamWriter writer = new StreamWriter(fs);
        string logfrm = DateTime.Now.ToString("yyyyMMdd, hh:mm:ss.fff") + ", ClearTime, " + clearTime;  //작성 내용. 임시로 현재날짜쓰게돼있음.
        writer.WriteLine(logfrm);
        writer.Close();
        fs.Close();
    }
}