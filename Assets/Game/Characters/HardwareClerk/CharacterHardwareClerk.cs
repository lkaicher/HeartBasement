using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterHardwareClerk : CharacterScript<CharacterHardwareClerk>
{
	 
	
	
	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say("It's the Hardware Clerk");
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{
		D.ChatWithClerk2.Start();
		yield return E.Break;
	}

	IEnumerator OnUseInv( IInventory item )
	{

		yield return E.Break;
	}
}