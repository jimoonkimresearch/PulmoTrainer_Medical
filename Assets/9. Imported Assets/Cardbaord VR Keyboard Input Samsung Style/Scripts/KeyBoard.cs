using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class KeyBoard : MonoBehaviour {

	// Use this for initialization
	public float blinkingTime=1.5f;
	public Text objectiveText;
	public float distPos=0.1f;
	public Canvas mainCanvas;
	public bool conserveText=false;
    
    // this is the font of the texts
    public Font f;

	string previousText;
	float elapsed;
	bool capitalLeters,symbolMode,blink;
	int nb_leters;
	public Text[] charText;
	public Text[] charSymbol;
	string blinckTXT,actualTXT;

    //this is the attached animator
    Animator anim;


    public void updateFont()
    {
        GameObject[] go = GameObject.FindGameObjectsWithTag("but");

        anim = gameObject.GetComponent<Animator>();


        nb_leters = go.Length;
        charText = new Text[nb_leters];
        charSymbol = new Text[nb_leters];

        for (int ii = 0; ii < nb_leters; ii++)
        {
            charText[ii] = go[ii].transform.GetChild(0).GetComponent<Text>();
            charSymbol[ii] = go[ii].transform.GetChild(1).GetComponent<Text>();

            charText[ii].font = f;
            charSymbol[ii].font = f;
        }
    }
	void Start () 
	{
        /*  USE THIS AS A PROTOTYPE ONLY
		//GameObject[] go= GameObject.FindGameObjectsWithTag("char");
		nb_leters=go.Length;

		charText=new Text[nb_leters];

		for(int ii=0; ii<nb_leters;ii++)
		{
			charText[ii]=go[ii].GetComponent<Text>();
		}


		//find all the symbols (on the background)
	    go=GameObject.FindGameObjectsWithTag("charBack");
		nb_symbol=go.Length;

		if(nb_symbol!=nb_leters)
		{

			Debug.Log("Error: symbols!=chars --> letters="+ nb_leters+ "  symbols="+nb_symbol);
			return;

		}

		charSymbol=new Text[nb_leters];
		for(int ii=0; ii<nb_leters;ii++)
		{
		  	//Debug.Log(go[ii].name+"  " +ii);
			charSymbol[ii]=go[ii].GetComponent<Text>();
		}
		*/
        updateFont();



        elapsed =0;
		objectiveText=null;

		mainCanvas.enabled=false;
		symbolMode=false;
		capitalLeters=false;
	
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		// only blink when a text is selected
		elapsed+=Time.fixedDeltaTime;
		if(objectiveText!=null)
		{
			
			if(elapsed>blinkingTime)
			{
				elapsed=0;


				//Debug.Log("blink");

				if(blink==false)
				{
					objectiveText.text=actualTXT;

				}
				else
				{
					objectiveText.text=blinckTXT;
				}

				blink=!blink;
			}

		}



	}


    //this function is called to write a char
	public void writeChar(Text txt)
	{
		objectiveText.text=actualTXT+txt.text;
		actualTXT=objectiveText.text;
		blinckTXT=objectiveText.text+" |";
	
	}


    //this function errases a char
	public void errase()
	{
		if(actualTXT.Length>0)
		{
			objectiveText.text=actualTXT.Remove(actualTXT.Length-1);
		}

		actualTXT=objectiveText.text;
		blinckTXT=objectiveText.text+" |";
	}


	// this function gets new input forms and stores temp string values
	public void selectTextInput(Text clickedText)
	{

		// prevent line to stay at the end of the text:
		if(objectiveText!=null)
		{
			objectiveText.text=actualTXT;
		}


		objectiveText=clickedText;
		previousText=clickedText.text;



		if(conserveText==false)
		{
			objectiveText.text="";
		}

		actualTXT=objectiveText.text;
		blinckTXT=objectiveText.text+" |";

        mainCanvas.enabled=true;
        anim.SetTrigger("fadeIN");



    }

    // when we press ok
	public void acceptText()
	{
		objectiveText.text=actualTXT;
        //mainCanvas.enabled=false;
        anim.SetTrigger("fadeOUT");
        objectiveText =null;
	
	}

    //when we press cancel
	public void cancelText()
	{
		
		objectiveText.text=previousText;
		mainCanvas.enabled=false;

		objectiveText=null;
	}

    //change leters to upper case
	public void uperLowerCase()
	{

		if(capitalLeters==false)
		{
			for(int ii=0; ii<nb_leters;ii++)
			{
				charText[ii].text=charText[ii].text.ToUpper();
				capitalLeters=true;
			}
		}
		else
		{
			for(int ii=0; ii<nb_leters;ii++)
			{
				charText[ii].text=charText[ii].text.ToLower();
				capitalLeters=false;
			}
		}
	}
	
    //change lettrers to symbols
	public void symbolChangeMode()
	{

		if(symbolMode==false)
		{
			
			string temp1;
			for(int ii=0; ii<nb_leters;ii++)
			{
				temp1=charText[ii].text;
				charText[ii].text=charSymbol[ii].text;
				charSymbol[ii].text=temp1;
			}

			symbolMode=true;



		}
		else
		{
			string temp2;
			for(int ii=0; ii<nb_leters;ii++)
			{
				temp2=charSymbol[ii].text;
				charSymbol[ii].text=charText[ii].text;
				charText[ii].text=temp2;
			}

			symbolMode=false;

		}
		
	}




}


