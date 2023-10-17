using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SvRooij.ContentPrep.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SvRooij.ContentPrep
{
    /// <summary>
    /// Main entry point for the ContentPrep library
    /// </summary>
    public class Packager
    {
        private readonly ILogger<Packager> logger;
        internal const string PackageFileExtension = ".intunewin";
        // Version of the latest IntuneWinAppUtil tool
        internal const string ToolVersion = "1.8.4.0";
        internal const string EncryptedPackageFileName = "IntunePackage.intunewin";

        /// <summary>
        /// Creates a new instance of the Packager class
        /// </summary>
        /// <param name="logger">Supply an optional ILogger, or register by DI off course.</param>
        public Packager(ILogger<Packager>? logger = null)
        {
            this.logger = logger ?? new NullLogger<Packager>();
        }

        /// <summary>
        /// Creates a package from a setup file
        /// </summary>
        /// <param name="folder">Full path of source folder</param>
        /// <param name="setupFile">Full path of main setup file, in source folder</param>
        /// <param name="outputFolder">Output path to publish the package</param>
        /// <param name="applicationDetails">(optional) Application details, this code does not load this data from the MSI file.</param>
        /// <param name="cancellationToken">(optiona) Cancellation token</param>
        public async Task CreatePackage(
          string folder,
          string setupFile,
          string outputFolder,
          ApplicationDetails? applicationDetails = null,
          CancellationToken cancellationToken = default)
        {
            CheckCreateParamsOrThrow(folder, setupFile, outputFolder);
            logger.LogInformation("Creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string outputFileName = GetOutputFileName(setupFile, outputFolder);
            try
            {
                //CheckParamsOrThrow(folder, setupFile, outputFolder);
                string packageFolder = Path.Combine(tempFolder, "IntuneWinPackage");
                string packageContentFolder = Path.Combine(packageFolder, "Contents");
                string encryptedPackageLocation = Path.Combine(packageContentFolder, EncryptedPackageFileName);

                logger.LogInformation("Compressing the source folder {Folder} to {EncryptedPackageLocation}", folder, encryptedPackageLocation);
                await Zipper.ZipDirectory(folder, encryptedPackageLocation, false, false);

                long setupFileSize = new FileInfo(encryptedPackageLocation).Length;
                logger.LogInformation("Generating application info");
                // TODO: Add support for reading info from MSI files, but has to be cross platform
                ApplicationInfo applicationInfo = applicationDetails?.MsiInfo != null
                    ? (ApplicationInfo)new MsiApplicationInfo() { MsiInfo = applicationDetails.MsiInfo }
                    : new ApplicationInfo();
                applicationInfo.FileName = EncryptedPackageFileName;
                applicationInfo.Name = string.IsNullOrEmpty(applicationDetails?.Name) ? Path.GetFileName(setupFile) : applicationDetails!.Name!;
                applicationInfo.UnencryptedContentSize = setupFileSize;
                applicationInfo.ToolVersion = ToolVersion;
                applicationInfo.SetupFile = setupFile.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar);
                logger.LogDebug("Application info: {@ApplicationInfo}", applicationInfo);
                logger.LogInformation("Encrypting file {EncryptedPackageLocation}", encryptedPackageLocation);
                applicationInfo.EncryptionInfo = await Encryptor.EncryptFileAsync(encryptedPackageLocation, cancellationToken);
                string metadataFolder = Path.Combine(packageFolder, "Metadata");
                string metadataFile = Path.Combine(metadataFolder, "Detection.xml");
                logger.LogDebug("Generating detection XML file {MetadataFile}", metadataFile);
                string xml = applicationInfo.ToXml();
                if (!Directory.Exists(metadataFolder))
                    Directory.CreateDirectory(metadataFolder);

                using (FileStream fileStream = new FileStream(metadataFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    byte[] xmlData = Encoding.UTF8.GetBytes(xml);
                    await fileStream.WriteAsync(xmlData, 0, xmlData.Length, cancellationToken);
                }
                logger.LogInformation("Generated detection XML file {MetadataFile}", metadataFile);
                await Zipper.ZipDirectory(packageFolder, outputFileName, true, true);
                logger.LogInformation("Done creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
                throw;
            }
            finally
            {

                await Task.Delay(1000, cancellationToken);
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        logger.LogDebug("Removing temporary files");
                        Directory.Delete(tempFolder, true);
                        logger.LogDebug("Removed temporary files");
                    }

                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error removing temporary files");
                }
            }
        }

        /// <summary>
        /// Decrypt an intunewin file to a folder
        /// </summary>
        /// <param name="packageFile">Full path of intunewin file</param>
        /// <param name="outputFolder">Output folder</param>
        /// <param name="cancellationToken">(optional) Cancellation token</param>
        /// <returns></returns>
        public async Task Unpack(
            string packageFile,
            string outputFolder,
            CancellationToken cancellationToken = default)
        {
            CheckUnpackParamsOrThrow(packageFile, outputFolder);
            logger.LogInformation("Unpacking intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                // Unzip file packageFile to tempFolder
                logger.LogDebug("Unzipping {PackageFile} to {TempFolder}", packageFile, tempFolder);
                using (FileStream fileStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await Zipper.UnzipStreamAsync(fileStream, tempFolder, cancellationToken);
                }
                logger.LogDebug("Unzipped {PackageFile} to {TempFolder}", packageFile, tempFolder);
                // Read metadata file
                var metadataFile = Path.Combine(tempFolder, "IntuneWinPackage", "Metadata", "Detection.xml");
                logger.LogDebug("Reading metadata file {MetadataFile}", metadataFile);

                ApplicationInfo? applicationInfo = null;
                using (FileStream metadataStream = new FileStream(metadataFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
                using (StreamReader metadataCopy = new StreamReader(metadataStream))
                {
                    applicationInfo = ApplicationInfo.FromXml(metadataCopy);
                    metadataCopy.Close();
                    metadataStream.Close();
                }

                if (applicationInfo == null || applicationInfo.EncryptionInfo == null)
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Could not read metadata file {0}", metadataFile));

                // Decrypt file
                var encryptedPackage = Path.Combine(tempFolder, "IntuneWinPackage", "Contents", applicationInfo.FileName);
                logger.LogDebug("Decrypting {EncryptedPackage} to {OutputFolder}", encryptedPackage, outputFolder);
                using (FileStream encryptedFileStream = new FileStream(encryptedPackage, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
                using (Stream decryptedStream = await Encryptor.DecryptFileAsync(encryptedFileStream, applicationInfo.EncryptionInfo.EncryptionKey!, applicationInfo.EncryptionInfo.MacKey!, cancellationToken))
                {
                    await Zipper.UnzipStreamAsync(decryptedStream, outputFolder, cancellationToken);
                    decryptedStream.Close();
                    encryptedFileStream.Close();
                    logger.LogInformation("Unpacked intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
                }

                Directory.Delete(tempFolder, true);

            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error unpacking intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
                throw;
            }
        }

        internal static void TryWritingToFolder(string folder)
        {
            string path = Directory.Exists(folder) ? Path.Combine(folder, Guid.NewGuid().ToString()) : throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Folder '{0}' can not be found", folder));
            FileStream? tempFileStream = null;
            try
            {
                // This will throw if the folder is not writable
                tempFileStream = File.Open(path, FileMode.OpenOrCreate);
            }
            finally
            {
                tempFileStream?.Close();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        internal static string GetOutputFileName(string setupFile, string outputFolder) => Path.Combine(outputFolder, string.Format(CultureInfo.InvariantCulture, "{0}{1}", Path.GetFileNameWithoutExtension(setupFile), ".intunewin"));

        private static void CheckSetupFileExistsAndInSetupFolder(string setupFolder, string setupFile)
        {
            if (!File.Exists(setupFile))
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "File '{0}' can not be found", setupFile));
            if (setupFile.IndexOf(setupFolder, StringComparison.OrdinalIgnoreCase) != 0)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Setup file '{0}' should be in folder '{1}'", setupFile, setupFolder));
        }

        private static void CheckCreateParamsOrThrow(string folder, string setupFile, string outputFolder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException(nameof(folder));
            if (string.IsNullOrEmpty(setupFile))
                throw new ArgumentNullException(nameof(setupFile));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(nameof(outputFolder));
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Folder '{0}' can not be found", folder));
            CheckSetupFileExistsAndInSetupFolder(folder, setupFile);
            TryWritingToFolder(outputFolder);
        }

        private static void CheckUnpackParamsOrThrow(string packageFile, string outputFolder)
        {
            if (string.IsNullOrEmpty(packageFile))
                throw new ArgumentNullException(nameof(packageFile));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(nameof(outputFolder));
            if (!File.Exists(packageFile))
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "File '{0}' can not be found", packageFile));
            TryWritingToFolder(outputFolder);
        }
    }
}
