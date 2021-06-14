using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;

namespace PowerScript
{	
	// Shortcut access to SystemAudio.Get
	public class Audio : SystemAudio
	{
	}

	public static partial class C
	{
		// Access to specific characters (Auto-generated)
		public static ICharacter Dave		{ get{return PowerQuest.Get.GetCharacter("Dave"); } }
		public static ICharacter HardwareClerk		{ get{return PowerQuest.Get.GetCharacter("HardwareClerk"); } }
		public static ICharacter Neighbor1		{ get{return PowerQuest.Get.GetCharacter("Neighbor1"); } }
		public static ICharacter Neighbor2		{ get{return PowerQuest.Get.GetCharacter("Neighbor2"); } }
		public static ICharacter r		{ get{return PowerQuest.Get.GetCharacter("r"); } }
		// #CHARS# - Do not edit this line, it's used by the system to insert characters
	}

	public static partial class I
	{		
		// Access to specific Inventory (Auto-generated)
		public static IInventory BilgePunp		{ get{return PowerQuest.Get.GetInventory("BilgePunp"); } }
		// #INVENTORY# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	public static partial class G
	{
		// Access to specific gui (Auto-generated)
		public static IGui DisplayBox		{ get{return PowerQuest.Get.GetGui("DisplayBox"); } }
		public static IGui InfoBar		{ get{return PowerQuest.Get.GetGui("InfoBar"); } }
		public static IGui Toolbar		{ get{return PowerQuest.Get.GetGui("Toolbar"); } }
		public static IGui Inventory		{ get{return PowerQuest.Get.GetGui("Inventory"); } }
		public static IGui DialogTree		{ get{return PowerQuest.Get.GetGui("DialogTree"); } }
		public static IGui SpeechBox		{ get{return PowerQuest.Get.GetGui("SpeechBox"); } }
		// #GUI# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	public static partial class R
	{
		// Access to specific room (Auto-generated)
		public static IRoom Home		{ get{return PowerQuest.Get.GetRoom("Home"); } }
		public static IRoom Map		{ get{return PowerQuest.Get.GetRoom("Map"); } }
		public static IRoom Hardware		{ get{return PowerQuest.Get.GetRoom("Hardware"); } }
		// #ROOM# - Do not edit this line, it's used by the system to insert rooms for easy access
	}

	// Dialog
	public static partial class D
	{
		// Access to specific dialog trees (Auto-generated)

		public static IDialogTree DialogWithClerk		{ get{return PowerQuest.Get.GetDialogTree("DialogWithClerk"); } }
		// #DIALOG# - Do not edit this line, it's used by the system to insert rooms for easy access	    	    
	}


}
