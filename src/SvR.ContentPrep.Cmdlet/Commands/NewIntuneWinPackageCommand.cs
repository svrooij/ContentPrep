using System.IO;
using System.Management.Automation;
using SvRooij.ContentPrep;
namespace SvR.ContentPrep.Cmdlet
{
    /// <summary>
    /// Create a new IntuneWin package
    /// </summary>
    /// <remarks>
    /// This is a re-implementation of the IntuneWinAppUtil.exe tool, it's not feature complete use at your own risk.
    /// </remarks>
    [Cmdlet(VerbsCommon.New, "IntuneWinPackage", HelpUri = "https://github.com/svrooij/ContentPrep/blob/main/src/SvR.ContentPrep.Cmdlet/README.md")]
    public class NewIntuneWinPackageCommand : PSCmdlet
    {
        /// <summary>
        /// Directory containing all the installation files
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The directory containing all the installation files")]
        public string SourcePath { get; set; }

        /// <summary>
        /// The main setupfile in the source directory
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The main setupfile in the source directory")]
        public string SetupFile { get; set; }

        /// <summary>
        /// Destination folder
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 2,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Destination folder")]
        public string DestinationPath { get; set; }

        private Packager packager;
        private PowerShellLogger<Packager> logger;

        /// <summary>
        /// 
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteVerbose("Begin creating package");
            logger = new PowerShellLogger<Packager>(this);
            packager = new Packager(logger);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                var setupFile = Path.Combine(SourcePath, SetupFile);
                if (!Directory.Exists(DestinationPath))
                {
                    WriteVerbose($"Creating destination folder {DestinationPath}");
                    Directory.CreateDirectory(DestinationPath);
                }
                WriteVerbose($"Trying to create package for {setupFile}");
                ThreadAffinitiveSynchronizationContext.RunSynchronized(() => 
                    packager.CreatePackage(SourcePath, setupFile, DestinationPath)
                );
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(ex, "1", ErrorCategory.InvalidOperation, null));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            packager = null;
            logger = null;
        }
    }
}