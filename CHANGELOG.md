# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### PR-01 — Abstractions Layer
- **Contracts** (10): IAppAware, IScreenAware, IDataAware, IDataItemAware, IMenuAware, IViewModeAware, IOperationAware, IReportAware, INavigationAware, IAuthorizable
- **Models — Data** (6): DataRequest, DataLoadStrategy, PagedResult, ItemKey, DataChange, DataChangeKind
- **Models — Navigation** (2): NavigationContext, NavigationResult
- **Models — Menu** (4): MenuDefinition, MenuContext, MenuHost, MenuItemDescriptor
- **Models — Operations** (2): OperationContext, OperationResult
- **Models — ViewTemplates** (3): ViewMode, ViewModeCategory, ViewTemplateDescriptor
- **Models — Screens** (2): ScreenViewMode, ScreenRegistration
- **Models — Reports** (3): ReportRequest, ReportResult, ReportExportFormat
- **Services** (11): IDataService, IDataSource, INavigationService, IMenuManager, IOperationBus, IReportService, IViewTemplateRegistry, IViewTemplateHost, INotificationService, ISyncService, IUserContext
- **Attributes** (5): ScreenAttribute, DataSourceAttribute, ViewTemplateAttribute, PersistAttribute, AuthorizeAttribute

#### PR-00 — Repository Scaffold
- Initial solution structure (AppFramework.slnx)
- 6 source projects + 3 test projects
- Unified build settings (Directory.Build.props, Directory.Packages.props)
- .editorconfig + .gitignore
- CI workflow (GitHub Actions)
- PR template
- MIT license
- README + CHANGELOG

---

## How to Release

1. Update `CHANGELOG.md` and move items from `[Unreleased]` to a new version section.
2. Update `<VersionPrefix>` and `<VersionSuffix>` in `Directory.Build.props`.
3. Commit and tag: `git tag v1.0.0` and push.