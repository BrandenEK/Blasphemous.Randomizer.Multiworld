﻿using Blasphemous.Randomizer.ItemRando;
using UnityEngine;

namespace Blasphemous.Randomizer.Multiworld.Models;

public abstract class MultiworldItem : Item
{
    /// <summary>
    /// The BlasRando internal location id where this item is found
    /// </summary>
    public string LocationId { get; }

    public MultiworldItem(string locationId, string name, string hint, int type, bool progression) : base("AP", name, hint, type, progression, 0)
    {
        LocationId = locationId;
    }

    public sealed override void addToInventory()
    {
        Main.Multiworld.APManager.SendLocation(LocationId);
    }
}

/// <summary>
/// A multiworld item that belongs to this player
/// </summary>
public class MultiworldSelfItem : MultiworldItem
{
    /// <summary>
    /// The item that this item respresents internally
    /// </summary>
    public Item InternalItem { get; }

    /// <summary>
    /// The name that will be displayed for this item (Doesn't have progress-dependent text resolution)
    /// </summary>
    public string DisplayName { get; }

    public MultiworldSelfItem(string locationId, Item item, string name) : base(locationId, item.name, item.hint, item.type, item.progression)
    {
        InternalItem = item;
        DisplayName = name;
    }

    public override string GetName(bool upgraded) => DisplayName;

    public override string GetDescription(bool upgraded) => InternalItem.GetDescription(upgraded);

    public override string GetNotification(bool upgraded) => InternalItem.GetNotification(upgraded);

    public override Sprite GetImage(bool upgraded) => InternalItem.GetImage(upgraded);
}

/// <summary>
/// A multiworld item that belongs to a different player
/// </summary>
public class MultiworldOtherItem : MultiworldItem
{
    /// <summary>
    /// The name of the player that this item belongs to
    /// </summary>
    public string PlayerName { get; }
    
    private readonly ItemType _type;

    public MultiworldOtherItem(string locationId, string name, string player, ItemType type) : base(locationId, name, "[AP]", 200, type == ItemType.Progression)
    {
        PlayerName = player;
        _type = type;
    }

    public override string GetName(bool upgraded) => name;

    public override string GetDescription(bool upgraded) => GetTextWithPlayerName(_type switch
    {
        ItemType.Progression => "arprog",
        ItemType.Useful => "arusef",
        ItemType.Trap => "artrap",
        _ => "arbasc"
    });

    public override string GetNotification(bool upgraded) => GetTextWithPlayerName("arnot");

    public override Sprite GetImage(bool upgraded) => Main.Multiworld.ImageAP;

    private string GetTextWithPlayerName(string term)
    {
        string text = Main.Multiworld.LocalizationHandler.Localize(term);
        return text.Replace("*", PlayerName);
    }

    public enum ItemType
    {
        Basic = 0,
        Progression = 1,
        Useful = 2,
        Trap = 4,
    }
}
