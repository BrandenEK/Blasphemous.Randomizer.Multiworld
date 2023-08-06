using Framework.Managers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlasphemousMultiworld.AP.Receivers
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
                    if (Main.Multiworld.APManager.LocationIdExists(apId, out string locationId))
                    {
                        locationQueue.Add(locationId);
                        Main.Multiworld.Log("Queueing check for location: " + locationId);
                    }
                    else
                    {
                        Main.Multiworld.LogError("Received invalid checked location: " + apId);
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
