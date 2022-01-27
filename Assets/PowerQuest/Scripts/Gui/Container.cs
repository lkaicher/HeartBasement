using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using PowerTools.Quest;

namespace PowerTools.QuestGui
{

// A Quest Gui Control that contains other gui controls. Add a grid container component too for some more functionality.
[System.Serializable] 
[AddComponentMenu("Quest Gui/Container")]
public partial class Container : GuiControl, IContainer
{
    GridContainer m_grid = null;

    [Tooltip("If true, the container's size (for Fit To/Align To purposes) expands as items are added/removed from the grid")]
    [SerializeField] bool m_dynamicGridSize = false;
    
    public GridContainer Grid => m_grid;

    void InitComponents()
    {
        if ( m_grid == null )
        {
            m_grid = GetComponent<GridContainer>();            
        }
    }

    void Awake()
    {
        InitComponents();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {        
    }


    public override void UpdateBaseline()
    {
        // "Don't update baseline of child items
    }
    
	// Returns the "rect" of the control. For images, that's the image size/position. Used for aligning controls to each other
	public override RectCentered GetRect(Transform exclude = null)
	{        
        if ( Application.isEditor )
            InitComponents();
        if ( m_grid != null )
        {
            if ( m_dynamicGridSize )            
                return m_grid.FilledRect;
            return m_grid.Rect;
        }
        return base.GetRect(exclude);
	}
}
}
