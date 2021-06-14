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
}