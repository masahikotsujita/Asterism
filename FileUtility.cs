using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Asterism {

    class FileUtility {

        public static int XCopy(String source, String destination, String workingDirectory, Action<String> outputHandler) {
            var process = new Process() {
                StartInfo = new ProcessStartInfo("xcopy") {
                    Arguments = $"\"{source}\" \"{ destination }\" /Y",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };
            process.OutputDataReceived += (object sender, DataReceivedEventArgs args) => {
                outputHandler(args.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return process.ExitCode;
        }

        public static String ReplacePathSeparatorsForWindows(String path) {
            return path.Replace('/', '\\');
        }

        public static String GetRelativePath(String from, String to) {
            var fromUri = new Uri(from);
            var toUri = new Uri(to);
            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = relativeUri.ToString();
            relativePath = relativePath.Replace('/', '\\');
            return relativePath;
        }

    }
    
}
