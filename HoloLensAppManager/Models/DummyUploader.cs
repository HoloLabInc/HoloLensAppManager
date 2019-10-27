using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;

namespace HoloLensAppManager.Models
{
    class DummyUploader : IUploader
    {
        public Task<Application> Download(string appName, string version, ProcessorArchitecture architecture, bool useCache = true)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AppInfo>> GetAppInfoListAsync()
        {
            var appInfoList = new List<AppInfo>();

            var firstApp = new AppInfo
            {
                Name = "DummyApplication1",
                DeveloperName = "DummyDeveloper1",
                Description = "This app targets x86",
                SupportedArchitecture = SupportedArchitectureType.X86
            };

            var secondApp = new AppInfo
            {
                Name = "DummyApplication2",
                DeveloperName = "DummyDeveloper2",
                Description = "This app targets arm",
                SupportedArchitecture = SupportedArchitectureType.Arm
            };

            var thirdApp = new AppInfo
            {
                Name = "DummyApplication3",
                DeveloperName = "DummyDeveloper3",
                Description = "This app targets x86 and arm",
                SupportedArchitecture = SupportedArchitectureType.X86 | SupportedArchitectureType.Arm

            };

            var fourthApp = new AppInfo
            {
                Name = "DummyApplication4",
                DeveloperName = "DummyDeveloper4",
                Description = "This app targets all",
                SupportedArchitecture = SupportedArchitectureType.X64 | SupportedArchitectureType.X86 | SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64
            };

            var fifthApp = new AppInfo
            {
                Name = "DummyApplication5",
                DeveloperName = "DummyDeveloper5",
                Description = "This app targets all",
                SupportedArchitecture = SupportedArchitectureType.X64 | SupportedArchitectureType.X86 | SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64
            };

            appInfoList.Add(firstApp);
            appInfoList.Add(secondApp);
            appInfoList.Add(thirdApp);
            appInfoList.Add(fourthApp);
            appInfoList.Add(fifthApp);

            return appInfoList;
        }

        public Task<bool> Upload(Application application)
        {
            throw new NotImplementedException();
        }
    }
}
