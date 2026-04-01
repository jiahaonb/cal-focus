using CalFocus.App.Services;
using CalFocus.App.Views.Pages;

namespace CalFocus.App.Views
{
    public sealed partial class MainPage : Page
    {
        private readonly NavigationStateService _navigationStateService = new();
        private App CurrentApp => (App)Application.Current;

        public MainPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            var selectedTag = NormalizeTag(_navigationStateService.LoadSelectedTag());
            NavigateByTag(selectedTag);
            SelectNavItemByTag(selectedTag);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CurrentApp.NavigationRequested += OnNavigationRequested;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CurrentApp.NavigationRequested -= OnNavigationRequested;
            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
        }

        private void OnNavigationRequested(string tag)
        {
            var normalized = NormalizeTag(tag);
            NavigateByTag(normalized);
            _navigationStateService.SaveSelectedTag(normalized);
            SelectNavItemByTag(normalized);
        }

        private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer?.Tag is not string tag)
            {
                return;
            }

            NavigateByTag(tag);
            _navigationStateService.SaveSelectedTag(tag);
        }

        private void NavigateByTag(string tag)
        {
            var targetPage = tag switch
            {
                "home" => typeof(SchedulePage),
                "calendar" => typeof(CalendarSchedulePage),
                "todoreminder" => typeof(TodoReminderPage),
                "widgets" => typeof(WidgetCenterPage),
                "settings" => typeof(ThemePage),
                "helpabout" => typeof(HelpAboutPage),
                _ => typeof(SchedulePage)
            };

            if (ContentFrame.CurrentSourcePageType != targetPage)
            {
                _ = ContentFrame.Navigate(targetPage);
            }
        }

        private static string NormalizeTag(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return "home";
            }

            if (string.Equals(tag, "theme", StringComparison.OrdinalIgnoreCase))
            {
                return "settings";
            }

            if (string.Equals(tag, "schedule", StringComparison.OrdinalIgnoreCase))
            {
                return "home";
            }

            return tag switch
            {
                "home" or "calendar" or "todoreminder" or "widgets" or "settings" or "helpabout" => tag,
                _ => "home"
            };
        }

        private void SelectNavItemByTag(string tag)
        {
            foreach (var item in RootNav.MenuItems.OfType<NavigationViewItem>())
            {
                if (item.Tag?.ToString() == tag)
                {
                    RootNav.SelectedItem = item;
                    return;
                }
            }

            foreach (var item in RootNav.FooterMenuItems.OfType<NavigationViewItem>())
            {
                if (item.Tag?.ToString() == tag)
                {
                    RootNav.SelectedItem = item;
                    return;
                }
            }

            RootNav.SelectedItem = RootNav.MenuItems[0];
        }
    }
}
