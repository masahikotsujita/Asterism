using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Asterism {

    class MSBuild {

        enum Version {
            VS2012,
            VS2013,
            VS2015,
            VS2017,
            VS2019
        }

        private static readonly String MSBUILD_PATH_2012                = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2015                = @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2017_COMMUNITY      = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2017_PROFESSIONAL   = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2017_ENTERPRISE     = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2019_COMMUNITY      = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2019_PROFESSIONAL   = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe";
        private static readonly String MSBUILD_PATH_2019_ENTERPRISE     = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe";

        public static int Build(String slnFilePath, Action<String> outputHandler) {
            if (GetVersionForSolution(slnFilePath) is Version version) {
                String msBuildPath;
                if ((msBuildPath = GetAvailableMSBuildPathForVersion(version)) != null) {
                    var process = new Process() {
                        StartInfo = new ProcessStartInfo(msBuildPath) {
                            Arguments = $"\"{slnFilePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
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
                else {
                    return -1;
                }
            }
            else {
                return -1;
            }
        }

        static String GetAvailableMSBuildPathForVersion(Version version) {
            switch (version) {
            case Version.VS2012:
                return File.Exists(MSBUILD_PATH_2012) ? MSBUILD_PATH_2012 : null;
            case Version.VS2015:
                return File.Exists(MSBUILD_PATH_2015) ? MSBUILD_PATH_2015 : null;
            case Version.VS2017:
                return
                    File.Exists(MSBUILD_PATH_2017_ENTERPRISE) ? MSBUILD_PATH_2017_ENTERPRISE :
                    File.Exists(MSBUILD_PATH_2017_PROFESSIONAL) ? MSBUILD_PATH_2017_PROFESSIONAL :
                    File.Exists(MSBUILD_PATH_2017_COMMUNITY) ? MSBUILD_PATH_2017_COMMUNITY : null;
            case Version.VS2019:
                return
                    File.Exists(MSBUILD_PATH_2019_ENTERPRISE) ? MSBUILD_PATH_2019_ENTERPRISE :
                    File.Exists(MSBUILD_PATH_2019_PROFESSIONAL) ? MSBUILD_PATH_2019_PROFESSIONAL :
                    File.Exists(MSBUILD_PATH_2019_COMMUNITY) ? MSBUILD_PATH_2019_COMMUNITY : null;
            }
            return null;
        }

        static Version? GetVersionForSolution(string slnFilePath) {
            Regex regex = new Regex(@"VisualStudioVersion\s*=\s*(\d+)\.\d+\.\d+\.\d+");
            StreamReader reader = File.OpenText(slnFilePath);
            String line;
            while ((line = reader.ReadLine()) != null) {
                Match match = regex.Match(line);
                if (match.Success) {
                    int majorVersionNumber = int.Parse(match.Groups[1].Value);
                    switch (majorVersionNumber) {
                    case 12:
                        return Version.VS2012;
                    case 13:
                        return Version.VS2013;
                    case 14:
                        return Version.VS2015;
                    case 15:
                        return Version.VS2017;
                    case 16:
                        return Version.VS2019;
                    default:
                        return null;
                    }
                }
            }
            return null;
        }
        
    }

}
