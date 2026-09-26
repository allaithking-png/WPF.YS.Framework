using System;
using System.Windows.Controls;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;   // ServiceCollection
using Microsoft.Extensions.Logging;                // ILoggerFactory
using Microsoft.Extensions.Logging.Abstractions;   // NullLoggerFactory, NullLogger<T>
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Navigation;
using AppFramework.Core.ViewTemplates;
using Xunit;

namespace AppFramework.Core.Tests;

public class ViewTemplateHostTests
{
    // =============== Test Views ===============

    public sealed class TestGridView : UserControl { }
    public sealed class TestCardView : UserControl { }

    // =============== Test ViewModel ===============

    public sealed class TestViewModel { }

    // =============== Helpers ===============

    private static (ViewTemplateHost host, ViewTemplateRegistry registry, IServiceProvider services)
        CreateHost()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        var provider = services.BuildServiceProvider();

        var registry = new ViewTemplateRegistry(NullLogger<ViewTemplateRegistry>.Instance);

        var resolver = new FakeScreenViewResolver();
        var host = new ViewTemplateHost(
            registry,
            resolver,
            provider,
            NullLogger<ViewTemplateHost>.Instance);

        return (host, registry, provider);
    }

    private sealed class FakeScreenViewResolver : IScreenViewResolver
    {
        public object? ResolveView(object viewModel) => null;
        public void RegisterMapping(Type viewModelType, Type viewType) { }
    }

    // =============== Tests ===============

    [Fact]
    public void ShowInMode_WithRegisteredTemplate_RendersView()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView),
                    IsDefault = true
                });

            host.ShowInMode(new TestViewModel(), ViewMode.Grid, "Main");

            host.CurrentView.Should().BeOfType<TestGridView>();
            host.CurrentMode.Should().Be(ViewMode.Grid);
        });

    [Fact]
    public void ShowInMode_UnknownMode_UsesDefault()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView),
                    IsDefault = true
                });

            host.ShowInMode(new TestViewModel(), ViewMode.Card, "Main");

            host.CurrentMode.Should().Be(ViewMode.Grid);
            host.CurrentView.Should().BeOfType<TestGridView>();
        });

    [Fact]
    public void ShowInMode_NoTemplates_DoesNotThrow()
        => StaTestHelper.RunSta(() =>
        {
            var (host, _, _) = CreateHost();

            var act = () => host.ShowInMode(new TestViewModel(), ViewMode.Grid, "Main");
            act.Should().NotThrow();
            host.CurrentView.Should().BeNull();
        });

    [Fact]
    public void SwitchMode_ChangesToAnotherTemplate()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView),
                    IsDefault = true
                },
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Card,
                    ViewType = typeof(TestCardView)
                });

            host.ShowInMode(new TestViewModel(), ViewMode.Grid, "Main");
            host.SwitchMode(ViewMode.Card);

            host.CurrentMode.Should().Be(ViewMode.Card);
            host.CurrentView.Should().BeOfType<TestCardView>();
        });

    [Fact]
    public void SwitchMode_WithoutShow_DoesNothing()
        => StaTestHelper.RunSta(() =>
        {
            var (host, _, _) = CreateHost();
            var act = () => host.SwitchMode(ViewMode.Card);
            act.Should().NotThrow();
        });

    [Fact]
    public void ShowInMode_FiresViewRenderedEvent()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView)
                });

            ViewRenderedEventArgs? args = null;
            host.ViewRendered += (_, e) => args = e;

            host.ShowInMode(new TestViewModel(), ViewMode.Grid, "Main");

            args.Should().NotBeNull();
            args!.Mode.Should().Be(ViewMode.Grid);
            args.View.Should().BeOfType<TestGridView>();
        });

    [Fact]
    public void ShowInMode_FiresModeChangedEvent()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView)
                });

            ViewMode? received = null;
            host.ModeChanged += (_, m) => received = m;

            host.ShowInMode(new TestViewModel(), ViewMode.Grid, "Main");

            received.Should().Be(ViewMode.Grid);
        });

    [Fact]
    public void ShowInMode_SetsDataContextOnView()
        => StaTestHelper.RunSta(() =>
        {
            var (host, registry, _) = CreateHost();
            registry.Register(typeof(TestViewModel),
                new ViewTemplateDescriptor
                {
                    Mode = ViewMode.Grid,
                    ViewType = typeof(TestGridView)
                });

            var vm = new TestViewModel();
            host.ShowInMode(vm, ViewMode.Grid, "Main");

            host.CurrentView!.DataContext.Should().Be(vm);
        });
}
