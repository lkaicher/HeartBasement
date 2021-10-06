using UnityEngine;
using System.Collections;
using PowerTools.Quest;
using PowerScript;
using static GlobalScript;

public class DialogChatWithBarney : DialogTreeScript<DialogChatWithBarney>
{
	public IEnumerator Start()
	{
		yield return C.WalkToClicked();
		C.FaceClicked();
		C.HardwareClerk.Face(C.Dave);
		yield return C.HardwareClerk.Say("What is it?", 9);
		yield return E.Break;
		
	}

	public IEnumerator Option1( IDialogOption option )
	{
		yield return C.Dave.Say("I would like to buy a pump", 30);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Face(C.Dave);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("HSO: Sure, I have this bilge pump (a hand operated pump).", 10);
		yield return E.WaitSkip();
		yield return C.Dave.Say("Is that the only one you have?", 31);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("Yes", 11);
		yield return E.WaitSkip();
		yield return C.Dave.Say("OK, I will take it.", 32);
		
		Audio.Play("Bucket");
		//I.BilgePump.Disable();
		I.BilgePump.AddAsActive();
		yield return E.WaitSkip();
	}


	public IEnumerator Option2( IDialogOption option )
	{
		yield return C.Dave.Say("What's with this forest anyway?", 33);
		yield return C.HardwareClerk.Say("Whaddaya mean?", 12);
		
		// Here we start a seperate branch of dialog (about forests):
		// Turn off the main options
		OptionOff(1,2,3);
		OptionOff("bye");
		// Turn on the 'forest' branch options
		OptionOn("tree","leaf","forestdone");
		
		yield return E.Break;
		
	}


	public IEnumerator Option3( IDialogOption option )
	{
		yield return C.Dave.Say("Nice well there, huh?", 34);
		yield return C.HardwareClerk.Say("No. I hate it. Lets never speak of it again", 13);
		yield return C.Dave.Say("Alright", 35);
		option.OffForever();
		yield return E.Break;
		
	}



	public IEnumerator OptionForestDone( IDialogOption option )
	{
		// Here we're returning from the 'forest' dialog branch to the main dialog
		
		// Turn off the 'forest' options
		OptionOff("tree","leaf","forestdone");
		
		// Turn on the main options. If they've had 'OptionOffForever' they wont turn on again.
		OptionOn(1,2,3);
		OptionOn("bye");
		
		// Only set the 'Forest' option as used (which changes the color) if all its child options have been used.
		Option(2).Used = Option("tree").Used && Option("leaf").Used;
		yield return E.Break;
		
	}


	public IEnumerator OptionTree( IDialogOption option )
	{
		yield return C.Dave.Say("The trees are cool", 36);
		yield return C.HardwareClerk.Say(" I guess", 14);
		yield return E.Break;
		
	}


	public IEnumerator OptionLeaf( IDialogOption option )
	{
		yield return C.Dave.Say("I like the foliage", 37);
		yield return C.HardwareClerk.Say("Yes. It is pleasant foliage", 15);
		yield return E.Break;
		
	}

	public IEnumerator OptionBye( IDialogOption option )
	{
		yield return C.Dave.Say("Later!", 38);
		yield return E.WaitSkip();
		yield return C.HardwareClerk.FaceRight();
		yield return E.WaitSkip();
		yield return C.HardwareClerk.Say("Whatever", 16);
		
		// Don't mark the 'end' option as used
		option.Used = false;
		
		// stop the dialog
		Stop();
	}
}