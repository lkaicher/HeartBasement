using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.EventSystems;
using PowerTools;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

// Script class to inherit from for gui scripts
[System.Serializable]
public class GuiScript<T> : QuestScript where T : QuestScript 
{	
	/// Access with Data property. \sa Data
	protected Gui m_gui = null;
	
	/// Get the IGui associated with this script. 
	/// eg. `Data.Hide()` 
	/// \sa PowerTools.Quest.IGui
	public Gui Data { get{return m_gui;} }	
	public IGui Gui { get{return m_gui;} }	
	
	/// Gets an IGuiControl from this script's gui.
	/// All gui buttons, image, labels, etc are types of Controls. This function returns their base class. Can be used for your own custom Controls too.
	/// eg. `Control.MyCustomButton.Hide()`
	/// eg. `Control.MyCustomControl.Instance.GetComponent<CoolCustomControl>().DoSomethingCool();`
	/// \sa Button \sa Label \sa Image
	public IGuiControl Control(string name) { return Data?.GetControl(name) ?? null; }

	/// Gets an IButton from this script's gui.
	/// eg. `Button.KeypadEnter.Clickable = false;`
	/// \sa Control \sa Label \sa Image \sa Slider \sa InventoryPanel
	public IButton Button(string name) { return Data?.GetControl(name) as IButton ?? null; }

	/// Gets an ILabel from this script's gui.
	/// eg. `Label.KeypadReadout.Text = "ENTER PASSWORD";`
	/// \sa Control \sa Button \sa Image
	public ILabel Label(string name) { return Data?.GetControl(name) as ILabel ?? null; }
	
	/// Gets an IImage from this script's gui.
	/// eg. `Image.LockedIndicator.Image = "Unlocked";`
	/// \sa Control \sa Label \sa Button
	public IImage Image(string name) { return Data?.GetControl(name) as IImage ?? null; }
	
	/// Gets an ISlider from this script's gui.
	/// eg. `Sliders.Volume.Ratio = Settings.Volume";`
	/// \sa Control \sa Button \sa Image \sa Label
	public ISlider Slider(string name) { return Data?.GetControl(name) as ISlider ?? null; }

	/// Gets an IInventoryPanel from this script's gui.
	/// eg. `IInventoryPanel.MyInvPanel.ScrollForward();`
	/// \sa Control \sa Label \sa Button \sa Image
	public IInventoryPanel InventoryPanel(string name) { return Data?.GetControl(name) as IInventoryPanel ?? null; }

	
	/// Gets an IContainer from this script's gui.
	/// eg. `IContainer.MyList.Grid.ScrollForward();`
	/// \sa Control \sa Label \sa Button \sa Image
	public IContainer Container(string name) { return Data?.GetControl(name) as IContainer ?? null; }
	
	/// PowerQuest internal function: (Called via reflection)
	protected void Initialise(Gui gui) { m_gui = gui; }

	/// PowerQuest internal functionAllows access to specific room's script by calling eg. GuiSomething.Script instead of E.GetScript<GuiSomething>()
	public static T Script { get {return E.GetScript<T>(); } }
}

//
// Prop Data and functions. Persistant between scenes, as opposed to GuiComponent which lives on a GameObject in a scene.
//
[System.Serializable] 
public partial class Gui : IQuestClickable, IQuestScriptable, IGui
{
	//
	// Default values set in inspector
	//
	[Tooltip("The sort order for the gui, like other hotspots, LOWER is IN-FRONT")]
	[Range(-319,319)]
	[SerializeField] float m_baseline = -1;
	
	[Tooltip("Whether the gui is starts on")]
	[SerializeField] bool m_visible = true;	
	[Tooltip("Whether the gui hides itself during cutscenes")]
	[SerializeField] bool m_visibleInCutscenes = true;	
	[Tooltip("If true, the gui blocks input to the game or any guis behind it. Useful for popups and things")]
	[SerializeField] bool m_modal = false;

	[Header("When Shown...")]
	[Tooltip("Whether the gui should pause the game when it's visible")]
	[SerializeField] bool m_pauseGame = false;
	[Tooltip("Whether guis behind this one should be hidden")]
	[SerializeField] bool m_hideObscuredGuis = false;
	[Tooltip("Other guis to hide when gui is visible")]
	[UnityEngine.Serialization.FormerlySerializedAs("m_hideGuisWhenActive")]
	[SerializeField] string[] m_hideSpecificGuis = null; // todo: rename "m_hideSpecificGuis"...?
		
	[Header("Mouse over")]
	[SerializeField] string m_description = string.Empty;
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = string.Empty;
	[Tooltip("Whether to show the inventory cursor while active")]
	[SerializeField] bool m_allowInventoryCursor = false;

	[Header("Read only")]
	[ReadOnly][SerializeField] Vector2 m_position = Vector2.zero; // This is taken from the instance position in the scene first time it's loaded
	[ReadOnly][SerializeField] string m_scriptName = "GuiNew";
	[ReadOnly][SerializeField] string m_scriptClass = "GuiNew";
	

	//
	// Private variables
	//
	
	//[Tooltip("Whether clicking on hotspot triggers an event")]
	bool m_clickable = true;

	bool m_hiddenBySystem = false;

	QuestScript m_script = null;
	GameObject m_prefab = null;
	GuiComponent m_instance = null;
	//List<string> m_hiddenGuis = new List<string>();
	
	List<GuiControl> m_controls = new List<GuiControl>();

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

	public GuiControl GetControl(string name) 
	{ 
		GuiControl result = m_controls.Find(prop=>prop !=null && string.Equals(prop.ScriptName, name, System.StringComparison.OrdinalIgnoreCase ));
		if ( result == null )
			Debug.LogError("Gui Control '"+name+"' doesn't exist in " +ScriptName);
		return result;
	}

	public bool Visible 
	{ 
		get { return m_visible;} 
		set
		{
			bool changed = m_visible != value;
			m_visible = value; 
			if ( changed && m_visible &&  VisibleInCutscenes == false && PowerQuest.Get.GetBlocked() )
				HiddenBySystem = true; // set visible when it should be hidden by system
			if ( changed )
				OnVisibilityChanged();
		} 
	}

	/// Whether clicking on hotspot triggers an event. Can be set false to have a gui visible but not clickable.
	public bool Clickable { get{ return m_clickable;} set{m_clickable = value;} }
	
	// Property used when gui is hidden by the system, despite being "shown" by the user. (eg. when obscured by another gui, or hidden while in a cutscene)
	public bool HiddenBySystem
	{ 
		get => m_hiddenBySystem; 
		set 
		{	
			bool oldVisibility = IsActuallyVisible();
			m_hiddenBySystem = value;
			if  ( oldVisibility != IsActuallyVisible() )
				OnVisibilityChanged();			
		}
	}
	public string[] HideSpecificGuis => m_hideSpecificGuis;

	public bool Modal { get{ return m_modal;} set{m_modal = value;} }	
	public bool PauseGame { get{ return m_pauseGame;} set{m_pauseGame = value;} }
	public bool VisibleInCutscenes { get{ return m_visibleInCutscenes;} set{m_visibleInCutscenes = value;} }
	public bool HideObscuredGuis { get{ return m_hideObscuredGuis;} set{ m_hideObscuredGuis = value;} }
	
	// Returns true if the gui is under another modal gui
	public bool ObscuredByModal { get => PowerQuest.Get.GetIsGuiObscuredByModal(this); }

	public Vector2 Position { get{ return m_position;} set{ m_position = value; if ( m_instance != null ) { m_instance.transform.position = m_position.WithZ(m_instance.transform.position.z); } } }
	public float Baseline { get{ return m_baseline;} 
		set 
		{ 	
			if ( m_baseline != value )
			{
				m_baseline = value;
				if ( m_instance != null )
					m_instance.OnSetBaseline();				
			}
		} 
	}
	public bool AllowInventoryCursor { get { return m_allowInventoryCursor; } }
	
	public void Show() { Visible=true; Clickable=true; }
	
	/// Shows the gui, in front of all others.
	public void ShowAtFront()
	{
		Show();
		float minBaseline = float.MinValue;
		PowerQuest.Get.GetGuis().ForEach( item=> { if ( item != null && item.Baseline < minBaseline) minBaseline=item.Baseline;} );
		if ( minBaseline > float.MinValue )
			Baseline=minBaseline-1;
	}
	/// Shows the gui, behind all others.
	public void ShowAtBack()
	{
		Show();

		float minBaseline = float.MaxValue;
		PowerQuest.Get.GetGuis().ForEach( item=> { if ( item != null && item.Baseline > minBaseline) minBaseline=item.Baseline;} );				
		if ( minBaseline < float.MaxValue )
			Baseline=minBaseline+1;
	}

	/// Shows the gui, behind a specific other gui.
	public void ShowBehind(IGui gui)
	{
		Show();
		if ( gui != null )
			Baseline = gui.Baseline+1;
	}

	/// Shows the gui, in front of a specific other gui.
	public void ShowInfront(IGui gui)
	{
		Show();
		if ( gui != null )
			Baseline = gui.Baseline-1;
	}

	public void Hide() 
	{
		Visible=false;
		Clickable=false;
	}

	public bool HasFocus { get { return PowerQuest.Get.GetFocusedGui()== this; } }

	//
	// Getters/Setters - These are used by the engine. Scripts mainly use the properties
	//

	public QuestScript GetScript() { return m_script; }
	public IQuestScriptable GetScriptable() { return this; }
	public string GetScriptName(){ return m_scriptName; }
	public string GetScriptClassName() { return m_scriptClass; }
	public void HotLoadScript(Assembly assembly) 
	{ 
		QuestUtils.HotSwapScript( ref m_script, m_scriptClass, assembly ); 
		if ( m_script != null )
		{
			// Hack to set gui in script
			System.Reflection.MethodInfo method = m_script.GetType().GetMethod( "Initialise", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy  );
			if ( method != null ) 
				method.Invoke(m_script, new object[]{Data});		
		}
	}
	public T GetScript<T>() where T : GuiScript<T> {  return ( m_script != null ) ? m_script as T : null; }

	public GameObject GetPrefab() { return m_prefab; }
	public GuiComponent GetInstance() { return m_instance; }
	public void SetInstance(GuiComponent instance) 
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
				
		// Set the instances for child controls-  They don't have their own data (like rooms) but need access to the gui's data (I think?);		
		GuiControl[] controls= m_instance.GetComponentsInChildren<GuiControl>(true);
		m_controls = new List<GuiControl>(controls);
		foreach ( GuiControl control in controls )
		{
			control.SetGui(this);
		}
		
		// Check if starting visible, if so, call OnGuiShown now
		if ( Visible && PowerQuest.Get.GetRestoringGame() == false )
			PowerQuest.Get.OnGuiShown(this);
	}

	//
	// Public Functions
	//
	public void OnInteraction( eQuestVerb verb ) {}	// No op for gui
	public void OnCancelInteraction( eQuestVerb verb ) {}	// No op for gui

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
		m_description = string.Empty;
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
		if ( m_script != null )
		{
			// Set data in script
			System.Reflection.MethodInfo method = m_script.GetType().GetMethod( "Initialise", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy  );
			if ( method != null ) 
				method.Invoke(m_script, new object[]{Data});		
		}
		
		// Start with gui off
		if ( m_instance != null )
		{
			m_instance.gameObject.SetActive(false);		
			m_instance.OnSetBaseline();
		}

		// This will turn guis back on (and call OnEnable again)
		OnVisibilityChanged();
	}

	public void Initialise( GameObject prefab )
	{
		m_prefab = prefab;
		// Construct the script
		m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);
		if ( m_script != null )
		{
			// Hack to set gui in script
			System.Reflection.MethodInfo method = m_script.GetType().GetMethod( "Initialise", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy );
			if ( method != null ) 
				method.Invoke(m_script, new object[]{Data});
			
		}
	}

	bool IsActuallyVisible() { return Visible && HiddenBySystem == false; }

	// Since multiple things control visibility, this controls when it changed. NB: ONLY CALL IF IsActuallyVisible CHANGED! 
	void OnVisibilityChanged()
	{
		if ( m_instance != null ) 
			m_instance.gameObject.SetActive(IsActuallyVisible());

		// Not really sure if should pause/unpause if the gui is marked Shown, but hidden by some other gui. (eg. in case that a pausing gui is hidden by a non-pauseing one... might be wierd..?
		if ( PauseGame )
		{				
			if ( IsActuallyVisible() )
				PowerQuest.Get.Pause(m_scriptName);
			else
				PowerQuest.Get.UnPause(m_scriptName);
		}
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
	[SerializeField, ReadOnly, NonReorderable] List<AnimationClip> m_animations =  new List<AnimationClip>();
	[SerializeField, ReadOnly, NonReorderable] List<Sprite> m_sprites = new List<Sprite>();	
		
	// this is stored mainly for editor
    [SerializeField] [HideInInspector] List<GuiControl> m_controlComponents = new List<GuiControl>();

	// Callback when gui is focused (At top, or mouse is over one of its controls)
	public System.Action CallbackOnFocus = null;
	// Callback when gui loses focused
	public System.Action CallbackOnDefocus = null;

	// Called from PowerQuest when gui gains focus (At top, or mouse is over one of its controls)
	public void OnFocus()
	{
		CallbackOnFocus?.Invoke();
	}

	// Called from PowerQuest when gui loses focus
	public void OnDefocus()
	{
		CallbackOnDefocus?.Invoke();
	}

	public Gui GetData() { return m_data; }
	public void SetData(Gui data) { m_data = data; }
	
	// Note- this function might end up being relatively expensive
	public AnimationClip GetAnimation(string animName) 
	{ 
		AnimationClip clip = m_animations.Find(item=>item != null && string.Equals(animName, item.name, System.StringComparison.OrdinalIgnoreCase));  

		// Tryu in shared gui animations
		if ( clip == null && PowerQuest.Get != null )		
			clip = PowerQuest.Get.GetGuiAnimation(animName);	

		// Try in inventory animations
		if ( clip == null && PowerQuest.Get != null  )		
			clip = PowerQuest.Get.GetInventoryAnimation(animName);
		return clip;
	}
	public List<AnimationClip> GetAnimations() { return m_animations; }
	

	public Sprite GetSprite(string animName) 
	{
		Sprite sprite = PowerQuest.FindSpriteInList(m_sprites, animName);

		// Try in shared gui sprites
		if ( sprite == null && PowerQuest.Get != null )		
			sprite = PowerQuest.Get.GetGuiSprite(animName);

		// Try in inventory sprites
		if ( sprite == null && PowerQuest.Get != null )		
			sprite = PowerQuest.Get.GetInventorySprite(animName);

		return sprite;
	}
	public List<Sprite> GetSprites() { return m_sprites; }
	
	public void OnSetBaseline()
	{
		foreach (GuiControl control in m_controlComponents)
		{
			if ( control != null ) control.UpdateBaseline();
		}
	}

	void Awake()
	{
		m_controlComponents.Clear();
		m_controlComponents.AddRange(GetComponentsInChildren<GuiControl>());	
	}
	
	// Use this for initialization
	void Start () 
	{	

		// Update baselines of controls
		OnSetBaseline();
	}

	void OnEnable()
	{	
		if ( PowerQuest.Get != null && GetData() != null && GetData().Visible )
			PowerQuest.Get.OnGuiShown(GetData());
	}


	// Update is called once per frame
	void Update () 
	{
		// Handle mouse clicks for the gui itself
		if ( PowerQuest.Get.GetFocusedGui() == GetData() && PowerQuest.Get.GetFocusedGuiControl() == null && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) )
		{			
			PowerQuest.Get.ProcessGuiClick(GetData());
		}

	}
	
	public void RegisterControl(GuiControl control)
	{
		if ( m_controlComponents.Exists(item=>item==control) == false )
			m_controlComponents.Add(control);
	}
	public void EditorUpdateChildComponents()
	{
		m_controlComponents.Clear();
		m_controlComponents.AddRange(GetComponentsInChildren<GuiControl>(true));
	}

	public List<GuiControl> GetControlComponents() { return m_controlComponents; }
}

}
