using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Reflection;
using PowerTools;

//
// PowerQuest Partial Class: Toss stuff in here while working on it lol
//

namespace PowerTools.Quest
{


public partial interface IPowerQuest
{
	// Obsolete misspelled 'Occurance' functions
	[System.Obsolete("Replaced with correctly spelled FirstOccurrence()")]
	bool FirstOccurance(string uniqueString);
	[System.Obsolete("Replaced with correctly spelled FirstOccurrence()")]
	int GetOccuranceCount(string thing);
	[System.Obsolete("Replaced with correctly spelled Occurrence()")]
	int Occurrance(string thing);
}

public partial class PowerQuest
{
	
	// Obsolete misspelled 'Occurance' functions
	[System.Obsolete("Replaced with correctly spelled FirstOccurrence()")]
	public bool FirstOccurance(string uniqueString) { return FirstOccurrence(uniqueString); }
	[System.Obsolete("Replaced with correctly spelled FirstOccurrence()")]
	public int GetOccuranceCount(string thing) { return GetOccurrenceCount(thing); }
	[System.Obsolete("Replaced with correctly spelled Occurrence()")]
	public int Occurrance(string thing) { return Occurrence(thing); }

}

}
