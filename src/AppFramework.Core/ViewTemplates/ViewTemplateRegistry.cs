using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.ViewTemplates;

/// <summary>
/// تنفيذ <see cref="IViewTemplateRegistry"/>.
/// </summary>
public sealed class ViewTemplateRegistry : IViewTemplateRegistry
{
    private readonly ILogger<ViewTemplateRegistry> _logger;
    private readonly Dictionary<Type, List<ViewTemplateDescriptor>> _templates = new();
    private readonly object _lock = new();

    public ViewTemplateRegistry(ILogger<ViewTemplateRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<Type>? TemplatesChanged;

    // ==========================================================
    //  Register
    // ==========================================================

    public void Register(Type viewModelType, params ViewTemplateDescriptor[] templates)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        ArgumentNullException.ThrowIfNull(templates);

        if (templates.Length == 0) return;

        lock (_lock)
        {
            if (!_templates.TryGetValue(viewModelType, out var list))
            {
                list = new List<ViewTemplateDescriptor>();
                _templates[viewModelType] = list;
            }

            // أزل أي قوالب موجودة بنفس الـ Mode (idempotent)
            foreach (var t in templates)
            {
                list.RemoveAll(x => x.Mode == t.Mode);
            }

            // تحقق: فقط Default واحد
            var defaults = list.Concat(templates).Count(t => t.IsDefault);
            if (defaults > 1)
            {
                _logger.LogWarning(
                    "{Count} default templates for {VM}. Only the first will be used.",
                    defaults, viewModelType.Name);
            }

            list.AddRange(templates);
        }

        _logger.LogDebug("Registered {Count} templates for {VM}",
            templates.Length, viewModelType.Name);

        TemplatesChanged?.Invoke(this, viewModelType);
    }

    public void RegisterTemplate(Type viewModelType, ViewTemplateDescriptor template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Register(viewModelType, template);
    }

    public void Unregister(Type viewModelType, ViewMode mode)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        lock (_lock)
        {
            if (_templates.TryGetValue(viewModelType, out var list))
            {
                list.RemoveAll(t => t.Mode == mode);
                _logger.LogDebug("Unregistered {Mode} for {VM}", mode, viewModelType.Name);
            }
        }

        TemplatesChanged?.Invoke(this, viewModelType);
    }

    // ==========================================================
    //  Query
    // ==========================================================

    public IReadOnlyList<ViewTemplateDescriptor> GetTemplates(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        lock (_lock)
        {
            if (_templates.TryGetValue(viewModelType, out var list))
                return list.ToList();   // نسخة للقراءة الآمنة

            return Array.Empty<ViewTemplateDescriptor>();
        }
    }

    public IReadOnlyList<ViewTemplateDescriptor> GetTemplates<TViewModel>()
        => GetTemplates(typeof(TViewModel));

    public ViewTemplateDescriptor? GetDefault(Type viewModelType)
    {
        var list = GetTemplates(viewModelType);
        if (list.Count == 0) return null;

        // 1) أول قالب IsDefault=true
        var explicitDefault = list.FirstOrDefault(t => t.IsDefault);
        if (explicitDefault is not null) return explicitDefault;

        // 2) أول قالب من فئة Default
        var categoryDefault = list.FirstOrDefault(t => t.Category == ViewModeCategory.Default);
        if (categoryDefault is not null) return categoryDefault;

        // 3) أول قالب مسجّل
        return list[0];
    }

    public ViewTemplateDescriptor? GetByMode(Type viewModelType, ViewMode mode)
    {
        var list = GetTemplates(viewModelType);
        return list.FirstOrDefault(t => t.Mode == mode);
    }
}