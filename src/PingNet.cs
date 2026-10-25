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
        private const string Shared = "PingHeim_Shared";
        private static ZRoutedRpc _registeredOn;

        public static void Register()
        {
            var rpc = ZRoutedRpc.instance;
            if (rpc == null || rpc == _registeredOn) return;
            _registeredOn = rpc;
            rpc.Register<Vector3, UserInfo, string>(Pinged, RpcPinged);
            rpc.Register<ZPackage>(Shared, RpcShared);
        }

        /// <summary>Called for each other player the ping found.</summary>
        public static void Notify(ZDO playerZdo, Vector3 source)
        {
            if (!PingHeimPlugin.NotifyPlayers.Value || ZRoutedRpc.instance == null) return;

            long owner = playerZdo.GetOwner();
            if (owner == 0L || owner == ZDOMan.GetSessionID()) return;

            ZRoutedRpc.instance.InvokeRoutedRPC(owner, Pinged, source, UserInfo.GetLocalUser(), Game.instance != null ? Player.m_localPlayer?.GetPlayerName() ?? "" : "");
        }

        /// <summary>Sends what a ping found to every other player, so they hear it and see the same results.</summary>
        public static void Share(Player player, string summary)
        {
            if (!PingHeimPlugin.ShareWithPlayers.Value || ZRoutedRpc.instance == null || player == null) return;

            var pkg = new ZPackage();
            pkg.Write(player.GetPlayerName() ?? "");
            pkg.Write(player.transform.position);
            pkg.Write(summary ?? "");
            UserInfo.GetLocalUser().Serialize(ref pkg);
            pkg.Write(Scanner.LastFound.Count);
            foreach (var found in Scanner.LastFound)
            {
                pkg.Write((int)found.Kind);
                pkg.Write(found.Position);
                pkg.Write(found.Label ?? "");
                pkg.Write(found.Spacing);
                pkg.Write(found.Tier);
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Shared, pkg);
        }

        private static void RpcShared(long sender, ZPackage pkg)
        {
            var player = Player.m_localPlayer;
            if (player == null || sender == ZDOMan.GetSessionID() || !PingHeimPlugin.ReceiveFromPlayers.Value) return;

            string name = pkg.ReadString();
            var origin = pkg.ReadVector3();
            string summary = pkg.ReadString();
            var user = new UserInfo();
            user.Deserialize(ref pkg);
            int count = pkg.ReadInt();

            PingSound.Play();
            // The caster's own ping, as if they'd middle-clicked: PING and their name in the world and on the map.
            if (Chat.instance != null) Chat.instance.OnNewChatMessage(null, sender, origin, Talker.Type.Ping, user, "");
            Marks.ClearTemporary();
            // The one who pinged is on the map for everyone else too.
            Marks.Add(PingKind.Player, origin, name);
            for (int i = 0; i < count; i++)
            {
                var kind = (PingKind)pkg.ReadInt();
                var position = pkg.ReadVector3();
                string label = pkg.ReadString();
                float spacing = pkg.ReadSingle();
                int tier = pkg.ReadInt();
                // Don't mark yourself.
                if (kind == PingKind.Player && (position - player.transform.position).sqrMagnitude < 4f) continue;
                Marks.Add(kind, position, label, spacing, tier);
            }

            player.Message(MessageHud.MessageType.TopLeft, (string.IsNullOrEmpty(name) ? "Someone" : name) + " pinged. " + summary);
        }

        private static void RpcPinged(long sender, Vector3 position, UserInfo user, string name)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            // The same ping you'd get from a middle click: the "PING" text in the world and on the map.
            if (Chat.instance != null) Chat.instance.OnNewChatMessage(null, sender, position, Talker.Type.Ping, user, "");

            // And they show on your map for a few minutes.
            Marks.Add(PingKind.Player, position, name);
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
