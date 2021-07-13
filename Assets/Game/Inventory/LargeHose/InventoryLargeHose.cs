using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryLargeHose : InventoryScript<InventoryLargeHose>
{

	public bool isHose = true;
	IEnumerator OnInteractInventory( IInventory thisItem )
	{

		yield return E.Break;
	}
}