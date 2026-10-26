using System.Linq;
using UnityEngine;

namespace PingHeim
{
    /// <summary>A key that opens the Ping Stone's options, for when clicking with it equipped doesn't (or you'd rather not equip it).</summary>
    internal static class PingKeys
    {
        public static void Tick()
        {
            var player = Player.m_localPlayer;
            if (player == null || PingOptions.PopupOpen || !Input.GetKeyDown(PingHeimPlugin.OpenKey.Value)) return;
            if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible()) return;
            if (Minimap.IsOpen() || (InventoryGui.instance != null && InventoryGui.IsVisible())) return;
            if (!player.GetInventory().GetAllItems().Any(PingItem.IsPingStone)) return;
            PingOptions.OpenPopup();
        }
    }
}
