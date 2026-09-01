using System;
using UnityEngine;

public sealed class GamePresentationLibrary : ScriptableObject
{
    [Serializable]
    public sealed class SceneMusicEntry
    {
        [SerializeField] private string sceneName;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float gain = 0.16f;

        public string SceneName => sceneName;
        public float Gain => gain;

        public SceneMusicEntry()
        {
        }

        public SceneMusicEntry(
            string configuredSceneName,
            AudioClip[] configuredClips,
            float configuredGain)
        {
            sceneName = configuredSceneName;
            clips = configuredClips;
            gain = Mathf.Clamp01(configuredGain);
        }

        public bool TrySelectClip(out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
                return false;

            int availableClipCount = 0;
            foreach (AudioClip candidate in clips)
            {
                if (candidate != null)
                    availableClipCount++;
            }

            if (availableClipCount == 0)
                return false;

            int selectedIndex = UnityEngine.Random.Range(
                0,
                availableClipCount);
            foreach (AudioClip candidate in clips)
            {
                if (candidate == null)
                    continue;

                if (selectedIndex == 0)
                {
                    clip = candidate;
                    return true;
                }

                selectedIndex--;
            }

            return false;
        }

#if UNITY_EDITOR
        public bool Matches(SceneMusicEntry other)
        {
            if (other == null ||
                !string.Equals(
                    sceneName,
                    other.sceneName,
                    StringComparison.Ordinal) ||
                !Mathf.Approximately(gain, other.gain))
            {
                return false;
            }

            int clipCount = clips == null ? 0 : clips.Length;
            int otherClipCount = other.clips == null
                ? 0
                : other.clips.Length;
            if (clipCount != otherClipCount)
                return false;

            for (int index = 0; index < clipCount; index++)
            {
                if (clips[index] != other.clips[index])
                    return false;
            }

            return true;
        }
#endif
    }

    [SerializeField] private AudioClip forestMusic;
    [SerializeField] private AudioClip happyMusic;
    [SerializeField] private SceneMusicEntry[] miniGameMusic;
    [SerializeField] private Font displayFont;
    [SerializeField] private Sprite resumeIcon;

    public AudioClip ForestMusic => forestMusic;
    public AudioClip HappyMusic => happyMusic;
    public Font DisplayFont => displayFont;
    public Sprite ResumeIcon => resumeIcon;

    public bool TryGetMiniGameMusic(
        string sceneName,
        out AudioClip clip,
        out float gain)
    {
        clip = null;
        gain = 0f;
        if (string.IsNullOrEmpty(sceneName) || miniGameMusic == null)
            return false;

        foreach (SceneMusicEntry entry in miniGameMusic)
        {
            if (entry == null ||
                !string.Equals(
                    entry.SceneName,
                    sceneName,
                    StringComparison.Ordinal) ||
                !entry.TrySelectClip(out clip))
            {
                continue;
            }

            gain = Mathf.Clamp01(entry.Gain);
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    public bool ConfigureEditor(
        AudioClip configuredForestMusic,
        AudioClip configuredHappyMusic,
        Font configuredDisplayFont,
        Sprite configuredResumeIcon,
        SceneMusicEntry[] configuredMiniGameMusic)
    {
        if (forestMusic == configuredForestMusic &&
            happyMusic == configuredHappyMusic &&
            displayFont == configuredDisplayFont &&
            resumeIcon == configuredResumeIcon &&
            MiniGameMusicMatches(configuredMiniGameMusic))
        {
            return false;
        }

        forestMusic = configuredForestMusic;
        happyMusic = configuredHappyMusic;
        displayFont = configuredDisplayFont;
        resumeIcon = configuredResumeIcon;
        miniGameMusic = configuredMiniGameMusic;
        return true;
    }

    private bool MiniGameMusicMatches(
        SceneMusicEntry[] configuredMiniGameMusic)
    {
        int entryCount = miniGameMusic == null
            ? 0
            : miniGameMusic.Length;
        int configuredEntryCount = configuredMiniGameMusic == null
            ? 0
            : configuredMiniGameMusic.Length;
        if (entryCount != configuredEntryCount)
            return false;

        for (int index = 0; index < entryCount; index++)
        {
            if (miniGameMusic[index] == null)
            {
                if (configuredMiniGameMusic[index] != null)
                    return false;

                continue;
            }

            if (!miniGameMusic[index].Matches(
                    configuredMiniGameMusic[index]))
            {
                return false;
            }
        }

        return true;
    }
#endif
}