# -*- coding: utf-8 -*-
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
    
    <ScrollViewer Padding="40" VerticalScrollBarVisibility="Auto">
        <Grid ColumnSpacing="24" MaxWidth="1000" HorizontalAlignment="Left">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="400" />
                <ColumnDefinition Width="400" />
            </Grid.ColumnDefinitions>
            
            <!-- Todos -->
            <StackPanel Grid.Column="0" Spacing="16">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBlock Text="待办事项" FontSize="20" FontWeight="SemiBold" Foreground="{ThemeResource AppTitleBrush}" VerticalAlignment="Center" />
                    <Border Background="{ThemeResource AppBrandBrush}" Opacity="0.8" CornerRadius="4" Padding="6,2">
                        <TextBlock x:Name="TodoCountText" FontSize="12" Foreground="White" FontWeight="Bold" />
                    </Border>
                </StackPanel>
                <ListView x:Name="TodoList" SelectionMode="None" IsItemClickEnabled="False">
                    <ListView.ItemContainerStyle>
                        <Style TargetType="ListViewItem">
                            <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                            <Setter Property="Padding" Value="0" />
                            <Setter Property="Margin" Value="0,0,0,8" />
                        </Style>
                    </ListView.ItemContainerStyle>
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <Border Background="{ThemeResource AppCardBrush}" CornerRadius="8" Padding="16" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="*" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>
                                    <Button Content="&#xE10B;" FontFamily="Segoe MDL2 Assets" Background="Transparent" BorderThickness="0" Foreground="{ThemeResource AppBrandBrush}" Click="OnCompleteTodoClick" Tag="{Binding Id}" VerticalAlignment="Center" />
                                    <StackPanel Grid.Column="1" Margin="12,0,0,0" VerticalAlignment="Center">
                                        <TextBlock Text="{Binding Title}" FontSize="16" Foreground="{ThemeResource AppTextBrush}" FontWeight="Medium" />
                                        <TextBlock Text="{Binding Description}" FontSize="12" Foreground="{ThemeResource AppMutedBrush}" TextTrimming="CharacterEllipsis" />
                                    </StackPanel>
                                    <Button Grid.Column="2" Content="&#xE107;" FontFamily="Segoe MDL2 Assets" Background="Transparent" BorderThickness="0" Foreground="#EF4444" Click="OnDeleteTodoClick" Tag="{Binding Id}" VerticalAlignment="Center" />
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </StackPanel>

            <!-- Reminders -->
            <StackPanel Grid.Column="1" Spacing="16">
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <TextBlock Text="重要提醒" FontSize="20" FontWeight="SemiBold" Foreground="{ThemeResource AppTitleBrush}" VerticalAlignment="Center" />
                    <Border Background="{ThemeResource AppBrandBrush}" Opacity="0.8" CornerRadius="4" Padding="6,2">
                        <TextBlock x:Name="ReminderCountText" FontSize="12" Foreground="White" FontWeight="Bold" />
                    </Border>
                </StackPanel>
                <ListView x:Name="ReminderList" SelectionMode="None" IsItemClickEnabled="False">
                    <ListView.ItemContainerStyle>
                        <Style TargetType="ListViewItem">
                            <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                            <Setter Property="Padding" Value="0" />
                            <Setter Property="Margin" Value="0,0,0,8" />
                        </Style>
                    </ListView.ItemContainerStyle>
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <Border Background="{ThemeResource AppCardBrush}" CornerRadius="8" Padding="16" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="*" />
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>
                                    <TextBlock Text="&#xEA8F;" FontFamily="Segoe MDL2 Assets" Foreground="#F59E0B" FontSize="20" VerticalAlignment="Center" />
                                    <StackPanel Grid.Column="1" Margin="16,0,0,0" VerticalAlignment="Center">
                                        <TextBlock Text="{Binding Title}" FontSize="16" Foreground="{ThemeResource AppTextBrush}" FontWeight="Medium" />
                                        <TextBlock Text="{Binding Time}" FontSize="12" Foreground="{ThemeResource AppMutedBrush}" />
                                    </StackPanel>
                                    <Button Grid.Column="2" Content="&#xE70F;" FontFamily="Segoe MDL2 Assets" Background="Transparent" BorderThickness="0" Foreground="{ThemeResource AppBrandBrush}" Click="OnEditReminderClick" Tag="{Binding Id}" VerticalAlignment="Center" Margin="0,0,4,0"/>
                                    <Button Grid.Column="3" Content="&#xE107;" FontFamily="Segoe MDL2 Assets" Background="Transparent" BorderThickness="0" Foreground="#EF4444" Click="OnDeleteReminderClick" Tag="{Binding Id}" VerticalAlignment="Center" />
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </StackPanel>
        </Grid>
    </ScrollViewer>
</Page>''')

print("Fixed TodoReminderPage")
