// Created by Dave Lloyd (@duzzondrums) for Powerhoof - http://tools.powerhoof.com for updates

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerTools
{

[CustomEditor(typeof(PowerSpriteImport))]
public class PowerSpriteImportEditor : Editor 
{
	#region Definitions
	static readonly string LIST_PROPERTY_NAME = "m_animations";
	static readonly string REGEX_PNG = @"(?<name>.*)_(?<id>\d+)\.png";
	static readonly string[] ASEPRITE_PATHS = { @"C:\Program Files\Aseprite\Aseprite.exe", @"C:\Program Files (x86)\Aseprite\Aseprite.exe", @"C:\Program Files (x86)\Steam\SteamApps\common\Aseprite\Aseprite.exe" };

	#endregion 
	#region Variables: Private

	[SerializeField] bool m_showSpriteImporterSettings = false;

	PowerSpriteImport m_component;
	List<PowerSpriteImport.AnimImportData> m_items;	
	string m_asepritePath = "";
	
	ReorderableList m_list;

	#endregion
	#region Context menu items

	[MenuItem("Assets/Create/Power Sprite Importer")]
	public static void CreateImporter()
	{
		PowerSpriteImport asset = ScriptableObject.CreateInstance<PowerSpriteImport> ();
 
		string path = AssetDatabase.GetAssetPath (Selection.activeObject);
		if (path == "") 
		{
			path = "Assets";
		} 
		else if (Path.GetExtension (path) != "") 
		{
			path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
		}
 
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/SpriteImporter.asset");
 
		AssetDatabase.CreateAsset (asset, assetPathAndName);
 
		AssetDatabase.SaveAssets ();
        	AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
	}

	public static PowerSpriteImport CreateImporter(string path)
	{
		PowerSpriteImport asset = ScriptableObject.CreateInstance<PowerSpriteImport>();

		AssetDatabase.CreateAsset(asset, path);
		AssetDatabase.SaveAssets();
	    AssetDatabase.Refresh();
		return asset;
	}

	[MenuItem("Assets/Import Sprites from Photoshop",true)]
	static bool ContextImportFromPhotoshopValidate(MenuCommand command) { return Selection.activeObject != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject)) == false; }

	[MenuItem("Assets/Import Sprites from Photoshop",false,20)]
	static void ContextImportFromPhotoshop(MenuCommand command)
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if ( string.IsNullOrEmpty(Path.GetExtension(path)) == false )
			path = Path.GetDirectoryName(path); // if selected object path is to a file (not a directory) change it to the directory path.

		EditorUtility.DisplayProgressBar("Exporting from Photoshop", "Exporting from currently open photoshop file",0.8f);

		// Find vbs script path
		string[] guids = AssetDatabase.FindAssets("PSDExport");
		Debug.Assert(guids.Length > 0,"Couldn't find PSDExport.vbs");
		string exporterPath = Path.GetFullPath( AssetDatabase.GUIDToAssetPath(guids[0]) );

		// Runs script
		System.Diagnostics.Process process = new System.Diagnostics.Process();
		process.StartInfo.FileName = exporterPath;
		process.StartInfo.Arguments = "active" + " " + Path.GetFullPath(path);
		process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
		process.Start();
		// Wait up to 20 seconds for exit
		process.WaitForExit(20000);
		EditorUtility.ClearProgressBar();
	}


	[MenuItem("Assets/Create/Animation From Sprites",true)]
	static bool ContextCreateAnimationFromSpritesValidate(MenuCommand command) { return Selection.activeObject != null && Selection.activeObject is Texture; }

	[MenuItem("Assets/Create/Animation From Sprites",false,410)]
	static void ContextCreateAnimationFromSprites(MenuCommand command)
	{
		PowerSpriteImportEditor.CreateAnimationsFromSelected();
	}

	#endregion
	#region Functions: Init

	void OnEnable()
	{
	
		m_component = (PowerSpriteImport)target;
		m_items = m_component.m_animations;
		
		if ( Directory.Exists(m_component.m_sourceDirectory) == false )
			m_component.m_sourceDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

		RecalcLengths();
						
		m_list = new ReorderableList(serializedObject, serializedObject.FindProperty(LIST_PROPERTY_NAME),true, true, true, true);
		m_list.drawHeaderCallback = (Rect rect) => {  
			EditorGUI.LabelField(new Rect(rect) {width = rect.width}, "Animation Name");
			EditorGUI.LabelField(new Rect(rect) {x = rect.width-47, width=100}, "First Frame");
		};
		
		m_list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
		{			
			SerializedProperty listItem = m_list.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField( new Rect(rect.x, rect.y, rect.width-45, EditorGUIUtility.singleLineHeight),
				listItem.FindPropertyRelative("m_name"), GUIContent.none);
			if ( EditorGUI.EndChangeCheck() )
					m_list.index = index;
			//EditorGUI.LabelField(new Rect(rect.x + rect.width-90, rect.y, 90, EditorGUIUtility.singleLineHeight), "1st frame:");
			EditorGUI.BeginChangeCheck();
			m_items[index].m_firstFrame = EditorGUI.DelayedIntField( new Rect(rect.x + rect.width-40, rect.y, 40, EditorGUIUtility.singleLineHeight), m_items[index].m_firstFrame);
			if ( EditorGUI.EndChangeCheck() )
            {
            	// Recalc previous length since it will have changed
				if ( index > 0 )
					m_items[index-1].m_length = Mathf.Max(1, m_items[index].m_firstFrame - m_items[index-1].m_firstFrame);
            	// Recalc first frames
            	RecalcFirstFrames();
            }

		};
		m_list.onAddCallback += (list) => 
		{			
			if ( m_list.index < 0 || m_list.index >= m_list.count-1 )
			{
				m_items.Add(new PowerSpriteImport.AnimImportData());
				m_list.index = m_list.index+1;
			}
			else 
			{
				m_items.Insert( m_list.index+1, new PowerSpriteImport.AnimImportData() );
				m_list.index = m_list.index+1;
			}
			RecalcFirstFrames();

		};
		m_list.onRemoveCallback += (list) =>
		{
			int index = m_list.index;
			m_items.RemoveAt(m_list.index);
			RecalcFirstFrames();
			index--;
			if ( index >= 0 )
				m_list.index = index;

		}; 
		m_list.onReorderCallback += (list) => 
		{
			RecalcFirstFrames();
		};
	}


	#endregion
	#region Functions: GUI Layout

	override public void OnInspectorGUI() 
	{	
		if ( m_list == null )
			return;
		
		GUILayout.Space(10);

		//
		// PSD/Asprites source file
		//
		EditorGUILayout.LabelField("Source Image File", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();	
		m_component.m_sourcePSD = EditorGUILayout.TextField(m_component.m_sourcePSD);				
				
		if ( GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(25)) )
		{		
			string result = EditorUtility.OpenFilePanel("Select source PSD",m_component.m_sourcePSD,"");
			if ( string.IsNullOrEmpty(result) == false ) m_component.m_sourcePSD = result;
			EditorGUIUtility.keyboardControl = -1;
		}
		if ( string.IsNullOrEmpty(m_component.m_sourcePSD) == false )
		{
			m_component.m_sourcePSD = MakeRelative(m_component.m_sourcePSD, Application.dataPath);
		}	
		if ( string.IsNullOrEmpty(m_component.m_sourcePSD) == false )
		{
			if ( GUILayout.Button("Open in Editor", EditorStyles.miniButton ) )
			{
				if ( string.IsNullOrEmpty(m_component.m_sourcePSD) == false )
				{
					if ( File.Exists(Application.dataPath+"/../"+m_component.m_sourcePSD) )
					{				
						Application.OpenURL(Application.dataPath+"/../"+m_component.m_sourcePSD);
					}
					else 
					{
						EditorUtility.DisplayDialog("Couldn't find file","The file isn't there anymore. Make sure the relative path from project directory is the same as when you set it.","OK");
					}
				}
				else
				{
					if ( EditorUtility.DisplayDialog("Path not set","You need to set the path first!","Sure", "Maybe later") )
					{
						string result = EditorUtility.OpenFilePanel("Select source PSD",m_component.m_sourcePSD,"");
						if ( string.IsNullOrEmpty(result) == false ) m_component.m_sourcePSD = result;
					}
				}
			}	
		}
		EditorGUILayout.EndHorizontal();


		EditorGUILayout.Space();

		//
		// Source Directory/Import
		//
		/* Uncomment if you want to import from a folder instead of directly from photoshop/aseprite. Files should be in this format: <name>_1.png, <name>_2.png... /
		if ( string.IsNullOrEmpty(m_component.m_sourcePSD) )
		{
			EditorGUILayout.LabelField("Source Image Folder", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			m_component.m_sourceDirectory = EditorGUILayout.TextField( "Source PNG Directory", m_component.m_sourceDirectory );
			if ( GUILayout.Button("...",  GUILayout.Width(25)) )
			{
				string result = EditorUtility.OpenFolderPanel("Select Directory with Source PNGs",m_component.m_sourceDirectory,"");
				if ( string.IsNullOrEmpty(result) == false ) m_component.m_sourceDirectory = result;
				EditorGUIUtility.keyboardControl = -1;

			}	
			EditorGUILayout.EndHorizontal();
			m_component.m_deleteImportedPngs = EditorGUILayout.Toggle("Delete source PNGs", m_component.m_deleteImportedPngs);
		}

		GUILayout.Space(10);
		/**/

		//
		// List
		//	
		EditorGUILayout.LabelField("Animation Specification", EditorStyles.boldLabel);
		serializedObject.Update();
		m_list.DoLayoutList();

		EditorGUILayout.BeginHorizontal();
		bool importPNGs = GUILayout.Button( "Import Sprites" );
		bool createAnimations = GUILayout.Button( "Create Animations" );
		EditorGUILayout.EndHorizontal();	

		//	EditorGUILayout.HelpBox("NB: If animation is shortened, the extra sprites won't be deleted automatically. Delete them manually.", MessageType.None);
		
		EditorGUILayout.Space();
		m_showSpriteImporterSettings = EditorGUILayout.Foldout(m_showSpriteImporterSettings, "Advanced Settings");
		if ( m_showSpriteImporterSettings )
		{
			m_component.m_spriteDirectory = EditorGUILayout.TextField("Sprite Folder", m_component.m_spriteDirectory );	
			m_component.m_gui = EditorGUILayout.Toggle("GUI Sprite", m_component.m_gui);
			m_component.m_packingTag = EditorGUILayout.TextField("Packing Tag", m_component.m_packingTag);
			m_component.m_pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", m_component.m_pixelsPerUnit);
			m_component.m_filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", (System.Enum)m_component.m_filterMode);
			m_asepritePath = EditorGUILayout.TextField("Aseprite Path", m_asepritePath);
		}


		GUILayout.Space(20);

		EditorGUILayout.HelpBox(@"TO USE:
    1) Duplicate this asset into the target folder.
    2) Choose your source PSD/Aseprite file.
    3) Add the names you want imported anim(s) to have.
    4) Import away!
    5) Leave the importer where it is to easily re-import later.", 
			MessageType.None);


		EditorGUILayout.HelpBox(@"OR JUST IGNORE THIS PREFAB AND:
     1) Open photoshop file you want to import sprites from.
     2) Right click target folder -> Import Sprites from Photoshop
     3) Select sprites, right click -> Create -> Animation from Sprites", 

			MessageType.None);

		GUILayout.Space(20);


				
		serializedObject.ApplyModifiedProperties();
		
		if ( importPNGs )
		{
			ImportPNGs();
			importPNGs = false;
		}
		else if ( createAnimations )
		{		
			CreateAnimations();
			createAnimations = false;
		}
		else if (GUI.changed)
		{
			EditorUtility.SetDirty(target);	
		}
	}


	#endregion
	#region Functions: Helpers

	void ImportPNGs()
	{
		if ( string.IsNullOrEmpty( m_component.m_sourcePSD ) == false && m_component.m_sourcePSD.EndsWith(".ase") )
		{
			m_component.m_sourceDirectory = Path.GetDirectoryName(m_component.m_sourcePSD)+@"/Export";

			Directory.CreateDirectory(m_component.m_sourceDirectory);

			// Find aseprite path
			System.Diagnostics.Process process = new System.Diagnostics.Process();

			string asepritePath = EditorPrefs.GetString("AsePath");
			if ( File.Exists(asepritePath) == false )
			{		
				if ( File.Exists(m_asepritePath) )
				{
					asepritePath = m_asepritePath;
					EditorPrefs.SetString("AsePath", asepritePath);
				}
				else 
				{
					foreach ( string tmpasePath in ASEPRITE_PATHS )
					{
						if (File.Exists(tmpasePath) )
						{
							asepritePath = tmpasePath;
							m_asepritePath = asepritePath;
							EditorPrefs.SetString("AsePath", asepritePath);
							break;
						}							
					}
				}
			}
			if ( File.Exists(asepritePath) == false )
			{
				EditorUtility.DisplayDialog("Error","Couldn't find path to Aseprite, set it in Advanced Settings first!","OK");
				return;
			}

			process.StartInfo.FileName = asepritePath;
			process.StartInfo.Arguments = string.Format("-b \"{0}\" --save-as \"{1}\\{2}_1.png\"", Path.GetFullPath(m_component.m_sourcePSD), Path.GetFullPath(m_component.m_sourceDirectory), Path.GetFileNameWithoutExtension(m_component.m_sourcePSD) );
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
			process.Start();

			// Wait up to 20 seconds for exit
			process.WaitForExit(20000);
			// import the pngs
			ImportPNGsFromFolder();
			// Delete sprites
			Directory.Delete(m_component.m_sourceDirectory,true);
			if ( File.Exists(m_component.m_sourceDirectory+".meta") )
				File.Delete(m_component.m_sourceDirectory+".meta");

		}
		else if ( string.IsNullOrEmpty( m_component.m_sourcePSD ) == false && m_component.m_sourcePSD.EndsWith(".psd") )
		{
			
			EditorUtility.DisplayProgressBar("Exporting from Photoshop", "Exporting from "+m_component.m_sourcePSD,0.8f);


			// Find vbs script path
			string[] guids = AssetDatabase.FindAssets("PSDExport");
			Debug.Assert(guids.Length > 0,"Couldn't find PSDExport.vbs");
			string exporterPath = Path.GetFullPath( AssetDatabase.GUIDToAssetPath(guids[0]) );

			// Runs script
			m_component.m_sourceDirectory = Path.GetDirectoryName(m_component.m_sourcePSD)+@"/Export";
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = exporterPath;
			process.StartInfo.Arguments = Path.GetFullPath(m_component.m_sourcePSD) + " " + Path.GetFullPath(m_component.m_sourceDirectory);
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
			process.Start();
			// Wait up to 20 seconds for exit
			process.WaitForExit(20000);
			EditorUtility.ClearProgressBar();
			// import the pngs
			ImportPNGsFromFolder();
			try 
			{
				// Delete exportfolder + metadata
				Directory.Delete(m_component.m_sourceDirectory,true);
				if ( File.Exists(m_component.m_sourceDirectory+".meta") )
					File.Delete(m_component.m_sourceDirectory+".meta");
			}
			catch
			{
			}
		}
		else 
		{
			ImportPNGsFromFolder();
		}
	}

	void ImportPNGsFromFolder()
	{

		if ( string.IsNullOrEmpty(m_component.m_sourceDirectory) )
		{
			EditorUtility.DisplayDialog("Error","Failed to import, set source directory first!","OK");
			return;
		}
		
		if ( m_items.Count  == 0 )
		{
			EditorUtility.DisplayDialog("Error","Failed to import, set some anims up first!","OK");
			return;
		}
		
		string ourPath = GetSubdirectory(m_component.m_spriteDirectory);
		
		string[] sourceFileNames = Directory.GetFiles(m_component.m_sourceDirectory,"*_*.png");
		int[] sourceFileIds = new int[sourceFileNames.Length];
		string resultString = "";

		Regex pngRegex = new Regex(REGEX_PNG, RegexOptions.IgnoreCase | RegexOptions.Compiled );
		for ( int i = 0; i < sourceFileNames.Length; ++i )
		{
			int id = -1;
			Match match = pngRegex.Match( sourceFileNames[i] );
			id = int.Parse( match.Groups["id"].Value );
			sourceFileIds[i] = id;
		}

		for ( int i = 0; i < m_items.Count; ++i )
		{		
			int firstFrame = m_items[i].m_firstFrame;			
			int lastFrame = ( i+1 < m_items.Count) ? m_items[i+1].m_firstFrame : 1000;
			//if ( lastFrame < firstFrame )
				//lastFrame = 10000;
			string failString = null;
			int copiedCount = 0;


			for ( int spriteId = firstFrame; spriteId < lastFrame; ++spriteId )
			{		
				// Find path that matches prefix
				string sourcePath = null;
				
				int sourceIndex = System.Array.FindIndex(sourceFileIds, sourceId=>sourceId==spriteId);
				if ( sourceIndex >= 0 && sourceIndex < sourceFileNames.Length )
				{
					sourcePath = sourceFileNames[sourceIndex];
				}

				if ( string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(m_items[i].m_name) )
				{
					//failString = "Failed to copy from empty Path";
					break;
				}
				else 
				{
					
					string fileName = m_items[i].m_name + "_"+(spriteId-firstFrame).ToString()+".png";
					string targetPath = ourPath + fileName;
					//Debug.Log("source: "+sourcePath+", targetPath = " + targetPath);

					// Copy the file
					try
					{						
						bool alreadyExisted = File.Exists(targetPath);
						File.Copy(sourcePath, targetPath, true);
						if ( alreadyExisted == false )
						{
							string relativeTargetPath = MakeRelative(targetPath, Application.dataPath);
							TextureImporter importer = TextureImporter.GetAtPath(relativeTargetPath) as TextureImporter;
							if ( importer == null )
								AssetDatabase.ImportAsset(relativeTargetPath, ImportAssetOptions.ForceUpdate); // If importer didn't already exist import the asset first before changing settings
							
							importer = TextureImporter.GetAtPath(relativeTargetPath) as TextureImporter;
							importer.spritePixelsPerUnit = m_component.m_pixelsPerUnit;
							importer.filterMode = m_component.m_filterMode;
							importer.mipmapEnabled = false;
							importer.textureCompression = TextureImporterCompression.Uncompressed;
							//importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
							if ( m_component.m_gui && string.IsNullOrEmpty(m_component.m_packingTag) == false)
								importer.spritePackingTag = "[RECT]"+m_component.m_packingTag;
							else
								importer.spritePackingTag = m_component.m_packingTag;			

							#if UNITY_2017_1_OR_NEWER
							// This isn't necessary in 5.6, but seems to be in 2018, not sure about 2017
							AssetDatabase.ImportAsset(relativeTargetPath, ImportAssetOptions.ForceUpdate);
							#endif
						}
						copiedCount++;	
						//resultString += "Imported "+m_items[i].m_name + "_"+(spriteId-firstFrame).ToString()+".png"+"\n";
					}
					catch(System.Exception e )
					{
						if (e!= null){};
						if ( string.IsNullOrEmpty(failString) )
							failString = "Failed to copy from "+sourcePath+" to "+targetPath;
					}

					// Delete the copied file
					if ( m_component.m_deleteImportedPngs )
					{
						try
						{
							File.Delete(sourcePath);						
						}
						catch(System.Exception e )
						{
							if (e!= null){};
							if ( string.IsNullOrEmpty(failString) )
								failString = "Failed to delete "+sourcePath;
						}
					}

				}	
			}


			if ( string.IsNullOrEmpty(failString) )
			{
				if ( copiedCount <= 0 )
				{
					if ( string.IsNullOrEmpty(m_items[i].m_name) == false )
						resultString += "Failed to import "+m_items[i].m_name + ": No frames found\n";
				}
				else if ( copiedCount == 1 )
				{
					// Imported Idle (1 frame)
					resultString += "Imported "+m_items[i].m_name + " (1 frame)\n";
				}
				else 
				{
					// Imported Idle (5 frames)
					resultString += "Imported "+m_items[i].m_name + " ("+copiedCount.ToString()+" frames)\n";
				}
			}
			else 
			{
				resultString += "Failed to import "+ m_items[i].m_name +" "+ failString+"\n";
			}

			
		}

		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		
		if ( string.IsNullOrEmpty(resultString) == false )
		{
			EditorUtility.DisplayDialog("Import Result",resultString,"OK");
		}
		else 
		{
			
			EditorUtility.DisplayDialog("Import Result","No sprites were found in "+m_component.m_sourceDirectory,"OK");
		}
		
		AssetDatabase.Refresh();
		
	}

	
	void CreateAnimations()
	{

		string spritePath = GetSubdirectory(m_component.m_spriteDirectory);

		string componentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_component));


		//
		// Create anims
		//
		{
			// Add new stuff/make edits

			for ( int i = 0; i < m_items.Count; ++i )
			{	
				
				// Skip anims with no name
				if ( string.IsNullOrEmpty(m_items[i].m_name) )
					continue;
					
				int firstFrame = m_items[i].m_firstFrame;			
				int lastFrame = firstFrame+1;
				if ( i+1 < m_items.Count) 
				{
					lastFrame = m_items[i+1].m_firstFrame;
				}
				else 
				{
					// TODO: calculate actual last frame better (check how crawl does it!)
					for ( int spriteId = firstFrame; spriteId < 1000; ++spriteId )
					{
						lastFrame = spriteId;
						string fileName = m_items[i].m_name + "_"+(spriteId-firstFrame).ToString()+".png";

						if ( File.Exists(spritePath + fileName) == false )
						{
							break;
						}
					}
				}

				bool isNew = false;
				string animFileName = componentPath+"/"+ m_items[i].m_name+".anim";
				AnimationClip animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFileName);
				if ( animClip == null )
				{					
					animClip = new AnimationClip();
					isNew = true;
				}

				// for now don't override existing animations - i think RHB has better importer that doesn't just blat anims, but for now just ignore
				if ( isNew == false )
					continue; 
				
				animClip.name = m_items[i].m_name;
				//animClip.legacy = true;

				EditorCurveBinding curveBinding = new EditorCurveBinding();
				// I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
				curveBinding.type = m_component.m_gui ? typeof(UnityEngine.UI.Image) : typeof(SpriteRenderer);
				// This is the property name to change the sprite of a sprite renderer
				curveBinding.propertyName = "m_Sprite";
				// Regular path to the gameobject that will be changed (empty string means root)
				curveBinding.path = "";

				if ( isNew )
					animClip.frameRate = 10;

				ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[lastFrame - firstFrame];

				for ( int frameIndex = 0; frameIndex < keyframes.Length; ++frameIndex )
				{
					int spriteId = firstFrame + frameIndex;
					string fileName = m_items[i].m_name + "_"+(spriteId-firstFrame).ToString()+".png";
					string relativeTargetPath = MakeRelative( spritePath + fileName, Application.dataPath);

					keyframes[frameIndex] = new ObjectReferenceKeyframe();	
					keyframes[frameIndex].time = (1.0f/animClip.frameRate) * frameIndex;
					#if UNITY_5_0_0
					Sprite sprite = AssetDatabase.LoadAssetAtPath(relativeTargetPath, typeof(Sprite)) as Sprite;
					#else
					Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativeTargetPath);
					#endif
					keyframes[frameIndex].value = sprite;
				}

				AnimationUtility.SetObjectReferenceCurve(animClip,curveBinding,keyframes);

				if ( isNew )
				{
					// Save the animation
					AssetDatabase.CreateAsset(animClip, animFileName);
				}
			}

			AssetDatabase.SaveAssets();
		}

		// Refresh asset database
		AssetDatabase.Refresh();
	}

	static void CreateAnimationsFromSelected()
	{

		// Put textures in list and sort
		List<Texture2D> textures = new List<Texture2D>(Selection.objects.Length);
		foreach( Object obj in Selection.objects )
		{
			if ( obj != null && obj as Texture2D != null )
				textures.Add(obj as Texture2D);
		}
		if ( textures.Count <= 0 )
			return;

		using ( PowerSpriteImportEditor.NaturalComparer comparer = new PowerSpriteImportEditor.NaturalComparer() )
		{				
			textures.Sort( (a, b) => comparer.Compare(a.name,b.name) );
		}

		string spritePath =  Path.GetDirectoryName(AssetDatabase.GetAssetPath(textures[0]));
		string animName = textures[0].name;
		int lastUnderscoreIndex = animName.LastIndexOf('_');
		if ( lastUnderscoreIndex > 0 )
			animName = animName.Substring(0,lastUnderscoreIndex);

		// Create list of sprites
		List<Sprite> sprites = new List<Sprite>();
		foreach( Texture2D tex in textures )
		{
			// Grab all sprites associated with a texture, add to list
			string path = AssetDatabase.GetAssetPath(tex);
			Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
			foreach (Object subAsset in assets) 
			{
				if (subAsset is Sprite) 
				{
					sprites.Add((Sprite)subAsset);
				}
			}
		}


		//
		// Create anims
		//
		bool isNew = false;
		string animFileName = spritePath+"/"+ animName+".anim";
		AnimationClip animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFileName);
		if ( animClip == null )
		{					
			animClip = new AnimationClip();
			isNew = true;
		}

		animClip.name = animName;

		EditorCurveBinding curveBinding = new EditorCurveBinding();
		// I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
		curveBinding.type = typeof(SpriteRenderer);
		// This is the property name to change the sprite of a sprite renderer
		curveBinding.propertyName = "m_Sprite";
		// Regular path to the gameobject that will be changed (empty string means root)
		curveBinding.path = "";

		if ( isNew )
			animClip.frameRate = 10;

		ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];

		for ( int frameIndex = 0; frameIndex < keyframes.Length; ++frameIndex )
		{
			keyframes[frameIndex] = new ObjectReferenceKeyframe();	
			keyframes[frameIndex].time = (1.0f/animClip.frameRate) * frameIndex;
			keyframes[frameIndex].value = sprites[frameIndex];
		}

		AnimationUtility.SetObjectReferenceCurve(animClip,curveBinding,keyframes);

		if ( isNew )
		{
			// Save the animation
			AssetDatabase.CreateAsset(animClip, animFileName);
		}

		AssetDatabase.SaveAssets();		

		// Refresh asset database
		AssetDatabase.Refresh();
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

	// Gets a subdirectory of current game object directory, creating it if necessary
	string GetSubdirectory(string subDirectoryName )
	{
		string result = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_component));
		if ( string.IsNullOrEmpty(subDirectoryName) == false )
		{
			result += "/" + subDirectoryName;
			Directory.CreateDirectory( result );
		}
		result = Path.GetFullPath(result);

		result = result.Replace("\\", "/");

		if (result.EndsWith("/") == false )
			result += "/";
		return result;
	}


	void RecalcLengths()
	{
		if ( m_items == null )
			return;
		// Recalculate lengths of all frames except last
		for ( int i = 0; i < m_items.Count-1; ++i )
		{
			m_items[i].m_length = Mathf.Max(1, m_items[i+1].m_firstFrame - m_items[i].m_firstFrame);
		}
		
	}

	void RecalcFirstFrames()
	{
		if ( m_items == null )
			return;
		// Recalculate first frames based on order of frames, and calculated lengths.
		for ( int i = 1; i < m_items.Count; ++i )
		{
			m_items[i].m_firstFrame = m_items[i-1].m_firstFrame + m_items[i-1].m_length;
		}		
	}

	/// For sorting strings by natural order (so, for example walk_9.png is sorted before walk_10.png)
	public class NaturalComparer: Comparer<string>, System.IDisposable 
	{
		// NaturalComparer function courtesy of Justin Jones http://www.codeproject.com/Articles/22517/Natural-Sort-Comparer

		Dictionary<string, string[]> m_table = null;

		public NaturalComparer() 
		{
			m_table = new Dictionary<string, string[]>();
		}

		public void Dispose() 
		{
			m_table.Clear();
			m_table = null;
		}

		public override int Compare(string x, string y) 
		{
			if (x == y) 
				return 0;

			string[] x1, y1;
			if (!m_table.TryGetValue(x, out x1)) 
			{
				x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
				m_table.Add(x, x1);
			}
			if (!m_table.TryGetValue(y, out y1)) 
			{
				y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
				m_table.Add(y, y1);
			}

			for (int i = 0; i < x1.Length && i < y1.Length; i++) 
			{
				if (x1[i] != y1[i]) 
				{
					return PartCompare(x1[i], y1[i]);
				}
			}

			if (y1.Length > x1.Length) 
			{
				return 1;
			} 
			else if (x1.Length > y1.Length) 
			{
				return -1;
			} 

			return 0;		
		}


		static int PartCompare(string left, string right) 
		{
			int x, y;
			if (!int.TryParse(left, out x)) 
			{
				return left.CompareTo(right);
			}

			if (!int.TryParse(right, out y)) 
			{
				return left.CompareTo(right);
			}

			return x.CompareTo(y);
		}

	}

}
#endregion
}