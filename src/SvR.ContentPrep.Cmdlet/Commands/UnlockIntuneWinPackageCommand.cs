using System.IO;
using System.Management.Automation;

namespace SvR.ContentPrep.Cmdlet
{
    [Cmdlet(VerbsCommon.Unlock, "IntuneWinPackage")]
    public class UnlockIntuneWinPackageCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The location of the .intunewin file")]
        public string SourceFile { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Destination folder")]
        public string DestinationPath { get; set; }

        private Packager packager;
        private PowerShellLogger<Packager>? logger;

        protected override void BeginProcessing()
        {
            WriteVerbose("Begin unlocking package");
            logger = new PowerShellLogger<Packager>(this);
            packager = new Packager(logger);
        }

        protected override void ProcessRecord()
        {
            try
            {
                if (!File.Exists(SourceFile))
                {
                    WriteError(new ErrorRecord(new FileNotFoundException("File not found", SourceFile), "1", ErrorCategory.InvalidOperation, null));
                    return;
                }
                if (!Directory.Exists(DestinationPath))
                {
                    WriteVerbose($"Creating destination folder {DestinationPath}");
                    Directory.CreateDirectory(DestinationPath);
                }
                ThreadAffinitiveSynchronizationContext.RunSynchronized(() => packager.Unpack(SourceFile, DestinationPath));
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(ex, "2", ErrorCategory.InvalidOperation, null));
            }
        }

        protected override void EndProcessing()
        {
            packager = null;
            logger = null;
        }
    }
}