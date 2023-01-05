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

	IEnumerator OnClickStartGame( IGuiControl control )
	{
		G.TitleMenu.Hide();
		if (!E.RestoreSave(1)){
			C.Dave.ChangeRoomBG(R.Home);
		}
		yield return E.Break;
	}
}