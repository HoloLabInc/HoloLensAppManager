using System;

using HoloLensAppManager.ViewModels;

using Windows.UI.Xaml.Controls;

namespace HoloLensAppManager.Views
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel { get; } = new ShellViewModel();

        public ShellPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Initialize(shellFrame);
        }
    }
}
