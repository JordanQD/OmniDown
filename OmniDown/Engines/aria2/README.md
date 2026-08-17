# aria2 sidecar

Release packages do not bundle aria2 executables. This directory may contain
developer-only engines for local Debug/F5 builds.

Recommended Windows layout:

```text
Engines/
  aria2/
    win-x64/
      aria2c.exe
    win-arm64/
      aria2c.exe
```

Executable files in this tree are ignored by Git, copied only in Debug builds,
and never copied to publish output. Users import their own engine in Settings;
OmniDown stores that copy under the package-managed
`%LOCALAPPDATA%\Packages\<package-family>\LocalState\engines\aria2`, so a
normal MSIX uninstall removes it.
