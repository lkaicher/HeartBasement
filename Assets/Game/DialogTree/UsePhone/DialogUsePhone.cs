using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogUsePhone : DialogTreeScript<DialogUsePhone>
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
		yield return C.Dave.Say("Hi Jim. My basement is flooded, can you come over and help me pump it out?");
		yield return E.WaitSkip();
		yield return C.Display("That doesn't sound good. I'll be right over.");
		yield return E.WaitSkip();
		C.Neighbor2.Visible=true;
		C.Neighbor2.Clickable=true;
		C.Neighbor2.ChangeRoom(R.Home);
		C.Neighbor2.SetPosition(Point("HomeDoorPosition"));
		
		yield return C.Neighbor2.Say("Hi Dave, I'm here to he-");
		yield return E.WaitSkip();
		yield return C.Neighbor2.Say("...");
		yield return E.WaitSkip();
		yield return C.Neighbor2.Say("Oh boy.");
		
		Stop();
		
		D.UsePhone.OptionOff(1);
		
		yield return E.Break;
	}
}