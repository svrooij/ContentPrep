using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SvR.ContentPrep
{
    internal static class Zipper
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal static async Task<long> ZipDirectory(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
          string directory,
          string targetZipFile,
          bool noCompression,
          bool includeBaseDirectory,
          bool appendFile = false,
          bool addFileToExistingZip = false)
        {
            string directoryName = Path.GetDirectoryName(targetZipFile);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
            if (!appendFile && File.Exists(targetZipFile))
                File.Delete(targetZipFile);
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            long num = 0;
            foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                num += file.Length;
            CompressionLevel compressionLevel = noCompression ? CompressionLevel.NoCompression : CompressionLevel.Fastest;

            // This is an async version of the synchronous ZipFile.CreateFromDirectory method
            //using (var archive = ZipFile.Open(targetZipFile, addFileToExistingZip ? ZipArchiveMode.Update : ZipArchiveMode.Create))
            //    {
            //        foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            //        {
            //            if (file.Length > 0)
            //            {
            //                string entryName = file.FullName.Replace(directory, "");
            //                ZipArchiveEntry entry = archive.CreateEntry(entryName, compressionLevel);
            //                using (Stream entryStream = entry.Open())
            //                using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            //                {
            //                    await fileStream.CopyToAsync(entryStream);
            //                    fileStream.Close();
            //                    entryStream.Close();
            //                }
            //            }
            //        }

            //    }
            if (!addFileToExistingZip)
            {
                ZipFile.CreateFromDirectory(directory, targetZipFile, compressionLevel, includeBaseDirectory);
            }
            else
            {
                // TODO: This is a hack to get around the fact that ZipFile.CreateFromDirectory doesn't support appending to an existing zip file
                using (var archive = ZipFile.Open(targetZipFile, ZipArchiveMode.Update))
                {
                    foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        if (file.Length > 0)
                        {
                            archive.CreateEntryFromFile(file.FullName, file.FullName.Replace(directory, ""), compressionLevel);
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
                    string directoryName = Path.Combine(destinationFolder, Path.GetDirectoryName(entry.FullName));
                    if (!string.IsNullOrEmpty(directoryName))
                        Directory.CreateDirectory(directoryName);
                    if (entry.Length > 0L)
                    {
                        string destinationFileName = Path.Combine(destinationFolder, entry.FullName);
                        using (Stream entryStream = entry.Open())
                        using (FileStream fileStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
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
