using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class GuiChapterSelect : GuiScript<GuiChapterSelect>
{

	void LoadChapter(int chapter){
		Globals.LoadingChapter = true;
		Globals.gameStage = (gameProgress) chapter;
		Globals.SetInventory();
		C.Dave.ChangeRoom(R.Home);
	}

	void OnShow()
	{
		
	}

	IEnumerator OnClickBack( IGuiControl control )
	{
		G.ChapterSelect.Hide();
		G.TitleMenu.Show();
		yield return E.Break;
	}

	IEnumerator OnClick0( IGuiControl control )
	{
		LoadChapter(0);
		yield return E.Break;
	}
	IEnumerator OnClick1( IGuiControl control )
	{
		LoadChapter(1);
		yield return E.Break;
	}
	IEnumerator OnClick2( IGuiControl control )
	{
		LoadChapter(2);
		yield return E.Break;
	}
	IEnumerator OnClick3( IGuiControl control )
	{
		LoadChapter(3);
		yield return E.Break;
	}
	IEnumerator OnClick4( IGuiControl control )
	{
		LoadChapter(4);
		yield return E.Break;
	}
	IEnumerator OnClick5( IGuiControl control )
	{
		LoadChapter(5);
		yield return E.Break;
	}
	IEnumerator OnClick6( IGuiControl control )
	{
		LoadChapter(6);
		yield return E.Break;
	}
	IEnumerator OnClick7( IGuiControl control )
	{
		LoadChapter(7);
		yield return E.Break;
	}
	IEnumerator OnClick8( IGuiControl control )
	{
		LoadChapter(8);
		yield return E.Break;
	}
	IEnumerator OnClick9( IGuiControl control )
	{
		LoadChapter(9);
		yield return E.Break;
	}
}