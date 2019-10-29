using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;

namespace HoloLensAppManager.Models
{
    class DummyUploader : IUploader
    {
        public async Task<List<AppInfo>> GetAppInfoListAsync()
        {
            var dummyAppVersion = new HashSet<AppVersion>()
            {
                new AppVersion("1.0.0.0")
            };

            var appInfoList = new List<AppInfo>();

            var firstApp = new AppInfo
            {
                Name = "DummyApplication1",
                DeveloperName = "DummyDeveloper1",
                Description = "This app targets x86",
                SupportedArchitecture = SupportedArchitectureType.X86,
                Versions = dummyAppVersion
            };

            var secondApp = new AppInfo
            {
                Name = "DummyApplication2",
                DeveloperName = "DummyDeveloper2",
                Description = "This app targets arm",
                SupportedArchitecture = SupportedArchitectureType.Arm,
                Versions = dummyAppVersion
            };

            var thirdApp = new AppInfo
            {
                Name = "DummyApplication3",
                DeveloperName = "DummyDeveloper3",
                Description = "This app targets x86 and arm",
                SupportedArchitecture = SupportedArchitectureType.X86 | SupportedArchitectureType.Arm,
                Versions = dummyAppVersion,
                UpdateAt = DateTime.MinValue,
            };

            var fourthApp = new AppInfo
            {
                Name = "DummyApplication4",
                DeveloperName = "DummyDeveloper4",
                Description = "This app targets all",
                SupportedArchitecture = SupportedArchitectureType.X64 | SupportedArchitectureType.X86 | SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64,
                Versions = dummyAppVersion,
                UpdateAt = DateTime.MaxValue,
            };

            var fifthApp = new AppInfo
            {
                Name = "DummyApplication5",
                DeveloperName = "DummyDeveloper5",
                Description = "This app targets all",
                SupportedArchitecture = SupportedArchitectureType.X64 | SupportedArchitectureType.X86 | SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64,
                Versions = dummyAppVersion,
                UpdateAt = DateTime.Today,
            };

            appInfoList.Add(firstApp);
            appInfoList.Add(secondApp);
            appInfoList.Add(thirdApp);
            appInfoList.Add(fourthApp);
            appInfoList.Add(fifthApp);

            return appInfoList;
        }

        public Task<(AppInfo appInfo, UploadStatusType status)> Upload(Application application)
        {
            throw new NotImplementedException();
        }

        public Task<(Application app, DownloadErrorType error)> Download(string appName, string version, SupportedArchitectureType architecture, bool useCache = true)
        {
            throw new NotImplementedException();
        }
    }
}
