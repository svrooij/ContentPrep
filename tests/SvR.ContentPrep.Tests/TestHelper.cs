using System.Security.Cryptography;

namespace SvRooij.ContentPrep.Tests;

public static class TestHelper
{
    /// <summary>
    /// Asynchronously create a temp file with random data
    /// </summary>
    /// <param name="folder">in which folder should the file be created</param>
    /// <param name="filename">desired filename</param>
    /// <param name="sizeInMb">Filesize in MB</param>
    /// <param name="cancellationToken">CancellationToken to cancel the request</param>
    /// <returns>Full path to tempfile</returns>
    public static async Task<string> GenerateTempFileInFolder(string folder, string filename, int sizeInMb, CancellationToken cancellationToken = default)
    {
        var tempFilename = Path.Combine(folder, filename);
        Random rnd = new Random();
        byte[] data = new byte[1024];
        await using var fs = new FileStream(tempFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        int iterations = sizeInMb * 1024;
        for (int i = 0; i < iterations; i++)
        {
            rnd.NextBytes(data);
            await fs.WriteAsync(data, cancellationToken);
        }
        fs.Close();
        return tempFilename;
    }

    public static string GetTestFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static void RemoveFolderIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {

        }

    }
}