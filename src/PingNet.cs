using HarmonyLib;
using UnityEngine;

namespace PingHeim
{
    /// <summary>
    /// Tells a detected player that they were pinged. It goes straight to that player (not everyone, unlike the game's
    /// own map ping), and the server just passes it on, so only players' games need the mod for this part.
    /// </summary>
    internal static class PingNet
    {
        private const string Pinged = "PingHeim_Pinged";
        private static ZRoutedRpc _registeredOn;

        public static void Register()
        {
            var rpc = ZRoutedRpc.instance;
            if (rpc == null || rpc == _registeredOn) return;
            _registeredOn = rpc;
            rpc.Register<Vector3, UserInfo, string>(Pinged, RpcPinged);
        }

        /// <summary>Called for each other player the ping found.</summary>
        public static void Notify(ZDO playerZdo, Vector3 source)
        {
            if (!PingHeimPlugin.NotifyPlayers.Value || ZRoutedRpc.instance == null) return;

            long owner = playerZdo.GetOwner();
            if (owner == 0L || owner == ZDOMan.GetSessionID()) return;

            ZRoutedRpc.instance.InvokeRoutedRPC(owner, Pinged, source, UserInfo.GetLocalUser(), Game.instance != null ? Player.m_localPlayer?.GetPlayerName() ?? "" : "");
        }

        private static void RpcPinged(long sender, Vector3 position, UserInfo user, string name)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            // The same ping you'd get from a middle click: the "PING" text in the world and on the map.
            if (Chat.instance != null) Chat.instance.OnNewChatMessage(null, sender, position, Talker.Type.Ping, user, "");

            // And they show on your map for a few minutes.
            Marks.AddDot(position, DotKind.Player, name);
            player.Message(MessageHud.MessageType.TopLeft, (string.IsNullOrEmpty(name) ? "Someone" : name) + " pinged you");
        }
    }

    /// <summary>Hooks the message up as soon as the game has a connection to use.</summary>
    [HarmonyPatch(typeof(Game), "Start")]
    internal static class GameStartPatch
    {
        private static void Postfix() => PingNet.Register();
    }
}
