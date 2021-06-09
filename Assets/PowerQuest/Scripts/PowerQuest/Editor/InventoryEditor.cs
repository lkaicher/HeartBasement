using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.Quest
{


[CanEditMultipleObjects]
[CustomEditor(typeof(InventoryComponent))]
public class InventoryComponentEditor : Editor 
{

	public void OnEnable()
	{
		InventoryComponent component = (InventoryComponent)target;
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
		DrawDefaultInspector();
		InventoryComponent component = (InventoryComponent)target;
		if ( component == null ) return;

		EditorGUILayout.LabelField("Script Functions", EditorStyles.boldLabel);
		if ( GUILayout.Button("Edit Script") )
		{
			// Open the script
			QuestScriptEditor.Open( component );	
		}

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

		EditorGUI.BeginDisabledGroup( Application.isPlaying == false || PowerQuest.Exists == false );
		GUILayout.BeginHorizontal();
		if ( GUILayout.Button("Give Item") )
		{			
			component.GetData().Add();
		}
		else if ( GUILayout.Button("Remove Item"))
		{
			component.GetData().Remove();
		}
		GUILayout.EndHorizontal();
		EditorGUI.EndDisabledGroup();

		GUILayout.Space(5);
		EditorGUILayout.LabelField("Utils", EditorStyles.boldLabel);


		if ( GUILayout.Button("Rename") )
		{
			ScriptableObject.CreateInstance< RenameQuestObjectWindow >().ShowQuestWindow(
				component.gameObject, eQuestObjectType.Inventory, component.GetData().ScriptName, PowerQuestEditor.OpenPowerQuestEditor().RenameQuestObject );
		}

		if (GUI.changed)
			EditorUtility.SetDirty(target);
		
	}


}

}