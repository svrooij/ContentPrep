# SvR.ContentPrep

I felt there was a need for an open-source version of the [Microsoft content preparation tool](https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool), mainly because I wanted to be able to use in [WingetIntune](https://github.com/svrooij/WingetIntune) without having to rely on a closed-source tool that had a dependency upon Windows. I want to run this in the cloud and using closed source executable was not really an option.

> **Warning**: This is not a replacement for the Microsoft tool, it is a re-implementation of the tool based upon public available information. It is not feature complete and it might not work for your use case. If you need a tool that works, use the Microsoft tool. This library is provided as-is, without any warranty or support.

## What does it do?

It allows you to create packages that can be uploaded to Intune and it allows you to decrypt those packages.

## PowerShell module

This repository contains a PowerShell module that can be used to create and decrypt packages. It is available on the [PowerShell Gallery](https://www.powershellgallery.com/packages/SvRooij.ContentPrep.Cmdlet/).

See [PowerShell Documentation](./src//SvR.ContentPrep.Cmdlet/README.md) for more information.

## Library

There is also a library available that can be used in your own C# application. It is available on [NuGet](https://www.nuget.org/packages/SvRooij.ContentPrep/).

See [Library Documentation](./src/SvR.ContentPrep/README.md) for more information.

## More content

I've written a lot of content on Intune, if you want to know more, check out my [blog](https://svrooij.io/tags/intune).

Check out [Winget Intune](https://github.com/svrooij/wingetintune) for more information on my other project to package applications from WinGet for Intune all within seconds.

## Support

If you like my work, becoming a [GitHub Sponsor](https://github.com/sponsors/svrooij) is the best way to support me.
