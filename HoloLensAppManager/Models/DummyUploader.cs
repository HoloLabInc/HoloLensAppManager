using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoloLensAppManager.Models
{
    public class DummyUploader : IUploader
    {
        private IUploader uploader;
        private List<AppInfo> appInfoList = new List<AppInfo>();

        private List<AppInfo> searchAppInfoList_ = new List<AppInfo>();

        List<AppInfo> IUploader.appInfoList()
        {
            return appInfoList;
        }

        List<AppInfo> IUploader.searchAppInfoList()
        {
            return searchAppInfoList_;
        }

        public async Task<List<AppInfo>> GetAppInfoListAsync(string searchKeyword = null)
        {
            if (searchKeyword != null)
            {
                return await MakeSearchAppList();
            }

            return await MakeInitialAppList();
        }

        private async Task<List<AppInfo>> MakeSearchAppList()
        {
            return searchAppInfoList_;
        }

        private async Task<List<AppInfo>> MakeInitialAppList()
        {
            var firstApp = new AppInfo
            {
                Name = "DummyApplication1",
                DeveloperName = "DummyDeveloper1",
                Description = "This is the first dummy application"
            };

            var secondApp = new AppInfo
            {
                Name = "DummyApplication2",
                DeveloperName = "DummyDeveloper2",
                Description = "This is the second dummy application"
            };

            var thirdApp = new AppInfo
            {
                Name = "DummyApplication3",
                DeveloperName = "DummyDeveloper3",
                Description = "This is the third dummy application"
            };

            var fourthApp = new AppInfo
            {
                Name = "DummyApplication4",
                DeveloperName = "DummyDeveloper4",
                Description = "This is the fourth dummy application"
            };

            var fifthApp = new AppInfo
            {
                Name = "DummyApplication5",
                DeveloperName = "DummyDeveloper5",
                Description = "This is the fifth dummy application"
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

        public Task<Application> Download(string appName, string version, bool useCache = true)
        {
            throw new NotImplementedException();
        }
    }
}
