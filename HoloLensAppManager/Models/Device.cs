using HoloLensAppManager.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;

namespace HoloLensAppManager.Models
{
    class Device
    {
        public static bool IsHoloLens
        {
            get
            {
                return AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Holographic";
            }
        }
    }
}
