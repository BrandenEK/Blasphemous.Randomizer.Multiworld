using Framework.Managers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
{
    public class LocationReceiver
    {
        private readonly List<string> locationQueue = new();

        public void OnReceiveLocations(ReadOnlyCollection<long> locations)
        {
            lock (APManager.receiverLock)
            {
                foreach (long apId in locations)
                {
                    Main.Multiworld.Log($"Receiving checked location: {apId}");
                    try
                    {
                        string internalId = Main.Multiworld.LocationScouter.MultiworldToInternalId(apId);
                        locationQueue.Add(internalId);
                    }
                    catch
                    {
                        Main.Multiworld.LogError("Invalid location id");
                    }
                }
            }
        }

        public void Update()
        {
            if (locationQueue.Count == 0)
                return;

            Main.Multiworld.LogWarning("Processing location queue");

            foreach (string locationId in locationQueue)
            {
                if (!Core.Events.GetFlag("LOCATION_" + locationId))
                {
                    Core.Events.SetFlag("APLOCATION_" + locationId, true, false);
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
