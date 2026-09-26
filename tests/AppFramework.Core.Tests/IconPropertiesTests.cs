using System.Windows;
using System.Windows.Controls;
using FluentAssertions;
using AppFramework.Controls.Attached;
using Xunit;

namespace AppFramework.Core.Tests;

public class IconPropertiesTests
{
    // =============== Icon ===============

    [Fact]
    public void SetIcon_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.SetIcon(button, "Save");

            IconProperties.GetIcon(button).Should().Be("Save");
        });

    [Fact]
    public void Icon_DefaultIsNull()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.GetIcon(button).Should().BeNull();
        });

    // =============== Position ===============

    [Fact]
    public void SetPosition_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.SetPosition(button, IconProperties.IconPosition.Right);

            IconProperties.GetPosition(button).Should().Be(IconProperties.IconPosition.Right);
        });

    [Fact]
    public void Position_DefaultIsLeft()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.GetPosition(button).Should().Be(IconProperties.IconPosition.Left);
        });

    // =============== IconSize ===============

    [Fact]
    public void SetIconSize_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.SetIconSize(button, 24);

            IconProperties.GetIconSize(button).Should().Be(24);
        });

    [Fact]
    public void IconSize_DefaultIs16()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            IconProperties.GetIconSize(button).Should().Be(16);
        });

    // =============== Loading ===============

    [Fact]
    public void SetIsLoading_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button();
            LoadingProperties.SetIsLoading(button, true);

            LoadingProperties.GetIsLoading(button).Should().BeTrue();
        });

    [Fact]
    public void IsLoading_WhenTrue_DisablesControl()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button { IsEnabled = true };
            LoadingProperties.SetIsLoading(button, true);

            button.IsEnabled.Should().BeFalse();
        });

    [Fact]
    public void IsLoading_WhenFalse_ReenablesControl()
        => StaTestHelper.RunSta(() =>
        {
            var button = new Button { IsEnabled = false };
            LoadingProperties.SetIsLoading(button, true);
            LoadingProperties.SetIsLoading(button, false);

            button.IsEnabled.Should().BeTrue();
        });

    // =============== Validation ===============

    [Fact]
    public void SetIsValid_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var textBox = new TextBox();
            ValidationProperties.SetIsValid(textBox, false);

            ValidationProperties.GetIsValid(textBox).Should().BeFalse();
        });

    [Fact]
    public void IsValid_DefaultIsTrue()
        => StaTestHelper.RunSta(() =>
        {
            var textBox = new TextBox();
            ValidationProperties.GetIsValid(textBox).Should().BeTrue();
        });

    [Fact]
    public void IsValid_WhenFalse_AppliesErrorBorder()
        => StaTestHelper.RunSta(() =>
        {
            var textBox = new TextBox();
            ValidationProperties.SetIsValid(textBox, false);

            textBox.BorderThickness.Left.Should().Be(1.5);
            textBox.BorderBrush.Should().NotBeNull();
        });

    [Fact]
    public void SetErrorMessage_StoresValue()
        => StaTestHelper.RunSta(() =>
        {
            var textBox = new TextBox();
            ValidationProperties.SetErrorMessage(textBox, "Required");

            ValidationProperties.GetErrorMessage(textBox).Should().Be("Required");
        });
}
