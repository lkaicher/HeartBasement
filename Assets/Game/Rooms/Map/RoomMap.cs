using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomMap : RoomScript<RoomMap>
{


	void OnEnterRoom()
	{
		C.Dave.Say(" The hardware store is to the northeast");
	}

	IEnumerator OnInteractPropHome( IProp prop )
	{
		C.Dave.ChangeRoomBG(R.Home);
		yield return E.Break;
	}

	IEnumerator OnInteractPropHardware( IProp prop )
	{
		C.Dave.ChangeRoomBG(R.Hardware);
		yield return E.Break;
	}

	IEnumerator OnInteractPropNeighbor2( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropNeighbor1( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtPropHardware( IProp prop )
	{

		yield return E.Break;
	}
}