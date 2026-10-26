using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PingHeim
{
    internal enum PingKind { Monster, Trader, Player, Structure, Portal, Spawner }

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
        // Saved lists from before marks could fade held everything for good; from version 2 on, only marks you click to keep.
        private const string PlacesVersionKey = "PingHeim.PlacesVersion";
        private const int MaxPlaces = 1500;

        private class Record
        {
            public PingKind Kind;
            public Vector3 Position;
            public string Label = "";
            public float Expires;           // Time.time it goes away; 0 = kept
            public bool Pinned;             // kept for good (the player clicked it on the map)
            public int Tier;                // creatures: how big and strong (0 = tiny ... 4 = boss)
            public Minimap.PinData Pin;
        }

        private static readonly List<Record> Records = new List<Record>();
        private static readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();
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
                case PingKind.Spawner: return PingHeimPlugin.ShowSpawners.Value;
                default: return PingHeimPlugin.ShowPortals.Value;
            }
        }

        /// <summary>Buildings and portals are found again and again by repeated pings, so a find near a known one just renews it.</summary>
        private static bool IsPlace(PingKind kind) => kind == PingKind.Structure || kind == PingKind.Portal || kind == PingKind.Spawner;

        private static float NewExpiry() => Time.time + PingHeimPlugin.DotMinutes.Value * 60f;

        /// <summary>Forgets the previous ping's creatures, traders and players (a new ping replaces them), except ones you kept.</summary>
        public static void ClearTemporary()
        {
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                if (IsPlace(Records[i].Kind) || Records[i].Pinned) continue;
                RemovePin(Records[i]);
                Records.RemoveAt(i);
            }
        }

        /// <summary>Records something found. Returns true if it's new (a building or portal already known is just renewed).</summary>
        public static bool Add(PingKind kind, Vector3 position, string label, float spacing = 0f, int tier = 0)
        {
            if (IsPlace(kind))
            {
                foreach (var existing in Records)
                {
                    if (existing.Kind != kind) continue;
                    var offset = existing.Position - position;
                    offset.y = 0f;
                    if (offset.magnitude >= spacing) continue;
                    if (!existing.Pinned) existing.Expires = NewExpiry();
                    return false;
                }

                if (Records.Count >= MaxPlaces) return false;
            }

            Records.Add(new Record { Kind = kind, Position = position, Label = label ?? "", Tier = tier, Expires = NewExpiry() });
            return true;
        }

        /// <summary>
        /// A click on one of our pins on the map keeps it for good (or lets it fade again). Returns true if the click was ours.
        /// </summary>
        public static bool ToggleAtClick(Minimap map)
        {
            if (map == null || !Minimap.IsOpen()) return false;
            var pin = map.GetClosestPin(map.ScreenToWorldPoint(ZInput.pointerPosition), map.PinInteractRadius);
            if (pin == null) return false;

            Record found = null;
            foreach (var record in Records)
                if (record.Pin == pin) found = record;
            if (found == null) return false;

            found.Pinned = !found.Pinned;
            found.Expires = found.Pinned ? 0f : NewExpiry();
            RemovePin(found);   // made again next tick, with or without the "kept" mark
            _placesDirty = true;
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft,
                found.Pinned ? "Kept on your map. Click it again to let it fade." : $"It will fade in {PingHeimPlugin.DotMinutes.Value:0.#} minutes.");
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
            string kept = record.Pinned ? " (kept)" : "";
            switch (record.Kind)
            {
                case PingKind.Structure:
                    // The game's own house icon.
                    pin = map.AddPin(record.Position, PingHeimPlugin.StructureIcon.Value, kept.Trim(), false, false);
                    break;
                case PingKind.Portal:
                    pin = map.AddPin(record.Position, PingHeimPlugin.PortalIcon.Value, record.Label + kept, false, false);
                    break;
                default:
                    // Creatures, traders, players and spawners: our own flat icons.
                    pin = map.AddPin(record.Position, Minimap.PinType.Icon3, record.Label + kept, false, false);
                    pin.m_icon = SpriteFor(record.Kind, record.Tier);
                    // The biggest creatures get a larger pin.
                    pin.m_doubleSize = record.Kind == PingKind.Monster && record.Tier >= 3 && PingHeimPlugin.SizeByStrength.Value;
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
                if (!record.Pinned && record.Expires > 0f && Time.time >= record.Expires)
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

        private static char Code(PingKind kind)
        {
            switch (kind)
            {
                case PingKind.Portal: return 'P';
                case PingKind.Spawner: return 'N';
                case PingKind.Monster: return 'M';
                case PingKind.Trader: return 'T';
                case PingKind.Player: return 'L';
                default: return 'S';
            }
        }

        private static PingKind KindOf(string code)
        {
            switch (code)
            {
                case "P": return PingKind.Portal;
                case "N": return PingKind.Spawner;
                case "M": return PingKind.Monster;
                case "T": return PingKind.Trader;
                case "L": return PingKind.Player;
                default: return PingKind.Structure;
            }
        }

        private static void SavePlaces(Player player)
        {
            var text = new StringBuilder();
            foreach (var record in Records)
            {
                if (!record.Pinned) continue;
                if (text.Length > 0) text.Append(';');
                text.Append(Code(record.Kind)).Append('|')
                    .Append(record.Position.x.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Position.y.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Position.z.ToString("0.#", CultureInfo.InvariantCulture)).Append('|')
                    .Append(record.Label.Replace(';', ' ').Replace('|', ' ')).Append('|').Append(record.Tier);
            }

            player.m_customData[PlacesKey] = text.ToString();
            player.m_customData[PlacesVersionKey] = "2";
        }

        private static void LoadPlaces(Player player)
        {
            if (!player.m_customData.TryGetValue(PlacesKey, out var saved) || string.IsNullOrEmpty(saved)) return;

            // An old list (before version 2) was never chosen by the player: its marks come back as ordinary ones that fade, and the
            // saved list is rewritten with only the ones you keep from now on.
            bool legacy = !player.m_customData.TryGetValue(PlacesVersionKey, out var version) || version != "2";
            if (legacy)
            {
                player.m_customData[PlacesVersionKey] = "2";
                _placesDirty = true;
            }

            foreach (var entry in saved.Split(';'))
            {
                var part = entry.Split('|');
                if (part.Length < 4) continue;
                if (!float.TryParse(part[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    || !float.TryParse(part[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                    || !float.TryParse(part[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                // Anything saved here was kept for good (buildings and portals from earlier versions always were).
                int tier = part.Length > 5 && int.TryParse(part[5], out int t) ? t : 0;
                Records.Add(new Record
                {
                    Kind = KindOf(part[0]),
                    Position = new Vector3(x, y, z),
                    Label = part.Length > 4 ? part[4] : "",
                    Tier = tier,
                    Pinned = !legacy,
                    Expires = legacy ? NewExpiry() : 0f,
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

        // Creatures, from tiny to boss: the paw gets bigger and the colour warmer.
        private static readonly Color[] TierColors =
        {
            new Color(1f, 0.86f, 0.30f),     // tiny
            new Color(1f, 0.60f, 0.20f),     // small
            new Color(0.93f, 0.22f, 0.18f),  // medium
            new Color(0.72f, 0.04f, 0.22f),  // large
            new Color(0.72f, 0.32f, 1f),     // boss
        };
        private static readonly float[] TierScale = { 0.55f, 0.75f, 1f, 1f, 1f };

        public static Color TierColor(int tier) => TierColors[Mathf.Clamp(tier, 0, TierColors.Length - 1)];

        private static Sprite SpriteFor(PingKind kind, int tier)
        {
            if (kind == PingKind.Monster && !PingHeimPlugin.SizeByStrength.Value) tier = 2;
            int key = kind == PingKind.Monster ? 100 + Mathf.Clamp(tier, 0, 4) : (int)kind;
            if (Sprites.TryGetValue(key, out var sprite) && sprite != null) return sprite;

            switch (kind)
            {
                case PingKind.Monster: sprite = MakeSprite(TierColor(tier), paw: true, scale: TierScale[Mathf.Clamp(tier, 0, 4)]); break;
                case PingKind.Trader: sprite = MakeSprite(new Color(1f, 1f, 1f), paw: false, scale: 1f); break;
                case PingKind.Spawner: sprite = MakeSprite(new Color(0.45f, 0.85f, 0.30f), paw: false, scale: 1f, skull: true); break;
                default: sprite = MakeSprite(new Color(0.30f, 0.55f, 1f), paw: false, scale: 1f); break;
            }

            Sprites[key] = sprite;
            return sprite;
        }

        public static Color ColorOf(PingKind kind)
        {
            switch (kind)
            {
                case PingKind.Monster: return new Color(0.95f, 0.35f, 0.32f);
                case PingKind.Trader: return Color.white;
                case PingKind.Player: return new Color(0.45f, 0.68f, 1f);
                case PingKind.Spawner: return new Color(0.55f, 0.88f, 0.40f);
                default: return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        private static float Ellipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float dx = (u - cx) / rx, dy = (v - cy) / ry;
            return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
        }

        /// <summary>Signed distance to the shape (negative inside), in 0-1 picture units.</summary>
        private static float Shape(float u, float v, bool paw, bool skull = false)
        {
            if (skull)
            {
                // A rounded skull with a jaw, and two eye holes cut out of it.
                float cranium = Ellipse(u, v, 0.50f, 0.58f, 0.30f, 0.28f);
                float jaw = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.15f, Mathf.Abs(v - 0.27f) - 0.13f);
                float shape = Mathf.Min(cranium, jaw);
                float eyes = Mathf.Min(Ellipse(u, v, 0.38f, 0.58f, 0.075f, 0.085f), Ellipse(u, v, 0.62f, 0.58f, 0.075f, 0.085f));
                return Mathf.Max(shape, -eyes);
            }

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

        private static Sprite MakeSprite(Color fill, bool paw, float scale, bool skull = false)
        {
            const int size = 64;
            const float rim = 0.045f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = 0.5f + ((x + 0.5f) / size - 0.5f) / scale, v = 0.5f + ((y + 0.5f) / size - 0.5f) / scale;
                    float d = Shape(u, v, paw, skull) * scale;
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
