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
			Prop("Text").Disable();
			Prop("Text2").Disable();
			Prop("Text3").Disable();
			Prop("Text4").Disable();
			Prop("Text5").Disable();
		
		switch ( (int)Globals.m_progressExample ) {
				case 1:
					Prop("Text").Enable();
					break;
				case 2: 
					Prop("Text2").Enable();
					break;
				case 3:
				   Prop("Text3").Enable();
					break;
				case 4:
					 Prop("Text4").Enable();
					break;
				case 5:
					 Prop("Text5").Enable();
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