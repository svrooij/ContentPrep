# SvRooij.ContentPrep.Cmdlet

This is a PowerShell module that contains several cmdlets to create and Decrypt `.intunewin` packages.
Normally you would need the closed source [Microsoft Win32-content-prep-tool](https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool) to create these packages, but with this module you can do it with PowerShell.

Since the process is not documented anywhere not all features are supported yet, but the most important ones are.

> **Warning**: This is not a replacement for the Microsoft tool, it is a re-implementation of the tool based upon public available information. It is not feature complete and it might not work for your use case. If you need a tool that works, use the Microsoft tool. This library is provided as-is, without any warranty or support.

## Installation

**Note:** this is not yet published but will be soon.

You can install the module from the [PowerShell Gallery](https://www.powershellgallery.com/packages/SvRooij.ContentPrep.Cmdlet/).

```powershell
Install-Module -Name SvRooij.ContentPrep.Cmdlet
```

## Features

- [x] Create `.intunewin` packages
  - [x] Super fast because of asynchronous processing
  - [ ] Support for catalog files (what is this, need help?)
  - [ ] Support for reading MSI details from MSI installers (is there any cross platform way to do this?)
- [x] Decrypt `.intunewin` packages

## Creating `.intunewin` packages

```powershell
New-IntuneWinPackage -SourcePath "C:\Path\To\Source" -DestinationPath "C:\Path\To\Destination" -SetupFile "setup.exe"
```

Command: `New-IntuneWinPackage`

| Parameter | Description | Sample |
| --- | --- | --- |
| `-SourcePath` | The path to the source folder | `C:\Path\To\Source` |
| `-DestinationPath` | The path to the destination folder | `C:\Path\To\Destination` |
| `-SetupFile` | Setup file inside SourcePath | `setup.exe` |
| `-Verbose` | Show verbose output | |
| `-Debug` | Show debug output | |

> The `-DestinationPath` must not be inside the `-SourcePath` folder.

## Decrypting `.intunewin` packages

```powershell
Unlock-IntuneWinPackage -SourcePath "C:\Path\To\Source" -DestinationPath "C:\Path\To\Destination"
```

Command: `Unlock-IntuneWinPackage`

| Parameter | Description | Sample |
| --- | --- | --- |
| `-SourcePath` | The path to the source folder | `C:\Path\To\Source` |
| `-DestinationPath` | The path to the destination folder | `C:\Path\To\Destination` |
| `-Verbose` | Show verbose output | |
| `-Debug` | Show debug output | |

## Library

This PowerShell module is just an easy way to use the [SvRooij.ContentPrep](https://github.com/svrooij/ContentPrep) library. Want to integrate this code in your own project? Check out the library instead.

## Async code

This module and the library is build with async code. That means that logging failed because PowerShell forces the output to be synchronous. This is a [known issue](https://github.com/PowerShell/PowerShell/issues/10452) without a solution in sight.

To overcome this problem this module uses the [ThreadAffinitiveSynchronizationContext](https://github.com/NTTLimitedRD/OctopusDeploy.Powershell/blob/7653993ffbf3ddfc7381e1196dbaa6fdf43cd982/OctopusDeploy.Powershell/ThreadAffinitiveSynchronizationContext.cs) from [OctopusDeploy.Powershell](https://github.com/NTTLimitedRD/OctopusDeploy.Powershell), which is [licensed under MIT](https://github.com/NTTLimitedRD/OctopusDeploy.Powershell/blob/master/LICENSE). If I did something wrong with the license, please let me know.

## Support

If you like my work, becoming a [GitHub Sponsor](https://github.com/sponsors/svrooij) is the best way to support me.
