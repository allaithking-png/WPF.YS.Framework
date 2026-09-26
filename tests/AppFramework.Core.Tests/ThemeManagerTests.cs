using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Controls.Theming;
using Xunit;

namespace AppFramework.Core.Tests;

public class ThemeManagerTests : IDisposable
{
    private readonly string _tempFolder;

    public ThemeManagerTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "AppFrameworkTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempFolder, recursive: true); } catch { /* ignore */ }
    }

    private ThemeManager CreateManager()
        => new(NullLogger<ThemeManager>.Instance, _tempFolder);

    [Fact]
    public void CurrentTheme_DefaultIsLight()
    {
        var mgr = CreateManager();
        mgr.CurrentTheme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public void AvailableThemes_ContainsThree()
    {
        var mgr = CreateManager();
        mgr.AvailableThemes.Should().HaveCount(3);
        mgr.AvailableThemes.Should().Contain(AppTheme.Light);
        mgr.AvailableThemes.Should().Contain(AppTheme.Dark);
        mgr.AvailableThemes.Should().Contain(AppTheme.Corporate);
    }

    [Fact]
    public void ApplyTheme_WithoutApplication_DoesNotThrow()
    {
        var mgr = CreateManager();
        var act = () => mgr.ApplyTheme(AppTheme.Dark);
        act.Should().NotThrow();
    }

    [Fact]
    public void SaveCurrentTheme_WritesFile()
    {
        var mgr = CreateManager();
        mgr.SaveCurrentTheme("user1");

        var file = Path.Combine(_tempFolder, "user1", "theme.txt");
        File.Exists(file).Should().BeTrue();
        File.ReadAllText(file).Trim().Should().Be("Light");
    }

    [Fact]
    public void ApplySavedTheme_NoFile_AppliesLight()
    {
        var mgr = CreateManager();
        var act = () => mgr.ApplySavedTheme("unknownuser");
        act.Should().NotThrow();
    }

    [Fact]
    public void SaveThenLoad_ReturnsCorrectTheme()
    {
        var mgr = CreateManager();
        mgr.SaveCurrentTheme("user1");

        var file = Path.Combine(_tempFolder, "user1", "theme.txt");
        File.ReadAllText(file).Trim().Should().Be("Light");
    }
}