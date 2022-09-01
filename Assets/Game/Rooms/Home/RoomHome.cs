using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class RoomHome : RoomScript<RoomHome>
{
	// This area is where you can put variables you want to use for game logic in your room
	
	// Pump Part variables
	public enum handleType
	{
		small,
		medium,
		large
	};
	
	public enum hoseType
	{
		small,
		medium,
		large
	};
	
	private string[] sizeString = { "Small", "Medium", "Large" };
	
	
	handleType currentHandle = handleType.small;
	hoseType currentHose = hoseType.small;
	
	// Water level variables
	// public int waterLevelInt = (int)Globals.gameStage * 40;
	public int waterLevelInt = 0;
    /*
    public IEnumerator lowerWater()
    {
				waterLevelInt++;
				Prop("Back").Animation = "WaterLower" + waterLevelInt;
		        yield return E.Wait((float)1.0);
		        Prop("Back").Animation = "WaterLevel" + waterLevelInt;       
                yield return E.Break; 
    }
    */
    // enums like this are a nice way of keeping track of what's happened in a room
    enum eThingsYouveDone
    {
        Start,
        InsultedChimp,
        EatenSandwich,
        LoadedCrossbow,
        AttackedFlyingNun,
        PhonedAlbatross
    }

    eThingsYouveDone m_thingsDone = eThingsYouveDone.Start;

    public void OnEnterRoom()
    {
		// Put things here that you need to set up BEFORE the room fades in (but nothing "blocking")
		// Note, you can also just do this at the top of OnEnterRoomAfterFade
		
		// sets water level according to the stage of the game
		
		//C.Tony.ChangeRoom(R.Hardware);
		
		
		
		if (C.Tony.Room == R.Home)
		{
			C.Tony.SetPosition(
				new Vector2(Point("HomeDoorPosition")[0] - 100, Point("HomeDoorPosition")[1])
			);
		}
		
		//GuiTestGui test = gameObject.GetComponent("GuiTestGui");
		
		//gameObject.
		
		// C.Dave.WalkToBG(Point("EntryWalk"));
		
		
		// I.BilgePump.Add();
		
		
		
		
		
 }

    public IEnumerator OnEnterRoomAfterFade()
    {
		// Put things here that happen when you enter a room
		
		
		if ((R.Current.FirstTimeVisited) && (Globals.gameStage < gameProgress.UsedBucket)) // Only run this part the first time you visit
		{
		
		
			// PowerQuest.Get.StartCoroutine(ChangeWaterStage(1));
		
		
			Prop("Pump").Disable();
			Prop("Handle").Disable();
			Prop("Hose").Disable();
			C.Dave.SetPosition(Point("StartPosition"));
			C.Dave.Moveable = false;
		
			yield return C.Dave.Say("Oh no! My basement is flooded!", 0);
		
			yield return E.WaitSkip();
		
		
			yield return C.Dave.Say("Good thing I have my trusty bucket!", 41);
		
		
		
			yield return E.WaitSkip();
			yield return C.Display(
				"Your bucket is over on the shelf. Click on it to add it to your inventory.", 30);
		
		
		}
		else
		{
			C.Dave.Position = Point("HomeDoorPosition");
			yield return C.Dave.Face(eFace.Left, true);
		}
		yield return E.Break;
		
 }

    IEnumerator OnLookAtHotspotDoor(IHotspot hotspot)
    {
        yield return C.Dave.Say(" It's a door to the outside.", 2);
        yield return E.Break;
    }

    IEnumerator OnInteractHotspotDoor(IHotspot hotspot)
    {
        if (Globals.gameStage == gameProgress.None)
        {
            yield return C.Dave.Say("Ok Here I go...", 3);
        }
        C.Dave.ChangeRoomBG(R.Map);
        yield return E.Break;
    }

    IEnumerator OnUseInvHotspotDoor(IHotspot hotspot, IInventory item)
    {
        yield return E.Break;
    }

    IEnumerator OnUseInvPropWater(IProp prop, IInventory item)
    {
		// NB: You need to check they used the correct item!
		if (item == I.Bucket)
		{
			Prop("Water").Clickable = false;
			if (Globals.tutorialStage== tutorialProgress.selectedBucket)
			{
				I.Bucket.AnimCursor = "bucketFull";
				I.Bucket.AnimCursorInactive = "bucketFull";
				I.Bucket.AnimGui = "bucketFull";
				// Display: You scoop some water up.
				Globals.tutorialStage= tutorialProgress.usedBucket;
				Globals.gameStage = gameProgress.UsedBucket;
				yield return StageComplete();

		
				I.Active = null;
				yield return E.WaitSkip();
				yield return C.Dave.Say("Oh man... this is going to take forever.", 44);
				yield return C.Dave.Say(
					"Maybe there's something at Doc's hardware store that can help.", 45);
				yield return E.WaitSkip();
				yield return C.Display("Click on a space in the room to walk to it.", 28);
				C.Dave.Moveable = true;
		
				// I.Bucket.SetActive();
			}
			else
			{
				yield return C.Dave.Say(" This bucket ain't gonna cut it...", 6);
			}
		}
		if (item == I.BilgePump)
		{
			Prop("Water").Clickable = false;
			yield return C.Dave.WalkTo(
				new Vector2(Point("PumpPosition")[0], Point("PumpPosition")[1] + 50)
			);
			I.BilgePump.Remove();
			Prop("Pump").Enable();
			Prop("Handle").Enable();
			Prop("Hose").Enable();
			// FaceClicked
		
			/*
			//Display(1): Dave begins to try to pump out the water.
			Prop("Pump").Visible = false;
			Prop("Handle").Visible = false;
			yield return C.Dave.PlayAnimation("Pumping");
			Prop("Pump").Visible = true;
			Prop("Handle").Visible = true;
			Globals.gameStage = gameProgress.TriedPump;
			//lowerWater();
			// C.Dave.WalkTo(0,-400);
			Prop("Back").Animation="WaterLevel2";
			yield return C.Display("Congratulations! The water level has decreased. However, it is not enough...", 2);
			yield return C.Dave.Say("This is too hard! I think the handle is too short and the diameter of the hose is too small, I need to go back to the hardware store.", 4);
			yield return E.Wait(1);
			yield return E.WaitSkip();
			yield return C.Dave.FaceDown();
			*/
		
		}
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropWater(IProp prop)
    {
        yield return C.Dave.Say("That's a lot of water!", 5);
        yield return E.Break;
    }

    IEnumerator OnInteractPropWater(IProp prop)
    {
        // Dave(6):  I can't clean out all of this using only my hands.
        yield return E.Break;
    }

    IEnumerator OnExitRoom(IRoom oldRoom, IRoom newRoom)
    {
        yield return E.Break;
    }

    IEnumerator UpdateBlocking()
    {

		if (
			(Globals.tutorialStage== tutorialProgress.usedBucket)
			&& (C.Player.Position != Point("StartPosition") && !C.Player.Walking)
		)
		{
			Globals.tutorialStage= tutorialProgress.complete;
			Globals.gameStage = gameProgress.UsedBucket;
		
			yield return C.Display(
				"Walk all the way to the right and click the door to leave your basement.", 36);
		}
		
		if (waterLevelInt == 5){
			yield return C.Dave.Say(" Finally! I got that dang water outta my dang basement!", 46);
			yield return C.Dave.Say(" I sure hope that never happens again!", 47);
			C.Dave.ChangeRoom(R.Cutscene);
		}
		
		yield return E.Break;
		
 }

    void Update() 
	{
		//yield return E.Break;
	}


    IEnumerator OnAnyClick()
    {
        yield return E.Break;
    }

    IEnumerator OnWalkTo()
    {
        yield return E.Break;
    }

    IEnumerator OnInteractPropPump(IProp prop)
    {
		string[] pumpAnims = { "PumpingS", "PumpingM", "PumpingL" };
		
		yield return C.Dave.WalkTo(Point("PumpPosition"));
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		
		//Debug.Log((int)currentHandle);
		//Debug.Log(pumpAnims[(int)currentHandle]);
		yield return C.Dave.PlayAnimation(pumpAnims[(int)currentHandle]);
		yield return C.Dave.PlayAnimation(pumpAnims[(int)currentHandle]);
		yield return C.Dave.PlayAnimation(pumpAnims[(int)currentHandle]);
		Prop("Pump").Visible = true;
		Prop("Handle").Visible = true;
		
		if (Globals.gameStage == gameProgress.UsedBucket)
		{
			Globals.gameStage = gameProgress.TriedPump1;		
			yield return StageComplete();

		
			yield return C.Display(
				"Congratulations! The water level has decreased. However, it is not enough...", 2);
			yield return C.Dave.Say(
				"This is too hard! I think the handle is too short and the diameter of the hose is too small, I need to go back to the hardware store.", 4);
			yield return E.Wait(1);
			yield return E.WaitSkip();
			yield return C.Dave.FaceDown();
		}
		
		if (currentHandle == handleType.large && currentHose == hoseType.large)
		{
			Globals.gameStage = gameProgress.RightParts;
			yield return StageComplete();

		
			yield return C.Display(
				"You've chosen the correct parts for the pump and the water level has decreased.", 3);
			yield return C.Dave.Say("Still not enough... I could use some extra hands.", 7);
			// C.Dave.ChangeRoom(R.Cutscene);
		}
		else
		{
			yield return C.Dave.Say(" This isn't any better. I should try different parts.", 8);
		}
		
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropPump(IProp prop)
    {
        yield return C.Display(
            sizeString[(int)currentHandle] + " Handle\n" + sizeString[(int)currentHose] + " Hose"
        );

        yield return E.Break;
    }

    IEnumerator OnUseInvPropPump(IProp prop, IInventory item)
    {
        if (item == I.SmallHandle)
        {
            string prevHandle = sizeString[(int)currentHandle];
            returnHandleToInv();
            currentHandle = handleType.small;
            I.SmallHandle.Remove();
            yield return C.Display(
                prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle"
            );
            Prop("Handle").SetPosition(-310, -81);
        }
        else if (item == I.MediumHandle)
        {
            string prevHandle = sizeString[(int)currentHandle];
            returnHandleToInv();
            currentHandle = handleType.medium;
            I.MediumHandle.Remove();
            yield return C.Display(
                prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle"
            );
            Prop("Handle").SetPosition(-310, -71);
        }
        else if (item == I.LargeHandle)
        {
            string prevHandle = sizeString[(int)currentHandle];
            returnHandleToInv();
            currentHandle = handleType.large;
            I.LargeHandle.Remove();
            yield return C.Display(
                prevHandle + " Handle replaced with " + sizeString[(int)currentHandle] + " Handle"
            );
            Prop("Handle").SetPosition(-310, -61);
        }
        else if (item == I.SmallHose)
        {
            string prevHose = sizeString[(int)currentHose];
            returnHoseToInv();
            currentHose = hoseType.small;
            I.SmallHose.Remove();
            yield return C.Display(
                prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose"
            );
            Prop("Hose").Animation = "HoseS";
        }
        else if (item == I.MediumHose)
        {
            string prevHose = sizeString[(int)currentHose];
            returnHoseToInv();
            currentHose = hoseType.medium;
            I.MediumHose.Remove();
            yield return C.Display(
                prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose"
            );
            Prop("Hose").Animation = "HoseM";
        }
        else if (item == I.LargeHose)
        {
            string prevHose = sizeString[(int)currentHose];
            returnHoseToInv();
            currentHose = hoseType.large;
            I.LargeHose.Remove();
            yield return C.Display(
                prevHose + " Hose replaced with " + sizeString[(int)currentHose] + " Hose"
            );
            Prop("Hose").Animation = "HoseL";
        }
        else { }

        yield return E.Break;
    }

    private void returnHandleToInv()
    {
        switch (currentHandle)
        {
            case handleType.small:
                I.SmallHandle.Add();
                break;
            case handleType.medium:
                I.MediumHandle.Add();
                break;
            case handleType.large:
                I.LargeHandle.Add();
                break;
            default:
                break;
        }
    }

    private void returnHoseToInv()
    {
        switch (currentHose)
        {
            case hoseType.small:
                I.SmallHose.Add();
                break;
            case hoseType.medium:
                I.MediumHose.Add();
                break;
            case hoseType.large:
                I.LargeHose.Add();
                break;
            default:
                break;
        }
    }

    IEnumerator OnInteractCharacterTony(ICharacter character)
    {
		C.Tony.WalkToBG(Point("PumpPosition"));
		
		yield return C.Dave.WalkTo(
			new Vector2(Point("PumpPosition")[0] - 150, Point("PumpPosition")[1])
		);
		yield return C.Dave.Face(eFace.Right);
		
		yield return E.Wait(2);
		
		yield return C.Tony.Say("Here goes nothing!", 0);
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		Prop("Pump").Visible = true;
		Prop("Handle").Visible = true;
		
		Globals.gameStage = gameProgress.TonyPumped;
		yield return StageComplete();
		
		yield return C.Display(" The recruited muscle has helped bring the water level down.", 4);
		
		yield return C.Tony.Say(" Phew... I'm wiped out. Got any grub?", 4);
		
		yield return E.Break;
		
 }

    IEnumerator OnInteractCharacterNeighbor2(ICharacter character)
    {
        Camera.SetCharacterToFollow(C.Neighbor2, 200);

        C.Neighbor2.WalkToBG(Point("WindowPosition"));
        yield return C.Dave.WalkTo(Point("PumpPosition"));

        yield return E.WaitUntil(() => C.Neighbor2.Position == Point("WindowPosition"));

        yield return C.Display(
            "Jim helps get some more water out by scooping it out the window with his bucket. Equivalent to using a diuretic.", 5);

        Globals.gameStage = gameProgress.TonyPumped;
        // lowerWater();

        yield return E.Wait(2);
        yield return E.FadeOut();
        yield return C.Display(" 30 minutes later...", 6);
        yield return E.FadeIn();

        yield return C.Neighbor2.Say("Phew, I'm exhausted", 0);

        yield return C.Dave.Say(" Me too. We could use some extra muscle.", 9);

        Camera.SetCharacterToFollow(C.Dave, 200);

        yield return E.Break;
    }

    void OnPostRestore(int version) { }

    IEnumerator OnInteractPropBucket(IProp prop)
    {
		I.Bucket.Add();
		Prop("Bucket").Disable();
		yield return C.Display("Bucket added to  your toolbox.", 34);
		
		if (Globals.tutorialStage== tutorialProgress.start)
		{
			Globals.tutorialStage= tutorialProgress.clickedBucket;
			yield return E.WaitSkip();
			yield return C.Dave.Say(" There it is! Now I can scoop up some of this water.", 42);
			yield return E.WaitSkip();
			yield return C.Display(" Click on the bucket icon in your toolbox to select it.", 32);
		}
		
		yield return E.Break;
		
 }

    IEnumerator OnLookAtPropBucket(IProp prop)
    {
        yield return C.Dave.Say(" There's my bucket! I can use that to scoop out the water.", 43);
        yield return E.Break;
    }

    IEnumerator OnUseInvHotspotWindow(IHotspot hotspot, IInventory item)
    {
        if (item == I.Bucket)
        {
            I.Active = null;
            if (Globals.tutorialStage== tutorialProgress.usedBucket)
            {
                I.Bucket.AnimCursor = "bucket";
                I.Bucket.AnimCursorInactive = "bucket";
                I.Bucket.AnimGui = "bucket";

                Globals.tutorialStage= tutorialProgress.complete;

                // ...
                // Display(35):  You use the bucket to scoop some water out of the window.
                yield return E.WaitSkip();
                yield return C.Dave.Say("Oh man... this is going to take forever.", 44);
                yield return C.Dave.Say(
                    "Maybe there's something at Doc's hardware store that can help.", 45);
                yield return E.WaitSkip();
                yield return C.Display("Click on a space in the room to walk to it.", 37);
                C.Dave.Moveable = true;
            }
            else
            {
                yield return C.Dave.Say(" This bucket aint going to cut it...", 30);
            }
        }
        yield return E.Break;
    }

    IEnumerator OnUseInvPropBucket(IProp prop, IInventory item)
    {
        yield return E.Break;
    }

    IEnumerator OnInteractHotspotWindow(IHotspot hotspot)
    {
		yield return E.Break;
		
		Settings.LanguageId = (Settings.LanguageId + 1) % Settings.GetLanguages().Length;
 }

    IEnumerator OnUseInvPropWaterFront(IProp prop, IInventory item)
    {
        yield return E.Break;
    }

	IEnumerator OnLookAtHotspotWindow( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It’s a window. Yup.", 48);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBleach( IHotspot hotspot )
	{
		yield return C.Dave.Say("It’s bleach, fer cleanin’ yer clothes.", 49);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotSprayPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("It’s a can of Mach brand orange spray paint.", 50);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotSprayPaint( IHotspot hotspot )
	{
		yield return C.Dave.Say("It’s a can of Mach brand orange spray paint.", 51);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotWashingMachine( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It’s my trusty old washing machine. Although at this point I probably could just throw some detergent in the water and make my whole basement the washing machine.", 52);
		
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotBoiler( IHotspot hotspot )
	{
		yield return C.Dave.Say("This clunker of a boiler is from the 1940’s. It works, but it makes the worst darn noises you’ve ever dun heard.", 53);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotTV( IHotspot hotspot )
	{
		yield return C.Dave.Say("Haven’t used this in a while. For all you kids out there, this is what TV’s looked like in the stone age.", 54);
		yield return E.Break;
	}

	IEnumerator OnLookAtHotspotCouch( IHotspot hotspot )
	{
		yield return C.Dave.Say(" I can’t believe I thought this looked good.", 55);
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBleach( IHotspot hotspot )
	{
		yield return C.Dave.Say("It’s bleach, fer cleanin’ yer clothes.", 56);
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotWashingMachine( IHotspot hotspot )
	{
		yield return C.Dave.Say(" It’s my trusty old washing machine. Although at this point I probably could just throw some detergent in the water and make my whole basement the washing machine.", 57);
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotBoiler( IHotspot hotspot )
	{
		yield return C.Dave.Say("This clunker of a boiler is from the 1940’s. It works, but it makes the worst darn noises you’ve ever dun heard.", 58);
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotTV( IHotspot hotspot )
	{
		yield return C.Dave.Say("Haven’t used this in a while. For all you kids out there, this is what TV’s looked like in the stone age.", 59);
		
		yield return E.Break;
	}

	IEnumerator OnInteractHotspotCouch( IHotspot hotspot )
	{
		yield return C.Dave.Say(" I can’t believe I thought this looked good.", 60);
		
		yield return E.Break;
	}

	IEnumerator OnUseInvCharacterTony( ICharacter character, IInventory item )
	{
		if (item == I.Beer) {
		yield return C.Tony.Say("Pizza AND beer?!", 5);
		yield return C.Tony.Say("Just what I needed!", 6);
		Prop("Pump").Visible = false;
		Prop("Handle").Visible = false;
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		yield return C.Tony.PlayAnimation("Pumping");
		Prop("Pump").Visible = true;
		Prop("Handle").Visible = true;
		
		Globals.gameStage = gameProgress.TonyAte;
		yield return StageComplete();
		
		
		}
		yield return E.Break;
	}

	IEnumerator OnLookAtPropWaterFront( IProp prop )
	{

		yield return E.Break;
	}

	IEnumerator OnInteractPropWaterFront( IProp prop )
	{

		yield return E.Break;
	}

	public void LowerWaterShader(int spriteIndex)
	{
		GameObject dave = GameObject.Find("CharacterDave");
		Renderer renderer = dave.GetComponent<Renderer>();
		Material uniqueMaterial = renderer.material;
		double baseLevel = 0.5;
		int totalFrames = 20;
		double increment = baseLevel / totalFrames;
		int numIncrements = totalFrames - spriteIndex;
		uniqueMaterial.SetFloat("_Level", (float)(baseLevel + numIncrements*increment) );

	}

	public void LowerWater(int spriteIndex)
	{
		GameObject water = GameObject.Find("Water");
		SpriteRenderer spriteRenderer = water.GetComponent<SpriteRenderer>();
		Sprite waterSprite = Resources.Load<Sprite>("Water"+spriteIndex);
		spriteRenderer.sprite = waterSprite;
		Debug.Log("howdy");	
	
	}

	

	public  IEnumerator ChangeWaterStage(int stageNum)
	{
		Debug.Log("changewaterstage start");	

		int totalFrames = 20;
		int framesPerStage = 4;
		int startingIndex = totalFrames - framesPerStage*(stageNum-1);
		int endingIndex = startingIndex - framesPerStage;
		Debug.Log("Start index: "+ startingIndex + " End Index: " + endingIndex);
		for(int i = startingIndex; i >= endingIndex; i--){
			Debug.Log("begin loop iteration "+ i);
			LowerWater(i);
			LowerWaterShader(i);
			yield return new WaitForSeconds((float)0.5);
			Debug.Log(" end loop iteration "+ i);	
		}
        //Print the time of when the function is first called.
        //Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.

        //After we have waited 5 seconds print the time again.
        //Debug.Log("Finished Coroutine at timestamp : " + Time.time);
		yield return E.Break;
	}

	public IEnumerator StageComplete()
	{
		yield return ChangeWaterStage((int)Globals.gameStage);
		yield return E.Break;
	}
}
