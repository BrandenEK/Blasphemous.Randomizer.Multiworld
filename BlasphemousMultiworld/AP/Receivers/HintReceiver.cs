using Archipelago.MultiClient.Net.Models;
using Framework.Managers;
using System.Collections.Generic;

namespace BlasphemousMultiworld.AP.Receivers
{
    public class HintReceiver
    {
        private readonly List<string> hintQueue = new();

        public void OnReceiveHints(Hint[] hints)
        {
            lock (APManager.receiverLock)
            {
                foreach (Hint hint in hints)
                {
                    if (hint.Found || hint.FindingPlayer != Main.Multiworld.APManager.PlayerSlot)
                        continue;

                    if (Main.Multiworld.APManager.LocationIdExists(hint.LocationId, out string locationId))
                    {
                        hintQueue.Add(locationId);
                        Main.Multiworld.Log("Queueing hint for location: " + locationId);
                    }
                    else
                    {
                        Main.Multiworld.LogError("Received invalid hint location: " + hint.LocationId);
                    }
                }
            }
        }

        public void Update()
        {
            if (hintQueue.Count == 0)
                return;

            Main.Multiworld.LogWarning("Processing hint queue");

            foreach (string locationId in hintQueue)
            {
                Core.Events.SetFlag("APHINT_" + locationId, true, false);
            }

            ClearHintQueue();
        }

        public void ClearHintQueue()
        {
            hintQueue.Clear();
        }
    }
}
