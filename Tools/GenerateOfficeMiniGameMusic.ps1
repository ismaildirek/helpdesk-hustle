$source = @'
using System;
using System.IO;

public static class OfficeMiniGameMusicGenerator
{
    private const int SampleRate = 22050;

    public static void Generate(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        WriteWave(Path.Combine(outputDirectory, "17_server_cooling.wav"),
            8.0 * 4.0 * 60.0 / 100.0, ServerCooling);
        WriteWave(Path.Combine(outputDirectory, "18_security_scan.wav"),
            8.0 * 4.0 * 60.0 / 124.0, SecurityScan);
    }

    private static void WriteWave(string path, double duration,
        Func<double, int, double> composer)
    {
        int count = (int)Math.Ceiling(duration * SampleRate);
        int size = count * sizeof(short);
        using (FileStream stream = File.Create(path))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' }); writer.Write(36 + size);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' }); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate);
            writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' }); writer.Write(size);
            for (int i = 0; i < count; i++)
            {
                double time = i / (double)SampleRate;
                double edge = Math.Min(Clamp(time / 0.018), Clamp((duration - time) / 0.018));
                double sample = Math.Tanh(composer(time, i) * 1.15) * edge;
                writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, sample)) * 32767));
            }
        }
    }

    private static double ServerCooling(double time, int index)
    {
        const double bpm = 100;
        double beat = time * bpm / 60.0;
        int[] roots = { 45, 41, 48, 43 };
        int root = roots[((int)(beat / 4)) % roots.Length];
        double sixteenth = beat * 4;
        int[] arp = { 0, 7, 12, 15, 12, 7, 3, 10 };
        double phase = Repeat(sixteenth, 1);
        double pulse = Pulse(Midi(root + arp[((int)sixteenth) % arp.Length] + 12), time, .42)
            * Math.Exp(-4.2 * phase) * 0.075;
        double pad = (Sine(Midi(root), time) + Sine(Midi(root + 7), time) * .55) * 0.055;
        double fan = Noise(index * 5 + 31) * (0.012 + 0.006 * Math.Sin(time * Math.PI * 0.5));
        double bass = Triangle(Midi(root - 12), time) * Math.Exp(-3 * Repeat(beat, 1)) * 0.11;
        return pulse + pad + fan + bass;
    }

    private static double SecurityScan(double time, int index)
    {
        const double bpm = 124;
        double beat = time * bpm / 60.0;
        int[] roots = { 50, 46, 53, 48 };
        int root = roots[((int)(beat / 4)) % roots.Length];
        double eighth = beat * 2;
        int[] sequence = { 0, 12, 7, 15, 3, 10, 14, 7 };
        double phase = Repeat(eighth, 1);
        double scan = Sine(Midi(root + sequence[((int)eighth) % sequence.Length] + 12), time)
            * Math.Exp(-6 * phase) * 0.12;
        double gate = Repeat(beat * 4, 1) < .52 ? 1 : 0;
        double synth = Pulse(Midi(root + 7), time, .28) * gate * 0.025;
        double bass = Sine(Midi(root - 12), time) * Math.Exp(-4 * Repeat(beat, 1)) * 0.14;
        double tick = Noise(index * 7 + 41) * Math.Exp(-32 * Repeat(beat * 2, 1)) * 0.02;
        return scan + synth + bass + tick;
    }

    private static double Midi(int note) { return 440 * Math.Pow(2, (note - 69) / 12.0); }
    private static double Sine(double frequency, double time) { return Math.Sin(2 * Math.PI * frequency * time); }
    private static double Triangle(double frequency, double time) { return Math.Asin(Sine(frequency, time)) * 2 / Math.PI; }
    private static double Pulse(double frequency, double time, double width) { return Repeat(frequency * time, 1) < width ? 1 : -1; }
    private static double Repeat(double value, double length) { return value - Math.Floor(value / length) * length; }
    private static double Clamp(double value) { return Math.Max(0, Math.Min(1, value)); }
    private static double Noise(int value)
    {
        unchecked { uint x = (uint)value; x ^= x << 13; x ^= x >> 17; x ^= x << 5; return x / (double)uint.MaxValue * 2 - 1; }
    }
}
'@

Add-Type -TypeDefinition $source -Language CSharp
$output = Join-Path $PSScriptRoot '..\Assets\Audio\MiniGameMusic'
[OfficeMiniGameMusicGenerator]::Generate([IO.Path]::GetFullPath($output))
Write-Output "Created two mini-game music tracks in $output"
