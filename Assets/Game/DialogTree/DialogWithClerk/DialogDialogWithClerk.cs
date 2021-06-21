using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogDialogWithClerk : DialogTreeScript<DialogDialogWithClerk>
{
	public IEnumerator OnStart()
	{
		// Globals.m_progressExample = eProgress.TriedPump1;
		
		
		if (Globals.m_progressExample == eProgress.None) {
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
				
				
				yield return C.HardwareClerk.Say("Sure, I have this bilge pump (a hand operated pump).");
				 
				yield return E.WaitSkip();
				 
				yield return C.Dave.Say("Is that the only one you have?");
				
				yield return E.WaitSkip();
				
				yield return C.HardwareClerk.Say("Yes");
				
				yield return E.WaitSkip();
				
				yield return C.Dave.Say("OK, I will take it.");
				
				I.BilgePump.Add();
				
				yield return C.Display("You now have a pump in your inventory, to use it grab it from your inventory (upper right)");
				
				Stop();
		
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
				yield return C.HardwareClerk.Face(C.Dave);
				yield return E.WaitSkip();
				yield return C.Dave.Say(" I am having a hard time pumping the water out of my basement.");
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("You may want to consider some options for the pump.");
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("We have handles and hoses in stock.");
				Stop();	
				D.BuyOptions.Start();
				
		
				
				
		yield return E.Break;
	}
}