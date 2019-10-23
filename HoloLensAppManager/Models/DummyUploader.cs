using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoloLensAppManager.Models
{
    public class DummyUploader : IUploader
    {
        public List<AppInfo> appInfoList = new List<AppInfo>();

        public Task<Application> Download(string appName, string version, bool useCache = true)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AppInfo>> GetAppInfoListAsync(bool isSearching = false, string keyword = "")
        {
            if (isSearching)
            {
                return await SearchInAppList(keyword);
            }

            return await MakeInitialAppList();
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

        public async Task<List<AppInfo>> SearchInAppList(string keyword)
        {
            List<AppInfo> newAppInfoList = new List<AppInfo>();
            foreach (var app in appInfoList)
            {
                if (app.Description.Contains(keyword))
                {
                    newAppInfoList.Add(app);
                }
                else if (app.Name.Contains(keyword))
                {
                    newAppInfoList.Add(app);
                }
                else if (app.DeveloperName.Contains(keyword))
                {
                    newAppInfoList.Add(app);
                }
            }

            return newAppInfoList;
        }
    }
}
