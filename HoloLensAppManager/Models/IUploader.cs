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

        List<AppInfo> searchAppInfoList();
        List<AppInfo> appInfoList();
        Task<bool> Upload(Application application);
        Task<List<AppInfo>> GetAppInfoListAsync(string searchKeywords = null);
        Task<Application> Download(string appName, string version, bool useCache = true);
    }
}
