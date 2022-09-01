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
		if (C.Dave.LastRoom == R.Hardware) {
			C.Dave.SetPosition(Point("HardwarePoint"));
		} else {
			C.Dave.SetPosition(Point("HomePoint"));
		}
		
		//GUI
		G.Inventory.Hide();
	}

	IEnumerator OnInteractPropHome( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HomePoint"));
		C.Dave.ChangeRoomBG(R.Home);
		G.Inventory.Show();
		yield return E.Break;
	}

	IEnumerator OnInteractPropHardware( IProp prop )
	{
		yield return C.Dave.WalkTo(Point("HardwarePoint"));
		C.Dave.ChangeRoomBG(R.Hardware);
		G.Inventory.Show();
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

		yield return C.Display(" The Hardware Store", 7);
		yield return E.Break;
	}

	IEnumerator OnLookAtPropHome( IProp prop )
	{
		yield return C.Display(" Home", 8);
		yield return E.Break;
	}

	IEnumerator OnLookAtPropNeighbor1( IProp prop )
	{
		yield return C.Display(" Jim's House", 9);
		yield return E.Break;
	}

	IEnumerator OnLookAtPropNeighbor2( IProp prop )
	{
		yield return C.Display(" Bob's House", 10);
		yield return E.Break;
	}
}