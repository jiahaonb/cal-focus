# -*- coding: utf-8 -*-
with open(r'd:\Recently\cal-focus\src\CalFocus.App\Views\MainPage.xaml', 'w', encoding='utf-8') as f:
    f.write('''<?xml version="1.0" encoding="UTF-8" ?>
<Page
    x:Class="CalFocus.App.Views.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid Background="{ThemeResource AppBackgroundBrush}">
        <NavigationView x:Name="RootNav" SelectionChanged="OnSelectionChanged" Background="{ThemeResource AppCardBrush}" OpenPaneLength="220" IsBackButtonVisible="Collapsed">
            <NavigationView.MenuItems>
                <NavigationViewItem Icon="Home" Content="主页" Tag="home" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Calendar" Content="日历日程" Tag="calendar" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Accept" Content="待办提醒" Tag="todoreminder" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Go" Content="小组件" Tag="widgets" Foreground="{ThemeResource AppTextBrush}" />
                <NavigationViewItem Icon="Setting" Content="主题外观" Tag="theme" Foreground="{ThemeResource AppTextBrush}" />
            </NavigationView.MenuItems>

            <Frame x:Name="ContentFrame" Padding="0,0,0,0" Background="{ThemeResource AppBackgroundBrush}" />
        </NavigationView>
    </Grid>
</Page>''')
print("Fixed MainPage.xaml")