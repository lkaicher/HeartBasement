using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PowerTools.Quest
{


#region Class: EditorUtils
public class EditorUtils
{
	public static readonly int NUM_BUILD_IN_PROPERTIES = 8;
	
	public delegate void RefreshTargetDelegate();
	public delegate void CreateListItemGUIDelegate( int index );
	public delegate T CreateNewListItemDelegate<T>( T before, T after );
	
	public static void UpdateListInspector<T>( ref List<T> list, RefreshTargetDelegate refreshTargetDelegate, CreateListItemGUIDelegate createItemGUIDelegate, CreateNewListItemDelegate<T> createNewItemDelegate )  where T : new()
	{
		GUILayout.Space(5);		
		
		int addAt = -2;
		int removeAt = -1;
		int upAt = -1;
		int downAt = -1;
		
		int count = list.Count;
		for (int i = 0; i < count; ++i )
		{
			EditorGUILayout.BeginHorizontal();
			
				//EditorGUILayout.BeginVertical(GUILayout.MaxWidth(25));
				EditorGUILayout.LabelField( i.ToString()+"-",EditorStyles.boldLabel, GUILayout.MaxWidth(17) );
				
				//EditorGUILayout.EndVertical();
			
				createItemGUIDelegate( i );
			
			
				if ( GUILayout.Button( new GUIContent("\u25B2"), EditorStyles.miniButtonLeft, GUILayout.MaxWidth(20) ) )
				{
					upAt = i;
				}
				if ( GUILayout.Button( new GUIContent("\u25BC"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
				{
					downAt = i;
				}
				if ( GUILayout.Button( new GUIContent("+"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
				{
					addAt = i;
				}
				if ( GUILayout.Button( new GUIContent("X"),  EditorStyles.miniButtonRight, GUILayout.MaxWidth(20) ) )
				{
					removeAt = i;
				}
			
			EditorGUILayout.EndHorizontal();
			
				
				//EditorGUILayout.BeginVertical(GUILayout.MaxWidth(40));
				
				//EditorGUILayout.EndVertical();
			
		}
		if ( count == 0 )
		{
			if ( GUILayout.Button( new GUIContent("+"), EditorStyles.miniButtonMid,GUILayout.MaxWidth(20) ) )
			{
				addAt = -1;
			}
		}
		
		if ( addAt >= -1 )
		{
			T prev = default(T);			
			T next = default(T);
			if ( addAt == -1 )
			{				
				if ( count > 0 )
				{
					prev = list[count-1];
				}
			}
			else 
			{
				prev = list[addAt];
			}
			
			if ( addAt+1 >= count )
			{			
				if ( count > 0 )
				{
					next = list[0];
				}		
			}
			else
			{
				next = list[addAt+1];
			}
			
			if ( createNewItemDelegate != null )
				list.Insert(addAt+1, createNewItemDelegate( prev, next ) );			
			else
				list.Insert(addAt+1, new T() );			
			
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		
		else if ( removeAt >= 0 )
		{
			list.RemoveAt(removeAt);
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();	
		}		
		
		else if ( upAt > 0 )
		{
			T temp = list[upAt-1];
			list[upAt-1] = list[upAt];
			list[upAt] = temp;
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		else if ( downAt >= 0 && downAt < list.Count-1)
		{
			T temp = list[downAt];
			list[downAt] = list[downAt+1];
			list[downAt+1] = temp;
			
			if ( refreshTargetDelegate != null )
				refreshTargetDelegate();
		}
		
	}

	
	// Trying using sprite data to generate collider. This needs the sprite to be the currently visible one
	// Also won't handle saving/loading or even room changes. Probably better as a flag for "use sprite collision" and it'll update on sprite change
	// Or, drag sprites in with ID to choose which to use for collider based on ID/Name
	public static void UpdateClickableCollider(GameObject clickable)
	{		
		SpriteRenderer renderer = clickable.GetComponentInChildren<SpriteRenderer>();
		PolygonCollider2D polygonCollider = clickable.GetComponentInChildren<PolygonCollider2D>();
		if( renderer == null || polygonCollider == null )
			return;

		Sprite sprite = renderer.sprite;
		if ( sprite == null )
			return;

		polygonCollider.pathCount = sprite.GetPhysicsShapeCount();
		
		List<Vector2> path = new List<Vector2>();		
		for (int i = 0; i < polygonCollider.pathCount; i++) 
		{
			path.Clear();
			sprite.GetPhysicsShape(i, path);
			polygonCollider.SetPath(i, path.ToArray());
		}
		// offset by powersprite offset
		PowerTools.PowerSprite powerSprite = renderer.GetComponent<PowerTools.PowerSprite>();		
		if ( powerSprite != null )
			polygonCollider.offset = powerSprite.Offset;
				
	}
	
}

#endregion

[CustomPropertyDrawer(typeof(ParallaxAttribute))]
public class ParallaxPropertyDrawer  : PropertyDrawer
{   
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property)+20;
    } 

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
		pos.height = EditorGUI.GetPropertyHeight(prop);
		EditorGUI.BeginProperty(pos, label, prop);		 
        Rect pos2 = EditorGUI.PrefixLabel (pos, GUIUtility.GetControlID (FocusType.Passive), new GUIContent("Depth", "Distant: 0 to 1. No Parallax: 1. Closer to camera: 1+"));
		EditorGUI.BeginChangeCheck ();					
		//float newVal = EditorGUI.Slider( pos, "Depth", -prop.floatValue+1, 0.0f, 2.0f);
		float newVal = EditorGUI.FloatField( pos, new GUIContent("Depth", "Distant: 0 to 1. No Parallax: 1. Closer to camera: 1+"), -prop.floatValue+1 );
		if ( EditorGUI.EndChangeCheck() )
		{
			prop.floatValue = -newVal+1;
		}				

        EditorGUI.EndProperty();
		
		pos.y = pos.y+18;
		pos.height = 20;
		if ( newVal < 0 )
			EditorGUI.HelpBox(pos,"Camera Angling (reverse movement)", MessageType.Warning);	
		else if ( newVal == 0 )
			EditorGUI.HelpBox(pos,"Full Distance (stationary)", MessageType.Error);	
		else if (newVal < 1 )
			EditorGUI.HelpBox(pos,"Further from camera (less movement)", MessageType.Info);	
		else if (  newVal == 1 )
			EditorGUI.HelpBox(pos,"No Parallax", MessageType.None);	
		else
			EditorGUI.HelpBox(pos,"Closer to camera (More movement)", MessageType.Info);	
		

    }
}


[CustomPropertyDrawer (typeof (MinMaxRange))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    public override void OnGUI (Rect pos, SerializedProperty prop, GUIContent label) 
	{
		//EditorGUIUtility.LookLikeControls ();
		
		EditorGUI.BeginProperty(pos, label, prop);
		
	    SerializedProperty min = prop.FindPropertyRelative ("m_min");
	    SerializedProperty max = prop.FindPropertyRelative ("m_max");
		SerializedProperty hasMax = prop.FindPropertyRelative("m_hasMax");
		SerializedProperty hasValue = prop.FindPropertyRelative("m_hasValue");
			
		float toWidth = 16;
		float maxLblWidth = 55;
		float buttonWidth = 20;
		
		float rectWidth = 0;//(pos.width-toWidth)*0.4f;		
        pos = EditorGUI.PrefixLabel (pos, GUIUtility.GetControlID (FocusType.Passive), label);
		//EditorGUI.LabelField( new Rect(posX, pos.y,rectWidth, pos.height), prop.name);
		//posX += rectWidth;
		float posX = pos.x;
		
		 // Don't make child fields be indented
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
				
		if ( min != null && max != null && hasMax != null )
		{
			bool hasMaxVal = hasMax.boolValue;
			
			if ( hasMaxVal == false )
			{
				rectWidth = pos.width-(maxLblWidth+4+buttonWidth);
			}
			else
			{
				rectWidth = (pos.width-(toWidth+2+buttonWidth+2))*0.5f;
			}
			
			// EditorGUI.IndentedRect(new Rect(posX, pos.y,rectWidth, pos.height));
			EditorGUI.BeginChangeCheck ();			
			float minVal = EditorGUI.FloatField( new Rect(posX, pos.y,rectWidth, pos.height), min.floatValue);			
			if ( EditorGUI.EndChangeCheck() )
			{
				min.floatValue = minVal;
				if ( max.floatValue < minVal )
				{
					max.floatValue = minVal;
				}	
				hasValue.boolValue = false;
			}
			
			if ( hasMaxVal )
			{
				
				posX += rectWidth+2;
				
				EditorGUI.LabelField( new Rect(posX, pos.y,toWidth, pos.height), "to");
				posX += toWidth;
				
				// EditorGUI.IndentedRect(new Rect(posX, pos.y,rectWidth, pos.height));
				EditorGUI.BeginChangeCheck ();			
				float maxVal = EditorGUI.FloatField( new Rect(posX, pos.y,rectWidth, pos.height), max.floatValue);
				if ( EditorGUI.EndChangeCheck() )
				{
					max.floatValue = maxVal;
					hasValue.boolValue = false;
				}
				posX += rectWidth+2;
			}
			else 
			{
				posX += rectWidth+4;
				EditorGUI.LabelField( new Rect(posX, pos.y,maxLblWidth, pos.height), "Is Range");
				posX += maxLblWidth;
			}
			
			// button
			EditorGUI.BeginChangeCheck ();		
			hasMaxVal = EditorGUI.Toggle(new Rect(posX, pos.y, buttonWidth, pos.height),hasMaxVal);
			
			if ( EditorGUI.EndChangeCheck() )
			{
				hasMax.boolValue = hasMaxVal;
				hasValue.boolValue = false;
			}
		}
		else
		{
			Debug.LogWarning("Min or max properties null");
		}
		
        EditorGUI.indentLevel = indent;
		
		EditorGUI.EndProperty();
		
		//EditorGUILayout.LabelField("Yo dawg!");
	}
	
}

public static class EditorExtension
{
	public static int DrawBitMaskField (Rect aPosition, int aMask, System.Type aType, GUIContent aLabel)
	{
		var itemNames = System.Enum.GetNames(aType);
		var itemValues = System.Enum.GetValues(aType) as int[];
		
		int val = aMask;
		int maskVal = 0;
		for(int i = 0; i < itemValues.Length; i++)
		{
			if (itemValues[i] != 0)
			{
				if ((val & itemValues[i]) == itemValues[i])
					maskVal |= 1 << i;
			}
			else if (val == 0)
				maskVal |= 1 << i;
		}
		int newMaskVal = EditorGUI.MaskField(aPosition, aLabel, maskVal, itemNames);
		int changes = maskVal ^ newMaskVal;
		
		for(int i = 0; i < itemValues.Length; i++)
		{
			if ((changes & (1 << i)) != 0)            // has this list item changed?
			{
				if ((newMaskVal & (1 << i)) != 0)     // has it been set?
				{
					if (itemValues[i] == 0)           // special case: if "0" is set, just set the val to 0
					{
						val = 0;
						break;
					}
					else
						val |= itemValues[i];
				}
				else                                  // it has been reset
				{
					val &= ~itemValues[i];
				}
			}
		}
		return val;
	}
}

[CustomPropertyDrawer(typeof(BitMaskAttribute))]
public class EnumBitMaskPropertyDrawer : PropertyDrawer
{
	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		var typeAttr = attribute as BitMaskAttribute;
		// Add the actual int value behind the field name
		label.text = label.text + "("+prop.intValue+")";
		prop.intValue = EditorExtension.DrawBitMaskField(position, prop.intValue, typeAttr.propType, label);
	}
}

// Press Alt-Enter to add new line in text box in inspector
public class CreateNewLine : EditorWindow
{
	[MenuItem("Edit/Insert New Line &\r")]
	static void InsertNewLine()
	{
		EditorGUIUtility.systemCopyBuffer = System.Environment.NewLine;
		EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste"));
	}

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}

[CustomPropertyDrawer(typeof(Rect))]
public class RectPropertyDrawer : PropertyDrawer
{
	static readonly GUIContent[] labels1 = new GUIContent[]
	{
		new GUIContent("X"),
		new GUIContent("Y"),
		new GUIContent("W"),
		new GUIContent("H")
	};
	static readonly GUIContent[] labels2 = new GUIContent[]
	{		
		new GUIContent("L"),
		new GUIContent("R"),
		new GUIContent("B"),		
		new GUIContent("T")
	};

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
		return base.GetPropertyHeight (prop, label) * 3;
	}

	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		Rect resultRect = prop.rectValue;

		float lineHeight = base.GetPropertyHeight (prop, label);

		Rect guiRect = position;
		guiRect.height = lineHeight;


		EditorGUI.BeginProperty(position, label, prop);

		EditorGUI.LabelField(guiRect, label);

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		//guiRect = EditorGUI.IndentedRect(guiRect);	
		guiRect.y += lineHeight;
		guiRect.width = guiRect.width - 20;
		guiRect.x += 20;

		float[] values = new float[]{ resultRect.center.x, resultRect.center.y, resultRect.width, resultRect.height};

		EditorGUI.BeginChangeCheck();

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Center Pos/Size"),*/ labels1, values );

		Rect tempRect = resultRect;
		Vector2 rectCenter = resultRect.center;
		if ( tempRect.center.x != values[0] ) { rectCenter.x = values[0]; resultRect.center = rectCenter;}
		if ( tempRect.center.y != values[1] ) { rectCenter.y = values[1];	resultRect.center = rectCenter;}		
		if ( tempRect.width != values[2] ) { resultRect.width = values[2]; resultRect.center = rectCenter;}
		if ( tempRect.height != values[3] ) { resultRect.height = values[3]; resultRect.center = rectCenter;}

		guiRect.y += lineHeight;

		values = new float[]{ resultRect.xMin, resultRect.xMax, resultRect.yMax, resultRect.yMin };

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Bounds"),*/ labels2, values );

		if ( tempRect.xMin != values[0] ) resultRect.xMin = values[0];
		if ( tempRect.xMax != values[1] ) resultRect.xMax = values[1];
		if ( tempRect.yMax != values[2] ) resultRect.yMax = values[2];
		if ( tempRect.yMin != values[3] ) resultRect.yMin = values[3];



		EditorGUI.indentLevel = indent;

		//rect.center.y = rect.center.y;
		//rect.center.
		//EditorGUI.MultiFloatField( position, label, labels, values );
		//GUILayout.Label("blah");

		if ( EditorGUI.EndChangeCheck() )
		{
			prop.rectValue = resultRect;
		}

		EditorGUI.EndProperty();
	}
}


[CustomPropertyDrawer(typeof(RectCentered))]
public class RectCenteredPropertyDrawer : PropertyDrawer
{
	static readonly GUIContent[] labels1 = new GUIContent[]
	{
		new GUIContent("X"),
		new GUIContent("Y"),
		new GUIContent("W"),
		new GUIContent("H")
	};
	static readonly GUIContent[] labels2 = new GUIContent[]
	{		
		new GUIContent("L"),
		new GUIContent("R"),
		new GUIContent("B"),		
		new GUIContent("T")
	};

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
		return base.GetPropertyHeight (prop, label) * 3;
	}

	public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label)
	{
		SerializedProperty min = prop.FindPropertyRelative ("m_min");
		SerializedProperty max = prop.FindPropertyRelative ("m_max");
		RectCentered resultRect = new RectCentered( min.vector2Value, max.vector2Value );

		float lineHeight = base.GetPropertyHeight (prop, label);

		Rect guiRect = position;
		guiRect.height = lineHeight;

		EditorGUI.BeginProperty(position, label, prop);

		EditorGUI.LabelField(guiRect, label);

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
	
		guiRect.y += lineHeight;
		guiRect.width = guiRect.width - 20;
		guiRect.x += 20;

		float[] values = new float[]{ resultRect.Center.x, resultRect.Center.y, resultRect.Width, resultRect.Height};

		EditorGUI.BeginChangeCheck();

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Center Pos/Size"),*/ labels1, values );

		RectCentered tempRect = new RectCentered(resultRect);
		Vector2 rectCenter = resultRect.Center;
		if ( tempRect.Center.x != values[0] ) { rectCenter.x = values[0]; resultRect.Center = rectCenter;}
		if ( tempRect.Center.y != values[1] ) { rectCenter.y = values[1];	resultRect.Center = rectCenter;}		
		if ( tempRect.Width != values[2] ) { resultRect.Width = values[2]; resultRect.Center = rectCenter;}
		if ( tempRect.Height != values[3] ) { resultRect.Height = values[3]; resultRect.Center = rectCenter;}


		guiRect.y += lineHeight;

		values = new float[]{ resultRect.MinX, resultRect.MaxX, resultRect.MinY, resultRect.MaxY };

		EditorGUI.MultiFloatField( guiRect, /*new GUIContent("Bounds"),*/ labels2, values );

		if ( tempRect.MinX != values[0] ) resultRect.MinX = values[0];
		if ( tempRect.MaxX != values[1] ) resultRect.MaxX = values[1];
		if ( tempRect.MinY != values[2] ) resultRect.MinY = values[2];
		if ( tempRect.MaxY != values[3] ) resultRect.MaxY = values[3];

		EditorGUI.indentLevel = indent;

		if ( EditorGUI.EndChangeCheck() )
		{
			min.vector2Value = resultRect.Min;
			max.vector2Value = resultRect.Max;
		}

		EditorGUI.EndProperty();
	}
}

/*
	Weird idea for automatically adding references to something like this somewhere
		public Dictionary<System.Guid, Object> m_thingJonWontLike = null;

[CustomPropertyDrawer(typeof(ObjectReferenceAttribute ))]
public class ObjectReferenceDrawer : PropertyDrawer
{
    static int id = 0;
    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
		Object oldRefValue = property.objectReferenceValue;
        EditorGUI.PropertyField(position, property, label, true);
		if ( GUI.changed && oldRefValue != property.objectReferenceValue )
        {
        	if ( property.objectReferenceValue != null )
        	{
				Debug.Log( id.ToString() + " " + property.objectReferenceValue.ToString());
        	}
        	else 
        	{
				Debug.Log( id.ToString() + " null");
        	}
        	id++;
        	// TODO- Remove previous, and add new to list of referenced objects
        }

    }
}
*/
}
