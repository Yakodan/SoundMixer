namespace SoundMixer.Core.Models;

public class SoundClipInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Оригинальное имя файла</summary>
    public string Name { get; set; } = "";

    /// <summary>Пользовательское имя (если задано, показывается вместо Name)</summary>
    public string? DisplayName { get; set; }

    /// <summary>Эмодзи или короткий текст как иконка</summary>
    public string Emoji { get; set; } = "🔊";

    /// <summary>Путь к файлу (теперь всегда внутри AppData)</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Оригинальный путь откуда был импортирован</summary>
    public string? OriginalFilePath { get; set; }

    public float Volume { get; set; } = 1.0f;
    public string? HotKey { get; set; }
    public double TrimStart { get; set; } = 0;
    public double TrimEnd { get; set; } = 0;
    public bool Loop { get; set; } = false;
    public string Color { get; set; } = "#FF4CAF50";

    /// <summary>Отображаемое имя: DisplayName если задано, иначе Name</summary>
    public string EffectiveName => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : Name;
}