using HoloLensAppManager.Helpers;
using HoloLensAppManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

        AzureStorageUploader uploader;

        public SettingViewModel()
        {
            uploader = new AzureStorageUploader();
        }

        private async Task ClearCache()
        {
            SuccessMessage = "";
            ErrorMessage = "";

            var result = await uploader.ClearCache();
            if (result)
            {
                SuccessMessage = "削除しました";
            }
            else
            {
                ErrorMessage = "削除に失敗しました";
            }
        }
    }
}
