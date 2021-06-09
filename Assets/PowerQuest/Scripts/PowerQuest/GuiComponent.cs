using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;
using PowerTools;

namespace PowerTools.Quest
{

// Script class to inherit from for gui scripts
[System.Serializable]
public class GuiScript<T> : QuestScript where T : QuestScript {
	/// Allows access to specific room's script by calling eg. GuiSomething.Script instead of E.GetScript<GuiSomething>()
	public static T Script { get {return E.GetScript<T>(); } }}

//
// Prop Data and functions. Persistant between scenes, as opposed to GuiComponent which lives on a GameObject in a scene.
//
[System.Serializable] 
public partial class Gui : IQuestClickable, IQuestScriptable, IGui
{

	//
	// Default values set in inspector
	//
	[SerializeField] string m_description = "Gui New";
	[ReadOnly][SerializeField] Vector2 m_position = Vector2.zero; // This is taken from the instance position in the scene first time it's loaded
	[SerializeField] float m_baseline = 0;
	[SerializeField] bool m_visible = true;
	[Tooltip("Whether clicking on hotspot triggers an event")]
	[SerializeField] bool m_clickable = true;
	[Tooltip("If true, game clicks won't be processed while this gui is visible")]
	[SerializeField] bool m_modal = false;
	[Tooltip("Whether the gui should pause the game when it's visible")]
	[SerializeField] bool m_pauseGame = false;
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = "";
	[Tooltip("Whether to show the inventory cursor while active")]
	[SerializeField] bool m_allowInventoryCursor = false;
	[Tooltip("Other guis to hide when gui is visible")]
	[SerializeField] string[] m_hideGuisWhenActive = null;
	[ReadOnly][SerializeField] string m_scriptName = "GuiNew";
	[ReadOnly][SerializeField] string m_scriptClass = "GuiNew";

	//
	// Private variables
	//
	QuestScript m_script = null;
	GameObject m_prefab = null;
	GuiComponent m_instance = null;
	List<string> m_hiddenGuis = new List<string>();

	//
	//  Properties
	//
	public eQuestClickableType ClickableType { get {return eQuestClickableType.Gui; } }
	public string ScriptName { get{ return m_scriptName;} }
	public MonoBehaviour Instance { get{ return m_instance; } }
	public Gui Data { get{ return this; } }

	public string Description { get{return m_description;} set{m_description = value;} } // No op for gui
	public Vector2 WalkToPoint { get{return Vector2.zero;} set{} } // No op for gui
	public Vector2 LookAtPoint { get{return Vector2.zero;} set{} } // No op for gui
	public string Cursor { get { return m_cursor; } set { m_cursor = value; } }

	public bool Visible { get{ return m_visible;} 
		set
		{
			bool changed = m_visible != value;
			m_visible = value; 
			if ( m_instance != null ) 
			{
				m_instance.gameObject.SetActive(m_visible);
			}
			if ( changed && PauseGame )
			{				
				if ( m_visible )
					PowerQuest.Get.Pause(m_scriptName);
				else
					PowerQuest.Get.UnPause(m_scriptName);
			}

			if ( changed )
			{
				if ( m_visible )
				{
					foreach ( string guiName in m_hideGuisWhenActive )
					{
						Gui gui = PowerQuest.Get.GetGui(guiName);
						if ( gui != null && gui.Visible )
						{
							gui.Visible = false;
							m_hiddenGuis.Add(guiName);
						}
					}
				}
				else 
				{
					foreach ( string guiName in m_hiddenGuis )
					{
						Gui gui = PowerQuest.Get.GetGui(guiName);
						if ( gui != null )
							gui.Visible = true;						
					}
					m_hiddenGuis.Clear();
				}
			}

		} 
	}
	public bool Clickable { get{ return m_clickable;} set{m_clickable = value;} }
	public bool Modal { get{ return m_modal;} set{m_modal = value;} }
	public bool PauseGame { get{ return m_pauseGame;} set{m_pauseGame = value;} }
	public Vector2 Position { get{ return m_position;} set{ m_position = value; if ( m_instance != null ) { m_instance.transform.position = m_position.WithZ(m_instance.transform.position.z); } } }
	public float Baseline { get{ return m_baseline;} set{m_baseline = value;} }
	public bool AllowInventoryCursor { get { return m_allowInventoryCursor; } }

	//
	// Getters/Setters - These are used by the engine. Scripts mainly use the properties
	//

	public QuestScript GetScript() { return m_script; }
	public IQuestScriptable GetScriptable() { return this; }
	public string GetScriptName(){ return m_scriptName; }
	public string GetScriptClassName() { return m_scriptClass; }
	public void HotLoadScript(Assembly assembly) { QuestUtils.HotSwapScript( ref m_script, m_scriptClass, assembly ); }

	public GameObject GetPrefab() { return m_prefab; }
	public GuiComponent GetInstance() { return m_instance; }
	public void SetInstance(GuiComponent instance) 
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
	}

	//
	// Public Functions
	//
	public void OnInteraction( eQuestVerb verb ) {}	// No op for gui
	public void OnCancelInteraction( eQuestVerb verb ) {}	// No op for gui
	public void PlayAnimation() {throw new System.NotImplementedException();} // TODO: Object Animation
	public void PauseAnimation() {throw new System.NotImplementedException();} // TODO: Object Animation
	public void StopAnimation() {throw new System.NotImplementedException();} // TODO: Object Animation
	public void IsCollidingWith() {throw new System.NotImplementedException();} // TODO: object collision

	//
	// Internal Functions
	//

	//
	// Initialisation
	//

	public void EditorInitialise( string name )
	{
		m_scriptName = name;
		m_scriptClass = "Gui"+name;
		m_description = name;
	}
	public void EditorRename(string name)
	{
		m_scriptName = name;
		m_scriptClass = "Gui"+name;	
	}

	public void OnPostRestore( int version, GameObject prefab )
	{
		m_prefab = prefab;
		if ( m_script == null ) // script could be null if it didn't exist in old save game, but does now.
			m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);

		// Pause state might need to change
		if ( Modal )
		{
			// HACK- Need a way of detecting if someting changed before/after saving so can set this the same way it's set when visible is changed.
			if ( m_visible && SystemTime.GetPausedBy(m_scriptName) == false  )
				PowerQuest.Get.Pause(m_scriptName);
			else if ( m_visible == false && SystemTime.GetPausedBy(m_scriptName) )
				PowerQuest.Get.UnPause(m_scriptName);
		}
	}

	public void Initialise( GameObject prefab )
	{
		m_prefab = prefab;
		// Construct the script
		m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);
	}

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
}

//
// The component on the prop in scene
//
[SelectionBase]
public class GuiComponent : MonoBehaviour
{
	[SerializeField] Gui m_data = new Gui();

	public Gui GetData() { return m_data; }
	public void SetData(Gui data) { m_data = data; }

	/// Updates input, if true returned, or Modal is true, then input won't be passed to next gui
	public bool UpdateInput()
	{
		return false;
	}

	/*
	// Use this for initialization
	void Start () 
	{	
	}

	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	public void OnLoadComplete()
	{
	}

	// Update is called once per frame
	void Update () 
	{
	}
	*/
}

}