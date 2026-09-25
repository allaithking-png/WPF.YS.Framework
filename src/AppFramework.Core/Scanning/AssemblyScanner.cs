using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Screens;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Scanning;

/// <summary>
/// يمسح Assemblies ويكتشف الأنواع الموسومة بـ Attributes ويسجّلها تلقائيًا.
/// </summary>
public sealed class AssemblyScanner
{
    private readonly IServiceCollection _services;
    private readonly ILogger<AssemblyScanner>? _logger;
    private readonly RegistrationResult _result = new();

    public AssemblyScanner(IServiceCollection services, ILogger<AssemblyScanner>? logger = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger;
    }

    /// <summary>نتيجة آخر عملية مسح.</summary>
    public RegistrationResult Result => _result;

    /// <summary>يمسح مجموعة من الـ Assemblies.</summary>
    public RegistrationResult Scan(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }

        _logger?.LogInformation("AssemblyScanner: {Summary}", _result);
        return _result;
    }

    private void ScanAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            _logger?.LogWarning(ex, "Partial type load for assembly {Assembly}", assembly.FullName);
        }

        foreach (var type in types)
        {
            if (type is null || type.IsAbstract || type.IsInterface) continue;

            RegisterScreenIfMarked(type);
            RegisterDataSourceIfMarked(type);
            RegisterViewModelIfAware(type);
        }
    }

    private void RegisterScreenIfMarked(Type type)
    {
        var attr = type.GetCustomAttribute<ScreenAttribute>(inherit: false);
        if (attr is null) return;

        // سجّل النوع نفسه في DI
        _services.AddTransient(type);

        // سجّل بيانات الشاشة كـ Singleton ليقرأها الـ NavigationService
        var registration = new ScreenRegistration(
            Id: attr.Id,
            Title: attr.Title,
            ViewModelType: type,
            Icon: attr.Icon,
            Category: attr.Category,
            SupportsMultiOpen: attr.SupportsMultiOpen,
            RestoreOnStartup: attr.RestoreOnStartup,
            DataSourceKey: attr.DataSourceKey,
            Permissions: attr.Permissions);

        _services.AddSingleton(registration);
        _result.Screens.Add(attr.Id);

        _logger?.LogDebug("Registered screen: {Id} ({Type})", attr.Id, type.Name);
    }

    private void RegisterDataSourceIfMarked(Type type)
    {
        var attr = type.GetCustomAttribute<DataSourceAttribute>(inherit: false);
        if (attr is null) return;

        if (!typeof(IDataSource).IsAssignableFrom(type))
        {
            _logger?.LogWarning(
                "Type {Type} has [DataSource] but does not implement IDataSource.",
                type.FullName);
            return;
        }

        // سجّل النوع كـ Singleton + IDataSource
        _services.AddSingleton(type);
        _services.AddSingleton(typeof(IDataSource), sp =>
        {
            var src = (IDataSource)sp.GetRequiredService(type);

            // سجّلها في IDataService تلقائيًا
            var dataService = sp.GetService<IDataService>();
            dataService?.RegisterSource(src);

            return src;
        });

        _result.DataSources.Add(attr.Key);
        _logger?.LogDebug("Registered data source: {Key} ({Type})", attr.Key, type.Name);
    }

    private void RegisterViewModelIfAware(Type type)
    {
        // سجّل أي ViewModel ينفّذ IAppAware
        if (!typeof(IAppAware).IsAssignableFrom(type)) return;

        // لكن لا نُسجّل الأنواع التي تحمل [Screen] مرتين
        if (type.GetCustomAttribute<ScreenAttribute>() is not null) return;

        _services.AddTransient(type);
        _result.ViewModels.Add(type.FullName ?? type.Name);

        _logger?.LogDebug("Registered ViewModel: {Type}", type.Name);
    }
}