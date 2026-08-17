using OmniDown.Models.Settings;
using OmniDown.Services.Storage;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OmniDown.Services.Engine;

public static class Aria2EngineStore
{
    public static string GetImportedEnginePath(Aria2EngineType engineType)
    {
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string fileName = engineType switch
        {
            Aria2EngineType.Aria2Next => "aria2-next.exe",
            Aria2EngineType.Custom => "aria2-custom.exe",
            _ => "aria2c.exe"
        };

        return Path.Combine(AppPaths.EngineDirectory, $"win-{architecture}", fileName);
    }

    public static string Import(string sourcePath, Aria2EngineType engineType)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) ||
            !string.Equals(Path.GetExtension(sourcePath), ".exe", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("请选择有效的 aria2 兼容可执行文件。", sourcePath);
        }

        string targetPath = GetImportedEnginePath(engineType);
        string sourceFullPath = Path.GetFullPath(sourcePath);
        string targetFullPath = Path.GetFullPath(targetPath);
        if (string.Equals(sourceFullPath, targetFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return targetFullPath;
        }

        string targetDirectory = Path.GetDirectoryName(targetFullPath)!;
        Directory.CreateDirectory(targetDirectory);

        string temporaryPath = targetFullPath + ".importing";
        try
        {
            File.Copy(sourceFullPath, temporaryPath, overwrite: true);
            File.Move(temporaryPath, targetFullPath, overwrite: true);
            return targetFullPath;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // A failed cleanup must not hide the original import error.
            }
        }
    }
}
