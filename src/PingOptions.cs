using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PingHeim
{
    /// <summary>
    /// The filters: which kinds of find are shown on the map. They appear as a small box on the map itself, and also
    /// as a window you can open from the stone (hold Shift while using it). The choices are saved in the config file.
    /// </summary>
    internal static class PingOptions
    {
        private const int WindowId = 0x91A6;
        private static bool _popup;
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;
        private static GUIStyle _title, _toggle, _small;
        private static Texture2D _background;

        private static Rect _mapBox;
        private static float _mapBoxScale;
        private static bool _draggingMapBox;
        private static Vector2 _grab;
        private static Rect _popupRect;
        private static bool _popupPlaced;

        public static bool PopupOpen => _popup;

        public static void OpenPopup()
        {
            if (_popup) return;
            _popup = true;
            _popupPlaced = false;
            _lockBefore = Cursor.lockState;
            _visibleBefore = Cursor.visible;
        }

        public static void ClosePopup()
        {
            if (!_popup) return;
            _popup = false;
            Cursor.lockState = _lockBefore;
            Cursor.visible = _visibleBefore;
        }

        private static float Scale => Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);

        /// <summary>Where the box sits on the map (in the same top-left coordinates the on-screen UI uses).</summary>
        private static Rect MapRect()
        {
            float s = Scale;
            float w = 210f * s, h = 268f * s;
            var saved = PingHeimPlugin.MapBoxPos.Value;
            if (_mapBox.width <= 0f || _mapBoxScale != s)
            {
                _mapBoxScale = s;
                _mapBox = saved.x < 0f ? new Rect(16f * s, Screen.height * 0.30f, w, h) : new Rect(saved.x, saved.y, w, h);
            }

            _mapBox.width = w;
            _mapBox.height = h;
            _mapBox.x = Mathf.Clamp(_mapBox.x, 0f, Mathf.Max(0f, Screen.width - w));
            _mapBox.y = Mathf.Clamp(_mapBox.y, 0f, Mathf.Max(0f, Screen.height - h));
            return _mapBox;
        }

        /// <summary>True if the mouse is over the box, so a click on it doesn't also land on the map underneath.</summary>
        public static bool MouseOverMapBox()
        {
            if (!Minimap.IsOpen()) return false;
            var mouse = Input.mousePosition;
            return MapRect().Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        }

        public static void Draw()
        {
            if (_popup)
            {
                DrawPopup();
                return;
            }

            if (!Minimap.IsOpen()) return;

            EnsureStyles();
            var rect = MapRect();
            GUI.DrawTexture(rect, Background());
            GUILayout.BeginArea(new Rect(rect.x + 10f * Scale, rect.y + 6f * Scale, rect.width - 20f * Scale, rect.height - 12f * Scale));
            GUILayout.Label("Ping Stone", _title);
            DrawToggles();
            GUILayout.EndArea();
            DragMapBox(rect);
        }

        /// <summary>Drag the filter box by any empty part of it (the toggles still take their own clicks).</summary>
        private static void DragMapBox(Rect rect)
        {
            var e = Event.current;
            var mouse = e.mousePosition;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(mouse))
            {
                _draggingMapBox = true;
                _grab = mouse - new Vector2(rect.x, rect.y);
            }
            else if (e.type == EventType.MouseDrag && _draggingMapBox)
            {
                _mapBox.x = mouse.x - _grab.x;
                _mapBox.y = mouse.y - _grab.y;
            }
            else if (e.type == EventType.MouseUp && _draggingMapBox)
            {
                _draggingMapBox = false;
                PingHeimPlugin.MapBoxPos.Value = new Vector2(_mapBox.x, _mapBox.y);
            }
        }

        private static void DrawPopup()
        {
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                ClosePopup();
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                ClosePopup();
                return;
            }

            EnsureStyles();
            float s = Scale;
            float width = 400f * s, height = 410f * s;
            if (!_popupPlaced)
            {
                var saved = PingHeimPlugin.PopupPos.Value;
                _popupRect = new Rect(saved.x < 0f ? (Screen.width - width) / 2f : saved.x, saved.y < 0f ? (Screen.height - height) / 2f : saved.y, width, height);
                _popupPlaced = true;
            }

            _popupRect.width = width;
            _popupRect.height = height;
            _popupRect.x = Mathf.Clamp(_popupRect.x, 0f, Mathf.Max(0f, Screen.width - width));
            _popupRect.y = Mathf.Clamp(_popupRect.y, 0f, Mathf.Max(0f, Screen.height - height));
            GUI.DrawTexture(_popupRect, Background());
            _popupRect = GUI.Window(WindowId, _popupRect, DrawPopupWindow, "Ping Stone");
            if (Event.current.type == EventType.MouseUp)
                PingHeimPlugin.PopupPos.Value = new Vector2(_popupRect.x, _popupRect.y);
        }

        private static void DrawPopupWindow(int id)
        {
            float s = Scale;
            GUILayout.Space(8f * s);
            GUILayout.Label("Choose what a ping shows on your map.", _small);
            GUILayout.Space(4f * s);
            DrawToggles();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Aggro ping: every hostile creature nearby hears you and comes for you.  Click with the stone equipped, or Shift+use it, to open this box.", _small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping now", GUILayout.Height(32f * s)))
            {
                ClosePopup();
                PingItem.Use(Player.m_localPlayer);
            }

            if (GUILayout.Button("Aggro ping", GUILayout.Height(32f * s)))
            {
                ClosePopup();
                AggroPing.Use(Player.m_localPlayer);
            }

            if (GUILayout.Button("Close (Esc)", GUILayout.Height(32f * s))) ClosePopup();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private static void DrawToggles()
        {
            Row(PingHeimPlugin.ShowMobs, "Creatures", PingKind.Monster);
            Row(PingHeimPlugin.ShowNpcs, "Traders", PingKind.Trader);
            Row(PingHeimPlugin.ShowPlayers, "Players", PingKind.Player);
            Row(PingHeimPlugin.ShowStructures, "Buildings", PingKind.Structure);
            Row(PingHeimPlugin.ShowPortals, "Portals", PingKind.Portal);
            Row(PingHeimPlugin.ShowSpawners, "Spawners", PingKind.Spawner);
        }

        private static void Row(ConfigEntry<bool> entry, string label, PingKind kind)
        {
            // White text with a square in the kind's own colour, which is easier to read than coloured text.
            string swatch = ColorUtility.ToHtmlStringRGB(Marks.ColorOf(kind));
            bool value = GUILayout.Toggle(entry.Value, $" <color=#{swatch}>\u25A0</color> {label}", _toggle);
            if (value != entry.Value) entry.Value = value;
        }

        /// <summary>A solid dark panel behind the options, so the text stands out on a busy map.</summary>
        private static Texture2D Background()
        {
            if (_background != null) return _background;
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.10f, 0.08f, 0.06f, 0.94f));
            _background.Apply();
            return _background;
        }

        private static void EnsureStyles()
        {
            float s = Scale;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _toggle = new GUIStyle(GUI.skin.toggle) { richText = true };
                _toggle.normal.textColor = _toggle.onNormal.textColor = _toggle.hover.textColor = _toggle.onHover.textColor = Color.white;
                _small = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }

            _title.fontSize = Mathf.RoundToInt(20f * s);
            _toggle.fontSize = Mathf.RoundToInt(18f * s);
            _toggle.padding.top = Mathf.RoundToInt(3f * s);
            _small.fontSize = Mathf.RoundToInt(14f * s);
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.window.fontSize = Mathf.RoundToInt(20f * s);
        }
    }

    /// <summary>Stops the player running around and the camera spinning while the options window is up.</summary>
    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class TakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (PingOptions.PopupOpen) __result = false;
        }
    }

    /// <summary>
    /// The player controller has its own input gate, separate from the player's. Without this, a click in the options
    /// window also made the player swing their weapon.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class ControllerTakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (PingOptions.PopupOpen) __result = false;
        }
    }

    /// <summary>A click on the options box must not also click the map beneath it.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    internal static class MapLeftClickPatch
    {
        private static bool Prefix(Minimap __instance)
        {
            if (PingOptions.MouseOverMapBox()) return false;
            // A click on one of our pins keeps it on the map (or lets it fade again).
            return !Marks.ToggleAtClick(__instance);
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    internal static class MapDoubleClickPatch
    {
        private static bool Prefix() => !PingOptions.MouseOverMapBox();
    }
}
