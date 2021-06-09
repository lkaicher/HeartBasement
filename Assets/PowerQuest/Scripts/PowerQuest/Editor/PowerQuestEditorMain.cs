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


	#endregion
	#region Variables: Private

	#endregion
	#region Funcs: Init

	void CreateMainGuiLists()
	{
		//
		// Create reorderable lists
		//

		m_listRooms = new ReorderableList( m_powerQuest.GetRoomPrefabs(), typeof(RoomComponent),true,true,true,true) 
		{ 
			drawHeaderCallback = 	(Rect rect) => {  m_showRooms = EditorGUI.Foldout(rect, m_showRooms,"Rooms", true); },
			drawElementCallback = 	LayoutRoomGUI,
			onSelectCallback = 		SelectRoom,
			onAddCallback = 		(ReorderableList list) => 
			{	
				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Room,
					"Room", "'Bathroom' or 'CastleGarden'",  CreateRoom,
					m_gamePath + "Rooms");
			},
			onRemoveCallback =	(ReorderableList list) => { DeleteQuestObject(list.index, "Room", m_powerQuest.GetRoomPrefabs()); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }
		};

		m_listCharacters = new ReorderableList( m_powerQuest.GetCharacterPrefabs(), typeof(CharacterComponent),true,true,true,true) 
		{ 			
			drawHeaderCallback = 	(Rect rect) => {  m_showCharacters = EditorGUI.Foldout(rect, m_showCharacters,"Characters", true); },
			drawElementCallback = 	LayoutCharacterGUI,
			onSelectCallback = 		SelectCharacter,
			onAddCallback = 		(ReorderableList list) => 
			{	
				CreateCharacterWindow window = ScriptableObject.CreateInstance<CreateCharacterWindow>();
				window.SetPath( m_gamePath + "Characters" );
				window.ShowUtility();
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "Character", m_powerQuest.GetCharacterPrefabs()); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }

		};


		m_listInventory = new ReorderableList( m_powerQuest.GetInventoryPrefabs(), typeof(InventoryComponent),true,true,true,true) 
		{ 
			drawHeaderCallback = 	(Rect rect) => {  m_showInventory = EditorGUI.Foldout(rect, m_showInventory,"Inventory Items", true); },
			drawElementCallback = 	LayoutInventoryGUI,
			onSelectCallback = 		SelectInventory,
			onAddCallback = 		(ReorderableList list) => 
			{	
				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Inventory, "Inventory", "'Crowbar' or 'RubberChicken'", CreateInventory,
					m_gamePath + "Inventory");
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "Inventory", m_powerQuest.GetInventoryPrefabs()); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }
		};

		m_listDialogTrees = new ReorderableList( m_powerQuest.GetDialogTreePrefabs(), typeof(DialogTreeComponent),true,true,true,true) 
		{ 
			drawHeaderCallback = 	(Rect rect) => {  m_showDialogTrees = EditorGUI.Foldout(rect, m_showDialogTrees,"Dialog Trees", true); },
			drawElementCallback = 	LayoutGuiDialogTree,
			onSelectCallback = 		SelectDialogTree,
			onAddCallback = 		(ReorderableList list) => 
			{	

				ScriptableObject.CreateInstance< CreateQuestObjectWindow >().ShowQuestWindow(
					eQuestObjectType.Dialog, "DialogTree", "'MeetSarah' or 'Policeman2'", CreateDialogTree,
					m_gamePath + "DialogTree");
			},
			onRemoveCallback = 		(ReorderableList list) => { DeleteQuestObject(list.index, "DialogTree", m_powerQuest.GetDialogTreePrefabs()); },
			onCanRemoveCallback = (ReorderableList list) => { return Application.isPlaying == false; }

		};

		m_listGuis = new ReorderableList( m_powerQuest.GetGuiPrefabs(), typeof(GuiComponent),true,true,true,true) 
		{ 
			drawHeaderCallback = 	(Rect rect) => {  m_showGuis = EditorGUI.Foldout(rect, m_showGuis,"Guis", true); },
			drawElementCallback = 	LayoutGuiGUI,
			onSelectCallback = 		SelectGui,
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

				UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get("http://www.powerquestpowerhoof.com/public/powerquestdocs/version.txt");
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
		if ( m_powerQuest != null && m_powerQuest.GetRoomPrefabs().IsIndexValid(index))
		{
			RoomComponent itemComponent = m_powerQuest.GetRoomPrefabs()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Room, m_listRooms, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index );

				float totalFixedWidth = 60+60;//35+35;
				float offset = rect.x;
				EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().ScriptName);
				offset += rect.width - totalFixedWidth;
				float fixedWidth = 60;
				
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), Application.isPlaying ? "Teleport"  : "Scene", EditorStyles.miniButtonLeft ) )
				{
					// Load the scene
					LoadRoomScene(itemComponent);
				}
				offset += fixedWidth;
				fixedWidth = 60;
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButtonRight ) )
				{
					// Open the script
					QuestScriptEditor.Open(itemComponent);
				}
				offset += fixedWidth;
			}
		}
	}




	#endregion
	#region Gui Layout: Inventory


	void LayoutInventoryGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_powerQuest.GetInventoryPrefabs().IsIndexValid(index))
		{
			InventoryComponent itemComponent = m_powerQuest.GetInventoryPrefabs()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{
			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Inventory, m_listInventory, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0)
					+ (Application.isPlaying ? 1 : 0);
				float totalFixedWidth = 60+(34*actionCount);
				float offset = rect.x;
				EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().GetScriptName());
				offset += rect.width - totalFixedWidth;
				float fixedWidth = 60;
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}
				offset += fixedWidth;
				fixedWidth = 34;
				int actionNum = 1; // Start at 1 since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					if (  GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_INVENTORY);
					}
					offset += fixedWidth;
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// Interact
						QuestScriptEditor.Open(itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_INVENTORY);
					}
					offset += fixedWidth;
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV_INVENTORY, PowerQuestEditor.SCRIPT_PARAMS_USEINV_INVENTORY);
					}
					offset += fixedWidth;
				}
				if ( Application.isPlaying )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth+3, EditorGUIUtility.singleLineHeight), "Give", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// Debug give item to player
						itemComponent.GetData().Add();
					}
					offset += fixedWidth+3;
				}
			}
		}
	}

	#endregion
	#region Gui Layout: DialogTree

	void LayoutGuiDialogTree(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_powerQuest.GetDialogTreePrefabs().IsIndexValid(index))
		{
			DialogTreeComponent itemComponent = m_powerQuest.GetDialogTreePrefabs()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Dialog, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index );

				
				int actionCount = (Application.isPlaying ? 1 : 0);
				float totalFixedWidth = 60+(34*actionCount);

				float offset = rect.x;
				EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().GetScriptName());
				offset += rect.width - totalFixedWidth;
				float fixedWidth = 60;
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButton ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}
				offset += fixedWidth;
				fixedWidth = 34;

				if ( Application.isPlaying )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth+3, EditorGUIUtility.singleLineHeight), "Test", EditorStyles.miniButton ) )
					{
						// Debug give item to player
						PowerQuest.Get.StartDialog(itemComponent.GetData().GetScriptName());
					}
					offset += fixedWidth+3;
				}
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
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Gui, m_listDialogTrees, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index );

				float totalFixedWidth = 60;//+30;
				float offset = rect.x;
				EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().GetScriptName());
				offset += rect.width - totalFixedWidth;
				float fixedWidth = 60;
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButton ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		

				}
				offset += fixedWidth;
			}
		}
	}

	#endregion
	#region Gui Layout: Character

	void LayoutCharacterGUI(Rect rect, int index, bool isActive, bool isFocused)
	{
		if ( m_powerQuest != null && m_powerQuest.GetCharacterPrefabs().IsIndexValid(index))
		{
			CharacterComponent itemComponent = m_powerQuest.GetCharacterPrefabs()[index];
			if ( itemComponent != null && itemComponent.GetData() != null )
			{			
				QuestEditorUtils.LayoutQuestObjectContextMenu( eQuestObjectType.Character, m_listCharacters, itemComponent.GetData().GetScriptName(), itemComponent.gameObject, rect, index );

				int actionCount = (PowerQuestEditor.GetActionEnabled(eQuestVerb.Look)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Use)?1:0)
					+ (PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory)?1:0);
				float totalFixedWidth = 60 + (34 *actionCount);
				float offset = rect.x;
				EditorGUI.LabelField(new Rect(rect.x, rect.y+2, rect.width-totalFixedWidth, EditorGUIUtility.singleLineHeight), itemComponent.GetData().GetScriptName());
				offset += rect.width - totalFixedWidth;
				float fixedWidth = 60;
				if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Script", EditorStyles.miniButtonLeft ) )
				{
					// Open the script
					QuestScriptEditor.Open( itemComponent );		
				}
				offset += fixedWidth;
				fixedWidth = 34;
				//GUIStyle nextStyle = EditorStyles.miniButtonLeft;
				int actionNum = 1; // start at one since there's already a left item
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Look) )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Look", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// Lookat
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_LOOKAT, PowerQuestEditor.SCRIPT_PARAMS_LOOKAT_CHARACTER);
					}
					offset += fixedWidth;
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Use) )
				{
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Use", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// Interact
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_INTERACT, PowerQuestEditor.SCRIPT_PARAMS_INTERACT_CHARACTER);
					}
					offset += fixedWidth;
				}
				if ( PowerQuestEditor.GetActionEnabled(eQuestVerb.Inventory) )
				{					
					if ( GUI.Button(new Rect(offset, rect.y, fixedWidth, EditorGUIUtility.singleLineHeight), "Inv", QuestEditorUtils.GetMiniButtonStyle(actionNum++,actionCount+1) ) )
					{
						// UseItem
						QuestScriptEditor.Open( itemComponent, PowerQuest.SCRIPT_FUNCTION_USEINV, PowerQuestEditor.SCRIPT_PARAMS_USEINV_CHARACTER);
					}
					offset += fixedWidth;
				}
			}
		}
	}


	#endregion
	#region Functions: Private


	#endregion
	
}

}