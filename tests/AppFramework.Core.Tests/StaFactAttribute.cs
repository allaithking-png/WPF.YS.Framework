using System;
using Xunit;

namespace AppFramework.Core.Tests;

/// <summary>
/// كـ [Fact] لكن يشغّل الاختبار على خيط STA (مطلوب لعناصر WPF).
/// </summary>
public sealed class StaFactAttribute : FactAttribute
{
}

/// <summary>
/// كـ [Theory] على خيط STA.
/// </summary>
public sealed class StaTheoryAttribute : TheoryAttribute
{
}