using Blasphemous.ModdingAPI.Persistence;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld
{
    public class MultiworldSlotData : SlotSaveData
    {

        public int itemsReceived;
        public List<string> scoutedLocations;

        public string server;
        public string name;
        public string password;
    }
}
