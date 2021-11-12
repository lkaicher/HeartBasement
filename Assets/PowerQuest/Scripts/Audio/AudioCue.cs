using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PowerTools.Quest
{

public class AudioCue : MonoBehaviour 
{
	/*public enum eShake 
	{
		None,
		Small,
		Medium,
		Large
	}*/

	public enum eAudioType
	{
		Sound = 1<<0,
		Music = 1<<1,
		Dialog = 1<<2,
		User1 = 1<<3,
		User2 = 1<<4,
		User3 = 1<<5,
		User4 = 1<<6,
		User5 = 1<<7,
	};
	
	[System.Serializable]
	public class Clip
	{
		[HideInInspector]public AudioClip m_sound = null;
		[HideInInspector]public float m_weight = 100;
		[HideInInspector]public bool m_loop = false; // Legacy: No longer used
		[Tooltip("Volume. Multiplier on the base cue volume")]
		public MinMaxRange m_volume = new MinMaxRange(1.0f);
		[Tooltip("Volume. Multiplier on the base cue pitch")]
		public MinMaxRange m_pitch = new MinMaxRange(1.0f);		
		[Tooltip("At what point in the clip should play end (crops the sound)")]
		public float m_startTime = 0.0f;
		[Tooltip("At what point in the clip should play end (crops the sound)")]
		public float m_endTime = 0.0f;
	}

	[Header("Type of Sound")]
	[Tooltip("Type of sound. Used to allow adjusting volume of different types of sounds")]
	[BitMask(typeof(eAudioType))]
	public int m_type = (int)eAudioType.Sound;
	[Tooltip("Whether it's a looping sound")]
	public bool m_loop = false;
	[Header("Basic settings")]
	[Tooltip("Volume. Usually Randomly chosen within the specified range. But if Camera Size Range is set then it's the min/max for volume as it goes out of camera range")]
	public MinMaxRange m_volume =new MinMaxRange(1.0f);
	[Tooltip("Pitch. Randomly chosen within the specified range")]
	public MinMaxRange m_pitch =new MinMaxRange(1.0f);
	[Tooltip("Stereo Pan. 0 is center, -1  is left, 1 is right. Randomly chosen within the specified range")]
	public MinMaxRange m_pan =new MinMaxRange(0.0f);
	//public int m_priority = 128;
	[Header("Playback Settings")]
	[Tooltip("Trigger another cue when this is played")]
	public AudioCue m_alsoPlay = null;	
	[Tooltip("The random chance that this sound will play at all")]
	[Range(0,1)]
	public float m_chance = 1;	
	[Tooltip("Delay before playing sound, after cue is played")]
	public MinMaxRange m_startDelay = new MinMaxRange(0.0f);
	[Tooltip("If >= 0 this overrides the default delay before the same sound can be repeated")] 
	public float m_noDuplicateTime = -1;
	[Header("Mixer Group")]
	[Tooltip("For more advanced enviroment effects, play using a mixer group")]
	public UnityEngine.Audio.AudioMixerGroup m_mixerGroup = null;
	[Header("Effects")]
	[Tooltip("Reverb setting")]
	public AudioReverbPreset m_reverbPreset = AudioReverbPreset.Off;
	[Range(0,0.9f)]
	[Tooltip("Distortion amount")]
	public float m_distortionLevel = 0;
	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioEchoFilter m_echoFilter = null;
	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioLowPassFilter m_lowPassFilter = null;
	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioHighPassFilter m_highPassFilter = null;
	[Tooltip("Drag an audio filter on to here. (Add an AudioSource and filter component to this cue and drag it here)")]
	public AudioChorusFilter m_chorusFilter = null;

	//public float m_shakeTime = 0.0f;
	//public eShake m_shakeType = eShake.None;

	public List<Clip> m_sounds = new List<Clip>(1);



	WeightedShuffledIndex m_shuffledIndex = null;

	public void Play()
	{
		SystemAudio.Play(this);
	}
	public void Play( Transform emmitter )
	{
		SystemAudio.Play(this, emmitter);
	}
	
	public Clip GetClip()
	{
		if ( m_sounds.Count == 0 )
			return null;
		ValidateShuffledList();
		return m_sounds[m_shuffledIndex];
	}

	public int GetClipIndex(AudioClip clip)
	{
		return m_sounds.FindIndex(item=>item.m_sound == clip);
	}

	public AudioClip GetClip(int index)
	{
		return m_sounds.IsIndexValid(index) ? m_sounds[index].m_sound : null;
	}

	public Clip GetClipData(int index)
	{
		return m_sounds.IsIndexValid(index) ? m_sounds[index] : null;
	}

	public Clip GetClipData(AudioClip clip)
	{
		return m_sounds.Find(item=>item.m_sound == clip);
	}

	public int GetClipCount() { return m_sounds.Count; }

	public WeightedShuffledIndex GetShuffledIndex() 
	{ 
		ValidateShuffledList();
		return m_shuffledIndex; 
	}

	void ValidateShuffledList()
	{
		if ( m_shuffledIndex == null || m_shuffledIndex.Count != m_sounds.Count )
		{
			m_shuffledIndex = new WeightedShuffledIndex();
			m_shuffledIndex.SetWeights( m_sounds, (item)=>item.m_weight );		
		}
	}


}

}
