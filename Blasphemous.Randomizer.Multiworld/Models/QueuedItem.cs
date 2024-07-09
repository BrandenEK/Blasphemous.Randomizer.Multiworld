namespace Blasphemous.Randomizer.Multiworld.Models;

/// <summary>
/// An item that has been received and is waiting to be processed
/// </summary>
public class QueuedItem(string itemId, int index, string player)
{
    /// <summary>
    /// The internal item id
    /// </summary>
    public string ItemId { get; } = itemId;

    /// <summary>
    /// The received index
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// The player it was sent from
    /// </summary>
    public string Player { get; } = player;
}
