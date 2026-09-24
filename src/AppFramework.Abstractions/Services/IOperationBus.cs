using System;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Operations;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// ناقل العمليات (Operation Bus) مع أحداث Before/Started/Completed.
/// </summary>
public interface IOperationBus
{
    /// <summary>تنفيذ عملية مع تتبع كامل للأحداث.</summary>
    Task<OperationResult> ExecuteAsync(OperationContext context, Func<Task<object?>> action);

    /// <summary>الإبلاغ يدويًا عن نتيجة عملية.</summary>
    void Report(OperationContext context, OperationResult result);

    /// <summary>الاشتراك في حدث "قبل البدء" (يمكن الإلغاء).</summary>
    IDisposable SubscribeStarting(Action<OperationContext> handler);

    /// <summary>الاشتراك في حدث "انتهت".</summary>
    IDisposable SubscribeCompleted(Action<OperationResult> handler);
}