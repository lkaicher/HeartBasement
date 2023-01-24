using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryRepairKit : InventoryScript<InventoryRepairKit>
{


	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		C.Dave.AddInventory(I.Wrench);
		C.Dave.AddInventory(I.Washer);
		C.Dave.RemoveInventory(I.RepairKit);
		yield return C.Dave.Say(" Oh boy!");
		yield return C.Dave.Say(" A washer and a wrench!");
		yield return E.Break;
	}
}