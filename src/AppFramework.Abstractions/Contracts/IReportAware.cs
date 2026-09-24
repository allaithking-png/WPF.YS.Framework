using System.Collections.Generic;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة الشاشة على الإعلان عن التقارير المرتبطة بها.
/// </summary>
public interface IReportAware : IAppAware
{
    /// <summary>
    /// معرّفات التقارير المرتبطة بهذه الشاشة.
    /// تُستخدم من MenuManager لعرض قائمة "التقارير المتاحة" في سياق الشاشة.
    /// </summary>
    IReadOnlyList<string> LinkedReportIds { get; }
}
