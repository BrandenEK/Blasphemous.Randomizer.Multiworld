using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Blasphemous.Randomizer.Multiworld.Models;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blasphemous.Randomizer.Multiworld;

/// <summary>
/// Responsible for scouting all locations on connect and storing item info
/// </summary>
public class LocationScouter
{
    private readonly Dictionary<string, MultiworldItem> _multiworldItems = new();
    private readonly List<KeyValuePair<string, long>> _idMapping = new();

    private bool _waitingForScout = false;

    //public LocationScouter()
    //{
    //    Main.Multiworld.APManager.OnConnect += OnConnect;
    //}

    /// <summary>
    /// Replaces the Randomizer getItem method with one that returns the items loaded by multiworld
    /// </summary>
    public MultiworldItem GetItemAtLocation(string locationId)
    {
        if (_multiworldItems.TryGetValue(locationId, out MultiworldItem item))
            return item;

        Main.Multiworld.LogError($"Location {locationId} was not recevied!");
        return new MultiworldOtherItem(locationId, "Unknown item", "Unknown player", MultiworldOtherItem.ItemType.Basic);
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

    /// <summary>
    /// Loads location info by getting the list of all locations from slotdata
    /// </summary>
    public IEnumerator LoadLocationsV1(LoginSuccessful success)
    {
        Main.Multiworld.LogWarning("Loading location info using v1");
        ResetLocationInfo();

        // Get location list from slot data
        MultiworldLocationV1[] locations = ((JArray)success.SlotData["locations"]).ToObject<MultiworldLocationV1[]>();

        foreach (MultiworldLocationV1 location in locations)
        {
            // Add id mapping
            _idMapping.Add(new KeyValuePair<string, long>(location.id, location.ap_id));

            MultiworldItem item = location.player_name == Main.Multiworld.ClientSettings.Name // Probably wont work
                ? GetSelfItem(location.id, location.name)
                : GetOtherItem(location.id, location.name, location.player_name, location.type);

            // Add item to mappedItems
            _multiworldItems.Add(location.id, item);
        }

        yield return null;
    }

    /// <summary>
    /// Loads location info by scouting all locations after getting the mapping through slotdata
    /// </summary>
    public IEnumerator LoadLocationsV2(LoginSuccessful success)
    {
        Main.Multiworld.LogWarning("Loading location info using v2");
        ResetLocationInfo();

        // Get location list from slot data
        MultiworldLocationV2[] locations = ((JArray)success.SlotData["locationinfo"]).ToObject<MultiworldLocationV2[]>();

        foreach (MultiworldLocationV2 location in locations)
        {
            // Add id mapping
            _idMapping.Add(new KeyValuePair<string, long>(location.GameId, location.ApId));
        }

        _waitingForScout = true;
        Main.Multiworld.APManager.ScoutMultipleLocations(locations.Select(x => x.ApId), OnScoutLocationsV2);
        yield return new WaitUntil(() => !_waitingForScout);
        yield return new WaitForSecondsRealtime(5);
    }

    private void OnScoutLocationsV2(Dictionary<long, ScoutedItemInfo> items)
    {
        Main.Multiworld.Log("Received location scout info");

        foreach (var kvp in items)
        {
            string internalId = MultiworldToInternalId(kvp.Key);
            ScoutedItemInfo itemInfo = kvp.Value;

            MultiworldItem item = kvp.Value.Player.Slot == Main.Multiworld.APManager.PlayerSlot
                ? GetSelfItem(internalId, itemInfo.ItemName)
                : GetOtherItem(internalId, itemInfo.ItemName, itemInfo.Player.Name, (byte)itemInfo.Flags);

            // Add item to mappedItems
            _multiworldItems.Add(internalId, item);
        }

        _waitingForScout = false;
    }

    private void ResetLocationInfo()
    {
        _multiworldItems.Clear();
        _idMapping.Clear();
    }

    private MultiworldItem GetSelfItem(string id, string name)
    {
        return new MultiworldSelfItem(id, Main.Randomizer.data.items.Values.First(x => x.name == name), name);
    }

    private MultiworldItem GetOtherItem(string id, string name, string player, byte type)
    {
        return new MultiworldOtherItem(id, name, player, (MultiworldOtherItem.ItemType)type);
    }
}
