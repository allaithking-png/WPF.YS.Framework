using System;

namespace AppFramework.Abstractions.Models.Screens;

/// <summary>
/// بيانات تسجيل شاشة في النظام.
/// </summary>
public sealed record ScreenRegistration(
    string Id,
    string Title,
    Type ViewModelType,
    string? Icon = null,
    string? Category = null,
    bool SupportsMultiOpen = false,
    bool RestoreOnStartup = false,
    string? DataSourceKey = null,
    string[]? Permissions = null);