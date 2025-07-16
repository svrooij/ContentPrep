using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SvRooij.ContentPrep
{
    internal static class Zipper
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal static async Task<long> ZipDirectory(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
          string directory,
          string targetZipFile,
          CompressionLevel compressionLevel,
          bool includeBaseDirectory,
          bool forceCorrectNames = false)
        {
            string directoryName = Path.GetDirectoryName(targetZipFile)!;
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            long num = 0;
            foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                num += file.Length;



            if (!forceCorrectNames)
            {
                // This native method to create a zip from a directory works differently on .net framework
                // all files have directories set with the \ as seperator
                // which is not what you want in some cases.
                ZipFile.CreateFromDirectory(directory, targetZipFile, compressionLevel, includeBaseDirectory);
            }
            else
            {
                string baseDirectory = includeBaseDirectory ? Path.GetDirectoryName(directory)! : directory;

                using (var archive = ZipFile.Open(targetZipFile, ZipArchiveMode.Create, System.Text.Encoding.UTF8))
                {
                    foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        if (file.Length > 0)
                        {
                            var nameInArchive = file.FullName.Replace(baseDirectory, "").Replace('\\', '/').TrimStart('/');
                            archive.CreateEntryFromFile(file.FullName, nameInArchive, compressionLevel);
                        }
                    }
                }

            }

            return num;
        }

        internal static async Task UnzipStreamAsync(Stream stream, string destinationFolder, CancellationToken cancellationToken)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    string directoryName = Path.Combine(destinationFolder, Path.GetDirectoryName(entry.FullName)!);
                    if (!string.IsNullOrEmpty(directoryName))
                        Directory.CreateDirectory(directoryName);
                    if (entry.Length > 0L)
                    {
                        string destinationFileName = Path.Combine(destinationFolder, entry.FullName);
                        using (Stream entryStream = entry.Open())
                        using (FileStream fileStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                        {
                            await entryStream.CopyToAsync(fileStream);
                            fileStream.Close();
                            entryStream.Close();
                        }
                    }
                }
            }
        }
    }
}
