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

```pwsh
# Re-build project main module and its dependencies
.\uaterry.exe build

# Re-build project main module and its dependencies AND stage on Perforce
.\uaterry.exe build -perforce
```

The support logic for locating UAT is in **uewhere** which can also be
distributed as its own standalone program. It looks for a **.uproject** in the
same  directory and attempts to resolve the corresponding engine installation
path.

```pwsh
# Locates the engine installation directory
.\uaterry.exe
```

The above snippet, if run on a .uproject configured to expect on 5.4, could
write the following path to stdout: `C:\Program Files\Epic Games\UE_5.4`.

## Building

Requires .NET 8.0 targeting Windows. Restore NuGet packages before building.

Both **uaterry** and **uewhere** are designed as heavy single-file applications,
packing  their dependencies into the compiled .EXE directly.

## License

[MIT License (c) 2025 Terry Nguyen](LICENSE.md)
