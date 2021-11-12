using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PowerTools.Quest;
using PowerTools;

namespace PowerTools.QuestGui
{

[CanEditMultipleObjects]
[CustomEditor(typeof(GridContainer), true)]
public class GridContainerEditor : ControlEditorBase 
{	

	Vector2 m_posCached = Vector2.zero;
	RectCentered m_bounds = RectCentered.zero;

	public override void OnInspectorGUI()
	{
		GridContainer grid = (GridContainer)target;			
		
		// Alignment buttons
		//LayoutAlignment("Align Image");
	
		//
		// Manual size
		//		
		
		// show manual image sizer
		SerializedObject serializedObj = new SerializedObject(grid);
		EditorGUI.BeginChangeCheck();

		m_bounds.Size = Utils.SnapRound(EditorGUILayout.Vector2Field("Size:", m_bounds.Size));

		if (EditorGUI.EndChangeCheck() )
		{
			grid.CustomSize = m_bounds;
				m_posCached = grid.transform.position;
				EditorUtility.SetDirty(target);
		}

		DrawDefaultInspector();		
	}
	

	public override void OnSceneGUI()
	{	
		// Custom size control (if image is split)
		GridContainer grid = (GridContainer)target;

		OnSceneDrawPivot(grid.transform);

		if ( grid.transform.parent != null )
		{
			if ( m_bounds == RectCentered.zero || m_posCached != (Vector2)grid.transform.position )
			{
				m_bounds = grid.CustomSize;		
				m_posCached = grid.transform.position;
			}			

			RectCentered oldBounds = m_bounds;			
			RectCentered bounds = OnSceneGuiRectCenter( oldBounds, true );
			if ( bounds != oldBounds )
			{
				m_bounds = bounds;
				grid.CustomSize = bounds;
				m_posCached = grid.transform.position;
				EditorUtility.SetDirty(grid.gameObject);// set whole thing dirty
			}

			// Show the grid of items
			
			//bounds.Transform(grid.transform);
			Vector2 gridActualSize = grid.Size;
			
			// vertical lines
			for ( float x = 0; x <= gridActualSize.x; x+=grid.ItemSpacing.x )
				Handles.DrawLine( new Vector2(x+bounds.MinX,bounds.MaxY), new Vector2(x+bounds.MinX,bounds.MaxY-gridActualSize.y));
			// horizontal lines
			for ( float y = gridActualSize.y; y > 0; y-=grid.ItemSpacing.y )
				Handles.DrawLine( new Vector2(bounds.MinX,bounds.MaxY-y), new Vector2(bounds.MinX+gridActualSize.x,bounds.MaxY-y));

		}	
	}
}
}
