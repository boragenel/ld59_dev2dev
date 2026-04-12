using UnityEngine;

using System;
using UnityEngine.Audio;
using System.Reflection;
using System.Reflection.Emit;

using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    [Header("MASTER MIXER")]
    public AudioMixer masterMixer;
    public AudioMixerGroup sfxMixer;
    public AudioMixerGroup musicMixer;

    public bool soundOn = true;

    [Header("Camera Stuff")]
    public bool followCamera = true;
    public Transform cameraToFollow = null;

    public Dictionary<string,AudioMixerGroup> mixers;

    public AudioClip[] sounds;
    Dictionary<SoundType, AudioClip> soundMap;

    

    public List<AudioSource> currentlyPlaying = new List<AudioSource>();

    public int numberOfChannels = 32;
    public List<AudioSource> channels;
    private int channelIndex = 0;

    

    // Static singleton property
    public static SoundManager Instance { get; private set; }

    

    public void Awake()
    {
        /// singleton stuff ///////
        // First we check if there are any other instances conflicting
        if (Instance != null && Instance != this)
        {
            // If that is the case, we destroy this instance
            Destroy(gameObject);
            return;
        }

        // Here we save our singleton instance
        Instance = this;

        // Furthermore we make sure that we don't destroy between scenes (this is optional)

        /////////////////////////////

        if(masterMixer == null)
        {
            Debug.LogError("Attach your master mixer!");
        }
        mixers = new Dictionary<string, AudioMixerGroup>();
        channels = new List<AudioSource>();

        populateAudioSources();
        populateSoundMap();
        

    }

    public void populateSoundMap()
    {


        soundMap = new Dictionary<SoundType, AudioClip>();
        for( int i= 0; i< sounds.Length; i++)
        {
            string enumName = sounds[i].name.ToUpperInvariant().Replace(' ', '_'); ;
            SoundType type = (SoundType)Enum.Parse(typeof(SoundType), enumName);
            if (!soundMap.ContainsKey(type))
            {
                soundMap.Add(type, sounds[i]);
            }

        }


        AudioMixerGroup[] mixerGroups = masterMixer.FindMatchingGroups(string.Empty);
        foreach(AudioMixerGroup amg in mixerGroups)
        {
            mixers.Add(amg.name, amg);
        }

    }


    public void populateAudioSources()
    {
        AudioSource tempSource;
        for(int i=0; i< numberOfChannels; i++)
        {
            tempSource = gameObject.AddComponent<AudioSource>();
            tempSource.playOnAwake = false;
            channels.Add(tempSource);
        }

    }

    public void PlayOneShot(SoundType sound,float volume = 1, float pitch=1,string mixerName="SFX")
    {

        if (!soundOn)
            return;

        AudioSource a = GetFirstAvailableChannel();
        if (a == null) {
            Debug.LogError("No available channels left. Try increasing the max channel number.");
            return;
        }

        a.outputAudioMixerGroup = mixers[mixerName];
        if(a.clip != null)
            a.time = 0;
        a.PlayOneShot(soundMap[sound]);
        a.volume = volume;
        a.pitch = pitch;

        //
        //channels.Remove(a);
        //currentlyPlaying.Add(a);
        //

        StartCoroutine(FreeAudioChannel(a,soundMap[sound]));


    }

    IEnumerator FreeAudioChannel(AudioSource a, AudioClip ac)
    {
        
        float acLen = ac.length;
        yield return new WaitForSeconds(ac.length);
        for (int i= 0; i < currentlyPlaying.Count; i++ )
        {

            if( currentlyPlaying[i]  == a )
            {
                currentlyPlaying.Remove(a);
                channels.Add(a);
            }

        }

    }

    public void PlayLooped(SoundType sound, float volume = 1, float pitch = 1, string mixerName = "SFX",float startAt = 0)
    {
        AudioSource a = GetFirstAvailableChannel();
        if (a == null)
        {
            Debug.LogError("No available channels left. Try increasing the max channel number.");
            return;
        }

        a.outputAudioMixerGroup = mixers[mixerName];
        a.clip = soundMap[sound];
        a.Play();
        a.time = startAt;
        a.volume = volume;

        a.pitch = pitch;


    }

    public void StopAllSoundsOfType(SoundType sound)
    {
        string enumName;
        for (int i = 0; i < currentlyPlaying.Count; i++)
        { 
            if (currentlyPlaying[i].clip == null)
                continue;

            enumName = currentlyPlaying[i].clip.name.ToUpperInvariant().Replace(' ', '_');
            if (enumName.CompareTo(sound.ToString()) == 0)
                currentlyPlaying[i].Stop();
        }
    }

    public void PauseAllSoundsOfType(SoundType sound)
    {
        string enumName;
        for (int i = 0; i < currentlyPlaying.Count; i++)
        {
            if (currentlyPlaying[i].clip == null)
                continue;

            enumName = currentlyPlaying[i].clip.name.ToUpperInvariant().Replace(' ', '_');
            if (enumName.CompareTo(sound.ToString()) == 0)
                currentlyPlaying[i].Pause();
        }
    }

    public void ResumeAllSoundsOfType(SoundType sound)
    {
        string enumName;
        for (int i = 0; i < currentlyPlaying.Count; i++)
        {
            if (currentlyPlaying[i].clip == null)
                continue;

            enumName = currentlyPlaying[i].clip.name.ToUpperInvariant().Replace(' ', '_');
            if (enumName.CompareTo(sound.ToString()) == 0)
                currentlyPlaying[i].UnPause();
        }
    }

    public AudioSource GetFirstAvailableChannel()
    {

        if (channels.Count > 0)
        {
            AudioSource a = channels[channels.Count - 1];
            if (a != null)
            { 
                channels.Remove(a);
                currentlyPlaying.Add(a);

            }
            return a;
        }
        else return null;
    }

    public void Update()
    {
     

        ClearInactiveChannels();

    }

    public void ClearInactiveChannels()
    {
        AudioSource a;
        for(int i=0; i< currentlyPlaying.Count; i++)
        {
            if(currentlyPlaying[i] != null && !currentlyPlaying[i].isPlaying)
            {
                a = currentlyPlaying[i];
                currentlyPlaying.Remove(a);
                i--;
                channels.Add(a);
            }
        }
    }

    /////////
    /////////

    
}