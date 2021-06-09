using UnityEngine;
using System.Collections;

namespace PowerTools.Quest
{


[System.Serializable]
public partial class QuestSettings
{	
	/// Whether dialog's displayed, audio only, or both
	public enum eDialogDisplay
	{
		TextAndSpeech,
		SpeechOnly,
		TextOnly
	};


	/// Gets/sets the master volume (0 to 1)
	public float Volume 
	{ 
		get { return m_masterVolume; } 
		set
		{ 
			m_masterVolume = value; 
			AudioListener.volume = value;
		} 
	}

	/// Gets/sets the music volume (0 to 1)
	public float VolumeMusic
	{ 
		get { return m_musicVolume; } 
		set
		{ 
			m_musicVolume = value; 
			SystemAudio.SetVolume(AudioCue.eAudioType.Music, value);
		} 
	}

	/// Gets/sets the sound effect volume (0 to 1)
	public float VolumeSFX
	{ 
		get { return m_sfxVolume; } 
		set
		{ 
			m_sfxVolume = value; 
			SystemAudio.SetVolume(AudioCue.eAudioType.Sound, value);
		} 
	}

	/// Gets/sets the dialog volume (0 to 1)
	public float VolumeDialog
	{ 
		get { return m_dialogVolume; } 
		set
		{ 			
			m_dialogVolume = value;
			SystemAudio.SetVolume(AudioCue.eAudioType.Dialog, value);
		} 
	}

	public string DisplayBoxGui
	{ 
		get { return m_displayBoxGui; }
		set { m_displayBoxGui = value; }
	}
	public string DialogTreeGui
	{ 
		get { return m_dialogTreeGui; }
		set { m_dialogTreeGui = value; }
	}

	/// Gets/sets the dialog display style (0 to 1)
	public eDialogDisplay DialogDisplay
	{ 
		get { return m_dialogDisplay; }
		set { m_dialogDisplay = value; }
	}

	/// Whether "Display" text is shown regardless of the DialogDisplay setting
	public bool AlwaysShowDisplayText 
	{ 
		get { return m_alwaysShowDisplayText; }
		set { m_alwaysShowDisplayText = value; }
	}

	public eSpeechStyle SpeechStyle 
	{ 
		get { return m_speechStyle; }
		set { m_speechStyle = value; }
	}

	public eSpeechPortraitLocation SpeechPortraitLocation
	{ 
		get { return m_speechPortraitLocation; }
		set { m_speechPortraitLocation = value; }
	}

	/// Length of time transition between rooms takes
	public float TransitionFadeTime { get { return m_transitionFadeTime; } set {m_transitionFadeTime = value; } }

	/// Initialises the system (Called from inside PowerQuest)
	public void OnInitialise()
	{
		AudioListener.volume = m_masterVolume;
		SystemAudio.SetVolume(AudioCue.eAudioType.Music, m_musicVolume);
		SystemAudio.SetVolume(AudioCue.eAudioType.Sound, m_sfxVolume);
		SystemAudio.SetVolume(AudioCue.eAudioType.Dialog, m_dialogVolume);

	}

	/// Initialises the system (Called from inside PowerQuest)
	public void OnPostRestore( int version )
	{
		OnInitialise();
	}

	/// Gets/Sets the current language by code
	public string Language
	{
		get { return m_languageCode; }
		set 
		{ 
			// Find the language code
			int languageId = System.Array.FindIndex(SystemText.Get.GetLanguages(), item=> string.Equals( item.m_code, m_languageCode, System.StringComparison.OrdinalIgnoreCase) );
			if ( languageId < 0 )
			{
				Debug.LogWarning("Couldn't find language code: "+value+", The code needs to be added to SystemText");
				return;
			}
			m_languageCode = value; 
			SystemText.Get.SetLanguage(languageId);
		}
	}
	/// Get's the current language
	public LanguageData[] GetLanguages() { return SystemText.Get.GetLanguages(); }

	//
	// Private vars, these are what are saved
	//
	[SerializeField] float m_masterVolume = 1.0f;
	[SerializeField] float m_musicVolume = 1.0f;
	[SerializeField] float m_sfxVolume = 1.0f;
	[SerializeField] float m_dialogVolume = 1.0f;
	[SerializeField] string m_displayBoxGui = "DisplayBox";
	[SerializeField] string m_dialogTreeGui = "DialogTree";
	[SerializeField] eDialogDisplay m_dialogDisplay = eDialogDisplay.TextAndSpeech;
	[Tooltip("How is dialog displayed. Above head (lucasarts style), next to a portrait (sierra style), or as a caption not attached to character position")]
	[SerializeField] eSpeechStyle m_speechStyle = eSpeechStyle.AboveCharacter;
	[Tooltip("Which side is portrait located (currently only LEFT is implemented)")]
	[SerializeField] eSpeechPortraitLocation m_speechPortraitLocation = eSpeechPortraitLocation.Left;

	[SerializeField] float m_transitionFadeTime = 0.3f;
	[Tooltip("If true, display is shown even when subtitles off")]
	[SerializeField] bool m_alwaysShowDisplayText = true;
	[Tooltip("The Default Language. Should match codes set in SystemText")]
	[SerializeField] string m_languageCode = "EN";


}
}