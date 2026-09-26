using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Navigation;

namespace AppFramework.Core.ViewTemplates;

/// <summary>
/// تنفيذ <see cref="IViewTemplateHost"/>.
/// يدير العرض الحالي ويتيح التبديل بين طرق العرض.
/// </summary>
/// <remarks>
/// هذا التنفيذ يُعيد <see cref="FrameworkElement"/> جاهزًا للعرض،
/// ويحتفظ بالحالة داخل نفسه. الـ Shell يستمع لحدث <see cref="ModeChanged"/>
/// ويعرض الناتج في الـ ContentControl المناسب.
/// </remarks>
public sealed class ViewTemplateHost : IViewTemplateHost
{
    private readonly IViewTemplateRegistry _registry;
    private readonly IScreenViewResolver _viewResolver;
    private readonly IServiceProvider _services;
    private readonly ILogger<ViewTemplateHost> _logger;

    private object? _currentViewModel;
    private ViewTemplateDescriptor? _currentTemplate;

    public ViewTemplateHost(
        IViewTemplateRegistry registry,
        IScreenViewResolver viewResolver,
        IServiceProvider services,
        ILogger<ViewTemplateHost> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _viewResolver = viewResolver ?? throw new ArgumentNullException(nameof(viewResolver));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ViewMode CurrentMode => _currentTemplate?.Mode ?? ViewMode.Grid;

    /// <summary>العرض الحالي (آخر FrameworkElement مُنتَج).</summary>
    public FrameworkElement? CurrentView { get; private set; }

    /// <summary>الـ ViewModel الحالي.</summary>
    public object? CurrentViewModel => _currentViewModel;

    public event EventHandler<ViewMode>? ModeChanged;

    /// <summary>حدث يُطلَق عند توليد View جديد (للعرض في الـ Shell).</summary>
    public event EventHandler<ViewRenderedEventArgs>? ViewRendered;

    // ==========================================================
    //  Show
    // ==========================================================

    public void ShowInMode(object viewModel, ViewMode mode, string regionName)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _ = regionName;   // محفوظ للاستخدام المستقبلي (لو أضفنا Regions)

        _currentViewModel = viewModel;

        // 1) جرّب القالب المطلوب
        var template = _registry.GetByMode(viewModel.GetType(), mode);

        // 2) Fallback إلى الافتراضي
        if (template is null)
        {
            template = _registry.GetDefault(viewModel.GetType());
            if (template is not null && template.Mode != mode)
            {
                _logger.LogDebug(
                    "View mode {Requested} not registered for {VM}; using default {Default}.",
                    mode, viewModel.GetType().Name, template.Mode);
            }
        }

        // 3) لا يوجد أي قالب
        if (template is null)
        {
            _logger.LogWarning("No view template registered for {VM}", viewModel.GetType().Name);
            return;
        }

        SwitchToTemplate(template);
    }

    public void SwitchMode(ViewMode mode)
    {
        if (_currentViewModel is null)
        {
            _logger.LogDebug("SwitchMode ignored: no current ViewModel");
            return;
        }

        var template = _registry.GetByMode(_currentViewModel.GetType(), mode);
        if (template is null)
        {
            _logger.LogDebug("SwitchMode: {Mode} not available for {VM}",
                mode, _currentViewModel.GetType().Name);
            return;
        }

        SwitchToTemplate(template);
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private void SwitchToTemplate(ViewTemplateDescriptor template)
    {
        // ✅ null-check
        var viewModel = _currentViewModel;
        if (viewModel is null)
        {
            _logger.LogDebug("SwitchToTemplate ignored: no current ViewModel");
            return;
        }

        try
        {
            FrameworkElement? view = null;

            if (template.ViewType is not null)
            {
                view = CreateViewFromType(template.ViewType, viewModel);
            }
            else
            {
                var resolved = _viewResolver.ResolveView(viewModel);
                view = resolved as FrameworkElement;
            }

            if (view is null)
            {
                _logger.LogWarning(
                    "Could not create view for template {Mode} on {VM}",
                    template.Mode, viewModel.GetType().Name);
                return;
            }

            CurrentView = view;
            _currentTemplate = template;

            ViewRendered?.Invoke(this, new ViewRenderedEventArgs(view, template.Mode));
            ModeChanged?.Invoke(this, template.Mode);

            _logger.LogInformation(
                "Rendered {VM} in mode {Mode}",
                viewModel.GetType().Name, template.Mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to view mode {Mode}", template.Mode);
        }
    }
    private FrameworkElement? CreateViewFromType(Type viewType, object viewModel)
    {
        // 1) جرّب DI
        object? instance = null;
        try
        {
            instance = ActivatorUtilities.CreateInstance(_services, viewType);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DI resolution failed for {View}; using Activator.", viewType.Name);
            instance = Activator.CreateInstance(viewType);
        }

        if (instance is not FrameworkElement fe)
        {
            _logger.LogWarning("{View} is not a FrameworkElement", viewType.Name);
            return null;
        }

        // اربط DataContext
        fe.DataContext = viewModel;

        return fe;
    }
}

/// <summary>حدث ViewRendered.</summary>
public sealed class ViewRenderedEventArgs : EventArgs
{
    public FrameworkElement View { get; }
    public ViewMode Mode { get; }

    public ViewRenderedEventArgs(FrameworkElement view, ViewMode mode)
    {
        View = view;
        Mode = mode;
    }
}
