using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using FluentAssertions;

namespace SvRooij.ContentPrep.Tests;

[TestClass]
public partial class PackagerTests
{
    private const string setupFileName = "setup.exe";
    [TestMethod]
    [DataRow(2, 10000, 2000L)]
    [DataRow(10, 30000, 3000L)]
    [DataRow(100, 60000, 8000L)]
    public async Task Packager_CreatePackage_Succeeds(int sizeInMb, int millisecondsDelay, long expectedPackageMs)
    {
        // Create Timeout in case something goes wrong
        var cts = new CancellationTokenSource(millisecondsDelay);
        var setupFolder = TestHelper.GetTestFolder();
        var setupFile = await TestHelper.GenerateTempFileInFolder(setupFolder, setupFileName, sizeInMb, cts.Token);
        var outputDirectory = TestHelper.GetTestFolder();
        var outputFile = Path.Combine(outputDirectory, "setup.intunewin");

        var packager = new Packager();

        try
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await packager.CreatePackage(setupFolder, setupFile, outputDirectory, null, cts.Token);
            stopwatch.Stop();
            var fileExists = File.Exists(outputFile);
            Assert.IsTrue(fileExists, "Output package not created");
            var filesize = new FileInfo(outputFile).Length;
            var fileGreaterThenSetup = filesize > (sizeInMb * 1024L * 1024L);
            Assert.IsTrue(fileGreaterThenSetup, "Output package is not bigger then the setup file");
            var fastEnough = (expectedPackageMs > stopwatch.ElapsedMilliseconds);
            Assert.IsTrue(fastEnough, $"It took {stopwatch.ElapsedMilliseconds}ms to package instead of {expectedPackageMs}");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail("Expected no timeout but got one");
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(setupFolder);
            TestHelper.RemoveFolderIfExists(outputDirectory);
        }

    }

    [TestMethod]
    [DataRow(2, 10000, 2000L)]
    [DataRow(10, 30000, 3000L)]
    [DataRow(100, 60000, 10000L)]
    public async Task Packager_CreateUploadablePackage_Succeeds(int sizeInMb, int millisecondsDelay, long expectedPackageMs)
    {
        // Create Timeout in case something goes wrong
        var cts = new CancellationTokenSource(millisecondsDelay);
        var setupFolder = TestHelper.GetTestFolder();
        var setupFile = await TestHelper.GenerateTempFileInFolder(setupFolder, setupFileName, sizeInMb, cts.Token);

        var setupFileInfo = new FileInfo(setupFile);

        var outputDirectory = TestHelper.GetTestFolder();
        var outputFile = Path.Combine(outputDirectory, "setup.intunewin");
        var intuneWinStream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 8192, true);
        var packager = new Packager();

        try
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var info = await packager.CreateUploadablePackage(setupFolder, intuneWinStream, new Models.ApplicationDetails { Name = "Test", SetupFile = setupFileName }, cts.Token);
            stopwatch.Stop();
            var fileExists = File.Exists(outputFile);
            fileExists.Should().BeTrue("output package should exist");
            var outputFilesize = new FileInfo(outputFile).Length;
            info!.UnencryptedContentSize.Should().BeLessThan(outputFilesize, "output package should have encryption data attached");
            expectedPackageMs.Should().BeGreaterThan(stopwatch.ElapsedMilliseconds, $"It took {stopwatch.ElapsedMilliseconds}ms to package instead of {expectedPackageMs}");

        }
        catch (TaskCanceledException)
        {
            Assert.Fail("Expected no timeout but got one");
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(setupFolder);
            //TestHelper.RemoveFolderIfExists(zipFolder);
            TestHelper.RemoveFolderIfExists(outputDirectory);
        }

    }

    [TestMethod]
    public async Task Packager_Unpack_Succeeds()
    {
        var cts = new CancellationTokenSource(60000);
        var setupFolder = TestHelper.GetTestFolder();
        var setupFile = await TestHelper.GenerateTempFileInFolder(setupFolder, setupFileName, 10, cts.Token);
        var hasher = SHA256.Create();
        using var fs = new FileStream(setupFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192,
            useAsync: true);
        var hash = await hasher.ComputeHashAsync(fs, cts.Token);
        var originalFilesize = new FileInfo(setupFile).Length;
        var packageDirectory = TestHelper.GetTestFolder();
        var outputDirectory = TestHelper.GetTestFolder();

        var packager = new Packager();

        try
        {
            // Create a fake package
            await packager.CreatePackage(setupFolder, setupFile, packageDirectory, null, cts.Token);

            var packageFile = Path.Combine(packageDirectory, "setup.intunewin");

            await packager.Unpack(packageFile, outputDirectory, cts.Token);

            var unpackedSetup = Path.Combine(outputDirectory, setupFileName);
            var unpackedFilesize = new FileInfo(unpackedSetup).Length;

            Assert.AreEqual(originalFilesize, unpackedFilesize, "Original and unpacked setup are not the same size");
            using var outputFs = new FileStream(unpackedSetup, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192,
                useAsync: true);
            var outputHash = await hasher.ComputeHashAsync(outputFs, cts.Token);
            outputHash.Should().BeEquivalentTo(hash, "hashes should match");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail("Expected no timeout but got one");
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(packageDirectory);
            TestHelper.RemoveFolderIfExists(outputDirectory);
            TestHelper.RemoveFolderIfExists(setupFolder);
            hasher.Dispose();
        }


    }
#if NET6_0_OR_GREATER
    [TestMethod]
#endif
    public async Task Packager_DecryptAndUnpack_Succeeds()
    {
        var cts = new CancellationTokenSource(60000);
        var setupFolder = TestHelper.GetTestFolder();
        var setupFile = await TestHelper.GenerateTempFileInFolder(setupFolder, setupFileName, 112, cts.Token);
        var hasher = SHA256.Create();
        using var fs = new FileStream(setupFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192,
            useAsync: true);
        var hash = await hasher.ComputeHashAsync(fs, cts.Token);

        var zipFolder = TestHelper.GetTestFolder();
        var zipFilename = Path.Combine(zipFolder, "intunewin.tmp");
#if NET6_0_OR_GREATER
        var zipStream = new FileStream(zipFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 8192, true);
        ZipFile.CreateFromDirectory(setupFolder, zipStream, CompressionLevel.NoCompression, false);
#else
        ZipFile.CreateFromDirectory(setupFolder, zipFilename, CompressionLevel.NoCompression, false);
        await Task.Delay(1000, cts.Token);
        var zipStream = new FileStream(zipFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 8192, false);
#endif

        long zipSize = zipStream.Length;

        var originalFilesize = new FileInfo(setupFile).Length;
        var outputDirectory = TestHelper.GetTestFolder();
        var outputFile = Path.Combine(outputDirectory, "setup.intunewin");
        var intuneWinStream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 8192, true);
        zipStream.Seek(0, SeekOrigin.Begin);
        var packager = new Packager();

        // Create a fake package (this is tested in another test, so we can skip that)
        var info = await packager.CreateUploadablePackage(zipStream, intuneWinStream, new Models.ApplicationDetails { Name = "Test", SetupFile = setupFileName }, cts.Token);
        await intuneWinStream.FlushAsync(cts.Token);
#if NET6_0_OR_GREATER
        await intuneWinStream.DisposeAsync();
#else
        intuneWinStream.Dispose();
#endif
        intuneWinStream = null;
        await Task.Delay(1000, cts.Token);

        var packageStream = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 8192, true);
        info.Should().NotBeNull("info should not be null");
        info!.EncryptionInfo.Should().NotBeNull("encryption info should not be null");

        try
        {
            // Start actual test
            await packager.DecryptAndUnpackStreamToFolder(packageStream, outputDirectory, info!.EncryptionInfo!, cts.Token);

            var unpackedSetup = Path.Combine(outputDirectory, setupFileName);
            var unpackedFilesize = new FileInfo(unpackedSetup).Length;

            unpackedFilesize.Should().Be(originalFilesize, "the file should exactly match");
            using var outputFs = new FileStream(unpackedSetup, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                useAsync: true);
            var outputHash = await hasher.ComputeHashAsync(outputFs, cts.Token);
            outputHash.Should().BeEquivalentTo(hash, "hashes should match");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail("Expected no timeout but got one");
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(outputDirectory);
            TestHelper.RemoveFolderIfExists(zipFolder);
            TestHelper.RemoveFolderIfExists(setupFolder);
            hasher.Dispose();
        }


    }

}