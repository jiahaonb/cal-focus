using CalFocus.App.Services;

namespace CalFocus.App.Views.Pages;

public sealed partial class TodoReminderPage : Page
{
    private App CurrentApp => (App)Application.Current;

    public TodoReminderPage()
    {
        InitializeComponent();

        // TODO: 待办和提醒服务初始化
        RefreshTodos();
        RefreshReminders();
    }

    private void RefreshTodos()
    {
        // TODO: 从 TodoService 获取待办列表
        var todos = new List<TodoEntry>();
        TodoList.ItemsSource = todos;
        TodoCountText.Text = $"共 {todos.Count} 项";
    }

    private void RefreshReminders()
    {
        // TODO: 从 ReminderService 获取提醒列表
        var reminders = new List<ReminderEntry>();
        ReminderList.ItemsSource = reminders;
        ReminderCountText.Text = $"共 {reminders.Count} 项";
    }

    private void OnCompleteTodoClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        // TODO: 实现完成待办逻辑
    }

    private void OnDeleteTodoClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        // TODO: 实现删除待办逻辑
    }

    private void OnEditReminderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        // TODO: 实现编辑提醒逻辑
    }

    private void OnDeleteReminderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        // TODO: 实现删除提醒逻辑
    }
}
