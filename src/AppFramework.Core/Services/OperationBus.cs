using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Operations;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Services;

/// <summary>
/// تنفيذ <see cref="IOperationBus"/>.
/// يدير أحداث العمليات (Before/Started/Completed) مع دعم الإلغاء.
/// </summary>
public sealed class OperationBus : IOperationBus
{
    private readonly ILogger<OperationBus> _logger;
    private readonly List<Action<OperationContext>> _startingHandlers = new();
    private readonly List<Action<OperationResult>> _completedHandlers = new();

    public OperationBus(ILogger<OperationBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult> ExecuteAsync(
        OperationContext context,
        Func<Task<object?>> action)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);

        // ========== 1) Starting — يمكن لأي مشترك إلغاء العملية ==========
        foreach (var handler in _startingHandlers)
        {
            try { handler(context); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Starting handler threw for operation {Id}", context.Id);
            }

            if (context.Cancel)
            {
                var cancelled = new OperationResult(
                    context.Id, false, null, "Cancelled by subscriber");
                _logger.LogInformation("Operation {Id} ({Type}) cancelled", context.Id, context.Type);
                NotifyCompleted(cancelled);
                return cancelled;
            }
        }

        _logger.LogInformation("Operation {Id} ({Type}) started", context.Id, context.Type);

        // ========== 2) Execute ==========
        try
        {
            var payload = await action();
            var result = new OperationResult(context.Id, true, payload, null);
            NotifyCompleted(result);
            _logger.LogInformation("Operation {Id} succeeded", context.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation {Id} failed", context.Id);
            var result = new OperationResult(context.Id, false, null, ex.Message, ex);
            NotifyCompleted(result);
            return result;
        }
    }

    public void Report(OperationContext context, OperationResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);
        NotifyCompleted(result);
    }

    public IDisposable SubscribeStarting(Action<OperationContext> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _startingHandlers.Add(handler);
        return new HandlerSubscription(() => _startingHandlers.Remove(handler));
    }

    public IDisposable SubscribeCompleted(Action<OperationResult> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _completedHandlers.Add(handler);
        return new HandlerSubscription(() => _completedHandlers.Remove(handler));
    }

    private void NotifyCompleted(OperationResult result)
    {
        Action<OperationResult>[] snapshot;
        snapshot = _completedHandlers.ToArray();

        foreach (var handler in snapshot)
        {
            try { handler(result); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Completed handler threw for operation {Id}", result.OperationId);
            }
        }
    }

    private sealed class HandlerSubscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public HandlerSubscription(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dispose();
        }
    }
}