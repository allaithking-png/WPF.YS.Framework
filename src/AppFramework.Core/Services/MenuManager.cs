using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Services;

/// <summary>
/// تنفيذ <see cref="IMenuManager"/>.
/// يدير قوائم الشاشات النشطة بترتيب Stack.
/// </summary>
public sealed class MenuManager : IMenuManager
{
    private readonly ILogger<MenuManager> _logger;
    private readonly Dictionary<string, Func<MenuContext, MenuDefinition>> _factories = new();
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly Stack<MenuContext> _contextStack = new();

    public MenuManager(ILogger<MenuManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MenuDefinition? Current { get; private set; }

    public MenuHost CurrentHost { get; set; } = MenuHost.MenuBar;

    public event EventHandler<MenuDefinition>? MenuChanged;

    // ========== Registration ==========

    public void RegisterScreen(string screenId, Func<MenuContext, MenuDefinition> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);
        ArgumentNullException.ThrowIfNull(factory);

        _factories[screenId] = factory;
        _logger.LogDebug("Menu factory registered for screen {ScreenId}", screenId);
    }

    public void RegisterCommand(string key, ICommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(command);

        _commands[key] = command;
        _logger.LogDebug("Command registered: {Key}", key);
    }

    public ICommand? ResolveCommand(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return _commands.TryGetValue(key, out var cmd) ? cmd : null;
    }

    // ========== Activation ==========

    public void ActivateScreen(string screenId, object? viewModel = null)
    {
        if (!_factories.TryGetValue(screenId, out var factory))
        {
            _logger.LogWarning("No menu factory for screen {ScreenId}", screenId);
            return;
        }

        var context = new MenuContext
        {
            ScreenId = screenId,
            ViewModel = viewModel,
            PreferredHost = CurrentHost
        };

        _contextStack.Push(context);
        BuildMenu(context);
    }

    public void DeactivateScreen(string screenId)
    {
        var remaining = _contextStack
            .Where(c => c.ScreenId != screenId)
            .Reverse()
            .ToList();

        _contextStack.Clear();
        foreach (var ctx in remaining)
            _contextStack.Push(ctx);

        if (_contextStack.Count > 0)
            BuildMenu(_contextStack.Peek());
        else
            ClearMenu();
    }

    public void Refresh()
    {
        if (_contextStack.Count > 0)
            BuildMenu(_contextStack.Peek());
    }

    // ========== Internal ==========

    private void BuildMenu(MenuContext context)
    {
        if (!_factories.TryGetValue(context.ScreenId, out var factory))
            return;

        MenuDefinition definition;
        try
        {
            definition = factory(context) ?? new MenuDefinition { Host = context.PreferredHost };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build menu for screen {ScreenId}", context.ScreenId);
            return;
        }

        ResolveCommands(definition.Items);

        Current = definition;
        MenuChanged?.Invoke(this, definition);
    }

    private void ClearMenu()
    {
        Current = null;
        var empty = new MenuDefinition { Host = CurrentHost };
        MenuChanged?.Invoke(this, empty);
    }

    private void ResolveCommands(IEnumerable<MenuItemDescriptor> items)
    {
        foreach (var item in items)
        {
            if (item.Command is null && !string.IsNullOrWhiteSpace(item.CommandKey))
            {
                item.Command = ResolveCommand(item.CommandKey);
            }

            if (item.Children.Count > 0)
            {
                ResolveCommands(item.Children);
            }
        }
    }
}