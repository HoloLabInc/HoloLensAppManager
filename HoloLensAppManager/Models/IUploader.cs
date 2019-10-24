using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoloLensAppManager.ViewModels;

namespace HoloLensAppManager.Models
{
    interface IUploader
    {
        //bool UploadPackageInfo(AppPackageInfo package);
        //bool UploadFile(string name, StoredFile file);
        Task<bool> Upload(Application application);

        Task<List<AppInfo>> GetAppInfoListAsync();

        Task<Application> Download(string appName, string version, bool useCache = true);
    }
}
