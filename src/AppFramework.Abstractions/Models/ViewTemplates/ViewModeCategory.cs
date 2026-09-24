namespace AppFramework.Abstractions.Models.ViewTemplates;

/// <summary>
/// تصنيف طريقة العرض: أساسية (تأتي مع الإطار) أو إضافية (يسجّلها ViewModel).
/// </summary>
public enum ViewModeCategory
{
    /// <summary>طريقة عرض أساسية يوفرها الإطار.</summary>
    Default,

    /// <summary>طريقة عرض إضافية يسجّلها مطور الشاشة.</summary>
    Extended
}