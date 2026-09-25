using System;
using System.Collections.Generic;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Controls.Menus;

/// <summary>
/// ط³ط¬ظ„ ظ…ط²ظˆظ‘ط¯ظٹ ط§ظ„ظ‚ظˆط§ط¦ظ….
/// </summary>
public sealed class MenuProviderRegistry
{
    private readonly Dictionary<MenuHost, IMenuProvider> _providers = new();

    /// <summary>طھط³ط¬ظٹظ„ ظ…ط²ظˆظ‘ط¯.</summary>
    public void Register(IMenuProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _providers[provider.Host] = provider;
    }

    /// <summary>ط§ظ„ط­طµظˆظ„ ط¹ظ„ظ‰ ظ…ط²ظˆظ‘ط¯ ظ„ظ†ظˆط¹ ظ…ط¹ظٹظ‘ظ†.</summary>
    public IMenuProvider? Get(MenuHost host)
        => _providers.TryGetValue(host, out var p) ? p : null;

    /// <summary>ظ‡ظ„ ظٹظˆط¬ط¯ ظ…ط²ظˆظ‘ط¯ ظ„ظ‡ط°ط§ ط§ظ„ظ†ظˆط¹طں</summary>
    public bool Supports(MenuHost host) => _providers.ContainsKey(host);

    /// <summary>طھط³ط¬ظٹظ„ ط§ظ„ظ…ط²ظˆظ‘ط¯ظٹظ† ط§ظ„ط§ظپطھط±ط§ط¶ظٹظٹظ†.</summary>
    public static MenuProviderRegistry CreateDefault()
    {
        var registry = new MenuProviderRegistry();
        registry.Register(new MenuBarProvider());
        registry.Register(new ToolbarProvider());
        registry.Register(new RibbonProvider());
        return registry;
    }
}
