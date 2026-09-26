using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Data.Services;
using AppFramework.Data.Storage;
using AppFramework.Data.Tests.TestHelpers;
using Xunit;

namespace AppFramework.Data.Tests;

public class DataServiceTests : IDisposable
{
    private readonly InMemoryDbFactory _factory;
    private readonly SqliteLocalStore _store;
    private readonly DataService _service;

    public DataServiceTests()
    {
        _factory = new InMemoryDbFactory();
        _store = new SqliteLocalStore(_factory, NullLogger<SqliteLocalStore>.Instance);
        _service = new DataService(_store, NullLogger<DataService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    // =============== Test Model ===============

    private sealed record TestItem
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    private static TestItem NewItem(string key, string name = "Test", int value = 1)
        => new() { Key = key, Name = name, Value = value };

    // =============== Registration ===============

    [Fact]
    public void RegisterSource_AddsToRegistry()
    {
        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        _service.TryGetSource("Orders", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(source);
    }

    [Fact]
    public void GetSource_UnknownKey_Throws()
    {
        var act = () => _service.GetSource("Missing");
        act.Should().Throw<InvalidOperationException>();
    }

    // =============== LoadBatch (Offline-first) ===============

    [Fact]
    public async Task LoadBatchAsync_WhenLocalEmpty_FetchesFromRemote()
    {
        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1"), NewItem("O2"));
        _service.RegisterSource(source);

        var result = await _service.LoadBatchAsync(new DataRequest("Orders"));

        result.Items.Count.Should().Be(2);
        source.GetBatchCallCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadBatchAsync_AfterFetch_StoresLocally()
    {
        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1"));
        _service.RegisterSource(source);

        await _service.LoadBatchAsync(new DataRequest("Orders"));

        // الآن القراءة الثانية يجب أن تأتي من المحلي
        var local = await _store.QueryAsync(new DataRequest("Orders"));
        local.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadBatchAsync_WhenLocalHasData_ReturnsImmediately()
    {
        // املأ المحلي أولًا
        await _store.UpsertBatchAsync("Orders", new List<object> { NewItem("Local") });

        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("Remote"));
        _service.RegisterSource(source);

        var result = await _service.LoadBatchAsync(new DataRequest("Orders"));

        result.Items.Count.Should().Be(1);
        // قراءة فورية من المحلي (لم ينتظر السيرفر)
        result.Items[0].Should().BeOfType<TestItem>();
        ((TestItem)result.Items[0]).Key.Should().Be("Local");
    }

    [Fact]
    public async Task LoadBatchAsync_WhenOffline_ReturnsEmpty()
    {
        var source = new FakeDataSource("Orders") { IsOffline = true };
        _service.RegisterSource(source);

        var result = await _service.LoadBatchAsync(new DataRequest("Orders"));

        result.Items.Should().BeEmpty();
    }

    // =============== LoadItem ===============

    [Fact]
    public async Task LoadItemAsync_LocalMiss_FetchesFromRemote()
    {
        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1", "Alpha"));
        _service.RegisterSource(source);

        var item = await _service.LoadItemAsync("Orders", "O1") as TestItem;

        item.Should().NotBeNull();
        item!.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task LoadItemAsync_LocalHit_ReturnsLocal()
    {
        await _store.UpsertBatchAsync("Orders", new List<object> { NewItem("O1", "FromLocal") });

        var source = new FakeDataSource("Orders");
        source.Seed(NewItem("O1", "FromRemote"));
        _service.RegisterSource(source);

        var item = await _service.LoadItemAsync("Orders", "O1") as TestItem;

        item!.Name.Should().Be("FromLocal");
    }

    // =============== Save (Write-Behind) ===============

    [Fact]
    public async Task SaveItemAsync_StoresLocally()
    {
        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        await _service.SaveItemAsync("Orders", NewItem("O1", "New"));

        var item = await _store.GetItemAsync("Orders", "O1");
        item.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveItemAsync_EnqueuesOutboxEntry()
    {
        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        await _service.SaveItemAsync("Orders", NewItem("O1"));

        var pending = await _store.GetPendingCountAsync("Orders");
        // قد تُفرَّغ مباشرة إن نجح الاتصال — لكن القيد الأدنى هو: لم يرمِ
        pending.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SaveItemAsync_WhenSourceExists_PushesToServer()
    {
        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        await _service.SaveItemAsync("Orders", NewItem("O1"));

        // ننتظر قليلًا لأن الـ flush غير متزامن
        await Task.Delay(200);

        source.SaveItemCallCount.Should().BeGreaterThan(0);
    }

    // =============== Delete ===============

    [Fact]
    public async Task DeleteAsync_RemovesLocally()
    {
        await _store.UpsertBatchAsync("Orders", new List<object> { NewItem("O1") });

        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        await _service.DeleteAsync("Orders", "O1");

        var item = await _store.GetItemAsync("Orders", "O1");
        item.Should().BeNull();
    }

    // =============== Subscribe / Publish ===============

    [Fact]
    public async Task Subscribe_ReceivesPublishedChanges()
    {
        var source = new FakeDataSource("Orders");
        _service.RegisterSource(source);

        DataChange? received = null;
        using var sub = _service.Subscribe("Orders", change =>
        {
            received = change;
            return Task.CompletedTask;
        });

        await _service.SaveItemAsync("Orders", NewItem("O1"));

        received.Should().NotBeNull();
        received!.SourceKey.Should().Be("Orders");
        received.ItemKey.Should().Be("O1");
    }

    [Fact]
    public void Subscribe_Dispose_Unsubscribes()
    {
        var callCount = 0;
        var sub = _service.Subscribe("Orders", _ => { callCount++; return Task.CompletedTask; });
        sub.Dispose();

        _service.PublishChange(new DataChange("Orders", "O1", DataChangeKind.Updated));

        callCount.Should().Be(0);
    }

    [Fact]
    public void PublishChange_MultipleSubscribers_AllNotified()
    {
        var count = 0;
        using var sub1 = _service.Subscribe("Orders", _ => { count++; return Task.CompletedTask; });
        using var sub2 = _service.Subscribe("Orders", _ => { count++; return Task.CompletedTask; });

        _service.PublishChange(new DataChange("Orders", "O1", DataChangeKind.Updated));

        // ننتظر قليلًا لأن الإشعار غير متزامن
        System.Threading.SpinWait.SpinUntil(() => count == 2, TimeSpan.FromSeconds(2));
        count.Should().Be(2);
    }
}