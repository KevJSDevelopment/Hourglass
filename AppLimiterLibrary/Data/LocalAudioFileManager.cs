using System;
using System.IO;
using System.Threading.Tasks;

public class LocalAudioFileManager
{
    private readonly string _baseDirectory;

    public LocalAudioFileManager()
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MotivationalAudio"
        );
    }

    public async Task<string[]> SaveAudioFileAsync(string computerId, Stream audioStream, string fileName, string fileExtension)
    {
        string computerDirectory = Path.Combine(_baseDirectory, computerId);
        Directory.CreateDirectory(computerDirectory);

        string newFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
        string filePath = Path.Combine(computerDirectory, newFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await audioStream.CopyToAsync(fileStream);
        }

        return [filePath, newFileName];
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