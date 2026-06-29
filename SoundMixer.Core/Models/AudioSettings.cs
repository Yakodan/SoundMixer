using Newtonsoft.Json;
using System.IO;

namespace SoundMixer.Core.Models;

[JsonObject(MemberSerialization.OptIn)]
public class AudioSettings
{
    [JsonProperty("inputDeviceId")] public string? InputDeviceId { get; set; }
    [JsonProperty("outputDeviceId")] public string? OutputDeviceId { get; set; }
    [JsonProperty("virtualCableDeviceId")] public string? VirtualCableDeviceId { get; set; }

    [JsonProperty("sampleRate")] public int SampleRate { get; set; } = 48000;
    [JsonProperty("bufferSize")] public int BufferSize { get; set; } = 480;
    [JsonProperty("channels")] public int Channels { get; set; } = 1;

    [JsonProperty("microphoneVolume")] public float MicrophoneVolume { get; set; } = 1.0f;
    [JsonProperty("soundboardVolume")] public float SoundboardVolume { get; set; } = 1.0f;
    [JsonProperty("monitorVolume")] public float MonitorVolume { get; set; } = 0.5f;

    [JsonProperty("monitorEnabled")] public bool MonitorEnabled { get; set; } = false;
    [JsonProperty("monitorMicrophone")] public bool MonitorMicrophone { get; set; } = false;
    [JsonProperty("monitorSoundboard")] public bool MonitorSoundboard { get; set; } = true;

    [JsonProperty("equalizerBands")] public float[] EqualizerBands { get; set; } = new float[10];

    [JsonProperty("noiseGateEnabled")] public bool NoiseGateEnabled { get; set; } = false;
    [JsonProperty("noiseGateThreshold")] public float NoiseGateThreshold { get; set; } = -40f;

    [JsonProperty("compressorEnabled")] public bool CompressorEnabled { get; set; } = false;
    [JsonProperty("compressorThreshold")] public float CompressorThreshold { get; set; } = -20f;
    [JsonProperty("compressorRatio")] public float CompressorRatio { get; set; } = 4f;

    [JsonProperty("theme")] public string Theme { get; set; } = "DarkPurple";

    [JsonProperty("soundClips", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<SoundClipInfo> SoundClips { get; set; } = new();

    public void Save(string path)
    {
        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Ошибка сохранения: {ex.Message}");
        }
    }

    public static AudioSettings Load(string path)
    {
        if (!File.Exists(path)) return new AudioSettings();

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonConvert.DeserializeObject<AudioSettings>(json);
            return settings ?? new AudioSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Ошибка загрузки: {ex.Message}");
            return new AudioSettings();
        }
    }
}