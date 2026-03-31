# -*- coding: utf-8 -*-
with open(r'd:\Recently\cal-focus\src\CalFocus.App\Views\MainPage.xaml', 'w', encoding='utf-8') as f:
    f.write('''<?xml version="1.0" encoding="UTF-8" ?>
<Window
    x:Class="CalFocus.App.Views.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid Background="{ThemeResource AppBackgroundBrush}">
        <NavigationView x:Name="NavView" SelectionChanged="OnNavViewSelectionChanged" Background="{ThemeResource AppCardBrush}" OpenPaneLength="220" IsBackButtonVisible="Collapsed">
            <NavigationView.MenuItems>
                <NavigationViewItem Icon="Home" Content="主页" Tag="Home" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Calendar" Content="日历日程" Tag="CalendarSchedule" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Accept" Content="待办提醒" Tag="TodoReminder" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Go" Content="小组件" Tag="WidgetCenter" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Setting" Content="主题外观" Tag="Theme" Foreground="{ThemeResource AppTextBrush}" />
            </NavigationView.MenuItems>

            <Frame x:Name="ContentFrame" Padding="0,0,0,0" Background="{ThemeResource AppBackgroundBrush}" />
        </NavigationView>
    </Grid>
</Window>''')

with open(r'd:\Recently\cal-focus\src\CalFocus.App\Views\Pages\TodoReminderPage.xaml', 'w', encoding='utf-8') as f:
    f.write('''<?xml version="1.0" encoding="UTF-8" ?>
<Page
    x:Class="CalFocus.App.Views.Pages.TodoReminderPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    mc:Ignorable="d"
    Background="{ThemeResource AppBackgroundBrush}">
    
    <Grid Padding="40">
        <StackPanel Spacing="32" MaxWidth="800" HorizontalAlignment="Left">
            
            <StackPanel Spacing="8">
                <TextBlock FontSize="32" FontWeight="SemiBold" Text="Todo / 待办提醒" Foreground="{ThemeResource AppTitleBrush}" CharacterSpacing="5" />
                <TextBlock FontSize="14" FontWeight="Medium" Foreground="{ThemeResource AppMutedBrush}" Text="快速记录您的待办事项和提醒，保持任务追踪畅通无阻" />
            </StackPanel>

            <Border Padding="32" CornerRadius="16" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}" Background="{ThemeResource AppCardBrush}">
                <StackPanel Spacing="24" HorizontalAlignment="Center" VerticalAlignment="Center">
                    <TextBlock Text="&#xE154;" FontFamily="Segoe MDL2 Assets" FontSize="48" Foreground="{ThemeResource AppMutedBrush}" HorizontalAlignment="Center" Opacity="0.5" />
                    <TextBlock FontSize="18" FontWeight="SemiBold" Text="待办功能正在开发中..." Foreground="{ThemeResource AppTitleBrush}" HorizontalAlignment="Center" />
                </StackPanel>
            </Border>

        </StackPanel>
    </Grid>
</Page>''')

print("done utf8")
