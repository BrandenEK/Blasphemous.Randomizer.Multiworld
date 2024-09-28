using Blasphemous.ModdingAPI;
using Framework.Managers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
{
    public class LocationReceiver
    {
        private readonly List<long> locationQueue = new();

        public void OnReceiveLocations(ReadOnlyCollection<long> locations)
        {
            lock (APManager.receiverLock)
            {
                locationQueue.AddRange(locations);
                foreach (long apId in locations)
                    ModLog.Info($"Receiving checked location: {apId}");
            }
        }

        public void Update()
        {
            if (locationQueue.Count == 0)
                return;

            ModLog.Warn("Processing location queue");

            foreach (long apId in locationQueue)
            {
                string internalId;
                try
                {
                    internalId = Main.Multiworld.LocationScouter.MultiworldToInternalId(apId);
                }
                catch
                {
                    ModLog.Error($"Invalid location id: {apId}");
                    continue;
                }

                if (!Core.Events.GetFlag("LOCATION_" + internalId))
                {
                    Core.Events.SetFlag("APLOCATION_" + internalId, true, false);
                }
            }

            ClearLocationQueue();
        }

        public void ClearLocationQueue()
        {
            locationQueue.Clear();
        }
    }
}
