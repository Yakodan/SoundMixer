using SoundMixer.Core.Models;
using System.IO;

namespace SoundMixer.Core.Soundboard;

public class SoundboardManager : IDisposable
{
    private readonly List<SoundClip> _clips = new();
    private readonly object _lock = new();
    private readonly int _sampleRate;
    private readonly int _channels;

    public IReadOnlyList<SoundClip> Clips => _clips.AsReadOnly();

    public event EventHandler? ClipsChanged;
    public event EventHandler<string>? ClipLoadError; // НОВОЕ СОБЫТИЕ

    public int SampleRate => _sampleRate;
    public int Channels => _channels;

    public SoundboardManager(int sampleRate = 48000, int channels = 1)
    {
        _sampleRate = sampleRate;
        _channels = channels;
    }

    public SoundClip AddClip(string sourceFilePath, string? name = null)
    {
        var info = new SoundClipInfo
        {
            Name = name ?? Path.GetFileNameWithoutExtension(sourceFilePath),
            OriginalFilePath = sourceFilePath
        };

        info.FilePath = SoundClipStorage.ImportFile(sourceFilePath, info.Id);

        var clip = new SoundClip(info);
        clip.Load(_sampleRate, _channels);

        lock (_lock)
        {
            _clips.Add(clip);
        }

        ClipsChanged?.Invoke(this, EventArgs.Empty);
        return clip;
    }

    public void RemoveClip(string clipId)
    {
        lock (_lock)
        {
            var clip = _clips.FirstOrDefault(c => c.Info.Id == clipId);
            if (clip != null)
            {
                clip.Stop();
                SoundClipStorage.DeleteFile(clip.Info.FilePath);
                clip.Dispose();
                _clips.Remove(clip);
            }
        }
        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ExportClip(string clipId, string destinationPath)
    {
        lock (_lock)
        {
            var clip = _clips.FirstOrDefault(c => c.Info.Id == clipId);
            if (clip != null)
                SoundClipStorage.ExportFile(clip.Info.FilePath, destinationPath);
        }
    }

    public void PlayClip(string clipId)
    {
        lock (_lock)
        {
            _clips.FirstOrDefault(c => c.Info.Id == clipId)?.Play();
        }
    }

    public void StopClip(string clipId)
    {
        lock (_lock)
        {
            _clips.FirstOrDefault(c => c.Info.Id == clipId)?.Stop();
        }
    }

    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var clip in _clips)
                clip.Stop();
        }
    }

    public int ReadMixed(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);
        float[] tempBuffer = new float[count];
        int maxRead = 0;

        lock (_lock)
        {
            foreach (var clip in _clips)
            {
                if (!clip.IsPlaying) continue;

                Array.Clear(tempBuffer, 0, count);
                int read = clip.ReadSamples(tempBuffer, 0, count);
                if (read > maxRead) maxRead = read;

                for (int i = 0; i < read; i++)
                    buffer[offset + i] += tempBuffer[i];
            }
        }

        for (int i = offset; i < offset + count; i++)
        {
            if (buffer[i] > 1f) buffer[i] = (float)Math.Tanh(buffer[i]);
            else if (buffer[i] < -1f) buffer[i] = (float)Math.Tanh(buffer[i]);
        }

        return maxRead;
    }

    public void LoadFromSettings(List<SoundClipInfo> clipInfos)
    {
        lock (_lock)
        {
            foreach (var info in clipInfos)
            {
                try
                {
                    // Жесткая перепроверка пути — если сохранён старый путь, формируем новый
                    string expectedFileName = info.Id + Path.GetExtension(info.FilePath);
                    string correctPath = Path.Combine(SoundClipStorage.ClipsFolder, expectedFileName);

                    if (File.Exists(correctPath))
                    {
                        info.FilePath = correctPath;
                    }
                    else if (!SoundClipStorage.FileExists(info.FilePath))
                    {
                        if (info.OriginalFilePath != null && File.Exists(info.OriginalFilePath))
                        {
                            info.FilePath = SoundClipStorage.ImportFile(info.OriginalFilePath, info.Id);
                        }
                        else
                        {
                            ClipLoadError?.Invoke(this, $"Файл '{info.Name}' не найден в хранилище!");
                            continue;
                        }
                    }

                    var clip = new SoundClip(info);
                    clip.Load(_sampleRate, _channels);
                    _clips.Add(clip);
                }
                catch (Exception ex)
                {
                    ClipLoadError?.Invoke(this, $"Ошибка загрузки '{info.Name}': {ex.Message}");
                }
            }
        }
        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    public List<SoundClipInfo> GetClipInfos()
    {
        lock (_lock)
        {
            return _clips.Select(c => c.Info).ToList();
        }
    }

    public void ToggleClip(string clipId)
    {
        lock (_lock)
        {
            _clips.FirstOrDefault(c => c.Info.Id == clipId)?.TogglePlay();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var clip in _clips)
                clip.Dispose();
            _clips.Clear();
        }
    }
}