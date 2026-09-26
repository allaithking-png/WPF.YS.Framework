# WPF.YS.Framework

> إطار عمل WPF محايد لبناء تطبيقات متكاملة: DI، Navigation، Menu، Reports، Data (Offline-first)، View Templates، Custom Controls — بعقود نظيفة وبلا ارتباط بمكتبات UI معيّنة.

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)
![Tests](https://img.shields.io/badge/tests-111%20passing-brightgreen)

---

## ✨ الميزات

| الميزة | الوصف |
|---|---|
| 🧩 **محايد** | لا Prism، لا مكتبات UI. `Microsoft.Extensions.*` فقط. |
| 🎯 **Contracts-first** | واجهات لكل شيء. Base classes اختيارية. |
| 📦 **Offline-first** | SQLite + Outbox + Sync Engine. يعمل بلا إنترنت. |
| 🎨 **Themes** | Light / Dark / Corporate + Custom. تبديل في runtime. |
| 🖥️ **View Templates** | Grid / List / Card / Kanban / Timeline + Custom. |
| 🔌 **Backend-agnostic** | REST / OData / XPO / GraphQL / أي شيء. |
| 🔍 **Auto-scan** | Attributes + AssemblyScanner. تسجيل تلقائي. |
| ⚡ **Async-first** | كل عملية I/O غير متزامنة. |

---

## 🚀 البداية السريعة

### 1. تثبيت الحزم

```bash
dotnet add package AppFramework.Hosting
dotnet add package AppFramework.Controls
```

### 2. تسجيل الإطار

```csharp
// App.xaml.cs
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    var builder = new AppHostBuilder(e.Args)
        .AddFramework(f =>
        {
            f.ConnectionString = null;   // افتراضي في %APPDATA%
            f.EnableSync = true;
            f.SyncInterval = TimeSpan.FromMinutes(5);
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
```

### 3. عرّف شاشة

```csharp
[Screen("Orders.List", "الطلبات", DataSourceKey = "Orders")]
[ViewTemplate(ViewMode.Grid, typeof(OrdersGridView), IsDefault = true, Title = "شبكة")]
[ViewTemplate(ViewMode.Kanban, typeof(OrdersKanbanView), Title = "كانبان")]
public sealed partial class OrdersViewModel
    : ObservableObject, IAppAware, IScreenAware, IDataAware, IViewModeAware
{
    public string ScreenId => "Orders.List";
    public string ScreenTitle => "الطلبات";
    public string? DataSourceKey => "Orders";

    public ObservableCollection<OrderDto> Items { get; } = new();

    public async Task LoadInitialAsync(CancellationToken ct = default)
    {
        var data = AppServices.Get<IDataService>();
        var result = await data.LoadBatchAsync(new DataRequest("Orders", 1, 50), ct);

        Items.Clear();
        foreach (var item in result.Items)
            if (item is OrderDto dto) Items.Add(dto);
    }

    // باقي الواجهات...
}
```

### 4. عرّف مصدر بيانات

```csharp
[DataSource("Orders")]
public sealed class OrdersDataSource : IDataSource
{
    public string Key => "Orders";

    public async Task<PagedResult<object>> GetBatchAsync(DataRequest request, CancellationToken ct)
    {
        // من API أو من قاعدة البيانات — أي شيء
        var orders = await _api.GetOrdersAsync(request.Page, request.PageSize, ct);
        return new PagedResult<object>(orders.Cast<object>().ToList(), orders.Total, request.Page, request.PageSize);
    }

    // باقي الطرق...
}
```

### 5. اعرض الشاشة

```xml
<Window x:Class="MyApp.MainWindow">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <menus:MenuHostControl x:Name="PART_MenuHost"
                               Grid.Row="0"
                               PreferredHost="MenuBar"/>

        <ContentControl x:Name="PART_MainContent" Grid.Row="1"/>
    </Grid>
</Window>
```

```csharp
// في MainWindow.xaml.cs
public MainWindow(IServiceProvider services)
{
    InitializeComponent();

    PART_MenuHost.MenuManager = services.GetRequiredService<IMenuManager>();
    PART_MenuHost.ProviderRegistry = services.GetRequiredService<MenuProviderRegistry>();

    var viewHost = services.GetRequiredService<IViewTemplateHost>();
    viewHost.ViewRendered += (_, e) => Dispatcher.Invoke(() =>
        PART_MainContent.Content = e.View);

    var nav = services.GetRequiredService<INavigationService>();
    _ = nav.OpenScreenAsync("Orders.List");
}
```

---

## 🏗️ المعمارية

```
┌─────────────────────────────────────────────────┐
│  Your Application                               │
│  (Screens, ViewModels, DataSources)             │
└─────────────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌──────────────┐ ┌──────────┐ ┌──────────────┐
│  Hosting     │ │ Controls │ │   Core       │
│ AppHostBuilder│ │ Themes  │ │ Navigation   │
│ Scanner      │ │ Menus   │ │ MenuManager  │
│              │ │Attached  │ │ OperationBus │
└──────────────┘ └──────────┘ │ ViewTemplates│
        │             │       └──────────────┘
        └─────────────┼─────────────┘
                      ▼
              ┌──────────────┐
              │  Data/Sync   │
              │ SQLite+Outbox│
              │  SyncEngine  │
              └──────────────┘
                      ▼
              ┌──────────────┐
              │ Abstractions │
              │ (Contracts)  │
              └──────────────┘
```

**تفاصيل كاملة**: [docs/architecture.md](docs/architecture.md)

---

## 📚 التوثيق

- [البداية السريعة](docs/getting-started.md)
- [المعمارية](docs/architecture.md)
- [نظام البيانات](docs/data-layer.md) _(قريبًا)_
- [المزامنة](docs/offline-sync.md) _(قريبًا)_
- [القوائم والتنقل](docs/menu-navigation.md) _(قريبًا)_

---

## 🧪 الاختبارات

```bash
dotnet test AppFramework.slnx
```

**111 اختبارًا ناجحًا** تغطي:
- Contracts & Models
- Core (Menu, OperationBus, Navigation, ViewTemplates)
- Data (SQLite, Outbox, DataService)
- Sync (Outbox Worker, Connectivity)
- Controls (Menu Providers, Themes, Attached Properties)

---

## 📦 الحزم

| الحزمة | الوصف |
|---|---|
| `AppFramework.Abstractions` | Contracts + Models + Attributes |
| `AppFramework.Core` | Navigation, Menu, OperationBus, ViewTemplates |
| `AppFramework.Data` | SQLite, Outbox, DataService, HttpDataSource |
| `AppFramework.Sync` | SyncEngine, Connectivity, BackgroundWorker |
| `AppFramework.Controls` | MenuProviders, Themes, Attached Properties |
| `AppFramework.Hosting` | AppHostBuilder + Scanner + DI |

---

## 🗺️ Roadmap

- [x] Repository Scaffold
- [x] Abstractions Layer
- [x] Core Foundation
- [x] Navigation Service
- [x] Menu Providers
- [x] Data Layer
- [x] Data Remote
- [x] Sync Engine
- [x] View Templates
- [x] Custom Controls + Themes
- [x] Hosting + Sample App

**🎉 v1.0.0-alpha.1 مكتمل**

---

## 🤝 المساهمة

راجع [.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md) قبل فتح PR.

---

## 📄 الترخيص

MIT — راجع [LICENSE](LICENSE).