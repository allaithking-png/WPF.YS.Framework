using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;
using AppFramework.Data.Services;
using AppFramework.Data.Storage;
using AppFramework.Data.Tests.TestHelpers;
using AppFramework.Sync.Services;
using AppFramework.Sync.Tests.TestHelpers;
using Xunit;

namespace AppFramework.Sync.Tests;

public class SyncServiceTests : IDisposable
{
    private readonly InMemoryDbFactory _factory;
    private readonly SqliteLocalStore _store;
    private readonly DataService _dataService;
    private readonly FakeConnectivityMonitor _connectivity;
    private readonly SyncService _sync;

    public SyncServiceTests()
    {
        _factory = new InMemoryDbFactory();
        _store = new SqliteLocalStore(_factory, NullLogger<SqliteLocalStore>.Instance);
        _dataService = new DataService(_store, NullLogger<DataService>.Instance);
        _connectivity = new FakeConnectivityMonitor(startOnline: true);
        _sync = new SyncService(
            _dataService,
            _store,
            _connectivity,
            NullLogger<SyncService>.Instance);
    }

    public void Dispose()
    {
        _sync.Dispose();
        _factory.Dispose();
    }

    // =============== Test Model ===============

    private sealed record TestItem
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
    }

    private static TestItem NewItem(string key, string name = "Test")
        => new() { Key = key, Name = name };

    // =============== Tests ===============

    [Fact]
    public async Task SyncAsync_WhenOffline_ReturnsZero()
    {
        _connectivity.SetOnline(false);

        var result = await _sync.SyncAsync("Orders");

        result.Pushed.Should().Be(0);
        result.Pulled.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_WithNoPendingChanges_PullsFromRemote()
    {
        // ترتيب: مصدر فيه عناصر
        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1"), NewItem("O2"));
        _dataService.RegisterSource(source);

        var result = await _sync.SyncAsync("Orders");

        result.Pulled.Should().Be(2);
    }

    [Fact]
    public async Task SyncAsync_PushesPendingChanges()
    {
        var source = new FakeDataSource("Orders");
        _dataService.RegisterSource(source);

        // سجّل مصدر للمزامنة
        _sync.RegisterSource("Orders");

        // أضف تغييرًا معلّقًا يدويًا (لأن DataService يفرّغ تلقائيًا)
        await _store.EnqueueChangeAsync(
            new DataChange("Orders", "O1", DataChangeKind.Updated, NewItem("O1")));

        var result = await _sync.SyncAsync("Orders");

        result.Pushed.Should().Be(1);
    }

    [Fact]
    public async Task SyncAsync_WhenPushed_ClearsOutbox()
    {
        var source = new FakeDataSource("Orders");
        _dataService.RegisterSource(source);
        _sync.RegisterSource("Orders");

        await _store.EnqueueChangeAsync(
            new DataChange("Orders", "O1", DataChangeKind.Updated, NewItem("O1")));

        await _sync.SyncAsync("Orders");

        var pending = await _store.GetPendingCountAsync("Orders");
        pending.Should().Be(0);
    }

    [Fact]
    public async Task SyncAllAsync_IteratesAllRegisteredSources()
    {
        var source1 = new FakeDataSource("Orders");
        source1.Seed(NewItem("O1"));
        var source2 = new FakeDataSource("Customers");
        source2.Seed(NewItem("C1"), NewItem("C2"));

        _dataService.RegisterSource(source1);
        _dataService.RegisterSource(source2);
        _sync.RegisterSource("Orders");
        _sync.RegisterSource("Customers");

        var result = await _sync.SyncAllAsync();

        result.Pulled.Should().Be(3);
    }

    [Fact]
    public async Task SyncAsync_FiresSyncCompletedEvent()
    {
        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1"));
        _dataService.RegisterSource(source);

        SyncResult? received = null;
        _sync.SyncCompleted += (_, r) => received = r;

        await _sync.SyncAsync("Orders");

        received.Should().NotBeNull();
        received!.SourceKey.Should().Be("Orders");
    }

    [Fact]
    public void IsOnline_ReflectsConnectivityMonitor()
    {
        _sync.IsOnline.Should().BeTrue();
        _connectivity.SetOnline(false);
        _sync.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void ConnectivityChanged_FiresOnChange()
    {
        bool? received = null;
        _sync.ConnectivityChanged += (_, online) => received = online;

        _connectivity.SetOnline(false);

        received.Should().BeFalse();
    }

    [Fact]
    public void StartAutoSync_DoesNotThrow()
    {
        var act = () => _sync.StartAutoSync(TimeSpan.FromSeconds(30));
        act.Should().NotThrow();

        _sync.StopAutoSync();
    }

    [Fact]
    public async Task SyncAllAsync_NoSourcesRegistered_ReturnsEmpty()
    {
        var result = await _sync.SyncAllAsync();
        result.Pushed.Should().Be(0);
        result.Pulled.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_UnknownSource_DoesNotThrow()
    {
        // لا مصدر مسجّل باسم "Missing"
        var result = await _sync.SyncAsync("Missing");
        result.Pushed.Should().Be(0);
        result.Pulled.Should().Be(0);
    }
}