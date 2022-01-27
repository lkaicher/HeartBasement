using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace PowerTools.Quest
{



// Use this for initializatio
[CustomEditor(typeof(AudioCue))]
[CanEditMultipleObjects]
public class AudioCueEditor : Editor {
		
	AudioCue m_object;
	List<AudioCue.Clip> m_items;
	SerializedObject m_targetObject = null;
	SerializedProperty m_listProperty = null;
	string m_fileSearchPattern = "";

	bool m_foldoutAdvanced = false;

	void OnEnable()
	{
		
		m_object = (AudioCue)target;
		m_items = m_object.m_sounds;
		m_targetObject = new SerializedObject( target );	
		m_listProperty = m_targetObject.FindProperty("m_sounds");
		
		if ( m_object != null && m_object.name.Length > 0 )
		{
			m_fileSearchPattern =  (m_object.name + "*.wav").ToLower();
		}
		
	}
	
	override public void OnInspectorGUI() 
	{		
		m_object = (AudioCue)target;
		m_items = m_object.m_sounds;
		m_targetObject = new SerializedObject( target );
		m_listProperty = m_targetObject.FindProperty("m_sounds");
		
		EditorGUILayout.BeginHorizontal();
		
			if ( GUILayout.Button("Play") && m_object )
			{
				if ( Application.isEditor )
				{
					if ( SystemAudio.GetValid() )
					{
						GameObject.DestroyImmediate( SystemAudio.Get.gameObject );
					}
				}
				foreach( Object targ in targets )
					SystemAudio.Play(targ as AudioCue);
			}
		
			if ( GUILayout.Button("Stop")  )
			{
				if ( SystemAudio.GetValid() )
				{
					GameObject.DestroyImmediate( SystemAudio.Get.gameObject );
				}
			}
		EditorGUILayout.EndHorizontal();

		if( GUILayout.Button("Add cue to Audio System"))
		{
			string[] strResults = AssetDatabase.FindAssets("SystemAudio");
			SystemAudio audioSystem = null;
			if ( strResults.Length > 0 )
			{
				audioSystem = AssetDatabase.LoadAssetAtPath<SystemAudio>(AssetDatabase.GUIDToAssetPath(strResults[0]));
			}
			if ( audioSystem == null )
			{
				Debug.LogWarning("Couldn't find SystemAudio");
			}
			else 
			{
				foreach( Object targ in targets )
				{
					
					AudioCue cue = targ as AudioCue;
					if ( cue == null )					
					{
						Debug.LogWarning("Cue is null");
					}					
					#if UNITY_2018_3_OR_NEWER							
					else if ( cue.gameObject.scene.IsValid() )
					{
						Debug.LogWarning("Can't add cues from prefab editor");
						EditorUtility.DisplayDialog("Couldn't add cue", "You can't add cues from the prefab editor.\n\nIn 2019+ you can just do it from regular inspector. In 2018.3+ drag cues manually into the list in SystemAudio prefab. (Blame unity for breaking things)","Ok");
					}
					#endif
					else 
					{

						Undo.RecordObject(audioSystem, "Adding cue to audio system");	
						if ( audioSystem.EditorAddCue( cue ) )
						{
							Debug.Log("Added cue");
							EditorUtility.SetDirty(audioSystem);
						}
						else 
						{
							Debug.Log("Cue already existed");
						}
					}
				}
			}
		}

		SerializedProperty prop = m_targetObject.GetIterator();
		if ( prop.Next(true) )
		{
			// Skip built in properites					
			for ( int i = 0; i < 9 && prop.Next (false); ++i )	{}
			
			while ( prop.Next (false) )
			{				
				if ( prop.editable && prop.name != "m_sounds" && prop.name != "m_totalWeight"  )
				{
					EditorGUILayout.PropertyField( prop );
				}
			}
		}

		if ( targets.Length <= 1 )
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Sound Clips:", EditorStyles.boldLabel);
			EditorUtils.UpdateListInspector<AudioCue.Clip>( ref m_items, null, new EditorUtils.CreateListItemGUIDelegate(BuildSpawnItemInspector),  null );				
			DropAreaGUI();
		}
		EditorGUI.indentLevel = 0;

		EditorGUILayout.Space();

		if ( targets.Length <= 1 )
		{
			m_foldoutAdvanced = EditorGUILayout.Foldout(m_foldoutAdvanced,"Advanced");
			if ( m_foldoutAdvanced )
			{
				EditorGUILayout.Space();
				AutoImportCuesGUI();
			}
		}
		
		if ( GUI.changed )
		{
			m_object.GetShuffledIndex().SetWeights( m_object.m_sounds, (item)=>item.m_weight );		
			
        	m_targetObject.ApplyModifiedProperties();	
			EditorUtility.SetDirty(target);					
			
			m_targetObject = new SerializedObject( target );	
			m_listProperty = m_targetObject.FindProperty("m_sounds");
		}
	}


	private void DropAreaGUI()
	{
		var evt = Event.current;
		var dropArea = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
		GUI.Box(dropArea, "Drop sounds here to add");
		
		switch (evt.type)
		{
		case EventType.DragUpdated:
		case EventType.DragPerform:
		{
			if ( dropArea.Contains(evt.mousePosition))
			{
				
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				
				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					foreach(var draggedObject in DragAndDrop.objectReferences)
					{
						AudioClip clip = draggedObject as AudioClip;
						if ( !clip )
							continue;
						m_items.Add(new AudioCue.Clip() { m_sound = clip } );
						GUI.changed = true;
					}
				}
				
				Event.current.Use();
				EditorUtility.SetDirty(target);

			}
		} break;
		}
	}


	void AutoImportCuesGUI()
	{
		m_fileSearchPattern = EditorGUILayout.TextField("File Search Pattern", m_fileSearchPattern);

		if( GUILayout.Button("Load Matching Files"))
		{

			// Clear all existing items first
			m_items.Clear();

			string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_object));

			DirectoryInfo directory = new DirectoryInfo(path);

			FileInfo[] info = directory.GetFiles (m_fileSearchPattern);

			foreach (FileInfo f in info)
			{
				
				string assetPath = MakeRelative(f.FullName,Application.dataPath);

				//Debug.Log ();

				AudioClip clip = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AudioClip)) as AudioClip;

				if( !clip )
				{
					Debug.Log ("asset didn't load");

					continue;
				}
				else
					Debug.Log(clip.name);

				// search for duplicate name
				bool duplicateItem = false;

				foreach(AudioCue.Clip item in m_items)
				{
					if(item.m_sound.name == clip.name)
					{
						duplicateItem = true;
					}
				}

				if (!duplicateItem)
				{
					m_items.Add(new AudioCue.Clip() { m_sound = clip } );
				}
			
			}

			
		}

	}
	
	// Delegate
	void BuildSpawnItemInspector( int i )
	{		
		EditorGUILayout.BeginVertical();		
			m_items[i].m_weight = EditorGUILayout.Slider( (100.0f*m_object.GetShuffledIndex().GetRatio(i)).ToString ("0.00")+"%", m_items[i].m_weight, 0,m_object.GetShuffledIndex().GetMaxWeight()*1.2f);				
			m_items[i].m_sound = EditorGUILayout.ObjectField("", (Object)m_items[i].m_sound, typeof(AudioClip), false) as AudioClip; // false is "allowSceneObjects"
			
			if ( i >= m_listProperty.arraySize )
			{
				m_targetObject = new SerializedObject( target );	
				m_listProperty = m_targetObject.FindProperty("m_sounds");
			}
		
			if ( i < m_listProperty.arraySize )
			{
				SerializedProperty listItem = m_listProperty.GetArrayElementAtIndex(i);		
				EditorGUILayout.PropertyField( listItem, new GUIContent("Data"), true );	
			}
		
		EditorGUILayout.EndVertical();
		
	}

	public static string MakeRelative(string filePath, string referencePath)
	{
		try 
		{
			var fileUri = new System.Uri(filePath);
			var referenceUri = new System.Uri(referencePath);
			if ( referenceUri.IsAbsoluteUri)
				return referenceUri.MakeRelativeUri(fileUri).ToString();
		}
		catch (System.Exception e)
		{
			if ( e != null ){}
		}
		return filePath;
	}

	[MenuItem("Assets/Create Audio Cue from Clips #%&c",true)]
	static bool ContextCreateAudioCueValidate(MenuCommand command)
	{		
		if ( Selection.objects.Length < 0 )
			return false;
		return System.Array.Find(Selection.objects, item=>item is AudioClip);

		//return (command.context as AudioClip) != null;
	}

	[MenuItem("Assets/Create Audio Cue from Clips #%&c",false,32)]
	static void ContextCreateAudioCue(MenuCommand command)
	{
		// Create object with audio cue component

		// Add all selected cues to object
		Object[] clipObjs = System.Array.FindAll(Selection.objects, item=>item is AudioClip);
		AudioClip[] clips = System.Array.ConvertAll(clipObjs, item=>item as AudioClip);
		if ( clips.Length <= 0 )
			return;


		// Sort sprites by name and insert
		using ( PowerTools.Anim.NaturalComparer comparer = new PowerTools.Anim.NaturalComparer() )
		{
			System.Array.Sort(clips, (a, b) => comparer.Compare(a.name,b.name) );
		}

		string path = AssetDatabase.GetAssetPath(clips[0]);
		path = Path.GetDirectoryName(path) + "/Sound" + Path.GetFileNameWithoutExtension(path) +".prefab";
	
		// Create the audio cue
		GameObject go = new GameObject();
		AudioCue cue = go.AddComponent<AudioCue>();

		// Add the clips
		foreach ( AudioClip clip in clips )
		{
			cue.m_sounds.Add( new AudioCue.Clip() { m_sound = clip, m_weight = 100 } );
		}
		cue.GetShuffledIndex().SetWeights( cue.m_sounds, (item)=>item.m_weight );		

		// Set mask as "sound"
		cue.m_type = (int)AudioCue.eAudioType.Sound;

		// Create the prefab

		#if UNITY_2018_3_OR_NEWER
			Object p = PrefabUtility.SaveAsPrefabAsset(go,path);
		#else
			Object p = PrefabUtility.CreateEmptyPrefab(path);
			p = PrefabUtility.ReplacePrefab(go, p, ReplacePrefabOptions.ConnectToPrefab);			
		#endif
		GameObject.DestroyImmediate(go);

		// Select the cue
		Selection.activeObject = p;
		//EditorGUIUtility.PingObject(p);

	}
	
}
 
 
}
