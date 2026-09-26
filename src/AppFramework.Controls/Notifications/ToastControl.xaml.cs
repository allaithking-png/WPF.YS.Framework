using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Notifications;

public partial class ToastControl : UserControl
{
    private readonly List<ToastActionVm> _actions = new();

    public ToastControl()
    {
        InitializeComponent();
    }

    public event EventHandler? Closed;

    public void Configure(
        string title,
        string message,
        ToastSeverity severity,
        IReadOnlyList<NotificationAction>? actions,
        Action<NotificationAction>? onActionInvoked)
    {
        PART_Title.Text = title;
        PART_Message.Text = message;

        var (icon, color) = severity switch
        {
            ToastSeverity.Success => ("✓", "#4CAF50"),
            ToastSeverity.Warning => ("⚠", "#FFC107"),
            ToastSeverity.Error => ("✕", "#E53935"),
            _ => ("ℹ", "#1976D2")
        };

        PART_Icon.Text = icon;
        PART_IconBorder.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));

        _actions.Clear();
        if (actions is not null)
        {
            foreach (var a in actions)
            {
                var vm = new ToastActionVm(a);
                vm.Invoked += (_, action) =>
                {
                    onActionInvoked?.Invoke(action);
                    Closed?.Invoke(this, EventArgs.Empty);
                };
                _actions.Add(vm);
            }
        }
        PART_Actions.ItemsSource = _actions;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
        => Closed?.Invoke(this, EventArgs.Empty);
}

public enum ToastSeverity { Info, Success, Warning, Error }

public sealed class ToastActionVm
{
    public string Label { get; }
    public NotificationAction Action { get; }
    public RelayCommand ClickCommand { get; }

    public event EventHandler<NotificationAction>? Invoked;

    public ToastActionVm(NotificationAction action)
    {
        Action = action;
        Label = action.Label;
        ClickCommand = new RelayCommand(() => Invoked?.Invoke(this, action));
    }
}

// Helper simple RelayCommand
public sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
