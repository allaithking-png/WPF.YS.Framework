using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Abstractions.Services;

namespace AppFramework.Data.Reports;

/// <summary>
/// تنفيذ <see cref="IReportService"/>.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly Dictionary<ReportExportFormat, IReportRenderer> _renderers = new();
    private readonly Dictionary<ReportExportFormat, IReportExporter> _exporters = new();
    private readonly ConcurrentDictionary<string, Func<ReportRequest, Task<ReportResult>>> _generators = new();

    public ReportService(
        IEnumerable<IReportRenderer> renderers,
        IEnumerable<IReportExporter> exporters,
        ILogger<ReportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        foreach (var r in renderers) _renderers[r.Format] = r;
        foreach (var e in exporters) _exporters[e.Format] = e;

        _logger.LogInformation("ReportService initialized with {Renderers} renderers and {Exporters} exporters",
            _renderers.Count, _exporters.Count);
    }

    // ==========================================================
    //  Registration
    // ==========================================================

    public void RegisterReport(string reportId, Func<ReportRequest, Task<ReportResult>> generator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportId);
        ArgumentNullException.ThrowIfNull(generator);

        _generators[reportId] = generator;
        _logger.LogDebug("Report registered: {ReportId}", reportId);
    }

    // ==========================================================
    //  Generation
    // ==========================================================

    public async Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_generators.TryGetValue(request.ReportId, out var generator))
        {
            _logger.LogWarning("Report generator not found: {ReportId}", request.ReportId);
            return new ReportResult
            {
                ReportId = request.ReportId,
                Data = Array.Empty<object>()
            };
        }

        try
        {
            var result = await generator(request);
            result.ReportId = request.ReportId;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation failed: {ReportId}", request.ReportId);
            throw;
        }
    }

    // ==========================================================
    //  Rendering
    // ==========================================================

    public async Task<byte[]> RenderAsync(
        ReportRequest request,
        ReportExportFormat format,
        CancellationToken ct = default)
    {
        var result = await GenerateAsync(request, ct);

        // إن كان الـ Result يحتوي bytes جاهزة، أرجعها
        if (result.RenderedBytes is { Length: > 0 })
            return result.RenderedBytes;

        // ابحث عن Renderer مناسب
        if (!_renderers.TryGetValue(format, out var renderer))
        {
            _logger.LogWarning("No renderer registered for format {Format}", format);
            return Array.Empty<byte>();
        }

        return await renderer.RenderAsync(result, ct);
    }

    // ==========================================================
    //  Export
    // ==========================================================

    public async Task ExportAsync(
        ReportRequest request,
        ReportExportFormat format,
        string targetPath,
        CancellationToken ct = default)
    {
        // 1) صيّر إلى bytes
        var bytes = await RenderAsync(request, format, ct);

        if (bytes.Length == 0)
        {
            _logger.LogWarning("No bytes rendered for {Format}", format);
        }

        // 2) اكتب الملف مباشرة (تجاوز الـ exporters)
        var dir = System.IO.Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        await System.IO.File.WriteAllBytesAsync(targetPath, bytes, ct);

        _logger.LogInformation("Report exported: {Path} ({Bytes} bytes)", targetPath, bytes.Length);
    }

    // ==========================================================
    //  Preview (stub — UI سيتولّى العرض)
    // ==========================================================

    public void ShowPreview(ReportResult result)
    {
        // لا نفعل شيئًا هنا — الـ UI سيتعامل مع العرض
        // (سنُتيح في المستقبل خدمة معاينة مخصّصة)
        _logger.LogDebug("ShowPreview called for {ReportId} (no UI action in Core)", result.ReportId);
    }
}
