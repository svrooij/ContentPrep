﻿using System;
using System.IO;
using System.Management.Automation;
using System.Threading;
using SvRooij.ContentPrep;
namespace SvR.ContentPrep.Cmdlet
{
    /// <summary>
    /// <para type="synopsis">Decrypt an IntuneWin package</para>
    /// <para type="description">Decrypt IntuneWin files, based on this post https://svrooij.io/2023/10/09/decrypting-intunewin-files/</para>
    /// <para type="link" uri="https://github.com/svrooij/ContentPrep/blob/main/src/SvR.ContentPrep.Cmdlet/README.md">Documentation</para>
    /// </summary>
    /// <example>
    /// <code>
    /// Unlock-IntuneWinPackage -SourceFile "C:\Temp\Source\MyApp.intunewin" -DestinationPath "C:\Temp\Destination"
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Unlock, "IntuneWinPackage", HelpUri = "https://github.com/svrooij/ContentPrep/blob/main/src/SvR.ContentPrep.Cmdlet/README.md")]
    public class UnlockIntuneWinPackageCommand : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The location of the .intunewin file</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The location of the .intunewin file")]
        public string SourceFile { get; set; }

        /// <summary>
        /// <para type="description">Destination folder</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Destination folder")]
        public string DestinationPath { get; set; }

        private Packager packager;
        private PowerShellLogger<Packager> logger;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            WriteVerbose("Begin unlocking package");
            logger = new PowerShellLogger<Packager>(this);
            packager = new Packager(logger);
        }

        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            try
            {
                SourceFile = SourceFile?.TrimEnd();
                if (!Path.IsPathRooted(SourceFile))
                    SourceFile = Path.Combine(Environment.CurrentDirectory, SourceFile);
                DestinationPath = DestinationPath?.TrimEnd();
                if (!Path.IsPathRooted(DestinationPath))
                    DestinationPath = Path.Combine(Environment.CurrentDirectory, DestinationPath);
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
                ThreadAffinitiveSynchronizationContext.RunSynchronized(() => packager.Unpack(SourceFile, DestinationPath, cancellationToken: cancellationTokenSource.Token));
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(ex, "2", ErrorCategory.InvalidOperation, null));
            }
        }

        /// <inheritdoc/>
        protected override void EndProcessing()
        {
            packager = null;
            logger = null;
            cancellationTokenSource.Dispose();
        }

        /// <inheritdoc/>
        protected override void StopProcessing()
        {
            cancellationTokenSource.Cancel();
            WriteVerbose("Stopping processing");
            base.StopProcessing();
        }
    }
}