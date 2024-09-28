using Archipelago.MultiClient.Net.Models;
using Blasphemous.ModdingAPI;
using Framework.Managers;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
{
    public class HintReceiver
    {
        private readonly List<long> hintQueue = new();

        public void OnReceiveHints(Hint[] hints)
        {
            lock (APManager.receiverLock)
            {
                foreach (Hint hint in hints)
                {
                    if (hint.Found || hint.FindingPlayer != Main.Multiworld.APManager.PlayerSlot)
                        continue;

                    ModLog.Info($"Receiving hinted location: {hint.LocationId}");
                    hintQueue.Add(hint.LocationId);
                }
            }
        }

        public void Update()
        {
            if (hintQueue.Count == 0)
                return;

            ModLog.Warn("Processing hint queue");

            foreach (long apId in hintQueue)
            {
                string internalId;
                try
                {
                    internalId = Main.Multiworld.LocationScouter.MultiworldToInternalId(apId);                    
                }
                catch
                {
                    ModLog.Error($"Invalid location id: {apId}");
                    return;
                }

                Core.Events.SetFlag("APHINT_" + internalId, true, false);
            }

            ClearHintQueue();
        }

        public void ClearHintQueue()
        {
            hintQueue.Clear();
        }
    }
}
