using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace AppFramework.Core.Base;

/// <summary>
/// أساس اختياري للحوارات (Dialogs).
/// يدير دورة حياة "Ok/Cancel" بنمط TCS.
/// </summary>
public abstract class BaseDialog : BaseViewModel
{
    private readonly TaskCompletionSource<object?> _tcs = new();

    public BaseDialog()
    {
        OkCommand = new AsyncRelayCommand(ExecuteOkAsync);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    /// <summary>نتيجة الحوار (تُكمل عند Ok أو Cancel).</summary>
    public Task<object?> Result => _tcs.Task;

    public IAsyncRelayCommand OkCommand { get; }
    public IRelayCommand CancelCommand { get; }

    private async Task ExecuteOkAsync()
    {
        if (!await CanCloseAsync()) return;

        object? result = null;
        try
        {
            result = await BuildResultAsync();
        }
        catch (System.Exception ex)
        {
            _tcs.TrySetException(ex);
            return;
        }
        _tcs.TrySetResult(result);
    }

    private void ExecuteCancel() => _tcs.TrySetResult(null);

    /// <summary>تحقق قبل الإغلاق (افتراضيًا يسمح).</summary>
    protected virtual Task<bool> CanCloseAsync() => Task.FromResult(true);

    /// <summary>نتيجة الحوار (يجب تجاوزها).</summary>
    protected virtual Task<object?> BuildResultAsync() => Task.FromResult<object?>(null);
}