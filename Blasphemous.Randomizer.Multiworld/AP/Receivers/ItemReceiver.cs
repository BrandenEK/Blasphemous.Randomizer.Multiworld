using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Blasphemous.ModdingAPI;
using Blasphemous.Randomizer.Multiworld.Models;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
{
    public class ItemReceiver
    {
        private readonly List<QueuedItem> itemQueue = new();
        private int itemsReceived = 0;

        public void OnReceiveItem(ReceivedItemsHelper helper)
        {
            lock (APManager.receiverLock)
            {
                ItemInfo item = helper.PeekItem();

                string player = Main.Multiworld.APManager.GetPlayerNameFromSlot(item.Player);
                if (string.IsNullOrEmpty(player))
                    player = "Server";

                helper.DequeueItem();

                ModLog.Info($"Receiving item: {item.ItemName}");
                try
                {
                    string itemId = Main.Randomizer.data.items.Values.First(x => x.name == item.ItemName).id;
                    itemQueue.Add(new QueuedItem(itemId, helper.Index, player));
                }
                catch
                {
                    ModLog.Error("Invalid item name");
                }
            }
        }

        public void Update()
        {
            if (itemQueue.Count == 0)
                return;

            ModLog.Warn("Processing item queue");

            foreach (QueuedItem item in itemQueue)
            {
                ModLog.Info($"Item '{item.ItemId}' is at index {item.Index} with {itemsReceived} items currently received");
                if (item.Index > itemsReceived)
                {
                    Main.Randomizer.data.items[item.ItemId].addToInventory();
                    Main.Multiworld.NotificationManager.DisplayNotification(item);
                    itemsReceived++;
                }
            }

            ClearItemQueue();
        }

        public void ClearItemQueue()
        {
            itemQueue.Clear();
        }

        public int SaveItemsReceived() => itemsReceived;
        public void LoadItemsReceived(int items) => itemsReceived = items;
        public void ResetItemsReceived() => itemsReceived = 0;
    }
}
