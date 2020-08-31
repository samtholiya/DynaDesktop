using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using DynamicWallpaper.Helpers;
using DynamicWallpaper.Services;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using WinUI = Microsoft.UI.Xaml.Controls;

namespace DynamicWallpaper.Views
{
    // TODO WTS: Change the icons and titles for all NavigationViewItems in ShellPage.xaml.
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);

        public static ShellPage Current;
        private bool _isBackEnabled;
        private WinUI.NavigationViewItem _selected;

        public bool IsBackEnabled
        {
            get { return _isBackEnabled; }
            set { Set(ref _isBackEnabled, value); }
        }

        public WinUI.NavigationViewItem Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ShellPage()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
            Current = this;
        }

        private void Initialize()
        {
            NavigationService.Frame = shellFrame;
            NavigationService.NavigationFailed += Frame_NavigationFailed;
            NavigationService.Navigated += Frame_Navigated;
            navigationView.BackRequested += OnBackRequested;
            Window.Current.SetTitleBar(AppTitleBar);
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            KeyboardAccelerators.Add(_backKeyboardAccelerator);
            await Task.CompletedTask;
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw e.Exception;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            IsBackEnabled = NavigationService.CanGoBack;
            var selectedItem = GetSelectedItem(navigationView.MenuItems, e.SourcePageType);
            if (selectedItem != null)
            {
                Selected = selectedItem;
            }
        }

        private WinUI.NavigationViewItem GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
        {
            foreach (var item in menuItems.OfType<WinUI.NavigationViewItem>())
            {
                if (IsMenuItemForPageType(item, pageType))
                {
                    return item;
                }

                var selectedChild = GetSelectedItem(item.MenuItems, pageType);
                if (selectedChild != null)
                {
                    return selectedChild;
                }
            }

            return null;
        }

        private bool IsMenuItemForPageType(WinUI.NavigationViewItem menuItem, Type sourcePageType)
        {
            var pageType = menuItem.GetValue(NavHelper.NavigateToProperty) as Type;
            return pageType == sourcePageType;
        }

        private void OnItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is WinUI.NavigationViewItem selectedItem)
            {
                var pageType = selectedItem.GetValue(NavHelper.NavigateToProperty) as Type;
                NavigationService.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void OnBackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            NavigationService.GoBack();
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = NavigationService.GoBack();
            args.Handled = result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));



        // New changes


        private void NavigationViewControl_PaneClosing(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs args)
        {
            UpdateAppTitleMargin(sender);
        }

        private void NavigationViewControl_PaneOpened(Microsoft.UI.Xaml.Controls.NavigationView sender, object args)
        {
            UpdateAppTitleMargin(sender);
        }

        public string GetAppTitleFromSystem()
        {
            return Windows.ApplicationModel.Package.Current.DisplayName;
        }

        private void NavigationViewControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {
            Thickness currMargin = AppTitleBar.Margin;
            if (sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
            {
                AppTitleBar.Margin = new Thickness((sender.CompactPaneLength * 2), currMargin.Top, currMargin.Right, currMargin.Bottom);

            }
            else
            {
                AppTitleBar.Margin = new Thickness(sender.CompactPaneLength, currMargin.Top, currMargin.Right, currMargin.Bottom);
            }

            UpdateAppTitleMargin(sender);
            UpdateHeaderMargin(sender);
        }

        private void UpdateAppTitleMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {
            const int smallLeftIndent = 4, largeLeftIndent = 24;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            {
                AppTitle.TranslationTransition = new Vector3Transition();

                if ((sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Translation = new System.Numerics.Vector3(smallLeftIndent, 0, 0);
                }
                else
                {
                    AppTitle.Translation = new System.Numerics.Vector3(largeLeftIndent, 0, 0);
                }
            }
            else
            {
                Thickness currMargin = AppTitle.Margin;

                if ((sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Margin = new Thickness(smallLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
                else
                {
                    AppTitle.Margin = new Thickness(largeLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
            }
        }

        public PageHeader PageHeader
        {
            get
            {
                return UIHelper.GetDescendantsOfType<PageHeader>(navigationView).FirstOrDefault();
            }
        }

        private void UpdateHeaderMargin(Microsoft.UI.Xaml.Controls.NavigationView sender)
        {
            if (PageHeader != null)
            {
                if (sender.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
                {
                    Current.PageHeader.HeaderPadding = (Thickness)App.Current.Resources["PageHeaderMinimalPadding"];
                }
                else
                {
                    Current.PageHeader.HeaderPadding = (Thickness)App.Current.Resources["PageHeaderDefaultPadding"];
                }
            }
        }
    }
}
