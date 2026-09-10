using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Item : UIBehaviour 
{
	[SerializeField]
	Text uiText;

	[SerializeField]
	Image uiBackground;
	[SerializeField]
	Image uiIcon;

	private readonly Color[] colors = new Color[] {
		new Color(1, 1, 1, 1),
		new Color(0.9f, 0.9f, 1, 1),
	};

	public void UpdateItem(int count) 
	{
		uiText.text = (count + 1).ToString("00");
		uiBackground.color = colors[Mathf.Abs(count) % colors.Length];
		uiIcon.sprite = Resources.Load<Sprite>((Mathf.Abs(count) % 30 + 1).ToString("icon000"));
	}
}

[System.Serializable]
public class PlayerData
{
	public string playerName;
	public int playerLevel;
	public float playerProgrss;
	public int playerTrainingCount;

	PlayerData(string input, int level=0, float progress =0, int count=0)
    {
		playerName = input;
		playerLevel = level;
		playerProgrss = progress;
		playerTrainingCount = count;
    }

	public void print()
    {
		Debug.Log("Name: " + playerName + ", Level: " + playerLevel
			+ ", Progress" + playerProgrss + "%, TrainingCount: " + playerTrainingCount);
    }

	string ObjectToJson(object obj)
    {
		return JsonUtility.ToJson(obj);
    }

	T JsonToObject<T>(string jsondata)
    {
		return JsonUtility.FromJson<T>(jsondata);
    }
}