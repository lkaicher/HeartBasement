using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogUsePhone : DialogTreeScript<DialogUsePhone>
{
	public IEnumerator OnStart()
	{
		D.UsePhone.OptionOff(1);
		D.UsePhone.OptionOff(2);
		
		if (Globals.m_progressExample >= eProgress.TriedPump1){
			D.UsePhone.OptionOn(1);
		
		}
		if (Globals.m_progressExample == eProgress.Friend1) {
			D.UsePhone.OptionOn(2);
			}
		
		
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
		yield return C.Dave.Say("Hey Tony. My basement is flooded, I've got this pump but I need more muscle!", 27);
		yield return E.WaitSkip();
		yield return C.Display("Again?! Alright, I'll be right over", 17);
		yield return E.WaitSkip();
		C.Tony.Enable();
		C.Tony.ChangeRoom(R.Home);
		C.Tony.SetPosition(Point("HomeDoorPosition"));
		
		Camera.SetCharacterToFollow(C.Neighbor2, 200);
		yield return E.Wait(1);
		
		yield return C.Tony.Say("Ay Dave, I'm here to he-", 1);
		yield return E.WaitSkip();
		yield return C.Tony.Say("...", 2);
		yield return E.WaitSkip();
		yield return C.Tony.Say("Oh boy.", 3);
		
		Stop();
		Camera.SetCharacterToFollow(C.Dave, 200);
		D.UsePhone.OptionOffForever(1);
		
		yield return E.Break;
	}

	IEnumerator Option3( IDialogOption option )
	{
		
		
		switch (Globals.m_progressExample) {
			case eProgress.None:
				switch (Globals.tutorialProgress) {
					case tutorialStage.start:
						yield return C.Display("The bucket is located on the shelf above the laundry machine. Click on it to add it to your toolbox.");
						break;
					case tutorialStage.clickedBucket:
						yield return C.Display("Your toolbox is located at the bottom left. Move the mouse down to the lower left, and the toolbox will slide up. Then, click on the bucket icon to select it.");
						break;
					case tutorialStage.selectedBucket:
						yield return C.Display("With the bucket selected, use it on the water by clicking anywhere on the water.");
						break;
					case tutorialStage.usedBucket:
						yield return C.Display("To move around the room, click the spot you would like to walk to.");
						break;
					case tutorialStage.complete:
						yield return C.Display("Now the fun begins!");
						break;
					default:
						yield return C.Display("Try scooping up some of the water with a bucket.");
						break;
					}
				break;
			case eProgress.UsedBucket:
				// player has acquired the pump, but not yet placed it in the basement
				if (I.BilgePump.Owned) {
					yield return C.Display(" Now that you have a pump, you can select it from your toolbox and use it on the water in your basement.");
					if (C.Dave.Room == R.Hardware) {
						yield return C.Display(" You should go back home. Walk all the way to the left and click on the door.");
					}
				}
				// player has placed the pump in the basement, but not yet used it.
				else if (Prop("Pump").Visible) {
					yield return C.Display("Now that you've placed the pump, time to try it out! Click on the pump to give it a go.");
				}
				// player has not yet acquired the pump
				else {
					yield return C.Display("I hear Doc over at the hardware store is running a big sale on pumps. I bet one of those would help you.");
					if (C.Dave.Room == R.Home) {
						yield return C.Display(" You can leave your basement by clicking on the door, located all the way to the right. If you cannot see it, keep walking to the right.");
						}
				}
				break;
			// #TODO flesh out remaining hint scenarios
			case eProgress.TriedPump1:
				// player has acquired replacement parts
				// player has replaced parts, but they are not the correct parts
				// player has not yet acquired replacement parts
				yield return C.Display("You should go back to Doc at his hardware store. Maybe he has something else that could help.");
				break;
			case eProgress.RightParts:
				// player has called Tony and he has arrived, but not yet pumped.
				// player has not called Tony
				yield return C.Display("You could use some extra muscle. Try calling a friend, maybe they could help out.");
				break;
			case eProgress.Friend1:
				// player has ordered pizza, but not yet given it to tony
				// player has not yet ordered pizza
				yield return C.Display("Tony is tired, he needs something to boost his energy. That pizza place delivers, try giving them a call.");
				break;
			default: 
				yield return C.Display("Thank for for calling the Hint Hotline. Our lines our busy at the moment, please call back later.");
				break;
		}
		
		//C.Display(hintString[(int)Globals.m_progressExample]);
		Stop();
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
		yield return C.Dave.Say("Hi, I'd like to order a large cheese and pepporini pizza for delivery", 28);
		yield return C.Display(" Okay, what is the address?", 18);
		yield return C.Dave.Say(" 22 Hart Street", 29);
		yield return C.Display(" Your order will be there in 30 minutes.", 19);
		D.UsePhone.OptionOffForever(2);
		yield return C.Display("Pizza has been added to your inventory");
		C.Dave.AddInventory(I.Beer);
		Stop();
		yield return E.Break;
	}

	IEnumerator Option4( IDialogOption option )
	{
		Stop();
		
		yield return E.Break;
	}
}