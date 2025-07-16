@{
    # Script module or binary module file associated with this manifest.
    RootModule = 'SvRooij.ContentPrep.Cmdlet.dll'

    # Version number of this module, replaced by GitHub Action with correct tag.
    ModuleVersion = '0.4.0'

    # ID used to uniquely identify this module.
    GUID = 'a9e2730e-2b06-486c-ace6-8425afb6d64f'

    # Author of this module.
    Author = 'Stephan van Rooij'

    # Company or vendor that produced this module.
    CompanyName = 'Stephan van Rooij'

    Copyright = 'Stephan van Rooij 2023, licensed under GNU GPLv3'

    # Description of this module.
    Description = 'An open-source re-implementation of the ContentPrepTool for Intune Win32 apps.'

    # Minimum version of the Windows PowerShell engine required by this module.
    PowerShellVersion = '5.1'

    # Minimum version of the .NET Framework required by this module.
    DotNetFrameworkVersion = '4.7.2'

    # Processor architecture (None, X86, Amd64) supported by this module.
    # ProcessorArchitecture = 'None'

    # Modules that must be imported into the global environment prior to importing this module.
    # RequiredModules = @()

    # Assemblies that must be loaded prior to importing this module.
    # RequiredAssemblies = @(
    #     "Microsoft.Extensions.Logging.Abstractions.dll",
    #     "SvR.ContentPrep.dll",
    #     "System.Buffers.dll",
    #     "System.Memory.dll",
    #     "System.Numerics.Vectors.dll",
    #     "System.Runtime.CompilerServices.Unsafe.dll"
    # )

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    # ScriptsToProcess = @()

    # Type files (.ps1xml) that are loaded into the session prior to importing this module.
    # TypesToProcess = @()

    # Format files (.ps1xml) that are loaded into the session prior to importing this module.
    # FormatsToProcess = @()

    # Modules to import as nested modules of the module specified in RootModule/ModuleToProcess.
    # NestedModules = @()

    # Functions to export from this module.
    # FunctionsToExport = @()

    # Cmdlets to export from this module.
    CmdletsToExport = @(
        "New-IntuneWinPackage",
        "Unlock-IntuneWinPackage"
    )

    # Variables to export from this module.
    # VariablesToExport = @()

    # Aliases to export from this module.
    # AliasesToExport = @()

    # List of all files included in this module.
    FileList = @(
        "Microsoft.Bcl.AsyncInterfaces.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        "Microsoft.Extensions.Logging.Abstractions.dll",
        "SvRooij.ContentPrep.Cmdlet.dll",
        "SvRooij.ContentPrep.Cmdlet.dll-Help.xml",
        "SvRooij.ContentPrep.Cmdlet.psd1",
        "SvRooij.ContentPrep.Cmdlet.xml",
        "SvRooij.ContentPrep.dll",
        "SvRooij.ContentPrep.xml",
        "System.Buffers.dll",
        "System.Management.Automation.dll",
        "System.Memory.dll",
        "System.Numerics.Vectors.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Threading.Tasks.Extensions.dll"
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess.
    PrivateData = @{
        PSData = @{
            Tags = @('Intune', 'Win32', 'ContentPrep')

            LisenceUri = 'https://github.com/svrooij/ContentPrep/blob/main/LICENSE.txt'
            ProjectUri = 'https://github.com/svrooij/ContentPrep/'
            ReleaseNotes = 'Check-out https://github.com/svrooij/ContentPrep/releases for the current release notes'
        }
    }

    # HelpInfo URI of this module.
    HelpInfoURI = 'https://github.com/svrooij/ContentPrep/blob/main/src/SvR.ContentPrep.Cmdlet/README.md'
}