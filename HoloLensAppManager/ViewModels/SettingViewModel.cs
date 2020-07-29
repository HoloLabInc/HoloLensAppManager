using HoloLensAppManager.Helpers;
using HoloLensAppManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;

namespace HoloLensAppManager.ViewModels
{
    public class SettingViewModel : Observable
    {
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

        private ICommand clearCacheCommand;
        public ICommand ClearCacheCommand => clearCacheCommand ?? (clearCacheCommand =
            new RelayCommand(async () => { await ClearCache(); }));


        private ICommand openDownloadFolder;
        public ICommand OpenDownloadFolder => openDownloadFolder ?? (openDownloadFolder =
                                                 new RelayCommand(async () => { await OpenFolder(); }));


        AzureStorageUploader uploader;

        public SettingViewModel()
        {
            uploader = new AzureStorageUploader();
        }

        private async Task OpenFolder()
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }

        private async Task ClearCache()
        {
            var r = ResourceLoader.GetForCurrentView();

            SuccessMessage = "";
            ErrorMessage = "";

            var result = await uploader.ClearCache();
            if (result)
            {
                SuccessMessage = r.GetString("Setting_ClearCacheSuccessMessage");
            }
            else
            {
                ErrorMessage = r.GetString("Setting_ClearCacheFailureMessage");
            }
        }
    }
}
