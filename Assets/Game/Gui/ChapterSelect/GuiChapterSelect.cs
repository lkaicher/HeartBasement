using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiChapterSelect : GuiScript<GuiChapterSelect>
{



	void OnShow()
	{
		
	}

	IEnumerator OnClickBack( IGuiControl control )
	{
		G.ChapterSelect.Hide();
		G.TitleMenu.Show();
		yield return E.Break;
	}
}