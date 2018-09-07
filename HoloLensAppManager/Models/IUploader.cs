using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloLensAppManager.Models
{
    interface IUploader
    {
        //bool UploadPackageInfo(AppPackageInfo package);
        //bool UploadFile(string name, StoredFile file);
        Task<bool> Upload(Application application);
    }
}
