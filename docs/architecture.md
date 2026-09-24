# Architecture — WPF.YS.Framework

نظرة معمارية شاملة على إطار العمل.

---

## 1. المبادئ الأساسية

| المبدأ | الشرح |
|---|---|
| **محايد** | لا يعتمد على Prism أو أي مكتبة UI. يستخدم `Microsoft.Extensions.*` القياسية فقط. |
| **Contracts-first** | العقود (`I*` interfaces) تُعرَّف أولًا، والتنفيذ لاحقًا. |
| **اختياري بالكامل** | لا توجد Base Classes إلزامية. أي كلاس يمكنه تنفيذ واجهة واحدة للحصول على ميزة. |
| **Async-first** | كل عملية I/O لها نسخة `Task<T>` مع `CancellationToken`. |
| **Offline-first** | التخزين المحلي (SQLite) أولًا، والمزامنة مع الخادم ثانيًا. |
| **Modular** | كل طبقة (Data, Sync, Core, Controls) في مشروع مستقل. |

---

## 2. البنية العامة
┌─────────────────────────────────────────────────────────────┐
│ Application (SalesApp) │
│ يستخدم الإطار، يعرّف الشاشات والـ ViewModels والـ Services. │
└─────────────────────────────────────────────────────────────┘
│
┌─────────────────────┼─────────────────────┐
▼ ▼ ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────┐
│ Hosting │ │ Controls │ │ Core │
│ AppHostBuilder│ │ Custom Controls│ │ MenuManager │
│ AssemblyScanner│ │ + Themes │ │ NavigationSvc │
│ │ │ │ │ OperationBus │
└───────────────┘ └───────────────┘ └───────────────┘
│ │ │
└─────────────────────┼─────────────────────┘
▼
┌───────────────────┐
│ Abstractions │
│ (Contracts) │
│ + Models │
│ + Attributes │
└───────────────────┘

text

---

## 3. طبقات المشروع (Projects)

| المشروع | المسؤولية | يعتمد على |
|---|---|---|
| **AppFramework.Abstractions** | العقود والنماذج والـ Attributes | (لا شيء من الإطار) |
| **AppFramework.Core** | تنفيذ الخدمات المركزية | Abstractions |
| **AppFramework.Data** | طبقة البيانات + EF Core + SQLite | Abstractions |
| **AppFramework.Sync** | محرك المزامنة + Outbox | Abstractions, Data |
| **AppFramework.Controls** | Custom Controls + Themes (WPF) | Abstractions |
| **AppFramework.Hosting** | DI + AssemblyScanner + Generic Host | كل ما سبق |

---

## 4. العقود (Contracts) — القدرات الاختيارية

كل ViewModel/Screen يختار ما يحتاجه:

| العقد | القدرة |
|---|---|
| `IAppAware` | الحصول على `IServiceProvider` تلقائيًا |
| `IScreenAware` | دورة حياة الشاشة (تفعيل/إلغاء/إغلاق) |
| `IDataAware` | تحميل/حفظ بيانات الشاشة |
| `IDataItemAware` | عنصر بيانات يجلب نفسه |
| `IMenuAware` | بناء قوائم السياق |
| `IViewModeAware` | دعم طرق عرض متعددة |
| `IOperationAware` | الاستجابة لأحداث العمليات |
| `IReportAware` | الإعلان عن تقارير مرتبطة |
| `INavigationAware` | الاستجابة للتنقل |
| `IAuthorizable` | الإعلان عن صلاحيات مطلوبة |

---

## 5. نظام البيانات (Data Flow)
┌──────────────────┐
│ IDataService │
│ (Core Facade) │
└────────┬─────────┘
│
┌────────────────────────┼────────────────────────┐
▼ ▼ ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────┐
│ IDataSource │ │ ILocalStore │ │ Outbox │
│ (REST/XPO) │ │ (SQLite) │ │ (Pending) │
└───────────────┘ └───────────────┘ └───────────────┘

text

### نمط الاستراتيجيات

يدعم `DataLoadStrategy`:
- `KeysOnly` — جلب المفاتيح فقط (للقوائم الطويلة).
- `FirstPage` — جلب الدفعة الأولى.
- `Full` — جلب الكل.
- `OnDemand` — جلب عند الطلب (Lazy).

### Offline-first

1. **القراءة**: `LoadBatchLocalFirstAsync` يقرأ من SQLite أولًا، ثم يُحدّث في الخلفية.
2. **الكتابة**: تُكتب محليًا في `Outbox` ثم تُرسل للخادم عند توفر الاتصال.

---

## 6. نظام المزامنة (Sync)
┌──────────────────────────────────────────────────┐
│ SyncService │
│ ┌──────────────────────────────────────────┐ │
│ │ 1. Push Pending (Outbox → Server) │ │
│ │ 2. Pull Changes (Server → Local) │ │
│ │ 3. Merge + Conflict Resolution │ │
│ │ 4. Update Last Sync Timestamp │ │
│ └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘

text

يعمل عبر `BackgroundService` بشكل دوري (كل 5 دقائق افتراضيًا).

---

## 7. القوائم والتنقل

### MenuManager
- يُدار بـ `IMenuAware`.
- يدعم الـ `MenuHost`: Ribbon / MenuBar / Toolbar / ContextMenu / Explorer.
- القوائم تُبنى سياقيًا (لكل شاشة قائمتها).

### NavigationService
- يدير شاشات مفتوحة.
- يدعم Multi-Open عبر `NavigationContext.IsMultiOpen`.
- يستعيد الحالة من الجلسة السابقة.

---

## 8. Custom Controls (WPF)

بدلًا من الوراثة من مكتبة UI، نستخدم:
- **Attached Properties** (Icon, Loading, Validation).
- **Theme System** (ResourceDictionary قابل للتبديل).
- **XAML Templates** (ControlTemplate مخصص).
- **Behavior** (سلوك قابل لإعادة الاستخدام).

الميزة: **قابلية تبديل المظهر** دون تغيير الكود.

---

## 9. التسجيل التلقائي (Auto-Scan)

عبر **Attributes**:

```csharp
[Screen("Orders.List", "الطلبات", DataSourceKey = "Orders")]
[ViewTemplate(ViewMode.Grid, typeof(OrdersGridView), IsDefault = true)]
[Authorize("Sales.View")]
public sealed class OrdersViewModel : ...
ثم AssemblyScanner يكتشفها ويسجّلها في DI تلقائيًا.

10. تسلسل تنفيذ PR-01 → PR-10
PR	الطبقة	المسؤولية
PR-00	Repository	Scaffold
PR-01 ✅	Abstractions	Contracts + Models + Attributes
PR-02	Core Foundation	BaseViewModel, AssemblyScanner, AppServices
PR-03	Menu Manager	IMenuManager implementation + Providers
PR-04	Navigation	INavigationService implementation
PR-05	Data Local	SQLite + EF Core + ILocalStore
PR-06	Data Remote	IDataSource implementation + IDataService
PR-07	Sync Engine	Outbox + BackgroundService
PR-08	View Templates	Registry + Host + Generic views
PR-09	Custom Controls	Attached Properties + Themes
PR-10	Hosting + Sample	AppHostBuilder + SalesApp
11. المراجع
MVVM Toolkit

EF Core Docs

Generic Host

WPF Attached Properties

text

---

## ثالثًا: Commit + Push للتنظيف

في Terminal VS Code:

```powershell
cd "C:\Users\Owner\source\repos\GitHub Test\WPF.YS.Framework"

# تأكد أنك على main
git checkout main
git pull origin main

# أنشئ فرعًا صغيرًا للتنظيف
git checkout -b chore/docs-and-changelog

# أضف الملفات المعدّلة/الجديدة
git add CHANGELOG.md docs/architecture.md

# commit
git commit -m "docs: update CHANGELOG with PR-00 and PR-01, add architecture overview"

# ارفع
git push -u origin chore/docs-and-changelog