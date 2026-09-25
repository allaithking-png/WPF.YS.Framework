using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppFramework.Core.Navigation;

/// <summary>
/// تنفيذ افتراضي لـ <see cref="IScreenViewResolver"/>.
/// يستخدم Convention: "FooViewModel" → "FooView" في نفس الـ Assembly.
/// </summary>
/// <remarks>
/// يمكن تجاوزه بتسجيلات صريحة عبر <see cref="RegisterMapping"/>.
/// </remarks>
public sealed class WpfScreenViewResolver : IScreenViewResolver
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WpfScreenViewResolver> _logger;
    private readonly Dictionary<Type, Type> _explicitMappings = new();

    public WpfScreenViewResolver(IServiceProvider services, ILogger<WpfScreenViewResolver> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RegisterMapping(Type viewModelType, Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        ArgumentNullException.ThrowIfNull(viewType);

        if (!typeof(FrameworkElement).IsAssignableFrom(viewType))
            throw new ArgumentException($"{viewType.Name} must derive from FrameworkElement.");

        _explicitMappings[viewModelType] = viewType;
        _logger.LogDebug("Explicit View mapping: {VM} → {View}", viewModelType.Name, viewType.Name);
    }

    public object? ResolveView(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var vmType = viewModel.GetType();

        // 1) تسجيل صريح
        if (_explicitMappings.TryGetValue(vmType, out var explicitViewType))
        {
            return CreateView(explicitViewType, viewModel);
        }

        // 2) حلّ عبر Convention
        var viewType = FindConventionView(vmType);
        if (viewType is null)
        {
            _logger.LogWarning("No View found for ViewModel {VM}", vmType.Name);
            return null;
        }

        return CreateView(viewType, viewModel);
    }

    private object? CreateView(Type viewType, object viewModel)
    {
        try
        {
            // حاول الحصول من DI أولًا (لو مُسجّل)
            var view = _services.GetService(viewType);
            if (view is null)
            {
                view = Activator.CreateInstance(viewType);
            }

            if (view is FrameworkElement fe)
            {
                fe.DataContext = viewModel;
            }

            return view;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create View {Type}", viewType.Name);
            return null;
        }
    }

    private Type? FindConventionView(Type viewModelType)
    {
        var assembly = viewModelType.Assembly;
        var vmName = viewModelType.Name;

        // "FooViewModel" → "FooView"
        // "FooVM" → "FooView"
        string? baseName = null;
        if (vmName.EndsWith("ViewModel", StringComparison.Ordinal))
            baseName = vmName[..^"ViewModel".Length];
        else if (vmName.EndsWith("VM", StringComparison.Ordinal))
            baseName = vmName[..^"VM".Length];

        if (string.IsNullOrEmpty(baseName)) return null;

        var candidateName = baseName + "View";

        // ابحث في نفس الـ Assembly
        return assembly.GetTypes().FirstOrDefault(t =>
            t.Name == candidateName &&
            typeof(FrameworkElement).IsAssignableFrom(t) &&
            !t.IsAbstract);
    }
}