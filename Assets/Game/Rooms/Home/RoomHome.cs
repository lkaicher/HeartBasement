using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHome : RoomScript<RoomHome>
{
	// This area is where you can put variables you want to use for game logic in your room
	
	// Here's an example variable, an integer which is used when clicking the sky.
	// The 'm_' at the start is just a naming convention so you can tell it's not just a 'local' variable
	int m_timesClickedSky = 0;
	
	// enums like this are a nice way of keeping track of what's happened in a room
	enum eThingsYouveDone { Start, InsultedChimp, EatenSandwich, LoadedCrossbow, AttackedFlyingNun, PhonedAlbatross }
	eThingsYouveDone m_thingsDone = eThingsYouveDone.Start;
	public void OnEnterRoom()
	{
		// Put things here that you need to set up BEFORE the room fades in (but nothing "blocking")
		// Note, you can also just do this at the top of OnEnterRoomAfterFade
		
		
		if ( R.Current.FirstTimeVisited ) // Only run this part the first time you visit
		{
		C.Dave.Say("Oh no! My basement is flooded! Maybe I can get something at the local hardware store to help. ");
		E.WaitSkip();
		C.Display("Left Click to Walk & Interact\nRight Click to Look At");
		}
		C.Dave.WalkToBG(Point("EntryWalk"));
		
		
		I.BilgePunp.Add();
		
		
		
	}

	public IEnumerator OnEnterRoomAfterFade()
	{
		// Put things here that happen when you enter a room
		
		if ( R.Current.FirstTimeVisited  ) // Only run this part the first time you visit
		{	C.Dave.WalkToBG(Point("EntryWalk"));
			yield return C.Dave.Say("Oh no! The basement is flooded!");
		   
		
			//Audio.PlayMusic("MusicExample");
			yield return E.WaitSkip();
			yield return C.Display("Left Click to Walk & Interact\nRight Click to Look At");
		}
		
	}





	
	IEnumerator OnLookAtHotspotDoor( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It's a door to the outside.");
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotDoor( IHotspot hotspot )
	{
			yield return C.Dave.Say("Ok Here I go...");
		C.Dave.ChangeRoomBG(R.Map);
		yield return E.Break;
	}

	IEnumerator OnUseInvHotspotDoor( IHotspot hotspot, IInventory item )
	{

		yield return E.Break;
	}


	IEnumerator OnUseInvPropWater( IProp prop, IInventory item )
	{
		// NB: You need to check they used the correct item!
		if ( item == I.BilgePunp )
		{ 
			yield return C.WalkToClicked();
			yield return C.FaceClicked();
			yield return C.Display("Dave begins to try to pump out the water.");
			Globals.m_progressExample = eProgress.TriedPump1;
			Globals.myVar = "testGlobal";
			yield return C.Dave.WalkTo(0,-400);
			yield return C.Dave.Say("This is too hard! I think the handle is too short and the diameter of the hose is too small, I need to go back to the hardware store. ");
			yield return E.Wait(1);
			  yield return E.WaitSkip();
			yield return C.Dave.FaceDown();
		
			
			// Here we're setting a custom 'enum' so we could check it somewhere else to see if the player won yet
				  
		}
		yield return E.Break;
		
	}

	IEnumerator OnLookAtPropWater( IProp prop )
	{
		yield return C.Dave.Say("That's alot of water!");
		yield return E.Break;
	}

	IEnumerator OnInteractPropWater( IProp prop )
	{
		yield return C.Dave.Say(" I can't clean out all of this using only my hands.");
		yield return E.Break;
	}
}