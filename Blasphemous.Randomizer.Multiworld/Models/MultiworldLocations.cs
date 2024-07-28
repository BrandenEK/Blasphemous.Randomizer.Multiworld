namespace Blasphemous.Randomizer.Multiworld.Models;

public class MultiworldLocationV1
{
    // Location
    public string id;
    public long ap_id;
    // Item
    public string name;
    public byte type;
    // Player
    public string player_name;
}

public class MultiworldLocationV2
{
    public string GameId { get; set; }
    public long ApId { get; set; }
}
