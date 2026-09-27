using System;
using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// ملف المستخدم الشخصي في مستكشف التنقل.
/// </summary>
public sealed class UserNavProfile
{
    /// <summary>معرّف المستخدم.</summary>
    public string UserId { get; set; } = "";

    /// <summary>آخر تحديث (UTC).</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // ==========================================================
    //  الإحصاءات (usage tracking)
    // ==========================================================

    /// <summary>عدد مرات استخدام كل عنصر (ItemId → count).</summary>
    public Dictionary<string, int> UsageCounts { get; set; } = new();
    /// <summary>إعدادات سطح المكتب.</summary>
    public LauncherSettings LauncherSettings { get; set; } = new();

    /// <summary>آخر مرة فُتح (ItemId → UTC).</summary>
    public Dictionary<string, DateTime> LastOpened { get; set; } = new();

    /// <summary>أول مرة عُرف (ItemId → UTC).</summary>
    public Dictionary<string, DateTime> FirstSeen { get; set; } = new();

    // ==========================================================
    //  التخصيص (personalization)
    // ==========================================================

    /// <summary>معرّفات المثبتة.</summary>
    public List<string> PinnedIds { get; set; } = new();

    /// <summary>معرّفات المفضلة.</summary>
    public List<string> FavoriteIds { get; set; } = new();

    /// <summary>شجرة المستخدم الشخصية (مجلداته).</summary>
    public List<NavItem> PersonalTree { get; set; } = new();

    /// <summary>معرّفات مخفية (التي أخفاها المستخدم).</summary>
    public List<string> HiddenIds { get; set; } = new();

    // ==========================================================
    //  Helpers
    // ==========================================================

    /// <summary>سجّل فتح عنصر.</summary>
    public void RecordOpen(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var now = DateTime.UtcNow;

        UsageCounts[itemId] = UsageCounts.GetValueOrDefault(itemId) + 1;
        LastOpened[itemId] = now;

        if (!FirstSeen.ContainsKey(itemId))
            FirstSeen[itemId] = now;
    }

    /// <summary>دمج بيانات staging (pending) في هذا الملف.</summary>
    public void MergeFrom(UserNavProfile pending)
    {
        if (pending is null) return;

        foreach (var kvp in pending.UsageCounts)
        {
            UsageCounts[kvp.Key] = UsageCounts.GetValueOrDefault(kvp.Key) + kvp.Value;
        }

        foreach (var kvp in pending.LastOpened)
        {
            if (!LastOpened.TryGetValue(kvp.Key, out var existing) || existing < kvp.Value)
                LastOpened[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in pending.FirstSeen)
        {
            if (!FirstSeen.TryGetValue(kvp.Key, out var existing) || existing > kvp.Value)
                FirstSeen[kvp.Key] = kvp.Value;
        }

        foreach (var id in pending.PinnedIds)
            if (!PinnedIds.Contains(id)) PinnedIds.Add(id);

        foreach (var id in pending.FavoriteIds)
            if (!FavoriteIds.Contains(id)) FavoriteIds.Add(id);

        foreach (var id in pending.HiddenIds)
            if (!HiddenIds.Contains(id)) HiddenIds.Add(id);

        // للشجرة الشخصية: نُبقي شجرة الـ main (لا نُدمج متداخلة)
        // (يمكن تحسينها لاحقًا)

        LastUpdated = DateTime.UtcNow;
    }
    /// <summary>
    /// آخر مرة شاهد المستخدم فيها سجل التغييرات.
    /// (يُستخدم لحساب Badges "جديد" / "محدّث").
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    /// سجل التغييرات (History Log).
    /// </summary>
    public List<HistoryEntry> History { get; set; } = new();

    // ==========================================================
    //  Badge Helpers
    // ==========================================================

    /// <summary>هل حدث هذا الحدث بعد آخر مشاهدة؟</summary>
    public bool IsUnseen(HistoryEntry entry)
    {
        if (entry is null) return false;
        if (LastSeenAt is null) return true;   // لم يشاهد أبدًا

        return entry.Timestamp > LastSeenAt.Value;
    }

    /// <summary>عدد الأحداث غير المشاهدة.</summary>
    public int UnseenCount()
    {
        if (LastSeenAt is null) return History.Count;

        return History.Count(h => h.Timestamp > LastSeenAt.Value);
    }

    /// <summary>يحدّث LastSeenAt للآن.</summary>
    public void MarkAllSeen()
    {
        LastSeenAt = DateTime.UtcNow;

        foreach (var entry in History)
        {
            if (entry.UserId is not null)
                entry.MarkSeenBy(entry.UserId);
        }
    }
}
