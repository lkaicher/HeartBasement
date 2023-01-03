using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiTitleMenu : GuiScript<GuiTitleMenu>
{


	IEnumerator OnClickChapterSelect( IGuiControl control )
	{
		G.TitleMenu.Hide();
		G.ChapterSelect.Show();
		yield return E.Break;
	}
}