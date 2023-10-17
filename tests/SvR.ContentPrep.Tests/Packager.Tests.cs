namespace SvRooij.ContentPrep.Tests;

[TestClass]
public class PackagerTests
{
    [TestMethod]
    public void CreatePackage_NullFolder_ThrowsArgumentNull()
    {
        // Arrange
        var packager = new Packager();

        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => packager.CreatePackage(null, null, null));
    }

    [TestMethod]
    public void CreatePackage_NullSetupFile_ThrowsArgumentNull()
    {
        // Arrange
        var packager = new Packager();

        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => packager.CreatePackage("test", null, null));
    }

    [TestMethod]
    public void CreatePackage_NullOutput_ThrowsArgumentNull()
    {
        // Arrange
        var packager = new Packager();

        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => packager.CreatePackage("test", "test", null));
    }

    [TestMethod]
    public void CreatePackage_NonExistingFolder_ThrowsDirectoryNotFound()
    {
          // Arrange
        var packager = new Packager();
        var inputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());


        Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => packager.CreatePackage(inputFolder, "test", "test"));
    }

    [TestMethod]
    public void CreatePackage_NonExistingSetup_ThrowsFileNotFound()
    {
        // Arrange
        var packager = new Packager();
        var inputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(inputFolder);
        var setup = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".msi");

        Assert.ThrowsExceptionAsync<FileNotFoundException>(() => packager.CreatePackage(inputFolder, setup, "test"));
        Directory.Delete(inputFolder);
    }

    [TestMethod]
    public void CreatePackage_ExistingSetupWrongFolder_ThrowsArgumentException()
    {
        // Arrange
        var packager = new Packager();
        var inputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(inputFolder);
        var setup = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".msi");
        File.CreateText(setup).Close();

        Assert.ThrowsExceptionAsync<ArgumentException>(() => packager.CreatePackage(inputFolder, setup, "test"));
        Directory.Delete(inputFolder, true);
    }

    [TestMethod]
    public void CreatePackage_NoOutputDir_ThrowsDirectoryNotFound()
    {
        // Arrange
        var packager = new Packager();
        var inputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(inputFolder);
        var setup = Path.Combine(inputFolder, Guid.NewGuid().ToString() + ".msi");
        File.CreateText(setup).Close();

        Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => packager.CreatePackage(inputFolder, setup, outputFolder));
        Directory.Delete(inputFolder, true);
        //Directory.Delete(outputFolder, true);
    }

    [TestMethod]
    public void Unpack_NullPackage_ThrowsArgumentNull()
    {
        // Arrange
        var packager = new Packager();

        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => packager.Unpack(null, null));
    }

    [TestMethod]
    public void Unpack_NullOutput_ThrowsArgumentNull()
    {
        // Arrange
        var packager = new Packager();

        Assert.ThrowsExceptionAsync<ArgumentNullException>(() => packager.Unpack("test", null));
    }

    [TestMethod]
    public void Unpack_NonExistingPackage_ThrowsFileNotFound()
    {
        // Arrange
        var packager = new Packager();
        var package = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".msi");

        Assert.ThrowsExceptionAsync<FileNotFoundException>(() => packager.Unpack(package, "test"));
    }

    [TestMethod]
    public void Unpack_NonExistingOutput_ThrowsDirectoryNotFound()
    {
        var packager = new Packager();
        var package = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".msi");
        File.CreateText(package).Close();
        var outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => packager.Unpack(package, outputFolder));
    }
}