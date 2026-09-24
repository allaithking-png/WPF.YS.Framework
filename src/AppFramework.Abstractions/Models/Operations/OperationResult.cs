using System;

namespace AppFramework.Abstractions.Models.Operations;

/// <summary>
/// نتيجة عملية منتهية.
/// </summary>
public sealed record OperationResult(
    string OperationId,
    bool Success,
    object? Payload = null,
    string? Message = null,
    Exception? Error = null);