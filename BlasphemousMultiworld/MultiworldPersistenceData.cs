using System;
using Framework.Managers;

namespace BlasphemousMultiworld
{
    [Serializable]
    public class MultiworldPersistenceData : PersistentManager.PersistentData
    {
        public MultiworldPersistenceData() : base("ID_MULTIWORLD") { }

        public int itemsReceived;
    }
}
