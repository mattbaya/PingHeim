using System.Collections.Generic;
using UnityEngine;

namespace PingHeim
{
    internal struct ScanResult
    {
        public int Monsters, Traders, Players, Structures;
    }

    /// <summary>
    /// Finds what's around the player, using the game's list of every object it currently knows about (the areas
    /// around you), and puts it on the map. Nothing is looked up by hand: each object's prefab says what it is.
    /// </summary>
    internal static class Scanner
    {
        private enum Kind { None, Monster, Trader, Player, Structure }

        private class Info
        {
            public Kind Kind;
            public bool Hostile;
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
                    var faction = character.m_faction;
                    info.Hostile = faction != Character.Faction.AnimalsVeg && faction != Character.Faction.Players
                        && faction != Character.Faction.Dverger && faction != Character.Faction.PlayerSpawned
                        && faction != Character.Faction.TrainingDummy;
                }
                else if (prefab.GetComponent<Piece>() != null && prefab.GetComponent<Plant>() == null)
                {
                    info.Kind = Kind.Structure;
                }
            }

            Cache[prefabHash] = info;
            return info;
        }

        public static ScanResult Run(Player player)
        {
            var result = new ScanResult();
            if (ZDOMan.instance == null || player == null) return result;

            Vector3 origin = player.transform.position;
            float radius = PingHeimPlugin.Radius.Value;
            float radiusSquared = radius * radius;
            var self = player.m_nview != null && player.m_nview.GetZDO() != null ? player.m_nview.GetZDO().m_uid : ZDOID.None;

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
                    case Kind.Monster:
                        if (!PingHeimPlugin.ShowMobs.Value) break;
                        if (zdo.GetBool(ZDOVars.s_dead)) break;
                        if (!PingHeimPlugin.IncludeTamed.Value && zdo.GetBool(ZDOVars.s_tamed)) break;
                        if (PingHeimPlugin.OnlyHostile.Value && !info.Hostile) break;
                        Marks.AddDot(position, DotKind.Monster, PingHeimPlugin.MobNames.Value ? info.Name : "");
                        result.Monsters++;
                        break;

                    case Kind.Trader:
                        if (!PingHeimPlugin.ShowNpcs.Value) break;
                        Marks.AddDot(position, DotKind.Trader, info.Name);
                        result.Traders++;
                        break;

                    case Kind.Player:
                        if (!PingHeimPlugin.ShowPlayers.Value || zdo.m_uid == self) break;
                        Marks.AddDot(position, DotKind.Player, zdo.GetString(ZDOVars.s_playerName));
                        result.Players++;
                        PingNet.Notify(zdo, origin);
                        break;

                    case Kind.Structure:
                        // Only things players built (the world's own ruins and the like have no creator).
                        if (!PingHeimPlugin.ShowStructures.Value || zdo.GetLong(ZDOVars.s_creator) == 0L) break;
                        long key = ((long)Mathf.FloorToInt(position.x / cluster) << 32) ^ (uint)Mathf.FloorToInt(position.z / cluster);
                        cells[key] = cells.TryGetValue(key, out var sum) ? sum + position : position;
                        counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
                        break;
                }
            }

            foreach (var pair in cells)
            {
                var centre = pair.Value / counts[pair.Key];
                if (Marks.AddStructure(centre, cluster)) result.Structures++;
            }

            return result;
        }
    }
}
