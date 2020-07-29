using HoloLensAppManager.Helpers;
using HoloLensAppManager.Models;
using HoloLensAppManager.Services;
using HoloLensAppManager.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;

namespace HoloLensAppManager.ViewModels
{
    public class EditApplicationViewModel : Observable
    {

        private string pageTitle;
        public string PageTitle
        {
            get { return pageTitle; }
            set
            {
                this.Set(ref this.pageTitle, value);
            }
        }

        private string saveErrorMessage;
        public string SaveErrorMessage
        {
            get { return saveErrorMessage; }
            set
            {
                this.Set(ref this.saveErrorMessage, value);
            }
        }


        private string removeErrorMessage;
        public string RemoveErrorMessage
        {
            get { return removeErrorMessage; }
            set
            {
                this.Set(ref this.removeErrorMessage, value);
            }
        }

        private string removeSelectedVersionErrorMessage;
        public string RemoveSelectedVersionErrorMessage
        {
            get { return removeSelectedVersionErrorMessage; }
            set
            {
                this.Set(ref this.removeSelectedVersionErrorMessage, value);
            }
        }

        private string removeApplicationName;
        public string RemoveApplicationName
        {
            get { return removeApplicationName; }
            set
            {
                this.Set(ref this.removeApplicationName, value);
            }
        }




        private ICommand saveCommand;
        public ICommand SaveCommand => saveCommand ?? (saveCommand =
            new RelayCommand(async () => { await SaveAppInfo(); }));

        private ICommand removeCommand;
        public ICommand RemoveCommand => removeCommand ?? (removeCommand =
            new RelayCommand(async () => { await RemoveApp(); }));

        private ICommand removeSelectedVersionCommand;
        public ICommand RemoveSelectedVersionCommand => removeSelectedVersionCommand ?? (removeSelectedVersionCommand =
            new RelayCommand(async () => { await RemoveSelectedVersion(); }));


        private AppInfoForInstall appInfoForInstall;
        public AppInfoForInstall AppInfoForInstall
        {
            get
            {
                return appInfoForInstall;
            }
            set
            {
                this.Set(ref this.appInfoForInstall, value);

                var r = ResourceLoader.GetForCurrentView();
                var pageTitleTemplate = r.GetString("EditApplication_PageTitleTemplate");
                PageTitle = string.Format(pageTitleTemplate, AppInfoForInstall?.AppInfo?.Name);
            }
        }

        AzureStorageUploader uploader;

        private async Task RemoveApp()
        {
            var r = ResourceLoader.GetForCurrentView();

            if (AppInfoForInstall?.AppInfo?.Name == RemoveApplicationName)
            {
                RemoveErrorMessage = r.GetString("EditApplication_RemovingApllication");
                var result = await uploader.Delete(AppInfoForInstall.AppInfo);
                if (result)
                {
                    NavigationService.Navigate(typeof(InstallPage));
                }
                else
                {
                    RemoveErrorMessage = r.GetString("EditApplication_RemoveApplicationFailed");
                }
            }
            else
            {
                RemoveErrorMessage = r.GetString("EditApplication_WrongApplicationName");
            }
        }

        private async Task RemoveSelectedVersion()
        {
            var r = ResourceLoader.GetForCurrentView();

            RemoveSelectedVersionErrorMessage = "";
            var removeVersion = AppInfoForInstall.SelectedVersion;
            if (removeVersion == null)
            {
                return;
            }

            var result = await uploader.DeleteApplication(AppInfoForInstall.AppInfo.Name, removeVersion.ToString());
            if (result)
            {
                var removeVersionTemplate = r.GetString("EditApplication_RemoveSelectedVersionSuccessTemplate");
                RemoveSelectedVersionErrorMessage = string.Format(removeVersionTemplate, removeVersion.ToString());

                AppInfoForInstall.AppInfo.Versions.Remove(removeVersion);
                AppInfoForInstall.SelectLatestVersion();
            }
            else
            {
                RemoveSelectedVersionErrorMessage = r.GetString("EditApplication_RemoveSelectedVersionFailure");
            }
        }


        public EditApplicationViewModel()
        {
            uploader = new AzureStorageUploader();
            AppInfoForInstall = new AppInfoForInstall();
        }

        private async Task SaveAppInfo()
        {
            SaveErrorMessage = "";
            var result = await uploader.UpdateAppInfo(AppInfoForInstall.AppInfo);
            if (result)
            {
                NavigationService.Navigate(typeof(InstallPage));
            }
            else
            {
                var r = ResourceLoader.GetForCurrentView();
                SaveErrorMessage = r.GetString("EditApplication_SaveAppInfoFailure");
            }
        }
    }
}

