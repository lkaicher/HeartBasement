using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class InventoryCellPhone : InventoryScript<InventoryCellPhone>
{


	IEnumerator OnInteractInventory( IInventory thisItem )
	{
		// if (Globals.m_progressExample != eProgress.None) {
		//	D.UsePhone.Start();
		// }
		
		
		yield return E.Break;
	}

	IEnumerator OnLookAtInventory( IInventory thisItem )
	{
		if  ( (Globals.m_progressExample == eProgress.TriedPump1) && (!D.UsePhone.GetOptionUsed(1)) ) {
			yield return C.Dave.Say(" Maybe I could call my friend Jim to come help out. ");
		} else {
			yield return C.Dave.Say(" Thats my cell phone. ");
		}
		
		D.UsePhone.Start();
		
		yield return E.Break;
	}
}