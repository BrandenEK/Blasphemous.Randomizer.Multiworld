using Archipelago.MultiClient.Net;
using Blasphemous.Randomizer.Multiworld.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.Randomizer.Multiworld;

/// <summary>
/// Responsible for scouting all locations on connect and storing item info
/// </summary>
public class LocationScouter
{
    private readonly Dictionary<string, MultiworldItem> _multiworldItems = new();
    private readonly List<KeyValuePair<string, long>> _idMapping = new();

    public LocationScouter()
    {
        Main.Multiworld.APManager.OnConnect += OnConnect;
    }

    /// <summary>
    /// Replaces the Randomizer getItem method with one that returns the items loaded by multiworld
    /// </summary>
    public MultiworldItem GetItemAtLocation(string locationId)
    {
        if (_multiworldItems.TryGetValue(locationId, out MultiworldItem item))
            return item;

        Main.Multiworld.LogError($"Location {locationId} was not recevied!");
        return null;
    }

    /// <summary>
    /// Converts an internal id to a multiworld one
    /// </summary>
    public long InternalToMultiworldId(string locationId)
    {
        return _idMapping.First(x => x.Key == locationId).Value;
    }

    /// <summary>
    /// Converts a multiworld id to an internal one
    /// </summary>
    public string MultiworldToInternalId(long locationId)
    {
        return _idMapping.First(x => x.Value == locationId).Key;
    }

    private void OnConnect(LoginResult login)
    {
        if (login is not LoginSuccessful success)
            return;

        _multiworldItems.Clear();
        _idMapping.Clear();

        // Get location list from slot data
        ArchipelagoLocation[] locations = ((JArray)success.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();

        foreach (ArchipelagoLocation location in locations)
        {
            // Add id mapping
            _idMapping.Add(new KeyValuePair<string, long>(location.id, location.ap_id));

            MultiworldItem item = location.player_name == Main.Multiworld.ClientSettings.Name // Probably wont work
                ? GetSelfItem(location)
                : GetOtherItem(location);

            // Add item to mappedItems
            _multiworldItems.Add(location.id, item);
        }
    }

    private MultiworldItem GetSelfItem(ArchipelagoLocation location)
    {
        return new MultiworldSelfItem(location.id, Main.Randomizer.data.items.Values.First(x => x.name == location.name), location.name);
    }

    private MultiworldItem GetOtherItem(ArchipelagoLocation location)
    {
        return new MultiworldOtherItem(location.id, location.name, location.player_name, (MultiworldOtherItem.ItemType)location.type);
    }
}
