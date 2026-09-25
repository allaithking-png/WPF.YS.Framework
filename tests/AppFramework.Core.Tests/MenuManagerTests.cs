using System;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Core.Services;
using Xunit;

namespace AppFramework.Core.Tests;

public class MenuManagerTests
{
    private static MenuManager CreateManager() => new(NullLogger<MenuManager>.Instance);

    [Fact]
    public void ActivateScreen_WithRegisteredFactory_BuildsMenu()
    {
        // Arrange
        var manager = CreateManager();
        manager.RegisterScreen("Test", _ => new MenuDefinition
        {
            Host = MenuHost.MenuBar,
            Items = { new MenuItemDescriptor { Title = "File" } }
        });

        // Act
        manager.ActivateScreen("Test");

        // Assert
        manager.Current.Should().NotBeNull();
        manager.Current!.Items.Should().HaveCount(1);
        manager.Current.Items[0].Title.Should().Be("File");
    }

    [Fact]
    public void ActivateScreen_FiresMenuChangedEvent()
    {
        // Arrange
        var manager = CreateManager();
        manager.RegisterScreen("Test", _ => new MenuDefinition());
        MenuDefinition? received = null;
        manager.MenuChanged += (_, d) => received = d;

        // Act
        manager.ActivateScreen("Test");

        // Assert
        received.Should().NotBeNull();
    }

    [Fact]
    public void DeactivateScreen_ReturnsToPreviousMenu()
    {
        // Arrange
        var manager = CreateManager();
        manager.RegisterScreen("A", _ => new MenuDefinition
        {
            Items = { new MenuItemDescriptor { Title = "Menu-A" } }
        });
        manager.RegisterScreen("B", _ => new MenuDefinition
        {
            Items = { new MenuItemDescriptor { Title = "Menu-B" } }
        });

        manager.ActivateScreen("A");
        manager.ActivateScreen("B");

        // Act
        manager.DeactivateScreen("B");

        // Assert
        manager.Current!.Items[0].Title.Should().Be("Menu-A");
    }

    [Fact]
    public void ResolveCommand_WithRegisteredKey_ReturnsCommand()
    {
        // Arrange
        var manager = CreateManager();
        var cmd = new TestCommand();
        manager.RegisterCommand("Test.Save", cmd);

        // Act
        var resolved = manager.ResolveCommand("Test.Save");

        // Assert
        resolved.Should().BeSameAs(cmd);
    }

    [Fact]
    public void ResolveCommand_WithUnregisteredKey_ReturnsNull()
    {
        var manager = CreateManager();
        manager.ResolveCommand("Missing").Should().BeNull();
    }

    [Fact]
    public void BuildMenu_ResolvesCommandKeyToCommand()
    {
        // Arrange
        var manager = CreateManager();
        var cmd = new TestCommand();
        manager.RegisterCommand("Cmd.Save", cmd);
        manager.RegisterScreen("Test", _ => new MenuDefinition
        {
            Items =
            {
                new MenuItemDescriptor { Title = "Save", CommandKey = "Cmd.Save" }
            }
        });

        // Act
        manager.ActivateScreen("Test");

        // Assert
        manager.Current!.Items[0].Command.Should().BeSameAs(cmd);
    }

    [Fact]
    public void Refresh_RebuildsCurrentMenu()
    {
        // Arrange
        var manager = CreateManager();
        manager.RegisterScreen("Test", _ => new MenuDefinition());
        manager.ActivateScreen("Test");
        var firedCount = 0;
        manager.MenuChanged += (_, _) => firedCount++;

        // Act
        manager.Refresh();

        // Assert
        firedCount.Should().Be(1);
    }

    private sealed class TestCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}