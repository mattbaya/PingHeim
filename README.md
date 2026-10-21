# PingHeim

A [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) mod for Valheim: a craftable **Ping Stone** that puts the creatures, traders, other players and player-built structures around you on the map. Thunderstore package: `PingHeim`.

Player-facing docs: [package/README.md](package/README.md).

> Testing build: not yet tried in the game.

## Layout

| Path | Purpose |
| --- | --- |
| `src/` | Mod source (`PingHeim.csproj`, targets `netstandard2.1`) |
| `src/Resources/ping_icon.png` | The item's inventory icon (embedded) |
| `tools/make_ping.py` | Synthesizes `package/sounds/ping.wav` (standard library only) |
| `tools/make_icon.py` | Draws the item icon and the Thunderstore icon (standard library only) |
| `package/` | Thunderstore files |
| `build.sh` | Builds and zips a Thunderstore package into `dist/` |
| `libs/` | Game assemblies and Jötunn to compile against (**git-ignored, never commit**) |

## How it works

The Ping Stone is a Jötunn custom item (cloned from the Surtling Core). A Harmony prefix on `Humanoid.UseItem` runs the ping. `Scanner` walks the game's list of known objects (`ZDOMan.m_objectsByID`) within the radius and decides what each is from its prefab (a `Player`, a `Trader`, a `Character`, or a player-built `Piece`). `Marks` puts them on the minimap: temporary pins for creatures, traders and players, saved pins (named with an invisible character, so their icon can be restored when a map loads) for structures. The recharge is a status effect with a countdown. Detected players are told directly with a routed RPC (`PingHeim_Pinged`), which makes their game show the game's own ping at the source and a temporary pin for the pinger.

## Building

Same setup as [Bag O' Heim](https://github.com/mattbaya/BagOHeim#building) plus `UnityEngine.AudioModule`, `UnityEngine.UnityWebRequestModule`, `UnityEngine.UnityWebRequestAudioModule` and `Splatform.dll` in `libs/Managed/`. Then `./build.sh`.

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for anything larger than a small fix. Releases to Thunderstore are published by the maintainer.
