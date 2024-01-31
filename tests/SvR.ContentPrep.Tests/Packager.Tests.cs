using System.Diagnostics;
using System.Security.Cryptography;

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
        catch (AssertFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail("Expected no exceptions but got {0}", ex.GetType().Name);
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(setupFolder);
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
        await using var fs = new FileStream(setupFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
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
            await using var outputFs = new FileStream(unpackedSetup, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                useAsync: true);
            var outputHash = await hasher.ComputeHashAsync(outputFs, cts.Token);

            var hashesAreEqual = TestHelper.CompareHashes(hash, outputHash);
            Assert.IsTrue(hashesAreEqual, "Hashes don't match");
        }
        catch (AssertFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail("Expected no exceptions but got {0}", ex.GetType().Name);
        }
        finally
        {
            TestHelper.RemoveFolderIfExists(packageDirectory);
            TestHelper.RemoveFolderIfExists(outputDirectory);
            TestHelper.RemoveFolderIfExists(setupFolder);
            hasher.Dispose();
        }


    }

}