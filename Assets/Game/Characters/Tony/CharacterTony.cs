using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterTony : CharacterScript<CharacterTony>
{


	IEnumerator OnUseInv( IInventory item )
	{

		yield return E.Break;
	}

	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say(" It's my neighbor Bob. Maybe he can help.");
		
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{
		if ( R.Current == R.Hardware) {
			Vector2 speakPosition = C.Tony.Position;
			speakPosition[0] = (C.Tony.Position[0] - 100);
			yield return C.Dave.WalkTo(speakPosition);
			D.GetHelpBob.Start();
		}
		
		yield return E.Break;
	}
}