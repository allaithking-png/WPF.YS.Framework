using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AppFramework.Core.Navigation;

/// <summary>
/// تنفيذ <see cref="INavigationExplorer"/>.
/// </summary>
public sealed class NavigationExplorerService : INavigationExplorer, IDisposable
{
    private readonly IUserNavProfileStore _store;
    private readonly ILogger<NavigationExplorerService> _logger;

    private readonly List<HistoryEntry> _history = new();
    private readonly object _historyLock = new();
    private string _currentUserId = "default-user";

    private readonly DispatcherTimer _autoSaveTimer;
   
    /// <summary>الشاشة النشطة حاليًا.</summary>
    public string? ActiveScreenId { get; private set; }

    /// <summary>حدث عند تغيير الشاشة النشطة.</summary>
    public event EventHandler<string?>? ActiveScreenChanged;

    /// <summary>حدث عند طلب فتح عنصر (من الأيقونات).</summary>
    public event EventHandler<NavItem>? ItemOpenRequested;

    // ==========================================================
    //  In-memory state
    // ==========================================================

    /// <summary>المعروف من النظام (Screens + Reports + External).</summary>
    private readonly Dictionary<string, NavItem> _known = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>حالة المستخدم المُلتزمة.</summary>
    private UserNavProfile _profile = new();

    /// <summary>حالة المستخدم في staging (غير محفوظة).</summary>
    private UserNavProfile _pending = new();

    /// <summary>حقول staging (usage counts، فتحات حديثة).</summary>
    private bool _hasPendingChanges;

    private readonly object _lock = new();

    public NavigationExplorerService(
        IUserNavProfileStore store,
        ILogger<NavigationExplorerService> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            if (_hasPendingChanges && !string.IsNullOrEmpty(_currentUserId))
            {
                await SaveAsync(_currentUserId);
            }
        };
    }

    public event EventHandler? TreeChanged;
    public event EventHandler<NavItem>? ItemOpened;

    // ==========================================================
    //  Registration
    // ==========================================================

    public void RegisterScreen(string screenId, string title, string? icon = null,
        string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(screenId)) return;

        lock (_lock)
        {
            _known[screenId] = new NavItem
            {
                Id = screenId,
                Title = title,
                Icon = icon ?? "Screen",
                Kind = NavItemKind.Screen,
                Target = screenId,
                CategoryPath = category,
                Description = description
            };
        }

        _logger.LogDebug("Explorer: screen registered: {ScreenId}", screenId);
    }
    public int UnseenHistoryCount
    {
        get
        {
            lock (_historyLock)
            {
                return _profile.UnseenCount();
            }
        }
    }

    public event EventHandler? HistoryChanged;
    public void RegisterReport(string reportId, string title, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(reportId)) return;

        lock (_lock)
        {
            _known[reportId] = new NavItem
            {
                Id = reportId,
                Title = title,
                Icon = icon ?? "Report",
                Kind = NavItemKind.Report,
                Target = reportId
            };
        }
    }
    public IReadOnlyList<HistoryEntry> GetHistory(int count = 50)
    {
        lock (_historyLock)
        {
            return _profile.History
                .OrderByDescending(h => h.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    public IReadOnlyList<HistoryEntry> GetUnseenHistory(int count = 50)
    {
        lock (_historyLock)
        {
            var cutoff = _profile.LastSeenAt;
            return _profile.History
                .Where(h => cutoff is null || h.Timestamp > cutoff.Value)
                .OrderByDescending(h => h.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    public void MarkAllHistorySeen()
    {
        lock (_historyLock)
        {
            _profile.MarkAllSeen();
        }

        MarkPending();
        OnHistoryChanged();
        OnTreeChanged();   // لتحديث Badges
    }

    public void MarkHistorySeen(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return;

        lock (_historyLock)
        {
            var entry = _profile.History.FirstOrDefault(h => h.Id == entryId);
            entry?.MarkSeenBy(_currentUserId);
        }

        MarkPending();
        OnHistoryChanged();
    }

    public void AddHistory(HistoryKind kind, string? itemId, string title, string? details = null)
    {
        System.Diagnostics.Debug.WriteLine($"[History] Add: {kind} — {title}");
        var entry = new HistoryEntry
        {
            ItemId = itemId,
            Kind = kind,
            Title = title,
            Details = details,
            UserId = _currentUserId,
            Timestamp = DateTime.UtcNow
        };

        lock (_historyLock)
        {
            _profile.History.Add(entry);
            System.Diagnostics.Debug.WriteLine($"[History] Total entries: {_profile.History.Count}");
            // احتفظ بآخر 500 حدث فقط
            if (_profile.History.Count > 500)
            {
                var toRemove = _profile.History.Count - 500;
                _profile.History.RemoveRange(0, toRemove);
            }
        }

        MarkPending();
        OnHistoryChanged();
        _logger.LogInformation("History: {Kind} — {Title}", kind, title);
    }
    private void OnHistoryChanged()
    {
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
    public void RegisterExternal(NavItem item)
    {
        if (item is null || string.IsNullOrEmpty(item.Id)) return;

        lock (_lock)
        {
            _known[item.Id] = item;
        }
    }

    // ==========================================================
    //  Tree Building
    // ==========================================================

    public IReadOnlyList<NavItem> BuildRoot()
    {
        var root = new List<NavItem>();
        var s = _profile.LauncherSettings ?? new LauncherSettings();
        // 1) المثبتة
        if (s.ShowPinnedGroup)
        {
            var pinned = GetPinned();
            if (pinned.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__pinned__",
                    Title = "📌 المثبتة",
                    Kind = NavItemKind.Folder,
                    Children = pinned.ToList()
                });
            }
        }

        // 2) المفضلة
        // 2) المفضلة
        if (s.ShowFavoritesGroup)
        {
            var favorites = GetFavorites();
            if (favorites.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__favorites__",
                    Title = "⭐ المفضلة",
                    Kind = NavItemKind.Folder,
                    Children = favorites.ToList()
                });
            }
        }
        // 3) الأكثر استخدامًا
        if (s.ShowMostUsedGroup)
        {
            var mostUsed = GetMostUsed(10);
            if (mostUsed.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__mostused__",
                    Title = "🔥 الأكثر استخدامًا",
                    Kind = NavItemKind.Folder,
                    Children = mostUsed.ToList()
                });
            }
        }

        // 4) آخر ما فتحت
        if (s.ShowRecentGroup)
        {
            var recent = GetRecentlyOpened(10);
            if (recent.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__recent__",
                    Title = "🕐 آخر ما فتحت",
                    Kind = NavItemKind.Folder,
                    Children = recent.ToList()
                });
            }
        }

        // 5) الجديدة لك
        if (s.ShowNewGroup)
        {
            var newItems = GetNewForUser(7);
            if (newItems.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__new__",
                    Title = "✨ الجديدة لك",
                    Kind = NavItemKind.Folder,
                    Children = newItems.ToList()
                });
            }
        }
        // 6) كل الشاشات (مجمّعة حسب التصنيف)
        // 6) كل الشاشات
        if (s.ShowAllScreensGroup)
        {
            var allScreens = GetAllItems().Where(i => i.Kind == NavItemKind.Screen).ToList();
            if (allScreens.Count > 0)
            {
                var byCategory = allScreens
                    .GroupBy(s => s.CategoryPath ?? "غير مصنّف")
                    .Select(g => new NavItem
                    {
                        Id = $"__cat__{g.Key}",
                        Title = g.Key,
                        Kind = NavItemKind.Folder,
                        Children = g.OrderBy(i => i.Title).ToList()
                    }).ToList();

                root.Add(new NavItem
                {
                    Id = "__screens__",
                    Title = "🖥️ كل الشاشات",
                    Kind = NavItemKind.Folder,
                    Children = byCategory
                });
            }
        }
        // 7) كل التقارير
        // 7) التقارير
        if (s.ShowReportsGroup)
        {
            var reports = GetAllItems().Where(i => i.Kind == NavItemKind.Report).ToList();
            if (reports.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__reports__",
                    Title = "📊 التقارير",
                    Kind = NavItemKind.Folder,
                    Children = reports
                });
            }
        }

        // 8) العناصر الخارجية
        if (s.ShowExternalGroup)
        {
            var externals = GetAllItems()
            .Where(i => i.Kind == NavItemKind.ExternalFile
                     || i.Kind == NavItemKind.ExternalFolder
                     || i.Kind == NavItemKind.Url)
            .ToList();
            if (externals.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__external__",
                    Title = "🔗 عناصر خارجية",
                    Kind = NavItemKind.Folder,
                    Children = externals
                });
            }
        }

        // 9) ملفاتي (شجرة المستخدم)
        // 10) ملفاتي (نفس PersonalTree لكن تصنيف مختلف — أو احذفها)
        if (s.ShowPersonalTreeGroup && _profile.PersonalTree.Count > 0)
        {
        
            var personal = GetPersonalTree();
            if (personal.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__personal__",
                    Title = "📁 ملفاتي",
                    Kind = NavItemKind.Folder,
                    Children = personal.ToList()
                });
            }
        }
        // 10) العناصر المضافة يدويًا (شاشات، ملفات، روابط)
        if (s.ShowMyItemsGroup && _profile.PersonalTree.Count > 0)
        {
            if (_profile.PersonalTree.Count > 0)
            {
                root.Add(new NavItem
                {
                    Id = "__useritems__",
                    Title = "✨ عناصري",
                    Kind = NavItemKind.Folder,
                    Children = _profile.PersonalTree
                });
            }
        }
        // طبّق الترتيب
        foreach (var group in root)
            SortGroupItems(group);
        return root;
    }
    private void SortGroupItems(NavItem group)
    {
        if (group.Children.Count == 0) return;

        var mode = _profile.LauncherSettings.SortMode;

        IEnumerable<NavItem> sorted = mode switch
        {
            LauncherSortMode.Name => group.Children.OrderBy(i => i.Title, StringComparer.CurrentCulture),
            LauncherSortMode.MostUsed => group.Children.OrderByDescending(i => i.UsageCount),
            LauncherSortMode.RecentlyUsed => group.Children.OrderByDescending(i => i.LastOpened),
            _ => group.Children.OrderBy(i => i.SortOrder)  // Custom
        };

        var sortedList = sorted.ToList();
        group.Children.Clear();
        foreach (var item in sortedList)
            group.Children.Add(item);

        // للـ sub-groups
        foreach (var sub in group.Children)
            SortGroupItems(sub);
    }
    // ==========================================================
    //  Query
    // ==========================================================

    public IReadOnlyList<NavItem> GetRecentlyOpened(int count = 10)
    {
        var result = _known.Values
            .Where(i => _profile.LastOpened.ContainsKey(i.Id))
            .OrderByDescending(i => _profile.LastOpened[i.Id])
            .Take(count)
            .Select(Clone)
            .ToList();

        return result;
    }

    public IReadOnlyList<NavItem> GetMostUsed(int count = 10)
    {
        return _known.Values
            .Where(i => _profile.UsageCounts.ContainsKey(i.Id))
            .OrderByDescending(i => _profile.UsageCounts[i.Id])
            .Take(count)
            .Select(CloneWithUsage)
            .ToList();
    }

    public IReadOnlyList<NavItem> GetFavorites()
    {
        return _known.Values
            .Where(i => _profile.FavoriteIds.Contains(i.Id))
            .Select(CloneWithFavorites)
            .ToList();
    }

    public IReadOnlyList<NavItem> GetPinned()
    {
        return _known.Values
            .Where(i => _profile.PinnedIds.Contains(i.Id))
            .Select(CloneWithPinned)
            .ToList();
    }

    public IReadOnlyList<NavItem> GetNewForUser(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return _known.Values
            .Where(i => _profile.FirstSeen.TryGetValue(i.Id, out var d) && d >= since)
            .OrderByDescending(i => _profile.FirstSeen[i.Id])
            .Select(Clone)
            .ToList();
    }

    public IReadOnlyList<NavItem> GetAllItems()
        => _known.Values.Select(Clone).ToList();

    public IReadOnlyList<NavItem> GetPersonalTree()
        => _profile.PersonalTree.ToList();

    public IReadOnlyList<NavItem> Search(string query, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<NavItem>();

        var q = query.Trim();
        return _known.Values
            .Where(i => i.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || (i.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(maxResults)
            .Select(Clone)
            .ToList();
    }

    // ==========================================================
    //  User customization
    // ==========================================================

    public void AddToFavorites(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (_profile.FavoriteIds.Contains(itemId)) return;

        _profile.FavoriteIds.Add(itemId);
        MarkPending();
        OnTreeChanged();
    }

    public void RemoveFromFavorites(string itemId)
    {
        if (_profile.FavoriteIds.Remove(itemId)) { MarkPending(); OnTreeChanged(); }
    }

    public void ToggleFavorite(string itemId)
    {
        if (_profile.FavoriteIds.Contains(itemId)) RemoveFromFavorites(itemId);
        else AddToFavorites(itemId);
    }

    public void Pin(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (_profile.PinnedIds.Contains(itemId)) return;

        _profile.PinnedIds.Add(itemId);
        MarkPending();
        OnTreeChanged();
    }

    public void Unpin(string itemId)
    {
        if (_profile.PinnedIds.Remove(itemId)) { MarkPending(); OnTreeChanged(); }
    }

    public void TogglePin(string itemId)
    {
        if (_profile.PinnedIds.Contains(itemId)) Unpin(itemId);
        else Pin(itemId);
    }

    public void Hide(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (_profile.HiddenIds.Contains(itemId)) return;

        _profile.HiddenIds.Add(itemId);
        MarkPending();
        OnTreeChanged();

        var item = _known.GetValueOrDefault(itemId);
        AddHistory(HistoryKind.Updated, itemId, $"إخفاء: {item?.Title ?? itemId}");
    }

    public void Unhide(string itemId)
    {
        if (_profile.HiddenIds.Remove(itemId)) { MarkPending(); OnTreeChanged(); }
    }

    public void CreateUserFolder(string title, string? parentFolderId = null)
    {
        var folder = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Icon = "Folder",
            Kind = NavItemKind.Folder
        };

        if (parentFolderId is null)
        {
            _profile.PersonalTree.Add(folder);
        }
        else
        {
            var parent = FindFolder(_profile.PersonalTree, parentFolderId);
            parent?.Children.Add(folder);
        }

        MarkPending();
        OnTreeChanged();
    }

    public void MoveToFolder(string itemId, string folderId)
    {
        var item = _known.GetValueOrDefault(itemId);
        var folder = FindFolder(_profile.PersonalTree, folderId);
        if (item is null || folder is null) return;

        folder.Children.Add(Clone(item));
        MarkPending();
        OnTreeChanged();
    }

    public void DeleteUserFolder(string folderId)
    {
        if (RemoveFolder(_profile.PersonalTree, folderId))
        {
            MarkPending();
            OnTreeChanged();
        }
    }

    // ==========================================================
    //  Usage tracking
    // ==========================================================

    public void RecordOpen(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        // سجّل في profile (الالتزام) مباشرة، وفي pending للتأمين
        _profile.RecordOpen(itemId);
        _pending.RecordOpen(itemId);
        _hasPendingChanges = true;

        // اكتب الـ pending (fire-and-forget)
        _ = SavePendingAsync();

        if (_known.TryGetValue(itemId, out var item))
            ItemOpened?.Invoke(this, item);
    }

    // ==========================================================
    //  Persistence
    // ==========================================================

    public async Task LoadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return;
        _currentUserId = userId;   // ← ✅ عيّنه هنا
        try
        {
            // 1) حمّل الـ main
            var main = await _store.LoadAsync(userId, ct) ?? new UserNavProfile { UserId = userId };

            // 2) حمّل الـ pending إن وُجد
            var pending = await _store.LoadPendingAsync(userId, ct);

            if (pending is not null)
            {
                // ادمج
                main.MergeFrom(pending);
                _logger.LogInformation("Recovered {Usage} usages from pending file", pending.UsageCounts.Count);

                // احذف الملف المؤقت
                await _store.DeletePendingAsync(userId, ct);
            }

            _profile = main;
            _profile.UserId = userId;
            _pending = new UserNavProfile { UserId = userId };
            _hasPendingChanges = false;

            _logger.LogInformation("Profile loaded for {User}: {Screens} screens, {Pinned} pinned, {Fav} favorites",
                userId, _known.Count, _profile.PinnedIds.Count, _profile.FavoriteIds.Count);

            OnTreeChanged();
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile for {User}", userId);
        }
    }

    public async Task SaveAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            // 1) ادمج الـ pending في الـ main
            _profile.MergeFrom(_pending);
            _profile.LastUpdated = DateTime.UtcNow;

            // 2) احفظ الـ main
            await _store.SaveAsync(_profile, ct);

            // 3) احذف الـ pending
            await _store.DeletePendingAsync(userId, ct);

            // 4) صفّر الـ pending
            _pending = new UserNavProfile { UserId = userId };
            _hasPendingChanges = false;

            _logger.LogInformation("Profile saved for {User}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile for {User}", userId);
        }
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        await _store.DeleteAsync(userId, ct);
        _profile = new UserNavProfile { UserId = userId };
        _pending = new UserNavProfile { UserId = userId };
        OnTreeChanged();
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private async Task SavePendingAsync()
    {
        if (!_hasPendingChanges) return;

        try
        {
            await _store.SavePendingAsync(_pending);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to save pending profile");
        }
    }

    private void MarkPending() => _hasPendingChanges = true;
    private void OnTreeChanged() => TreeChanged?.Invoke(this, EventArgs.Empty);

    private NavItem Clone(NavItem source)
    {
        // احسب Badge من History
        var lastSeen = _profile.LastSeenAt;
        var latestHistoryForItem = _profile.History
            .Where(h => h.ItemId == source.Id)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefault();

        bool isNew = false;
        bool isUpdated = false;

        if (latestHistoryForItem is not null)
        {
            var isUnseen = lastSeen is null || latestHistoryForItem.Timestamp > lastSeen.Value;

            if (isUnseen)
            {
                isNew = latestHistoryForItem.Kind == HistoryKind.Created;
                isUpdated = latestHistoryForItem.Kind == HistoryKind.Updated
                         || latestHistoryForItem.Kind == HistoryKind.Moved
                         || latestHistoryForItem.Kind == HistoryKind.AddedToFolder;
            }
        }
        var c = new NavItem
        {
            Id = source.Id,
            Title = source.Title,
            Icon = source.Icon,
            Kind = source.Kind,
            Target = source.Target,
            Parameters = source.Parameters,
            Description = source.Description,
            CategoryPath = source.CategoryPath,
            ParentFolderId = source.ParentFolderId,
            CustomIconSize = source.CustomIconSize,
            CustomBackground = source.CustomBackground,
            SortOrder = source.SortOrder,
            IsFavorite = _profile.FavoriteIds.Contains(source.Id),
            IsPinned = _profile.PinnedIds.Contains(source.Id),
            UsageCount = _profile.UsageCounts.GetValueOrDefault(source.Id),
            LastOpened = _profile.LastOpened.GetValueOrDefault(source.Id),
            FirstSeen = _profile.FirstSeen.GetValueOrDefault(source.Id),
            IsActive = !string.IsNullOrEmpty(ActiveScreenId)
                    && string.Equals(source.Target, ActiveScreenId, StringComparison.OrdinalIgnoreCase),
            IsNew = isNew,
            IsUpdated = isUpdated,
        };
       
        // انسخ الأبناء (للمجلدات)
        foreach (var child in source.Children)
            c.Children.Add(Clone(child));

        return c;
    }
    /// <summary>عدد الأحداث غير المشاهدة لكل عنصر.</summary>
    public Dictionary<string, int> GetUnseenCountPerItem()
    {
        var result = new Dictionary<string, int>();
        System.Diagnostics.Debug.WriteLine($"[History] GetUnseenCountPerItem: {_profile.History.Count} entries, LastSeen={_profile.LastSeenAt}");

        var cutoff = _profile.LastSeenAt;

        lock (_historyLock)
        {
            foreach (var entry in _profile.History)
            {
                if (entry.ItemId is null) continue;
                if (cutoff is not null && entry.Timestamp <= cutoff.Value) continue;

                result[entry.ItemId] = result.GetValueOrDefault(entry.ItemId) + 1;
            }
        }

        return result;
    }

    private NavItem CloneWithUsage(NavItem source)
    {
        var c = Clone(source);
        c.UsageCount = _profile.UsageCounts.GetValueOrDefault(source.Id);
        return c;
    }

    private NavItem CloneWithFavorites(NavItem source)
    {
        var c = Clone(source);
        c.IsFavorite = true;
        return c;
    }

    private NavItem CloneWithPinned(NavItem source)
    {
        var c = Clone(source);
        c.IsPinned = true;
        return c;
    }

    private static NavItem? FindFolder(IEnumerable<NavItem> items, string folderId)
    {
        foreach (var i in items)
        {
            if (i.Id == folderId && i.Kind == NavItemKind.Folder) return i;
            var nested = FindFolder(i.Children, folderId);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static bool RemoveFolder(IList<NavItem> items, string folderId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Id == folderId && items[i].Kind == NavItemKind.Folder)
            {
                items.RemoveAt(i);
                return true;
            }
            if (RemoveFolder(items[i].Children, folderId)) return true;
        }
        return false;
    }

   

    // ==========================================================
    //  Active Screen
    // ==========================================================

    /// <summary>يحدد الشاشة النشطة (تُلوَّن في الـ Explorer).</summary>
    public void SetActiveScreen(string? screenId)
    {
        if (ActiveScreenId == screenId) return;

        ActiveScreenId = screenId;
        ActiveScreenChanged?.Invoke(this, screenId);

        // أعِد بناء الشجرة لأن الـ IsActive يُحسب عند Clone
        OnTreeChanged();
    }

    /// <summary>يُستدعى من الـ View عند النقر على أيقونة (فتح شاشة/تقرير/ملف).</summary>
    public void RequestOpen(NavItem item)
    {
        if (item is null) return;

        RecordOpen(item.Id);

        // تنفيذ الفتح حسب النوع
        switch (item.Kind)
        {
            case NavItemKind.Screen:
            case NavItemKind.Report:
                ItemOpenRequested?.Invoke(this, item);
                break;

            case NavItemKind.Url:
                OpenUrl(item.Target);
                break;

            case NavItemKind.ExternalFile:
                OpenFile(item.Target);
                break;

            case NavItemKind.ExternalFolder:
                OpenFolder(item.Target);
                break;
        }
    }

    // ==========================================================
    //  Add External Items
    // ==========================================================

    /// <summary>يضيف شاشة يدويًا (ضمن مجلد شخصي).</summary>
    public NavItem AddScreen(string title, string screenId, string? icon = null, string? folderId = null)
    {
        var item = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Icon = icon ?? "🖥️",
            Kind = NavItemKind.Screen,
            Target = screenId,
            ParentFolderId = folderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsNew = true
        };

        AddToPersonalTree(item, folderId);
        return item;
    }

    /// <summary>يضيف تقريرًا.</summary>
    public NavItem AddReport(string title, string reportId, string? icon = null, string? folderId = null)
    {
        var item = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Icon = icon ?? "📊",
            Kind = NavItemKind.Report,
            Target = reportId,
            ParentFolderId = folderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsNew = true
        };

        AddToPersonalTree(item, folderId);
        return item;
    }

    /// <summary>يضيف رابطًا إلكترونيًا.</summary>
    public NavItem AddUrl(string title, string url, string? icon = null, string? folderId = null)
    {
        var item = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Icon = icon ?? "🔗",
            Kind = NavItemKind.Url,
            Target = url,
            ParentFolderId = folderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsNew = true
        };

        AddToPersonalTree(item, folderId);
        return item;
    }

    /// <summary>يضيف ملفًا خارجيًا (على القرص).</summary>
    public NavItem AddExternalFile(string path, string? title = null, string? folderId = null)
    {
        var item = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title ?? Path.GetFileName(path),
            Icon = "📄",
            Kind = NavItemKind.ExternalFile,
            Target = path,
            ParentFolderId = folderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsNew = true,
            Description = path
        };

        AddToPersonalTree(item, folderId);
        return item;
    }

    /// <summary>يضيف مجلدًا خارجيًا (على القرص).</summary>
    public NavItem AddExternalFolder(string path, string? title = null, string? folderId = null)
    {
        var item = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title ?? Path.GetFileName(path),
            Icon = "📁",
            Kind = NavItemKind.ExternalFolder,
            Target = path,
            ParentFolderId = folderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsNew = true,
            Description = path
        };

        AddToPersonalTree(item, folderId);
        return item;
    }

    /// <summary>يضيف مجلدًا محليًا (داخل Explorer).</summary>
    public NavItem AddLocalFolder(string title, string? icon = null, string? parentFolderId = null)
    {
        var folder = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Icon = icon ?? "📂",
            Kind = NavItemKind.Folder,
            ParentFolderId = parentFolderId,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        AddToPersonalTree(folder, parentFolderId);
        return folder;
    }

    // ==========================================================
    //  Internal helpers
    // ==========================================================

    private void AddToPersonalTree(NavItem item, string? folderId)
    {
        if (folderId is null)
        {
            _profile.PersonalTree.Add(item);
        }
        else
        {
            var folder = FindFolder(_profile.PersonalTree, folderId);
            if (folder is not null)
                folder.Children.Add(item);
            else
                _profile.PersonalTree.Add(item);
        }

        lock (_lock)
        {
            _known[item.Id] = item;
        }

        MarkPending();
        OnTreeChanged();

        // ✅ سجّل في History
        AddHistory(
            HistoryKind.Created,
            item.Id,
            $"إضافة {GetKindLabel(item.Kind)}: {item.Title}",
            item.Target);

        _logger.LogInformation("Added to personal tree: {Title} ({Kind})", item.Title, item.Kind);
    }

    /// <summary>تسمية عربية لنوع العنصر.</summary>
    private static string GetKindLabel(NavItemKind kind) => kind switch
    {
        NavItemKind.Screen => "شاشة",
        NavItemKind.Report => "تقرير",
        NavItemKind.Url => "رابط",
        NavItemKind.ExternalFile => "ملف",
        NavItemKind.ExternalFolder => "مجلد خارجي",
        NavItemKind.Folder => "مجلد",
        _ => "عنصر"
    };

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private static void OpenFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private static void OpenFolder(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch { /* ignore */ }
    }
    public LauncherSettings Settings => _profile.LauncherSettings;

    public event EventHandler<LauncherSettings>? SettingsChanged;

    public void UpdateSettings(LauncherSettings settings)
    {
        if (settings is null) return;

        _profile.LauncherSettings = settings;
        MarkPending();
        SettingsChanged?.Invoke(this, settings);
        OnTreeChanged();
    }

    public void MoveItemBefore(string itemId, string targetItemId)
    {
        if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(targetItemId))
            return;
        if (itemId == targetItemId) return;

        var item = _known.GetValueOrDefault(itemId);
        var target = _known.GetValueOrDefault(targetItemId);
        if (item is null || target is null) return;

        // نحن نعمل على ترتيب المجموعات المنطقية (Pinned, Favorites, PersonalTree)
        // نحدّد المجموعة التي يوجد فيها العنصران
        var (container1, _) = FindContainer(itemId);
        var (container2, _) = FindContainer(targetItemId);

        if (container1 is null || container2 is null) return;
        if (!ReferenceEquals(container1, container2)) return; // نفس المجموعة فقط

        // انقل العنصر قبل الهدف
        var list = container1;

        var itemIndex = list.FindIndex(i => i.Id == itemId);
        var targetIndex = list.FindIndex(i => i.Id == targetItemId);

        if (itemIndex < 0 || targetIndex < 0) return;

        var moved = list[itemIndex];
        list.RemoveAt(itemIndex);

        // أعد حساب index الهدف بعد الإزالة
        targetIndex = list.FindIndex(i => i.Id == targetItemId);
        list.Insert(targetIndex, moved);

        // حدّث SortOrder
        for (int i = 0; i < list.Count; i++)
            list[i].SortOrder = i;

        MarkPending();
        OnTreeChanged();
        _logger.LogInformation("Moved {Id} before {Target}", itemId, targetItemId);
    }

    public void MoveItemToEnd(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var (list, _) = FindContainer(itemId);
        if (list is null) return;

        var item = list.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;

        list.Remove(item);
        item.SortOrder = list.Count;
        list.Add(item);

        // حدّث SortOrder للجميع
        for (int i = 0; i < list.Count; i++)
            list[i].SortOrder = i;

        MarkPending();
        OnTreeChanged();
    }

    public void ReorderGroup(IReadOnlyList<string> itemIdsInOrder)
    {
        if (itemIdsInOrder is null || itemIdsInOrder.Count == 0) return;

        // استخدم العنصر الأول لتحديد المجموعة
        var (list, _) = FindContainer(itemIdsInOrder[0]);
        if (list is null) return;

        var map = list.ToDictionary(i => i.Id, i => i);
        var ordered = new List<NavItem>();

        foreach (var id in itemIdsInOrder)
        {
            if (map.TryGetValue(id, out var item))
                ordered.Add(item);
        }

        // أضف العناصر التي لم تكن في القائمة
        foreach (var item in list)
            if (!itemIdsInOrder.Contains(item.Id))
                ordered.Add(item);

        list.Clear();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SortOrder = i;
            list.Add(ordered[i]);
        }

        MarkPending();
        OnTreeChanged();
    }

    /// <summary>يجد القائمة (PinnedIds ليست قائمة NavItem، لذا PersonalTree أو داخل مجلد).</summary>
    private (List<NavItem>? list, string? folderId) FindContainer(string itemId)
    {
        // ابحث في PersonalTree
        var result = FindInFolderList(_profile.PersonalTree, itemId);
        if (result is not null) return (result, null);

        return (null, null);
    }

    private static List<NavItem>? FindInFolderList(List<NavItem> list, string itemId)
    {
        if (list.Any(i => i.Id == itemId)) return list;

        foreach (var folder in list.Where(i => i.Kind == NavItemKind.Folder))
        {
            var found = FindInFolderList(folder.Children, itemId);
            if (found is not null) return found;
        }

        return null;
    }

    public NavItem CreateFolderFromItems(string folderTitle, string item1Id, string item2Id)
    {
        if (string.IsNullOrEmpty(item1Id) || string.IsNullOrEmpty(item2Id))
            throw new ArgumentException("Both item IDs are required");

        var item1 = _known.GetValueOrDefault(item1Id);
        var item2 = _known.GetValueOrDefault(item2Id);

        if (item1 is null || item2 is null)
            throw new InvalidOperationException("One or both items not found");

        // أنشئ المجلد
        var folder = new NavItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = folderTitle,
            Icon = "📁",
            Kind = NavItemKind.Folder,
            FirstSeen = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // أضف العنصرين داخل المجلد
        folder.Children.Add(CloneForFolder(item1));
        folder.Children.Add(CloneForFolder(item2));

        // احذف العنصرين من الجذر (إن كانا فيه)
        RemoveFromPersonalTree(item1Id);
        RemoveFromPersonalTree(item2Id);

        // أضف المجلد للجذر
        folder.SortOrder = _profile.PersonalTree.Count;
        _profile.PersonalTree.Add(folder);

        // سجّل المجلد في _known
        lock (_lock)
        {
            _known[folder.Id] = folder;
        }

        MarkPending();
        OnTreeChanged();

        _logger.LogInformation("Created folder {Title} with {Count} items",
            folderTitle, folder.Children.Count);

        AddHistory(HistoryKind.Created, folder.Id,
       $"مجلد جديد: {folderTitle}",
       $"يجمع {item1.Title} + {item2.Title}");

        return folder;
    }

    public void AddItemToFolder(string itemId, string folderId)
    {
        var item = _known.GetValueOrDefault(itemId);
        var folder = FindFolder(_profile.PersonalTree, folderId);
        if (item is null || folder is null) return;

        // انسخ
        folder.Children.Add(CloneForFolder(item));

        // احذف من الجذر
        RemoveFromPersonalTree(itemId);

        MarkPending();
        OnTreeChanged();
    }

    public void RemoveItemFromFolder(string itemId)
    {
        // ابحث في كل المجلدات
        if (RemoveFromFolderRecursive(_profile.PersonalTree, itemId))
        {
            MarkPending();
            OnTreeChanged();
        }
    }

    private static bool RemoveFromFolderRecursive(List<NavItem> list, string itemId)
    {
        foreach (var folder in list.Where(i => i.Kind == NavItemKind.Folder))
        {
            var toRemove = folder.Children.FirstOrDefault(c => c.Id == itemId);
            if (toRemove is not null)
            {
                folder.Children.Remove(toRemove);
                return true;
            }

            if (RemoveFromFolderRecursive(folder.Children, itemId))
                return true;
        }
        return false;
    }

    private void RemoveFromPersonalTree(string itemId)
    {
        var removed = RemoveFromListRecursive(_profile.PersonalTree, itemId);
        if (!removed)
        {
            // قد يكون في النظام
        }
    }

    private static bool RemoveFromListRecursive(List<NavItem> list, string itemId)
    {
        var item = list.FirstOrDefault(i => i.Id == itemId);
        if (item is not null)
        {
            list.Remove(item);
            return true;
        }

        foreach (var folder in list.Where(i => i.Kind == NavItemKind.Folder))
        {
            if (RemoveFromListRecursive(folder.Children, itemId))
                return true;
        }
        return false;
    }

    private NavItem CloneForFolder(NavItem source)
    {
        return new NavItem
        {
            Id = source.Id,
            Title = source.Title,
            Icon = source.Icon,
            Kind = source.Kind,
            Target = source.Target,
            Parameters = source.Parameters,
            Description = source.Description,
            CategoryPath = source.CategoryPath,
            CustomIconSize = source.CustomIconSize,
            CustomBackground = source.CustomBackground,
            SortOrder = source.SortOrder
        };
    }
    public void UpdateItem(string itemId, Action<NavItem> update)
    {
        if (string.IsNullOrEmpty(itemId) || update is null) return;

        var item = _known.GetValueOrDefault(itemId);
        if (item is null) return;

        var oldTitle = item.Title;

        update(item);
        item.UpdatedAt = DateTime.UtcNow;
        item.IsUpdated = true;

        MarkPending();
        OnTreeChanged();

        // ✅ سجّل في History
        AddHistory(
            HistoryKind.Updated,
            item.Id,
            $"تحديث: {item.Title}",
            $"من \"{oldTitle}\" إلى \"{item.Title}\"");
    }

    public void Dispose()
    {
        // محاولة حفظ الـ pending عند الإغلاق (fire-and-forget)
        if (_hasPendingChanges)
            _ = SavePendingAsync();
    }

}
