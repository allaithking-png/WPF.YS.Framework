using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Data.Storage;
using AppFramework.Data.Tests.TestHelpers;
using Xunit;

namespace AppFramework.Data.Tests;

public class SqliteLocalStoreTests : IDisposable
{
    private readonly InMemoryDbFactory _factory;
    private readonly SqliteLocalStore _store;

    public SqliteLocalStoreTests()
    {
        _factory = new InMemoryDbFactory();
        _store = new SqliteLocalStore(_factory, NullLogger<SqliteLocalStore>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    // =============== Test Model ===============

    private sealed record TestItem
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    private static TestItem NewItem(string key, string name = "Test", int value = 42)
        => new() { Key = key, Name = name, Value = value };

    // =============== Upsert ===============

    [Fact]
    public async Task UpsertBatchAsync_InsertsNewItems()
    {
        var items = new List<object>
        {
            NewItem("A", "Alpha"),
            NewItem("B", "Beta")
        };

        await _store.UpsertBatchAsync("Test", items);

        var result = await _store.QueryAsync(new DataRequest("Test"));
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task UpsertBatchAsync_UpdatesExistingItems()
    {
        await _store.UpsertBatchAsync("Test", new List<object> { NewItem("A", "Alpha", 1) });
        await _store.UpsertBatchAsync("Test", new List<object> { NewItem("A", "Alpha2", 2) });

        var result = await _store.QueryAsync(new DataRequest("Test"));
        result.TotalCount.Should().Be(1);

        var item = result.Items[0] as TestItem;
        item.Should().NotBeNull();
        item!.Name.Should().Be("Alpha2");
        item.Value.Should().Be(2);
    }

    [Fact]
    public async Task UpsertBatchAsync_EmptyList_DoesNothing()
    {
        await _store.UpsertBatchAsync("Test", Array.Empty<object>());
        var result = await _store.QueryAsync(new DataRequest("Test"));
        result.TotalCount.Should().Be(0);
    }

    // =============== Query ===============

    [Fact]
    public async Task QueryAsync_FiltersBySourceKey()
    {
        await _store.UpsertBatchAsync("Orders", new List<object> { NewItem("O1") });
        await _store.UpsertBatchAsync("Customers", new List<object> { NewItem("C1") });

        var result = await _store.QueryAsync(new DataRequest("Orders"));

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_Paginates()
    {
        var items = Enumerable.Range(1, 25).Select(i => (object)NewItem($"K{i}")).ToList();
        await _store.UpsertBatchAsync("Test", items);

        var page1 = await _store.QueryAsync(new DataRequest("Test", Page: 1, PageSize: 10));
        var page2 = await _store.QueryAsync(new DataRequest("Test", Page: 2, PageSize: 10));
        var page3 = await _store.QueryAsync(new DataRequest("Test", Page: 3, PageSize: 10));

        page1.Items.Count.Should().Be(10);
        page2.Items.Count.Should().Be(10);
        page3.Items.Count.Should().Be(5);
        page1.HasMore.Should().BeTrue();
        page3.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task QueryKeysAsync_ReturnsOnlyKeys()
    {
        await _store.UpsertBatchAsync("Test", new List<object> { NewItem("A"), NewItem("B") });

        var result = await _store.QueryKeysAsync(new DataRequest("Test"));

        result.Items.Should().HaveCount(2);
        result.Items.Select(k => k.Key).Should().Contain(new[] { "A", "B" });
    }

    [Fact]
    public async Task GetItemAsync_ReturnsItem()
    {
        await _store.UpsertBatchAsync("Test", new List<object> { NewItem("X", "Xylophone") });

        var item = await _store.GetItemAsync("Test", "X") as TestItem;

        item.Should().NotBeNull();
        item!.Name.Should().Be("Xylophone");
    }

    [Fact]
    public async Task GetItemAsync_UnknownKey_ReturnsNull()
    {
        var item = await _store.GetItemAsync("Test", "Missing");
        item.Should().BeNull();
    }

    // =============== Remove ===============

    [Fact]
    public async Task RemoveAsync_DeletesItem()
    {
        await _store.UpsertBatchAsync("Test", new List<object> { NewItem("A"), NewItem("B") });

        await _store.RemoveAsync("Test", "A");

        var result = await _store.QueryAsync(new DataRequest("Test"));
        result.TotalCount.Should().Be(1);
    }

    // =============== Outbox ===============

    [Fact]
    public async Task EnqueueChangeAsync_AddsToOutbox()
    {
        var change = new DataChange("Test", "A", DataChangeKind.Updated, NewItem("A"));

        await _store.EnqueueChangeAsync(change);

        var count = await _store.GetPendingCountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task DequeuePendingAsync_ReturnsOldest()
    {
        await _store.EnqueueChangeAsync(new DataChange("Test", "A", DataChangeKind.Updated));
        await Task.Delay(10);
        await _store.EnqueueChangeAsync(new DataChange("Test", "B", DataChangeKind.Updated));

        var changes = await _store.DequeuePendingAsync(10);

        changes.Should().HaveCount(2);
        changes[0].ItemKey.Should().Be("A");
    }

    [Fact]
    public async Task MarkSyncedAsync_RemovesFromOutbox()
    {
        var change = new DataChange("Test", "A", DataChangeKind.Updated);
        await _store.EnqueueChangeAsync(change);

        var pending = await _store.DequeuePendingAsync(10);
        await _store.MarkSyncedAsync(pending);

        var count = await _store.GetPendingCountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetPendingCountAsync_FiltersBySourceKey()
    {
        await _store.EnqueueChangeAsync(new DataChange("Orders", "A", DataChangeKind.Updated));
        await _store.EnqueueChangeAsync(new DataChange("Customers", "B", DataChangeKind.Updated));

        var ordersCount = await _store.GetPendingCountAsync("Orders");
        var allCount = await _store.GetPendingCountAsync();

        ordersCount.Should().Be(1);
        allCount.Should().Be(2);
    }
}