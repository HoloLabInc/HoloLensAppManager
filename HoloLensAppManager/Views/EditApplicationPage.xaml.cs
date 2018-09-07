using HoloLensAppManager.Models;
using HoloLensAppManager.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace HoloLensAppManager.Views
{


    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class EditApplicationPage : Page
    {
        public EditApplicationViewModel ViewModel { get; } = new EditApplicationViewModel();

        public EditApplicationPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            AppInfoForInstall appInfo = e.Parameter as AppInfoForInstall;
            ViewModel.AppInfoForInstall = appInfo;
            base.OnNavigatedTo(e);
        }
    }
}
