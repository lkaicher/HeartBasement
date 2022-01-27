using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Reflection;
using PowerTools;
using PowerTools.QuestGui;

//
// PowerQuest Gui stuff: Toss stuff in here while working on it lol
//

namespace PowerTools.Quest
{

public partial class PowerQuest
{

	// Gui managment stuff
	Gui m_focusedGui = null;
	GuiControl m_focusedControl = null;
	GuiControl m_keyboardFocusedControl = null;


	// Called from PQ update
	void UpdateGuiVisibility()
	{
		// Sort guis by baseline
		List<Gui> sortedGuis = new List<Gui>(m_guis);
		sortedGuis.Sort((Gui a, Gui b)=> a.Baseline.CompareTo(b.Baseline));
		bool hideRemaining = false;

		// Loop through from front to back, and hide any that are obscured, or if cutscene is active
		foreach ( Gui gui in sortedGuis )
		{			
			if ( hideRemaining )
				gui.HiddenBySystem = true;		
			else if ( gui.VisibleInCutscenes == false && (GetBlocked() || m_queuedScriptInteractions.Count > 0 ))
				gui.HiddenBySystem = true;
			else
				gui.HiddenBySystem = false;

			if ( gui.Visible && gui.HideObscuredGuis )
					hideRemaining = true;
		}
		// Hide guis that specifically want others hidden
		foreach ( Gui gui in sortedGuis )
		{
			// Hide any specfiic guis on sorted guis
			if ( gui.Visible )
			{
				foreach( string toHide in gui.HideSpecificGuis )
				{
					Gui toHideG = GetGui(toHide);
					if ( toHideG != null )
						toHideG.HiddenBySystem = true;
				}
			}

		}
	}
	
	IGui m_blockingGui = null;
	public IGui GetBlockingGui() {return m_blockingGui;}
	public Coroutine WaitForGui(IGui gui) {	return StartQuestCoroutine(CoroutineWaitForGui(gui)); }
	
	public void OnGuiShown(Gui gui)
	{

		// Call OnShow on gui script
		if ( gui == null )
			throw new System.Exception("gui is null");
		if ( gui.GetScript() != null )
		{
			System.Reflection.MethodInfo method = gui.GetScript().GetType().GetMethod( "OnShow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
			if ( method != null ) method.Invoke(gui.GetScript(),null);
		}
	}

	IEnumerator CoroutineWaitForGui(IGui gui)
	{
		bool wasCutscene = m_cutscene;
		if ( wasCutscene )
			EndCutscene();
					
		// Set the blocking gui so it can be updated while blocked
		m_blockingGui = gui;
		gui.Show();

		bool hideCursor = GetCursor().HideWhenBlocking;
		GetCursor().HideWhenBlocking = false;
		GetCursor().Visible = true;
		yield return WaitWhile(()=> m_blockingGui.Visible ); // NB: Real risk of soft-locking if something else is causing gui to be hidden or un-clickable
		GetCursor().HideWhenBlocking = hideCursor;
		m_blockingGui=null;

		if (wasCutscene)
			StartCutscene();

		yield return Break;
	}

	// Checks if a particular gui can be focused or clicked on. (ie: there's no modal guis on top of it)
	public bool GetIsGuiObscuredByModal(Gui gui)
	{
		for ( int i = 0; i < m_guis.Count; ++i )
		{
			Gui other = m_guis[i];
			if ( other.Visible && other.HiddenBySystem == false && other.Baseline <= gui.Baseline && other.Modal )
				return true;
		}
		return false;
	}
	


}

}
