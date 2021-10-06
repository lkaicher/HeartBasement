using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class CharacterNeighbor2 : CharacterScript<CharacterNeighbor2>
{


	IEnumerator OnLookAt()
	{
		yield return C.Dave.Say(" That's my friend Jim. ", 15);
		
		yield return E.Break;
	}

	IEnumerator OnInteract()
	{

		yield return E.Break;
	}
}