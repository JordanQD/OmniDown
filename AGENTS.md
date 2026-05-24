# AGENTS.md

## Scope

These instructions apply to this repository and are intended to be reusable for similar WinUI 3 / Windows App SDK desktop projects.

## Project Baseline

- Treat the app as a packaged WinUI 3 desktop app unless the project file or manifest clearly says otherwise.
- Prefer stable Windows App SDK releases. Do not upgrade to preview or experimental packages unless the task explicitly requires them.
- Do not upgrade the target .NET version, Windows SDK BuildTools, CommunityToolkit packages, or publishing settings as part of an unrelated feature.
- Keep dependency upgrades narrow: change one dependency family at a time, then build before making behavioral changes.

## Build And Restore

- Use the solution file when one exists:

```powershell
dotnet build OmniDown.slnx
```

- If only restore is needed, use:

```powershell
dotnet restore OmniDown.slnx
```

- A successful build with `0` errors is acceptable even if NuGet emits `NU1900` vulnerability-data warnings caused by a local HTTPS or proxy failure.
- Do not treat `NU1900` as a package compatibility failure unless restore also fails to download required packages.
- If package restore needs the network, first verify whether the issue is NuGet configuration, proxy, DNS, or Windows TLS/Schannel before changing project files.

## NuGet Network Diagnostics

When NuGet cannot load `https://api.nuget.org/v3/index.json`, check these in order:

```powershell
dotnet nuget list source
netsh winhttp show proxy
Test-NetConnection 127.0.0.1 -Port 7890
Test-NetConnection api.nuget.org -Port 443
curl.exe -v --noproxy "*" https://api.nuget.org/v3/index.json
```

If `curl` fails with `schannel: AcquireCredentialsHandle failed: SEC_E_NO_CREDENTIALS`, this is a Windows HTTPS/TLS problem, not a NuGet source problem. Do not rewrite `NuGet.Config` to work around it.

Temporary restore workaround, only when the project already has the required packages cached:

```powershell
dotnet restore OmniDown.slnx -p:NuGetAudit=false
```

Use this only to keep local development moving. Do not permanently disable NuGet audit unless the user asks for that tradeoff.

## WinUI Change Discipline

- Keep WinUI and Windows App SDK changes aligned with Microsoft Learn and official WindowsAppSDK samples.
- Prefer existing project patterns over adding new frameworks or custom infrastructure.
- For app UI, use WinUI controls and theme resources first; add CommunityToolkit controls only when built-in controls do not cover the need.
- For Windows Widgets, do not render WinUI XAML inside the widget. Use the Windows App SDK Widgets provider APIs and Adaptive Card templates.
- Keep widget provider logic thin. Reuse app-owned state snapshots or shared services instead of duplicating download-engine business logic in the widget process.

## Verification Expectations

After dependency or WinUI infrastructure changes:

1. Restore/build the solution.
2. Report warnings separately from errors.
3. If the change affects startup, packaging, protocol activation, tray behavior, or widgets, launch and manually verify that surface when possible.
4. Do not claim a runtime feature is verified from build success alone.

## Git Hygiene

- Do not revert user changes.
- Keep diffs focused on the requested task.
- Before final reporting, inspect `git status --short` and summarize the files changed.
