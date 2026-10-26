using System.Collections.Generic;
using UnityEngine;

namespace PingHeim
{
    internal struct ScanResult
    {
        public int Monsters, Traders, Players, Structures, Portals, Spawners;
    }

    /// <summary>
    /// Finds what's around the player, using the game's list of every object it currently knows about (the areas
    /// around you), and puts it on the map. Nothing is looked up by hand: each object's prefab says what it is.
    /// </summary>
    internal static class Scanner
    {
        private enum Kind { None, Monster, Trader, Player, Structure, Portal, Spawner }

        private class Info
        {
            public Kind Kind;
            public bool Hostile;
            public bool Boss;
            public float Health;
            public string Name = "";
        }

        private static readonly Dictionary<int, Info> Cache = new Dictionary<int, Info>();

        private static Info InfoFor(int prefabHash)
        {
            if (Cache.TryGetValue(prefabHash, out var cached)) return cached;

            var info = new Info();
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
            if (prefab != null)
            {
                if (prefab.GetComponent<Player>() != null)
                {
                    info.Kind = Kind.Player;
                }
                else if (prefab.GetComponent<Trader>() != null)
                {
                    info.Kind = Kind.Trader;
                    var character = prefab.GetComponent<Character>();
                    if (character != null) info.Name = Localization.instance.Localize(character.m_name);
                }
                else if (prefab.GetComponent<Character>() != null)
                {
                    var character = prefab.GetComponent<Character>();
                    info.Kind = Kind.Monster;
                    info.Name = Localization.instance.Localize(character.m_name);
                    info.Boss = character.m_boss;
                    info.Health = character.m_health;
                    var faction = character.m_faction;
                    info.Hostile = faction != Character.Faction.AnimalsVeg && faction != Character.Faction.Players
                        && faction != Character.Faction.Dverger && faction != Character.Faction.PlayerSpawned
                        && faction != Character.Faction.TrainingDummy;
                }
                // Only spawners you can destroy (nests, bone piles and the like), not the invisible fixed spawn points.
                else if (prefab.GetComponent<SpawnArea>() != null && (prefab.GetComponent<Destructible>() != null || prefab.GetComponent<WearNTear>() != null))
                {
                    info.Kind = Kind.Spawner;
                    info.Name = SpawnerName(prefab);
                }
                else if (prefab.GetComponent<TeleportWorld>() != null)
                {
                    info.Kind = Kind.Portal;
                }
                else if (prefab.GetComponent<Piece>() != null && prefab.GetComponent<Plant>() == null)
                {
                    info.Kind = Kind.Structure;
                }
            }

            Cache[prefabHash] = info;
            return info;
        }

        /// <summary>One thing the last scan put on the map, kept so it can be shared with the other players.</summary>
        internal struct Found
        {
            public PingKind Kind;
            public Vector3 Position;
            public string Label;
            public float Spacing;
            public int Tier;
        }

        /// <summary>What the last scan found.</summary>
        public static readonly List<Found> LastFound = new List<Found>();

        private static bool Add(PingKind kind, Vector3 position, string label, float spacing = 0f, int tier = 0)
        {
            LastFound.Add(new Found { Kind = kind, Position = position, Label = label ?? "", Spacing = spacing, Tier = tier });
            return Marks.Add(kind, position, label, spacing, tier);
        }

        /// <summary>How big and strong a creature is: 0 tiny, 1 small, 2 medium, 3 large, 4 boss (by its health, which includes stars).</summary>
        private static int TierOf(Info info, ZDO zdo)
        {
            if (info.Boss) return 4;
            float health = zdo.GetFloat(ZDOVars.s_maxHealth, info.Health);
            return health < 35f ? 0 : health < 120f ? 1 : health < 400f ? 2 : 3;
        }

        /// <summary>What a spawner makes, for its label (the first creature it lists, or its own name).</summary>
        private static string SpawnerName(GameObject prefab)
        {
            GameObject made = null;
            var area = prefab.GetComponent<SpawnArea>();
            if (area != null && area.m_prefabs != null && area.m_prefabs.Count > 0) made = area.m_prefabs[0].m_prefab;
            var character = made != null ? made.GetComponent<Character>() : null;
            return character != null ? Localization.instance.Localize(character.m_name) + " spawner" : "Spawner";
        }

        public static ScanResult Run(Player player)
        {
            LastFound.Clear();
            var result = new ScanResult();
            if (ZDOMan.instance == null || player == null) return result;

            Vector3 origin = player.transform.position;
            float radius = PingHeimPlugin.Radius.Value;
            float radiusSquared = radius * radius;
            var self = player.m_nview != null && player.m_nview.GetZDO() != null ? player.m_nview.GetZDO().m_uid : ZDOID.None;

            // A new ping replaces the last one's creatures, traders and players.
            Marks.ClearTemporary();

            var cells = new Dictionary<long, Vector3>();   // structure cluster -> sum of positions
            var counts = new Dictionary<long, int>();
            float cluster = Mathf.Max(1f, PingHeimPlugin.StructureCluster.Value);

            foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (zdo == null) continue;
                var position = zdo.GetPosition();
                float dx = position.x - origin.x, dz = position.z - origin.z;
                if (dx * dx + dz * dz > radiusSquared) continue;

                var info = InfoFor(zdo.GetPrefab());
                switch (info.Kind)
                {
                    // Everything found is recorded; the on-map filters decide what's shown.
                    case Kind.Monster:
                        if (zdo.GetBool(ZDOVars.s_dead)) break;
                        if (!PingHeimPlugin.IncludeTamed.Value && zdo.GetBool(ZDOVars.s_tamed)) break;
                        if (PingHeimPlugin.OnlyHostile.Value && !info.Hostile) break;
                        Add(PingKind.Monster, position, PingHeimPlugin.MobNames.Value ? info.Name : "", 0f, TierOf(info, zdo));
                        result.Monsters++;
                        break;

                    case Kind.Trader:
                        Add(PingKind.Trader, position, info.Name);
                        result.Traders++;
                        break;

                    case Kind.Player:
                        if (zdo.m_uid == self) break;
                        Add(PingKind.Player, position, zdo.GetString(ZDOVars.s_playerName));
                        result.Players++;
                        PingNet.Notify(zdo, origin);
                        break;

                    case Kind.Spawner:
                        if (Add(PingKind.Spawner, position, info.Name, 3f)) result.Spawners++;
                        break;

                    case Kind.Portal:
                        // Only portals players built.
                        if (zdo.GetLong(ZDOVars.s_creator) == 0L) break;
                        if (Add(PingKind.Portal, position, zdo.GetString(ZDOVars.s_tag), 3f)) result.Portals++;
                        break;

                    case Kind.Structure:
                        // Only things players built (the world's own ruins and the like have no creator).
                        if (zdo.GetLong(ZDOVars.s_creator) == 0L) break;
                        long key = ((long)Mathf.FloorToInt(position.x / cluster) << 32) ^ (uint)Mathf.FloorToInt(position.z / cluster);
                        cells[key] = cells.TryGetValue(key, out var sum) ? sum + position : position;
                        counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
                        break;
                }
            }

            foreach (var pair in cells)
            {
                var centre = pair.Value / counts[pair.Key];
                if (Add(PingKind.Structure, centre, "", cluster * 0.6f)) result.Structures++;
            }

            return result;
        }
    }
}
