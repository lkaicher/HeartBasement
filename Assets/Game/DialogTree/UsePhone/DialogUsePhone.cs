using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogUsePhone : DialogTreeScript<DialogUsePhone>
{
	public IEnumerator OnStart()
	{
		if (Globals.m_progressExample == eProgress.None) {
			D.UsePhone.OptionOff(1);
			D.UsePhone.OptionOff(2);
		} else {
			D.UsePhone.OptionOn(1);
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
		yield return C.Dave.Say("Hi Jim. My basement is flooded, can you come over and help me out?", 27);
		yield return E.WaitSkip();
		yield return C.Display("That doesn't sound good. I'll bring my bucket.", 17);
		yield return E.WaitSkip();
		C.Neighbor2.Enable();
		C.Neighbor2.ChangeRoom(R.Home);
		C.Neighbor2.SetPosition(Point("HomeDoorPosition"));
		
		Camera.SetCharacterToFollow(C.Neighbor2, 200);
		yield return E.Wait(1);
		
		yield return C.Neighbor2.Say("Hi Dave, I'm here to he-", 1);
		yield return E.WaitSkip();
		yield return C.Neighbor2.Say("...", 2);
		yield return E.WaitSkip();
		yield return C.Neighbor2.Say("Oh boy.", 3);
		
		Stop();
		Camera.SetCharacterToFollow(C.Dave, 200);
		D.UsePhone.OptionOffForever(1);
		
		yield return E.Break;
	}

	IEnumerator Option3( IDialogOption option )
	{
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
		Stop();
		yield return E.Break;
	}
}