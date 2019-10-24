using System;

using HoloLensAppManager.ViewModels;

using Windows.UI.Xaml.Controls;

namespace HoloLensAppManager.Views
{
    public sealed partial class InstallPage : Page
    {
        public InstallViewModel ViewModel { get; } = new InstallViewModel();

        // TODO WTS: Change the grid as appropriate to your app.
        // For help see http://docs.telerik.com/windows-universal/controls/raddatagrid/gettingstarted
        // You may also want to extend the grid to work with the RadDataForm http://docs.telerik.com/windows-universal/controls/raddataform/dataform-gettingstarted
        public InstallPage()
        {
            InitializeComponent();
            //ViewModel = new InstallViewModel(this.EditDialog);
        }

        private void username_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void password_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ((PasswordBox)sender).SelectAll();
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = ((TextBox) sender).Text;
            ViewModel.SearhWithKeyword(keyword);
        }
    }
}
