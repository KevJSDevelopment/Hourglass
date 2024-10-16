using System;
using System.IO;
using System.Threading.Tasks;

public class LocalAudioFileManager
{
    private readonly string _baseDirectory;

    public LocalAudioFileManager(string appName)
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "MotivationalAudio"
        );
    }

    public async Task<string> SaveAudioFileAsync(string computerId, Stream audioStream, string fileExtension)
    {
        string computerDirectory = Path.Combine(_baseDirectory, computerId);
        Directory.CreateDirectory(computerDirectory);

        string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{fileExtension}";
        string filePath = Path.Combine(computerDirectory, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await audioStream.CopyToAsync(fileStream);
        }

        return filePath;
    }

    public bool DeleteAudioFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Stream GetAudioFileStream(string filePath)
    {
        if (File.Exists(filePath))
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
        throw new FileNotFoundException("Audio file not found.", filePath);
    }
}