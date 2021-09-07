using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomCutscene : RoomScript<RoomCutscene>
{


	void Update()
	{
	}

	void OnEnterRoom()
	{
			Prop("Text").Visible = false;
			Prop("Text2").Visible = false;
			Prop("Text3").Visible = false;
			Prop("Text4").Visible = false;
			Prop("Text5").Visible = false;
		
		switch ( (int)Globals.m_progressExample ) {
				case 1:
					Prop("Text").Visible = true;
					break;
				case 2: 
					Prop("Text2").Visible = true;
					break;
				case 3:
				   Prop("Text3").Visible = true;
					break;
				case 4:
					 Prop("Text4").Visible = true;
					break;
				case 5:
					 Prop("Text5").Visible = true;
					break;
				default:
					break;
			}
	}

	IEnumerator OnInteractPropBack( IProp prop )
	{
		C.Dave.ChangeRoomBG(R.Home);
		//C.Dave.SetPosition(returnPosition);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtPropBack( IProp prop )
	{

		yield return E.Break;
	}
}