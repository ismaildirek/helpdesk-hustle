$source = @'
using System;
using System.IO;

public static class MusicPreviewGenerator
{
    private const int SampleRate = 22050;
    private static readonly int[] Roots = { 48, 45, 41, 43 };
    private static readonly int[,] Intervals =
    {
        { 0, 4, 7, 11 },
        { 0, 3, 7, 10 },
        { 0, 4, 7, 11 },
        { 0, 4, 7, 9 }
    };
    private static readonly int[] MidnightRoots = { 45, 41, 43, 40 };
    private static readonly int[] MidnightNotes =
        { 0, 12, 7, 15, 10, 3, 19, 7, 12, 22, 15, 10 };
    private static readonly int[] ServerRoots = { 50, 53, 46, 48 };
    private static readonly int[] ServerArp =
        { 0, 12, 7, 15, 19, 15, 10, 7, 3, 10, 14, 19, 22, 17, 12, 7 };
    private static readonly int[] CoffeeRoots = { 48, 45, 50, 43, 46 };
    private static readonly int[] CoffeeNotes =
        { 0, 7, 12, 10, 3, 15, 12, 7, 5, 12, 17, 14 };

    public static void GenerateAll(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        WriteWave(
            Path.Combine(outputDirectory, "01_After_Hours_Office.wav"),
            8.0 * 4.0 * 60.0 / 88.0,
            WarmOffice);
        WriteWave(
            Path.Combine(outputDirectory, "02_Neon_Helpdesk.wav"),
            12.0 * 4.0 * 60.0 / 126.0,
            NeonHelpdesk);
        WriteWave(
            Path.Combine(outputDirectory, "03_Corporate_Glitch.wav"),
            10.0 * 4.0 * 60.0 / 110.0,
            CorporateGlitch);
        WriteWave(
            Path.Combine(outputDirectory, "04_Midnight_Support_Glitch.wav"),
            9.0 * 4.0 * 60.0 / 96.0,
            MidnightSupportGlitch);
        WriteWave(
            Path.Combine(outputDirectory, "05_Server_Rush_Glitch.wav"),
            12.0 * 4.0 * 60.0 / 132.0,
            ServerRushGlitch);
        WriteWave(
            Path.Combine(outputDirectory, "06_Coffee_Break_EXE.wav"),
            10.0 * 4.0 * 60.0 / 104.0,
            CoffeeBreakExe);
    }

    private static void WriteWave(
        string path,
        double duration,
        Func<double, int, double> composer)
    {
        int sampleCount = (int)Math.Ceiling(duration * SampleRate);
        int dataSize = sampleCount * sizeof(short);

        using (FileStream stream = File.Create(path))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);

            for (int index = 0; index < sampleCount; index++)
            {
                double time = index / (double)SampleRate;
                double edge = Math.Min(
                    Clamp01(time / 0.02),
                    Clamp01((duration - time) / 0.02));
                double sample = SoftLimit(composer(time, index)) * edge;
                writer.Write((short)Math.Round(
                    Math.Max(-1.0, Math.Min(1.0, sample)) * 32767.0));
            }
        }
    }

    private static double WarmOffice(double time, int sampleIndex)
    {
        const double tempo = 88.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 8;
        int chord = bar % 4;
        int root = Roots[chord];
        double beatInBar = Repeat(beat, 4.0);
        double padGate = SmoothGate(beatInBar, 0.22) *
                         SmoothGate(4.0 - beatInBar, 0.22);

        double pad = 0.0;
        for (int note = 0; note < 4; note++)
        {
            double frequency = Midi(root + Intervals[chord, note] + 12);
            pad += Warm(frequency, time) * 0.25;
        }

        double halfBeat = beat * 2.0;
        int pluckStep = ((int)Math.Floor(halfBeat)) % 4;
        double pluckPhase = Repeat(halfBeat, 1.0);
        double pluckFrequency = Midi(
            root + Intervals[chord, pluckStep] + 24);
        double pluck = Triangle(pluckFrequency, time) *
                       Math.Exp(-4.6 * pluckPhase);

        int[] melody =
        {
            7, 11, 12, 7, 4, 7, 11, 14,
            7, 10, 12, 15, 12, 10, 7, 3
        };
        int melodyStep = ((int)Math.Floor(halfBeat)) % melody.Length;
        double melodyFrequency = Midi(root + melody[melodyStep] + 24);
        double melodyTone = Math.Sin(
            2.0 * Math.PI * melodyFrequency * time) *
            Math.Exp(-6.0 * pluckPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Warm(Midi(root - 12), time) *
                      Math.Exp(-3.5 * beatPhase);
        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 2)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (68.0 - 24.0 * local) * local) *
                Math.Exp(-12.0 * local);
        }

        double brush = (beatNumber == 1 || beatNumber == 3)
            ? Noise(sampleIndex) * Math.Exp(-8.0 * beatPhase)
            : 0.0;
        double hatPhase = Repeat(halfBeat, 1.0);
        double hat = Noise(sampleIndex * 3 + 11) *
                     Math.Exp(-19.0 * hatPhase);

        return pad * padGate * 0.27 +
               pluck * 0.13 +
               melodyTone * 0.065 +
               bass * 0.14 +
               kick * 0.13 +
               brush * 0.035 +
               hat * 0.014;
    }

    private static double NeonHelpdesk(double time, int sampleIndex)
    {
        const double tempo = 126.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 12;
        int[] roots = { 45, 48, 52, 50 };
        int root = roots[bar % roots.Length];
        double sixteenth = beat * 4.0;
        int arpStep = ((int)Math.Floor(sixteenth)) % 8;
        int[] arp = { 0, 7, 12, 16, 19, 16, 12, 7 };
        double stepPhase = Repeat(sixteenth, 1.0);
        double leadFrequency = Midi(root + arp[arpStep] + 12);
        double lead = Pulse(leadFrequency, time, 0.32) *
                      Math.Exp(-3.2 * stepPhase);

        int[] melody =
        {
            12, 14, 16, 19, 16, 14, 12, 7,
            12, 16, 19, 21, 19, 16, 14, 11
        };
        double halfBeat = beat * 2.0;
        int melodyStep = ((int)Math.Floor(halfBeat)) % melody.Length;
        double melodyPhase = Repeat(halfBeat, 1.0);
        double melodyTone = Square(
            Midi(root + melody[melodyStep] + 12), time) *
            Math.Exp(-4.0 * melodyPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Pulse(Midi(root - 12), time, 0.45) *
                      Math.Exp(-4.5 * beatPhase);
        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 2)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (86.0 - 38.0 * local) * local) *
                Math.Exp(-15.0 * local);
        }

        double snare = (beatNumber == 1 || beatNumber == 3)
            ? Noise(sampleIndex) * Math.Exp(-11.0 * beatPhase)
            : 0.0;
        double hat = Noise(sampleIndex * 5 + 31) *
                     Math.Exp(-24.0 * stepPhase);

        return lead * 0.15 +
               melodyTone * 0.075 +
               bass * 0.13 +
               kick * 0.17 +
               snare * 0.06 +
               hat * 0.018;
    }

    private static double CorporateGlitch(double time, int sampleIndex)
    {
        const double tempo = 110.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 10;
        int[] roots = { 50, 46, 43, 48, 45 };
        int root = roots[bar % roots.Length];
        double beatInBar = Repeat(beat, 4.0);

        double pad =
            FmTone(Midi(root + 12), time, 0.75) * 0.42 +
            FmTone(Midi(root + 15), time, 0.62) * 0.28 +
            FmTone(Midi(root + 19), time, 0.5) * 0.24;
        pad *= SmoothGate(beatInBar, 0.18) *
               SmoothGate(4.0 - beatInBar, 0.18);

        double quarterTriplet = beat * 3.0;
        int blipStep = ((int)Math.Floor(quarterTriplet)) % 12;
        int[] blips = { 0, 12, 7, 15, 3, 10, 19, 7, 12, 22, 10, 3 };
        double blipPhase = Repeat(quarterTriplet, 1.0);
        double blipFrequency = Midi(root + blips[blipStep] + 12);
        double blip = FmTone(blipFrequency, time, 2.2) *
                      Math.Exp(-6.5 * blipPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Triangle(Midi(root - 12), time) *
                      Math.Exp(-3.8 * beatPhase);
        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 3)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (78.0 - 30.0 * local) * local) *
                Math.Exp(-13.0 * local);
        }

        double glitchGate = ((int)Math.Floor(beat * 8.0) % 7 == 0)
            ? 1.0
            : 0.0;
        double glitchPhase = Repeat(beat * 8.0, 1.0);
        double glitch = Noise(sampleIndex * 7 + bar * 101) *
                        Math.Exp(-16.0 * glitchPhase) *
                        glitchGate;
        double click = (beatNumber == 2)
            ? Triangle(1200.0, time) * Math.Exp(-35.0 * beatPhase)
            : 0.0;

        return pad * 0.22 +
               blip * 0.12 +
               bass * 0.12 +
               kick * 0.15 +
               glitch * 0.055 +
               click * 0.025;
    }

    private static double MidnightSupportGlitch(
        double time,
        int sampleIndex)
    {
        const double tempo = 96.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 9;
        int root = MidnightRoots[bar % MidnightRoots.Length];
        double beatInBar = Repeat(beat, 4.0);

        double padGate = SmoothGate(beatInBar, 0.3) *
                         SmoothGate(4.0 - beatInBar, 0.3);
        double pad =
            FmTone(Midi(root + 12), time, 0.42) * 0.48 +
            FmTone(Midi(root + 15), time, 0.54) * 0.31 +
            FmTone(Midi(root + 19), time, 0.36) * 0.22;

        double triplet = beat * 3.0;
        int step = ((int)Math.Floor(triplet)) % MidnightNotes.Length;
        double stepPhase = Repeat(triplet, 1.0);
        double terminalTone = FmTone(
            Midi(root + MidnightNotes[step] + 12),
            time,
            1.65) * Math.Exp(-6.8 * stepPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Math.Sin(
            2.0 * Math.PI * Midi(root - 12) * time) *
            Math.Exp(-3.0 * beatPhase);

        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 3)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (72.0 - 26.0 * local) * local) *
                Math.Exp(-13.0 * local);
        }

        double eighth = beat * 2.0;
        double eighthPhase = Repeat(eighth, 1.0);
        double staticTick = Noise(sampleIndex * 5 + 71) *
                            Math.Exp(-25.0 * eighthPhase) * 0.6;
        double scanner = Math.Sin(
            2.0 * Math.PI * (820.0 + Math.Sin(time * 0.7) * 90.0) * time) *
            Math.Exp(-18.0 * Repeat(beat / 4.0, 1.0));

        return pad * padGate * 0.25 +
               terminalTone * 0.11 +
               bass * 0.13 +
               kick * 0.14 +
               staticTick * 0.025 +
               scanner * 0.018;
    }

    private static double ServerRushGlitch(
        double time,
        int sampleIndex)
    {
        const double tempo = 132.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 12;
        int root = ServerRoots[bar % ServerRoots.Length];

        double sixteenth = beat * 4.0;
        int step = ((int)Math.Floor(sixteenth)) % ServerArp.Length;
        double stepPhase = Repeat(sixteenth, 1.0);
        double arp = FmTone(
            Midi(root + ServerArp[step] + 12),
            time,
            2.5) * Math.Exp(-7.5 * stepPhase);

        double eighth = beat * 2.0;
        int leadStep = ((int)Math.Floor(eighth)) % 8;
        int[] leadNotes = { 12, 15, 19, 22, 19, 17, 15, 10 };
        double leadPhase = Repeat(eighth, 1.0);
        double lead = Pulse(
            Midi(root + leadNotes[leadStep] + 12),
            time,
            0.28) * Math.Exp(-5.0 * leadPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Pulse(Midi(root - 12), time, 0.38) *
                      Math.Exp(-5.2 * beatPhase);
        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 2)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (92.0 - 42.0 * local) * local) *
                Math.Exp(-17.0 * local);
        }

        double snare = (beatNumber == 1 || beatNumber == 3)
            ? Noise(sampleIndex * 3 + 97) * Math.Exp(-12.0 * beatPhase)
            : 0.0;
        double glitchGate = ((int)Math.Floor(beat * 8.0) % 5 == 0)
            ? 1.0
            : 0.0;
        double glitchPhase = Repeat(beat * 8.0, 1.0);
        double glitch = Noise(sampleIndex * 11 + bar * 131) *
                        Math.Exp(-19.0 * glitchPhase) *
                        glitchGate;

        return arp * 0.13 +
               lead * 0.055 +
               bass * 0.12 +
               kick * 0.17 +
               snare * 0.052 +
               glitch * 0.045;
    }

    private static double CoffeeBreakExe(
        double time,
        int sampleIndex)
    {
        const double tempo = 104.0;
        double beat = time * tempo / 60.0;
        int bar = ((int)Math.Floor(beat / 4.0)) % 10;
        int root = CoffeeRoots[bar % CoffeeRoots.Length];
        double beatInBar = Repeat(beat, 4.0);

        double padGate = SmoothGate(beatInBar, 0.16) *
                         SmoothGate(4.0 - beatInBar, 0.16);
        double pad =
            Warm(Midi(root + 12), time) * 0.34 +
            Warm(Midi(root + 16), time) * 0.22 +
            Warm(Midi(root + 19), time) * 0.2;

        double triplet = beat * 3.0;
        int step = ((int)Math.Floor(triplet)) % CoffeeNotes.Length;
        double stepPhase = Repeat(triplet, 1.0);
        double cupTone = FmTone(
            Midi(root + CoffeeNotes[step] + 24),
            time,
            1.25) * Math.Exp(-8.0 * stepPhase);

        double beatPhase = Repeat(beat, 1.0);
        int beatNumber = ((int)Math.Floor(beat)) % 4;
        double bass = Triangle(Midi(root - 12), time) *
                      Math.Exp(-4.2 * beatPhase);
        double kick = 0.0;
        if (beatNumber == 0 || beatNumber == 2)
        {
            double local = beatPhase * 60.0 / tempo;
            kick = Math.Sin(
                2.0 * Math.PI * (80.0 - 32.0 * local) * local) *
                Math.Exp(-14.0 * local);
        }

        double snap = (beatNumber == 1 || beatNumber == 3)
            ? Noise(sampleIndex * 7 + 151) * Math.Exp(-18.0 * beatPhase)
            : 0.0;
        double notificationPhase = Repeat(beat / 8.0, 1.0);
        double notification = FmTone(1046.5, time, 0.8) *
                              Math.Exp(-24.0 * notificationPhase);

        return pad * padGate * 0.19 +
               cupTone * 0.13 +
               bass * 0.12 +
               kick * 0.145 +
               snap * 0.035 +
               notification * 0.012;
    }

    private static double Midi(int note)
    {
        return 440.0 * Math.Pow(2.0, (note - 69.0) / 12.0);
    }

    private static double Warm(double frequency, double time)
    {
        double phase = 2.0 * Math.PI * frequency * time;
        return Math.Sin(phase) + Math.Sin(phase * 2.0) * 0.17;
    }

    private static double Triangle(double frequency, double time)
    {
        return Math.Asin(Math.Sin(2.0 * Math.PI * frequency * time)) *
               (2.0 / Math.PI);
    }

    private static double Square(double frequency, double time)
    {
        return Math.Sin(2.0 * Math.PI * frequency * time) >= 0.0
            ? 1.0
            : -1.0;
    }

    private static double Pulse(
        double frequency,
        double time,
        double duty)
    {
        return Repeat(time * frequency, 1.0) < duty ? 1.0 : -1.0;
    }

    private static double FmTone(
        double frequency,
        double time,
        double modulation)
    {
        double phase = 2.0 * Math.PI * frequency * time;
        return Math.Sin(phase + Math.Sin(phase * 2.0) * modulation);
    }

    private static double SmoothGate(double value, double width)
    {
        double normalized = Clamp01(value / width);
        return normalized * normalized * (3.0 - 2.0 * normalized);
    }

    private static double Repeat(double value, double length)
    {
        return value - Math.Floor(value / length) * length;
    }

    private static double SoftLimit(double value)
    {
        return Math.Tanh(value * 1.45) * 0.86;
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0.0, Math.Min(1.0, value));
    }

    private static double Noise(int seed)
    {
        uint value = unchecked((uint)seed);
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return (value & 0xffff) / 32767.5 - 1.0;
    }
}
'@

Add-Type -TypeDefinition $source -Language CSharp
$outputDirectory = Join-Path $PSScriptRoot '..\MusicPreviews'
$resolvedOutput = [System.IO.Path]::GetFullPath($outputDirectory)
[MusicPreviewGenerator]::GenerateAll($resolvedOutput)
Get-ChildItem -LiteralPath $resolvedOutput -Filter '*.wav' |
    Select-Object Name, Length, LastWriteTime
