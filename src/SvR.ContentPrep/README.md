# SvRooij.ContentPrep

A library to create and decrypt Intune content packages `.intunewin`.

> **Warning**: This is not a replacement for the Microsoft tool, it is a re-implementation of the tool based upon public available information. It is not feature complete and it might not work for your use case. If you need a tool that works, use the Microsoft tool. This library is provided as-is, without any warranty or support.

## Installation

```powershell
dotnet add package SvRooij.ContentPrep
```

## Usage

### Create a package

```csharp
// Add ILogger optional, or register through DI
var logger = new NullLogger();
var packager = new SvRooij.ContentPrep.Packager(logger);
await packager.CreatePackage("C:\\path\\to\\source", "setup.exe", "C:\path\\to\\destination", null, cancellationToken);
```

### Decrypt a package

```csharp
// Add ILogger optional, or register through DI
var logger = new NullLogger();
var packager = new SvRooij.ContentPrep.Packager(logger);
await packager.Unpack("C:\\path\\to\\source.intunewin", "C:\path\\to\\destination", cancellationToken);
```

## Support

If you like my work, becoming a [GitHub Sponsor](https://github.com/sponsors/svrooij) is the best way to support me.
