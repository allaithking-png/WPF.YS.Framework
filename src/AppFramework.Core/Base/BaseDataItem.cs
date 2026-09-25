using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Base;

/// <summary>
/// أساس اختياري لعنصر بيانات واحد (صف، بطاقة، عقدة).
/// يوفّر تحميلًا وحفظًا مستقلًا للعنصر عبر IDataService.
/// </summary>
public abstract class BaseDataItem : BaseViewModel, IDataItemAware
{
    private bool _isDirty;

    /// <summary>مفتاح مصدر البيانات (يجب تجاوزه).</summary>
    public virtual string? ItemDataSourceKey => null;

    /// <summary>المفتاح الفريد للعنصر (يجب تجاوزه).</summary>
    public abstract string ItemKey { get; }

    /// <summary>هل توجد تغييرات غير محفوظة؟</summary>
    public bool IsDirty
    {
        get => _isDirty;
        protected set => SetProperty(ref _isDirty, value);
    }

    public virtual async Task LoadSelfAsync(CancellationToken ct = default)
    {
        if (ItemDataSourceKey is null) return;

        var data = GetService<IDataService>();
        var loaded = await data.LoadItemAsync(ItemDataSourceKey, ItemKey, ct);
        if (loaded is not null)
        {
            CopyFrom(loaded);
            IsDirty = false;
        }
    }

    public virtual async Task SaveSelfAsync(CancellationToken ct = default)
    {
        if (ItemDataSourceKey is null) return;

        var data = GetService<IDataService>();
        var snapshot = CreateSnapshot();
        await data.SaveItemAsync(ItemDataSourceKey, snapshot, ct);
        IsDirty = false;
    }

    /// <summary>نسخ قيم من كائن آخر إلى هذا العنصر.</summary>
    protected virtual void CopyFrom(object source) { }

    /// <summary>إنشاء كائن للحفظ (افتراضيًا this).</summary>
    protected virtual object CreateSnapshot() => this;

    /// <summary>يُستدعى من الفئات المشتقة عند تغيير خاصية.</summary>
    protected void MarkDirty() => IsDirty = true;
}