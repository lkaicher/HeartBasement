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
		yield return C.Display(" The Hardware Store");
		yield return E.Break;
	}

	IEnumerator OnLookAtPropHome( IProp prop )
	{
		yield return C.Display(" Home");
		yield return E.Break;
	}

	IEnumerator OnLookAtPropNeighbor1( IProp prop )
	{
		yield return C.Display(" Jim's House");
		yield return E.Break;
	}

	IEnumerator OnLookAtPropNeighbor2( IProp prop )
	{
		yield return C.Display(" Bob's House");
		yield return E.Break;
	}
}