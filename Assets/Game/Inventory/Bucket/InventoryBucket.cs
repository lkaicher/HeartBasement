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
		
			yield return C.Display(" Click on the window with the bucket to scoop water out of the basement.", 33);
		
			I.Bucket.SetActive();
		}
		yield return E.Break;
	}
}