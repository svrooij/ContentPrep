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
        private readonly ILogger<Packager> _logger;
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
            _logger = logger ?? new NullLogger<Packager>();
        }

        /// <summary>
        /// Creates a package from a setup file
        /// </summary>
        /// <param name="folder">Full path of source folder</param>
        /// <param name="setupFile">Full path of main setup file, in source folder</param>
        /// <param name="outputFolder">Output path to publish the package</param>
        /// <param name="applicationDetails">(optional) Application details, this code does not load this data from the MSI file.</param>
        /// <param name="cancellationToken">(optiona) Cancellation token</param>
        /// <returns><see cref="ApplicationInfo"/> of the created package</returns>
        public async Task<ApplicationInfo?> CreatePackage(
          string folder,
          string setupFile,
          string outputFolder,
          ApplicationDetails? applicationDetails = null,
          CancellationToken cancellationToken = default)
        {
            CheckCreateParamsOrThrow(folder, setupFile, outputFolder);
            _logger.LogInformation("Creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string outputFileName = GetOutputFileName(setupFile, outputFolder);
            try
            {
                //CheckParamsOrThrow(folder, setupFile, outputFolder);
                string packageFolder = Path.Combine(tempFolder, "IntuneWinPackage");
                string packageContentFolder = Path.Combine(packageFolder, "Contents");
                string encryptedPackageLocation = Path.Combine(packageContentFolder, EncryptedPackageFileName);

                _logger.LogInformation("Compressing the source folder {Folder} to {EncryptedPackageLocation}", folder, encryptedPackageLocation);
                await Zipper.ZipDirectory(folder, encryptedPackageLocation, false, false);

                long filesizeBeforeEncryption = new FileInfo(encryptedPackageLocation).Length;
                _logger.LogInformation("Generating application info");
                // TODO: Add support for reading info from MSI files, but has to be cross platform
                ApplicationInfo applicationInfo = applicationDetails?.MsiInfo != null
                    ? new MsiApplicationInfo() { MsiInfo = applicationDetails.MsiInfo }
                    : new ApplicationInfo();
                applicationInfo.FileName = EncryptedPackageFileName;
                applicationInfo.Name = string.IsNullOrEmpty(applicationDetails?.Name) ? Path.GetFileName(setupFile) : applicationDetails!.Name!;
                applicationInfo.UnencryptedContentSize = filesizeBeforeEncryption;
                applicationInfo.ToolVersion = ToolVersion;
                applicationInfo.SetupFile = setupFile.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar);
                _logger.LogDebug("Application info: {@ApplicationInfo}", applicationInfo);
                _logger.LogInformation("Encrypting file {EncryptedPackageLocation}", encryptedPackageLocation);
                applicationInfo.EncryptionInfo = await Encryptor.EncryptFileAsync(encryptedPackageLocation, cancellationToken);
                string metadataFolder = Path.Combine(packageFolder, "Metadata");
                string metadataFile = Path.Combine(metadataFolder, "Detection.xml");
                _logger.LogDebug("Generating detection XML file {MetadataFile}", metadataFile);
                string xml = applicationInfo.ToXml();
                if (!Directory.Exists(metadataFolder))
                    Directory.CreateDirectory(metadataFolder);

                using (FileStream fileStream = new FileStream(metadataFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    byte[] xmlData = Encoding.UTF8.GetBytes(xml);
                    await fileStream.WriteAsync(xmlData, 0, xmlData.Length, cancellationToken);
                }
                _logger.LogInformation("Generated detection XML file {MetadataFile}", metadataFile);
                await Zipper.ZipDirectory(packageFolder, outputFileName, true, true);
                _logger.LogInformation("Done creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
                return applicationInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating package for {SetupFile} in {Folder} to {OutputFolder}", setupFile, folder, outputFolder);
                throw;
            }
            finally
            {

                await Task.Delay(50, cancellationToken);
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        _logger.LogDebug("Removing temporary files");
                        Directory.Delete(tempFolder, true);
                        _logger.LogDebug("Removed temporary files");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing temporary files");
                }
            }
        }

        /// <summary>
        /// Encrypt a zip file to be uploaded to Intune programmatically, without zipping the result with the `MetaData\detections.xml` file. The output stream should be exactly 48 bytes larger than the input stream. Because it will have the hash (32 bytes) and the iv (16 bytes) prefixed.
        /// </summary>
        /// <param name="streamWithZippedSetupFiles"><see cref="Stream"/> with the zipped setup files</param>
        /// <param name="outputStream"><see cref="Stream"/> to write the encrypted package to</param>
        /// <param name="applicationDetails">Specify the information about the package. `SetupFile` and `Name` are mandatory.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>Normally you would use the <see cref="CreatePackage(string, string, string, ApplicationDetails?, CancellationToken)"/> method, but if you want to upload that to Intune, you'll have to extract it anyway. <see href="https://svrooij.io/2023/08/31/publish-apps-to-intune/#extracting-the-intunewin-file">this blog</see></remarks>
        public async Task<ApplicationInfo?> CreateUploadablePackage(
            Stream streamWithZippedSetupFiles,
            Stream outputStream,
            ApplicationDetails applicationDetails,
            CancellationToken cancellationToken = default)
        {
            ApplicationInfo applicationInfo = applicationDetails.MsiInfo != null
                    ? new MsiApplicationInfo() { MsiInfo = applicationDetails.MsiInfo }
                    : new ApplicationInfo();
            applicationInfo.FileName = EncryptedPackageFileName;
            applicationInfo.Name = applicationDetails.Name!;
            applicationInfo.UnencryptedContentSize = streamWithZippedSetupFiles.Length;
            applicationInfo.ToolVersion = ToolVersion;
            applicationInfo.SetupFile = applicationDetails.SetupFile!;
            applicationInfo.EncryptionInfo = await Encryptor.EncryptStreamToStreamAsync(streamWithZippedSetupFiles, outputStream, cancellationToken);

            return applicationInfo;
        }

        /// <summary>
        /// Decrypt an intunewin file to a folder
        /// </summary>
        /// <param name="packageFile">Full path of intunewin file</param>
        /// <param name="outputFolder">Output folder</param>
        /// <param name="cancellationToken">(optional) Cancellation token</param>
        /// <returns><see cref="ApplicationInfo"/> contained in the package</returns>
        public async Task<ApplicationInfo?> Unpack(
            string packageFile,
            string outputFolder,
            CancellationToken cancellationToken = default)
        {
            CheckUnpackParamsOrThrow(packageFile, outputFolder);
            _logger.LogInformation("Unpacking intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                // Unzip file packageFile to tempFolder
                _logger.LogDebug("Unzipping {PackageFile} to {TempFolder}", packageFile, tempFolder);
                using (FileStream fileStream = new FileStream(packageFile, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await Zipper.UnzipStreamAsync(fileStream, tempFolder, cancellationToken);
                }
                _logger.LogDebug("Unzipped {PackageFile} to {TempFolder}", packageFile, tempFolder);
                // Read metadata file
                var metadataFile = Path.Combine(tempFolder, "IntuneWinPackage", "Metadata", "Detection.xml");
                _logger.LogDebug("Reading metadata file {MetadataFile}", metadataFile);

                ApplicationInfo? applicationInfo;
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
                var encryptedPackage = Path.Combine(tempFolder, "IntuneWinPackage", "Contents", applicationInfo.FileName!);
                _logger.LogDebug("Decrypting {EncryptedPackage} to {OutputFolder}", encryptedPackage, outputFolder);
                using (FileStream encryptedFileStream = new FileStream(encryptedPackage, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: Encryptor.DefaultBufferSize, useAsync: true))
                using (Stream decryptedStream = await Encryptor.DecryptStreamAsync(encryptedFileStream, applicationInfo.EncryptionInfo.EncryptionKey!, applicationInfo.EncryptionInfo.MacKey!, cancellationToken))
                {
                    await Zipper.UnzipStreamAsync(decryptedStream, outputFolder, cancellationToken);
                    decryptedStream.Close();
                    encryptedFileStream.Close();
                    _logger.LogInformation("Unpacked intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
                }

                Directory.Delete(tempFolder, true);

                return applicationInfo;

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unpacking intunewin at {PackageFile} to {OutputFolder}", packageFile, outputFolder);
                throw;
            }
        }

        /// <summary>
        /// Decrypt and unpack a stream to a folder
        /// </summary>
        /// <param name="encryptedStream">Encrypted file stream, needs CanRead and CanSeek. Hash (32 bytes) + IV (16 bytes) + encrypted data</param>
        /// <param name="outputFolder">Folder to extract the contents to</param>
        /// <param name="encryptionInfo">Encryption info as generated upon encrypting. Only the `EncryptionKey` and the `MacKey` are required.</param>
        /// <param name="cancellationToken">Cancellation token if your want to be able to cancel this request.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public async Task<bool> DecryptAndUnpackStreamToFolder(Stream encryptedStream, string outputFolder, FileEncryptionInfo encryptionInfo, CancellationToken cancellationToken = default)
        {
            if (encryptedStream == null)
                throw new ArgumentNullException(nameof(encryptedStream));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(nameof(outputFolder));
            if (encryptionInfo == null)
                throw new ArgumentNullException(nameof(encryptionInfo));
            if (!encryptedStream.CanRead || !encryptedStream.CanSeek)
                throw new ArgumentException("Stream can not be read or seeked in", nameof(encryptedStream));
            if (encryptedStream.Length < 49)
                throw new InvalidDataException("Stream is too short to contain a valid encryption header");
            _logger.LogDebug("Decrypting stream to {OutputFolder}", outputFolder);

            long streamLength = encryptedStream.Length;

            try
            {
                using (Stream decryptedStream = await Encryptor.DecryptStreamAsync(encryptedStream, encryptionInfo.EncryptionKey!, encryptionInfo.MacKey!, cancellationToken))
                {
                    await Zipper.UnzipStreamAsync(decryptedStream, outputFolder, cancellationToken);
                    //decryptedStream.Close();
                    _logger.LogInformation("Unpacked stream of size {StreamSize} to {OutputFolder}", streamLength, outputFolder);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error decrypting stream to folder {OutputFolder}", outputFolder);
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
            if (!setupFile.StartsWith(setupFolder, StringComparison.OrdinalIgnoreCase))
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
            if (outputFolder.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Output folder '{0}' can not be a subfolder of source folder '{1}'", outputFolder, folder));
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
