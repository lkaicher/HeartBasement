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
		
		
				yield return C.HardwareClerk.Say("Sure, I have this bilge pump. It's hand operated.", 1);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("Is that the only one you have?", 18);
		
				yield return E.WaitSkip();
		
				yield return C.HardwareClerk.Say("We’ve got a special Pumps 4 Chumps promotion going on! It’ll be sure to unflood your flood, no money back guarantee!", 2);
		
				yield return E.WaitSkip();
		
				yield return C.Dave.Say("OK, I will take it.", 19);
		
				I.BilgePump.Add();
		
				yield return C.Display("Pump added to inventory", 12);
		
				Stop();
		
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
				yield return E.WaitSkip();
				yield return C.Dave.Say(" I am having a hard time pumping the water out of my basement.", 20);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("You may want to consider some options for the pump.", 3);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("We have handles and hoses in stock.", 4);
				Stop();	
				D.BuyOptions.Start();
				
		
				
				
		yield return E.Break;
	}
}