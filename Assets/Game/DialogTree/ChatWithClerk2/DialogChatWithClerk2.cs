using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogChatWithClerk2 : DialogTreeScript<DialogChatWithClerk2>
{
public IEnumerator OnStart()
	{
		yield return E.ConsumeEvent;
	}

	public IEnumerator OnStop()
	{
		yield return E.Break;
	}

	IEnumerator Option1( IDialogOption option )
	{
				yield return C.Dave.Say("I would like to buy a pump");
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Face(C.Dave);
				 yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("HSO: Sure, I have this bilge pump (a hand operated pump).");
				 yield return E.WaitSkip();
				 yield return C.Dave.Say("Is that the only one you have?");
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("Yes");
				yield return E.WaitSkip();
				yield return C.Dave.Say("OK, I will take it.");
				I.BilgePunp.AddAsActive();
				Stop();
		yield return E.Break;
	}
}