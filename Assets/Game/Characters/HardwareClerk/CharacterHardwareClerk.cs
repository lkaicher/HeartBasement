using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterHardwareClerk : CharacterScript<CharacterHardwareClerk>
{
	 
	
	
	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say("There's the clerk. He looks bored.", 14);
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{
		 yield return C.Dave.WalkTo(Point("HWCounterPosition"));
		 
		
		
		
		 if (D.DialogWithClerk.GetOption(2).Used) {
			 D.BuyOptions.Start();
		 } else if (D.DialogWithClerk.GetOption(1).Used && Globals.m_progressExample == eProgress.None) {
			 yield return C.HardwareClerk.Say("Good luck.	 ", 0);
		 } else {
			 D.DialogWithClerk.Start();
		 } 
		 
		
		//D.DialogWithClerk.Start();
		yield return E.Break;
	}

	IEnumerator OnUseInv( IInventory item )
	{

		yield return E.Break;
	}
}