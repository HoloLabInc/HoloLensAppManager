using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using HoloLensAppManager.Helpers;
using HoloLensAppManager.Models;
using HoloLensAppManager.Views;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using HoloLensAppManager.Services;

namespace HoloLensAppManager.ViewModels
{
    public class UploadViewModel : Observable
    {
        private IUploader uploader;

        static int? StringToInt(string value)
        {
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out int number))
            {
                return null;
            }
            else
            {
                return number;
            }
        }

        private string developerName;
        public string DeveloperName
        {
            get { return developerName; }
            set
            {
                this.Set(ref this.developerName, value);
                localSettings.Values[DeveloperSettingKey] = value;
            }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set {
                this.Set(ref this.name, value);
            }
        }

        private StorageFile appPackage;
        public StorageFile AppPackage
        {
            get { return appPackage; }
            set {
                this.Set(ref this.appPackage, value);
                UpdateProperty();
            }
        }

        private ObservableCollection<StorageFile> dependenciesFiles = new ObservableCollection<StorageFile>();

        public ObservableCollection<StorageFile> DependenciesFiles
        {
            get { return dependenciesFiles; }
            //set { this.Set(ref this.dependenciesFiles, value); }
        }

        public bool DependenciesFilesExist
        {
            get
            {
                return dependenciesFiles.Count != 0;
            }
        }

        public bool AppPackageExists
        {
            get
            {
                return appPackage != null;
            }
        }

        private string successMessage;
        public string SuccessMessage
        {
            get { return successMessage; }
            set
            {
                this.Set(ref this.successMessage, value);
            }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                this.Set(ref this.errorMessage, value);
            }
        }

        bool uploading = false;
        public bool Uploading
        {
            get { return uploading; }
            set
            {
                this.Set(ref this.uploading, value);
                ((RelayCommand)uploadCommand).OnCanExecuteChanged();
                //uploadCommand.OnCanExecuteChanged();
            }
        }

        private bool CanExecuteUpload()
        {
            return !uploading;
        }

        private void UpdateProperty()
        {
            OnPropertyChanged("DependenciesFilesExist");
            OnPropertyChanged("AppPackageExists");
        }

        #region バージョン番号

        private Models.AppVersion appVersion;
        //public Models.Version AppVersion;

        private string version1;
        public string Version1
        {
            get { return version1; }
            set {
                this.Set(ref this.version1, value);
            }
        }

        private string version2;
        public string Version2
        {
            get { return version2; }
            set { this.Set(ref this.version2, value); }
        }

        private string version3;
        public string Version3
        {
            get { return version3; }
            set { this.Set(ref this.version3, value); }
        }


        private string version4;
        public string Version4
        {
            get { return version4; }
            set { this.Set(ref this.version4, value); }
        }
        #endregion

        #region コマンド
        private ICommand _getStorageItemsCommand;
        public ICommand GetStorageItemsCommand => _getStorageItemsCommand ?? (_getStorageItemsCommand = new RelayCommand<IReadOnlyList<IStorageItem>>(OnGetStorageItem));

        private ICommand uploadCommand;
        public ICommand UploadCommand => uploadCommand ?? (uploadCommand =
            new RelayCommand(async () => { await UploadPackage(); }, CanExecuteUpload));


        private ICommand selectPackageCommand;
        public ICommand SelectPackageCommand => selectPackageCommand ?? (selectPackageCommand =
            new RelayCommand(async () => { await OpenFilePicker(".appxbundle"); }));


        private ICommand addDependencyCommand;
        public ICommand AddDependencyCommand => addDependencyCommand ?? (addDependencyCommand =
            new RelayCommand(async () => { await OpenFilePicker(".appx"); }));


        private ICommand clearDependencyCommand;
        public ICommand ClearDependencyCommand => clearDependencyCommand ?? (clearDependencyCommand =
            new RelayCommand(async () => { await ClearDependency(); }));

        #endregion

        BusyIndicator indicator;

        #region 設定値
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        const string DeveloperSettingKey = "DeveloperName";
        #endregion

        public UploadViewModel()
        {
            object val = localSettings.Values[DeveloperSettingKey];
            if (val != null && val is string)
            {
                DeveloperName = (string)val;
            }
            uploader = new AzureStorageUploader();
        }

        public void OnGetStorageItem(IReadOnlyList<IStorageItem> items)
        {
            foreach (var item in items)
            {
                if (item is StorageFolder)
                {
                    Task.Run(async () =>
                    {
                        await SelectFolder((StorageFolder)item);
                    });
                }
                else
                {
                    Task.Run(async () =>
                    {
                        await SelectFile((StorageFile)item);
                    });
                }
            }
        }

        async Task SelectFolder(StorageFolder folder)
        {
            // Dependency を初期化
            await ClearDependency();

            // フォルダ内を選択
            IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
            foreach (var file in fileList)
            {
                await SelectFile(file);
            }

            // Dependencies フォルダの中を選択
            try
            {
                var dependenciesFolder = await folder.GetFolderAsync("Dependencies");

                if (dependenciesFolder == null)
                {
                    return;
                }

                var folders = await dependenciesFolder.GetFoldersAsync();
                foreach(var depFolder in folders)
                {
                    bool isValid = SupportedArchitectureHelper.IsValidArchitecture(depFolder.Name);

                    if (!isValid)
                    {
                        continue;
                    }

                    var dependencies = await depFolder.GetFilesAsync();

                    foreach (var dependency in dependencies)
                    {
                        await SelectFile(dependency);
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("folder not found");
            }
        }

        async Task SelectFile(StorageFile file)
        {
            var ext = file.FileType;

            switch (ext)
            {
                case ".appxbundle":
                case ".msixbundle":
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                    {
                        AppPackage = file;
                        // 名前とバージョンを自動設定
                        var filename = file.Name;
                        //var reg = new Regex(@"^([0-9a-zA-Z-]+)_(\d+)\.(\d+)\.(\d+)\.(\d+)_");
                        var reg = new Regex(@"^(.+?)_(\d+)\.(\d+)\.(\d+)\.(\d+)_");
                        var m = reg.Match(filename);
                        if (m.Success)
                        {
                            var groups = m.Groups;
                            Name = groups[1].Value;
                            Version1 = groups[2].Value;
                            Version2 = groups[3].Value;
                            Version3 = groups[4].Value;
                            Version4 = groups[5].Value;
                        }
                    });
                    break;
                case ".appx":
                    Debug.WriteLine("dependencies");
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                    {
                        foreach(var dep in dependenciesFiles)
                        {
                            if(dep.Path == file.Path)
                            {
                                // すでに追加されている場合
                                return;
                            }
                        }
                        dependenciesFiles.Add(file);
                        UpdateProperty();
                    });
                    break;
            }
        }

        private async Task UploadPackage()
        {
            Uploading = true;
            SuccessMessage = "";
            ErrorMessage = "";

            // アップロードするパッケージをクラスに格納
            var version = new AppVersion()
            {
                Version1 = StringToUint(version1),
                Version2 = StringToUint(version2),
                Version3 = StringToUint(version3),
                Version4 = StringToUint(version4),
            };

            var supportedArchitecture = SupportedArchitectureHelper.GetSupportedArchitectureFromAppPackage(appPackage);

            // アプリが対応するアーキテクチャ依存ファイルのみをアップロード
            var dependencies = new List<StorageFile>();
            foreach(var dep in dependenciesFiles)
            {
                var parent = System.IO.Directory.GetParent(dep.Path);
                var depArchitecture = SupportedArchitectureHelper.StringToSupportedArchitectureType(parent.Name);

                if (supportedArchitecture.HasFlag(depArchitecture))
                {
                    dependencies.Add(dep);
                    Debug.WriteLine(dep.Name);
                }
            }
            
            var uploadPackage = new Application()
            {
                DeveloperName = developerName,
                Name = name,
                Version = version,
                SupportedArchitecture = supportedArchitecture,
                AppPackage = appPackage,
                Dependencies = dependencies,
            };

            var r = ResourceLoader.GetForCurrentView();

            if (uploadPackage.IsValid)
            {
                var uploadingMessageTemplate = r.GetString("Upload_UploadingMessage");
                var uploadingMessage = string.Format(uploadingMessageTemplate, uploadPackage.Name);

                var uploadedMessageTemplate = r.GetString("Upload_SuccessMessage");
                var uploadedMessage = string.Format(uploadedMessageTemplate, uploadPackage.Name + " " + uploadPackage.Version.ToString());

                indicator?.Hide();
                indicator = new BusyIndicator()
                {
                    Message = uploadingMessage
                };
                indicator.Show();

                SuccessMessage = uploadingMessage;

                var (appInfo, result) = await uploader.Upload(uploadPackage);

                switch (result)
                {
                    case UploadStatusType.NewlyUploaded:
                        var app = new AppInfoForInstall()
                        {
                            AppInfo = appInfo
                        };
                        app.SelectLatestVersion();
                        NavigationService.Navigate(typeof(EditApplicationPage), app);
                        break;
                    case UploadStatusType.Updated:
                        // 入力項目をクリア
                        Version1 = "";
                        Version2 = "";
                        Version3 = "";
                        Version4 = "";
                        Name = "";
                        AppPackage = null;
                        await ClearDependency();
                        SuccessMessage = uploadedMessage;
                        break;
                    case UploadStatusType.NetworkError:
                    case UploadStatusType.UnknownError:
                        SuccessMessage = "";
                        ErrorMessage = r.GetString("Upload_FailureMessage");
                        break;
                }
                indicator?.Hide();
            }
            else
            {
                ErrorMessage = r.GetString("Upload_MissingMessage");
            }
            Uploading = false;
        }

        private async Task OpenFilePicker(string filetype = ".appx")
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List
            };

            picker.FileTypeFilter.Add(filetype);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await SelectFile(file);
            }
        }

        private async Task ClearDependency()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                dependenciesFiles.Clear();
                UpdateProperty();
            });
        }

        static uint? StringToUint(string number)
        {
            if (uint.TryParse(number, out uint result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }


    public class NullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        { return value; }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            int temp;
            if (string.IsNullOrEmpty((string)value) || !int.TryParse((string)value, out temp))
            {
                return null;
            }
            else
            {
                return temp;
            }
        }
    }
}
