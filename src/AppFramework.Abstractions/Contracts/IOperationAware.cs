using AppFramework.Abstractions.Models.Operations;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة العنصر على الاستجابة لأحداث العمليات (حفظ، حذف، تصدير، ...).
/// </summary>
public interface IOperationAware : IAppAware
{
    /// <summary>
    /// يُستدعى **قبل** بدء العملية.
    /// يمكن للعنصر أن يلغي العملية عبر <c>context.Cancel = true</c>.
    /// </summary>
    void OnOperationStarting(OperationContext context);

    /// <summary>يُستدعى **بعد** انتهاء العملية (نجاح أو فشل).</summary>
    void OnOperationCompleted(OperationResult result);
}
