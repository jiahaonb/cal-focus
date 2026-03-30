using CalFocus.App.Services;
using CalFocus.App.Views.Pages;

namespace CalFocus.App.Views
{
    public sealed partial class MainPage : Page
    {
        private readonly NavigationStateService _navigationStateService = new();

        public MainPage()
        {
            InitializeComponent();

            var selectedTag = NormalizeTag(_navigationStateService.LoadSelectedTag());
            NavigateByTag(selectedTag);
            SelectNavItemByTag(selectedTag);
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
                "theme" => typeof(ThemePage),
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

            return tag switch
            {
                "home" or "calendar" or "todoreminder" or "widgets" or "theme" => tag,
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

            RootNav.SelectedItem = RootNav.MenuItems[1];
        }
    }
}
