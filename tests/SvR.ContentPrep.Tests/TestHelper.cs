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
            bufferSize: 8192, useAsync: true);
        int iterations = sizeInMb * 1024;
        for (int i = 0; i < iterations; i++)
        {
            rnd.NextBytes(data);
            await fs.WriteAsync(data, cancellationToken);
        }
        fs.Close();
        return tempFilename;
    }

    /// <summary>
    /// Generate a directory name in the temp folder
    /// </summary>
    /// <returns>path of the created directory</returns>
    public static string GetTestFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// Delete a directory recursive, if exists
    /// </summary>
    /// <param name="path">Path of the directory to delete</param>
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

    /// <summary>
    /// Compare two byte arrays (hash output)
    /// </summary>
    /// <param name="input">array 1</param>
    /// <param name="compareTo">array 2</param>
    /// <returns>True if byte arrays are exactly equal</returns>
    internal static bool CompareHashes(byte[] input, byte[] compareTo)
    {
        if (input.Length != compareTo.Length)
        {
            return false;
        }

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] != compareTo[i])
            {
                return false;
            }
        }

        return true;
    }
}