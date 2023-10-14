using System.IO;
using System.Management.Automation;

namespace SvR.ContentPrep.Cmdlet
{
    [Cmdlet(VerbsCommon.New, "IntuneWinPackage")]
    public class NewIntuneWinPackageCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The directory containing all the installation files")]
        public string SourcePath { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The main setupfile in the source directory")]
        public string SetupFile { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 2,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Destination folder")]
        public string DestinationPath { get; set; }

        private Packager packager;
        private PowerShellLogger<Packager>? logger;

        protected override void BeginProcessing()
        {
            WriteVerbose("Begin creating package");
            logger = new PowerShellLogger<Packager>(this);
            packager = new Packager(logger);
        }

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
                ThreadAffinitiveSynchronizationContext.RunSynchronized(() => packager.CreatePackage(SourcePath, setupFile, DestinationPath));
                //packager.CreatePackage(SourcePath, setupFile, DestinationPath).GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(ex, "1", ErrorCategory.InvalidOperation, null));
            }
        }

        protected override void EndProcessing()
        {
            packager = null;
            logger = null;
        }
    }
}