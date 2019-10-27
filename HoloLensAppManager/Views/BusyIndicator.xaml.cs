using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace HoloLensAppManager.Views
{
    public sealed partial class BusyIndicator : UserControl
    {
        #region Privates

        /// <summary>
        /// 現在のページ
        /// </summary>
        private Page currentPage;

        /// <summary>
        /// 自分が乗るPopup
        /// </summary>
        private Popup popup = new Popup();

        #endregion //Privates

        #region Message 依存関係プロパティ
        /// <summary>
        /// Message 依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty MessageProperty
            = DependencyProperty.Register(
            "Message",
            typeof(string),
            typeof(BusyIndicator),
            new PropertyMetadata(
                null,
                (s, e) =>
                {
                    var control = s as BusyIndicator;
                    if (control != null)
                    {
                        control.OnMessageChanged();
                    }
                }));

        /// <summary>
        /// Message 変更イベントハンドラ
        /// </summary>
        private void OnMessageChanged()
        {
            this.Message_Part.Text = this.Message;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get { return (string)this.GetValue(MessageProperty); }
            set { this.SetValue(MessageProperty, value); }
        }
        #endregion //Message 依存関係プロパティ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BusyIndicator()
        {
            this.InitializeComponent();

            this.Width = Window.Current.Bounds.Width;
            this.Height = Window.Current.Bounds.Height;

            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        /// <summary>
        /// 読み込み完了イベントハンドラ
        /// </summary>
        /// <param name="sender">イベント発行者</param>
        /// <param name="e">イベント引数</param>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var rootrame = Window.Current.Content as Frame;
                    if (rootrame == null)
                    {
                        return;
                    }
                    this.currentPage = rootrame.Content as Page;
                    if (this.currentPage == null)
                    {
                        return;
                    }

                    if (this.currentPage.TopAppBar != null)
                    {
                        this.currentPage.TopAppBar.Opened += this.OnAppBarOpend;
                    }
                    if (this.currentPage.BottomAppBar != null)
                    {
                        this.currentPage.BottomAppBar.Opened += this.OnAppBarOpend;
                    }
                });
        }

        /// <summary>
        /// 表示
        /// </summary>
        public void Show()
        {
            this.popup.Child = this;
            this.popup.IsOpen = true;
            this.popup.Closed += this.OnClosed;
            Window.Current.SizeChanged += this.OnWindowSizeChanged;

            if (this.currentPage == null)
            {
                return;
            }

            if (this.currentPage.TopAppBar != null)
            {
                this.currentPage.TopAppBar.IsOpen = false;
            }
            if (this.currentPage.BottomAppBar != null)
            {
                this.currentPage.BottomAppBar.IsOpen = false;
            }
            this.currentPage.IsEnabled = false;
            this.currentPage.IsHitTestVisible = false;
        }

        /// <summary>
        /// ウィンドウサイズ変更イベントハンドラ
        /// </summary>
        /// <param name="sender">イベント発行者</param>
        /// <param name="e">イベント引数</param>
        private async void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            await this.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () =>
                            {
                                this.Width = Window.Current.Bounds.Width;
                                this.Height = Window.Current.Bounds.Height;
                            });
        }

        /// <summary>
        /// 非表示
        /// </summary>
        public void Hide()
        {
            if (popup != null)
            {
                popup.IsOpen = false;
            }

            if (this.currentPage == null)
            {
                return;
            }

            this.currentPage.IsEnabled = true;
            this.currentPage.IsHitTestVisible = true;
        }

        /// <summary>
        /// 非表示イベントハンドラ
        /// </summary>
        /// <param name="sender">イベント発行者</param>
        /// <param name="e">イベント引数</param>
        private void OnClosed(object sender, object e)
        {
            this.popup.Child = null;
            this.popup = null;
        }

        /// <summary>
        /// インスタンス破棄時の処理
        /// </summary>
        /// <param name="sender">イベント発行者</param>
        /// <param name="e">イベント引数</param>
        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.OnUnloaded;
            this.Loaded -= this.OnLoaded;

            this.popup.Closed -= this.OnClosed;
            this.popup.Child = null;
            this.popup = null;
            Window.Current.SizeChanged -= this.OnWindowSizeChanged;

            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    if (this.currentPage == null)
                    {
                        return;
                    }
                    if (this.currentPage.TopAppBar != null)
                    {
                        this.currentPage.TopAppBar.Opened -= this.OnAppBarOpend;
                    }
                    if (this.currentPage.BottomAppBar != null)
                    {
                        this.currentPage.BottomAppBar.Opened -= this.OnAppBarOpend;
                    }
                    this.currentPage = null;
                });
        }

        /// <summary>
        /// アプリバー表示イベントハンドラ
        /// </summary>
        /// <param name="sender">イベント発行者</param>
        /// <param name="e">イベント引数</param>
        private void OnAppBarOpend(object sender, object e)
        {
            var appBar = sender as AppBar;
            if (appBar == null)
            {
                return;
            }

            // ビジー中は抑止する
            if (this.popup != null)
            {
                appBar.IsOpen = false;
            }
        }
    }
}
