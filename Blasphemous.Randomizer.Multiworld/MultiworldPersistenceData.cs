using Blasphemous.ModdingAPI.Persistence;
using System;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld
{
    [Serializable]
    public class MultiworldPersistenceData : SaveData
    {
        public MultiworldPersistenceData() : base("ID_MULTIWORLD") { }

        public int itemsReceived;
        public List<string> scoutedLocations;
    }
}
