using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using ClipperLib;

namespace PowerTools.Quest
{

// For clipper library
using Path = List<IntPoint>;
using Paths = List<List<IntPoint>>;


[SelectionBase]
public class RoomComponent : MonoBehaviour 
{

	[SerializeField] Room m_data = new Room();

	[SerializeField] [HideInInspector] public string m_debugStartFunction = null;

	// this is stored mainly for editor
    [SerializeField] [HideInInspector] List<HotspotComponent> m_hotspotComponents = new List<HotspotComponent>();
	[SerializeField] [HideInInspector] List<PropComponent> m_propComponents = new List<PropComponent>();
	[SerializeField] [HideInInspector] List<RegionComponent> m_regionComponents = new List<RegionComponent>();
	[SerializeField] [HideInInspector] List<WalkableComponent> m_walkableAreas = new List<WalkableComponent>();
	[ReadOnly][SerializeField] List<AnimationClip> m_animations =  new List<AnimationClip>();
	
	Pathfinder m_pathfinder = new Pathfinder();

	public Room GetData() { return m_data; } 
	public void SetData(Room data) { m_data = data; }
	public GameObject GetPrefab()  { return ( Application.isPlaying && m_data.GetPrefab() != null ) ? m_data.GetPrefab() : gameObject; } // Used by editor
	public List<HotspotComponent> GetHotspotComponents() { return m_hotspotComponents; }
	public List<PropComponent> GetPropComponents() { return m_propComponents; }
	public List<RegionComponent> GetRegionComponents() { return m_regionComponents; }
	public List<WalkableComponent> GetWalkableAreas() { return m_walkableAreas; }

	public AnimationClip GetAnimation(string animName) { return m_animations.Find(item=>item != null && string.Equals(animName, item.name, System.StringComparison.OrdinalIgnoreCase));  }
	public List<AnimationClip> GetAnimations() { return m_animations; }

	// Called once room and everything in it has been created and PowerQuest has initialised references. After Start, Before OnEnterRoom.
	public void OnLoadComplete()
	{
		m_hotspotComponents.ForEach(item=>item.OnLoadComplete());
		m_propComponents.ForEach(item=>item.OnLoadComplete());
		m_regionComponents.ForEach(item=>item.OnLoadComplete());
				
		if ( PowerQuest.Get.GetPixelCamEnabled() )
		{
			// If pixel cam's enabled, set props with parallax to "sub-pixel layer"
			int layerHighRes = LayerMask.NameToLayer("HighRes");
			m_propComponents.ForEach(item=>
			{	
				if ( item.GetParallax() != 0.0f )
					item.gameObject.layer = layerHighRes;
			
			});
			
		}
		
	}

	public void EditorUpdateChildComponents()
	{
		m_hotspotComponents.Clear();
		m_hotspotComponents.AddRange(GetComponentsInChildren<HotspotComponent>(true));

		m_propComponents.Clear();
		m_propComponents.AddRange(GetComponentsInChildren<PropComponent>(true));

		m_regionComponents.Clear();
		m_regionComponents.AddRange(GetComponentsInChildren<RegionComponent>(true));

		m_walkableAreas.Clear();
		m_walkableAreas.AddRange(GetComponentsInChildren<WalkableComponent>(true));
	}

	// returns true if walkable area was found
    public bool SetActiveWalkableArea(int id)
    {
		if ( m_walkableAreas.IsIndexValid(id) )
		{			
			m_pathfinder.SetMainPolygon( m_walkableAreas[id].PolygonCollider );
			
			// Find holes
			if ( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) )
			{
				PolygonCollider2D[] childColliders = m_walkableAreas[id].transform.GetComponentsInChildren<PolygonCollider2D>();
				foreach( PolygonCollider2D collider in childColliders )
				{
					if( collider.transform != m_walkableAreas[id].transform )
						m_pathfinder.AddObstacle(collider);
				}
			}
			return true;
		}
		return false;
    }

	// Uses clipper to build walkable area from multiple polygons, subtracting holes. 
	// This will allow walkables to be made up of more than one poly, so you can turn parts of the are on or off
	// Also will allow regions to block off whole areas, rather than having to build 2 walkable areas for that.
	// In short- it's better, but needs some refactoring and lots of testing to work.
	// This would make more sense living in Pathfinder rather than room. too...
	public bool BuildWalkableArea()
	{

		// Try clipper!
		Clipper clipper = new Clipper();
		clipper.ReverseSolution = true;

		// Create path from firstPoly
		bool first = true;

		foreach ( WalkableComponent walkable in m_walkableAreas )
		{			
			// TODO: check if walkable is "enabled"
			Vector2[] points = walkable.PolygonCollider.points;

			//Pathfinder.ReversePoly(points);
			Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x,item.y);} )  );
			clipper.AddPath(poly, first ? PolyType.ptSubject : PolyType.ptClip, true);
			first = false;
		}

		Paths result = new Paths();
		bool success = clipper.Execute(ClipType.ctUnion, result, PolyFillType.pftEvenOdd);
		Debug.Log("Clipper main: "+ (success ? "succeed":"fail")+", results: "+result.Count);

		if ( success == false || result.Count < 0 )
			return false;

		// Now subtract holes
		int holes = 0;
		clipper.Clear();
		clipper.AddPath(result[0], PolyType.ptSubject, true);
		foreach ( WalkableComponent walkable in m_walkableAreas )
		{
			// TODO: check if walkable is "enabled"
			PolygonCollider2D[] childColliders = walkable.transform.GetComponentsInChildren<PolygonCollider2D>();
			foreach( PolygonCollider2D hole in childColliders )
			{
				// TODO: check if walkable is "enabled"
				if( hole.transform == walkable.PolygonCollider.transform )
					continue;			

				Vector2[] points = hole.points;
				Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x,item.y);} )  );
				clipper.AddPath(poly, PolyType.ptClip, true);
				holes++;
			}
		}

		foreach ( RegionComponent region in m_regionComponents )
		{
			if ( region.GetData().Enabled == false || region.GetData().Walkable )
				continue;

			Vector2[] points = region.GetPolygonCollider().points;
			Path poly = new Path( System.Array.ConvertAll<Vector2,IntPoint>(points, item => {return new IntPoint(item.x,item.y);} )  );
			clipper.AddPath(poly, PolyType.ptClip, true);
			holes++;
		}

		if ( holes > 0 )
		{
			result.Clear();
			success = clipper.Execute(ClipType.ctDifference, result, PolyFillType.pftEvenOdd);
			Debug.Log("Clipper holes: "+ (success ? "succeed":"fail")+", results: "+result.Count);
		}

		bool setMain = false;
		m_pathfinder = new Pathfinder();
		foreach ( Path path in result )
		{
			Vector2[] points = path.ConvertAll( item => new Vector2(item.X,item.Y) ).ToArray();
			if ( Clipper.Orientation(path) == false )
			{
				if ( setMain == false )
					m_pathfinder.SetMainPolygon( m_walkableAreas[0].PolygonCollider, points );
				setMain = true;
			}
			else 
			{
				Pathfinder.ReversePoly(points);	
				m_pathfinder.AddObstacle( m_walkableAreas[0].PolygonCollider, points );
			}
		}
		return true;
		
	}

	public Pathfinder GetPathfinder() { return m_pathfinder; }
		
	/*
	void Awake()
	{		
		UpdateWalkableAreaList();
		m_pathfinder.SetMainPolygon( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) ? m_walkableAreas[m_data.ActiveWalkableArea].PolygonCollider : null );

		// Find holes
		if ( m_walkableAreas.IsIndexValid(m_data.ActiveWalkableArea) )
		{
			PolygonCollider2D[] childColliders = m_walkableAreas[0].transform.GetComponentsInChildren<PolygonCollider2D>();
			foreach( PolygonCollider2D collider in childColliders )
			{
				if( collider.transform != m_walkableAreas[0].transform )
					m_pathfinder.AddObstacle(collider);
			}
		}
	}

	
	// Use this for initialization
	void Start () 
	{
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	*/

    void UpdateWalkableAreaList()
	{
		m_walkableAreas.Clear();
		m_walkableAreas.AddRange( GetComponentsInChildren<WalkableComponent>(true) );
    }



}

}