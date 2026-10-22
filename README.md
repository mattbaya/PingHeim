# PingHeim

A [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) + [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) mod for Valheim: a craftable **Ping Stone** that puts the creatures, traders, other players, buildings and portals around you on the map, with on/off switches for each kind. Thunderstore package: `PingHeim`.

Install: the Assets zip from a [GitHub release](https://github.com/mattbaya/PingHeim/releases) (not on Thunderstore yet).

Player-facing docs: [package/README.md](package/README.md). Version history: [package/CHANGELOG.md](package/CHANGELOG.md).

## Layout

| Path | Purpose |
| --- | --- |
| `src/` | Mod source (`PingHeim.csproj`, targets `netstandard2.1`) |
| `src/Resources/ping_icon.png` | The item's inventory icon (embedded) |
| `tools/make_ping.py` | Synthesizes `package/sounds/ping.wav` (standard library only) |
| `tools/make_icon.py` | Draws the item icon and the Thunderstore icon (standard library only) |
| `package/` | Thunderstore files: `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `sounds/` |
| `docs/images/` | Screenshots used in the Thunderstore description (served from GitHub) |
| `build.sh` | Builds and zips a Thunderstore package into `dist/` |
| `libs/` | Game assemblies and Jötunn to compile against (**git-ignored, never commit**) |

## How it works

The Ping Stone is a Jötunn custom item (cloned from the Surtling Core); a Harmony prefix on `Humanoid.UseItem` runs the ping (holding Shift opens the options window instead). `Scanner` walks the game's list of known objects (`ZDOMan.m_objectsByID`) within the radius and decides what each is from its prefab (a `Player`, a `Trader`, a `Character`, a `TeleportWorld`, or a player-built `Piece`). Everything found becomes a record in `Marks`, and a map pin is created or removed to match the on-map filters, so switching a kind off and on again needs no new ping. Creatures, traders and players expire after a few minutes. Buildings and portals are kept for good in the character's save data (`Player.m_customData["PingHeim.Places"]`), not in the map file, and are put back on the map whenever it loads. Buildings and portals use the game's own house and portal map icons (configurable by pin type); creatures and people use sprites drawn in code. `PingOptions` draws the filter box on the open map (IMGUI) and the window opened from the stone. The recharge is a status effect with a countdown. Detected players are told directly with a routed RPC (`PingHeim_Pinged`), which makes their game show the game's own ping at the source and a temporary pin for the pinger.

## Building

Same setup as [Bag O' Heim](https://github.com/mattbaya/BagOHeim#building) plus `UnityEngine.AudioModule`, `UnityEngine.UnityWebRequestModule`, `UnityEngine.UnityWebRequestAudioModule`, `UnityEngine.IMGUIModule` and `Splatform.dll` in `libs/Managed/`. Then `./build.sh`.

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for anything larger than a small fix. Releases to Thunderstore are published by the maintainer.
