using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ArchipelagoUtility;

public static class Test
{
    // MoveLink

    public static void SendMoveLink(SessionInfo info, float x, float y, float timespan)
    {
        if (!info.Connected)
            return;

        info.Session.Socket.SendPacket(new BouncePacket()
        {
            Slots = [info.Slot],
            Games = [GAME_NAME],
            Tags = ["MoveLink"],
            Data = new Dictionary<string, JToken>()
                {
                    { "slot", info.Slot },
                    { "timespan", timespan },
                    { "x", x },
                    { "y", y },
                }
        });
    }

    private const string GAME_NAME = "Blasphemous";
}
