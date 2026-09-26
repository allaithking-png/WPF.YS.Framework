using System;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Services;
using AppFramework.Data.Reports.Exporters;

namespace AppFramework.Data.Reports.DependencyInjection;

/// <summary>
/// امتدادات DI لخدمة التقارير.
/// </summary>
public static class ReportServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل خدمة التقارير + المصدرّين الافتراضيين (CSV/Excel/PDF).
    /// </summary>
    public static IServiceCollection AddAppFrameworkReports(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Renderers
        services.AddSingleton<IReportRenderer, CsvReportExporter>();
        services.AddSingleton<IReportRenderer, ExcelReportExporter>();
        services.AddSingleton<IReportRenderer, PdfReportExporter>();

        // Exporters
        services.AddSingleton<IReportExporter, CsvReportExporter>();
        services.AddSingleton<IReportExporter, ExcelReportExporter>();
        services.AddSingleton<IReportExporter, PdfReportExporter>();

        // Service
        services.AddSingleton<IReportService, ReportService>();

        return services;
    }
}