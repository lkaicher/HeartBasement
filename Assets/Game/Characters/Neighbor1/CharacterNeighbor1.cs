using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterNeighbor1 : CharacterScript<CharacterNeighbor1>
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
}