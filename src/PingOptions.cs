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

        public static bool PopupOpen => _popup;

        public static void OpenPopup()
        {
            if (_popup) return;
            _popup = true;
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
            float w = 210f * s, h = 238f * s;
            return new Rect(16f * s, Screen.height * 0.30f, w, h);
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
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 10f * Scale, rect.y + 6f * Scale, rect.width - 20f * Scale, rect.height - 12f * Scale));
            GUILayout.Label("Ping Stone", _title);
            DrawToggles();
            GUILayout.EndArea();
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
            float width = 360f * s, height = 330f * s;
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(rect, GUIContent.none);
            GUI.Window(WindowId, rect, DrawPopupWindow, "Ping Stone");
        }

        private static void DrawPopupWindow(int id)
        {
            float s = Scale;
            GUILayout.Space(8f * s);
            GUILayout.Label("Choose what a ping shows on your map.", _small);
            GUILayout.Space(4f * s);
            DrawToggles();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Hold Shift while using the stone to open this box. The same choices appear on the map.", _small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping now", GUILayout.Height(32f * s)))
            {
                ClosePopup();
                PingItem.Use(Player.m_localPlayer);
            }

            if (GUILayout.Button("Close (Esc)", GUILayout.Height(32f * s))) ClosePopup();
            GUILayout.EndHorizontal();
        }

        private static void DrawToggles()
        {
            Row(PingHeimPlugin.ShowMobs, "Creatures", PingKind.Monster);
            Row(PingHeimPlugin.ShowNpcs, "Traders", PingKind.Trader);
            Row(PingHeimPlugin.ShowPlayers, "Players", PingKind.Player);
            Row(PingHeimPlugin.ShowStructures, "Buildings", PingKind.Structure);
            Row(PingHeimPlugin.ShowPortals, "Portals", PingKind.Portal);
        }

        private static void Row(ConfigEntry<bool> entry, string label, PingKind kind)
        {
            var before = GUI.contentColor;
            GUI.contentColor = Marks.ColorOf(kind);
            bool value = GUILayout.Toggle(entry.Value, " " + label, _toggle);
            GUI.contentColor = before;
            if (value != entry.Value) entry.Value = value;
        }

        private static void EnsureStyles()
        {
            float s = Scale;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _toggle = new GUIStyle(GUI.skin.toggle);
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

    /// <summary>A click on the options box must not also click the map beneath it.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    internal static class MapLeftClickPatch
    {
        private static bool Prefix() => !PingOptions.MouseOverMapBox();
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    internal static class MapDoubleClickPatch
    {
        private static bool Prefix() => !PingOptions.MouseOverMapBox();
    }
}
