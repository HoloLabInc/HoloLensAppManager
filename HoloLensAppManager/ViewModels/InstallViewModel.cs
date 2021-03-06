﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using HoloLensAppManager.Helpers;
using HoloLensAppManager.Models;
using HoloLensAppManager.Services;
using HoloLensAppManager.Views;
using Microsoft.Tools.WindowsDevicePortal;
using Windows.ApplicationModel.Resources;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace HoloLensAppManager.ViewModels
{
    public class AppInfoForInstall : Observable
    {
        public AppInfo AppInfo;

        private List<AppVersion> sortedVersions;
        public List<AppVersion> SortedVersions
        {
            get
            {
                if(sortedVersions != null && sortedVersions.Count() == AppInfo.Versions.Count())
                {
                    return sortedVersions;
                }
                if (AppInfo.Versions == null)
                {
                    sortedVersions = new List<AppVersion>();
                }
                else
                {
                    sortedVersions = AppInfo.Versions.ToList();
                    sortedVersions.Sort();
                    sortedVersions.Reverse();
                }
                return sortedVersions;
            }
        }

        private AppVersion selectedVersion;
        public AppVersion SelectedVersion
        {
            get { return selectedVersion; }
            set
            {
                if (value != null)
                {
                    this.Set(ref this.selectedVersion, value);
                }
            }
        }

        public void SelectLatestVersion()
        {
            OnPropertyChanged("SortedVersions");
            if (SortedVersions.Count > 0)
            {
                SelectedVersion = SortedVersions[0];
            }
            else
            {
                SelectedVersion = null;
            }
        }
    }

    public class InstallViewModel : Observable
    {
        /// <summary>
        /// Sort condition
        /// </summary>
        private enum SortConditionType
        {
            UpdateDateDescending = 0,
            UpdateDateAscending,
            AppNameAscending,
            AppNameDescending,
        }

        private List<AppInfoForInstall> appInfoList = new List<AppInfoForInstall>();

        private ObservableCollection<AppInfoForInstall> searchedAppInfoList = new ObservableCollection<AppInfoForInstall>();
        public ObservableCollection<AppInfoForInstall> SearchedAppInfoList
        {
            get
            {
                return searchedAppInfoList;
            }
        }

        private int versionIndex;
        public int VersionIndex
        {
            get { return versionIndex; }
            set
            {
                this.Set(ref this.versionIndex, value);
            }
        }

        #region HoloLens 接続用プロパティ
        private string address;
        public string Address
        {
            get { return address; }
            set
            {
                this.Set(ref this.address, value);
                localSettings.Values[AddressSettingKey] = value;
                ((RelayCommand)ConnectCommand).OnCanExecuteChanged();
            }
        }

        private bool usbConnection;
        public bool UsbConnection
        {
            get { return usbConnection; }
            set
            {
                this.Set(ref this.usbConnection, value);
                localSettings.Values[UsbConnectionSettingKey] = value.ToString();
                ((RelayCommand)ConnectCommand).OnCanExecuteChanged();
                AddressEnabled = !usbConnection;
            }
        }

        private bool addressEnabled;
        public bool AddressEnabled
        {
            get { return addressEnabled; }
            set
            {
                this.Set(ref this.addressEnabled, value);
            }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set
            {
                this.Set(ref this.username, value);
                localSettings.Values[UsernameSettingKey] = value;
                ((RelayCommand)ConnectCommand).OnCanExecuteChanged();
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                this.Set(ref this.password, value);
                localSettings.Values[PasswordSettingKey] = value;
                ((RelayCommand)ConnectCommand).OnCanExecuteChanged();
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

        private string successMessage;
        public string SuccessMessage
        {
            get { return successMessage; }
            set
            {
                this.Set(ref this.successMessage, value);
            }
        }

        private string query = "";
        public string Query
        {
            get { return query; }
            set
            {
                this.Set(ref this.query, value);
                UpdateDisplayedApp();
            }
        }

        public List<string> SortConditions { get; }

        private int sortKeyIndex = 0;
        public int SortKeyIndex
        {
            get { return sortKeyIndex; }
            set
            {
                this.Set(ref this.sortKeyIndex, value);
                localSettings.Values[SortConditionSettingKey] = value;
                UpdateSortCondition();
            }
        }

        // Device targeting
        private bool targetIsHoloLens1;
        public bool TargetIsHoloLens1
        {
            get { return targetIsHoloLens1; }
            set
            {
                if (value)
                {
                    this.Set(ref this.targetIsHoloLens1, true);
                    this.Set(ref this.targetIsHoloLens2, false);
                    localSettings.Values[TargetDeviceSettingKey] = "HoloLens1";
                    UpdateDisplayedApp();
                }
            }
        }

        private bool targetIsHoloLens2;
        public bool TargetIsHoloLens2
        {
            get { return targetIsHoloLens2; }
            set
            {
                if (value)
                {
                    this.Set(ref this.targetIsHoloLens1, false);
                    this.Set(ref this.targetIsHoloLens2, true);
                    localSettings.Values[TargetDeviceSettingKey] = "HoloLens2";
                    UpdateDisplayedApp();
                }
            }
        }
        #endregion

        #region コマンド
        private ICommand connectCommand;
        public ICommand ConnectCommand => connectCommand ?? (connectCommand =
            new RelayCommand(async () => { await ConnectToDevice(); }, CanExecuteConnect));

        private bool CanExecuteConnect()
        {
            return (connectionStatus == ConnectionState.NotConnected || connectionStatus == ConnectionState.Connected)
                && ( !String.IsNullOrWhiteSpace(Address) || usbConnection)
                && !String.IsNullOrWhiteSpace(Username)
                && !String.IsNullOrWhiteSpace(Password);
        }

        private ICommand installCommand;
        public ICommand InstallCommand => installCommand ?? (installCommand =
            new RelayCommand<AppInfoForInstall>(async (app) => { await InstallApplication(app); }));

        private ICommand editCommand;
        public ICommand EditCommand => editCommand ?? (editCommand =
            new RelayCommand<AppInfoForInstall>(async (app) => { await EditApplication(app); }));


        #endregion

        #region 設定値
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        const string AddressSettingKey = "DeviceAddress";
        const string UsbConnectionSettingKey = "DeviceUsbConnection";
        const string UsernameSettingKey = "DeviceUserName";
        const string PasswordSettingKey = "DevicePassword";
        const string TargetDeviceSettingKey = "TargetDevice";
        const string SortConditionSettingKey = "SortCondition";
        #endregion

        #region アプリリストでの検索機能

        private void UpdateDisplayedApp()
        {
            var searchQuery = query;
            var newList = appInfoList.Where(app => IsAppDisplayed(app, searchQuery));

            // 表示されなくなったアプリを searchedAppInfoList から削除
            for (int i = searchedAppInfoList.Count - 1; i >= 0; i--)
            {
                var app = searchedAppInfoList[i];
                if (!newList.Contains(app))
                {
                    searchedAppInfoList.RemoveAt(i);
                }
            }

            // 新しく表示されるアプリを searchedAppInfoList に追加
            var newAppIndex = 0;
            foreach (var app in newList)
            {
                if (!searchedAppInfoList.Contains(app))
                {
                    searchedAppInfoList.Insert(newAppIndex, app);
                    app.SelectLatestVersion();
                }
                newAppIndex += 1;
            }
        }

        private bool IsAppDisplayed(AppInfoForInstall app, string searchQuery)
        {
            if(app.AppInfo.Versions.Count == 0)
            {
                return false;
            }

            var architectureIsValid = true;
            var supportedArchtecture = app.AppInfo.SupportedArchitecture;
            if (TargetIsHoloLens1) {
                architectureIsValid = supportedArchtecture.HasFlag(SupportedArchitectureType.X86);
            }
            else if (TargetIsHoloLens2)
            {
                architectureIsValid = supportedArchtecture.HasFlag(SupportedArchitectureType.Arm)
                    || supportedArchtecture.HasFlag(SupportedArchitectureType.Arm64);
            }

            return architectureIsValid && MatchWithSearchQuery(app, searchQuery);
        }

        private bool MatchWithSearchQuery(AppInfoForInstall app, string searchQuery)
        {
            var keywords = searchQuery.Split(' ');

            var description = app.AppInfo.Description;
            var appName = app.AppInfo.Name;
            var developerName = app.AppInfo.DeveloperName;

            var searchTargets = new string[] { description, appName, developerName };

            foreach (var keyword in keywords)
            {
                var keywordFound = false;
                foreach (var target in searchTargets)
                {
                    if (target.ToLower().Contains(keyword.ToLower()))
                    {
                        keywordFound = true;
                        break;
                    }
                }

                if (!keywordFound)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region アプリリストソート機能

        private void UpdateSortCondition()
        {
            var sortCondition = (SortConditionType)Enum.ToObject(typeof(SortConditionType), sortKeyIndex);
            switch (sortCondition)
            {
                case SortConditionType.UpdateDateAscending:
                    appInfoList.Sort((a, b) => a.AppInfo.UpdateAt.CompareTo(b.AppInfo.UpdateAt));
                    break;
                case SortConditionType.UpdateDateDescending:
                    appInfoList.Sort((a, b) => -a.AppInfo.UpdateAt.CompareTo(b.AppInfo.UpdateAt));
                    break;
                case SortConditionType.AppNameAscending:
                    appInfoList.Sort((a, b) => a.AppInfo.Name.CompareTo(b.AppInfo.Name));
                    break;
                case SortConditionType.AppNameDescending:
                    appInfoList.Sort((a, b) => -a.AppInfo.Name.CompareTo(b.AppInfo.Name));
                    break;
                default:
                    // same as UpdatedDateAscending
                    appInfoList.Sort((a, b) => a.AppInfo.UpdateAt.CompareTo(b.AppInfo.UpdateAt));
                    break;
            }

            searchedAppInfoList.Clear();
            UpdateDisplayedApp();
        }
        #endregion

        public enum ConnectionState
        {
            NotConnected, Connecting, Connected
        }

        ConnectionState connectionStatus = ConnectionState.NotConnected;
        public ConnectionState ConnectionStatus
        {
            get
            {
                return connectionStatus;
            }
            set
            {
                connectionStatus = value;
                ((RelayCommand)ConnectCommand).OnCanExecuteChanged();
            }
        }

        IUploader uploader;
        DevicePortal portal;
        BusyIndicator indicator;

        public InstallViewModel()
        {
            var r = ResourceLoader.GetForCurrentView();

            // 検索項目の設定
            var sortConditionTexts = new Dictionary<SortConditionType, string>()
            {
                { SortConditionType.UpdateDateDescending, r.GetString("Install_Sort_DateDesc") },
                { SortConditionType.UpdateDateAscending, r.GetString("Install_Sort_DateAsc") },
                { SortConditionType.AppNameAscending, r.GetString("Install_Sort_NameAsc") },
                { SortConditionType.AppNameDescending, r.GetString("Install_Sort_NameDesc") }
            };

            SortConditions = new List<string>();
            foreach (SortConditionType sortCondition in Enum.GetValues(typeof(SortConditionType)))
            {
                SortConditions.Add(sortConditionTexts[sortCondition]);
            }

            // 接続情報の設定
            Address = LoadSettingData(localSettings, AddressSettingKey);
            try
            {
                UsbConnection = Convert.ToBoolean(LoadSettingData(localSettings, UsbConnectionSettingKey));
            }
            catch (Exception)
            {
                UsbConnection = false;
            }
            Username = LoadSettingData(localSettings, UsernameSettingKey);
            Password = LoadSettingData(localSettings, PasswordSettingKey);
            var targetDevice = LoadSettingData(localSettings, TargetDeviceSettingKey);
            if (targetDevice == "HoloLens1")
            {
                TargetIsHoloLens1 = true;
            }
            else
            {
                TargetIsHoloLens2 = true;
            }

            SortKeyIndex = LoadSettingData<int>(localSettings, SortConditionSettingKey);

            // ローカルデバッグ設定
            var settings = ResourceLoader.GetForCurrentView("settings");
            var debugSetting = settings.GetString("LOCAL_DEBUG");

            var isLocalDebug = StringToBool(debugSetting);
            if (isLocalDebug)
            {
                uploader = new DummyUploader();
            }
            else
            {
                uploader = new AzureStorageUploader();
            }

            _ = UpdateApplicationList();

            indicator = new BusyIndicator()
            {
                Message = r.GetString("Install_BusyIndicatorMessage")
            };
        }

        private bool StringToBool(string inputString)
        {
            bool.TryParse(inputString, out var result);
            return result;
        }

        private string LoadSettingData(ApplicationDataContainer setting, string key)
        {
            object val = localSettings.Values[key];
            if (val != null && val is string)
            {
                return(string)val;
            }
            return "";
        }

        private T LoadSettingData<T>(ApplicationDataContainer setting, string key)
        {
            object val = localSettings.Values[key];
            if (val != null && val is T)
            {
                return (T)val;
            }
            return default;
        }


        public async Task UpdateApplicationList()
        {
            var list = await uploader.GetAppInfoListAsync();
            appInfoList.Clear();
            foreach(var app in list)
            {
                appInfoList.Add(new AppInfoForInstall()
                {
                    AppInfo = app
                }
                );
            }

            foreach (var app in appInfoList)
            {
                app.SelectLatestVersion();
            }

            UpdateSortCondition();
        }

        private async Task InstallApplication(AppInfoForInstall appForInstall)
        {
            var r = ResourceLoader.GetForCurrentView();

            if (appForInstall == null)
            {
                return;
            }

            if(appForInstall.SelectedVersion == null)
            {
                appForInstall.SelectLatestVersion();
                if(appForInstall.SelectedVersion == null)
                {
                    return;
                }
            }

            var appName = appForInstall.AppInfo.Name;

            var downloadingMessageTemplate = r.GetString("Install_DownloadingMessage");
            var downloadingMessage = string.Format(downloadingMessageTemplate, appName);

            indicator = new BusyIndicator()
            {
                Message = downloadingMessage
            };
            indicator.Show();

            ErrorMessage = "";
            SuccessMessage = downloadingMessage;

            var version = appForInstall.SelectedVersion.ToString();

            SupportedArchitectureType supportedArchitecture = SupportedArchitectureType.None;
            if (targetIsHoloLens1)
            {
                supportedArchitecture = SupportedArchitectureType.X86;
            }
            else if (targetIsHoloLens2)
            {
                supportedArchitecture = SupportedArchitectureType.Arm | SupportedArchitectureType.Arm64;
            }

            var (app, error) = await uploader.Download(appName, version, supportedArchitecture);
            if (app == null)
            {
                switch (error)
                {
                    case DownloadErrorType.UnknownError:
                    case DownloadErrorType.NetworkError:
                        var downloadErrorMessageTemplate = r.GetString("Install_DownloadErrorMessage");
                        ErrorMessage = string.Format(downloadErrorMessageTemplate, appForInstall.AppInfo.Name);
                        break;
                    case DownloadErrorType.NotSupportedArchitecture:
                        ErrorMessage = r.GetString("Install_AppPackageNotFoundErrorMessage");
                        break;
                }

                SuccessMessage = "";
                indicator.Hide();
            }
            else
            {
                indicator.Hide();
                indicator = new BusyIndicator()
                {
                    Message = r.GetString("Install_ConnectingMessage")
                };
                indicator.Show();

                var result = await ConnectToDevice();
                if (result)
                {
                    var installingMessageTemplate = r.GetString("Install_InstallingMessage");
                    var installingMessage = string.Format(installingMessageTemplate, appName);

                    indicator.Hide();
                    indicator = new BusyIndicator()
                    {
                        Message = installingMessage
                    };
                    indicator.Show();

                    SuccessMessage = installingMessage;
                    ErrorMessage = "";
                    var (installResult, installError) = await InstallPackageAsync(app);
                    if (!installResult)
                    {
                        SuccessMessage = "";
                        ErrorMessage = installError;
                    }
                    indicator.Hide();
                }
                else
                {
                    indicator.Hide();
                }
            }
        }

        private async Task EditApplication(AppInfoForInstall app)
        {
            NavigationService.Navigate(typeof(EditApplicationPage), app);
        }

        private async Task<(bool, string)> InstallPackageAsync(Application app)
        {
            // Install fails if app package name contains multibyte character
            var originalPackage = app.AppPackage;
            try
            {
                var folder = await originalPackage.GetParentAsync();
                var tmpFolder = await folder.CreateFolderAsync("install", CreationCollisionOption.ReplaceExisting);

                var installPackage = await CopyToFolder(tmpFolder, originalPackage, "package");
                var dependencies = new List<StorageFile>();

                var i = 0;
                foreach (var dependency in app.Dependencies)
                {
                    var tmpDependency = await CopyToFolder(tmpFolder, dependency, $"dependency_{i++}");
                    dependencies.Add(tmpDependency);
                }
                await portal?.InstallApplicationAsync(app.Name, installPackage, dependencies);
                return (true, "");
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        private async Task<StorageFile> CopyToFolder(StorageFolder folder, StorageFile file, string filename)
        {
            var extension = System.IO.Path.GetExtension(file.Path);
            var copiedFile = await file.CopyAsync(folder, $"{filename}{extension}", NameCollisionOption.GenerateUniqueName);
            return copiedFile;
        }

        private async Task<bool> ConnectToDevice()
        {
            var r = ResourceLoader.GetForCurrentView();

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                Console.WriteLine("Connecting...");
                ConnectionStatus = ConnectionState.Connecting;

                SuccessMessage = r.GetString("Install_ConnectingMessage");
                ErrorMessage = "";

            });

            string connectionAddress;
            Address = Address.Trim();

            if (UsbConnection || Address.StartsWith("127.0.0.1"))
            {
                connectionAddress = "https://127.0.0.1:10443";
            }
            else
            {
                connectionAddress = $"https://{Address}";
            }

            portal = new DevicePortal(
                new DefaultDevicePortalConnection(connectionAddress, Username, Password));

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Connecting...");
            Console.WriteLine("Connecting...");

            var tcs = new TaskCompletionSource<bool>();

            portal.AppInstallStatus += async (p, eventArgs) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                {
                    switch (eventArgs.Status)
                    {
                        case ApplicationInstallStatus.Completed:
                            SuccessMessage = eventArgs.Message;
                            ErrorMessage = "";
                            break;
                        case ApplicationInstallStatus.Failed:
                            SuccessMessage = "";
                            ErrorMessage = eventArgs.Message;
                            break;
                    }
                });
            };

            portal.ConnectionStatus += async (p, connectArgs) =>
            {
                if (connectArgs.Status == DeviceConnectionStatus.Connected)
                {
                    sb.Append("Connected to: ");
                    sb.AppendLine(p.Address);
                    sb.Append("OS version: ");
                    sb.AppendLine(p.OperatingSystemVersion);
                    sb.Append("Device family: ");
                    sb.AppendLine(p.DeviceFamily);
                    sb.Append("Platform: ");
                    sb.AppendLine(String.Format("{0} ({1})",
                        p.PlatformName,
                        p.Platform.ToString()));
                    tcs.SetResult(true);
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                    {
                        ConnectionStatus = ConnectionState.Connected;
                        SuccessMessage = r.GetString("Install_ConnectionSuccessMessage");
                    });

                }
                else if(connectArgs.Status == DeviceConnectionStatus.Failed)
                {
                    tcs.SetResult(false);
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                    {
                        ConnectionStatus = ConnectionState.NotConnected;
                        SuccessMessage = "";
                        ErrorMessage = r.GetString("Install_ConnectionFailureMessage");
                    });
                }
            };

            try
            {
                // If the user wants to allow untrusted connections, make a call to GetRootDeviceCertificate
                // with acceptUntrustedCerts set to true. This will enable untrusted connections for the
                // remainder of this session.
                Certificate certificate = null;
                bool allowUntrusted = true;
                if (allowUntrusted)
                {
                    certificate = await portal.GetRootDeviceCertificateAsync(true);
                }
                await portal.ConnectAsync(manualCertificate: certificate);
                return await tcs.Task;
            }
            catch (Exception exception)
            {
                sb.AppendLine(exception.Message);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
                {
                    ConnectionStatus = ConnectionState.NotConnected;
                    SuccessMessage = "";
                    ErrorMessage = r.GetString("Install_ConnectionFailureMessage");
                    indicator.Hide();
                });
                return false;
            }
        }
    }
}
