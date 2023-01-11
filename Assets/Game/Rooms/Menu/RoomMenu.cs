using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomMenu : RoomScript<RoomMenu>
{


	void OnEnterRoom()
	{
		
		//G.Inventory.Hide();
		G.TitleMenu.Show();
	}

	IEnumerator OnExitRoom( IRoom oldRoom, IRoom newRoom )
	{
		G.TitleMenu.Hide();
		G.ChapterSelect.Hide();
		//G.Inventory.Show();
		
		yield return E.Break;
	}
}