using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.Screens;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Navigation;

/// <summary>
/// تنفيذ <see cref="INavigationService"/>.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly ScreenRegistry _registry;
    private readonly IScreenViewResolver _viewResolver;
    private readonly ILogger<NavigationService> _logger;

    private readonly Dictionary<string, OpenScreen> _openScreens = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<NavigationContext> _history = new();

    public NavigationService(
        IServiceProvider services,
        ScreenRegistry registry,
        IScreenViewResolver viewResolver,
        ILogger<NavigationService> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _viewResolver = viewResolver ?? throw new ArgumentNullException(nameof(viewResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==========================================================
    //  State
    // ==========================================================

    public NavigationContext? Current { get; private set; }

    public string? ActiveScreenId => Current?.TargetId;

    public IReadOnlyList<OpenScreenInfo> OpenScreens
        => _openScreens.Values.Select(ToInfo).ToList();

    /// <summary>وصول داخلي لسجلات الشاشات.</summary>
    public IReadOnlyCollection<OpenScreen> OpenScreenRecords => _openScreens.Values;

    // ==========================================================
    //  Events
    // ==========================================================

    public event EventHandler<NavigationContext>? Navigating;
    public event EventHandler<NavigationContext>? Navigated;
    public event EventHandler<OpenScreenInfo>? ScreenOpened;
    public event EventHandler<OpenScreenInfo>? ScreenClosed;
    public event EventHandler<string?>? ActiveScreenChanged;

    // ==========================================================
    //  Open Screen
    // ==========================================================

    public async Task<NavigationResult> OpenScreenAsync(string screenId, NavigationContext? context = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        var registration = _registry.Get(screenId);
        if (registration is null)
        {
            _logger.LogWarning("Screen '{ScreenId}' is not registered.", screenId);
            return NavigationResult.Fail($"Screen '{screenId}' is not registered.", screenId);
        }

        context ??= new NavigationContext(screenId, IsMultiOpen: registration.SupportsMultiOpen);

        Navigating?.Invoke(this, context);

        // إن كانت الشاشة مفتوحة مسبقًا (ولم تكن multi-open)
        if (!registration.SupportsMultiOpen && _openScreens.TryGetValue(screenId, out var existing))
        {
            await ActivateExistingAsync(existing, context);
            return NavigationResult.Ok(screenId);
        }

        // أنشئ ViewModel
        var viewModel = _services.GetService(registration.ViewModelType);
        if (viewModel is null)
        {
            _logger.LogError("Could not resolve ViewModel type {Type}", registration.ViewModelType);
            return NavigationResult.Fail($"Could not resolve ViewModel for screen '{screenId}'.", screenId);
        }

        // ربط الخدمات
        if (viewModel is IAppAware aware && viewModel is not Base.BaseViewModel)
        {
            aware.AttachServices(_services);
        }

        // OnActivated
        if (viewModel is IScreenAware screenAware)
        {
            var activationContext = new ScreenActivationContext(
                TargetLocation: context.TargetLocation,
                Parameters: context.Parameters,
                IsRestored: false);

            await screenAware.OnActivatedAsync(activationContext);
        }

        // تحميل البيانات
        if (viewModel is IDataAware dataAware)
        {
            await dataAware.LoadInitialAsync();
        }

        // حل الـ View (اختياري)
        var view = _viewResolver.ResolveView(viewModel);
        if (view is null)
        {
            _logger.LogDebug("No direct View resolved for {Type}; will rely on ViewTemplateHost.",
                viewModel.GetType().Name);
        }

        // خزّن الحالة
        var openScreen = new OpenScreen(
            ScreenId: screenId,
            InstanceKey: context.InstanceKey,
            ViewModel: viewModel,
            View: view,
            Context: context,
            Registration: registration);

        _openScreens[context.InstanceKey] = openScreen;

        // سجّل في MenuManager
        TryActivateMenu(screenId, viewModel);

        // حدّث Current
        Current = context;
        _history.Push(context);

        // ✅ أطلق الأحداث
        ScreenOpened?.Invoke(this, ToInfo(openScreen));
        Navigated?.Invoke(this, context);
        ActiveScreenChanged?.Invoke(this, screenId);

        _logger.LogInformation("Opened screen {ScreenId} (instance {InstanceKey})",
            screenId, context.InstanceKey);

        return NavigationResult.Ok(screenId);
    }

    // ==========================================================
    //  Activate (switch to)
    // ==========================================================

    public async Task<bool> ActivateScreenAsync(string screenId)
    {
        if (string.IsNullOrEmpty(screenId)) return false;

        var openScreen = _openScreens.Values.FirstOrDefault(s =>
            string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase));

        if (openScreen is null) return false;

        var context = new NavigationContext(screenId);
        await ActivateExistingAsync(openScreen, context);
        return true;
    }

    // ==========================================================
    //  Close
    // ==========================================================

    public async Task CloseScreenAsync(string screenId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        var toClose = _openScreens.Values
            .Where(s => string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toClose.Count == 0) return;

        foreach (var openScreen in toClose)
        {
            // تحقق قبل الإغلاق
            if (openScreen.ViewModel is IScreenAware screenAware)
            {
                bool canClose = await screenAware.OnClosingAsync();
                if (!canClose)
                {
                    _logger.LogInformation("Close cancelled for {ScreenId}", screenId);
                    continue;
                }
            }

            // OnDeactivated
            if (openScreen.ViewModel is IScreenAware sa2)
            {
                await sa2.OnDeactivatedAsync();
            }

            // إزالة
            _openScreens.Remove(openScreen.InstanceKey);

            // ✅ أطلق ScreenClosed
            ScreenClosed?.Invoke(this, ToInfo(openScreen));
        }

        // إلغاء تفعيل القائمة
        TryDeactivateMenu(screenId);

        // حدّث Current
        if (Current is not null && string.Equals(Current.TargetId, screenId, StringComparison.OrdinalIgnoreCase))
        {
            Current = _openScreens.Values.LastOrDefault()?.Context;
        }

        // ✅ أطلق ActiveScreenChanged
        ActiveScreenChanged?.Invoke(this, Current?.TargetId);

        _logger.LogInformation("Closed screen {ScreenId}", screenId);
    }

    public async Task CloseAllExceptAsync(string screenId)
    {
        var toClose = _openScreens.Values
            .Where(s => !string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ScreenId)
            .Distinct()
            .ToList();

        foreach (var id in toClose)
            await CloseScreenAsync(id);
    }

    public async Task CloseAllAsync()
    {
        var toClose = _openScreens.Values
            .Select(s => s.ScreenId)
            .Distinct()
            .ToList();

        foreach (var id in toClose)
            await CloseScreenAsync(id);
    }

    // ==========================================================
    //  Reports / URLs
    // ==========================================================

    public Task<NavigationResult> OpenReportAsync(
        string reportId,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        _logger.LogWarning("OpenReportAsync is not implemented yet (reportId: {ReportId})", reportId);
        return Task.FromResult(NavigationResult.Fail("Reports not implemented yet.", reportId));
    }

    public Task OpenUrlAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL: {Url}", url);
        }

        return Task.CompletedTask;
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    public OpenScreen? GetOpenScreen(string screenId)
        => _openScreens.Values.FirstOrDefault(s =>
            string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase));

    // ==========================================================
    //  Internal
    // ==========================================================

    private OpenScreenInfo ToInfo(OpenScreen openScreen)
    {
        var reg = openScreen.Registration;
        var isActive = Current is not null
            && string.Equals(Current.TargetId, openScreen.ScreenId, StringComparison.OrdinalIgnoreCase);

        return new OpenScreenInfo(
            ScreenId: openScreen.ScreenId,
            InstanceKey: openScreen.InstanceKey,
            Title: reg.Title ?? openScreen.ScreenId,
            Icon: reg.Icon,
            IsActive: isActive,
            OpenedAt: DateTime.UtcNow);
    }

    private async Task ActivateExistingAsync(OpenScreen existing, NavigationContext context)
    {
        if (existing.ViewModel is IScreenAware screenAware)
        {
            var activationContext = new ScreenActivationContext(
                TargetLocation: context.TargetLocation,
                Parameters: context.Parameters,
                IsRestored: false);

            await screenAware.OnActivatedAsync(activationContext);
        }

        TryActivateMenu(existing.ScreenId, existing.ViewModel);

        Current = context;
        _history.Push(context);

        // ✅ أطلق الأحداث
        Navigated?.Invoke(this, context);
        ActiveScreenChanged?.Invoke(this, context.TargetId);
    }

    private void TryActivateMenu(string screenId, object viewModel)
    {
        try
        {
            var menuManager = _services.GetService<IMenuManager>();
            if (menuManager is null) return;

            // ✅ إن كان ViewModel ينفّذ IMenuAware، سجّل الـ factory تلقائيًا
            if (viewModel is IMenuAware menuAware)
            {
                menuManager.RegisterScreen(screenId, ctx => menuAware.BuildMenu(ctx));
            }

            menuManager.ActivateScreen(screenId, viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to activate menu for screen {ScreenId}", screenId);
        }
    }

    private void TryDeactivateMenu(string screenId)
    {
        try
        {
            var menuManager = _services.GetService<IMenuManager>();
            menuManager?.DeactivateScreen(screenId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deactivate menu for screen {ScreenId}", screenId);
        }
    }
}

/// <summary>حالة شاشة مفتوحة (داخلي).</summary>
public sealed record OpenScreen(
    string ScreenId,
    string InstanceKey,
    object ViewModel,
    object? View,
    NavigationContext Context,
    ScreenRegistration Registration);
