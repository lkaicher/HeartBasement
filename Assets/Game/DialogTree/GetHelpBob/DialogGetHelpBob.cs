using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogGetHelpBob : DialogTreeScript<DialogGetHelpBob>
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
		yield return C.Neighbor1.Face(C.Dave);
		
		yield return C.Neighbor1.Say("Uhh... Now?");
		yield return E.WaitSkip();
		yield return C.Dave.Say(" Yeah...");
		yield return E.WaitSkip();
		yield return E.WaitSkip();
		
		yield return C.Neighbor1.Say("Alright, I'll see what I can do. ");
		
		Stop();
		
		yield return C.Dave.WalkTo(Point("HWDoorPosition"));
		yield return C.Neighbor1.WalkTo(Point("HWDoorPosition"));
		
		C.Neighbor1.ChangeRoom(R.Home);
		C.Dave.ChangeRoom(R.Home);
		
		
		
		yield return E.Break;
	}
}