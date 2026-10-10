using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Routes one-shots through the GameAudioMixer groups (SFX / Music).
/// Mixer lives at Resources/GameAudioMixer so callers need no scene wiring.
/// </summary>
public static class GameAudio
{
    const string MixerResourcePath = "GameAudioMixer";

    static AudioMixer _mixer;
    static AudioMixerGroup _sfxGroup;
    static AudioMixerGroup _musicGroup;
    static bool _loadAttempted;

    public static AudioMixerGroup SfxGroup
    {
        get
        {
            EnsureLoaded();
            return _sfxGroup;
        }
    }

    public static AudioMixerGroup MusicGroup
    {
        get
        {
            EnsureLoaded();
            return _musicGroup;
        }
    }

    public static void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        // Temp source so playback survives the caller being destroyed (e.g. Missile explode).
        GameObject go = new GameObject("SFX OneShot");
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.outputAudioMixerGroup = SfxGroup;
        source.Play();

        Object.Destroy(go, clip.length / Mathf.Max(source.pitch, 0.01f) + 0.1f);
    }

    static void EnsureLoaded()
    {
        if (_loadAttempted)
            return;

        _loadAttempted = true;
        _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (_mixer == null)
        {
            Debug.LogWarning(
                "GameAudio: missing Resources/GameAudioMixer. SFX will play without mixer routing.");
            return;
        }

        _sfxGroup = FindGroup("SFX");
        _musicGroup = FindGroup("Music");
    }

    static AudioMixerGroup FindGroup(string name)
    {
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(name);
        if (groups == null || groups.Length == 0)
        {
            Debug.LogWarning($"GameAudio: mixer group '{name}' not found.");
            return null;
        }

        return groups[0];
    }
}
