using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
//using UI = UnityEngine.UI;
using PowerTools;
using PowerTools.Quest;
using System;
using PowerTools.QuestGui;

namespace PowerTools.Quest
{


public class GuiDialogTreeComponent : MonoBehaviour 
{
	enum eAlignVertical { BottomUp, TopDown }

	[SerializeField, Tooltip("The padding between each option")] float m_itemSpacing = 2;
	[SerializeField] Color m_colorDefault = Color.white;
	[SerializeField] Color m_colorUsed = Color.white;
	[SerializeField] Color m_colorHover = Color.white;
	[Tooltip("Whether any images nested under the option should be coloured along with the option text")]
	[SerializeField] bool m_changeImageColor = true;
	[Tooltip("If active, long lines will be wrapped (and stretch the interface vertically)")]
	[SerializeField] bool m_wrapText = true;
	[SerializeField] eAlignVertical m_verticalAlignment = eAlignVertical.BottomUp;
	[SerializeField, Tooltip("Max number of items to show in the list (0 for no limits)")] int m_maxVisibleItems = 0;
	[SerializeField, Tooltip("How much to offset text when back/forward buttons are OFF.")] float m_arrowButtonWidth = 0;
	[Header("Internal references")]
	[SerializeField] GameObject m_textInstance = null;
	[SerializeField] FitToObject m_background = null;
	[SerializeField] GameObject m_btnScrollBack = null; // Scroll back (or up) button
	[SerializeField] GameObject m_btnScrollForward = null; // Scroll forward (or down) button
	
	int m_itemsOffset = 0; // Keep track of the offset (none at dialog start)
	int m_maxNumOptions = 0; // Keep track of the options count in the current dialog branch

	float m_textContainerOffsetX = 0;

	List<QuestText> m_items = new List<QuestText>();

	float m_defaultItemHeight = 0;
	
	// Use this for initialization
	void Start() 
	{
		// If using simple buttons, move to the canvas
		if ( m_btnScrollBack != null && m_btnScrollBack.GetComponent<RectTransform>() != null )
		{
			Transform canvas = transform.parent.Find("QuestCanvas");
			transform.SetParent( canvas, true );
		}
	}

	void Awake()
	{
		m_textContainerOffsetX = m_textInstance.transform.parent.localPosition.x;
	}

	void OnEnable()
	{
		if ( m_textInstance.activeSelf )
		{		
			// Cache original text container offset		
			Renderer renderer = m_textInstance.gameObject.GetComponent<Renderer>();		
			if ( renderer != null )
				m_defaultItemHeight = renderer.bounds.size.y;
			m_textInstance.SetActive(false);
		}

		if (m_btnScrollForward)
			m_btnScrollForward.gameObject.SetActive( false );
		if (m_btnScrollForward)
			m_btnScrollBack.gameObject.SetActive( false );

		m_textInstance.GetComponent<QuestText>().Truncate = !m_wrapText;		

		UpdateItems();
	}
	
	// Update is called once per frame
	void Update() 
	{
		UpdateItems();
	}

	void UpdateItems()
	{

		DialogTree dialog = PowerQuest.Get.GetCurrentDialog();
		if ( dialog == null )
			return;
		
		float itemHeight = 1;
		float fixedHeightOffset = (m_maxVisibleItems * itemHeight) + (m_maxVisibleItems * itemHeight);

		// read the whole dialog branch, count the elements and take note of the total
		List<DialogOption> options = dialog.Options.FindAll((item)=>item.Visible);
		if (options.Count != m_maxNumOptions) 
		{
			m_maxNumOptions = options.Count;
			m_itemsOffset = 0;
		}

		int numOptions = m_maxNumOptions; // by default, show all options
		

		// If an maximum number of options is specified, slice the dialog
		// items collection accordingly
		if (m_maxVisibleItems > 0) 
		{
			int upperBound = Math.Min(m_maxVisibleItems, m_maxNumOptions);

			options = options.GetRange(m_itemsOffset, upperBound);
			numOptions = options.Count;

			// show the arrows accordingly
			m_btnScrollBack.gameObject.SetActive( m_itemsOffset > 0 );
			m_btnScrollForward.gameObject.SetActive( m_itemsOffset + upperBound != m_maxNumOptions );
		}

		while ( m_items.Count < numOptions )
		{
			// Add items (bottom up)
			Vector3 pos = m_textInstance.transform.position + new Vector3( 0, m_items.Count*m_itemSpacing, 0);
			GameObject obj = Instantiate(m_textInstance.gameObject, pos,Quaternion.identity, m_textInstance.transform.parent); 
			m_items.Add(obj.GetComponent<QuestText>());
			if ( m_background != null )
				m_background.FitToObjectHeight(obj);
		}

		// Reposition the text element to use all the space if we know we won't have arrows
		Transform textContainer = m_textInstance.transform.parent;					
		if ( m_maxVisibleItems > 0 && (m_btnScrollBack.gameObject.activeSelf || m_btnScrollForward.gameObject.activeSelf) ) 
			textContainer.localPosition = textContainer.localPosition.WithX(m_textContainerOffsetX);
		else 
			textContainer.localPosition = textContainer.localPosition.WithX(m_textContainerOffsetX - m_arrowButtonWidth);	

		Vector2 mousePos = PowerQuest.Get.GetCameraGui().ScreenToWorldPoint( Input.mousePosition.WithZ(0) );
		DialogOption selectedOption = null;
		
		bool top = m_verticalAlignment == eAlignVertical.TopDown;	

		float yOffset = top ? 0 : -m_defaultItemHeight;
					
		for ( int i = top? 0: numOptions-1; top ? i < numOptions : i >= 0; i+=(top?1:-1))
		{
			DialogOption option = options[i];
			QuestText item = m_items[i];			
			Renderer renderer = item.gameObject.GetComponent<Renderer>();
			
			item.gameObject.SetActive(true);
			item.text = option.Text;

			if ( top == false && renderer != null ) // If sorting from bottom- add offset before
				yOffset += renderer.bounds.size.y;

			item.gameObject.transform.position = m_textInstance.transform.position + new Vector3( 0, top?-yOffset:yOffset, 0);

			// Check mouse over
			bool over = (
				mousePos.y > item.GetComponent<Renderer>().bounds.min.y+1 && 
				mousePos.y < item.GetComponent<Renderer>().bounds.max.y-1 &&
				mousePos.x > item.GetComponent<Renderer>().bounds.min.x );
			
			if ( over )
				selectedOption = option;
			item.color = over ? m_colorHover : ( option.Used ? m_colorUsed : m_colorDefault);
			
			if ( m_changeImageColor )
			{
				SpriteRenderer[] sprites = item.GetComponentsInChildren<SpriteRenderer>();
				foreach( SpriteRenderer sr in sprites )
					sr.color = item.color;
			}
			yOffset += m_itemSpacing;
						
			if ( top && renderer != null ) // If sorting from top- add offset after
				yOffset += renderer.bounds.size.y;
			
		}

		for ( int i = numOptions; i < m_items.Count; ++i )
			m_items[i].gameObject.SetActive(false);

		PowerQuest.Get.SetDialogOptionSelected(selectedOption);
		if ( selectedOption != null && Input.GetMouseButtonDown(0) )
		{
			PowerQuest.Get.OnDialogOptionClick(selectedOption);
		}

	}

	public void ScrollBack() 
	{
		if ( m_itemsOffset == 0)
			return;
		m_itemsOffset -= 1;
	}

	public void ScrollForward() 
	{
		if ( m_itemsOffset == m_maxNumOptions - m_maxVisibleItems)
			return;
		m_itemsOffset += 1;
	}

	// Messages sent from button
	void OnClickScrollUp(Button button)
	{
		ScrollBack();
	}
	void OnClickScrollDown(Button button)
	{
		ScrollForward();
	}
}

}
