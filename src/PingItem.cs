using System.Reflection;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PingHeim
{
    /// <summary>The Ping Stone: what it's made of, what using it does, and its recharge timer.</summary>
    internal static class PingItem
    {
        public const string PrefabName = "PingHeim_PingStone";
        private const string BasePrefab = "SurtlingCore";
        private const string CooldownEffect = "PingHeim_Recharging";
        private static readonly int CooldownHash = CooldownEffect.GetStableHashCode();

        public static bool IsPingStone(ItemDrop.ItemData item) =>
            item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == PrefabName;

        public static void Create()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Create;

            var icon = LoadIcon();

            var item = new CustomItem(PrefabName, BasePrefab, new ItemConfig
            {
                Name = "Ping Stone",
                Description = "Equip it (right-click) and click to choose: send a ping across the area around you, or an aggro ping that calls every hostile creature nearby to you. A ping sends a pulse across the area around you. Monsters show as red dots, traders as white, other players as blue, " +
                    "all for a few minutes, and player-built structures are marked in grey for good. Players you reveal see where the ping came from. " +
                    "It then needs time to recharge. The options also say what it shows on your map.",
                CraftingStation = "forge",
                Requirements = new[]
                {
                    new RequirementConfig("Bronze", 3),
                    new RequirementConfig("FineWood", 5),
                    new RequirementConfig("SurtlingCore", 1),
                },
                Icons = icon != null ? new[] { icon } : null,
            });

            var drop = item.ItemDrop;
            if (drop == null)
            {
                PingHeimPlugin.Log.LogError("Could not create the Ping Stone.");
                return;
            }

            var shared = drop.m_itemData.m_shared;
            // Carried in the hand like a tool: right-click it to equip it, then click to open its options.
            shared.m_itemType = ItemDrop.ItemData.ItemType.Tool;
            shared.m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;
            shared.m_maxStackSize = 1;
            shared.m_maxQuality = 1;
            shared.m_weight = 0.5f;
            shared.m_teleportable = true;
            shared.m_value = 0;
            shared.m_equipStatusEffect = null;
            shared.m_attackStatusEffect = null;
            ItemManager.Instance.AddItem(item);

            AddRechargeEffect(icon);
        }

        /// <summary>The recharge shows on the status bar with a countdown.</summary>
        private static void AddRechargeEffect(Sprite icon)
        {
            var effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = CooldownEffect;
            effect.m_name = "Ping Stone recharging";
            effect.m_tooltip = "The Ping Stone is recharging. It can be used again when this runs out.";
            effect.m_icon = icon;
            effect.m_ttl = PingHeimPlugin.CooldownMinutes.Value * 60f;
            effect.m_cooldownIcon = true;
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
        }

        private static Sprite LoadIcon()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PingHeim.ping_icon.png"))
                {
                    if (stream == null) return null;
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.LoadImage(bytes);
                    texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    return sprite;
                }
            }
            catch (System.Exception e)
            {
                PingHeimPlugin.Log.LogWarning($"Couldn't load the item icon: {e.Message}");
                return null;
            }
        }

        /// <summary>Called when the player uses the stone.</summary>
        public static void Use(Player player)
        {
            var effects = player.GetSEMan();
            if (effects.HaveStatusEffect(CooldownHash))
            {
                var recharging = effects.GetStatusEffect(CooldownHash);
                float left = recharging != null ? recharging.GetRemaningTime() : 0f;
                player.Message(MessageHud.MessageType.Center, $"The Ping Stone is recharging ({Mathf.CeilToInt(left / 60f)} min left)");
                return;
            }

            PingSound.Play();
            var result = Scanner.Run(player);

            if (PingHeimPlugin.CooldownMinutes.Value > 0f) effects.AddStatusEffect(CooldownHash, resetTime: true);

            string summary = Summary(result);
            player.Message(MessageHud.MessageType.Center, summary);
            PingNet.Share(player, summary);
        }

        private static string Summary(ScanResult r)
        {
            string text = "Ping! ";
            text += $"{r.Monsters} creature{(r.Monsters == 1 ? "" : "s")}";
            if (r.Traders > 0) text += $", {r.Traders} trader{(r.Traders == 1 ? "" : "s")}";
            if (r.Players > 0) text += $", {r.Players} player{(r.Players == 1 ? "" : "s")}";
            if (r.Structures > 0) text += $", {r.Structures} new building marker{(r.Structures == 1 ? "" : "s")}";
            if (r.Spawners > 0) text += $", {r.Spawners} spawner{(r.Spawners == 1 ? "" : "s")}";
            if (r.Portals > 0) text += $", {r.Portals} new portal{(r.Portals == 1 ? "" : "s")}";
            return text;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    internal static class UseItemPatch
    {
        private static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item)
        {
            if (!PingItem.IsPingStone(item)) return true;
            var player = __instance as Player;
            if (player == null || player != Player.m_localPlayer) return true;
            if (inventory != null && inventory != player.GetInventory()) return true;

            // Holding Shift opens the options box straight away. Otherwise the game equips it, and a click then opens the options.
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                PingOptions.OpenPopup();
                return false;
            }

            return true;
        }
    }
}
