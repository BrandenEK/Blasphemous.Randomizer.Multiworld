using Archipelago.MultiClient.Net.Helpers;
using Blasphemous.Randomizer.Multiworld.Models;
using System.Collections.Generic;

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
                string player = Main.Multiworld.APManager.GetPlayerNameFromSlot(helper.PeekItem().Player);
                if (player == null || player == string.Empty)
                    player = "Server";
                string itemName = helper.PeekItemName();
                int itemIdx = helper.Index;
                helper.DequeueItem();

                if (Main.Multiworld.APManager.ItemNameExists(itemName, out string itemId))
                {
                    itemQueue.Add(new QueuedItem(itemId, itemIdx, player));
                    Main.Multiworld.Log("Queueing item: " + itemId);
                }
                else
                {
                    Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
                }
            }
        }

        public void Update()
        {
            if (itemQueue.Count == 0)
                return;

            Main.Multiworld.LogWarning("Processing item queue");

            foreach (QueuedItem item in itemQueue)
            {
                Main.Multiworld.Log($"Item '{item.ItemId}' is at index {item.Index} with {itemsReceived} items currently received");
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
