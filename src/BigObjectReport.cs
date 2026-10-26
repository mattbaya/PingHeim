using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PingHeim
{
    /// <summary>
    /// A one-off check, for whoever hosts the world: about 40 seconds after the host spawns, it writes the largest pieces of
    /// stored data in the world to the log (what they belong to, how big, where). Valheim can't send a single object of more than
    /// about half a megabyte to a joining player, and one that big makes the connection fail.
    /// </summary>
    internal static class BigObjectReport
    {
        private static bool _done;
        private static float _at;

        public static void Tick()
        {
            if (_done || !PingHeimPlugin.LogBigObjects.Value) return;
            if (ZNet.instance == null || !ZNet.instance.IsServer() || Player.m_localPlayer == null)
            {
                _at = 0f;
                return;
            }

            if (_at == 0f) _at = Time.time + 40f;
            if (Time.time < _at) return;

            _done = true;
            try { Scan(); }
            catch (Exception e) { PingHeimPlugin.Log.LogWarning("Big-object check failed: " + e.Message); }
        }

        private static void Scan()
        {
            var field = AccessTools.Field(typeof(ZDOExtraData), "s_byteArrays");
            var all = field != null ? field.GetValue(null) as IDictionary : null;
            if (all == null)
            {
                PingHeimPlugin.Log.LogWarning("Big-object check: couldn't find the world's stored data.");
                return;
            }

            var rows = new List<(long size, ZDOID id, int key)>();
            long total = 0;
            int count = 0;
            foreach (DictionaryEntry entry in all)
            {
                count++;
                if (!(entry.Key is ZDOID id) || !(entry.Value is IEnumerable inner)) continue;
                foreach (var item in inner)
                {
                    var type = item.GetType();
                    var keyProperty = type.GetProperty("Key");
                    var valueProperty = type.GetProperty("Value");
                    if (keyProperty == null || valueProperty == null) continue;
                    var bytes = valueProperty.GetValue(item, null) as byte[];
                    if (bytes == null) continue;
                    total += bytes.Length;
                    int key = keyProperty.GetValue(item, null) is int k ? k : 0;
                    rows.Add((bytes.Length, id, key));
                }
            }

            PingHeimPlugin.Log.LogInfo($"Big-object check: {count} objects hold stored data, {total / 1024} KB in all.");
            foreach (var row in rows.OrderByDescending(r => r.size).Take(12))
            {
                var zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(row.id) : null;
                string name = "unknown";
                string where = "";
                if (zdo != null)
                {
                    var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
                    name = prefab != null ? prefab.name : "prefab " + zdo.GetPrefab() + " (not loaded)";
                    var p = zdo.GetPosition();
                    where = $" at ({p.x:0}, {p.y:0}, {p.z:0})";
                }

                PingHeimPlugin.Log.LogInfo($"Big object: {name}{where}, {row.size / 1024.0:0.0} KB (data key {row.key}).");
            }
        }
    }
}
