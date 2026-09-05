using System.Text.Json;
using Mocha2021.Models;

namespace Mocha2021.Classes;

public class CommerceStore
{
    private readonly RoomDB _roomDb;
    private readonly Dictionary<string, object?> _giftDropStore;
    private readonly Dictionary<string, List<Dictionary<string, object?>>> _clubSearch;

    public static readonly Dictionary<string, int> RoomNameToStorefrontType = new()
    {
        ["RecCenter"] = 300,
        ["RecCenterUGC"] = 300,
    };

    public CommerceStore(RoomDB roomDb, IWebHostEnvironment env)
    {
        _roomDb = roomDb;
        var giftDropPath = Path.Combine(env.ContentRootPath, "Seed", "GiftDropStore.json");
        _giftDropStore = JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(giftDropPath))!;
        var clubSearchPath = Path.Combine(env.ContentRootPath, "Seed", "ClubSearch.json");
        _clubSearch = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object?>>>>(File.ReadAllText(clubSearchPath))!;
    }

    public int ResolveStorefrontType(int roomId)
    {
        var room = _roomDb.GetRoom(roomId);
        if (room != null && RoomNameToStorefrontType.TryGetValue(room.Name, out var t)) return t;
        return 2;
    }

    public List<Dictionary<string, object?>> AllStoreItems()
    {
        if (_giftDropStore.TryGetValue("StoreItems", out var items) && items is JsonElement el)
        {
            return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(el.GetRawText()) ?? new();
        }
        return new();
    }

    public object? NextUpdate() => _giftDropStore.GetValueOrDefault("NextUpdate");

    public double SubscriberDiscountPercent()
    {
        if (_giftDropStore.TryGetValue("SubscriberDiscountPercent", out var v) && v is JsonElement el)
            return el.GetDouble();
        return 0;
    }

    public List<Dictionary<string, object?>> ItemsForStorefront(string storefrontId)
    {
        var all = AllStoreItems();
        if (_giftDropStore.TryGetValue("CuratedShops", out var shopsObj) && shopsObj is JsonElement shopsEl &&
            shopsEl.ValueKind == JsonValueKind.Object && shopsEl.TryGetProperty(storefrontId, out var curatedEl))
        {
            var curatedIds = JsonSerializer.Deserialize<List<int>>(curatedEl.GetRawText()) ?? new();
            var byId = new Dictionary<int, Dictionary<string, object?>>();
            foreach (var entry in all)
            {
                if (entry.TryGetValue("PurchasableItemId", out var pid) && pid != null)
                {
                    var idVal = Convert.ToInt32(pid is JsonElement pe ? pe.GetInt32() : pid);
                    byId.TryAdd(idVal, entry);
                }
            }
            return curatedIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }
        return all;
    }

    public Dictionary<string, object?>? FindStoreItem(int purchasableItemId)
    {
        return AllStoreItems().FirstOrDefault(e =>
            e.TryGetValue("PurchasableItemId", out var pid) && pid != null &&
            Convert.ToInt32(pid is JsonElement pe ? pe.GetInt32() : pid) == purchasableItemId);
    }

    public List<Dictionary<string, object?>> ClubsForCategory(string category, int count)
    {
        if (_clubSearch.TryGetValue(category, out var clubs)) return clubs.Take(count).ToList();
        return new();
    }
}
