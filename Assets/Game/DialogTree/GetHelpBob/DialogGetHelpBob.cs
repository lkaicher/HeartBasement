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
		yield return C.Tony.Face(C.Dave);
		
		yield return C.Tony.Say("Uhh... Now?", 1);
		yield return E.WaitSkip();
		yield return C.Dave.Say(" Yeah...", 25);
		yield return E.WaitSkip();
		yield return E.WaitSkip();
		yield return C.Tony.Say("I dunno man...", 2);
		
		if (I.Beer.Owned) {
		yield return C.Dave.Say(" I have beer", 26);
		yield return E.WaitSkip();
		yield return C.Tony.Say("Alright, I'll see what I can do. ", 3);
		
		Stop();
		
		C.Tony.WalkToBG(Point("HWDoorPosition"));
		yield return C.Dave.WalkTo(Point("HWDoorPosition"));
		C.Dave.ChangeRoom(R.Home);
		C.Tony.ChangeRoom(R.Home);
		C.Tony.SetPosition(Point("HomeDoorPosition"));
		//C.Tony.WalkToBG(Point("PumpPosition"));
		
		//Vector2 davePosition = C.Tony.Position;
		//davePosition[0] = (Point("PumpPosition")[0] - 150);
		//C.Dave.WalkTo(Point("davePosition"));
		
		//Globals.m_progressExample = eProgress.Friend1;
		 
		//C.Tony.SetPosition(Point("PumpPosition"));
		} else {
		Stop();
		}
		// C.Dave.ChangeRoom(R.Home);
		
		
		
		yield return E.Break;
	}
}