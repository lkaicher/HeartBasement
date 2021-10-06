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
				yield return C.HardwareClerk.Face(C.Dave);
				 yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("HSO: Sure, I have this bilge pump (a hand operated pump).", 17);
				 yield return E.WaitSkip();
				 yield return C.Dave.Say("Is that the only one you have?", 39);
				yield return E.WaitSkip();
				yield return C.HardwareClerk.Say("Yes", 18);
				yield return E.WaitSkip();
				yield return C.Dave.Say("OK, I will take it.", 40);
				I.BilgePump.Add();
				yield return C.Display("You now have a pump in your inventory, to us it grab it from your inventory (upper right)", 28);
				Stop();
		yield return E.Break;
	}
}