using System;

using HoloLensAppManager.ViewModels;

using Windows.UI.Xaml.Controls;

namespace HoloLensAppManager.Views
{
    public sealed partial class UploadPage : Page
    {
        public UploadViewModel ViewModel { get; } = new UploadViewModel();

        public UploadPage()
        {
            InitializeComponent();
        }
    }
}
