using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{

public partial class PowerQuestEditor
{

	#region Variables: Static definitions

	#endregion
	#region Variables: Serialized

	// Reference to the prefab list, or a filtered list of the prefabs, used by Reorderable lists
	/*[SerializeField] */List<RoomComponent> m_listRoomPrefabs = null; // NB: commented out the 'serializefield' bit to see if that's what was causing invisible fields
	/*[SerializeField] */List<CharacterComponent> m_listCharacterPrefabs = null;
	/*[SerializeField] */List<InventoryComponent> m_listInventoryPrefabs = null;
	/*[SerializeField] */List<DialogTreeComponent> m_listDialogTreePrefabs = null;	

	// Whether to show filtered list of prefabs, or the whole thing
	[SerializeField] bool m_filterRooms = false;
	[SerializeField] bool m_filterCharacters = false;
	[SerializeField] bool m_filterInventory = false;
	[SerializeField] bool m_filterDialogTrees = false;

	#endregion
	#region Variables: Private

	#endregion
	#region Funcs: Init

	void ApplyFilter<T>( List<T> prefablist, ref List<T> list, ref bool filterBool) where T : MonoBehaviour
	{
		if ( filterBool ) 
			list = prefablist.FindAll(item=>IsHighlighted(item));
		if ( filterBool == false || list.Count == 0 ) 
		{
			list = prefablist;	
			filterBool = false;
		}
	}
	
	void LayoutListHeader(string name, ref bool show, ref bool filter, Rect rect)
	{
		show = EditorGUI.Foldout(new Rect(rect){width=rect.width-60}, show,name, true);
		if ( GUI.Button( new Rect(rect){x=rect.width-60, width=60 }, filter ? "Highlighted":"All", new GUIStyle(EditorStyles.miniLabel){alignment=TextAnchor.MiddleRight}) )
		{
			filter = !filter;
			CreateMainGuiLists(); // Refresh lists
		}
	}

	void CreateMainGuiLists()
	{
		//
		// Create reorderable lists
		//

		// Filters
		ApplyFilter(m_powerQuest.GetRoomPrefabs(), ref m_listRoomPrefabs, ref m_filterRooms);
		ApplyFilter(m_powerQuest.GetCharacterPrefabs(), ref m_listCharacterPrefabs, ref m_filterCharacters);
		ApplyFilter(m_powerQuest.GetInventoryPrefabs(), ref m_listInventoryPrefabs, ref m_filterInventory);
		ApplyFilter(m_powerQuest.GetDialogTreePrefabs(), ref m_listDialogTreePrefabs, ref m_filterDialogTrees);
					
		bool full = !m_filterRooms;
		m_listRooms = new ReorderableList( m_listRoomPrefabs, typeof(RoomComponent),full,true,full,full) 
		{
			drawHeaderCallback = (Rect rect) => LayoutListHeader("Rooms", ref m_showRooms, ref m_filterRooms, rect),
			drawElementCallback = 	LayoutRoomGUI,
			onSelectCallback = 		SelectRoom,
			onAddCallback = 		(ReorderableList list) => 
			{	
				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Room,
					"Room", "'Bathroom' or 'CastleGarden'",  CreateRoom,
					m_gamePath + "Rooms");
			},
			onRemoveCallback =	(ReorderableList list) => { DeleteQuestObject(list.index, "Room", m_listRoomPrefabs); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }
		};

		full = !m_filterCharacters;
		m_listCharacters = new ReorderableList( m_listCharacterPrefabs, typeof(CharacterComponent),full,true,full,full) 
		{ 			
			drawHeaderCallback = 	(Rect rect) => LayoutListHeader("Characters", ref m_showCharacters, ref m_filterCharacters, rect),
			drawElementCallback = 	LayoutCharacterGUI,
			onSelectCallback = 		SelectGameObjectFromList,
			onAddCallback = 		(ReorderableList list) => 
			{	
				CreateCharacterWindow window = ScriptableObject.CreateInstance<CreateCharacterWindow>();
				window.SetPath( m_gamePath + "Characters" );
				window.ShowUtility();
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "Character", m_listCharacterPrefabs); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }

		};

		full = !m_filterInventory;
		m_listInventory = new ReorderableList( m_listInventoryPrefabs, typeof(InventoryComponent),full,true,full,full) 
		{ 
			drawHeaderCallback = 	(Rect rect) => LayoutListHeader("Inventory", ref m_showInventory, ref m_filterInventory, rect),
			drawElementCallback = 	LayoutInventoryGUI,
			onSelectCallback = 		SelectGameObjectFromList,
			onAddCallback = 		(ReorderableList list) => 
			{	
				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Inventory, "Inventory", "'Crowbar' or 'RubberChicken'", CreateInventory,
					m_gamePath + "Inventory");
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "Inventory", m_listInventoryPrefabs); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }
		};

		full = !m_filterDialogTrees;
		m_listDialogTrees = new ReorderableList( m_listDialogTreePrefabs, typeof(DialogTreeComponent),full,true,full,full) 
		{ 
			drawHeaderCallback = 	(Rect rect) => LayoutListHeader("Dialog Trees", ref m_showDialogTrees, ref m_filterDialogTrees, rect),
			drawElementCallback = 	LayoutGuiDialogTree,
			onSelectCallback = 		SelectGameObjectFromList,
			onAddCallback = 		(ReorderableList list) => 
			{	

				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Dialog, "DialogTree", "'MeetSarah' or 'Policeman2'", CreateDialogTree,
					m_gamePath + "DialogTree");
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "DialogTree", m_listDialogTreePrefabs); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }

		};

		m_listGuis = new ReorderableList( m_powerQuest.GetGuiPrefabs(), typeof(GuiComponent),true,true,true,true) 
		{ 
			drawHeaderCallback = 	(Rect rect) => {  m_showGuis = EditorGUI.Foldout(rect, m_showGuis,"Guis", true); },
			drawElementCallback = 	LayoutGuiGUI,
			onSelectCallback = 		SelectGameObjectFromList,
			onAddCallback = 		(ReorderableList list) => 
			{	
				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Gui, "Gui", "'Toolbar' or 'InventoryBox'", CreateGui,
					m_gamePath + "Gui");
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "Gui", m_powerQuest.GetGuiPrefabs()); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }
		};

	}

	#endregion
	#region Gui Layout: Main


	void OnGuiMain()
	{
		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

		GUILayout.Space(5);

		if ( GUILayout.Button("Global Script") )
		{
			QuestScriptEditor.Open(PATH_GLOBAL_SCRIPT, PowerQuest.GLOBAL_SCRIPT_NAME, QuestScriptEditor.eType.Global );
		}

		// Layout rooms
		if ( m_showRooms && m_listRooms != null )
			m_listRooms.DoLayoutList();
		else 
			m_showRooms = EditorGUILayout.Foldout(m_showRooms,"Rooms", true); 
		GUILayout.Space(5);

		// Layout characters
		if ( m_showCharacters && m_listCharacters != null) 
			m_listCharacters.DoLayoutList();
		else  
			m_showCharacters = EditorGUILayout.Foldout(m_showCharacters,"Characters", true); 
		GUILayout.Space(5);

		// Layout Inventory
		if ( m_showInventory && m_listInventory != null ) 
			m_listInventory.DoLayoutList();
		else  
			m_showInventory = EditorGUILayout.Foldout(m_showInventory,"Inventory Items", true); 		
	
		GUILayout.Space(5);

		// Layout Dialogs
		if ( m_showDialogTrees && m_listDialogTrees != null ) 
			m_listDialogTrees.DoLayoutList();
		else 
			m_showDialogTrees = EditorGUILayout.Foldout(m_showDialogTrees,"Dialog Trees", true); 	

		GUILayout.Space(5);

		// Layout Gui
		if ( m_showGuis && m_listGuis != null ) 
			m_listGuis.DoLayoutList();
		else 
			m_showGuis = EditorGUILayout.Foldout(m_showGuis,"Guis", true); 	

		LayoutManual();

		EditorGUILayout.EndScrollView();
	}

	void LayoutManual()
	{
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Open Editor Manual") )
			Application.OpenURL(Path.GetFullPath("Assets/PowerQuest/PowerQuest-Manual.pdf"));
		if ( GUILayout.Button("Open Scripting API") )
			Application.OpenURL("http://powerquest.powerhoof.com/apipage.html");
		GUILayout.EndHorizontal();
		GUILayout.Space(5);	

		LayoutVersion();
				
	}

	void LayoutVersion()
	{
		GUILayout.Space(15);	
		if ( m_powerQuest != null )
		{
			//
			// Update
			// 
			System.DateTime nextCheck = System.DateTime.FromFileTimeUtc( m_newVersionCheckTime );
			nextCheck = nextCheck.AddDays(1);
			//nextCheck = nextCheck.AddSeconds(20);
			if ( System.DateTime.Compare(nextCheck,System.DateTime.UtcNow) < 0 )
			{
				// Time for another check
				Debug.Log("Checking for Powerquest Update");
				m_newVersionCheckTime = System.DateTime.UtcNow.ToFileTimeUtc();

				UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get("http://powerquest.powerhoof.com/version.txt");
				request.SendWebRequest().completed += ((item)=>
				{ 
					#if UNITY_2020_1_OR_NEWER
					if ( request.isDone && request.result != UnityEngine.Networking.UnityWebRequest.Result.ProtocolError )
					#else
					if ( request.isDone && request.isHttpError == false )
					#endif
					{
						string text = request.downloadHandler.text;
						int newVersion = Version(text);
						if ( newVersion > 0 && newVersion != m_powerQuest.EditorNewVersion )
						{
							m_powerQuest.EditorNewVersion = newVersion;
							EditorUtility.SetDirty(m_powerQuest);	
							if ( newVersion > m_powerQuest.EditorGetVersion() )
							{
								EditorUtility.DisplayDialog("PowerQuest Update Available!",
										"A new version of PowerQuest is available. (v"+Version(m_powerQuest.EditorNewVersion)+")\n\nTo update, click the Open Scripting API button, or go to http://powerquest.powerhoof.com",
										"Ok");				
							}
						}
					} 
				});

			}

			if ( m_powerQuest.EditorNewVersion > m_powerQuest.EditorGetVersion() )
			{	
				Rect rect = EditorGUILayout.GetControlRect(false, 20 );
				EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );			
				string text = "PowerQuest v"+Version( m_powerQuest.EditorGetVersion() ) + " <color=#4444c1>(v"+Version( m_powerQuest.EditorNewVersion )+" available)</color>";
				if (  GUI.Button(rect,text, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { richText = true, hover={ textColor=Color.white} } ) )
					Application.OpenURL("http://powerquest.powerhoof.com/version_history.html");					

			}
			else 
			{
				GUILayout.Label("PowerQuest v"+Version( m_powerQuest.EditorGetVersion() ), EditorStyles.centeredGreyMiniLabel );	
			}


		}
	}

	#endregion
	#region Gui Layout: Rooms

	void LayoutRoomGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_listRoomPrefabs.IsIndexValid(index))
		{
			RoomComponent itemComponent = m_listRoomPrefabs[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Room, m_listRooms, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true, (menu,prefab) =>
				{
					// Trying out adding scripts to context menu. If useful, add it to other lists.
					menu.AddSeparator(string.Empty);
					string path="Scripts/";
					RoomComponent component = prefab.GetComponent<RoomComponent>();
					menu.AddItem(path+"On Enter Room",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoom","", false) );
					menu.AddItem(path+"On Enter Room (After fading in)",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnEnterRoomAfterFade") );
					menu.AddItem(path+"On Exit Room",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnExitRoom", " IRoom oldRoom, IRoom newRoom ") );
					menu.AddItem(path+"Update (Blocking)",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "UpdateBlocking") );
					menu.AddItem(path+"Update",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "Update","", false) );
					menu.AddItem(path+"On Any Click",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnAnyClick") );
					menu.AddItem(path+"On Walk To",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnWalkTo") );
					menu.AddItem(path+"Post-Restore Game",true, () => QuestScriptEditor.Open( component, QuestScriptEditor.eType.Room, "OnPostRestore", " int version ", false) );					
				});

				float totalFixedWidth = 60+50+22;
				rect.width -= totalFixedWidth;
				
				EditorGUI.LabelField(rect, itemComponent.GetData().ScriptName, ((m_filterRooms == false && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );
				rect.y += 2;
				rect = rect.SetNextWidth(60);				
				if ( GUI.Button(rect, Application.isPlaying ? "Teleport"  : "Scene", EditorStyles.miniButtonLeft ) )
				{
					// Load the scene
					LoadRoomScene(itemComponent);
				}
				rect = rect.SetNextWidth(50);		
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonMid ) )
				{
					// Open the script
					QuestScriptEditor.Open(itemComponent);
				}
				
				rect = rect.SetNextWidth(22);		
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Room, m_listRooms, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, false );
				
			}
		}
	}




	#endregion
	#region Gui Layout: Inventory


	void LayoutInventoryGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_listInventoryPrefabs.IsIndexValid(index))
		{
			InventoryComponent itemComponent = m_listInventoryPrefabs[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Inventory, m_listInventory, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0)
					+ (Application.isPlaying ? 1 : 0);
				float fixedWidth = 34;
				float totalFixedWidth = 50+(fixedWidth*actionCount)+22;
				actionCount += 2;

				rect.width -= totalFixedWidth;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterInventory == false && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );

				rect.y += 2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}

				int actionNum = 1; // Start at 1 since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					rect = rect.SetNextWidth(fixedWidth);
					if (  GUI.Button(rect, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_INVENTORY);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					rect = rect.SetNextWidth(fixedWidth);
					if ( GUI.Button(rect, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Interact
						QuestScriptEditor.Open(itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_INVENTORY);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{
					rect = rect.SetNextWidth(fixedWidth);
					if ( GUI.Button(rect, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_USEINV_INVENTORY);
					}
				}
				if ( Application.isPlaying )
				{
					rect = rect.SetNextWidth(37);
					if ( GUI.Button(rect, "Give", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Debug give item to player
						itemComponent.GetData().Add();
					}
				}

				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Inventory, m_listInventory, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: DialogTree

	void LayoutGuiDialogTree(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_listDialogTreePrefabs.IsIndexValid(index))
		{
			DialogTreeComponent itemComponent = m_listDialogTreePrefabs[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Dialog, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );
								
				int actionCount = (Application.isPlaying ? 1 : 0);
				float totalFixedWidth = 50+(34*actionCount)+22;
				actionCount+=1;

				rect.width -= totalFixedWidth;

				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterDialogTrees == false && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );
				
				rect.y += 2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}

				if ( Application.isPlaying )
				{
					rect = rect.SetNextWidth(37);
					if ( GUI.Button(rect, "Test", EditorStyles.miniButtonMid ) )
					{
						// Debug give item to player
						PowerQuest.Get.StartDialog(itemComponent.GetData().GetScriptName());
					}
				}
				
				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Dialog, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: Gui

	void LayoutGuiGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_powerQuest.GetGuiPrefabs().IsIndexValid(index))
		{
			GuiComponent itemComponent = m_powerQuest.GetGuiPrefabs()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listGuis, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				float totalFixedWidth = 50+50+22;//+30;
				
				rect.width -= totalFixedWidth;
				rect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName() );

				rect.y = rect.y+2;

				rect = rect.SetNextWidth(50);
				
				if ( GUI.Button(rect, "Edit", EditorStyles.miniButtonLeft ) )
				{
					// Stage the prefab, and switch to Gui tab	
					Selection.activeObject = itemComponent.gameObject;
					AssetDatabase.OpenAsset(itemComponent.gameObject);									
					m_selectedTab = eTab.Gui;
					GUIUtility.ExitGUI();

				}
				
				rect = rect.SetNextWidth(50);
				
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonMid) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}
				
				rect = rect.SetNextWidth(22);

				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listGuis, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}

	#endregion
	#region Gui Layout: Character

	void LayoutCharacterGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_listCharacterPrefabs.IsIndexValid(index))
		{
			CharacterComponent itemComponent = m_listCharacterPrefabs[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Character, m_listCharacters, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index, true );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
				float totalFixedWidth = 50 + (34 *actionCount)+22;
				actionCount+=2;
				
				rect.width -= totalFixedWidth;
				EditorGUI.LabelField(rect, itemComponent.GetData().GetScriptName(), ((m_filterCharacters == false && IsHighlighted(itemComponent))?EditorStyles.whiteLabel:EditorStyles.label) );
				
				rect.y = rect.y+2;
				rect = rect.SetNextWidth(50);
				if ( GUI.Button(rect, "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}

				//GUIStyle nextStyle = EditorStyles.miniButtonLeft;
				int actionNum = 1; // start at one since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					rect = rect.SetNextWidth(34);
					if ( GUI.Button(rect, "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_CHARACTER);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					rect = rect.SetNextWidth(34);
					if ( GUI.Button(rect, "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// Interact
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_CHARACTER);
					}
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{				
					rect = rect.SetNextWidth(34);	
					if ( GUI.Button(rect, "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV, PowerQuestEditor.SCRIPT_PARAMS_USEINV_CHARACTER);
					}
				}
				
				rect = rect.SetNextWidth(22);
				if ( GUI.Button(rect, "...", EditorStyles.miniButtonRight ) )
					QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Character, m_listCharacters, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index,false );
				
			}
		}
	}


	#endregion
	#region Functions: Private
	
	// Selects the game object in the project view from the passed in list of prefabs
	void SelectGameObjectFromList(ReorderableList list)
	{
		if ( list.index >= 0 && list.index < list.list.Count )
		{
			MonoBehaviour component = list.list[list.index] as MonoBehaviour;
			if ( component != null )
			{
				// Was trying 'auto' focuseing project window so you didn't need it open always... it's kinda annoying though
				//if ( PrefabUtility.GetPrefabInstanceStatus(component) == PrefabInstanceStatus.NotAPrefab )  // This confusing statement checks that the it's not an instance of a prefab (therefore is found in the project)
				//	EditorUtility.FocusProjectWindow();

				Selection.activeObject = component.gameObject;
				GUIUtility.ExitGUI();
			}
		}
	}

	#endregion
	
}

}
