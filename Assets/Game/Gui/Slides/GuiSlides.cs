using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiSlides : GuiScript<GuiSlides>
{


	void OnShow()
	{
		G.Inventory.Hide();
		
		string image = "Slide" + (int) Globals.gameStage;
		Debug.Log(image);
		
		Image("SlideImage").Anim = image;
		
	}

	IEnumerator OnClickContinueButton( IGuiControl control )
	{
		G.Explanation.Hide();
		G.Inventory.Show();
		yield return E.Break;
		
	}
}