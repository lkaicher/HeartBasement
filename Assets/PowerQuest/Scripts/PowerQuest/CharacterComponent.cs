using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using System.Text.RegularExpressions;
using static PowerTools.Quest.Character;

namespace PowerTools.Quest
{

[SelectionBase]
public partial class CharacterComponent : MonoBehaviour 
{
	
	#region Static definitions

	public enum eFaceMask 
	{
		Left = 1<<eFace.Left,
		Right = 1<<eFace.Right,
		Down = 1<<eFace.Down,
		Up = 1<<eFace.Up,
		DownLeft = 1<<eFace.DownLeft,
		DownRight = 1<<eFace.DownRight,
		UpLeft = 1<<eFace.UpLeft,
		UpRight = 1<<eFace.UpRight,
	}

	static readonly eFace[] TURN_ORDER = 
	{
		eFace.UpLeft, eFace.Left, eFace.DownLeft, eFace.Down, eFace.DownRight, eFace.Right, eFace.UpRight, eFace.Up
	};

	[System.Serializable]
	public class TransitionAnim
	{		
		public string anim = null;
		/// comma delimited  anim names
		public string from = null;
		/// comma delimited  anim names
		public string to = null;
		public bool onFlip = false;
	};

	[System.Serializable]
	public class TurnAnim
	{
		public string fromAnim = null;
		[BitMask(typeof(eFaceMask))]
		public int fromDirection = 0;
		[BitMask(typeof(eFaceMask))]
		public int toDirection = 0;
		public string anim = null;
		public bool m_mirror = true;
	};

	#endregion
	#region Vars: Editor

	[Tooltip("Character data")]
	[SerializeField] Character m_data = new Character();

	[Tooltip("If the character needs to swap between multiple clickable colliders, hook them up here")]
	[SerializeField] Collider2D[] m_clickableColliders = null;	// If multiple colliders that can be enabled/disabled

	[Tooltip("If character's changes from one anim to another, matching the from/to in this list, the transition anim will be played first")]
	[SerializeField] TurnAnim[] m_turnAnims = null;

	[Tooltip("EXPERIMENTAL: If character's changes from one anim to another, matching the from/to in this list, the transition anim will be played first")]
	[SerializeField] TransitionAnim[] m_transitionAnims = null;
	
	[Tooltip("This list is read only, animations are automatically added to it")]
	[ReadOnly, NonReorderable, SerializeField] List<AnimationClip> m_animations = new List<AnimationClip>();


	#endregion
	#region Vars: Private

	bool m_firstUpdate = true;

	Vector2 m_targetPos = Vector2.zero;
	Vector2 m_targetEndPos = Vector2.zero;
	eState m_state = eState.None;

	SpriteRenderer m_sprite = null;
	PowerSprite m_powerSprite = null;
	SpriteAnim m_spriteAnimator = null;

	int m_playIdleDelayFrames = 0;
	Vector2[] m_path = null;
	int m_pathPointNext = -1;
	bool m_turningToWalk = false;

	eFace m_facing = eFace.Up;	// Used to check if facing has changed in an update facing call
	string m_currAnimBaseName = null; // Used to change animation when face direction changes
	
	int m_currLineId =-1;
	float m_turnTimer = 0;

	SpriteAnim m_mouth = null;
	SpriteAnimNodes m_mouthNode = null;
	
	// Whether the solid obstacle has been added for this character yet
	bool m_addedSolidObstacle = false;

	// Experimental "Transition" code. Not really working for flipping and stuff
	string m_transitionAnim = null;
	string m_transitioningToAnim = null;
	string m_transitioningFromAnim = null;
	bool m_flippedLastUpdate = false; // unsure if this is used, look at cleaning up.
	float m_animChangeTime = 0; // how long the current clip has been playing
	//float m_loopStartTime = -1; // whether a loop tag was hit, and at what time
	//float m_loopEndTime = -1;   // whether a loop end tag was hit, and at what time
	
	bool m_playWalkAnim = true; // Flag used for MoveTo, to move player without walk animation being played

	// used for automatically setting hotspots
	PolygonCollider2D m_autoHotspotCollider = null;
	Sprite m_lastHotspotSprite = null;

	GameObject m_shadow = null;
	SpriteAnim m_shadowAnim = null;

	bool m_walking = false;
	bool m_talking = false;
	bool m_animating = false;

	public bool Walking => m_walking;
	public bool Talking => m_talking;
	public bool Animating => m_animating;


	#endregion
	#region Public access Functions

	public Character GetData() { return m_data; }
	public void SetData(Character data) 
	{ 
		m_data = data; 

		// Set the clickable collider to enable the correct collision after restoring
		OnClickableColliderIdChanged();
		if ( string.IsNullOrEmpty(m_data.Animation) == false )
		{
			// Hack for restoring existing 'loop time'. Need to cache it, and pretend it's not set, so the game doesn't try and 'transition' out of looping anim.
			float loopStartTime = m_data.LoopStartTime;
			float loopEndTime = m_data.LoopEndTime;
			m_data.LoopStartTime = -1;
			m_data.LoopEndTime = -1;		

			PlayAnimation(m_data.Animation);

			m_data.LoopStartTime = loopStartTime;
			m_data.LoopEndTime = loopEndTime;

			// m_spriteAnimator.NormalizedTime = m_data.AnimationTime; // moved this to first update
			
		}
	}
	public SpriteRenderer GetSprite() { return m_sprite; }
	public SpriteAnim GetSpriteAnimator() { return m_spriteAnimator; }
	public eState GetState()	{ return m_state; }

	public List<AnimationClip> GetAnimations() { return m_animations; }

	public Vector2 GetTargetPosition() { return m_targetEndPos; }

	public string GetTransitionAnim( string from, bool wasFlipped, string to, bool flip )
	{
		bool flipping = flip != wasFlipped;
		TransitionAnim result = System.Array.Find(m_transitionAnims, item=> 
			{
				if (  to == null || from == null )
					return false;
				if ( Regex.IsMatch(to,item.to,RegexOptions.IgnoreCase) == false 
					|| Regex.IsMatch(from,item.from,RegexOptions.IgnoreCase) == false )
					return false;
				/*
				if ( string.Equals(to,item.to, System.StringComparison.OrdinalIgnoreCase) == false 
					|| string.Equals(from, item.from, System.StringComparison.OrdinalIgnoreCase) == false )
					return false;
				*/
				if ( item.onFlip )
					return flipping;
				return true;
			});
		if ( result != null )
			return result.anim;
		return null;
	}
	
	string GetTurnAnim(eFace oldFacingVerticalFallback)
	{
		if ( m_facing == m_data.GetTargetFaceDirection() || string.IsNullOrEmpty(m_currAnimBaseName) )
			return null;
		
		TurnAnim result = System.Array.Find(m_turnAnims, item=> 
			{
				eFace facing = m_facing;
				
				if ( item.m_mirror )
				{
					// Hackery to allow "down" facing to play the anim if animation is actually the "right" or "downright" anim
					if ( BitMask.IsSet(item.fromDirection, (int)eFace.Down) && m_facing == eFace.Down)				
						facing = oldFacingVerticalFallback;

					// More hackery-  When target facing is "down" the previous cardinal will always be the same, so no transition when mirrored
					if ( m_data.GetTargetFaceDirection() == eFace.Down ) 
						return false;
				}

				bool found = ( BitMask.IsSet(item.fromDirection,(int)facing)
								&& BitMask.IsSet(item.toDirection,(int)m_data.GetTargetFaceDirection()) 
								&& Regex.IsMatch(m_currAnimBaseName,$"^({item.fromAnim})$",RegexOptions.IgnoreCase) );
				
				// Try mirror, but not if facing down
				if ( found == false && item.m_mirror )
				{					
					found = ( BitMask.IsSet(item.toDirection,(int)facing) && BitMask.IsSet(item.fromDirection,(int)m_data.GetTargetFaceDirection()) &&
						Regex.IsMatch(m_currAnimBaseName,$"^({item.fromAnim})$",RegexOptions.IgnoreCase) );
				}
				return found;
			});

		return result != null ? result.anim : null;
	}

	string m_playAfterTurnAnim = null;
	string m_currTurnAnim = null;

	public bool StartTurnAnimation(eFace oldFacingVerticalFallback)
	{
		string turnAnim = GetTurnAnim(oldFacingVerticalFallback);
		if ( turnAnim == null )
			return false;
		m_playAfterTurnAnim = m_currAnimBaseName;
		m_currTurnAnim = turnAnim;
		PlayAnimInternal(turnAnim);
		return true;
	}

	public void EndTurnAnimation()
	{
		if ( m_playAfterTurnAnim != null)
			PlayAnimInternal(m_playAfterTurnAnim);
		m_playAfterTurnAnim = null;
	}

	public bool GetPlayingTurnAnimation()
	{
		return m_playAfterTurnAnim != null;
	}

	// Turn on/off visibility depending on whether character should be visible
	public void UpdateVisibility()
	{
		if ( m_data == null )
			return;
		bool shouldShow = m_data.Visible;
		if ( GetSpriteAnimator() != null )
			GetSpriteAnimator().enabled = shouldShow;
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		System.Array.ForEach(renderers, renderer => 
		{
				renderer.enabled = shouldShow;
		});
	}

	public void UpdateEnabled()
	{		
		UpdateVisibility();
		UpdateSolid();
	}

	// Called when enabled/disabled the charadter's solid obstacle for other characters to walk around
	public void UpdateSolid()
	{
		if ( GetData() == null || PowerQuest.Get.Pathfinder == null )
			return;

		// Add obstacle to pathfinder if it wasn't already there
		if ( m_addedSolidObstacle == false && GetData().Solid )
		{
			PowerQuest.Get.Pathfinder.AddObstacle(transform, CalcSolidPoly());
			m_addedSolidObstacle = true;
		}
		else if ( m_addedSolidObstacle == true && GetData().Solid == false )
		{
			PowerQuest.Get.Pathfinder.RemoveObstacle(transform);
			m_addedSolidObstacle = false;
		}
	}
	
	// Called when solid size has changed so obstacle can be removed/re-added with new dimentions
	public void UpdateSolidSize()
	{
		if ( m_addedSolidObstacle && PowerQuest.Get.Pathfinder != null )
		{
			PowerQuest.Get.Pathfinder.RemoveObstacle(transform);			
			m_addedSolidObstacle = false;
		}
		UpdateSolid();
	}	

	// Calculates and returns the polygon for the solid collider that other characters cant walk through. Public for editor
	public Vector2[] CalcSolidPoly()
	{
		Vector2 halfSize = GetData().SolidSize * 0.5f;
		Vector2[] poly = new Vector2[4]
		{
			new Vector2(-halfSize.x, -halfSize.y),
			new Vector2(-halfSize.x, halfSize.y),
			new Vector2(halfSize.x, halfSize.y),
			new Vector2(halfSize.x, -halfSize.y),
		};
		return poly;
	}
	

	public void UpdateUseSpriteAsHotspot()
	{
		if ( GetData() == null )
			return;
		bool useSprite = m_data.UseSpriteAsHotspot;
		if ( useSprite )
		{
			if ( m_clickableColliders == null || m_clickableColliders.Length == 0 )
			{
				// Find and add collider to be fallback clickable collider
				Collider2D collider = GetComponent<Collider2D>();
				if ( collider != null )
				{
					m_clickableColliders = new Collider2D[] {collider};
					m_data.ClickableColliderId = 0;
				}
			}

			if ( m_autoHotspotCollider == null )
			{
				// Add automatic collider
				m_autoHotspotCollider = gameObject.AddComponent<PolygonCollider2D>();
				m_autoHotspotCollider.isTrigger = true;
			}
			m_autoHotspotCollider.enabled = true;
			

			// disable clickable colliders
			for ( int i = 0; i < m_clickableColliders.Length; ++i )
			{
				if ( m_clickableColliders[i] != null )
					m_clickableColliders[i].enabled = false;
			}
		}
		else 
		{
			if ( m_autoHotspotCollider != null )
				m_autoHotspotCollider.enabled = false;

			// Return to default collider
			if ( GetData().ClickableColliderId >= 0 )
			{
				OnClickableColliderIdChanged();
			}
		}
	}
	
	public void UpdateShadow()
	{
		if ( m_shadow == null )
			return;
		m_shadow.SetActive(GetData().ShadowEnabled && m_animShadowOff == false);
		if ( m_shadowAnim != null && string.IsNullOrEmpty(GetData().AnimShadow) == false )
		{
			AnimationClip clip = GetAnimations().Find(item=>string.Equals(GetData().AnimShadow, item == null ? null : item.name, System.StringComparison.OrdinalIgnoreCase));
			m_shadowAnim.Play( clip );
		}
	}

	#endregion
	#region Start/Awake Functions

	// Use this for initialization
	void Awake () 
	{
		m_sprite = GetComponentInChildren<SpriteRenderer>();
		m_powerSprite = m_sprite.GetComponent<PowerSprite>();
		m_spriteAnimator = m_sprite.GetComponent<SpriteAnim>();	

		// Lazy add quest animation triggers if it's not there already
		QuestAnimationTriggers triggers = transform.GetComponent<QuestAnimationTriggers>();
		if ( triggers == null )
			transform.gameObject.AddComponent<QuestAnimationTriggers>();
			
		// Subscribe to callbacks when animation resets. To handle resetting anim event states
		m_spriteAnimator.CallbackOnPlay += OnAnimationReset;
		m_spriteAnimator.CallbackOnStop += OnAnimationReset;
		
		Transform shadowTrns = transform.Find("Shadow");
		if ( shadowTrns != null )
		{
			m_shadow = shadowTrns.gameObject;
			m_shadowAnim = shadowTrns.GetComponentInChildren<SpriteAnim>();
		}
		ExAwake();
	}

	void Start()
	{
		m_firstUpdate = true;
		m_spriteAnimator.NormalizedTime = m_data.AnimationTime; // moved this to first update
		ExStart();
	}

	void OnDestroy()
	{
		PowerQuest.Get?.Pathfinder?.RemoveObstacle(transform);
		ExOnDestroy();
	}
	

	#endregion
	#region Partial Functions for extentions

	partial void ExAwake();
	partial void ExStart();
	partial void ExOnDestroy();
	partial void ExUpdate();

	#endregion
	#region Update Functions
	
	void OnTransitionAnimComplete()
	{
		if ( string.IsNullOrEmpty(m_transitioningToAnim) )
			return;

		// Clears transtition data and starts the queued transition animation.
		string queuedAnim = m_transitioningToAnim;

		// Finished transition, so clear all transition variables
		m_transitioningToAnim = null;
		m_transitionAnim = null;
		m_transitioningFromAnim = null;
		m_data.LoopStartTime = -1;
		m_data.LoopEndTime = -1;	

		PlayAnimInternal(queuedAnim, true);
	}
	
	// When something changes the animation state, 
	void OnAnimStateChange()
	{
		if ( m_animating )
		{
			if ( m_state != eState.Animate )
				SetState(eState.Animate);
		}
		else if ( m_walking )
		{
			if (m_state != eState.Walk )
				SetState(eState.Walk);
		}
		else if ( m_talking )
		{
			if ( m_state != eState.Talk )
				SetState(eState.Talk);
		}
		else 
		{
			if ( m_state != eState.Idle )
				SetState(eState.Idle);
		}
	}

	// Update is called once per frame
	void Update() 
	{	
		if ( m_firstUpdate )
		{
			// set visible to trigger update of renderes being visible first time it's shown
			UpdateVisibility();

			UpdateFacingVisuals(m_data.Facing);
			OnAnimStateChange();
			UpdateSolid();
			UpdateUseSpriteAsHotspot();
			UpdateShadow();
			if ( m_animating )
				m_spriteAnimator.NormalizedTime = m_data.AnimationTime;
			
				
			m_firstUpdate = false;
		}
		m_animChangeTime += Time.deltaTime;

		// Set animation time- so it can be saved
		if ( m_spriteAnimator != null && Animating ) // NB: can't check m_spriteAnimator.IsPlaying()  because it'll keep increasing past end of anim when 'pauseAtEnd' is true, and that's how psa checks for "Playing"
			m_data.AnimationTime = m_spriteAnimator.NormalizedTime;
		else
			m_data.AnimationTime = -1;

		// Update transitional anims
		if ( string.IsNullOrEmpty(m_transitioningToAnim) == false && m_spriteAnimator.Playing == false )
		{
			// Clears transtition data and starts the queued transition animation.
			OnTransitionAnimComplete();
		}

		// Update turn anims
		if ( string.IsNullOrEmpty(m_playAfterTurnAnim) == false && m_spriteAnimator.Playing == false)
		{
			string queuedAnim = m_playAfterTurnAnim;
			m_playAfterTurnAnim = null;
			PlayAnimInternal(queuedAnim, true);
		}

		// Update character always turning to face a character
		m_data.UpdateFacingCharacter();

		// Update turn to face
		UpdateTurnToFace();

		if ( m_animating )
		{
			// If animation has finished, return to idle. 
			if ( m_spriteAnimator.Playing == false && m_data.PauseAnimAtEnd == false )
			{
				m_data.StopAnimation();
				/* calling m_data.StopAnimation() does these anyway
				m_animating = false; 
				OnAnimStateChange();				
				*/
			}
			else if ( PowerQuest.Get.GetSkippingCutscene() && m_spriteAnimator.GetCurrentAnimation().isLooping == false ) 
			{
				m_data.StopAnimation(); // When a cutscene's skipped, assume that any non-looping animation should be ended.				
				/* calling m_data.StopAnimation() does these anyway
				m_animating = false; 
				OnAnimStateChange();				
				*/
			}
		}
		
		// Update any walking
		UpdateWalking();
		UpdateAnimating();

		// update state switch
		switch (m_state) 
		{
			case eState.Idle:
			{
				// Play idle animation after delay (for returning from a played animation where we don't want to flicker to an old idle animation)
				if ( m_playIdleDelayFrames > 0 )
				{
					m_playIdleDelayFrames--;
					if ( m_playIdleDelayFrames <= 0 )
					{
						// Play idle anim						
						PlayAnimInternal(m_data.AnimIdle);				
					}						
				}
			} break;

			case eState.Walk:
			{
			} break;
			case eState.Animate:
			{
			} break;
			
			case eState.Talk:
			{	
			} break;
			default: break;
		}

		if ( m_data.Visible )
			UpdateLipSync();

		// Update sorting order
		if ( m_sprite != null )
			m_sprite.sortingOrder = -Mathf.RoundToInt((m_data.GetPosition().y + m_data.Baseline)*10.0f);

		if ( m_data.UseSpriteAsHotspot && PowerQuest.Get.GetBlocked() == false && m_data.Clickable && m_lastHotspotSprite != m_sprite.sprite && m_autoHotspotCollider != null && m_sprite != null )
		{
			Sprite sprite = m_sprite.sprite;

			m_autoHotspotCollider.pathCount = sprite.GetPhysicsShapeCount();
		
			List<Vector2> path = new List<Vector2>();		
			for (int i = 0; i < m_autoHotspotCollider.pathCount; i++) 
			{
				
				path.Clear();
				sprite.GetPhysicsShape(i, path);
				// Unity's polyes are awful inflated, so try shrinking them a bit. Hopefully this isn't too massively inefficient!! Also more applicable to pixel games
				Vector2[] newPath = Pathfinder.InflatePoly(path.ToArray(),2);
				m_autoHotspotCollider.SetPath(i, newPath);

			}
			if ( m_powerSprite != null )
				m_autoHotspotCollider.offset = m_powerSprite.Offset;
			
			m_lastHotspotSprite = sprite;
		}
		ExUpdate();
	}

	void UpdateAnimating()
	{
		if ( Animating == false )
			return;

		// If animation has finished, return to idle. 
		if ( m_spriteAnimator.Playing == false && m_data.PauseAnimAtEnd == false )
		{
			m_data.Animation = null; // NB: this calls back to CharacterComponent.StopAnimation();
			m_animating = false;
			//OnAnimStateChange();
		}
		else if ( PowerQuest.Get.GetSkippingCutscene() && m_spriteAnimator.GetCurrentAnimation().isLooping == false && m_data.PauseAnimAtEnd == false && m_data.LoopStartTime < 0 )
		{
			m_data.StopAnimation(); // When a cutscene's skipped, assume that any non-looping animation should be ended.
		}
	}



	// Returns true if still walking
	bool UpdateWalking()
	{
		if ( Walking == false )
			return false;

		Vector2 position = m_data.GetPosition();

		bool reachedTarget = false;
		
		if ( m_turningToWalk )
		{
			if ( m_data.GetFaceDirection() == m_data.GetTargetFaceDirection() )// || (m_playingTurningAnim && m_spriteAnimator.Playing == false) )
			{
				// Finished turning, change to walk anim
				m_turningToWalk = false;
				//m_playingTurningAnim = false;
				PlayAnimInternal(m_data.AnimWalk,false);
			}
			else 
			{ 
				// Still turning, don't start walking
				return true;
			} 
		}

		float remainingDeltaTime = Time.deltaTime;
		while ( reachedTarget == false && remainingDeltaTime > 0 )
		{			
			// Update path
			if ( m_path != null && m_pathPointNext > -1)
			{						
				m_targetPos = m_path[m_pathPointNext];
			}
			else if ( m_data.Waypoints.Count > 0 )
			{
				m_targetPos = m_data.Waypoints[0];
			}

			Vector2 direction = m_targetPos - position;
			float dist = Utils.NormalizeMag(ref direction);
			Vector2 finalWalkSpeed = new Vector2( (m_walkSpeedOverride.x != -1 ? m_walkSpeedOverride.x :  m_data.WalkSpeed.x), (m_walkSpeedOverride.y != -1 ? m_walkSpeedOverride.y :  m_data.WalkSpeed.y) );			
			float speed =  (Mathf.Abs(direction.x) * finalWalkSpeed.x) + (Mathf.Abs(direction.y) * finalWalkSpeed.y);
			if ( m_data.AdjustSpeedWithScaling )
				speed *= transform.localScale.y; // scale by speed
			
			if ( dist > 0 )
				m_data.FaceDirection( direction, true );

			if ( dist == 0 || dist < speed * remainingDeltaTime )
			{
				// going to get there this frame
				if ( dist > 0 )
					remainingDeltaTime -= dist/speed;
				position = m_targetPos;

				if ( m_path != null && m_pathPointNext > 0 )
				{
					// Reached pathfinding point, so go to next pathfinding point
					m_pathPointNext++;
					if ( m_pathPointNext >= m_path.Length )
					{
						// End of pathfinding, but may have waypoints to go to next
						reachedTarget = m_data.Waypoints.Count == 0;
						m_path = null;
						m_pathPointNext = -1;
					}					
				}
				else if ( m_data.Waypoints.Count > 0 )
				{
					// Reached waypoint, so go to next waypoint
					m_data.Waypoints.RemoveAt(0);
					reachedTarget = m_data.Waypoints.Count == 0;
				}
				else 
				{
					reachedTarget = true;
				}
			}
			else
			{
				position += direction * speed * remainingDeltaTime;
				remainingDeltaTime -= Time.deltaTime;
			}
		}

		// Finally, set the position! yaay!
		m_data.SetPosition(position);
		
		if ( reachedTarget )
		{		
			m_walking = false;
			OnAnimStateChange();
			if ( m_data.GetFaceAfterWalk() != eFace.None ) 
				m_data.FaceBG(m_data.GetFaceAfterWalk());
			m_path = null;
			m_pathPointNext = -1;
		}

		return reachedTarget == false;
	}

	void LateUpdate()
	{	
		m_flippedLastUpdate = Flipped();
	}

	void UpdateTurnToFace()
	{
		eFace targetDirection = m_data.GetTargetFaceDirection();
		if ( targetDirection == eFace.None )
			return;

		bool facingTarget = targetDirection == m_data.Facing;
		if ( facingTarget == false )
		{
			// Check we're not already at the correct frame, even if not facing target in data.
			if ( CheckTargetDirectionWillChangeAnim() == false )
			{
				// We've reached the target frame, even if the facing isn't reached yet (because missing some animation directions)
				m_data.SetFaceDirection(targetDirection);
				facingTarget = true;
			}
		}

		if ( facingTarget == false )
			m_turnTimer -= Time.deltaTime;
		else 
			m_turnTimer = -1; // Always start turning immediately when was stopped previously

		if ( targetDirection != m_data.Facing && m_turnTimer <= 0 )
		{
			m_turnTimer = 1.0f/m_data.TurnSpeedFPS;

			// We loop until we actually change direction visuals.
			bool changedDirectionVisuals = false;
			while ( changedDirectionVisuals == false && targetDirection != m_data.Facing )
			{

				int currentIndex = System.Array.FindIndex(TURN_ORDER, item=>item == m_data.Facing );
				int targetIndex = System.Array.FindIndex(TURN_ORDER, item=>item == targetDirection );

				// Work out shortest route to target
				int dist = targetIndex-currentIndex;
				int dir = (int)Mathf.Sign(dist);
				if (dist == 0 || Mathf.Abs(dist) == TURN_ORDER.Length/2 )
				{				
					// Same distance no matter which way we turn, so turn based on 
					if ( TURN_ORDER[currentIndex] == eFace.Up || TURN_ORDER[currentIndex] == eFace.Down )
					{	
						// If up or down, go by last r/l direction

						dir = ((m_data.GetFacingVerticalFallback() == eFace.Right) == (TURN_ORDER[currentIndex] == eFace.Up)) ? -1 : 1;
					}
					else 
					{
						// if not up/down, rotate around front
						dir = currentIndex < 3 ? 1 : -1;// (CharacterComponent.ToCardinal(TURN_ORDER[currentIndex]) == eFace.Right) ? -1 : 1;
					}
				}
				else 
				{
					// Otherwise, take shortest route
					dir = Mathf.Abs(dist) < Mathf.Abs((targetIndex-(TURN_ORDER.Length*dir)) - currentIndex) ? dir : -dir;
				}

				currentIndex += dir;
				if ( currentIndex < 0 )
					currentIndex = TURN_ORDER.Length-1;
				else if ( currentIndex >= TURN_ORDER.Length )
					currentIndex = 0;
				
				eFace newDirection = TURN_ORDER[currentIndex];

				// Cache curr flip/anim name
				bool oldFlipped = Flipped();
				string oldAnimName = m_spriteAnimator.ClipName;

				// Update curr face direction state and visuals
				m_data.SetFaceDirection(newDirection);
				UpdateFacingVisuals(newDirection);

				// Set flag if we've found a new direction frame
				changedDirectionVisuals = oldFlipped != Flipped();
				changedDirectionVisuals |= oldAnimName != m_spriteAnimator.ClipName;


			}

			{
				// Check again that we're not already at the correct frame, even if not facing target in data.
				if ( CheckTargetDirectionWillChangeAnim() == false )
				{
					// We've reached the target frame, even if the facing isn't reached yet (because missing some animation directions)
					m_data.SetFaceDirection(targetDirection);
				}
			}
		}		

	}

	#endregion
	#region Public Functions

	
	public void PlayAnimation(string animName)
	{
		PlayAnimInternal(animName,true);
		m_animating = true;
		OnAnimStateChange();
	}

	public void PauseAnimation()
	{
		if ( m_state == eState.Animate )
		{
			m_spriteAnimator.Pause();
		}
	}

	public void ResumeAnimation()
	{
		if ( m_state == eState.Animate )
		{
			m_spriteAnimator.Resume();
		}
	}

	public void StopAnimation()
	{
		// If transitioning to something already, set that as current anim so we can transition out		
		if ( string.IsNullOrEmpty(m_transitioningToAnim) == false )
			OnTransitionAnimComplete();
		m_animating = false;
		OnAnimStateChange();
	}

	// Skip animation transition and/or turning anim
	public void SkipTransition()
	{
		// Update transitional anims
		if ( string.IsNullOrEmpty(m_transitioningToAnim) == false )
		{			
			// Clears transtition data and starts the queued transition animation.			
			OnTransitionAnimComplete();
		}

		// Update turn anims
		if ( string.IsNullOrEmpty(m_playAfterTurnAnim) == false)
		{
			string queuedAnim = m_playAfterTurnAnim;
			m_playAfterTurnAnim = null;
			PlayAnimInternal(queuedAnim, true);
		}
	}
	public bool GetPlayingTransition()
	{
		return IsString.Set(m_transitioningToAnim) || IsString.Set(m_playAfterTurnAnim);
	}

	// AnimState is passed as none, anim will always update, otherwise only if that state has changed.
	public void OnAnimationChanged( eState animState = eState.None )
	{	
		
		if ( animState == eState.None )
			animState = m_state;

		// If current state has changed anim, then change the current state anim immediately
		if ( animState == m_state )
		{
			switch (m_state) 
			{

				case eState.Idle:
				{
					// Play idle anim
					PlayAnimInternal(m_data.AnimIdle, true);
				} break;
				case eState.Walk:
				{
					// Play walk anim
					if ( m_playWalkAnim )
						PlayAnimInternal(m_data.AnimWalk, false);
				} break;
				case eState.Talk:
				{
					// Play talk anim
					PlayAnimInternal(m_data.AnimTalk, true);
				} break;

			}
			UpdateMouthAnim();	
		}
	}

	void UpdateMouthAnim()
	{
		if ( m_mouth != null )
		{
			bool flip;
			bool wasactive = m_mouth.gameObject.activeSelf;
			if ( wasactive == false )
				m_mouth.gameObject.SetActive(true);
			m_mouth.Play( FindDirectionalAnimation( m_data.AnimMouth, out flip ) );
			m_mouth.Pause();
			if ( wasactive == false )
				m_mouth.gameObject.SetActive(false);
		}
	}
	
	public void OnClickableColliderIdChanged()
	{
		if ( m_clickableColliders == null || m_data.UseSpriteAsHotspot )
			return;
		for ( int i = 0; i < m_clickableColliders.Length; ++i )
		{
			if ( m_clickableColliders[i] != null )
				m_clickableColliders[i].enabled = (i == m_data.ClickableColliderId);
		}
	}

	// Called through when "StopWalk" is called from a game script.
	public void StopWalk()
	{
		CancelWalk();		
	}

	// Called when an engine event occurs that stops the walk
	public void CancelWalk()
	{
		m_path = null;
		m_pathPointNext = -1;
		m_targetPos = m_data.GetPosition();
		m_targetEndPos = m_targetPos;
		m_turningToWalk = false;
		m_playWalkAnim = true;
		m_walking = false;
		OnAnimStateChange();
	}

	// Stops the chracter moving, and skips it ot the target position
	public void SkipWalk()
	{
		if ( Walking )
		{
			// Skip to end position
			if ( m_path == null )
			{
				m_data.SetPosition( m_targetPos );
			}
			else 
			{
				m_data.SetPosition(m_path[m_path.Length-1]);
				// update facing if it's a path
				Vector2 direction = (m_path[m_path.Length-1] - m_path[m_path.Length-2]).normalized;
				if ( direction.sqrMagnitude > Mathf.Epsilon )
					m_data.FaceDirection(direction, true);
			}

			m_path = null;
			m_pathPointNext = -1;
			m_walking = false;
			OnAnimStateChange();
		}

	}

	// updates the facing direction and changes the anim to the correct one.
	public void UpdateFacingVisuals( eFace direction)
	{
		if ( direction != m_facing )
		{
			m_facing = direction;
			// Change facing
			if ( string.IsNullOrEmpty(m_currAnimBaseName) == false )
			{
				PlayAnimInternal(m_currAnimBaseName, false);
			}
			if (m_mouth != null && string.IsNullOrEmpty(m_data.AnimMouth) == false)
			{
				UpdateMouthAnim();
			}
		}
	}

	public void MoveToWalkableArea()
	{
		if ( PowerQuest.Get.Pathfinder.IsPointInArea(m_data.GetPosition()) == false )
		{
			m_data.SetPosition( PowerQuest.Get.Pathfinder.GetClosestPointToArea(m_data.GetPosition()) );
		}
	}

	public void WalkTo( Vector2 pos, bool anywhere, bool playWalkAnim, bool couldntFindPath = false )
	{
		if ( m_state == eState.Walk && m_targetEndPos == pos && m_playWalkAnim == playWalkAnim)
			return; // Already walking there. Should probably also check if "anywhere" flag has changed, but that's an edge case
			
		m_playWalkAnim = playWalkAnim;

		Pathfinder pathfinder = PowerQuest.Get.Pathfinder;
		if ( anywhere == false && pathfinder.GetValid() )
		{
			// Update which characters collision should be enabled in this pathfinder
			foreach ( Character character in PowerQuest.Get.GetCharacters() )
			{		
				if ( character.Instance != null )
				{
					if ( GetData().Solid && character != GetData() && character.Solid && character.Room == PowerQuest.Get.GetCurrentRoom() ) // other character's solid, and not this player
						pathfinder.EnableObstacle(character.Instance.transform);
					else 
						pathfinder.DisableObstacle(character.Instance.transform);				
				}
			}

			// Find closest Pos that's navigatable
			
			/* First, use clipper? For now just falling back to this if can't find path normal way.
			{
			RoomComponent rc = PowerQuest.Get.GetCurrentRoom().Instance;
			if ( rc != null )
				pos = rc.GetClosestPoint(m_data.Position, pos);}
			}
			/**/
			Vector2 originalPos = pos;
			if ( pathfinder.IsPointInArea(pos) == false )
				pos = pathfinder.GetClosestPointToArea(pos);			
				
			m_targetPos = pos;
			m_targetEndPos = m_targetPos;

			Vector2[] path = pathfinder.FindPath(m_data.Position, pos);

			if ( path != null && path.Length > 1 )
			{
				m_path = path;
				m_pathPointNext = 1;
				WalkToInternal( m_path[1] );
			}
			else if ( path == null || path.Length == 0 )
			{
				if ( couldntFindPath == false )
				{
					// First time, try again using clipper (possibly slow and definitely buggy, so not doing do it by default. Need more time to get it working as resplacement)
					RoomComponent rc = PowerQuest.Get.GetCurrentRoom().Instance;
					if ( rc != null )
						pos = rc.GetClosestPoint(m_data.Position, originalPos);
					WalkTo(pos, anywhere, playWalkAnim, true );
				}
				else 
				{
					if ( GetData().Solid )
					{
						// Try finding path with 'Solid' off- Since the clipper version doesn't always like that
						Debug.Log("Couldn't Find Path, trying with 'solid' flag off");
						GetData().Solid = false;
						WalkTo(pos,anywhere,playWalkAnim,couldntFindPath);
						GetData().Solid = true;

					}
					else 
					{
						Debug.Log("Couldn't Find Path.");
					}
				}
			}
		}
		else 
		{
			WalkToInternal(pos);
			m_targetEndPos = pos;
		}
	}

	public void StartSay(string text, int id)
	{
		m_currLineId = id;
		m_talking = true;
		OnAnimStateChange();
	}
	public void EndSay()
	{		
		m_currLineId = -1;
		m_talking = false;
		OnAnimStateChange();
	}

	public Vector2 GetTextPosition()
	{
		if ( m_data.TextPositionOverride != Vector2.zero )
			return m_data.TextPositionOverride;
		
		if ( m_sprite == null || m_sprite.sprite == null || m_sprite.enabled == false )
			return m_data.Position; // if sprite is hidden, just use plr position

		float maxHeight = 0;
		if ( m_sprite != null && m_sprite.sprite != null && m_sprite.enabled )
		{
			maxHeight = float.MinValue;
			System.Array.ForEach(m_sprite.sprite.vertices, item=> 
			{
				if (item.y > maxHeight) 
					maxHeight = item.y;
			} );
			if ( m_powerSprite != null )
				maxHeight += m_powerSprite.Offset.y;
		}
		maxHeight *= transform.localScale.y;

		return (Vector2)m_sprite.transform.position + new Vector2(0,maxHeight) + PowerQuest.Get.GetDialogTextOffset() + m_data.TextPositionOffset.Scaled( transform.localScale );
	}

	public void OnSkipCutscene()
	{
		if ( m_state == eState.Walk &&  m_targetPos != m_data.GetPosition() && m_targetPos != Vector2.zero )
		{
			m_data.SetPosition(m_targetPos); 
			m_targetPos = Vector2.zero;
			m_targetEndPos = m_data.Position;
			CancelWalk();
		}
	}


	//
	// private Internal functions
	//

	// Walks in straight line to specified point, setting the walk state if not in it already
	void WalkToInternal( Vector2 pos )
	{
		
		Vector2 toPos = pos - m_data.Position;
		
		if ( toPos.sqrMagnitude < Mathf.Epsilon )
			return;
		m_targetPos = pos;

		bool wasWalking = m_walking;
		m_walking = true;
		OnAnimStateChange();

		if ( Animating == false && m_playWalkAnim && wasWalking == false )
		{
			m_data.FaceDirection( toPos );

			if ( CheckTargetDirectionWillChangeAnim() ) // check if the walk anim will change. if it won't, just walk immediately without turning to face first.
			{
				// If not instantly at correct position, go back to idle anim, and rotate
				m_turningToWalk = true;
				PlayAnimInternal(m_data.AnimIdle);
			}
			else 
			{
				m_data.SetFaceDirection(m_data.GetTargetFaceDirection()); // Not turning to face, so assume we've reached the target (due to anim frames we may not have actually)
			}
		}
	}

	// Returns true if the target direction has a different animation or flip than the current direction. Used to work out if need to "turn to face"
	bool CheckTargetDirectionWillChangeAnim()
	{
		bool targetAnimationFlipped;
		string targetAnimationName = FindDirectionalAnimationName( m_data.GetTargetFaceDirection(), m_currAnimBaseName, out targetAnimationFlipped );
		return ( targetAnimationFlipped != Flipped() || targetAnimationName != m_spriteAnimator.ClipName );
	}
			
	void SetState(eState state)
	{	
		eState oldState = m_state;
		OnExitState(oldState, state);
		m_state = state;
		OnEnterState(oldState, state);
	}

	void OnEnterState( eState oldState, eState newState )
	{
		switch (newState) 
		{
			case eState.Idle:
			{
				if ( m_playIdleDelayFrames <= 0 )
				{
					// Play idle anim
					PlayAnimInternal(m_data.AnimIdle);
				}

			} break;
			case eState.Walk:
			{
				// Play walk anim
				if ( m_playWalkAnim )
				{
					PlayAnimInternal(m_data.AnimWalk);
					// Clicking multiple times can stop then start the walk anim again, this should stop it re-starting the animation
					m_playIdleDelayFrames = 2;
				}
			} break;
			case eState.Talk:
			{
				// Play talk anim
				//if ( string.IsNullOrEmpty(m_transitionAnim) || m_transitionAnim.StartsWith(m_data.AnimTalk) == false ) // Don't play again if alteady transitioning to it
				if ( IsString.Set(m_data.AnimTalk) )
					PlayAnimInternal(m_data.AnimTalk);
				else 
					PlayAnimInternal(m_data.AnimIdle);  // Fall back to idle anim when there's no talk anim

				if ( m_data.LipSyncEnabled && m_mouthNode == null )
					m_spriteAnimator.Pause();
				
			} break;
			case eState.Animate:
			{
				m_playIdleDelayFrames = 2;
			} break;
			default: break;

		}
	}

	void OnExitState( eState oldState, eState newState )
	{
		switch (oldState) 
		{
			case eState.Idle:
			{				
			} break;
			case eState.Walk:
			{
			} break;
			case eState.Talk:
			{
			} break;
			default: break;
		}
	}

	// Should be called when an animtion is stopped for any reason. Used to reset animation tags
	void OnAnimationReset()
	{
		AnimWalkSpeedReset();
	}
	
	// Plays directional anim and handles flipping
	void PlayAnimInternal(string animName, bool fromStart = true)
	{	
		//Debug.Log("PlayAnim: "+animName);
		string animNameNoPrefix = animName;
		if ( string.IsNullOrEmpty(GetData().AnimPrefix) == false )
			animName = GetData().AnimPrefix + animName;

		if ( IsString.Set(m_transitionAnim) && m_transitionAnim.StartsWith(animName, System.StringComparison.OrdinalIgnoreCase) )
			return; // Already transitioning to this animation, so don't try and play it again

		if ( m_currTurnAnim != null && animName != m_currTurnAnim && animNameNoPrefix != m_currTurnAnim )
		{
			// Check if should cancel turn anim cause another anim is playing
			m_currTurnAnim = null;
			m_playAfterTurnAnim = null;
		}
		
		bool ignoreTransitionLoopTime = false;	

		string oldClipName = m_spriteAnimator.ClipName;		

		bool flip;
		AnimationClip clip = FindDirectionalAnimation( animName, out flip );
	
		// If clip is null and there was a prefix, try without the prefix
		if ( clip == null && IsString.Set(GetData().AnimPrefix) )
		{
			animName = animNameNoPrefix;
			clip = FindDirectionalAnimation( animName, out flip );			
		}

		// Experimental "Transition" code. Not really working for flipping and stuff
		
		if ( IsString.Set(m_transitionAnim) && m_transitionAnim.StartsWith(animName, System.StringComparison.OrdinalIgnoreCase) == false && oldClipName != clip.name )
		{
			string newTransitionName = GetTransitionAnim(m_transitioningFromAnim, m_flippedLastUpdate, clip.name, flip);			
			if ( m_animChangeTime < 0.05f && IsString.Set(newTransitionName) && m_transitionAnim.StartsWith(newTransitionName, System.StringComparison.OrdinalIgnoreCase) )
			{
				// If already transitioning to the same thing...
				// Fix for when we change anims a few times really quick. (like resetting pose back to default, then starting/stopping talking). In this case the transitionTo anim has chnaged, but the transition should remain.
				
				ignoreTransitionLoopTime = true;	// This flag stops anim with loop tags from restarting
				oldClipName = m_transitioningFromAnim;	// hack the 'oldClipName'
				//Debug.Log($"Ignored transition to {m_transitioningToAnim} (with {animName}). TransAnim: {m_transitionAnim}, TransitioningFrom: {m_transitioningFromAnim}");
			}
			else 
			{			
				// Cancel the current transition
				// Changing to another anim that's not the transition target, so cancel the transition. NB: This probably breaks things for walking, where this funtion is played more often
				// TODO: this isn't handling flipping- Should probably remove the "flipping" stuff if wanna keep this code
				//Debug.Log($"Cleared transition to {m_transitioningToAnim} (with {animName}). TransAnim: {m_transitionAnim},  TransitioningFrom: {m_transitioningFromAnim}, NewTransName: {newTransitionName}");
				m_transitioningFromAnim = null;
				m_transitioningToAnim = null;
				m_transitionAnim = null;
			}
		}

		// Experimental "Transition" code.
		if ( clip != null && oldClipName != clip.name ) // ignore transitions to same animation
		{
			string transitionName = GetTransitionAnim(oldClipName, m_flippedLastUpdate, clip.name, flip);
			if ( IsString.Set(transitionName) )
			{
				//Debug.LogFormat("{0} to {1} ({2}): found", oldClipName, clip.name, flip != m_flippedLastUpdate);
				// Found transition				
				m_transitioningFromAnim = oldClipName;
				m_transitioningToAnim = animName;
				animName = transitionName;
				clip = FindDirectionalAnimation( animName, out flip );
				m_transitionAnim = clip.name; 
			}
			else if ( m_data.LoopStartTime > 0 && m_data.LoopEndTime > 0 )
			{
				// Transition out from looping anim
				if ( ignoreTransitionLoopTime == false && m_data.LoopEndTime > m_data.LoopStartTime )
					m_spriteAnimator.NormalizedTime = m_data.LoopEndTime;
				
				m_transitioningFromAnim = oldClipName;
				m_transitionAnim = oldClipName;
				m_transitioningToAnim = animName;

				m_animChangeTime = 0; // treat this like an anim change							
				return; // return so we just keep playing the current anim
			}			
			else
			{
				//Debug.LogFormat("{0} to {1} ({2}): NOT found", oldClipName, clip.name, flip != m_flippedLastUpdate);
			}
		}

		if ( clip != null )
		{
			m_currAnimBaseName = animName;
			if ( oldClipName != clip.name )
			{
				//Debug.Log("Starting: "+ clip.name);
				m_data.LoopStartTime = -1;
				m_data.LoopEndTime = -1;
				m_animChangeTime = 0;
				if ( fromStart || m_spriteAnimator.Clip == null  )
				{
					m_spriteAnimator.Play(clip);
				}
				else // if ( m_spriteAnimator.IsPlaying(clip) == false )
				{
					// continue from same time
					float animTime = Mathf.Clamp01(m_spriteAnimator.NormalizedTime+Time.deltaTime);
					m_spriteAnimator.Play(clip);
					m_spriteAnimator.NormalizedTime = animTime;					
				}
			}
		}
		if ( flip != Flipped() )
			transform.localScale = new Vector3(-transform.localScale.x,transform.localScale.y,transform.localScale.z);

		if ( clip == null && Debug.isDebugBuild && animName != "Idle" && animName != "Talk"  && animName != "Walk" && string.IsNullOrEmpty(animName) == false )
			Debug.Log("Failed to find animation: "+animName, gameObject);
		
	}

	bool Flipped() { return Mathf.Sign(transform.localScale.x) < 0; }

	//
	//  Directional animation- may be moved so it can be used by props too?
	//

	// Appended to names to find direction. Corresponds to eFace enum: Left, Right, Down, Up, DownLeft, DownRight, UpLeft, UpRight
	static readonly string[] DIRECTION_POSTFIX = 
	{
		"L","R","D","U","DL","DR","UL","UR"
	};
	static readonly int DIRECTION_COUNT = (int)eFace.UpRight + 1;

	AnimationClip FindDirectionalAnimation( string name, out bool flip )
	{
	    string finalName = FindDirectionalAnimationName(name, out flip);
	    if ( string.IsNullOrEmpty( finalName ) )
	        return null;
		return GetAnimations().Find(item=>string.Equals(finalName, item == null ? null : item.name, System.StringComparison.OrdinalIgnoreCase));
	}

	// Fun function to work out which animation direction to play!
	string FindDirectionalAnimationName( string name, out bool flip ) { return FindDirectionalAnimationName(m_data.Facing,name,out flip); }
	string FindDirectionalAnimationName( eFace facing, string name, out bool flip )
	{
		if ( name == null )
			name = string.Empty;
		flip = false;
		BitMask availableDirections = new BitMask();
		int lengthOriginal = name.Length;
		int lengthCardinal = name.Length+1;
		int lengthDiagonal = name.Length+2;
		for ( int i = 0; i < GetAnimations().Count; ++i )
		{
			//int thisPriority = int.MaxValue;
			if ( GetAnimations()[i] == null )
				continue;
			string clipName =  GetAnimations()[i].name;
			bool original = clipName.Length == lengthOriginal;
			bool cardinal = clipName.Length == lengthCardinal;
			bool diagonal = clipName.Length == lengthDiagonal;
			if ( (cardinal || diagonal || original) && clipName.StartsWith(name, System.StringComparison.OrdinalIgnoreCase) )
			{
				if ( cardinal )
				{
					for ( int j = 0; j <= (int)eFace.Up; ++j )
					{
						if ( clipName[lengthOriginal] == DIRECTION_POSTFIX[j][0] )
						{
							availableDirections.SetAt(j);
							break;
						}
					}
				}
				else if ( diagonal )
				{
					string postfix = clipName.Substring(lengthOriginal);
					for ( int j = (int)eFace.DownLeft; j <= (int)eFace.UpRight; ++j )
					{
						if ( postfix == DIRECTION_POSTFIX[j] )
						{
							availableDirections.SetAt(j);
							break;
						}
					}
				}
				else 
				{
					availableDirections.SetAt(DIRECTION_COUNT);
				}
			}
		}

		/*
			Find closest matching, direction animation			
			- Try perfect match
			- If it's horizontal
				- Try flipping
				- If its cardinal (l/r) try diagonal down, then diagonal up (including flipped)
				- If it's non-cardinal, try cardinal, then the other diagonal (including flipped)
			- If it's vertical (up/down) try the last facing direction (closest diagonal, then, cardinal, then farthest diagonal) (including flipped)
			- Lastly, use the default non-directional
			
			Eg: if facing left, priority will be 
				- exact match (IdleL)
				- Flipped match (IdleR with flip = true)
				- Down-Diagonal (IdleDL), then same but flipped (IdleDR with flip = true)
				- Up-Diagonal (IdleUL), then same but flipped (IdleUR with flip = true)
				- Down (IdleD), Up (IdleU)
				- Non directional (Idle)

			Eg: if facing Down, and was previously facing Left, priority will be 
				- exact match (IdleD)
				- Down-Diagonal in direction of last facing (IdleDL), then same but flipped (IdleDR with flip = true)
				- Horzontal in direction of last facing (IdleL), then same but flipped (IdleR with flip = true)
				- Up-Diagonal (IdleUL), then same but flipped (IdleUR with flip = true)
				- Up (IdleU)
				- Non directional (Idle)
		*/

		bool horizontal = (facing != eFace.Up && facing != eFace.Down);
		bool facingCardinal = (facing == eFace.Left || facing == eFace.Right) || (facing == eFace.Up || facing == eFace.Down);

		// If nothing matches, just return null
		if ( availableDirections.Value == 0 )
		{
			// No amimation of that name at all! But can still flip if facing left
			flip = (horizontal ? ToCardinal(facing) : m_data.GetFacingVerticalFallback()) == eFace.Left;
			return null;
		}

		// If only the default direction is set, just return the name
		if ( availableDirections.Value == 1 << DIRECTION_COUNT )
			return name;			
							

		// Try Exact match + flipped
		if ( TryGetAnimName( facing, ref name, ref flip, availableDirections ) )
			return name;

		// If non-up/down
		if ( horizontal ) 
		{
			// If Cardinal- Try Diagonal down, then diagonal up
			if ( facingCardinal )
			{
				// Try diagonal Down,then diagonal down + flipped
				if ( TryGetAnimName( ToDiagDown(facing), ref name, ref flip, availableDirections ) )
					return name;

				// Try diagonal Up, then diagonal up + flipped
				if ( TryGetAnimName( ToDiagUp(facing), ref name, ref flip, availableDirections ) )
					return name;
			}
			if ( facingCardinal == false )
			{
				// Try cardinal + flipped
				if ( TryGetAnimName( ToCardinal(facing), ref name, ref flip, availableDirections ) )
					return name;

				// Try other diagonal + flipped
				if ( TryGetAnimName( FlipV(facing), ref name, ref flip, availableDirections ) )
					return name;
			}

		}
		else 
		{			
			// Handle Vertical cardinal directions (up or down)

			// If going up or down, use the closest in the last faced direction (eg: if was going Left, and changed to Up, with no Up anim. Keep using Left anim (or closest diagonal) )
			if ( facing == eFace.Up )
			{
				if ( TryGetAnimName( ToDiagUp(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
				if ( TryGetAnimName( ToCardinal(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
				if ( TryGetAnimName( ToDiagDown(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
			}
			else if ( facing == eFace.Down )
			{
				if ( TryGetAnimName( ToDiagDown(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
				if ( TryGetAnimName( ToCardinal(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
				if ( TryGetAnimName( ToDiagUp(m_data.GetFacingVerticalFallback()), ref name, ref flip, availableDirections ) )
					return name;
			}
		}

		// No matches yet, try down, then up
		if ( TryGetAnimName( eFace.Down, ref name, ref flip, availableDirections ) )
			return name;
		if ( TryGetAnimName( eFace.Up, ref name, ref flip, availableDirections ) )
			return name;

		// Finally, try a defaul anim, since there's no directional ones
		if ( availableDirections.IsSet(DIRECTION_COUNT) )
		{
			return name;
		}

		return null;
	}

	// Static helpers for finding directional animation name
	static bool TryGetAnimName(eFace facing, ref string name, ref bool flip, BitMask availableDirections)
	{		
		if ( availableDirections.IsSet((int)facing) )
		{
			name = name+DIRECTION_POSTFIX[(int)facing];
			return true;
		}
		if ( availableDirections.IsSet((int)FlipH(facing)) )
		{
			flip = true;
			name = name+DIRECTION_POSTFIX[(int)FlipH(facing)];
			return true;
		}
		return false;
	}
	static eFace FlipH(eFace original)
	{
		switch(original)
		{
		case eFace.Right: return eFace.Left;
		case eFace.Left: return eFace.Right;
		case eFace.UpLeft: return eFace.UpRight;
		case eFace.UpRight: return eFace.UpLeft;
		case eFace.DownLeft: return eFace.DownRight;
		case eFace.DownRight: return eFace.DownLeft;
		}
		return original;
	}
	static eFace FlipV(eFace original)
	{
		switch(original)
		{
		case eFace.Up: return eFace.Down;
		case eFace.UpLeft: return eFace.DownRight;
		case eFace.UpRight: return eFace.DownRight;
		case eFace.Down: return eFace.Down;
		case eFace.DownLeft: return eFace.UpLeft;
		case eFace.DownRight: return eFace.UpRight;
		}
		return original;
	}
	static eFace ToDiagDown(eFace cardinalH)
	{
		switch(cardinalH)
		{
		case eFace.Right: return eFace.DownRight;
		case eFace.Left: return eFace.DownLeft;
		case eFace.UpLeft: return eFace.DownLeft;
		case eFace.UpRight: return eFace.DownRight;
		}
		return cardinalH;
	}
	static eFace ToDiagUp(eFace cardinalH)
	{
		switch(cardinalH)
		{
		case eFace.Right: return eFace.UpRight;
		case eFace.Left: return eFace.UpLeft;
		case eFace.DownLeft: return eFace.UpLeft;
		case eFace.DownRight: return eFace.UpRight;
		}
		return cardinalH;
	}
	public static eFace ToCardinal(eFace diagonal)
	{
		switch(diagonal)
		{
		case eFace.UpLeft: return eFace.Left;
		case eFace.UpRight: return eFace.Right;
		case eFace.DownLeft: return eFace.Left;
		case eFace.DownRight: return eFace.Right;
		}
		return diagonal;
	}

	#endregion
	#region Anim events

	void AnimSound(Object obj)
	{
	    if ( obj != null && (obj as GameObject) != null )
	    {
			SystemAudio.Play((obj as GameObject).GetComponent<AudioCue>());
	    }
	}

	void AnimSound(string sound)
	{
		SystemAudio.Play(sound);	    
	}

	void AnimFootstep()
	{
		SystemAudio.Play(m_data.FootstepSound);
	}

	void AnimMouth(string animName)
	{
		m_data.AnimMouth = animName;
	}


	void AnimLoopStart()
	{
		if ( m_spriteAnimator.ClipName == m_transitionAnim )
			return; // don't loop when transitioning
		m_data.LoopStartTime = m_spriteAnimator.NormalizedTime;
	}

	void AnimLoopEnd()
	{
		if ( m_spriteAnimator.ClipName == m_transitionAnim )
			return; // don't loop when transitioning
		m_data.LoopEndTime = m_spriteAnimator.NormalizedTime;
		m_spriteAnimator.NormalizedTime = m_data.LoopStartTime;		
	}
	
	/// Offset character from node 1 to node 2	
	void AnimOffset()
	{	
		SpriteAnimNodes nodes = m_data.Instance.GetComponent<SpriteAnimNodes>();
		Vector2 posA = nodes.GetPosition(1);
		Vector2 posB = nodes.GetPosition(2);
		m_data.SetPosition( m_data.GetPosition()+ (posA - posB) );
	}

	Vector2 m_walkSpeedOverride = -Vector2.one;
	// Overrides walk speed temporarily (until end of anim, or WalkSpeedReset tag is hit). If 'speed' is less than one, it's treated as a multiplier (eg: 0.5 will make character walk half-speed)
	void AnimWalkSpeed(float speed) { AnimWalkSpeedX(speed); AnimWalkSpeedY(speed); }
	void AnimWalkSpeedX(float speed) { m_walkSpeedOverride.x = (speed > -1.0f && speed < 1.0f ) ? (m_data.WalkSpeed.x * speed) : speed; }
	void AnimWalkSpeedY(float speed) { m_walkSpeedOverride.y = (speed > -1.0f && speed < 1.0f ) ? (m_data.WalkSpeed.y * speed) : speed; }
	void AnimWalkSpeedReset() { m_walkSpeedOverride = -Vector2.one; }
	
	// spawn game object at character's node
	void AnimSpawn(GameObject obj)
	{
		if ( obj != null )
			GameObject.Instantiate( obj, transform.position, Quaternion.identity );
	}
	
	bool m_animShadowOff = false;
	public void AnimShadowOff() 
	{ 
		m_animShadowOff = true; 
		UpdateShadow(); 
	}
	public void AnimShadowReset() 
	{
		m_animShadowOff = false; 
		UpdateShadow(); 
	}

	#endregion
	#region Lipsync stuff

	static readonly int NUM_LIP_SYNC_FRAMES = 6;

	void UpdateLipSync()
	{
		// Lipsync must be enabled to have seperate mouths at the moment. Those two should be seperated at some point.
		if ( m_data.LipSyncEnabled == false )
			return;

		bool useMouth = string.IsNullOrEmpty( m_data.AnimMouth ) == false; 

		if ( useMouth && m_mouth == null )
		{
			// Lazy add mouth component
			GameObject obj = new GameObject("Mouth", typeof(PowerSprite), typeof(SpriteAnim));
			m_mouth = obj.GetComponent<SpriteAnim>();
			if (  m_mouthNode == null )				
				m_mouthNode = gameObject.GetComponent<SpriteAnimNodes>();
			if (  m_mouthNode == null )				
				m_mouthNode = gameObject.AddComponent<SpriteAnimNodes>();
			obj.transform.SetParent(transform,false);
			bool flip;
			m_mouth.Play( FindDirectionalAnimation( m_data.AnimMouth, out flip ) );
			m_mouth.Pause();
			if ( m_mouthNode == null )				
				useMouth = false;		

			// Note: idle anim needs restarting			
			if ( m_animating == false ) // note: this is necessary when loading a save game to start anims at correct time (we don't want to restart), and order of ops makes this fiddly. May result in 1st mouth anim not playing if anim is overriden?
				m_spriteAnimator.Play( m_spriteAnimator.GetCurrentAnimation() );
		}

		// Check if playing talk anim, or showing dialog & have a mouth frame
		// the reason we don't check for useMouth, is so we can support lipsync without seperate mouth. 
		if ( Talking ) 
		{
			//  Check if should  hide mouth this frame			
			if ( useMouth && m_mouthNode.GetPositionRaw(0) == Vector2.zero )
			{
				// NB: Setting the mouth frame to 0,0 in the editor actually sets its position to 0.0001,0.0001. 
				m_mouth.gameObject.SetActive(false);	
				return; // If mouth is disabled this frame, just hide it and don't do anything else
			}

			// Set talking frame (if talking)
			if ( useMouth )
			{
				// Attach mouth to node
				m_mouth.gameObject.SetActive(true);
				m_mouth.GetComponent<SpriteRenderer>().sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
			}

			SpriteAnim talkAnimator = useMouth ? m_mouth : m_spriteAnimator;

	        // Update frames for lip sync
			TextData data = SystemText.FindTextData( m_currLineId, m_data.ScriptName );
	        // get time from audio source
	        float time = 0; 
	        if ( m_data.GetDialogAudioSource() != null )
					time = m_data.GetDialogAudioSource().time-0.1f; // added a bit for latency

			// Get character from time
			int index = -1;
			if ( data != null )
				index = System.Array.FindIndex( data.m_phonesTime, item => item > time );
	        index--;

			char character = SystemText.Get.GetLipsyncUsesXShape() ? 'X' : 'A'; // default is mouth closed
			if ( index >= 0 && index < data.m_phonesCharacter.Length )
	            character = data.m_phonesCharacter[index];


	        // map character to frame
			int finalLipSyncFrames = NUM_LIP_SYNC_FRAMES + SystemText.Get.GetLipsyncExtendedMouthShapes().Length;
			int characterId = Mathf.Min(character-'A', finalLipSyncFrames-1);

			// Debug.Log(character+": "+characterId+", "+((float)characterId+0.5f)/(float)finalLipSyncFrames);

			float newAnimTime = ((float)characterId+0.5f)/(float)finalLipSyncFrames;

			if ( data == null || data.m_phonesTime.Length <= 0 )
			{				
				// Don't have lipsync data yet
				if ( Utils.GetTimeIncrementPassed(0.1f) && Random.value > 0.2f )
					newAnimTime = Random.value; // randomly change the value at multiples of .05 sec
				else
					newAnimTime = talkAnimator.NormalizedTime;  // otherwise, don't change the time
			}

			talkAnimator.SetNormalizedTime( newAnimTime );
			talkAnimator.Pause();

			if ( useMouth )
			{
				// Attach mouth to node
				Transform mouthTrans = m_mouth.transform;
				Vector2 position = m_mouthNode.GetPosition(0);

				/* - this is already done above
				// hide if no position set
				if ( (position-(Vector2)transform.position).sqrMagnitude < 0.01f )
					m_mouth.gameObject.SetActive(false);								
				*/
				if ( m_powerSprite != null )
				{
					Vector2 spriteOffset = m_powerSprite.Offset;
					spriteOffset.Scale(transform.localScale);
					position += spriteOffset;
				}

				// Adjust for power sprite offset
				m_mouth.transform.position = position;

				// Flip if point is rotated to left- This is currently necessary for talk anims that face Left that aren't flipped in code
				if ( m_mouthNode.GetAngleRaw(0) > 90 )
					m_mouth.transform.localScale = new Vector3(-1,1,1);
				else 
					m_mouth.transform.localScale = Vector3.one;
			}
		}
		else 
		{
			// Else, hide mouth (if there is one)
			if ( m_mouth != null )
				m_mouth.gameObject.SetActive(false);
		}  
	}  
}

#endregion
}
