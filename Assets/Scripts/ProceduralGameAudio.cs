using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSound
{
    UiClick,
    CablePickup,
    CableConnected,
    WrongAction,
    KeyPress,
    RepairHit,
    EmailSwipe,
    PartInstalled,
    ModemPlug,
    PopupAction,
    VirusDestroyed,
    WifiConnected,
    BossWarning,
    TaskCompleted,
    ServerCool,
    ServerFan,
    SecurityScan,
    SecurityDecision
}

[DisallowMultipleComponent]
public sealed class ProceduralGameAudio : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SoundVolumeKey = "Audio.SoundVolume";
    private const string MutedKey = "Audio.Muted";
    private const float DefaultMusicFadeDuration = 0.7f;
    private const float FloorsMusicFadeDuration = 1.6f;
    private const float EntranceMusicGain = 0.42f;
    private const float OfficeMusicGain = 0.58f;
    private const float FloorsMusicGain = 0.27f;

    private static readonly int[] ChordRoots =
    {
        48, 45, 41, 43, 48, 45, 41, 43
    };

    private static readonly int[] ChordIntervals =
    {
        0, 4, 7, 11,
        0, 3, 7, 10,
        0, 4, 7, 11,
        0, 4, 7, 9
    };

    private static readonly int[] MelodySteps =
    {
        7, 11, 12, 7, 4, 7, 11, 14,
        7, 10, 12, 15, 12, 10, 7, 3,
        7, 11, 14, 11, 7, 4, 7, 9,
        7, 9, 12, 16, 14, 12, 9, 7
    };

    private static readonly int[] SuccessNotes = { 72, 76, 79, 84 };

    private static ProceduralGameAudio instance;

    private readonly AudioClip[] soundClips =
        new AudioClip[Enum.GetValues(typeof(GameSound)).Length];

    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private AudioSource[] soundSources;
    private AudioClip forestMusic;
    private AudioClip happyMusic;
    private AudioClip officeMusic;
    private GamePresentationLibrary presentationLibrary;
    private AudioClip officeAmbience;
    private Coroutine musicTransition;
    private int nextSoundSource;
    private float musicVolume;
    private float soundVolume;
    private float currentSceneGain;
    private bool isMuted;
    private string lastSelectedSceneName;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void Play(GameSound sound, float pitchVariation = 0f)
    {
        DeviceHaptics.PlayForSound(sound);
        ProceduralGameAudio audio = EnsureInstance();
        audio.PlaySound(sound, pitchVariation);
    }

    public static bool IsMuted => EnsureInstance().isMuted;

    public static void SetMuted(bool muted)
    {
        ProceduralGameAudio audio = EnsureInstance();
        audio.isMuted = muted;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        audio.ApplyMutedState();
    }

    public static void SetMusicVolume(float volume)
    {
        ProceduralGameAudio audio = EnsureInstance();
        audio.musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, audio.musicVolume);

        if (audio.musicSource != null &&
            audio.musicSource.isPlaying &&
            audio.musicTransition == null)
        {
            audio.musicSource.volume =
                audio.musicVolume * audio.currentSceneGain;
        }
    }

    public static void SetSoundVolume(float volume)
    {
        ProceduralGameAudio audio = EnsureInstance();
        audio.soundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundVolumeKey, audio.soundVolume);
        audio.RefreshAmbienceVolume(
            SceneManager.GetActiveScene().name);
    }

    private static ProceduralGameAudio EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject audioObject = new("Procedural Game Audio");
        instance = audioObject.AddComponent<ProceduralGameAudio>();
        DontDestroyOnLoad(audioObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        soundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 0.72f);
        isMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;

        presentationLibrary =
            Resources.Load<GamePresentationLibrary>(
                "GamePresentationLibrary");
        if (presentationLibrary != null)
        {
            forestMusic = presentationLibrary.ForestMusic;
            happyMusic = presentationLibrary.HappyMusic;
        }
        else
        {
            Debug.LogWarning(
                "Game presentation library is missing. Imported music will not play.",
                this);
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(musicSource);
        musicSource.loop = true;
        musicSource.volume = 0f;

        ambienceSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(ambienceSource);
        ambienceSource.loop = true;
        ambienceSource.volume = 0f;

        soundSources = new AudioSource[6];
        for (int index = 0; index < soundSources.Length; index++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            ConfigureSource(source);
            soundSources[index] = source;
        }

        ApplyMutedState();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.Equals(
                lastSelectedSceneName,
                activeSceneName,
                StringComparison.Ordinal))
        {
            SelectMusicForScene(activeSceneName);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SelectMusicForScene(scene.name);
    }

    private void SelectMusicForScene(string sceneName)
    {
        lastSelectedSceneName = sceneName;
        AudioClip requestedMusic = null;
        float requestedGain = 0f;
        float fadeDuration = DefaultMusicFadeDuration;

        if (string.Equals(
                sceneName,
                "Giris_Ekran",
                StringComparison.Ordinal))
        {
            requestedMusic = forestMusic;
            requestedGain = EntranceMusicGain;
        }
        else if (string.Equals(
                     sceneName,
                     "YeniOfis",
                     StringComparison.Ordinal))
        {
            requestedMusic = happyMusic != null
                ? happyMusic
                : GetOfficeMusic();
            requestedGain = OfficeMusicGain;
        }
        else if (string.Equals(
                     sceneName,
                     "katlar",
                     StringComparison.Ordinal))
        {
            requestedMusic = happyMusic != null
                ? happyMusic
                : GetOfficeMusic();
            requestedGain = FloorsMusicGain;
            fadeDuration = FloorsMusicFadeDuration;
        }
        else if (presentationLibrary != null &&
                 presentationLibrary.TryGetMiniGameMusic(
                     sceneName,
                     out AudioClip miniGameMusic,
                     out float miniGameGain))
        {
            requestedMusic = miniGameMusic;
            requestedGain = miniGameGain;
        }

        currentSceneGain = requestedGain;
        RefreshAmbienceVolume(sceneName);

        if (musicTransition != null)
            StopCoroutine(musicTransition);

        float targetVolume = musicVolume * requestedGain;
        if (musicSource.clip == requestedMusic &&
            requestedMusic != null &&
            musicSource.isPlaying)
        {
            musicTransition = StartCoroutine(
                FadePlayingMusic(targetVolume, fadeDuration));
            return;
        }

        if (musicSource.clip == null && requestedMusic == null)
            return;

        musicTransition = StartCoroutine(
            TransitionMusic(
                requestedMusic,
                targetVolume,
                fadeDuration));
    }

    private IEnumerator TransitionMusic(
        AudioClip nextClip,
        float targetVolume,
        float fadeDuration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        float halfDuration = Mathf.Max(0.05f, fadeDuration * 0.5f);

        while (musicSource.isPlaying &&
               elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = nextClip;

        if (nextClip == null)
        {
            musicSource.volume = 0f;
            musicTransition = null;
            yield break;
        }

        musicSource.volume = 0f;
        musicSource.Play();
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(
                0f,
                targetVolume,
                Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicTransition = null;
    }

    private IEnumerator FadePlayingMusic(
        float targetVolume,
        float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicTransition = null;
    }

    private void PlaySound(GameSound sound, float pitchVariation)
    {
        int soundIndex = (int)sound;
        if (soundIndex < 0 || soundIndex >= soundClips.Length)
            return;

        AudioClip clip = soundClips[soundIndex];
        if (clip == null)
        {
            clip = CreateSound(sound);
            soundClips[soundIndex] = clip;
        }

        AudioSource source = soundSources[nextSoundSource];
        nextSoundSource = (nextSoundSource + 1) % soundSources.Length;
        source.Stop();
        source.clip = clip;
        // UI navigation is frequent and should sit below gameplay feedback.
        // Keeping this gain local to UiClick preserves the user's global SFX
        // preference and the impact of success/error sounds.
        float categoryGain = sound == GameSound.UiClick ? 0.38f : 1f;
        source.volume = soundVolume * categoryGain;
        source.pitch = 1f + UnityEngine.Random.Range(
            -Mathf.Abs(pitchVariation),
            Mathf.Abs(pitchVariation));
        source.Play();
    }

    private void ApplyMutedState()
    {
        if (musicSource != null)
            musicSource.mute = isMuted;

        if (ambienceSource != null)
            ambienceSource.mute = isMuted;

        if (soundSources == null)
            return;

        foreach (AudioSource source in soundSources)
        {
            if (source != null)
                source.mute = isMuted;
        }
    }

    private void RefreshAmbienceVolume(string sceneName)
    {
        if (ambienceSource == null)
            return;

        float sceneGain = string.Equals(
            sceneName,
            "YeniOfis",
            StringComparison.Ordinal)
                ? 0.1f
                : string.Equals(
                    sceneName,
                    "katlar",
                    StringComparison.Ordinal)
                    ? 0.035f
                    : 0f;

        if (sceneGain <= 0f)
        {
            ambienceSource.Stop();
            ambienceSource.volume = 0f;
            return;
        }

        if (officeAmbience == null)
            officeAmbience = CreateOfficeAmbience();

        if (ambienceSource.clip != officeAmbience)
            ambienceSource.clip = officeAmbience;

        ambienceSource.volume = soundVolume * sceneGain;
        if (!ambienceSource.isPlaying)
            ambienceSource.Play();
    }

    private static AudioClip CreateOfficeAmbience()
    {
        const float duration = 8f;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        float filteredNoise = 0f;

        for (int sampleIndex = 0;
             sampleIndex < sampleCount;
             sampleIndex++)
        {
            float time = sampleIndex / (float)SampleRate;
            float rawNoise = Noise(sampleIndex * 13 + 97);
            filteredNoise = Mathf.Lerp(
                filteredNoise,
                rawNoise,
                0.0045f);

            float electricalHum =
                Mathf.Sin(2f * Mathf.PI * 50f * time) * 0.018f +
                Mathf.Sin(2f * Mathf.PI * 100f * time) * 0.006f;
            float ventilation = filteredNoise * 0.075f;

            float officeTick = Mathf.Repeat(time + 0.35f, 2.7f);
            float keyboardPulse = officeTick < 0.035f
                ? Noise(sampleIndex * 7 + 31) *
                  Mathf.Exp(-officeTick * 95f) * 0.045f
                : 0f;

            samples[sampleIndex] = SoftLimit(
                electricalHum + ventilation + keyboardPulse);
        }

        AudioClip clip = AudioClip.Create(
            "Generated Office Ambience",
            sampleCount,
            1,
            SampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GetOfficeMusic()
    {
        if (officeMusic != null)
            return officeMusic;

        const float tempo = 96f;
        const int barCount = 8;
        float duration = barCount * 4f * 60f / tempo;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int sampleIndex = 0;
             sampleIndex < sampleCount;
             sampleIndex++)
        {
            float time = sampleIndex / (float)SampleRate;
            float beat = time * tempo / 60f;
            int bar = Mathf.FloorToInt(beat / 4f) % barCount;
            float beatInBar = Mathf.Repeat(beat, 4f);
            int chordType = bar % 4;
            int root = ChordRoots[bar];

            float padEnvelope = SmoothGate(beatInBar, 0.18f) *
                SmoothGate(4f - beatInBar, 0.18f);
            float pad = 0f;
            for (int noteIndex = 0; noteIndex < 4; noteIndex++)
            {
                int interval = ChordIntervals[
                    chordType * 4 + noteIndex];
                float frequency = MidiToFrequency(root + interval + 12);
                pad += WarmOscillator(frequency, time) * 0.25f;
            }

            float halfBeat = beat * 2f;
            int arpStep = Mathf.FloorToInt(halfBeat) % 4;
            float arpPhase = Mathf.Repeat(halfBeat, 1f);
            int arpInterval = ChordIntervals[chordType * 4 + arpStep];
            float arpFrequency = MidiToFrequency(root + arpInterval + 24);
            float arp = Triangle(arpFrequency, time) *
                Mathf.Exp(-4.8f * arpPhase);

            int melodyIndex = Mathf.FloorToInt(halfBeat) %
                MelodySteps.Length;
            float melodyFrequency = MidiToFrequency(
                root + MelodySteps[melodyIndex] + 24);
            float melody = Mathf.Sin(
                2f * Mathf.PI * melodyFrequency * time) *
                Mathf.Exp(-6.2f * arpPhase);

            float beatPhase = Mathf.Repeat(beat, 1f);
            float bassFrequency = MidiToFrequency(root - 12);
            float bass = WarmOscillator(bassFrequency, time) *
                Mathf.Exp(-3.5f * beatPhase);

            int beatNumber = Mathf.FloorToInt(beat) % 4;
            float kick = 0f;
            if (beatNumber == 0 || beatNumber == 2)
            {
                float kickTime = beatPhase * 60f / tempo;
                kick = Mathf.Sin(
                    2f * Mathf.PI *
                    (72f - 28f * kickTime) * kickTime) *
                    Mathf.Exp(-12f * kickTime);
            }

            float snare = 0f;
            if (beatNumber == 1 || beatNumber == 3)
            {
                snare = Noise(sampleIndex) *
                    Mathf.Exp(-9f * beatPhase);
            }

            float hatPhase = Mathf.Repeat(halfBeat, 1f);
            float hat = Noise(sampleIndex * 3 + 17) *
                Mathf.Exp(-18f * hatPhase);

            float mix =
                pad * padEnvelope * 0.23f +
                arp * 0.11f +
                melody * 0.055f +
                bass * 0.13f +
                kick * 0.12f +
                snare * 0.035f +
                hat * 0.018f;

            float edgeFade = Mathf.Min(
                Mathf.Clamp01(time / 0.025f),
                Mathf.Clamp01((duration - time) / 0.025f));
            samples[sampleIndex] = SoftLimit(mix) * edgeFade;
        }

        officeMusic = AudioClip.Create(
            "Original - Office After Hours",
            sampleCount,
            1,
            SampleRate,
            false);
        officeMusic.SetData(samples, 0);
        return officeMusic;
    }

    private static AudioClip CreateSound(GameSound sound)
    {
        float duration = sound switch
        {
            GameSound.UiClick => 0.065f,
            GameSound.CablePickup => 0.12f,
            GameSound.CableConnected => 0.28f,
            GameSound.WrongAction => 0.24f,
            GameSound.KeyPress => 0.07f,
            GameSound.RepairHit => 0.18f,
            GameSound.EmailSwipe => 0.22f,
            GameSound.PartInstalled => 0.24f,
            GameSound.ModemPlug => 0.32f,
            GameSound.PopupAction => 0.18f,
            GameSound.VirusDestroyed => 0.26f,
            GameSound.WifiConnected => 0.38f,
            GameSound.BossWarning => 0.42f,
            GameSound.TaskCompleted => 0.72f,
            GameSound.ServerCool => 0.44f,
            GameSound.ServerFan => 0.34f,
            GameSound.SecurityScan => 0.48f,
            GameSound.SecurityDecision => 0.32f,
            _ => 0.1f
        };

        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int sampleIndex = 0;
             sampleIndex < sampleCount;
             sampleIndex++)
        {
            float time = sampleIndex / (float)SampleRate;
            float progress = Mathf.Clamp01(time / duration);
            float sample = sound switch
            {
                GameSound.UiClick =>
                    (SineSweep(time, 430f, 560f, progress) * 0.72f +
                     Triangle(310f, time) * 0.16f) *
                    Mathf.Exp(-42f * time),
                GameSound.CablePickup =>
                    Triangle(420f + progress * 180f, time) *
                    Mathf.Exp(-22f * time),
                GameSound.CableConnected =>
                    SineSweep(time, 460f, 1180f, progress) *
                    Mathf.Exp(-7f * time) +
                    Noise(sampleIndex) * Mathf.Exp(-28f * time) * 0.15f,
                GameSound.WrongAction =>
                    (Triangle(190f - progress * 55f, time) +
                     Triangle(205f - progress * 50f, time) * 0.5f) *
                    (1f - progress),
                GameSound.KeyPress =>
                    (Triangle(760f, time) * 0.65f +
                     Noise(sampleIndex) * 0.25f) *
                    Mathf.Exp(-48f * time),
                GameSound.RepairHit =>
                    (SineSweep(time, 105f, 48f, progress) * 0.8f +
                     Noise(sampleIndex) * 0.35f) *
                    Mathf.Exp(-19f * time),
                GameSound.EmailSwipe =>
                    (SineSweep(time, 540f, 1280f, progress) * 0.42f +
                     Noise(sampleIndex * 3 + 11) * 0.28f) *
                    Mathf.Exp(-9f * time),
                GameSound.PartInstalled =>
                    (SineSweep(time, 185f, 72f, progress) * 0.72f +
                     Triangle(680f, time) * 0.34f +
                     Noise(sampleIndex + 23) * 0.16f) *
                    Mathf.Exp(-14f * time),
                GameSound.ModemPlug =>
                    (SineSweep(time, 310f, 1040f, progress) * 0.68f +
                     Triangle(1220f, time) * 0.2f +
                     Noise(sampleIndex * 5 + 7) * 0.12f) *
                    Mathf.Exp(-6.8f * time),
                GameSound.PopupAction =>
                    (Triangle(980f - progress * 390f, time) * 0.66f +
                     SineSweep(time, 1380f, 720f, progress) * 0.32f) *
                    Mathf.Exp(-18f * time),
                GameSound.VirusDestroyed =>
                    (SineSweep(time, 250f, 68f, progress) * 0.72f +
                     Noise(sampleIndex * 7 + 31) * 0.48f) *
                    Mathf.Exp(-11f * time),
                GameSound.WifiConnected =>
                    (SineSweep(time, 520f, 1460f, progress) * 0.7f +
                     Triangle(1760f, time) * 0.22f) *
                    Mathf.Exp(-5.6f * time),
                GameSound.BossWarning =>
                    (Triangle(168f - progress * 34f, time) * 0.72f +
                     SineSweep(time, 360f, 210f, progress) * 0.38f) *
                    (0.62f + 0.38f *
                     Mathf.Sin(progress * Mathf.PI * 5f)) *
                    (1f - progress * 0.35f),
                GameSound.TaskCompleted =>
                    CreateSuccessTone(time, progress),
GameSound.ServerCool =>
                    (SineSweep(time, 920f, 250f, progress) * 0.44f +
                     Noise(sampleIndex * 5 + 29) * 0.24f) *
                    Mathf.Exp(-5.5f * time),
                GameSound.ServerFan =>
                    (SineSweep(time, 115f, 220f, progress) * 0.58f +
                     Triangle(440f, time) * 0.17f) *
                    Mathf.Exp(-7f * time),
                GameSound.SecurityScan =>
                    (SineSweep(time, 470f, 1480f, progress) * 0.62f +
                     Triangle(1820f, time) * 0.18f) *
                    (0.72f + Mathf.Sin(progress * Mathf.PI * 8f) * 0.18f) *
                    Mathf.Exp(-3.8f * time),
                GameSound.SecurityDecision =>
                    (Triangle(progress < 0.48f ? 620f : 880f, time) * 0.66f +
                     SineSweep(time, 440f, 970f, progress) * 0.25f) *
                    Mathf.Exp(-8.5f * time),
                _ => 0f
            };

            samples[sampleIndex] = SoftLimit(sample * 0.62f) *
                Mathf.Clamp01((duration - time) / 0.012f);
        }

        AudioClip clip = AudioClip.Create(
            $"Generated {sound}",
            sampleCount,
            1,
            SampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float CreateSuccessTone(float time, float progress)
    {
        int noteIndex = Mathf.Min(3, Mathf.FloorToInt(progress * 4f));
        float noteProgress = Mathf.Repeat(progress * 4f, 1f);
        float frequency = MidiToFrequency(SuccessNotes[noteIndex]);
        return (Mathf.Sin(2f * Mathf.PI * frequency * time) +
                Triangle(frequency * 2f, time) * 0.22f) *
               Mathf.Exp(-3.4f * noteProgress);
    }

    private static void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.bypassReverbZones = true;
    }

    private static float MidiToFrequency(int midiNote)
    {
        return 440f * Mathf.Pow(2f, (midiNote - 69f) / 12f);
    }

    private static float WarmOscillator(float frequency, float time)
    {
        float phase = 2f * Mathf.PI * frequency * time;
        return Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.18f;
    }

    private static float Triangle(float frequency, float time)
    {
        return Mathf.Asin(
            Mathf.Sin(2f * Mathf.PI * frequency * time)) *
            (2f / Mathf.PI);
    }

    private static float SineSweep(
        float time,
        float startFrequency,
        float endFrequency,
        float progress)
    {
        float frequency = Mathf.Lerp(
            startFrequency,
            endFrequency,
            progress);
        return Mathf.Sin(2f * Mathf.PI * frequency * time);
    }

    private static float SmoothGate(float value, float width)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value / width));
    }

    private static float SoftLimit(float value)
    {
        return (float)Math.Tanh(value * 1.4f) * 0.82f;
    }

    private static float Noise(int seed)
    {
        uint value = (uint)seed;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return (value & 0xffff) / 32767.5f - 1f;
    }
}
