namespace SvRooij.ContentPrep.Tests;

[TestClass]
public partial class PackagerTests
{
    [TestMethod]
    [DataRow(2, 10000)]
    [DataRow(10, 30000)]
    [DataRow(100, 60000)]
    public async Task Packager_CreatePackage_Succeeds(int sizeInMb, int millisecondsDelay)
    {
        // Create Timeout in case something goes wrong
        var cts = new CancellationTokenSource(millisecondsDelay);
        var setupFolder = TestHelper.GetTestFolder();
        var setupFile = await TestHelper.GenerateTempFileInFolder(setupFolder, "setup.exe", sizeInMb, cts.Token);
        var outputDirectory = TestHelper.GetTestFolder();

        var packager = new Packager();

        try
        {
            await packager.CreatePackage(setupFolder, setupFile, outputDirectory, null, cts.Token);
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

}