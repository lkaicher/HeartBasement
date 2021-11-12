using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{

[ExecuteInEditMode]
[AddComponentMenu("Quest Gui Layout/Fit To Object")]
public class FitToObject : MonoBehaviour 
{
	[Header("Fit Objects:")]
	[UnityEngine.Serialization.FormerlySerializedAs("m_containX")]
	[SerializeField] List<GameObject> m_fitWidth = new List<GameObject>();
	[UnityEngine.Serialization.FormerlySerializedAs("m_containY")]
	[SerializeField] List<GameObject> m_fitHeight = new List<GameObject>();	
	[Header("With Padding:")]
	[SerializeField] Padding m_padding = Padding.zero;
	[Header("Options")]
	[SerializeField] bool m_snapToPixel = true;
	[SerializeField, Tooltip("Fit the children of specified objects too?")] bool m_includeChildren = true;
	
	Vector2 m_spriteSizeInverted = Vector2.one;	
	SpriteRenderer m_sprite = null;	

	public void UpdateSize()
	{
		if ( m_sprite == null && Application.isPlaying == false )
			SetupSprite();

		bool sliced = ( m_sprite != null &&  m_sprite.drawMode == SpriteDrawMode.Sliced );
		Vector2 newPos = transform.position;
		Vector2 newScale = transform.localScale;
		if ( sliced )	
		    newScale = m_sprite.size;
		
		if ( m_fitWidth != null &&  m_fitWidth.Count > 0 )
		{

			bool first = true;
			RectCentered bounds = RectCentered.zero;

			for ( int i = 0; i < m_fitWidth.Count; ++i )
			{
				GameObject contain = m_fitWidth[i];
				if ( contain == null || contain == gameObject )
					continue;
				
				if ( contain && contain.activeInHierarchy )		
				{
					// If what we're containing is also a container, make sure that one's updated first
					FitToObject otherContain = contain.GetComponent<FitToObject>();
					if ( otherContain )
						otherContain.UpdateSize();
					
					RectCentered otherBounds = GuiUtils.CalculateGuiRect(contain.transform, m_includeChildren,null,null,transform);
					if ( otherBounds != RectCentered.zero )
					{
						// Scale and offset
						otherBounds.Transform(contain.transform);

						if ( first && otherBounds != RectCentered.zero)
						{
							first = false;
							bounds.CenterX = otherBounds.Center.x;
						}						
						bounds.Encapsulate( otherBounds );
					}

				}
			}
			
			bounds.MinX -= Padding.left;
			bounds.MaxX += Padding.right;

			if ( m_snapToPixel )
			{
				bounds.MinX = Utils.Snap(bounds.MinX);
				bounds.MaxX = Utils.Snap(bounds.MaxX);
			}

			if ( sliced )
				newScale.x = bounds.Width;// + m_padding.width;
			else 
				newScale.x = bounds.Width * m_spriteSizeInverted.x;// + m_padding.width;		

			newPos.x = bounds.Center.x;// + (-m_padding.left + m_padding.right)*0.5f;
		}

		if ( m_fitHeight != null &&  m_fitHeight.Count > 0 )
		{
			bool first = true;
			RectCentered bounds = RectCentered.zero;

			for ( int i = 0; i < m_fitHeight.Count; ++i )
			{
				GameObject contain = m_fitHeight[i];
				if ( contain == null || contain == gameObject )
					continue;
				
				if ( contain && contain.activeInHierarchy )		
				{
					// If what we're containing is also a container, make sure that one's updated first
					FitToObject otherContain = contain.GetComponent<FitToObject>();
					if ( otherContain )
						otherContain.UpdateSize();
					
					RectCentered otherBounds = GuiUtils.CalculateGuiRect(contain.transform, m_includeChildren,null,null,transform); // NB: we probably want to ignore, both ourselves and all our children too.
					//otherBounds.UndoTransform(contain.transform);
					if ( otherBounds != RectCentered.zero )
					{
						// Scale and offset
						otherBounds.Transform(contain.transform);

						if ( first && otherBounds != RectCentered.zero)
						{
							first = false;
							bounds.CenterY = otherBounds.Center.y;
						}						
						//Bounds otherBounds = contain.GetComponent<Renderer>().bounds;
						bounds.Encapsulate( otherBounds );
					}
					else 
					{
						bounds.Encapsulate( contain.transform.position );
					}

				}
			}
			
			bounds.MinY -= Padding.bottom;
			bounds.MaxY += Padding.top;

			if ( m_snapToPixel )
			{
				bounds.MinY = Utils.Snap(bounds.MinY);
				bounds.MaxY = Utils.Snap(bounds.MaxY);
			}		

			if ( sliced )
				newScale.y = bounds.Height;// + m_padding.height;
			else 
				newScale.y = bounds.Height * m_spriteSizeInverted.y;// + m_padding.height;

			newPos.y = bounds.Center.y;// + (-m_padding.bottom + m_padding.top)*0.5f;
		}


		//Debug.DrawLine(new Vector3(bounds.min.x,bounds.min.y,0),new Vector3(bounds.max.x,bounds.max.y,-10),Color.cyan);

		if ( sliced )	
		    m_sprite.size = newScale;
		else 
			transform.localScale = newScale.WithZ(1);
		transform.position = newPos.WithZ(transform.position.z);
	}	

	public void FitToObjectWidth(GameObject obj)
	{
		m_fitWidth.Add(obj);
		UpdateSize();
	}

	public void FitToObjectHeight(GameObject obj)
	{
		m_fitHeight.Add(obj);
		UpdateSize();
	}

	public Padding Padding {get{return m_padding;} set {m_padding = value; UpdateSize(); }}

	void SetupSprite()
	{
		m_sprite = GetComponentInChildren<SpriteRenderer>();
		Vector2 spriteSize = Vector2.one;
		if ( m_sprite != null && m_sprite.sharedMaterial != null && m_sprite.sharedMaterial.mainTexture != null )
		{
			spriteSize = new Vector2(m_sprite.sharedMaterial.mainTexture.width, m_sprite.sharedMaterial.mainTexture.height);
			//spriteSize.Scale(sprite.scale);
		}
		m_spriteSizeInverted.x = 1.0f/spriteSize.x;
		m_spriteSizeInverted.y = 1.0f/spriteSize.y;	
	}
	
	// Use this for initialization
	void Awake() 
	{	
		SetupSprite();
	}
	
	void OnEnable()
	{
		UpdateSize();
	}
	
	// Update is called once per frame
	void LateUpdate() 
	{
		UpdateSize();
	}
}
}
