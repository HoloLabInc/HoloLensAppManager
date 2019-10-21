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
            // TODO
            var pattern = @"(_([a-zA-Z0-9]+))+.(appxbundle|msixbundle)$";
            Match m = Regex.Match(appPackageName, pattern, RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return SupportedArchitectureType.None;
            }

            var architecture = SupportedArchitectureType.None;
            foreach (Capture archiCapture in m.Groups[2].Captures)
            {
                switch (archiCapture.Value.ToLower())
                {
                    case "x86":
                        architecture |= SupportedArchitectureType.X86;
                        break;
                    case "x64":
                        architecture |= SupportedArchitectureType.X64;
                        break;
                    case "arm":
                        architecture |= SupportedArchitectureType.Arm;
                        break;
                    case "arm64":
                        architecture |= SupportedArchitectureType.Arm64;
                        break;
                }
            }

            return architecture;
        }
    }
}
