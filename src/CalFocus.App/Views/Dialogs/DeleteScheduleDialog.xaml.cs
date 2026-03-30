using Microsoft.UI.Xaml.Controls;

namespace CalFocus.App.Views.Dialogs;

public enum DeleteStrategy
{
    ThisEventOnly,           // 仅删除此事件
    ThisAndFollowing,        // 删除此事件及以后的所有事件
    EntireSeries             // 删除整个系列
}

public sealed partial class DeleteScheduleDialog : ContentDialog
{
    public DeleteStrategy SelectedStrategy { get; set; } = DeleteStrategy.EntireSeries;

    public DeleteScheduleDialog()
    {
        InitializeComponent();
    }

    private void OnThisEventOnly_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.ThisEventOnly;
        Hide();
    }

    private void OnThisAndFollowing_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.ThisAndFollowing;
        Hide();
    }

    private void OnEntireSeries_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.EntireSeries;
        Hide();
    }
}
