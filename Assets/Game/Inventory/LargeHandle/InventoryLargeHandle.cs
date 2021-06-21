using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryLargeHandle : InventoryScript<InventoryLargeHandle>
{


	IEnumerator OnLookAtInventory( IInventory thisItem )
	{

		yield return E.Break;
	}
}