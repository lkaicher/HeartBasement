using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHardware : RoomScript<RoomHardware>
{


	IEnumerator OnInteractHotspotDoor( IHotspot hotspot )
	{
		if (Globals.m_progressExample == eProgress.None) {
		
			if (I.BilgePump.Owned){
			
				yield return C.Dave.Say("Let's take this back home and see how it works!", 10);
				C.Dave.ChangeRoomBG(R.Map);
			} else {
				yield return C.Dave.Say("I need to buy a pump.", 11);
			}
		} else {
			C.Dave.ChangeRoomBG(R.Map);
		}
			
		yield return E.Break;
	}

	void OnEnterRoom()
	{
		C.Dave.Position = Point("HWDoorPosition");
		C.Dave.Face(eFace.Right, true);
		C.HardwareClerk.ChangeRoomBG(R.Hardware);
		C.HardwareClerk.Position = Point("HWClerkPosition");
		
		// Globals.m_progressExample = eProgress.TriedPump1;
		
		
		if( Globals.m_progressExample >= eProgress.RightParts)
		{
			C.Tony.Enable();
		}
		else {
			C.Tony.Disable();
		}
		
		
		
		//E.SetPlayer(C.Tony);
	}

	IEnumerator OnEnterRoomAfterFade()
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotDoor( IHotspot hotspot )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAtCharacterHardwareClerk( ICharacter character )
	{
		
		
		yield return E.Break;
	}

	IEnumerator OnInteractPropBeerPack( IProp prop )
	{
		I.Beer.Add();
		Prop("BeerPack").Disable();
		
		yield return C.Display(" Beer has been added to your inventory.", 11);
		
		yield return E.Break;
	}

	IEnumerator OnInteractCharacterTony( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterDave( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterNeighbor2( ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnEnterRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnExitRegionScale( IRegion region, ICharacter character )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractCharacterHardwareClerk( ICharacter character )
	{

		yield return E.Break;
	}
}