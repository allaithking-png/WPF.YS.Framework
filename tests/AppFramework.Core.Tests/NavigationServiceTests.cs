using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.Screens;
using AppFramework.Core.Navigation;
using AppFramework.Abstractions.Services;      // ← أضف هذا
using Microsoft.Extensions.Logging;   // ← أضف
using Xunit;

namespace AppFramework.Core.Tests;

public class NavigationServiceTests
{
    // =============== Test Doubles ===============

    private sealed class TestViewModel : IAppAware
    {
        public IServiceProvider? Services { get; private set; }
        public int ActivatedCount { get; private set; }
        public int ClosingCount { get; private set; }
        public bool CancelClose { get; set; }

        public void AttachServices(IServiceProvider services) => Services = services;

        public void Activated() => ActivatedCount++;
        public void RequestClose() => ClosingCount++;
    }

    private sealed class TestScreenViewModel : IAppAware, IScreenAware
    {
        public string ScreenId { get; set; } = "Test";
        public string ScreenTitle { get; set; } = "Test Screen";
        public IServiceProvider? Services { get; private set; }
        public int Activated { get; private set; }
        public int Deactivated { get; private set; }
        public bool AllowClose { get; set; } = true;

        public void AttachServices(IServiceProvider services) => Services = services;
        public Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
        {
            Activated++;
            return Task.CompletedTask;
        }
        public Task OnDeactivatedAsync(CancellationToken ct = default)
        {
            Deactivated++;
            return Task.CompletedTask;
        }
        public Task<bool> OnClosingAsync(CancellationToken ct = default) => Task.FromResult(AllowClose);
    }

    private sealed class FakeViewResolver : IScreenViewResolver
    {
        public Dictionary<Type, Type> Mappings { get; } = new();
        public object? LastView { get; private set; }

        public object? ResolveView(object viewModel)
        {
            LastView = new object();
            return LastView;
        }

        public void RegisterMapping(Type viewModelType, Type viewType)
            => Mappings[viewModelType] = viewType;
    }

    // =============== Helpers ===============

    private static (NavigationService svc, FakeViewResolver resolver, ServiceProvider provider)
        CreateService(List<ScreenRegistration> registrations)
    {
        var services = new ServiceCollection();
// ✅ سجّل Logging — هذا هو الحل
    services.AddLogging();
        // سجّل ScreenRegistration كـ Singleton (كما يفعل AssemblyScanner)
        foreach (var reg in registrations)
            services.AddSingleton(reg);

        // سجّل TestViewModels كـ Transient
        foreach (var reg in registrations)
            services.AddTransient(reg.ViewModelType);

        // سجّل ScreenRegistry + Resolver
        services.AddSingleton<ScreenRegistry>();
        var resolver = new FakeViewResolver();
        services.AddSingleton<IScreenViewResolver>(resolver);
        services.AddSingleton<INavigationService, NavigationService>();

        var provider = services.BuildServiceProvider();
        var svc = (NavigationService)provider.GetRequiredService<INavigationService>();

        return (svc, resolver, provider);
    }

    // =============== Tests ===============

    [Fact]
    public async Task OpenScreenAsync_WithRegisteredScreen_ReturnsOk()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });

        var result = await svc.OpenScreenAsync("Test");

        result.Success.Should().BeTrue();
        result.TargetId.Should().Be("Test");
    }

    [Fact]
    public async Task OpenScreenAsync_WithUnregisteredScreen_ReturnsFail()
    {
        var (svc, _, _) = CreateService(new List<ScreenRegistration>());

        var result = await svc.OpenScreenAsync("Missing");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task OpenScreenAsync_CallsAttachServices()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });

        await svc.OpenScreenAsync("Test");

        var open = svc.GetOpenScreen("Test");
        var vm = (TestScreenViewModel)open!.ViewModel;
        vm.Services.Should().NotBeNull();
    }

    [Fact]
    public async Task OpenScreenAsync_CallsOnActivated()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });

        await svc.OpenScreenAsync("Test");

        var open = svc.GetOpenScreen("Test");
        var vm = (TestScreenViewModel)open!.ViewModel;
        vm.Activated.Should().Be(1);
    }

    [Fact]
    public async Task OpenScreenAsync_FiresNavigatingAndNavigated()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });
        var navigating = 0;
        var navigated = 0;
        svc.Navigating += (_, _) => navigating++;
        svc.Navigated += (_, _) => navigated++;

        await svc.OpenScreenAsync("Test");

        navigating.Should().Be(1);
        navigated.Should().Be(1);
    }

    [Fact]
    public async Task OpenScreenAsync_WithoutMultiOpen_ReusesInstance()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel), SupportsMultiOpen: false);
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });

        await svc.OpenScreenAsync("Test");
        var first = svc.GetOpenScreen("Test")!.ViewModel;

        await svc.OpenScreenAsync("Test");
        var second = svc.GetOpenScreen("Test")!.ViewModel;

        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task CloseScreenAsync_CallsOnClosing()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });
        await svc.OpenScreenAsync("Test");
        var open = svc.GetOpenScreen("Test");
        var vm = (TestScreenViewModel)open!.ViewModel;

        await svc.CloseScreenAsync("Test");

        vm.Deactivated.Should().Be(1);
        svc.GetOpenScreen("Test").Should().BeNull();
    }

    [Fact]
    public async Task CloseScreenAsync_WhenClosingCancelled_KeepsScreen()
    {
        var reg = new ScreenRegistration("Test", "Test", typeof(TestScreenViewModel));
        var (svc, _, _) = CreateService(new List<ScreenRegistration> { reg });
        await svc.OpenScreenAsync("Test");
        var open = svc.GetOpenScreen("Test");
        var vm = (TestScreenViewModel)open!.ViewModel;
        vm.AllowClose = false;

        await svc.CloseScreenAsync("Test");

        svc.GetOpenScreen("Test").Should().NotBeNull();
    }

    [Fact]
    public async Task OpenUrlAsync_DoesNotThrow()
    {
        var (svc, _, _) = CreateService(new List<ScreenRegistration>());
        await svc.OpenUrlAsync("https://example.com");
        // لا شيء للتحقق — فقط لا يرمي
    }
}