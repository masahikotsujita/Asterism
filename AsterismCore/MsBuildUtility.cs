using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AsterismCore {

public class MsBuildUtility {
    private enum Version {
        VS2012,
        VS2013,
        VS2015,
        VS2017,
        VS2019,
        VS2022
    }

    public static int Build(string slnFilePath, Dictionary<string, string> properties, Action<string> outputHandler) {
        if (GetVersionForSolution(slnFilePath) is Version version) {
            string msBuildPath;
            if ((msBuildPath = GetAvailableMSBuildPathForVersion(version)) != null) {
                var arguments = $"\"{slnFilePath}\"";
                if (properties != null && properties.Count > 0) {
                    arguments += " /property:" + string.Join(";", from property in properties
                                                                  select $"{property.Key}={property.Value}");
                }
                var process = new Process {
                    StartInfo = new ProcessStartInfo(msBuildPath) {
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.OutputDataReceived += (sender, args) => { outputHandler(args.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }
            return -1;
        }
        return -1;
    }

    private static string GetAvailableMSBuildPathForVersion(Version version) {
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
        case Version.VS2022:
            return
                File.Exists(MSBUILD_PATH_2022_ENTERPRISE) ? MSBUILD_PATH_2022_ENTERPRISE :
                File.Exists(MSBUILD_PATH_2022_PROFESSIONAL) ? MSBUILD_PATH_2022_PROFESSIONAL :
                File.Exists(MSBUILD_PATH_2022_COMMUNITY) ? MSBUILD_PATH_2022_COMMUNITY : null;
        }
        return null;
    }

    private static Version? GetVersionForSolution(string slnFilePath) {
        var regex = new Regex(@"VisualStudioVersion\s*=\s*(\d+)\.\d+\.\d+\.\d+");
        var reader = File.OpenText(slnFilePath);
        string line;
        while ((line = reader.ReadLine()) != null) {
            var match = regex.Match(line);
            if (match.Success) {
                var majorVersionNumber = int.Parse(match.Groups[1].Value);
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
                case 17:
                    return Version.VS2022;
                default:
                    return null;
                }
            }
        }
        return null;
    }

    private static readonly string MSBUILD_PATH_2012 = @"C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2015 = @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2017_COMMUNITY = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2017_PROFESSIONAL = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2017_ENTERPRISE = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2019_COMMUNITY = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2019_PROFESSIONAL = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2019_ENTERPRISE = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2022_COMMUNITY = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2022_PROFESSIONAL = @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin\MSBuild.exe";
    private static readonly string MSBUILD_PATH_2022_ENTERPRISE = @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Msbuild\Current\Bin\MSBuild.exe";
}

}