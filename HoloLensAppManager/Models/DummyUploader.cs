using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoloLensAppManager.Models
{
    class DummyUploader : IUploader
    {
        public Task<Application> Download(string appName, string version, bool useCache = true)
        {
            throw new NotImplementedException();
        }

        public Task<List<AppInfo>> GetAppInfoListAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Upload(Application application)
        {
            throw new NotImplementedException();
        }
    }
}