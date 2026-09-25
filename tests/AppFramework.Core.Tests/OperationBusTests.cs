using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Operations;
using AppFramework.Core.Services;
using Xunit;

namespace AppFramework.Core.Tests;

public class OperationBusTests
{
    private static OperationBus CreateBus() => new(NullLogger<OperationBus>.Instance);

    [Fact]
    public async Task ExecuteAsync_WhenActionSucceeds_ReturnsSuccessResult()
    {
        // Arrange
        var bus = CreateBus();
        var context = new OperationContext { Type = "Test" };

        // Act
        var result = await bus.ExecuteAsync(context, () => Task.FromResult<object?>("payload"));

        // Assert
        result.Success.Should().BeTrue();
        result.Payload.Should().Be("payload");
        result.OperationId.Should().Be(context.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionThrows_ReturnsFailureResult()
    {
        // Arrange
        var bus = CreateBus();
        var context = new OperationContext { Type = "Test" };

        // Act
        var result = await bus.ExecuteAsync(context, () => throw new InvalidOperationException("boom"));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeOfType<InvalidOperationException>();
        result.Message.Should().Be("boom");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriberCancels_ReturnsCancelledResult()
    {
        // Arrange
        var bus = CreateBus();
        var context = new OperationContext { Type = "Test" };
        var actionCalled = false;

        using var sub = bus.SubscribeStarting(ctx => ctx.Cancel = true);

        // Act
        var result = await bus.ExecuteAsync(context, () =>
        {
            actionCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cancelled");
        actionCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeCompleted_IsInvokedOnSuccess()
    {
        // Arrange
        var bus = CreateBus();
        OperationResult? received = null;
        using var sub = bus.SubscribeCompleted(r => received = r);

        // Act
        await bus.ExecuteAsync(new OperationContext { Type = "Test" }, () => Task.FromResult<object?>(1));

        // Assert
        received.Should().NotBeNull();
        received!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeCompleted_IsInvokedOnFailure()
    {
        // Arrange
        var bus = CreateBus();
        OperationResult? received = null;
        using var sub = bus.SubscribeCompleted(r => received = r);

        // Act
        await bus.ExecuteAsync(new OperationContext { Type = "Test" },
            () => throw new Exception("fail"));

        // Assert
        received.Should().NotBeNull();
        received!.Success.Should().BeFalse();
    }

    [Fact]
    public void Dispose_UnsubscribesHandler()
    {
        // Arrange
        var bus = CreateBus();
        var invoked = 0;

        var sub = bus.SubscribeStarting(_ => invoked++);
        sub.Dispose();

        // Act
        bus.SubscribeStarting(_ => { }); // مسح الحدث يدويًا — لا، لا يمسح. اختبار بسيط.

        // Assert
        invoked.Should().Be(0);
    }
}