using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogDialogWithClerk : DialogTreeScript<DialogDialogWithClerk>
{
	public IEnumerator OnStart()
	{
		//Globals.gameStage = gameProgress.TriedPump1;
		
		
		
		if (Globals.gameStage <= gameProgress.UsedBucket) {
			D.DialogWithClerk.OptionOff(2);
			D.DialogWithClerk.OptionOn(1);
		 //   D.DialogWithClerk.GetOption(1).Start();
		} else {
			D.DialogWithClerk.OptionOff(1);
			D.DialogWithClerk.OptionOn(2);
		}
		
		
		
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
		
				yield return E.WaitSkip();
		
		
				yield return C.HardwareClerk.Say("Hello valued customer, what can I do for you?", 1);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("My basement's flooded. Got anything to help with that'", 18);
		
				yield return E.WaitSkip();
		
				yield return C.HardwareClerk.Say("We've got a special Pumps 4 Chumps promotion going on! It'll be sure to unflood your flood, no money back guarantee!", 2);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("I hate everything you just said, but I guess I'll take it.", 19);
		
				I.BilgePump.Add();
		
				yield return C.Display("Pump added to you toolbox.", 12);
		
				Stop();
		
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
				yield return E.WaitSkip();
				yield return C.Dave.Say("Cut the crap, carpetbagger! That pump didn't do squat!", 20);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("I'm so sorry to hear that! I recommend upgrading to a higher end model handle and hose. We're running a 25% off special on all pump accessories!", 3);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("We have handles and hoses in stock.", 4);
				Stop();	
				D.BuyOptions.Start();
				
		
				
				
		yield return E.Break;
	}
}