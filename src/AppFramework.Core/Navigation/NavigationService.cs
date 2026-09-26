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
/// يدير فتح/إغلاق الشاشات، دورة حياتها، والتنقل بينها.
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

    public NavigationContext? Current { get; private set; }

    public event EventHandler<NavigationContext>? Navigating;
    public event EventHandler<NavigationContext>? Navigated;

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

        // 1) حدث Navigating — يمكن إلغاؤه
        Navigating?.Invoke(this, context);

        // 2) هل الشاشة مفتوحة من قبل؟
        if (!registration.SupportsMultiOpen && _openScreens.TryGetValue(screenId, out var existing))
        {
            // فعّلها فقط
            await ActivateExistingAsync(existing, context);
            return NavigationResult.Ok(screenId);
        }

        // 3) إنشاء ViewModel
        var viewModel = _services.GetService(registration.ViewModelType);
        if (viewModel is null)
        {
            _logger.LogError("Could not resolve ViewModel type {Type}", registration.ViewModelType);
            return NavigationResult.Fail($"Could not resolve ViewModel for screen '{screenId}'.", screenId);
        }

        // 4) ربط الخدمات
        if (viewModel is IAppAware aware && viewModel is not Base.BaseViewModel)
        {
            aware.AttachServices(_services);
        }

        // 5) استدعاء OnActivated
        if (viewModel is IScreenAware screenAware)
        {
            var activationContext = new ScreenActivationContext(
                TargetLocation: context.TargetLocation,
                Parameters: context.Parameters,
                IsRestored: false);

            await screenAware.OnActivatedAsync(activationContext);
        }

        // 6) تحميل البيانات الأولية
        if (viewModel is IDataAware dataAware)
        {
            await dataAware.LoadInitialAsync();
        }

        // 7) حل الـ View (اختياري — ViewTemplateHost قد يتولّى ذلك)
        var view = _viewResolver.ResolveView(viewModel);
        if (view is null)
        {
            // لا نُسقط — ViewTemplateHost سيعرض الـ View لاحقًا
            _logger.LogDebug("No direct View resolved for {Type}; will rely on ViewTemplateHost.",
                viewModel.GetType().Name);
        }

        // 8) تخزين الحالة
        var openScreen = new OpenScreen(
            ScreenId: screenId,
            InstanceKey: context.InstanceKey,
            ViewModel: viewModel,
            View: view,
            Context: context,
            Registration: registration);

        _openScreens[context.InstanceKey] = openScreen;

        // 9) تسجيل في MenuManager
        TryActivateMenu(screenId, viewModel);

        // 10) تحديد "الشاشة الحالية"
        Current = context;
        _history.Push(context);

        // 11) حدث Navigated
        Navigated?.Invoke(this, context);

        _logger.LogInformation("Opened screen {ScreenId} (instance {InstanceKey})", screenId, context.InstanceKey);
        return NavigationResult.Ok(screenId);
    }

    // ==========================================================
    //  Close Screen
    // ==========================================================

    public async Task CloseScreenAsync(string screenId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        var toClose = _openScreens.Values
            .Where(s => string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toClose.Count == 0)
        {
            _logger.LogDebug("CloseScreen: no open screen with id {ScreenId}", screenId);
            return;
        }

        foreach (var openScreen in toClose)
        {
            // 1) تحقق قبل الإغلاق
            if (openScreen.ViewModel is IScreenAware screenAware)
            {
                bool canClose = await screenAware.OnClosingAsync();
                if (!canClose)
                {
                    _logger.LogInformation("Close cancelled for {ScreenId}", screenId);
                    continue;
                }
            }

            // 2) OnDeactivated
            if (openScreen.ViewModel is IScreenAware sa2)
            {
                await sa2.OnDeactivatedAsync();
            }

            // 3) إزالة
            _openScreens.Remove(openScreen.InstanceKey);
        }

        // 4) إلغاء تفعيل القائمة
        TryDeactivateMenu(screenId);

        // 5) تحديث Current
        if (Current is not null && string.Equals(Current.TargetId, screenId, StringComparison.OrdinalIgnoreCase))
        {
            Current = _openScreens.Values.LastOrDefault()?.Context;
        }

        _logger.LogInformation("Closed screen {ScreenId}", screenId);
    }

    // ==========================================================
    //  Open Report (stub — لاحقًا في PR-06/PR-08)
    // ==========================================================

    public Task<NavigationResult> OpenReportAsync(
        string reportId,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        _logger.LogWarning("OpenReportAsync is not implemented yet (reportId: {ReportId})", reportId);
        return Task.FromResult(NavigationResult.Fail("Reports not implemented yet.", reportId));
    }

    // ==========================================================
    //  Open URL
    // ==========================================================

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
    //  Public helpers
    // ==========================================================

    /// <summary>كل الشاشات المفتوحة حاليًا.</summary>
    public IReadOnlyCollection<OpenScreen> OpenScreens => _openScreens.Values;

    /// <summary>البحث عن شاشة مفتوحة بمعرّفها.</summary>
    public OpenScreen? GetOpenScreen(string screenId)
        => _openScreens.Values.FirstOrDefault(s =>
            string.Equals(s.ScreenId, screenId, StringComparison.OrdinalIgnoreCase));

    // ==========================================================
    //  Internal
    // ==========================================================

    private async Task ActivateExistingAsync(OpenScreen existing, NavigationContext context)
    {
        // OnActivated مرة أخرى (يُستخدم لتحديث الـ target location مثلًا)
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

        Navigated?.Invoke(this, context);
    }

    private void TryActivateMenu(string screenId, object viewModel)
    {
        try
        {
            var menuManager = _services.GetService<IMenuManager>();
            menuManager?.ActivateScreen(screenId, viewModel);
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

/// <summary>حالة شاشة مفتوحة.</summary>
public sealed record OpenScreen(
    string ScreenId,
    string InstanceKey,
    object ViewModel,
    object? View,                   // ← nullable
    NavigationContext Context,
    ScreenRegistration Registration);
