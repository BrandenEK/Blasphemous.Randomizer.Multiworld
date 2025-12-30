using Archipelago.MultiClient.Net;

namespace ArchipelagoUtility;

public class SessionInfo
{
    public ArchipelagoSession Session { get; }

    public bool Connected { get; }

    public int Slot { get; }

    public SessionInfo(ArchipelagoSession session, bool connected, int slot)
    {
        Session = session;
        Connected = connected;
        Slot = slot;
    }
}
