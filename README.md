# UATerry

An **Unreal Automation Tool (UAT) locator** and command builder designed to
streamline common workflow needs like rebuilding project binaries. It should
be accompanied by easy to launch batch scripts or other automation tooling.

> [!WARNING]  
> This is only tested with numbered engine version identifiers, like "5.2".
> Custom builds that use GUIDs or directory paths are not yet supported.

> [!WARNING]  
> Currently only compiling for Windows.

## Usage

Download and place the binary ("**uaterry.exe**") in same directory as the
Unreal Engine project's .uproject file. 

```bat
REM Re-build project main module and its dependencies for development editor
uaterry.exe build

REM Re-build project main module and its dependencies for development editor AND stage on Perforce
.\uaterry.exe build -perforce
```

Batch files can contain the above logic and support double-click to execute,
allowing for a simple and accessible troubleshooting cmdlet that can be run
when you suspect that binaries need to be refreshed for the development
editor configuration.

A simple batch script that does this is available in the **scripts/** folder.

### uewhere

The support logic for locating UAT is in **uewhere** which can also be
distributed as its own standalone program. It looks for a **.uproject** in the
same  directory and attempts to resolve the corresponding engine installation
path.

```bat
REM Locates the engine installation directory
uewhere.exe
```

The above snippet, if run on a .uproject configured to expect on 5.4, could
write the following path to stdout: `C:\Program Files\Epic Games\UE_5.4`.

## Building

Requires .NET 8.0 targeting Windows. Restore NuGet packages before building.

Both **uaterry** and **uewhere** are designed as heavy single-file applications,
packing  their dependencies into the compiled .EXE directly.

## License

[MIT License (c) 2025 Terry Nguyen](LICENSE.md)
