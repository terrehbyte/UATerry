# UATerry

An **Unreal Automation Tool (UAT) locator** and command builder designed to
streamline common workflow needs like rebuilding project binaries. It should
be accompanied by easy to launch batch scripts or other automation tooling.

## Usage

Download and place the binary ("**uaterry.exe**") in same directory as the
Unreal Engine project's .uproject file. 

```pwsh
# Re-build project main module and its dependencies
.\uaterry.exe build[LICENSE.md](LICENSE.md)

# Re-build project main module and its dependencies AND stage on Perforce
.\uaterry.exe build -perforce
```

## Building

Requires .NET 8.0 targeting Windows. Restore NuGet packages before building.

## License

[MIT License (c) 2025 Terry Nguyen](LICENSE.md)
