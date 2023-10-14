# SvR.ContentPrep

A library to create and decrypt Intune content packages `.intunewin`.

## Installation

```powershell
dotnet add package SvRooij.ContentPrep
```

## Usage

### Create a package

```csharp
// Add ILogger optional, or register through DI
var packager = new SvR.ContentPrep.Packager(...);
await packager.CreatePackage("C:\\path\\to\\source", "setup.exe", "C:\path\to\destination", null, cancellationToken);
```

### Decrypt a package

```csharp
// Add ILogger optional, or register through DI
var packager = new SvR.ContentPrep.Packager(...);
await packager.Unpack("C:\\path\\to\\source.intunewin", "C:\path\to\destination", cancellationToken);
```

## Support

If you like my work, becoming a [GitHub Sponsor](https://github.com/sponsors/svrooij) is the best way to support me.
