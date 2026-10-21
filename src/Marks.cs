using System.Collections.Generic;
using UnityEngine;

namespace PingHeim
{
    internal enum DotKind { Monster, Trader, Player, Structure }

    /// <summary>
    /// The dots on the map. Creatures, traders and players are temporary and removed after a few minutes. Structures are
    /// ordinary saved map pins (so they stay for good), recognised by an invisible name so their icon can be restored
    /// after the map is loaded again.
    /// </summary>
    internal static class Marks
    {
        // A pin name that displays as nothing, used to tell our structure pins from the player's own.
        public const string StructureName = "​";
        private const float SavedPinCheckSeconds = 3f;

        private class Timed
        {
            public Minimap.PinData Pin;
            public float Expires;
        }

        private static readonly List<Timed> Active = new List<Timed>();
        private static readonly Dictionary<DotKind, Sprite> Sprites = new Dictionary<DotKind, Sprite>();
        private static Minimap _forMap;
        private static float _nextSavedCheck;

        private static Sprite SpriteFor(DotKind kind)
        {
            if (Sprites.TryGetValue(kind, out var sprite) && sprite != null) return sprite;

            Color fill;
            switch (kind)
            {
                case DotKind.Monster: fill = new Color(0.92f, 0.20f, 0.18f); break;
                case DotKind.Trader: fill = new Color(1f, 1f, 1f); break;
                case DotKind.Player: fill = new Color(0.27f, 0.50f, 1f); break;
                default: fill = new Color(0.58f, 0.58f, 0.58f); break;
            }

            sprite = MakeSprite(fill, kind == DotKind.Structure);
            Sprites[kind] = sprite;
            return sprite;
        }

        /// <summary>A small round dot (or square) with a dark rim so it shows up on the map.</summary>
        private static Sprite MakeSprite(Color fill, bool square)
        {
            const int size = 40;
            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            const float body = 14f, rim = 17f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
                    float d = square ? Mathf.Max(dx, dy) : Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01(body - d + 0.5f);
                    float outside = Mathf.Clamp01(rim - d + 0.5f);
                    var color = Color.Lerp(new Color(0f, 0f, 0f, 0.85f), fill, inside);
                    color.a = Mathf.Max(inside, outside * 0.85f);
                    pixels[y * size + x] = color;
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "PingHeimDot", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, false);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var made = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            made.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return made;
        }

        /// <summary>A temporary dot that disappears after the configured time.</summary>
        public static void AddDot(Vector3 position, DotKind kind, string label)
        {
            var map = Minimap.instance;
            if (map == null) return;

            var pin = map.AddPin(position, Minimap.PinType.Icon3, label ?? "", false, false);
            pin.m_icon = SpriteFor(kind);
            Active.Add(new Timed { Pin = pin, Expires = Time.time + PingHeimPlugin.DotMinutes.Value * 60f });
            map.m_pinUpdateRequired = true;
        }

        /// <summary>A permanent grey square, unless one is already there.</summary>
        public static bool AddStructure(Vector3 position, float spacing)
        {
            var map = Minimap.instance;
            if (map == null) return false;

            foreach (var pin in map.m_pins)
            {
                if (pin.m_name != StructureName) continue;
                var offset = pin.m_pos - position;
                offset.y = 0f;
                if (offset.magnitude < spacing * 0.6f) return false;
            }

            var added = map.AddPin(position, Minimap.PinType.Icon3, StructureName, true, false);
            added.m_icon = SpriteFor(DotKind.Structure);
            map.m_pinUpdateRequired = true;
            return true;
        }

        public static void Tick()
        {
            var map = Minimap.instance;
            if (map != _forMap)
            {
                // A different map (a world was loaded or left): our old pins went with it.
                Active.Clear();
                _forMap = map;
                _nextSavedCheck = 0f;
            }

            if (map == null) return;

            bool changed = false;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Time.time < Active[i].Expires) continue;
                map.RemovePin(Active[i].Pin);
                Active.RemoveAt(i);
                changed = true;
            }

            // Saved structure pins come back with the game's own icon when the map loads: put ours back.
            if (Time.time >= _nextSavedCheck)
            {
                _nextSavedCheck = Time.time + SavedPinCheckSeconds;
                var icon = SpriteFor(DotKind.Structure);
                foreach (var pin in map.m_pins)
                {
                    if (pin.m_name == StructureName && pin.m_icon != icon)
                    {
                        pin.m_icon = icon;
                        changed = true;
                    }
                }
            }

            if (changed) map.m_pinUpdateRequired = true;
        }
    }
}
