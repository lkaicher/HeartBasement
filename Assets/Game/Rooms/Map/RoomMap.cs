using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomMap : RoomScript<RoomMap>
{

	GameObject rain;
	
	void OnEnterRoom()
	{
		rain = GameObject.Find("Rain");
		rain.SetActive(false);

		
		
		C.Dave.SetPosition(Point(string.Format("{0}Point",C.Dave.LastRoom.ScriptName)));
		

		
		// if (C.Dave.LastRoom == R.Hardware) {
		//	 C.Dave.SetPosition(Point("HardwarePoint"));
		// } else if (C.Dave.LastRoom == R.Home){
		//	 C.Dave.SetPosition(Point("HomePoint"));
		// } else {
		//	 C.Dave.SetPosition(Point("StartingPoint"));
		// }
		
		// // Display:  The hardware store is to the north
		// if ( R.Current.FirstTimeVisited ) {
		//  C.Dave.Say(" What a trip!", 32);
		// }
		
		
		
		
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

	IEnumerator OnLookAtCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterDave( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterTony( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterTony( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterHardwareClerk( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterNeighbor2( ICharacter character, IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterNeighbor2( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterNeighbor2( ICharacter character )
	{

		yield return E.Break;
	}

	public IEnumerator Thunderstorm()
	{
		rain.SetActive(true);
		C.Dave.Visible = false;
		//yield return E.FadeIn(1);
		yield return Prop("Back").Fade(1,(float) 0.50, 3,eEaseCurve.Smooth);
		yield return E.Wait(3);
		yield return E.FadeOut(1);
		rain.SetActive(false);

		//C.Dave.Visible = true;
		C.Dave.ChangeRoom(R.Home);
		yield return E.FadeIn(1);
		//Debug.Log("HEY");
		yield return E.Break;
	}

	IEnumerator OnEnterRoomAfterFade()
	{
		if((int)Globals.gameStage == 6){
			yield return Thunderstorm();
		
		}
		
		yield return E.Break;
	}
}