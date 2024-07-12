using Archipelago.MultiClient.Net.Models;
using Framework.Managers;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
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

                    Main.Multiworld.Log($"Receiving hinted location: {hint.LocationId}");
                    try
                    {
                        string internalId = Main.Multiworld.LocationScouter.MultiworldToInternalId(hint.LocationId);
                        hintQueue.Add(internalId);
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
