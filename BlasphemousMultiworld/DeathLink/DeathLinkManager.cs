using Framework.Managers;

namespace BlasphemousMultiworld.DeathLink
{
    public class DeathLinkManager
    {
        public DeathLinkStatus CurrentStatus { get; set; }

        private bool DeathLinkEnabled
        {
            get => Main.Multiworld.MultiworldSettings.DeathLinkEnabled;
            set => Main.Multiworld.MultiworldSettings.DeathLinkEnabled = value;
        }

        public void Update()
        {
            // If you received a deathlink & are able to die
            if (DeathLinkEnabled && CurrentStatus == DeathLinkStatus.Queued && Main.Multiworld.InGame && !Core.LevelManager.InsideChangeLevel && !Core.Input.HasBlocker("*"))
            {
                CurrentStatus = DeathLinkStatus.Killing;
                Core.Logic.Penitent.KillInstanteneously();
            }
        }

        public void SendDeath()
        {
            if (!Main.Multiworld.MultiworldSettings.DeathLinkEnabled) return;

            Main.Multiworld.Log("Sending death link!");
            Main.Multiworld.connection.SendDeath();
        }

        public void ReceiveDeath(string player)
        {
            if (!Main.Multiworld.MultiworldSettings.DeathLinkEnabled) return;

            if (!Core.Events.GetFlag("CHERUB_RESPAWN"))
            {
                Main.Multiworld.Log("Received death link!");
                CurrentStatus = DeathLinkStatus.Queued;
                //itemReceiver.receiveItem(new QueuedItem("Death", 0, player));
            }
        }

        public bool ToggleDeathLink()
        {
            bool newDeathLinkEnabled = !DeathLinkEnabled;
            DeathLinkEnabled = newDeathLinkEnabled;
            Main.Multiworld.connection.EnableDeathLink(newDeathLinkEnabled);
            Main.Multiworld.Log("Setting deathlink status to " + newDeathLinkEnabled.ToString());
            return newDeathLinkEnabled;
        }
    }
}
