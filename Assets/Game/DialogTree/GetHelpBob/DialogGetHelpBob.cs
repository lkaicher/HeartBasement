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
		yield return C.Neighbor1.Say("I dunno man...");
		
		if (I.Beer.Owned) {
		yield return C.Dave.Say(" I have beer");
		yield return E.WaitSkip();
		yield return C.Neighbor1.Say("Alright, I'll see what I can do. ");
		
		Stop();
		
		C.Neighbor1.WalkToBG(Point("HWDoorPosition"));
		yield return C.Dave.WalkTo(Point("HWDoorPosition"));
		C.Dave.ChangeRoom(R.Home);
		C.Neighbor1.ChangeRoom(R.Home);
		C.Neighbor1.SetPosition(Point("HomeDoorPosition"));
		//C.Neighbor1.WalkToBG(Point("PumpPosition"));
		
		//Vector2 davePosition = C.Neighbor1.Position;
		//davePosition[0] = (Point("PumpPosition")[0] - 150);
		//C.Dave.WalkTo(Point("davePosition"));
		
		//Globals.m_progressExample = eProgress.Friend1;
		 
		//C.Neighbor1.SetPosition(Point("PumpPosition"));
		} else {
		Stop();
		}
		// C.Dave.ChangeRoom(R.Home);
		
		
		
		yield return E.Break;
	}
}