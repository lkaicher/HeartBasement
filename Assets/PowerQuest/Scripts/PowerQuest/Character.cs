using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PowerTools;

namespace PowerTools.Quest
{

//
// Character Data and functions. Persistant between scenes, as opposed to CharacterComponent which lives on a GameObject in a scene.
//
[System.Serializable]
public partial class Character : IQuestClickable, ICharacter, IQuestScriptable
{	
	#region Constants/Definitions

	// Degrees 
	static readonly float FACING_ANGLE_SEGMENT_DEG = 45.0f/2.0f;

	// Corresponds to eFace enum: Left, Right, Down, Up, DownLeft, DownRight, UpLeft, UpRight
	public static readonly Vector2[] FACE_DIRECTIONS = { Vector2.left, Vector2.right, Vector2.down, Vector2.up, new Vector2(-1,-1).normalized, new Vector2(1,-1).normalized, new Vector2(-1,1).normalized, Vector2.one.normalized }; 

	[System.Serializable]
	public class CollectedItem
	{
		public string m_name = string.Empty; // String rather than reference to make saving/loading easy
		public float m_quantity = 1; // Why a float? Well, maybe you have half a cup of water, I don't know!
	}

	public enum eState
	{
		Idle,
		Turn,
		Walk,
		Talk,
		Animate,
		None		
	}

	/*/ TODO: change facing angles to be customisable, like this:
	float m_maxAngleUR = 60.0f; // Higher than this is "Up"
	float m_maxAngleR = 30.0f; // Higher than this is "UpRight"
	float m_maxAngleDR = -30.0f; // Hihger than this is "DownRight", Lower than this is "Down"
	float m_maxAngleD = -60.0f; // Hihger than this is "DownRight", Lower than this is "Down"
	/*/// TODO: also give option for "AGS" style facing. eg. if no diagonal, use closest 45 degree.

	#endregion
	#region Vars: Inspector
		
	//
	// Default values set in inspector
	//
	[Header("Mouse-over Defaults")]
	[TextArea]
	[SerializeField] string m_description = "New Character";
	[Tooltip("If set, changes the name of the cursor when moused over")]
	[SerializeField] string m_cursor = null;
	[Header("Starting Room, Position, etc")]
	[SerializeField] string m_room = null;
	[SerializeField] Vector2 m_position = Vector2.zero;
	[SerializeField] eFace m_faceDirection = eFace.Down;
	[SerializeField] bool m_clickable = true;	// Whether character is clickable/can be interacted with
	[SerializeField] bool m_visible = true;		// Whether character sprites are visible
	[SerializeField] bool m_moveable = true;	// Whether character can walk
	[SerializeField] List<CollectedItem> m_inventory = new List<CollectedItem>();
	[Header("Movement Defaults")]
	[SerializeField] Vector2 m_walkSpeed = new Vector2(50,50);
	[SerializeField] bool m_turnBeforeWalking = true;
	[SerializeField] bool m_turnBeforeFacing = true;
	[Tooltip("How fast character turns (Frames per second)")]
	[SerializeField] float m_turnSpeedFPS = 12;
	[SerializeField] bool m_adjustSpeedWithScaling = true;
	[Header("Visuals Setup")]
	[SerializeField] Color m_textColour = Color.white;
	[SerializeField] string m_animIdle = "Idle";
	[SerializeField] string m_animWalk = "Walk";
	[SerializeField] string m_animTalk = "Talk";
	[SerializeField] string m_animMouth = string.Empty;
	[SerializeField] bool m_useRegionTinting = true;
	[SerializeField] bool m_useRegionScaling = true;
	[SerializeField, Tooltip("To use, talk anims should be frames ABCDEFX in that order from https://github.com/DanielSWolf/rhubarb-lip-sync. Rhubarb must be downloaded to Project/Rhubarb/Rhubarb.exe")] 
	bool m_LipSyncEnabled = false;

	[Header("Audio")]
	[Tooltip("Add Footstep event to animation to trigger the footstep sound")]
	[SerializeField] string m_footstepSound = string.Empty;

	[Header("Other Settings")]
	[SerializeField] float m_baseline = 0;
	[SerializeField] Vector2 m_walkToPoint = Vector2.zero;
	[SerializeField] Vector2 m_lookAtPoint = Vector2.zero;

	// Todo: Maybe... allow easy set up to walk to "either side" of things. Alternates include list of "walkto"s, and/or option to "WalkToEvenIfCloser"
	//[SerializeField] bool m_walkToSymmetrical = true;

	[ReadOnly][SerializeField] string m_scriptName = "New";
	[ReadOnly][SerializeField] string m_scriptClass = "CharacterNew";

	#endregion
	#region Vars: Private

	//
	// private variables
	//

	// Script and instance
	QuestScript m_script = null;
	GameObject m_prefab = null;
	CharacterComponent m_instance = null;

	// Inventory- currently selected item
	string m_activeInventory = null;
	// Inventory- Every item ever owned (for GetEverHadInventory()). Was using hashSet but that can't be serialised unfortunately
	List<string> m_inventoryAllTime = new List<string>();

	eFace m_targetFaceDirection = eFace.Right; // Used for turn-to-face
	eFace m_facingVerticalFallback = eFace.Right; // Should only be Left or Right- Used if there's no up/down frames to choose whether R or L anim is used
	
	eFace m_faceAfterWalk = eFace.None; // used for face after walking in background

	QuestText m_dialogText = null;

	AudioHandle m_dialogAudioSource = null;

	string m_lastRoom = null;

	Vector2 m_textPositionOverride = Vector2.zero;

	int m_clickableColliderId = 0;

	IEnumerator m_coroutineSay = null;

	int m_useCount = 0;
	int m_lookCount = 0;

	#endregion
	#region Properties

	//
	//  Properties
	//
	public eQuestClickableType ClickableType { get {return eQuestClickableType.Character; } }
	public string Description { get{ return m_description;} set{ m_description=value;} }
	public string ScriptName { get{ return m_scriptName;} }
	public MonoBehaviour Instance { get{ return m_instance; } }
	public Character Data { get{ return this; } }
	public IQuestClickable IClickable { get{ return this; } }

	public IRoom Room 
	{ 
		// TODO: Cache the room so dont' have to look it up each call
		get { return PowerQuest.Get.GetRoom(m_room); } 
		set
		{ 
			
			string oldRoom = m_room;
			m_room = value.ScriptName; 
			if ( oldRoom == m_room )
				return;

			m_lastRoom = oldRoom;

			string currRoom = PowerQuest.Get.GetCurrentRoom().ScriptName;

			// Add/remove instance of character if it's the current room that effected. 
			if ( currRoom == oldRoom )
			{

				// Handle the player changing rooms (should trigger scene change)
				if ( PowerQuest.Get.GetPlayer() == this )
				{		
					PowerQuest.Get.StartRoomTransition(value.Data);
				}
				// Player has left the current room, remove them from the scene
				else if ( Instance != null )
				{
					Instance.gameObject.name = "deleted"; // so it doesn't turn up in searches in same frame
					GameObject.Destroy(Instance.gameObject);
					
				}
			}	
			else if ( currRoom == m_room )
			{
				// Player has entered the current room, add them to the scene
				SpawnInstance();
			}	
		}	
	}

	public void ChangeRoomBG(IRoom room) { Room = room; }
	public Coroutine ChangeRoom(IRoom room) { return PowerQuest.Get.StartCoroutine(CoroutineChangeRoom(room)); }
	IEnumerator CoroutineChangeRoom(IRoom room)
	{
		if ( PowerQuest.Get.GetPlayer() == this )
		{
			#if UNITY_2019_1_OR_NEWER				
				Debug.Log("NB: C.Player.ChangeRoom() does not work correctly in Unity 2019 or later. Use C.Player.ChangeRoomBG() instead (for now).");
				PowerQuest.Get.ChangeRoomBG(room);
				yield break; // don't yield to function because it breaks unity in 2019+						
			#else
				yield return PowerQuest.Get.ChangeRoom(room);	
			#endif
		}
		else 
			Room = room; 


		
	}

	// Returns the last room visited before the current one
	public IRoom LastRoom { get { return PowerQuest.Get.GetRoom(m_lastRoom); } }

	public Vector2 Position{ get{return m_position;} set {SetPosition(value);} }
	public Vector2 TargetPosition{ get
	{
		if ( m_instance != null )
			return m_instance.GetTargetPosition();
		return m_position;
	} }

	public float Baseline { get{return m_baseline;} set{m_baseline = value;} }
	public Vector2 WalkSpeed { get{ return m_walkSpeed; } set { m_walkSpeed = value; } }
	public bool TurnBeforeWalking { get{ return m_turnBeforeWalking; } set { m_turnBeforeWalking = value; } }
	public bool TurnBeforeFacing { get{ return m_turnBeforeFacing; } set { m_turnBeforeFacing = value; } }
	public float TurnSpeedFPS { get{ return m_turnSpeedFPS; } set { m_turnSpeedFPS = value; } }

	public bool AdjustSpeedWithScaling { get { return m_adjustSpeedWithScaling; } set { m_adjustSpeedWithScaling = value; } }

	public eFace GetFaceAfterWalk() { return m_faceAfterWalk; }

	public eFace Facing 
	{ 
		get { return m_faceDirection;}
		set 
		{ 			
			m_faceAfterWalk = eFace.None;
			m_faceDirection = value; 
			m_targetFaceDirection = m_faceDirection; // Set target too to snap to target
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(value); 
		} 
	}
	public bool Clickable { get{return m_clickable && GetHiddenInRoom() == false;} set{m_clickable = value;} }
	public bool Visible 
	{ 
		get{return m_visible && GetHiddenInRoom() == false;} 
		set
		{
			m_visible = value;
			if ( m_instance )
			{
				m_instance.UpdateVisibility();
				/*
				bool shouldShow = m_visible && GetHiddenInRoom() == false;
				if ( m_instance.GetSpriteAnimator() != null )
					m_instance.GetSpriteAnimator().enabled = shouldShow;
				
				Renderer[] renderers = m_instance.GetComponentsInChildren<Renderer>(true);
				System.Array.ForEach(renderers, renderer => 
					{
						renderer.enabled = shouldShow;
					});
					*/
			}
		} 
	}
	
	public bool VisibleInRoom {  get { return Visible && Room != null && Room.Current; } }

	/// Set's visible & clickable (same as `Enable()`)
	public void Show( bool clickable = true ) { Enable( clickable ); }
	/// Set's invisible & non-clickable (same as `Disable()`)
	public void Hide() { Disable(); }
	/// Set's visible & clickable
	public void Enable( bool clickable = true )
	{
		Visible = true;
		Clickable = clickable;
		// Change character to be in current room
		Room = PowerQuest.Get.GetCurrentRoom();
	}
	/// Set's invisible & non-clickable
	public void Disable() { Visible = false; Clickable = false; }

	public bool Moveable { get{return m_moveable && GetHiddenInRoom() == false;} set{m_moveable = value;} }
	public bool Walking { get{ return m_instance == null ? false : m_instance.GetIsWalking(); } }
	public bool Talking { get{ return (m_dialogText != null && m_dialogText.gameObject.activeSelf) || (m_dialogAudioSource != null && m_dialogAudioSource.isPlaying); /*return m_instance == null ? false : m_instance.GetIsTalking();*/ } }
	public bool Animating { get{ return m_instance == null ? false : m_instance.GetState() == eState.Animate; } }
	public bool IsPlayer { get{ return PowerQuest.Get.GetPlayer() == this; } }

	public Color TextColour 
	{ 
		get { return m_textColour;} 
		set 
		{
			m_textColour = value; 
			if (m_dialogText != null )
			{
				m_dialogText.GetComponent<TextMesh>().color = m_textColour;
			}
		}
	}

	public string AnimIdle
	{ 
		get{return m_animIdle;}
		set
		{
			bool changed = m_animIdle != value;
			m_animIdle = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Idle );
		}
	}
	public string AnimWalk
	{ 
		get{return m_animWalk;}
		set
		{
			bool changed = m_animWalk != value;
			m_animWalk = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Walk );
		}
	}
	public string AnimTalk
	{ 
		get{return m_animTalk;}
		set
		{
			bool changed = m_animTalk != value;
			m_animTalk = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Talk );
		}
	}
	public string AnimMouth
	{ 
		get{return m_animMouth;}
		set
		{
			bool changed = m_animMouth != value;
			m_animMouth = value;
			if ( m_instance != null && changed ) m_instance.OnAnimationChanged( eState.Talk );
		}
	}

	public bool LipSyncEnabled
	{
		get{ return m_LipSyncEnabled; }
		set { m_LipSyncEnabled = value; }
	}

	public string FootstepSound
	{
		get{ return m_footstepSound; }
		set { m_footstepSound = value; }
	}

	// Returns true if currently playing an anim (using playAnimation, not an idle/walk/talk)
	public eState State { get { return ( m_instance != null ) ? (m_instance.GetState()) : eState.None; } }

	// The currently selected inventory item
	public IInventory ActiveInventory 
	{ 
		get { return PowerQuest.Get.GetInventory(m_activeInventory); } 
		set	{ m_activeInventory = value == null ? null : value.ScriptName; } 
	}
	public bool HasActiveInventory { get { return string.IsNullOrEmpty(m_activeInventory) == false; } }

	public string ActiveInventoryName { get { return m_activeInventory; } set { m_activeInventory = value; } }
	public bool HasActiveInventoryName { get { return string.IsNullOrEmpty(m_activeInventory); } }

	public string Cursor { get { return m_cursor; } set { m_cursor = value; } }

	public bool UseRegionTinting { get { return m_useRegionTinting; } set { m_useRegionTinting = value; } }
	public bool UseRegionScaling { get { return m_useRegionScaling; } set { m_useRegionScaling = value; } }

	public bool FirstUse { get { return UseCount == 0; } } 
	public bool FirstLook { get { return LookCount == 0; } }
	public int UseCount { get { return m_useCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Use) ? 1 : 0); } }
	public int LookCount { get { return m_lookCount - (PowerQuest.Get.GetInteractionInProgress(this,eQuestVerb.Look) ? 1 : 0); } }

	public Vector2 WalkToPoint { get{ return m_walkToPoint;} set{m_walkToPoint = value;} }
	public Vector2 LookAtPoint { get{ return m_lookAtPoint;} set{m_lookAtPoint = value;} }
	//public bool WalkToSymmetrical { get{ return m_walkToSymmetrical ;} set{m_walkToSymmetrical = value;} }

	public Vector2 TextPositionOverride { get{ return m_textPositionOverride; } set { m_textPositionOverride = value; } }

	// Callback when starting to say something. CallbackSay(string dialog, int id)
	public System.Action<string,int> CallbackOnSay = null;
	// Callback when finished saying something
	public System.Action CallbackOnEndSay = null;

	#endregion
	#region Funcs: Getter/Setter

	//
	// Getters/Setters - These are used by the engine. Scripts mainly use the properties
	//
	public QuestScript GetScript() { return m_script; }
	public IQuestScriptable GetScriptable() { return this; }
	public T GetScript<T>() where T : CharacterScript<T> {  return ( m_script != null ) ? m_script as T : null; }
	public string GetScriptName(){ return m_scriptName; }
	public string GetScriptClassName() { return m_scriptClass; }
	public void HotLoadScript(Assembly assembly) { QuestUtils.HotSwapScript( ref m_script, m_scriptClass, assembly ); }

	public GameObject GetPrefab() { return m_prefab; }

	// Spawns instance of ths character in the current room. Should only be used by PowerQuest or internally
	public GameObject SpawnInstance()	
	{
		GameObject characterInstance = GameObject.Find(GetPrefab().name);GameObject.Find(GetPrefab().name);
		if ( characterInstance == null )
		{
			characterInstance = GameObject.Instantiate( GetPrefab() ) as GameObject;
		}
		SetInstance(characterInstance.GetComponent<CharacterComponent>());
		SetPosition(GetPosition());
		Facing = GetFaceDirection();
		return characterInstance;
	}
	public GameObject GetInstance() { return m_instance.gameObject; }
	public void SetInstance(CharacterComponent instance) 
	{ 
		m_instance = instance; 
		m_instance.SetData(this);
		m_instance.name = m_prefab.name;

		// Set the clickable collider to enable the correct collision after restoring
		m_instance.OnClickableColliderIdChanged();
	}
	public void SetPosition(float x, float y) { SetPosition(new Vector2(x,y)); }
	public void SetPosition(Vector2 position) 
	{ 
		m_position = position; 
		if ( m_instance != null )
		{
			m_instance.transform.position = Utils.Snap(m_position,PowerQuest.Get.SnapAmount);
		}
	}
	public Vector2 GetPosition() { return m_position; }

	// Facing get/setters for component
	public eFace GetFacingVerticalFallback() { return m_facingVerticalFallback; }
	public eFace GetTargetFaceDirection() { return m_targetFaceDirection; }
	public eFace GetFaceDirection() { return m_faceDirection; }
	public void SetFaceDirection(eFace direction) { m_faceDirection = direction; } // used by CharacterComponent, doesnt' set target, just actual facing

	public List<CollectedItem> GetInventory() {return m_inventory;}

	public float GetInventoryItemCount()
	{
		return m_inventory.Count;
	}

	// REturns quantity of specific item
	public float GetInventoryQuantity(string itemName)
	{ 
		float result = 0;
		foreach ( CollectedItem inv in m_inventory )
		{
			if ( inv.m_name == itemName )
			{
				result += inv.m_quantity;
			}
		}
		return result;
	}

	public bool HasInventory(string itemName)
	{
		return m_inventory.Exists( inv=>inv.m_name == itemName );
	}
	public bool GetEverHadInventory(string itemName)
	{
		return m_inventoryAllTime.Contains(itemName);
	}

	public void AddInventory(string itemName, float quantity = 1)
	{
		Inventory invItem = PowerQuest.Get.GetInventory(itemName);
		if ( invItem == null ) 
			return;

		invItem.OnCollected();
		if ( PowerQuest.Get.CallbackOnInventoryCollected != null )
			PowerQuest.Get.CallbackOnInventoryCollected.Invoke(Data,invItem);

		if ( invItem.Stack )
		{
			// Find existing and increment quantity
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			if ( collectedItem == null )
			{
				m_inventory.Add(new CollectedItem() {m_name = itemName, m_quantity = quantity});
				if ( m_inventoryAllTime.Contains(itemName) == false )
					m_inventoryAllTime.Add(itemName);
			}
			else 
			{
				collectedItem.m_quantity += quantity;
			}
		}
		else 
		{
			// Add 'quantity' number of the item
			for ( int i = 0; i < quantity; ++i )
			{
				m_inventory.Add(new CollectedItem() {m_name = itemName, m_quantity = quantity});
			}
			if ( m_inventoryAllTime.Contains(itemName) == false )
				m_inventoryAllTime.Add(itemName);
		}

	}
	public void RemoveInventory( string itemName, float quantity = 1 )
	{
		Inventory invItem = PowerQuest.Get.GetInventory(itemName);
		if ( invItem == null ) 
			return;
		if ( invItem.Stack )
		{
			// Find existing and decrement quantity, and remove if none left
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			if ( collectedItem != null )
			{
				collectedItem.m_quantity -= quantity;
				if ( collectedItem.m_quantity <= 0 )
				{
					m_inventory.Remove(collectedItem);
				}
			}
		}
		else 
		{
			// remove up to "quantity" number of the item
			CollectedItem collectedItem =  m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			for (int i = 0; i < quantity && collectedItem != null; ++i)
			{
				m_inventory.Remove(collectedItem);
				collectedItem = m_inventory.Find( item=>string.Equals(itemName, item.m_name, System.StringComparison.OrdinalIgnoreCase) );
			}
		}

		// if the currently selected item was Removed, remove it
		if ( itemName == m_activeInventory && HasInventory(itemName) == false )
		{
			ActiveInventory = null;
		}
		
	}
	public void ClearInventory()
	{
		m_inventory.Clear();
	}

	public float GetInventoryQuantity(IInventory item) { return GetInventoryQuantity(item?.ScriptName); }
	public bool HasInventory(IInventory item) { return HasInventory(item?.ScriptName); }
	public bool GetEverHadInventory(IInventory item) { return GetEverHadInventory(item?.ScriptName); }
	public void AddInventory(IInventory item, float quantity = 1) { AddInventory( item?.ScriptName,quantity ); }
	public void RemoveInventory( IInventory item, float quantity = 1 ) { RemoveInventory( item?.ScriptName,quantity ); }

	public AudioSource GetDialogAudioSource() { return m_dialogAudioSource; }


	#endregion
	#region Funcs: Init

	//
	// Initialisation
	//

	public void EditorInitialise( string name )
	{
		// creates defaults
		m_description = name;
		m_scriptName = name;
		m_scriptClass = "Character"+name;	
	}
	public void EditorRename(string name)
	{
		m_scriptName = name;
		m_scriptClass = "Character"+name;	
	}
	public string EditorGetRoom() { return m_room; }
	public void EditorSetRoom(string roomName) { m_room = roomName; }

	public void OnPostRestore( int version, GameObject prefab )
	{
		m_prefab = prefab;
		if ( m_script == null ) // script could be null if it didn't exist in old save game, but does now.
			m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);

		// Set the clickable collider to enable the correct collision after restoring
		if ( m_instance != null ) m_instance.OnClickableColliderIdChanged();
	}

	public void Initialise( GameObject prefab )
	{
		m_prefab = prefab;
		// Construct the script
		m_script = QuestUtils.ConstructByName<QuestScript>(m_scriptClass);

		// Deep copy inventory
		List<CollectedItem> defaultInventory = m_inventory;
		m_inventory = new List<CollectedItem>();
		QuestUtils.CopyFields(m_inventory, defaultInventory);

		// Add starting inventory to m_inventoryAllTime
		m_inventoryAllTime.Clear();
		foreach ( CollectedItem item in m_inventory )
		{
			if ( m_inventoryAllTime.Contains(item.m_name) == false )
				m_inventoryAllTime.Add(item.m_name);
		}
	}


	#endregion
	#region Funcs: Public

	//
	// Public Functions
	//

	public void OnInteraction( eQuestVerb verb )
	{		
		if ( verb == eQuestVerb.Look ) ++m_lookCount;
		else if ( verb == eQuestVerb.Use) ++m_useCount;
	}
	public void OnCancelInteraction( eQuestVerb verb )
	{		
		if ( verb == eQuestVerb.Look ) --m_lookCount;
		else if ( verb == eQuestVerb.Use) --m_useCount;
	}

	public void WalkToBG( float x, float y, bool anywhere = false, eFace thenFace = eFace.None )
	{
		WalkToBG(new Vector2(x,y), anywhere,thenFace);
	}

	public void WalkToBG( Vector2 pos, bool anywhere = false, eFace thenFace = eFace.None )
	{
		m_faceAfterWalk = thenFace;
		//Debug.Log(GetDescription() + " Walking to "+pos.ToString());	
			
		// This stops the sequence being cancellable,  since 'Walking' property is used to check if it's possible to cancel, and BG walking shouldn't be cancelable
		PowerQuest.Get.DisableCancel();

		if ( Moveable == false )
			return;
		if ( m_instance != null && PowerQuest.Get.GetSkipCutscene() == false )
		{
			m_instance.WalkTo(pos, anywhere);
		}
		else 
		{
			SetPosition(pos);
		}	

	}
	public void WalkToBG(IQuestClickableInterface clickable, bool anywhere = false, eFace thenFace = eFace.None)
	{
		m_faceAfterWalk = thenFace;
		if ( clickable != null )
		{			
			if ( clickable.IClickable.Instance != null )
				WalkToBG((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint, anywhere);
			else
				WalkToBG(clickable.IClickable.WalkToPoint, anywhere);
		}
	}
	public Coroutine WalkTo(float x, float y, bool anywhere = false) {	return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(new Vector2(x,y), anywhere)); }
	public Coroutine WalkTo(Vector2 pos, bool anywhere = false) {	return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(pos, anywhere)); }
	public Coroutine WalkTo(IQuestClickableInterface clickable, bool anywhere = false) 
	{
		m_faceAfterWalk = eFace.None;
		if ( clickable != null )
		{
			if ( clickable.IClickable.Instance !=  null )
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(((Vector2)clickable.IClickable.Instance.transform.position + clickable.IClickable.WalkToPoint), anywhere));			
			else
				return PowerQuest.Get.StartQuestCoroutine(CoroutineWalkTo(clickable.IClickable.WalkToPoint, anywhere));
		}
		return null;
	}
	public Coroutine WalkToClicked(bool anywhere = false) {	return PowerQuest.Get.StartCoroutine(CoroutineWalkTo(PowerQuest.Get.GetLastWalkTo(), anywhere)); }

	// Stops walking on the spot
	public Coroutine StopWalking() 
	{ 
		m_faceAfterWalk = eFace.None; 
		if ( m_instance!= null ) 
			m_instance.StopWalk(); 
		return null; 
	}	
	
	// Non-blocking versions of facing functions
	public void FaceDownBG(bool instant = false) { Face(eFace.Down, instant); }
	public void FaceUpBG(bool instant = false) { Face(eFace.Up, instant); }
	public void FaceLeftBG(bool instant = false) { Face(eFace.Left, instant); }
	public void FaceRightBG(bool instant = false) { Face(eFace.Right, instant); }
	
	public void FaceUpRightBG(bool instant = false) { Face(eFace.UpRight, instant); }
	public void FaceUpLeftBG(bool instant = false) { Face(eFace.UpLeft, instant); }
	public void FaceDownRightBG(bool instant = false) { Face(eFace.DownLeft, instant); }
	public void FaceDownLeftBG(bool instant = false) { Face(eFace.DownRight, instant); }

	public void FaceBG( eFace direction, bool instant = false ) { Face(direction,instant); }
	public void FaceBG( IQuestClickableInterface clickable, bool instant = false ) { FaceBG(clickable.IClickable, instant); }
	public void FaceBG( IQuestClickable clickable, bool instant = false ) { Face(clickable,instant);}
	public void FaceBG(float x, float y, bool instant = false) { Face(x,y, instant); }
	public void FaceBG(Vector2 location, bool instant = false) { Face(location,instant); }
	public void FaceClickedBG(bool instant = false) { FaceClicked(instant); }
	public void FaceAwayBG(bool instant = false) { FaceAway(instant); }
	public void FaceDirectionBG(Vector2 directionV2, bool instant = false) { FaceDirection(directionV2,instant); }

	// Face enum direction
	public Coroutine Face( eFace direction, bool instant = false )
	{
		if ( PowerQuest.Get.GetRoomLoading() || PowerQuest.Get.GetSkipCutscene() || m_instance == null || Visible == false )
			instant = true;		
		if ( Walking && m_turnBeforeWalking == false )
			instant = true;
		if ( Walking == false && m_turnBeforeFacing == false )
			instant = true;
		
		m_targetFaceDirection = direction;
		if ( direction != eFace.Up && direction != eFace.Down )
			m_facingVerticalFallback = CharacterComponent.ToCardinal(direction);
		
		if ( instant )
		{
			m_faceDirection = direction; // set this to snap to target
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(direction); 
			return null;
		}

		// Check for animation- if so, snap direction immediately
		if ( m_instance.StartTurnAnimation())
		{
			m_faceDirection = direction; // set this to snap to target
			if ( m_instance != null ) 
				m_instance.UpdateFacingVisuals(direction); 			
		}

		return PowerQuest.Get.StartQuestCoroutine(CoroutineFace(direction)); 
	}

	public Coroutine FaceDown(bool instant = false) { return Face(eFace.Down, instant); }
	public Coroutine FaceUp(bool instant = false) { return Face(eFace.Up, instant); }
	public Coroutine FaceLeft(bool instant = false) { return Face(eFace.Left, instant); }
	public Coroutine FaceRight(bool instant = false) { return Face(eFace.Right, instant); }

	public Coroutine FaceUpRight(bool instant = false) { return Face(eFace.UpRight, instant); }
	public Coroutine FaceUpLeft(bool instant = false) { return Face(eFace.UpLeft, instant); }
	public Coroutine FaceDownRight(bool instant = false) { return Face(eFace.DownLeft, instant); }
	public Coroutine FaceDownLeft(bool instant = false) { return Face(eFace.DownRight, instant); }
	
	public Coroutine Face( IQuestClickableInterface clickable, bool instant = false ) { return Face(clickable.IClickable, instant); }
	public Coroutine Face( IQuestClickable clickable, bool instant = false )
	{
		if ( clickable != null )
		{
			return Face(clickable.Position + clickable.LookAtPoint, instant);
		}
		return null;
	}

	// Face Location, from character's feet to coordinate. (NB: not actually a coroutine, just so yield returning this doesn't error)
	public Coroutine Face(float x, float y, bool instant = false) { return Face(new Vector2(x,y), instant);}
	public Coroutine Face(Vector2 location, bool instant = false)
	{
		return FaceDirection((location - m_position).normalized, instant);
	}
	public Coroutine FaceClicked(bool instant = false)
	{
		return Face(PowerQuest.Get.GetLastLookAt(), instant);
	}
	public Coroutine FaceAway(bool instant = false)
	{
		return FaceDirection( -FACE_DIRECTIONS[(int)GetTargetFaceDirection()], instant );		
	}

	// Face a vector direction. (NB: not actually a coroutine, just so yield returning this doesn't error)
	public Coroutine FaceDirection(Vector2 directionV2, bool instant = false)
	{	
		// Calculates the closest direction- Each direction (including diagonals) is a 30 degree segment

		if ( directionV2.sqrMagnitude <= 0 )
		{
			Debug.LogWarning("FaceDirection called with zero direction passed. Ignoring");
			return null;
		}
		int count = (int)eFace.UpRight+1;
		float cosAngle = Mathf.Cos(Mathf.Deg2Rad * FACING_ANGLE_SEGMENT_DEG);
		directionV2.Normalize();
		for ( int i = 0; i < count; ++i )
		{
			// Find match - within 30 degree tolerance
			if ( Vector2.Dot(FACE_DIRECTIONS[i],directionV2) >= cosAngle )
			{
				// If up or down- save a hint of left/right in case there's no up/down frames
				eFace face = (eFace)i;
				if ( face == eFace.Up || face == eFace.Down )
				{
					if ( directionV2.x > 0 ) 
						m_facingVerticalFallback = eFace.Right;
					else if ( directionV2.x < 0 ) 
						m_facingVerticalFallback = eFace.Left;
				}

				return Face(face, instant);
			}
		}
		return null;
	}

	/// Start charcter saying something
	public Coroutine Say(string dialog, int id = -1)
	{
		if ( m_coroutineSay != null )
		{			
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
			PowerQuest.Get.OnSay();
		}

		if ( CallbackOnSay != null )
			CallbackOnSay.Invoke(dialog,id);

		m_coroutineSay = CoroutineSay(dialog, id);
		return PowerQuest.Get.StartCoroutine(m_coroutineSay); 
	}

	// Start character saying something in the background.
	public Coroutine SayBG(string dialog, int id = -1) 
	{
		if ( m_coroutineSay != null )
		{
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
		}
		m_coroutineSay = CoroutineSayBG(dialog, id);
		return PowerQuest.Get.StartCoroutine(m_coroutineSay); 
	}

	public void CancelSay()
	{
		if ( m_coroutineSay != null )
		{			
			PowerQuest.Get.StopCoroutine(m_coroutineSay);
			EndSay();
		}
	}

	public Coroutine PlayAnimation(string animName)
	{
		if ( m_instance != null ) return PowerQuest.Get.StartCoroutine(CoroutinePlayAnimation(animName)); 
		return null;
	}

	public Coroutine WaitForAnimation()
	{
		if ( m_instance != null ) return PowerQuest.Get.StartCoroutine(CoroutineWaitForAnimation()); 
		return null;
	}


	public void PlayAnimationBG(string animName, bool pauseAtEnd = false )
	{
		if ( PowerQuest.Get.GetSkipCutscene() )
			return;
		if ( m_instance != null ) m_instance.PlayAnimation(animName, pauseAtEnd); 
	}

	public void PauseAnimation()
	{
		if ( m_instance != null ) m_instance.PauseAnimation();
	}

	public void ResumeAnimation()
	{
		if ( m_instance != null ) m_instance.ResumeAnimation();
	}

	public void StopAnimation()
	{
		if ( m_instance != null ) m_instance.StopAnimation();
	}


	public void AddAnimationTrigger(string triggerName, bool removeAfterTriggering, System.Action action )
	{
		if ( m_instance != null )
		{
			QuestAnimationTriggers triggerComponent = m_instance.GetComponent<QuestAnimationTriggers>();
			if ( triggerComponent == null )
				triggerComponent = m_instance.gameObject.AddComponent<QuestAnimationTriggers>();
			if ( triggerComponent != null )
				triggerComponent.AddTrigger(triggerName, action, removeAfterTriggering);
		}
	}

	public void RemoveAnimationTrigger(string triggerName)
	{
		if ( m_instance != null )
		{
			QuestAnimationTriggers triggerComponent = m_instance.GetComponent<QuestAnimationTriggers>();
			if ( triggerComponent != null )
				triggerComponent.RemoveTrigger(triggerName);
		}
	}

	public Coroutine WaitForAnimTrigger(string triggerName) { return PowerQuest.Get.StartCoroutine(CoroutineWaitForAnimTrigger(triggerName)); }

	public int ClickableColliderId 
	{ 
		get { return m_clickableColliderId; }
		set
		{
			m_clickableColliderId = value;
			if ( m_instance != null )
				m_instance.OnClickableColliderIdChanged();
		} 
	}


	#endregion
	#region Funcs: Private
	//
	// Internal Functions
	//


	IEnumerator CoroutineWalkTo( Vector2 position, bool anywhere = false )
	{		
		m_faceAfterWalk = eFace.None;
		EndSay();

		if ( Moveable == false )
			yield break;

		// WalkToBG( position, anywhere ); // can't use walktoBG since that stops the sequence being cancelable
		{
			if ( m_instance != null && PowerQuest.Get.GetSkipCutscene() == false )
				m_instance.WalkTo(position, anywhere);
			else 
				SetPosition(position);
		}		

		if ( PowerQuest.Get.GetSkipCutscene() )
		{
			m_instance.SkipWalk();
			yield break;
		}
			
		bool skip = false;
		//bool cancel = false;
		while ( m_instance != null && skip == false && Moveable && m_instance.GetIsWalking() )
		{
			if ( PowerQuest.Get.GetSkipCutscene() )
			{
				// when walk is skipped, character teleports to location and face direction
				skip = true;
				m_instance.SkipWalk();
			}
			else 
			{
				yield return new WaitForEndOfFrame();
			}
		}

		// After first walk, sequences are no longer cancelable
		//if ( IsPlayer ) // ADVJAM2019: Change to be any character walk.
			PowerQuest.Get.OnPlayerWalkComplete();
		
		yield break;
	}

	bool GetHiddenInRoom()
	{
		return IsPlayer && PowerQuest.Get.GetCurrentRoom().PlayerVisible == false;
	}

	IEnumerator CoroutineSay(string text, int id = -1)
	{
		StopWalking();
		if ( PowerQuest.Get.GetSkipCutscene() )
			yield break;

		StartSay( text, id );
		yield return PowerQuest.Get.WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, PowerQuest.Get.GetShouldSayTextAutoAdvance(), true);		
		EndSay();
	}


	IEnumerator CoroutineSayBG(string text, int id = -1)
	{	
		StartSay( text, id,true );
		yield return PowerQuest.Get.WaitForDialog(PowerQuest.Get.GetTextDisplayTime(text), m_dialogAudioSource, true, false);
		EndSay();
	}



	IEnumerator CoroutinePlayAnimation(string animName)
	{
		StopWalking();

		if ( PowerQuest.Get.GetSkipCutscene() )
			yield break;
		
		if ( m_instance == null ) yield break;
		m_instance.PlayAnimation(animName);
		while ( Animating && PowerQuest.Get.GetSkipCutscene() == false )
		{				
			yield return new WaitForEndOfFrame();
		}
		if ( m_instance.GetState() == eState.Animate )
			m_instance.StopAnimation();
	}

	IEnumerator CoroutineWaitForAnimation()
	{
		if ( PowerQuest.Get.GetSkipCutscene() )
			yield break;

		if ( m_instance == null ) yield break;
		while ( m_instance.GetState() == eState.Animate && PowerQuest.Get.GetSkipCutscene() == false )
		{				
			yield return new WaitForEndOfFrame();
		}
		if ( m_instance.GetState() == eState.Animate )
			m_instance.StopAnimation();		
	}

	IEnumerator CoroutineFace(eFace direction)
	{
		if ( PowerQuest.Get.GetSkipCutscene() )
		{			
			m_targetFaceDirection = direction;
			m_instance.UpdateFacingVisuals(direction);
			yield break;
		}
		bool skip = false;

		if ( m_instance != null && skip == false && Visible && PowerQuest.Get.GetSkipCutscene() == false && PowerQuest.Get.GetRoomLoading() == false )
		{
			/*if ( m_instance.StartTurnAnimation() )
			{
				// should turn instantly 

				// Using turn anim rather than normal turning
				while ( m_instance != null && skip == false && Visible && PowerQuest.Get.GetSkipCutscene() == false && PowerQuest.Get.GetRoomLoading() == false
					&& m_instance.GetPlayingTurnAnimation() )
				{
					yield return new WaitForEndOfFrame();
					m_targetFaceDirection = direction;
					m_instance.UpdateFacingVisuals(direction);
				}
				m_targetFaceDirection = direction;
				m_instance.UpdateFacingVisuals(direction);
				m_instance.EndTurnAnimation();
			}
			else */
			{
				while ( m_instance != null && skip == false && Visible && PowerQuest.Get.GetSkipCutscene() == false && PowerQuest.Get.GetRoomLoading() == false
					&& (m_targetFaceDirection != m_faceDirection || m_instance.GetPlayingTurnAnimation()))
				{
					yield return new WaitForEndOfFrame();		
				}
				//m_instance.EndTurnAnimation();
			}
		}
		m_targetFaceDirection = m_faceDirection;
		m_instance.UpdateFacingVisuals(m_faceDirection);

		yield break;
	}

	void StartSay(string line, int id = -1, bool background = false )
	{		
		//Debug.Log(Description() + ": " + line);	

		// Get tranlated string
		line = SystemText.GetDisplayText(line, id, m_scriptName);

		// Start audio (if enabled)
		SystemAudio.Stop(m_dialogAudioSource);
		if ( PowerQuest.Get.Settings.DialogDisplay != QuestSettings.eDialogDisplay.TextOnly )
		{
			m_dialogAudioSource = SystemText.PlayAudio(id, m_scriptName, (m_instance!= null?m_instance.transform:null));
		}

		// Create or set dialog text active (if enabled). TODO: Check if it's background dialog, which should be shown above head
		if ( PowerQuest.Get.Settings.SpeechStyle != eSpeechStyle.Portrait )
		{
			// Above character speech (Lucasarts style)
			bool showText = ( m_dialogAudioSource == null || PowerQuest.Get.Settings.DialogDisplay != QuestSettings.eDialogDisplay.SpeechOnly );
			if ( showText )
			{
				if (m_dialogText == null )
				{
					GameObject go = GameObject.Instantiate(PowerQuest.Get.GetDialogTextPrefab().gameObject) as GameObject;
					m_dialogText = go.GetComponent<QuestText>();
					go.GetComponent<TextMesh>().color = m_textColour;
				}
				else 
				{
					m_dialogText.gameObject.SetActive(true);
				}

				// Convert position to gui camera space
				if( PowerQuest.Get.Settings.SpeechStyle != eSpeechStyle.Caption )
				{
					Vector3 dialogWorldPos = (( m_instance != null ) ? m_instance.GetTextPosition() : m_position).WithZ(m_dialogText.transform.position.z);					
					m_dialogText.OrderInLayer = background ? -15 : -10;
					m_dialogText.AttachTo(dialogWorldPos);					
				}
			}

			if ( m_instance != null )
			{
				m_instance.StartSay(line, id);
			}
			
			if ( showText )
			{			
				m_dialogText.SetText(line);
			}
		}
		else // ( PowerQuest.Get.Settings.SpeechStyle == eSpeechStyle.Portrait )
		{
			// Show next to portrait (Sierra style)
			// TOOD: work out if should show text // bool showText = ( m_dialogAudioSource == null || PowerQuest.Get.Settings.DialogDisplay != QuestSettings.eDialogDisplay.SpeechOnly );

			Gui gui = PowerQuest.Get.GetGui("SpeechBox");
			if ( gui == null || gui.GetInstance() == null )
				return;
			gui.Visible = true;
			gui.Instance.GetComponent<GuiSpeechBoxComponent>().SetText(this, line, id);
			

		}
	}

	void EndSay()
	{	
		if ( Talking && CallbackOnEndSay != null )
			CallbackOnEndSay.Invoke();

		SystemAudio.Stop(m_dialogAudioSource);
		if ( m_dialogText != null )
		{
			m_dialogText.gameObject.SetActive(false);
		}
		if ( m_instance != null )
		{
			m_instance.EndSay();
		}
		Gui dialogGui = PowerQuest.Get.GetGui("SpeechBox");
		if ( dialogGui != null )
			dialogGui.Visible = false;
	}

	IEnumerator CoroutineWaitForAnimTrigger(string triggerName)
	{
		if ( PowerQuest.Get.GetSkipCutscene() == false )
		{
			bool hit = false;
			AddAnimationTrigger(triggerName,true,()=>hit=true);
			yield return PowerQuest.Get.WaitUntil(()=> hit || m_instance == null || m_instance.GetSpriteAnimator().Playing == false );
		}
		yield break;
	}

	#endregion

	// Handles setting up defaults incase items have been added or removed since last loading a save file
	[System.Runtime.Serialization.OnDeserializing]
	void CopyDefaults( System.Runtime.Serialization.StreamingContext sc )
	{
		QuestUtils.InitWithDefaults(this);
	}
	
}

}