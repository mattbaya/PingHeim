using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PingHeim
{
    internal enum PingKind { Monster, Trader, Player, Structure, Portal }

    /// <summary>
    /// Everything a ping has found, and the map pins that show it. Each find is kept as a record, and its pin is
    /// created or removed to match the filters, so turning a kind off and on again brings it back without another ping.
    ///
    /// Creatures, traders and players are temporary and expire after a few minutes. Buildings and portals are kept for
    /// good in the character's own save data (not in the map file) and put back on the map whenever it loads.
    /// </summary>
    internal static class Marks
    {
        private const string PlacesKey = "PingHeim.Places";
        private const int MaxPlaces = 1500;

        private class Record
        {
            public PingKind Kind;
            public Vector3 Position;
            public string Label = "";
            public float Expires;           // Time.time it goes away; 0 = permanent
            public Minimap.PinData Pin;
        }

        private static readonly List<Record> Records = new List<Record>();
        private static readonly Dictionary<PingKind, Sprite> Sprites = new Dictionary<PingKind, Sprite>();
        private static Minimap _forMap;
        private static Player _forPlayer;
        private static bool _placesDirty;
        private static float _nextSave;
        private static bool _loggedIcons;

        public static bool IsShown(PingKind kind)
        {
            switch (kind)
            {
                case PingKind.Monster: return PingHeimPlugin.ShowMobs.Value;
                case PingKind.Trader: return PingHeimPlugin.ShowNpcs.Value;
                case PingKind.Player: return PingHeimPlugin.ShowPlayers.Value;
                case PingKind.Structure: return PingHeimPlugin.ShowStructures.Value;
                default: return PingHeimPlugin.ShowPortals.Value;
            }
        }

        public static bool IsPermanent(PingKind kind) => kind == PingKind.Structure || kind == PingKind.Portal;

        /// <summary>Forgets the previous ping's creatures, traders and players (a new ping replaces them).</summary>
        public static void ClearTemporary()
        {
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                if (IsPermanent(Records[i].Kind)) continue;
                RemovePin(Records[i]);
                Records.RemoveAt(i);
            }
        }

        /// <summary>Records something found. Returns true if it's new (permanent finds already known are skipped).</summary>
        public static bool Add(PingKind kind, Vector3 position, string label, float spacing = 0f)
        {
            if (IsPermanent(kind))
            {
                foreach (var existing in Records)
                {
                    if (existing.Kind != kind) continue;
                    var offset = existing.Position - position;
                    offset.y = 0f;
                    if (offset.magnitude < spacing) return false;
                }

                if (Records.Count >= MaxPlaces) return false;
                _placesDirty = true;
            }

            Records.Add(new Record
            {
                Kind = kind,
                Position = position,
                Label = label ?? "",
                Expires = IsPermanent(kind) ? 0f : Time.time + PingHeimPlugin.DotMinutes.Value * 60f,
            });
            return true;
        }

        private static void RemovePin(Record record)
        {
            if (record.Pin == null) return;
            if (Minimap.instance != null) Minimap.instance.RemovePin(record.Pin);
            record.Pin = null;
        }

        private static Minimap.PinData CreatePin(Minimap map, Record record)
        {
            Minimap.PinData pin;
            switch (record.Kind)
            {
                case PingKind.Structure:
                    // The game's own house icon.
                    pin = map.AddPin(record.Position, PingHeimPlugin.StructureIcon.Value, "", false, false);
                    break;
                case PingKind.Portal:
                    pin = map.AddPin(record.Position, PingHeimPlugin.PortalIcon.Value, record.Label, false, false);
                    break;
                default:
                    pin = map.AddPin(record.Position, Minimap.PinType.Icon3, record.Label, false, false);
                    pin.m_icon = SpriteFor(record.Kind);
                    break;
            }

            return pin;
        }

        public static void Tick()
        {
            var map = Minimap.instance;
            if (map != _forMap)
            {
                // A different map (a world was loaded or left): the pins went with it, the records stay.
                foreach (var record in Records) record.Pin = null;
                _forMap = map;
            }

            var player = Player.m_localPlayer;
            if (player != _forPlayer)
            {
                _forPlayer = player;
                foreach (var record in Records) RemovePin(record);
                Records.Clear();
                if (player != null) LoadPlaces(player);
            }

            if (map == null) return;

            if (!_loggedIcons)
            {
                _loggedIcons = true;
                LogIcons(map);
            }

            bool changed = false;
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                var record = Records[i];
                if (record.Expires > 0f && Time.time >= record.Expires)
                {
                    RemovePin(record);
                    Records.RemoveAt(i);
                    changed = true;
                    continue;
                }

                bool want = IsShown(record.Kind);
                if (want && record.Pin == null)
                {
                    record.Pin = CreatePin(map, record);
                    changed = true;
                }
                else if (!want && record.Pin != null)
                {
                    RemovePin(record);
                    changed = true;
                }
            }

            if (changed) map.m_pinUpdateRequired = true;

            if (_placesDirty && Time.time >= _nextSave && player != null)
            {
                _nextSave = Time.time + 2f;
                _placesDirty = false;
                SavePlaces(player);
            }
        }

        // --- Buildings and portals live in the character's save data ---

        private static void SavePlaces(Player player)
        {
            var text = new StringBuilder();
            foreach (var record in Records)
            {
                if (!IsPermanent(record.Kind)) continue;
                if (text.Length > 0) text.Append(';');
                text.Append(record.Kind == PingKind.Portal ? 'P' : 'S').Append('|')
                    .Append(record.Position.x.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Position.y.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Position.z.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Label.Replace(';', ' ').Replace('|', ' '));
            }

            player.m_customData[PlacesKey] = text.ToString();
        }

        private static void LoadPlaces(Player player)
        {
            if (!player.m_customData.TryGetValue(PlacesKey, out var saved) || string.IsNullOrEmpty(saved)) return;

            foreach (var entry in saved.Split(';'))
            {
                var part = entry.Split('|');
                if (part.Length < 4) continue;
                if (!float.TryParse(part[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    || !float.TryParse(part[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                    || !float.TryParse(part[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                Records.Add(new Record
                {
                    Kind = part[0] == "P" ? PingKind.Portal : PingKind.Structure,
                    Position = new Vector3(x, y, z),
                    Label = part.Length > 4 ? part[4] : "",
                });
            }
        }

        /// <summary>Writes the game's own map icon names to the log once, so the House / Portal icon choice can be checked.</summary>
        private static void LogIcons(Minimap map)
        {
            try
            {
                var names = new List<string>();
                foreach (var data in map.m_icons) names.Add($"{data.m_name}={(data.m_icon != null ? data.m_icon.name : "none")}");
                PingHeimPlugin.Log.LogInfo("Map icons: " + string.Join(", ", names));
            }
            catch (System.Exception e)
            {
                PingHeimPlugin.Log.LogWarning($"Couldn't list the map icons: {e.Message}");
            }
        }

        // --- Our own icons: a person and a paw print, flat like the game's pins ---

        private static Sprite SpriteFor(PingKind kind)
        {
            if (Sprites.TryGetValue(kind, out var sprite) && sprite != null) return sprite;

            switch (kind)
            {
                case PingKind.Monster: sprite = MakeSprite(new Color(0.93f, 0.22f, 0.18f), paw: true); break;
                case PingKind.Trader: sprite = MakeSprite(new Color(1f, 1f, 1f), paw: false); break;
                default: sprite = MakeSprite(new Color(0.30f, 0.55f, 1f), paw: false); break;
            }

            Sprites[kind] = sprite;
            return sprite;
        }

        public static Color ColorOf(PingKind kind)
        {
            switch (kind)
            {
                case PingKind.Monster: return new Color(0.95f, 0.35f, 0.32f);
                case PingKind.Trader: return Color.white;
                case PingKind.Player: return new Color(0.45f, 0.68f, 1f);
                default: return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        private static float Ellipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float dx = (u - cx) / rx, dy = (v - cy) / ry;
            return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
        }

        /// <summary>Signed distance to the shape (negative inside), in 0-1 picture units.</summary>
        private static float Shape(float u, float v, bool paw)
        {
            if (paw)
            {
                // One big pad and four toes.
                float d = Ellipse(u, v, 0.50f, 0.34f, 0.26f, 0.20f);
                d = Mathf.Min(d, Ellipse(u, v, 0.18f, 0.60f, 0.09f, 0.12f));
                d = Mathf.Min(d, Ellipse(u, v, 0.38f, 0.76f, 0.09f, 0.125f));
                d = Mathf.Min(d, Ellipse(u, v, 0.62f, 0.76f, 0.09f, 0.125f));
                d = Mathf.Min(d, Ellipse(u, v, 0.82f, 0.60f, 0.09f, 0.12f));
                return d;
            }

            // A head and shoulders.
            float head = Ellipse(u, v, 0.50f, 0.68f, 0.19f, 0.19f);
            float shoulders = Mathf.Max(Ellipse(u, v, 0.50f, 0.12f, 0.36f, 0.36f), v - 0.43f);
            return Mathf.Min(head, shoulders);
        }

        private static Sprite MakeSprite(Color fill, bool paw)
        {
            const int size = 64;
            const float rim = 0.045f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float d = Shape(u, v, paw);
                    float inside = Mathf.Clamp01(0.5f - d * size);
                    float outside = Mathf.Clamp01(0.5f - (d - rim) * size);
                    var color = Color.Lerp(new Color(0f, 0f, 0f, 0.9f), fill, inside);
                    color.a = Mathf.Max(inside, outside * 0.9f);
                    pixels[y * size + x] = color;
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "PingHeimIcon", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, false);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var made = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            made.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return made;
        }
    }
}
