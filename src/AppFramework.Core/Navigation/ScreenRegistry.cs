using System;
using System.Collections.Generic;
using AppFramework.Abstractions.Models.Screens;

namespace AppFramework.Core.Navigation;

/// <summary>
/// سجل مركزي لكل الشاشات المسجّلة في النظام.
/// </summary>
public sealed class ScreenRegistry
{
    private readonly Dictionary<string, ScreenRegistration> _screens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// يُنشئ السجل من قائمة ScreenRegistration المسجّلة في DI.
    /// </summary>
    public ScreenRegistry(IEnumerable<ScreenRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        foreach (var reg in registrations) Register(reg);
    }

    public int Count => _screens.Count;

    public void Register(ScreenRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        _screens[registration.Id] = registration;
    }

    public void RegisterRange(IEnumerable<ScreenRegistration> registrations)
    {
        foreach (var reg in registrations) Register(reg);
    }

    public bool Contains(string screenId) => _screens.ContainsKey(screenId);

    public ScreenRegistration? Get(string screenId)
        => _screens.TryGetValue(screenId, out var reg) ? reg : null;

    public IReadOnlyCollection<ScreenRegistration> All => _screens.Values;

    public IEnumerable<ScreenRegistration> GetByCategory(string category)
    {
        foreach (var reg in _screens.Values)
        {
            if (string.Equals(reg.Category, category, StringComparison.OrdinalIgnoreCase))
                yield return reg;
        }
    }
}