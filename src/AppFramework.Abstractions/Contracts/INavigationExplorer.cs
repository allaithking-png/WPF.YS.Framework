using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// خدمة مستكشف التنقل — تُدير شجرة الشاشات/التقارير/الملفات
/// مع إحصاءات المستخدم وتخصيصاته.
/// </summary>
public interface INavigationExplorer
{

    Dictionary<string, int> GetUnseenCountPerItem();
   
    // ==========================================================
    //  Registration (النظام)
    // ==========================================================

    /// <summary>تسجيل شاشة في المستكشف.</summary>
    void RegisterScreen(string screenId, string title, string? icon = null,
        string? category = null, string? description = null);

    /// <summary>تسجيل تقرير.</summary>
    void RegisterReport(string reportId, string title, string? icon = null);

    /// <summary>تسجيل عنصر خارجي (ملف/مجلد/رابط).</summary>
    void RegisterExternal(NavItem item);

    // ==========================================================
    //  Reading
    // ==========================================================

    /// <summary>الشجرة الكاملة (النظام + المستخدم).</summary>
    IReadOnlyList<NavItem> BuildRoot();

    /// <summary>آخر ما فتحه المستخدم.</summary>
    IReadOnlyList<NavItem> GetRecentlyOpened(int count = 10);

    /// <summary>الأكثر استخدامًا.</summary>
    IReadOnlyList<NavItem> GetMostUsed(int count = 10);

    /// <summary>المفضلة.</summary>
    IReadOnlyList<NavItem> GetFavorites();

    /// <summary>المثبتة.</summary>
    IReadOnlyList<NavItem> GetPinned();

    /// <summary>الجديدة للمستخدم (أول مرة خلال N يوم).</summary>
    IReadOnlyList<NavItem> GetNewForUser(int days = 7);

    /// <summary>كل شاشات النظام.</summary>
    IReadOnlyList<NavItem> GetAllItems();

    /// <summary>الشجرة الشخصية.</summary>
    IReadOnlyList<NavItem> GetPersonalTree();

    /// <summary>البحث في كل العناصر.</summary>
    IReadOnlyList<NavItem> Search(string query, int maxResults = 50);

    // ==========================================================
    //  User customization
    // ==========================================================

    void AddToFavorites(string itemId);
    void RemoveFromFavorites(string itemId);
    void ToggleFavorite(string itemId);

    void Pin(string itemId);
    void Unpin(string itemId);
    void TogglePin(string itemId);

    void Hide(string itemId);
    void Unhide(string itemId);

    void CreateUserFolder(string title, string? parentFolderId = null);
    void MoveToFolder(string itemId, string folderId);
    void DeleteUserFolder(string folderId);

    // ==========================================================
    //  Usage tracking
    // ==========================================================

    /// <summary>سجّل فتح عنصر (يُستدعى من NavigationService).</summary>
    void RecordOpen(string itemId);

    // ==========================================================
    //  Persistence
    // ==========================================================

    /// <summary>تحميل ملف المستخدم.</summary>
    Task LoadAsync(string userId, CancellationToken ct = default);

    /// <summary>حفظ فوري.</summary>
    Task SaveAsync(string userId, CancellationToken ct = default);

    /// <summary>حذف ملف المستخدم.</summary>
    Task DeleteAsync(string userId, CancellationToken ct = default);

    // ==========================================================
    //  Events
    // ==========================================================

    /// <summary>حدث عند تغيير الشجرة (إضافة/حذف).</summary>
    event EventHandler? TreeChanged;

    /// <summary>حدث عند فتح عنصر.</summary>
    event EventHandler<NavItem>? ItemOpened;

    /// <summary>الشاشة النشطة حاليًا.</summary>
    string? ActiveScreenId { get; }

    /// <summary>حدث عند تغيير الشاشة النشطة.</summary>
    event EventHandler<string?>? ActiveScreenChanged;

    /// <summary>يحدد الشاشة النشطة.</summary>
    void SetActiveScreen(string? screenId);

    /// <summary>يطلب فتح عنصر.</summary>
    void RequestOpen(NavItem item);

    // ==========================================================
    //  Launcher Settings
    // ==========================================================

    /// <summary>الحصول على الإعدادات الحالية (نسخة للقراءة).</summary>
    LauncherSettings Settings { get; }

    /// <summary>تحديث الإعدادات.</summary>
    void UpdateSettings(LauncherSettings settings);

    /// <summary>حدث عند تغيير الإعدادات.</summary>
    event EventHandler<LauncherSettings>? SettingsChanged;

    // ==========================================================
    //  Drag & Drop / Reorder
    // ==========================================================

    /// <summary>نقل عنصر قبل عنصر آخر (ترتيب).</summary>
    void MoveItemBefore(string itemId, string targetItemId);

    /// <summary>نقل عنصر إلى نهاية القائمة.</summary>
    void MoveItemToEnd(string itemId);

    /// <summary>تحديث ترتيب مجموعة كاملة دفعة واحدة.</summary>
    void ReorderGroup(IReadOnlyList<string> itemIdsInOrder);

    // ==========================================================
    //  Folders
    // ==========================================================

    /// <summary>إنشاء مجلد يجمع عدة عناصر (سحب أيقونة على أخرى).</summary>
    NavItem CreateFolderFromItems(string folderTitle, string item1Id, string item2Id);

    /// <summary>إضافة عنصر إلى مجلد.</summary>
    void AddItemToFolder(string itemId, string folderId);

    /// <summary>إزالة عنصر من مجلد (نقله للمستوى الأعلى).</summary>
    void RemoveItemFromFolder(string itemId);

    // ==========================================================
    //  Custom properties
    // ==========================================================

    /// <summary>تحديث خصائص مخصصة لعنصر (حجم، لون، عنوان...).</summary>
    void UpdateItem(string itemId, Action<NavItem> update);

    // ==========================================================
    //  Add Items (manually)
    // ==========================================================

    /// <summary>يضيف شاشة إلى الشجرة الشخصية.</summary>
    NavItem AddScreen(string title, string screenId, string? icon = null, string? folderId = null);

    /// <summary>يضيف تقريرًا إلى الشجرة الشخصية.</summary>
    NavItem AddReport(string title, string reportId, string? icon = null, string? folderId = null);

    /// <summary>يضيف رابطًا إلكترونيًا.</summary>
    NavItem AddUrl(string title, string url, string? icon = null, string? folderId = null);

    /// <summary>يضيف ملفًا خارجيًا (على القرص).</summary>
    NavItem AddExternalFile(string path, string? title = null, string? folderId = null);

    /// <summary>يضيف مجلدًا خارجيًا (على القرص).</summary>
    NavItem AddExternalFolder(string path, string? title = null, string? folderId = null);

    /// <summary>يضيف مجلدًا محليًا (تنظيمي).</summary>
    NavItem AddLocalFolder(string title, string? icon = null, string? parentFolderId = null);
    // ==========================================================
    //  History
    // ==========================================================

    /// <summary>عدد الأحداث غير المشاهدة.</summary>
    int UnseenHistoryCount { get; }

    /// <summary>حدث عند إضافة/تعليم History.</summary>
    event EventHandler? HistoryChanged;

    /// <summary>يُعيد آخر N حدث (الأحدث أولًا).</summary>
    IReadOnlyList<HistoryEntry> GetHistory(int count = 50);

    /// <summary>يُعيد الأحداث غير المشاهدة فقط.</summary>
    IReadOnlyList<HistoryEntry> GetUnseenHistory(int count = 50);

    /// <summary>يُعلّم كل الأحداث كمشاهَدة.</summary>
    void MarkAllHistorySeen();

    /// <summary>يُعلّم حدثًا واحدًا كمشاهَد.</summary>
    void MarkHistorySeen(string entryId);

    /// <summary>يُضيف حدثًا إلى السجل يدويًا.</summary>
    void AddHistory(HistoryKind kind, string? itemId, string title, string? details = null);
  
}
