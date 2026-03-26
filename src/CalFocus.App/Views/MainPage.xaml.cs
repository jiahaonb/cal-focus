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

            var selectedTag = _navigationStateService.LoadSelectedTag();
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
                "schedule" => typeof(SchedulePage),
                "reminder" => typeof(ReminderPage),
                "widgets" => typeof(WidgetCenterPage),
                "theme" => typeof(ThemePage),
                _ => typeof(SchedulePage)
            };

            if (ContentFrame.CurrentSourcePageType != targetPage)
            {
                _ = ContentFrame.Navigate(targetPage);
            }
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

            RootNav.SelectedItem = RootNav.MenuItems[0];
        }
    }
}
