# البداية السريعة

دليل شامل لإعداد مشروع WPF جديد باستخدام WPF.YS.Framework.

---

## المتطلبات

- **.NET 10 SDK** أو أحدث
- **Visual Studio 2022** أو **VS Code** مع C# Dev Kit
- **Git** (اختياري، للتحكم بالإصدارات)

---

## 1. إنشاء المشروع

```bash
dotnet new wpf -n MyApp -f net10.0
cd MyApp
```

---

## 2. تثبيت حزم الإطار

```bash
dotnet add package AppFramework.Hosting
dotnet add package AppFramework.Controls
```

أو عبر `PackageReference` في `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\AppFramework.Hosting\AppFramework.Hosting.csproj" />
  <ProjectReference Include="..\..\src\AppFramework.Controls\AppFramework.Controls.csproj" />
</ItemGroup>
```

---

## 3. تهيئة `App.xaml.cs`

```csharp
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Theming;
using AppFramework.Hosting;

namespace MyApp;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new AppHostBuilder(e.Args)
            .AddFramework(f =>
            {
                f.EnableSync = true;
                f.AutoScanAssemblies.Add(typeof(App).Assembly);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<MainWindow>();
            });

        await AppHost.StartAsync(builder);

        var main = AppHost.GetService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();
        base.OnExit(e);
    }
}
```

---

## 4. عرّف أول شاشة

### `ViewModels/HomeViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;

namespace MyApp.ViewModels;

[Screen("Home", "الرئيسية")]
public sealed partial class HomeViewModel : ObservableObject, IAppAware, IScreenAware
{
    private IServiceProvider? _services;

    public string ScreenId => "Home";
    public string ScreenTitle => "الرئيسية";

    [ObservableProperty] private string _message = "مرحبًا بك في WPF.YS.Framework";

    public void AttachServices(IServiceProvider services) => _services = services;

    public Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task OnDeactivatedAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> OnClosingAsync(CancellationToken ct = default) => Task.FromResult(true);
}
```

### `Views/HomeView.xaml`

```xml
<UserControl x:Class="MyApp.Views.HomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock Text="{Binding Message}" 
                   FontSize="24" 
                   Foreground="{DynamicResource App.PrimaryBrush}"/>
    </StackPanel>
</UserControl>
```

**ملاحظة**: أضف `[ViewTemplate(ViewMode.Grid, typeof(HomeView))]` فوق `HomeViewModel`.

---

## 5. الـ Shell (MainWindow)

### `MainWindow.xaml`

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:menus="clr-namespace:AppFramework.Controls.Menus;assembly=AppFramework.Controls"
        Title="MyApp" Width="1000" Height="600"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <menus:MenuHostControl x:Name="PART_MenuHost"
                               Grid.Row="0"
                               PreferredHost="MenuBar"/>

        <ContentControl x:Name="PART_MainContent" Grid.Row="1" Margin="8"/>
    </Grid>
</Window>
```

### `MainWindow.xaml.cs`

```csharp
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Menus;
using AppFramework.Core.ViewTemplates;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();

        PART_MenuHost.MenuManager = services.GetRequiredService<IMenuManager>();
        PART_MenuHost.ProviderRegistry = services.GetRequiredService<MenuProviderRegistry>();

        var viewHost = services.GetRequiredService<IViewTemplateHost>();
        viewHost.ViewRendered += (_, e) => Dispatcher.Invoke(() =>
            PART_MainContent.Content = e.View);

        var navigation = services.GetRequiredService<INavigationService>();
        Loaded += async (_, _) => await navigation.OpenScreenAsync("Home");
    }
}
```

---

## 6. تشغيل

```bash
dotnet run
```

---

## الخطوات التالية

- [Data Layer](data-layer.md) — ربط قاعدة بيانات
- [Offline Sync](offline-sync.md) — العمل بدون إنترنت
- [View Templates](view-templates.md) — طرق عرض متعددة
- [Themes](theming.md) — تخصيص المظهر

---

## المراجع

- [المعمارية العامة](architecture.md)
- [الأمثلة](../../samples/)