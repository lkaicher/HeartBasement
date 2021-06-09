using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using System.Text.RegularExpressions;

namespace PowerTools.Quest
{

[SelectionBase]
public class CharacterComponent : MonoBehaviour 
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
	};

	#endregion
	#region Vars: Editor

	[Tooltip("Character data")]
	[SerializeField] Character m_data = new Character();

	[Tooltip("If the character needs to swap between multiple clickable colliders, hook them up here")]
	[SerializeField, ReorderableArray] Collider2D[] m_clickableColliders = null;	// If multiple colliders that can be enabled/disabled

	[Tooltip("If character's changes from one anim to another, matching the from/to in this list, the transition anim will be played first")]
	[SerializeField, ReorderableArray] TurnAnim[] m_turnAnims = null;

	[Tooltip("EXPERIMENTAL: If character's changes from one anim to another, matching the from/to in this list, the transition anim will be played first")]
	[SerializeField, ReorderableArray] TransitionAnim[] m_transitionAnims = null;

	[ReadOnly][SerializeField] List<AnimationClip> m_animations = new List<AnimationClip>();


	#endregion
	#region Vars: Private

	Vector2 m_targetPos = Vector2.zero;
	Vector2 m_targetEndPos = Vector2.zero;
	Character.eState m_state = Character.eState.None;

	SpriteRenderer m_sprite = null;
	PowerSprite m_powerSprite = null;
	SpriteAnim m_spriteAnimator = null;

	int m_playIdleDelayFrames = 0;
	Vector2[] m_path = null;
	int m_pathPointNext = -1;
	bool m_turningToWalk = false;

	eFace m_facing = eFace.Up;	// Used to check if facing has changed in an update facing call
	string m_currAnimBaseName = null; // Used to change animation when face direction changes

	bool m_pauseAnimAtEnd = false; // If true, animation (from PlayAnimation()) will pause on last frame and not return to Idle (Until StopAnimation is called). TODO: if useful, move to data so can be saved.

	int m_currLineId =-1;
	float m_turnTimer = 0;

	SpriteAnim m_mouth = null;
	SpriteAnimNodes m_mouthNode = null;

	// Experimental "Transition" code. Not really working for flipping and stuff
	string m_transitionAnim = null;
	string m_transitioningToAnim = null;
	string m_transitioningFromAnim = null;
	bool m_flippedLastUpdate = false; // unsure if this is used, look at cleaning up.
	float m_animChangeTime = 0; // how long the current clip has been playing
	float m_loopStartTime = -1; // whether a loop tag was hit, and at what time
	float m_loopEndTime = -1; // whether a loop end tag was hit, and at what time	

	#endregion
	#region Public access Functions

	public Character GetData() { return m_data; }
	public void SetData(Character data) { m_data = data; }
	public SpriteRenderer GetSprite() { return m_sprite; }
	public SpriteAnim GetSpriteAnimator() { return m_spriteAnimator; }
	public Character.eState GetState()	{ return m_state; }

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

	string GetTurnAnim()
	{
		TurnAnim result = System.Array.Find(m_turnAnims, item=> 
			{
				return ( BitMask.IsSet(item.fromDirection,(int)(m_facing)) && BitMask.IsSet(item.toDirection,(int)m_data.GetTargetFaceDirection()) &&
					string.Equals(item.fromAnim, m_currAnimBaseName, System.StringComparison.OrdinalIgnoreCase) );
			});
		return result != null ? result.anim : null;
	}

	string m_playAfterTurnAnim = null;
	string m_currTurnAnim = null;

	public bool StartTurnAnimation()
	{
		string turnAnim = GetTurnAnim();
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
		bool shouldShow = m_data.Visible;
		if ( GetSpriteAnimator() != null )
			GetSpriteAnimator().enabled = shouldShow;
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		System.Array.ForEach(renderers, renderer => 
		{
				renderer.enabled = shouldShow;
		});
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
	}

	void Start()
	{
		// set visible to trigger update of renderes being visible first time it's shown
		UpdateVisibility();

		UpdateFacingVisuals(m_data.Facing);
		if ( m_state == Character.eState.None )
		{
			SetState(Character.eState.Idle);
		}

	}


	#endregion
	#region Update Functions
	
	// Update is called once per frame
	void Update() 
	{	
		m_animChangeTime += Time.deltaTime;
		// Update transitional anims
		if ( string.IsNullOrEmpty(m_transitioningToAnim) == false && m_spriteAnimator.Playing == false )
		{
			string queuedAnim = m_transitioningToAnim;

			// Finished transition, so clear all transition variables
			m_transitioningToAnim = null;
			m_transitionAnim = null;
			m_transitioningFromAnim = null;
			m_loopStartTime = -1;
			m_loopEndTime = -1;	

			PlayAnimInternal(queuedAnim, true);
		}

		// Update turn anims
		if ( string.IsNullOrEmpty(m_playAfterTurnAnim) == false && m_spriteAnimator.Playing == false)
		{
			string queuedAnim = m_playAfterTurnAnim;
			m_playAfterTurnAnim = null;
			PlayAnimInternal(queuedAnim, true);
		}


		// Update turn to face
		UpdateTurnToFace();

		// update state switch
		switch (m_state) 
		{
			case Character.eState.Idle:
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

			case Character.eState.Walk:
			{

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
						break;
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

					Vector2 direction = m_targetPos - position;
					float dist = Utils.NormalizeMag(ref direction);
					Vector2 finalWalkSpeed = new Vector2( (m_walkSpeedOverride.x != -1 ? m_walkSpeedOverride.x :  m_data.WalkSpeed.x), (m_walkSpeedOverride.y != -1 ? m_walkSpeedOverride.y :  m_data.WalkSpeed.y) );
					float speed =  (Mathf.Abs(direction.x) * finalWalkSpeed.x) + (Mathf.Abs(direction.y) * finalWalkSpeed.y);
					if ( m_data.AdjustSpeedWithScaling )
						speed *= transform.localScale.y; // scale by speed. In future this would be an option
					if ( dist == 0 )
					{
						reachedTarget = true;
						break;
					}
					
					m_data.FaceDirection( direction, true );

					if ( dist < speed * remainingDeltaTime )
					{
						// going to get there this frame
						remainingDeltaTime -= dist/speed;
						position = m_targetPos;

						if ( m_path != null && m_pathPointNext > 0 )
						{
							// Reached pathfinding point, so go to next pathfinding point
							m_pathPointNext++;
							reachedTarget = m_pathPointNext >= m_path.Length;
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

				if ( reachedTarget )
				{
					m_path = null;
					m_pathPointNext = -1;
					SetState(Character.eState.Idle);
					
					if ( m_data.GetFaceAfterWalk() != eFace.None ) 
						m_data.FaceBG(m_data.GetFaceAfterWalk());
				}

				// Finally, set the position! yaay!
				m_data.SetPosition(position);

			} break;
			case Character.eState.Animate:
			{
				// If animation has finished, return to idle. 
				if ( m_spriteAnimator.Playing == false && m_pauseAnimAtEnd == false )
					SetState(Character.eState.Idle);
				else if ( PowerQuest.Get.GetSkipCutscene() && m_spriteAnimator.GetCurrentAnimation().isLooping == false ) 
					m_data.StopAnimation(); // When a cutscene's skipped, assume that any non-looping animation should be ended.
			} break;
			
			case Character.eState.Talk:
			{	
			} break;
			default: break;
		}

		if ( m_data.Visible )
			UpdateLipSync();

		// Update sorting order
		SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>(false);
		if ( sprite != null )
		{
			sprite.sortingOrder = -Mathf.RoundToInt((m_data.GetPosition().y + m_data.Baseline)*10.0f);
		}
	}

	void LateUpdate()
	{	
		m_flippedLastUpdate = Flipped();
	}

	void UpdateTurnToFace()
	{
		eFace targetDirection = m_data.GetTargetFaceDirection();

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

	
	public void PlayAnimation(string animName, bool pauseAtEnd = false )
	{
		m_pauseAnimAtEnd = pauseAtEnd;
		PlayAnimInternal(animName,true);
		SetState(Character.eState.Animate);
	}

	public void PauseAnimation()
	{
		if ( m_state == Character.eState.Animate )
		{
			m_spriteAnimator.Pause();
		}
	}

	public void ResumeAnimation()
	{
		m_pauseAnimAtEnd = false;
		if ( m_state == Character.eState.Animate )
		{
			m_spriteAnimator.Resume();
		}
	}

	public void StopAnimation()
	{
		m_pauseAnimAtEnd = false;
		if ( m_state == Character.eState.Animate )
			SetState(Character.eState.Idle);
	}

	public void OnAnimationChanged( Character.eState animState )
	{		
		// If current state has changed anim, then change the current state anim immediately
		if ( animState == m_state )
		{
			switch (m_state) 
			{

				case Character.eState.Idle:
				{
					// Play idle anim
					PlayAnimInternal(m_data.AnimIdle, true);
				} break;
				case Character.eState.Walk:
				{
					// Play walk anim
					PlayAnimInternal(m_data.AnimWalk, false);
				} break;
				case Character.eState.Talk:
				{
					// Play talk anim
					PlayAnimInternal(m_data.AnimTalk, true);
					if ( m_mouth != null && m_mouth.isActiveAndEnabled )
					{
						bool flip;
						m_mouth.Play( FindDirectionalAnimation( m_data.AnimMouth, out flip ) );
						m_mouth.Pause();
					}
				} break;

			}
		}
	}
	
	public void OnClickableColliderIdChanged()
	{
		if ( m_clickableColliders == null )
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
		if ( GetIsWalking() )
		{
			SetState(Character.eState.Idle);
		}
	}

	// Stops the chracter moving, and skips it ot the target position
	public void SkipWalk()
	{
		if ( GetIsWalking() )
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
			SetState(Character.eState.Idle);
		}

	}

	public bool GetIsWalking()
	{
		return GetState() == Character.eState.Walk;
	}

	/// Since player can be takling while walking/animating, this check only returns true if actually playing talk anim (in talk state). For ANY time talking, use charactredata.Talking property.
	public bool GetInTalkState()
	{
		return GetState() == Character.eState.Talk;
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
	}

	public void MoveToWalkableArea()
	{
		if ( PowerQuest.Get.Pathfinder.IsPointInArea(m_data.GetPosition()) == false )
		{
			m_data.SetPosition( PowerQuest.Get.Pathfinder.GetClosestPointToArea(m_data.GetPosition()) );
		}
	}

	public void WalkTo( Vector2 pos, bool anywhere = false )
	{
		if ( m_state == Character.eState.Walk && m_targetEndPos == pos )
			return; // Already walking there.
		
		if ( anywhere == false && PowerQuest.Get.Pathfinder.GetValid() )
		{
			// Find closest Pos that's navigatable
			if ( PowerQuest.Get.Pathfinder.IsPointInArea(pos) == false )
				pos = PowerQuest.Get.Pathfinder.GetClosestPointToArea(pos);
						
			m_targetPos = pos;
			m_targetEndPos = m_targetPos;

			Vector2[] path = PowerQuest.Get.Pathfinder.FindPath(transform.position, pos);

			if ( path != null && path.Length > 1 )
			{
				m_path = path;
				m_pathPointNext = 1;
				WalkToInternal( m_path[1] );
			}
			else if ( path == null || path.Length == 0 )
			{
				Debug.LogWarning("Couldn't Find Path");
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
		if ( m_state != Character.eState.Animate && m_state != Character.eState.Talk )
			SetState(Character.eState.Talk);
	}
	public void EndSay()
	{
		
		m_currLineId = -1;
		if ( m_state == Character.eState.Talk )
			SetState(Character.eState.Idle);
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

		return (Vector2)m_sprite.transform.position + new Vector2(0,maxHeight) + PowerQuest.Get.GetDialogTextOffset();
	}

	public void OnSkipCutscene()
	{
		if ( m_state == Character.eState.Walk &&  m_targetPos != m_data.GetPosition() && m_targetPos != Vector2.zero )
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
		Vector2 toPos = pos - (Vector2)(transform.position);
		if ( toPos.sqrMagnitude < Mathf.Epsilon )
			return;		
		m_targetPos = pos;

		if ( m_state != Character.eState.Walk )
		{
			SetState(Character.eState.Walk);
		}

		m_data.FaceDirection( toPos.normalized );

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

	// Returns true if the target direction has a different animation or flip than the current direction. Used to work out if need to "turn to face"
	bool CheckTargetDirectionWillChangeAnim()
	{
		bool targetAnimationFlipped;
		string targetAnimationName = FindDirectionalAnimationName( m_data.GetTargetFaceDirection(), m_currAnimBaseName, out targetAnimationFlipped );
		return ( targetAnimationFlipped != Flipped() || targetAnimationName != m_spriteAnimator.ClipName );
	}

	void SetState(Character.eState state)
	{	
		Character.eState oldState = m_state;
		OnExitState(oldState, state);
		m_state = state;
		OnEnterState(oldState, state);
	}

	void OnEnterState( Character.eState oldState, Character.eState newState )
	{
		switch (newState) 
		{
			case Character.eState.Idle:
			{
				if ( m_playIdleDelayFrames <= 0 )
				{
					// Play idle anim
					//if ( string.IsNullOrEmpty(m_transitionAnim) || m_transitionAnim.StartsWith(m_data.AnimIdle) == false ) // Don't play again if alteady transitioning to it
					PlayAnimInternal(m_data.AnimIdle);
				}

			} break;
			case Character.eState.Walk:
			{
				// Play walk anim
				PlayAnimInternal(m_data.AnimWalk);
			} break;
			case Character.eState.Talk:
			{
				// Play talk anim
				//if ( string.IsNullOrEmpty(m_transitionAnim) || m_transitionAnim.StartsWith(m_data.AnimTalk) == false ) // Don't play again if alteady transitioning to it
				if ( string.IsNullOrEmpty(m_data.AnimTalk) == false )
					PlayAnimInternal(m_data.AnimTalk);

			    if ( m_data.LipSyncEnabled && m_mouthNode == null )
			    {
			        m_spriteAnimator.Pause();
			    }
			} break;
			case Character.eState.Animate:
			{
				m_playIdleDelayFrames = 2;
			} break;
			default: break;

		}
	}

	void OnExitState( Character.eState oldState, Character.eState newState )
	{
		switch (oldState) 
		{
			case Character.eState.Idle:
			{				
			} break;
			case Character.eState.Walk:
			{
			} break;
			case Character.eState.Talk:
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

		if ( string.IsNullOrEmpty(m_transitionAnim) == false && m_transitionAnim.StartsWith(animName, System.StringComparison.OrdinalIgnoreCase) )
			return; // Already transitioning to this animation, so don't try and play it again

		if ( animName != m_currTurnAnim && m_currTurnAnim != null )
		{
			// Check if should cancel turn anim cause another anim is playing
			m_currTurnAnim = null;
			m_playAfterTurnAnim = null;
		}
		
		bool flip;
		AnimationClip clip = FindDirectionalAnimation( animName, out flip );
		string oldClipName = m_spriteAnimator.ClipName;		

		bool ignoreTransitionLoopTime = false;

		// Experimental "Transition" code. Not really working for flipping and stuff
		if ( string.IsNullOrEmpty(m_transitionAnim) == false && m_transitionAnim.StartsWith(animName, System.StringComparison.OrdinalIgnoreCase) == false && oldClipName != clip.name )
		{			
			if ( m_animChangeTime < 0.05f )
			{
				// Fix for when we change anims a few times really quick. (like changing pose, then start/stopping talking)
				ignoreTransitionLoopTime = true;	// This flag stops anim with loop tags from restarting
				oldClipName = m_transitioningFromAnim;	// hack the 'oldClipName'
				//Debug.LogFormat("Ignored transition to {0} (with {1}), transAnim: {2}", m_transitioningToAnim, animName, m_transitionAnim);
			}
			else 
			{			
				// Cancel the current transition
				// Changing to another anim that's not the transition target, so cancel the transition. NB: This probably breaks things for walking, where this funtion is played more often
				// TODO: this isn't handling flipping- Should probably remove the "flipping" stuff if wanna keep this code
				//Debug.LogFormat("Cleared transition to {0} (with {1}), transAnim: {2}", m_transitioningToAnim, animName, m_transitionAnim);
				m_transitioningFromAnim = null;
				m_transitioningToAnim = null;
				m_transitionAnim = null;
			}
		}
		

		// Experimental "Transition" code.
		if ( clip != null && oldClipName != clip.name ) // ignore transitions to same animation
		{
			string transitionName = GetTransitionAnim(oldClipName, m_flippedLastUpdate, clip.name, flip);
			if ( string.IsNullOrEmpty(transitionName) == false )
			{
				//Debug.LogFormat("{0} to {1} ({2}): found", oldClipName, clip.name, flip != m_flippedLastUpdate);
				// Found transition				
				m_transitioningFromAnim = oldClipName;
				m_transitioningToAnim = animName;
				animName = transitionName;
				clip = FindDirectionalAnimation( animName, out flip );
				m_transitionAnim = clip.name; 
			}
			else if ( m_loopStartTime > 0 )
			{
				// Transition out from looping anim
				if ( ignoreTransitionLoopTime == false && m_loopEndTime > m_loopStartTime )
					m_spriteAnimator.NormalizedTime = m_loopEndTime;
				
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
				m_loopStartTime = -1;
				m_loopEndTime = -1;
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
		if (  flip != Flipped() )
			transform.localScale = new Vector3(-transform.localScale.x,transform.localScale.y,transform.localScale.z);

		if ( clip == null && Debug.isDebugBuild && animName != "Idle" && animName != "Talk"  && animName != "Walk" && string.IsNullOrEmpty(animName) == false )
			Debug.Log("Failed to find animation: "+animName);
		
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
		m_loopStartTime = m_spriteAnimator.NormalizedTime;
	}

	void AnimLoopEnd()
	{
		if ( m_spriteAnimator.ClipName == m_transitionAnim )
			return; // don't loop when transitioning
		m_loopEndTime = m_spriteAnimator.NormalizedTime;
		m_spriteAnimator.NormalizedTime = m_loopStartTime;		
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
	// Overrides walk speed temporarily (until end of anim, or WalkSpeedReset tag is hit)
	void AnimWalkSpeed(float speed) { m_walkSpeedOverride = new Vector2(speed,speed); }
	void AnimWalkSpeedX(float speed) { m_walkSpeedOverride.x = speed; }
	void AnimWalkSpeedY(float speed) { m_walkSpeedOverride.y = speed; }		
	void AnimWalkSpeedReset() { m_walkSpeedOverride = -Vector2.one; }
	
	// spawn game object at character's node
	void AnimSpawn(GameObject obj)
	{
		if ( obj != null )
			GameObject.Instantiate( obj, transform.position, Quaternion.identity );
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
			m_spriteAnimator.Play( m_spriteAnimator.GetCurrentAnimation() );
		}

		// Check if playing talk anim, or showing dialog & have a mouth frame
		// the reason we don't check for useMouth, is so we can support lipsync without seperate mouth. 
		if ( GetInTalkState() || (useMouth && m_data.Talking)) 
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