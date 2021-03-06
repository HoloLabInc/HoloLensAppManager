﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HoloLensAppManager.ViewModels;
using Windows.System;

namespace HoloLensAppManager.Models
{
    public enum DownloadErrorType
    {
        NoError = 0,
        UnknownError,
        NetworkError,
        NotSupportedArchitecture
    }

    public enum UploadStatusType
    {
        UnknownError = 0,
        NewlyUploaded,
        Updated,
        NetworkError
    }

    interface IUploader
    {
        Task<(AppInfo appInfo, UploadStatusType status)> Upload(Application application);

        Task<List<AppInfo>> GetAppInfoListAsync();

        Task<(Application app, DownloadErrorType error)> Download(string appName, string version, SupportedArchitectureType desirableArchitecture, bool useCache = true);
    }
}
