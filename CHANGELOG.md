# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-alpha.1] - 2026-09-26

### Added

#### PR-10 — Hosting + Sample App
- `AppHostBuilder` — unified builder for all services
- `AppHost` — start/stop application lifecycle
- `ViewTemplateScanner` — auto-register `[ViewTemplate]`
- `SalesApp.Wpf` sample with Orders + Products screens
- Grid + Kanban views for Orders
- Theme switcher in runtime
- Complete README + Getting Started guide

#### PR-09 — Custom Controls + Themes
- `IconProperties` (Icon, Position, IconSize)
- `LoadingProperties` (IsLoading, LoadingText)
- `ValidationProperties` (IsValid, ErrorMessage, ErrorBrush)
- `IThemeService` + `ThemeManager`
- 3 themes: Light, Dark, Corporate
- `Controls.xaml` with styles for all major controls

#### PR-08 — View Templates Registry + Host
- `ViewTemplateRegistry` — per-ViewModel view templates
- `ViewTemplateHost` — runtime mode switching
- `JsonViewModePersistence` — per-user preferences
- Generic views: Grid, List, Card

#### PR-07 — Sync Engine
- `SyncService` — Outbox push + remote pull
- `NetworkConnectivityMonitor` — network change detection
- `SyncBackgroundService` — IHostedService worker
- Auto-sync on connectivity restored

#### PR-06 — Data Remote
- `DataService` — Offline-first orchestration
- `HttpDataSource` — REST example
- Write-Behind pattern (Outbox + async push)
- Pub/Sub per source

#### PR-05 — Data Layer (Local)
- `LocalDbContext` + EF Core + SQLite
- `SqliteLocalStore` — ILocalStore implementation
- Outbox pattern for sync
- JSON payload storage

#### PR-04 — Menu Providers
- `IMenuProvider` + MenuBar/Toolbar/Ribbon providers
- `MenuHostControl` — smart UserControl
- `MenuViewService` helper

#### PR-03 — Navigation Service
- `NavigationService` — full INavigationService
- `ScreenRegistry` — auto-populated
- `WpfScreenViewResolver` — convention-based
- `JsonNavigationSessionService` — session persistence

#### PR-02 — Core Foundation
- Base classes (BaseViewModel, BaseScreen, etc.)
- `MenuManager` — full implementation
- `OperationBus` — Before/Started/Completed events
- `AssemblyScanner` — attribute-based discovery

#### PR-01 — Abstractions Layer
- 10 contracts (IAppAware, IScreenAware, etc.)
- 22 models (Data, Navigation, Menu, Operations, ViewTemplates, Screens, Reports)
- 11 service interfaces
- 5 attributes

#### PR-00 — Repository Scaffold
- Solution structure
- Build settings (Directory.Build.props, Directory.Packages.props)
- CI workflow
- Documentation

---

## How to Release

1. Update `CHANGELOG.md` and move items from `[Unreleased]` to a new version section.
2. Update `<VersionPrefix>` and `<VersionSuffix>` in `Directory.Build.props`.
3. Commit and tag: `git tag v1.0.0` and push.