using HoloLensAppManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace HoloLensAppManager.Helpers
{
    static class SupportedArchitectureHelper
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
            return SupportedArchitectureType.None;
        }
    }
}
