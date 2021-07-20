// Created by Dave Lloyd (@duzzondrums) for Powerhoof - http://tools.powerhoof.com for updates

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools
{

/// Power sprite import- Tools for importing sprites/anims from photoshop/aseprite
/**
    SIMPLE IMPORT (PSP only):
    - Right click a directory->Import from Photoshop
		- Photoshop must be open with file you wanna import sprites from
		
	CUSTOM IMPORT:
	- Duplicate the PowerSpriteImport asset  into the folder you want to import anims to (or Right Click->Create->Sprite Importer)
	- Set a source Photoshop or Aseprite file
	- Set the names of Animation(s), and starting frames
		- You can leave an empty one to skip importing frames
	- Click Import Sprites to import sprites
	- Click CreateAnimations to create animations from the sprites
		- Then edit them in PowerSpriteAnimator!!
	- Leave the importers where they are so you can re-import later
*/
public class PowerSpriteImport : ScriptableObject 
{
	[System.Serializable]
	public class AnimImportData 
	{
		public string m_name = string.Empty;
		public int m_firstFrame = 0;

		// length is only used for aiding with calculating first frames when editing list. First frame is whats actually used 
		public int m_length = 1; 
	};
	public List<AnimImportData> m_animations = new List<AnimImportData>();
	

	public string m_packingTag = string.Empty;
	public float m_pixelsPerUnit = 1;
	public FilterMode m_filterMode = FilterMode.Point;
	public string m_sourcePSD = string.Empty;
	public string m_sourceDirectory = string.Empty;
	public bool m_deleteImportedPngs = true;
	public string m_spriteDirectory = "Sprites";
	public bool m_gui = false;

	
}
}