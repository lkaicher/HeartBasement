using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryBucket : InventoryScript<InventoryBucket>
{


	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		if (Globals.tutorialProgress == tutorialStage.clickedBucket)
		{
			Globals.tutorialProgress = tutorialStage.selectedBucket;
		
			yield return C.Display(" Click on the water with the bucket to scoop it up, then click on the window to dump it out.", 33);
		
			Prop("Water").Clickable = true;
			I.Bucket.SetActive();
		
		
		}
		yield return E.Break;
	}
}