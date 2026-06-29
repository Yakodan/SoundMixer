namespace SoundMixer.Core.Soundboard;

public static class SoundClipStorage
{
    public static string ClipsFolder
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "SoundMixer", "Clips");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string ImportFile(string sourceFilePath, string clipId)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Файл не найден: {sourceFilePath}");

        string extension = Path.GetExtension(sourceFilePath);
        string destFileName = $"{clipId}{extension}";
        string destPath = Path.Combine(ClipsFolder, destFileName);

        File.Copy(sourceFilePath, destPath, overwrite: true);
        return destPath;
    }

    public static void ExportFile(string storedFilePath, string destinationPath)
    {
        if (!File.Exists(storedFilePath))
            throw new FileNotFoundException($"Файл не найден: {storedFilePath}");

        var destDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(storedFilePath, destinationPath, overwrite: true);
    }

    public static void DeleteFile(string storedFilePath)
    {
        try
        {
            if (File.Exists(storedFilePath))
                File.Delete(storedFilePath);
        }
        catch { }
    }

    public static bool FileExists(string storedFilePath)
    {
        return !string.IsNullOrEmpty(storedFilePath) && File.Exists(storedFilePath);
    }

    public static long GetTotalSize()
    {
        if (!Directory.Exists(ClipsFolder)) return 0;
        return Directory.GetFiles(ClipsFolder).Sum(f => new FileInfo(f).Length);
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}