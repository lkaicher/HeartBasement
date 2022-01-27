using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;

namespace PowerTools.Quest
{

public class SystemDebug : SingletonAuto<SystemDebug>
{
	abstract class DebugElement
	{
		public Color m_color = Color.yellow;
		public float m_time = 0;
		public abstract void Draw();		
	}

	class DebugLine : DebugElement
	{		
		public Vector2 m_start = Vector2.zero;
		public Vector2 m_end = Vector2.zero;
		
		public override void Draw()
		{
			Debug.DrawLine(m_start, m_end, m_color);
		}
	}

	class DebugPoint: DebugElement
	{
		public Vector2 m_point = Vector2.zero;		

		public override void Draw()
		{
			Debug.DrawLine(m_point.WithOffset(-1,-1), m_point.WithOffset(1,1), m_color);
			Debug.DrawLine(m_point.WithOffset(-1,1), m_point.WithOffset(1,-1), m_color);
		}		
	}
	class DebugText: DebugElement
	{
		public string text = string.Empty;

		public override void Draw()
		{			
		}
	}

	List<DebugElement> m_elements = new List<DebugElement>();

	void LateUpdate()
	{
		for ( int i = m_elements.Count-1; i >= 0; --i )
		{
			DebugElement element = m_elements[i];
			element.Draw();

			element.m_time -= Time.deltaTime;
			if ( m_elements[i].m_time <= 0 )
			{
				m_elements.RemoveAt(i);
			}
		}		
	}

	public void DrawLine(Vector2 start, Vector2 end, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=start,m_end=end} );	
	}
	public void DrawPoint(Vector2 point, Color color, float time = 0 ) 
	{
		m_elements.Add(new DebugPoint(){m_color=color, m_time=time, m_point=point} );	
	}
	public void DrawPoly(Vector2[] poly, Color color, float time = 0 ) 
	{		
		for ( int j = 0; j < poly.Length; ++j )
		{
			int i = j == 0 ? poly.Length-1 : j-1;
			m_elements.Add(new DebugLine(){m_color=color, m_time=time, m_start=poly[i],m_end=poly[j]} );	
		}
	}
}

}
