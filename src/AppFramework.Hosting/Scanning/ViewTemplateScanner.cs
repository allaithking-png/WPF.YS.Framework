using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Hosting.Scanning;

/// <summary>
/// يمسح Assemblies ويكتشف [ViewTemplate] ويسجّلها في IViewTemplateRegistry.
/// </summary>
/// <remarks>
/// هذا الـ scanner لا يُسجّل خدمة، بل يُنتج قائمة (Type → Descriptors)
/// تُطبَّق لاحقًا على IViewTemplateRegistry عند الإقلاع (في AppHost).
/// </remarks>
internal sealed class ViewTemplateScanner
{
    private readonly ILogger? _logger;

    // Static: لأن القائمة تُقرأ لاحقًا عند الإقلاع
    private static readonly List<(Type ViewModelType, ViewTemplateDescriptor[] Templates)> _pendingRegistrations = new();

    public ViewTemplateScanner(IServiceCollection services, ILogger? logger)
    {
        _ = services;   // محفوظ للتوافق مع API — لا نُسجّل هنا
        _logger = logger;
    }

    public void Scan(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            ScanAssembly(assembly);
    }

    private void ScanAssembly(Assembly assembly)
    {
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type is null || type.IsAbstract || type.IsInterface) continue;

            var attrs = type.GetCustomAttributes<ViewTemplateAttribute>(inherit: false).ToArray();
            if (attrs.Length == 0) continue;

            var descriptors = attrs.Select(a => new ViewTemplateDescriptor
            {
                Mode = a.Mode,
                ViewType = a.ViewType,
                Title = string.IsNullOrEmpty(a.Title) ? a.Mode.ToString() : a.Title,
                Icon = a.Icon,
                IsDefault = a.IsDefault,
                Category = a.IsDefault ? ViewModeCategory.Default : ViewModeCategory.Extended
            }).ToArray();

            lock (_pendingRegistrations)
            {
                _pendingRegistrations.Add((type, descriptors));
            }

            _logger?.LogDebug("View templates queued for {Type}: {Count}",
                type.Name, descriptors.Length);
        }
    }

    /// <summary>يُستدعى من AppHost بعد بناء الـ provider لتسجيل القوالب فعليًا.</summary>
    public static void ApplyTo(IServiceProvider services)
    {
        var registry = services.GetService<AppFramework.Abstractions.Services.IViewTemplateRegistry>();
        if (registry is null) return;

        lock (_pendingRegistrations)
        {
            foreach (var (vmType, descriptors) in _pendingRegistrations)
            {
                registry.Register(vmType, descriptors);
            }
        }
    }
}
