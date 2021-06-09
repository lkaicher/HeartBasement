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
}