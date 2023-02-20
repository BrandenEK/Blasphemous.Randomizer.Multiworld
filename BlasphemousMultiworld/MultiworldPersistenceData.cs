using System;
using ModdingAPI;

namespace BlasphemousMultiworld
{
    [Serializable]
    public class MultiworldPersistenceData : ModPersistentData
    {
        public MultiworldPersistenceData() : base("ID_MULTIWORLD") { }

        public int itemsReceived;
    }
}
