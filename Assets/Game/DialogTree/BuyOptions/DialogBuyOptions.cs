using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogBuyOptions : DialogTreeScript<DialogBuyOptions>
{
	public IEnumerator OnStart()
	{
		if (I.MediumHandle.Owned) {
			D.BuyOptions.OptionOff(1);
		}
		if (I.LargeHandle.Owned) {
			D.BuyOptions.OptionOff(2);
		}
		if (I.MediumHose.Owned) {
			D.BuyOptions.OptionOff(3);
		}
		if (I.LargeHose.Owned) {
			D.BuyOptions.OptionOff(4);
		}
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
			yield return C.Dave.Say("I'll take a medium handle.");
			yield return E.WaitSkip();
			I.MediumHandle.Add();
			yield return C.Display("Medium Handle added to  your inventory.");
			yield return E.WaitSkip();
			yield return C.HardwareClerk.Say("Anything else?");
				
			Stop();
			D.BuyOptions.Start();
		
		
		yield return E.Break;
	}

	IEnumerator Option2( IDialogOption option )
	{
			yield return C.Dave.Say("I'll take a large handle.");
			yield return E.WaitSkip();
			I.LargeHandle.Add();
			yield return C.Display("Large Handle added to  your inventory.");
			yield return E.WaitSkip();
			yield return C.HardwareClerk.Say("Anything else?");
				
			Stop();
			D.BuyOptions.Start();
		
		yield return E.Break;
	}

	IEnumerator Option5( IDialogOption option )
	{
		Stop();
		
		yield return E.Break;
	}

	IEnumerator Option3( IDialogOption option )
	{
			yield return C.Dave.Say("I'll take a medium diameter hose.");
			yield return E.WaitSkip();
			I.MediumHose.Add();
			yield return C.Display("Medium Hose added to  your inventory.");
			yield return E.WaitSkip();
			yield return C.HardwareClerk.Say("Anything else?");
				
			Stop();
			D.BuyOptions.Start();
		
		
		yield return E.Break;
	}

	IEnumerator Option4( IDialogOption option )
	{
			yield return C.Dave.Say("I'll take a large diameter hose.");
			yield return E.WaitSkip();
			I.LargeHose.Add();
			yield return C.Display("Large Hose added to  your inventory.");
			yield return E.WaitSkip();
			yield return C.HardwareClerk.Say("Anything else?");
				
			Stop();
			D.BuyOptions.Start();
		
		
		yield return E.Break;
	}
}