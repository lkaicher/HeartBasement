using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHardware : RoomScript<RoomHardware>
{


	IEnumerator OnInteractHotspotDoor( IHotspot hotspot )
	{
			yield return C.Dave.Say("Let's take this back home and see how it works!");
		C.Dave.ChangeRoomBG(R.Map);
		yield return E.Break;
	}

	void OnEnterRoom()
	{
		// Globals.m_progressExample = eProgress.TriedPump1;
		if(Globals.m_progressExample == eProgress.TriedPump1)
		{
		
		C.Neighbor1.Visible=true;
		C.Neighbor2.Visible=true;
		}
		else{
		
		C.Neighbor1.Visible=false;
		C.Neighbor2.Visible=false;
		}
	}

	IEnumerator OnEnterRoomAfterFade()
	{

		yield return E.Break;
	}
}