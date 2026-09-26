using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Core.ViewTemplates;
using Xunit;

namespace AppFramework.Core.Tests;

public class ViewTemplateRegistryTests
{
    private static ViewTemplateRegistry CreateRegistry()
        => new(NullLogger<ViewTemplateRegistry>.Instance);

    private static ViewTemplateDescriptor MakeDescriptor(
        ViewMode mode,
        bool isDefault = false,
        ViewModeCategory category = ViewModeCategory.Extended)
        => new()
        {
            Mode = mode,
            Title = mode.ToString(),
            Category = category,
            IsDefault = isDefault
        };

    // =============== Test ViewModels ===============

    private class VM1 { }
    private class VM2 { }

    // =============== Register ===============

    [Fact]
    public void Register_WithTemplates_StoresThem()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1),
            MakeDescriptor(ViewMode.Grid),
            MakeDescriptor(ViewMode.Card));

        var templates = reg.GetTemplates<VM1>();

        templates.Should().HaveCount(2);
        templates.Select(t => t.Mode).Should().Contain(new[] { ViewMode.Grid, ViewMode.Card });
    }

    [Fact]
    public void Register_SameMode_ReplacesExisting()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid, category: ViewModeCategory.Default));
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid, category: ViewModeCategory.Extended));

        var templates = reg.GetTemplates<VM1>();

        templates.Should().HaveCount(1);
        templates[0].Category.Should().Be(ViewModeCategory.Extended);
    }

    [Fact]
    public void Register_EmptyArray_DoesNothing()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1));
        reg.GetTemplates<VM1>().Should().BeEmpty();
    }

    [Fact]
    public void Register_TwoModels_KeepsSeparate()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid));
        reg.Register(typeof(VM2), MakeDescriptor(ViewMode.Card));

        reg.GetTemplates<VM1>().Should().HaveCount(1);
        reg.GetTemplates<VM2>().Should().HaveCount(1);
        reg.GetTemplates<VM1>()[0].Mode.Should().Be(ViewMode.Grid);
        reg.GetTemplates<VM2>()[0].Mode.Should().Be(ViewMode.Card);
    }

    // =============== Unregister ===============

    [Fact]
    public void Unregister_RemovesSpecificMode()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1),
            MakeDescriptor(ViewMode.Grid),
            MakeDescriptor(ViewMode.Card));

        reg.Unregister(typeof(VM1), ViewMode.Grid);

        var templates = reg.GetTemplates<VM1>();
        templates.Should().HaveCount(1);
        templates[0].Mode.Should().Be(ViewMode.Card);
    }

    // =============== Query ===============

    [Fact]
    public void GetDefault_WithExplicitDefault_ReturnsIt()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1),
            MakeDescriptor(ViewMode.Grid, isDefault: false),
            MakeDescriptor(ViewMode.Card, isDefault: true));

        var def = reg.GetDefault(typeof(VM1));
        def.Should().NotBeNull();
        def!.Mode.Should().Be(ViewMode.Card);
    }

    [Fact]
    public void GetDefault_NoExplicitDefault_ReturnsCategoryDefault()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1),
            MakeDescriptor(ViewMode.Grid, category: ViewModeCategory.Default),
            MakeDescriptor(ViewMode.Card, category: ViewModeCategory.Extended));

        var def = reg.GetDefault(typeof(VM1));
        def!.Mode.Should().Be(ViewMode.Grid);
    }

    [Fact]
    public void GetDefault_NoFlags_ReturnsFirstTemplate()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Card));

        var def = reg.GetDefault(typeof(VM1));
        def!.Mode.Should().Be(ViewMode.Card);
    }

    [Fact]
    public void GetDefault_NoTemplates_ReturnsNull()
    {
        var reg = CreateRegistry();
        reg.GetDefault(typeof(VM1)).Should().BeNull();
    }

    [Fact]
    public void GetByMode_ExistingMode_ReturnsIt()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid));

        var t = reg.GetByMode(typeof(VM1), ViewMode.Grid);
        t.Should().NotBeNull();
        t!.Mode.Should().Be(ViewMode.Grid);
    }

    [Fact]
    public void GetByMode_MissingMode_ReturnsNull()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid));

        reg.GetByMode(typeof(VM1), ViewMode.Card).Should().BeNull();
    }

    // =============== Events ===============

    [Fact]
    public void Register_FiresTemplatesChangedEvent()
    {
        var reg = CreateRegistry();
        Type? received = null;
        reg.TemplatesChanged += (_, t) => received = t;

        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid));

        received.Should().Be(typeof(VM1));
    }

    [Fact]
    public void Unregister_FiresTemplatesChangedEvent()
    {
        var reg = CreateRegistry();
        reg.Register(typeof(VM1), MakeDescriptor(ViewMode.Grid));

        Type? received = null;
        reg.TemplatesChanged += (_, t) => received = t;

        reg.Unregister(typeof(VM1), ViewMode.Grid);

        received.Should().Be(typeof(VM1));
    }
}