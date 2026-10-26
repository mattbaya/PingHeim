using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using UnityEngine;

namespace PingHeim
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // The Ping Stone is a new item: anyone who sees or holds one (and the server) needs the mod.
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class PingHeimPlugin : BaseUnityPlugin
    {
        public const string Guid = "mbaya.PingHeim";
        public const string Name = "PingHeim";
        internal static ConfigEntry<bool> ShareWithPlayers;
        internal static ConfigEntry<bool> SizeByStrength;
        internal static ConfigEntry<bool> ShowSpawners;
        internal static ConfigEntry<bool> LogBigObjects;
        internal static ConfigEntry<KeyCode> OpenKey;
        internal static ConfigEntry<float> AggroRadius;
        internal static ConfigEntry<bool> AggroBosses;
        internal static ConfigEntry<float> AggroCooldownSeconds;
        internal static ConfigEntry<bool> ReceiveFromPlayers;
        internal static ConfigEntry<Vector2> PopupPos;
        internal static ConfigEntry<Vector2> MapBoxPos;
        public const string Version = "0.6.7";

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> CooldownMinutes;
        internal static ConfigEntry<float> DotMinutes;
        internal static ConfigEntry<bool> ShowMobs;
        internal static ConfigEntry<bool> OnlyHostile;
        internal static ConfigEntry<bool> IncludeTamed;
        internal static ConfigEntry<bool> ShowNpcs;
        internal static ConfigEntry<bool> ShowPlayers;
        internal static ConfigEntry<bool> ShowStructures;
        internal static ConfigEntry<bool> ShowPortals;
        internal static ConfigEntry<Minimap.PinType> StructureIcon;
        internal static ConfigEntry<Minimap.PinType> PortalIcon;
        internal static ConfigEntry<float> StructureCluster;
        internal static ConfigEntry<bool> MobNames;
        internal static ConfigEntry<bool> NotifyPlayers;
        internal static ConfigEntry<float> Volume;
        internal static PingHeimPlugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            Radius = Config.Bind("Ping", "Radius", 120f,
                new ConfigDescription("How far (in meters) a ping reaches around you. The game only knows about things in the areas around you, " +
                    "so what you can detect is capped at roughly 64-128 m, depending on where you stand in your area.", new AcceptableValueRange<float>(20f, 300f)));
            CooldownMinutes = Config.Bind("Ping", "CooldownMinutes", 10f,
                new ConfigDescription("How long the Ping Stone takes to recharge after use. Takes effect on next game start.", new AcceptableValueRange<float>(0f, 120f)));
            DotMinutes = Config.Bind("Ping", "DotMinutes", 3f,
                new ConfigDescription("How long everything a ping finds (creatures, traders, players, buildings, portals) stays on the map, unless you click it on the map to keep it.", new AcceptableValueRange<float>(0.5f, 60f)));
            OpenKey = Config.Bind("Ping", "OpenKey", KeyCode.P, "While you carry a Ping Stone, this key opens its options (ping, aggro ping, what it shows). The stone can also be clicked when equipped.");
            LogBigObjects = Config.Bind("Diagnostics", "LogBigObjects", true, "When you host a world, write the largest stored objects in it to the log once, about 40 seconds in (to help find what makes players' connections fail).");
            AggroRadius = Config.Bind("Aggro", "Radius", 60f, new ConfigDescription("How far the aggro ping reaches, in metres.", new AcceptableValueRange<float>(10f, 150f)));
            AggroBosses = Config.Bind("Aggro", "IncludeBosses", false, "Also call bosses with the aggro ping.");
            AggroCooldownSeconds = Config.Bind("Aggro", "CooldownSeconds", 60f, new ConfigDescription("How long before the aggro ping can be used again.", new AcceptableValueRange<float>(0f, 600f)));
            ShowSpawners = Config.Bind("Detect", "Spawners", true, "Show creature spawners (nests, bone piles and the like) on the map, as a green skull. Also switchable from the box on the map.");
            SizeByStrength = Config.Bind("Detect", "SizeByStrength", true, "Show creatures bigger and warmer in colour the bigger and stronger they are (tiny yellow to large crimson, bosses purple).");
            ShowMobs = Config.Bind("Detect", "Monsters", true, "Show creatures (monsters and animals) on the map, as a red paw print. Also switchable from the box on the map.");
            OnlyHostile = Config.Bind("Detect", "OnlyHostileMonsters", false, "Show only hostile creatures, not deer, boar and other peaceful animals.");
            IncludeTamed = Config.Bind("Detect", "IncludeTamedCreatures", false, "Also show creatures that have been tamed.");
            ShowNpcs = Config.Bind("Detect", "Traders", true, "Show vendors (Haldor, Hildir, the Bog Witch) on the map, as a white person. Also switchable from the box on the map.");
            ShowPlayers = Config.Bind("Detect", "Players", true, "Show other players on the map, as a blue person. Also switchable from the box on the map.");
            ShowStructures = Config.Bind("Detect", "Structures", true, "Mark player-built buildings on the map with the game's house icon. They are remembered for good. Also switchable from the box on the map.");
            ShowPortals = Config.Bind("Detect", "Portals", true, "Mark player-built portals on the map with the game's portal icon and their name. They are remembered for good. Also switchable from the box on the map.");
            StructureIcon = Config.Bind("Icons", "BuildingIcon", Minimap.PinType.Icon1,
                "Which of the game's own map icons marks a building. Icon1 is the house. (The game's icon names are written to the log the first time you open the map.)");
            PortalIcon = Config.Bind("Icons", "PortalIcon", Minimap.PinType.Icon4,
                "Which of the game's own map icons marks a portal. Icon4 is the portal.");
            StructureCluster = Config.Bind("Detect", "StructureClusterMeters", 16f,
                new ConfigDescription("Buildings are grouped into one marker per this many meters, so a base isn't covered in squares.", new AcceptableValueRange<float>(4f, 64f)));
            MobNames = Config.Bind("Detect", "LabelMonsters", false, "Put the creature's name beside each red dot.");
            NotifyPlayers = Config.Bind("Detect", "NotifyPlayers", true,
                "Players your ping detects see a ping where you are, and you show on their map for a few minutes.");
            ShareWithPlayers = Config.Bind("Share", "ShareWithPlayers", true,
                "When you use a Ping Stone, every other player (who has this mod) hears it and sees what it found on their own map.");
            ReceiveFromPlayers = Config.Bind("Share", "ReceiveFromPlayers", true,
                "When another player uses a Ping Stone, hear it and show what it found on your map.");
            PopupPos = Config.Bind("Window", "PopupPosition", new Vector2(-1f, -1f),
                "Where the Ping Stone options window sits (top-left, pixels). Drag it to move it; -1 = centred.");
            MapBoxPos = Config.Bind("Window", "MapBoxPosition", new Vector2(-1f, -1f),
                "Where the filter box on the map sits (top-left, pixels). Drag it to move it; -1 = the default spot on the left.");
            Volume = Config.Bind("Sound", "PingVolume", 0.8f, new ConfigDescription("Volume of the ping sound.", new AcceptableValueRange<float>(0f, 1f)));

            PrefabManager.OnVanillaPrefabsAvailable += PingItem.Create;

            StartCoroutine(PingSound.Load(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "sounds", "ping.wav")));

            _harmony = new Harmony(Guid);
            SafePatch.All(_harmony, typeof(PingHeimPlugin).Assembly, Log);
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
            Marks.Tick();
            PingKeys.Tick();
        }

        private void OnGUI() => PingOptions.Draw();

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
