using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomMap : RoomScript<RoomMap>
{

	
	
	
	void OnEnterRoom()
	{
		// Display:  The hardware store is to the north
		if (C.Dave.LastRoom == R.Home) {
			C.Dave.SetPosition(Point("HomePoint"));
		} else if (C.Dave.LastRoom == R.Hardware) {
			C.Dave.SetPosition(Point("HardwarePoint"));
		} else {
			C.Dave.SetPosition(Point("HomePoint"));
	}
	
	}

	IEnumerator OnInteractPropHome( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HomePoint"));
		C.Dave.ChangeRoomBG(R.Home);
		yield return E.Break;
	}

	IEnumerator OnInteractPropHardware( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HardwarePoint"));
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

	IEnumerator OnInteractPropHillHouse( IProp prop )
	{
		Globals.m_progressExample = eProgress.WonGame;
		C.Dave.ChangeRoom(R.Cutscene);
		
		
		yield return E.Break;
	}
}