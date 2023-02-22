using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiExplanation : GuiScript<GuiExplanation>
{


	IEnumerator OnAnyClick( IGuiControl control )
	{
		G.Explanation.Hide();
		yield return E.Break;
	}

	IEnumerator OnClickNewButtonText( IGuiControl control )
	{
		G.Explanation.Hide();
		
		yield return E.Break;
	}

	void OnShow()
	{
		string image = "Slide" + (int) Globals.gameStage;
		Image("EndChapter2").Anim = image;
		Debug.Log(image);
	}
}