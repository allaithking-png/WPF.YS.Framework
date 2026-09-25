using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Base;

/// <summary>
/// أساس اختياري للشاشات.
/// يجمع Contracts الشائعة (IScreenAware + IDataAware + IMenuAware) مع وصول سريع للخدمات.
/// </summary>
/// <remarks>
/// اختياري تمامًا. أي ViewModel عادي يمكنه تنفيذ الواجهات مباشرة دون وراثة.
/// </remarks>
public abstract class BaseScreen : BaseViewModel, IScreenAware, IDataAware, IMenuAware
{
    // ==================== IScreenAware ====================

    /// <summary>معرّف الشاشة (يجب تجاوزه).</summary>
    public abstract string ScreenId { get; }

    /// <summary>عنوان الشاشة (يجب تجاوزه).</summary>
    public abstract string ScreenTitle { get; }

    public virtual Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task OnDeactivatedAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task<bool> OnClosingAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    // ==================== IDataAware ====================

    public virtual string? DataSourceKey => null;

    public virtual Task LoadInitialAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task LoadMoreAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public virtual Task SaveAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    // ==================== IMenuAware ====================

    public virtual MenuDefinition BuildMenu(MenuContext context)
        => new() { Host = context.PreferredHost };

    // ==================== وصول سريع للخدمات المشتركة ====================

    protected IDataService Data => GetService<IDataService>();
    protected INavigationService Navigation => GetService<INavigationService>();
    protected IMenuManager Menu => GetService<IMenuManager>();
    protected IOperationBus Operations => GetService<IOperationBus>();
    protected INotificationService Notify => GetService<INotificationService>();
    protected IUserContext User => GetService<IUserContext>();
    protected IReportService Reports => GetService<IReportService>();
}