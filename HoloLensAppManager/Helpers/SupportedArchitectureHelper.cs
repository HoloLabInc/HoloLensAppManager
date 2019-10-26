using HoloLensAppManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace HoloLensAppManager.Helpers
{
    static public class SupportedArchitectureHelper
    {
        static public SupportedArchitectureType GetSupportedArchitectureFromAppPackage(StorageFile appFile)
        {
            if(appFile == null)
            {
                return SupportedArchitectureType.None;
            }
            return GetSupportedArchitectureFromAppPackage(appFile.Name);
        }

        static public SupportedArchitectureType GetSupportedArchitectureFromAppPackage(string appPackageName)
        {
            var pattern = @"(_([a-zA-Z0-9]+))+.(appxbundle|msixbundle)$";
            Match m = Regex.Match(appPackageName, pattern, RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return SupportedArchitectureType.None;
            }

            var supportedArchitecture = SupportedArchitectureType.None;
            foreach (Capture archiCapture in m.Groups[2].Captures)
            {
                var architecture = StringToSupportedArchitectureType(archiCapture.Value);

                supportedArchitecture |= architecture;
            }

            return supportedArchitecture;
        }

        static public bool IsValidArchitecture(string name)
        {
            foreach (var architecture in Enum.GetNames(typeof(SupportedArchitectureType))) {
                if(architecture.ToLower() == name.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        static public SupportedArchitectureType StringToSupportedArchitectureType(string architecture)
        {
            switch (architecture.ToLower())
            {
                case "x86":
                    return SupportedArchitectureType.X86;
                case "x64":
                    return SupportedArchitectureType.X64;
                case "arm":
                    return SupportedArchitectureType.Arm;
                case "arm64":
                    return SupportedArchitectureType.Arm64;
            }
            return SupportedArchitectureType.None;
        }
    }
}
