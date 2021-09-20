using System;
using System.Diagnostics;

namespace Asterism {

internal class FileUtility {
    public static int XCopy(string source, string destination, string workingDirectory, Action<string> outputHandler) {
        var process = new Process {
            StartInfo = new ProcessStartInfo("xcopy") {
                Arguments = $"\"{source}\" \"{destination}\" /Y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };
        process.OutputDataReceived += (sender, args) => { outputHandler(args.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static string ReplacePathSeparatorsForWindows(string path) {
        return path.Replace('/', '\\');
    }

    public static string GetRelativePath(string from, string to) {
        var fromUri = new Uri(from);
        var toUri = new Uri(to);
        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = relativeUri.ToString();
        relativePath = relativePath.Replace('/', '\\');
        return relativePath;
    }
}

}