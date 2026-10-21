# PingHeim

**A craftable Ping Stone that lights up everything around you on the map: creatures, traders, other players and buildings.**

Use the stone and a sonar-style ping rolls out across your area. Everything it finds appears as a coloured dot on your map:

| Dot | What it is | How long it stays |
| --- | --- | --- |
| **Red** | Creatures: monsters and animals | 3 minutes |
| **White** | Traders (Haldor, Hildir, the Bog Witch) | 3 minutes |
| **Blue** | Other players | 3 minutes |
| **Grey square** | Buildings that players have built | Permanently |

Then the stone needs to recharge (10 minutes by default). A status icon with a countdown shows when it's ready again.

<!-- IMAGE: map-ping.png (the large map after a ping: red, white, blue dots and grey squares) -->

## Contents

- [Getting started](#getting-started)
- [Pinging other players](#pinging-other-players)
- [Examples](#examples)
- [What a ping can see](#what-a-ping-can-see)
- [Multiplayer](#multiplayer)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

## Getting started

1. Craft a **Ping Stone** at a **forge**: 3 Bronze, 5 Fine Wood, 1 Surtling Core.
2. Carry it. **Right-click it** in your inventory (or put it on the hotbar and press its key) to use it.
3. You hear the ping. Open your map: creatures, traders and players show as dots, and any player-built structures nearby are marked with grey squares.

The stone is **not used up**. After a ping it shows a recharge timer in your status effects, and using it too early just tells you how many minutes are left.

<!-- IMAGE: ping-stone.png (the Ping Stone in the inventory and on the hotbar with its recharge icon) -->

## Pinging other players

A ping isn't silent. Any **other player it detects** gets a **ping at your position**, just like a middle-click on the map: the word PING appears in the world and on their map. They are told "*Your name* pinged you", and **you show up on their map as a blue dot for 3 minutes**, so a ping reveals you as well as them.

You can switch that off with `Detect / NotifyPlayers`, so the ping stays your secret (you will still see them).

## Examples

**Before you enter a swamp crypt:** ping from the doorway to see what's wandering nearby, and which of your friends are close.

**Scouting a base site:** ping where you're thinking of building. The grey squares show whether someone's already built close by.

**Checking on the farm at night:** a ping tells you whether anything is lurking around the pens.

**Finding Haldor:** he moves. A ping in a Black Forest clearing shows a white dot if he's within range.

**Group hunts:** one player pings and everyone nearby sees them. Use it to meet up.

**Quiet scouting:** turn `NotifyPlayers` off and ping without alerting anyone you detect.

## What a ping can see

- It reaches **120 metres** around you by default (`Ping / Radius`). Valheim only keeps track of the areas close to you, so in practice a ping finds things within roughly **64 to 128 metres**, depending on where you stand in your area. You'll see more if you stand near the middle of an area than at its edge.
- **Creatures** include animals and monsters. It skips creatures you have tamed unless you turn that on (`IncludeTamedCreatures`), and you can limit it to hostile creatures (`OnlyHostileMonsters`) or add the creature's name beside each dot (`LabelMonsters`).
- **Buildings** are grouped into one grey square per 16 metres (`StructureClusterMeters`), so a whole base isn't covered in markers. They are real map pins and **stay on the map for good**. Repeated pings don't stack up duplicate squares. Only things that players built count, not ruins or dungeons.
- **Players** show as blue dots with their names.

## Multiplayer

Install **PingHeim on the server as well as every player**: the Ping Stone is a new item, and a server without the mod removes items it doesn't recognise. Versions must match. Everything the ping does (the dots, the notice other players get) is between players' own games, so there is nothing for the server to do beyond having the mod installed.

## Configuration

`BepInEx/config/mbaya.PingHeim.cfg`, or Gale's config editor:

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| Ping | Radius | 120 | Metres around you (limited by what the game knows, see above) |
| Ping | CooldownMinutes | 10 | Recharge time (restart the game to apply) |
| Ping | DotMinutes | 3 | How long creature, trader and player dots stay |
| Detect | Monsters | true | Red dots |
| Detect | OnlyHostileMonsters | false | Skip peaceful animals |
| Detect | IncludeTamedCreatures | false | |
| Detect | LabelMonsters | false | Show names beside red dots |
| Detect | Traders | true | White dots |
| Detect | Players | true | Blue dots |
| Detect | Structures | true | Permanent grey squares |
| Detect | StructureClusterMeters | 16 | One square per this many metres |
| Detect | NotifyPlayers | true | Detected players see your ping and you appear on their map |
| Sound | PingVolume | 0.8 | |

## Troubleshooting

- **The stone isn't in the forge's crafting list.** Check the mod is in the profile you launch from, and that you're at a forge with the Bronze, Fine Wood and Surtling Core.
- **Nothing happens when I use it.** Right-click it in your inventory. A message tells you if it's still recharging.
- **I see no dots.** Dots appear on the map: open it (M). Only things within the area the game keeps track of can be found, so try again from a more central spot.
- **The dots disappeared.** Creature, trader and player dots expire after `DotMinutes` (3 by default). Building squares stay.
- **A building square is in the wrong place.** Squares sit at the middle of each group of buildings (`StructureClusterMeters`). Delete one on the map like any pin if you don't want it.
- **The server removes my Ping Stone.** The server needs the mod too, with the same version.
- **Something else.** Open an issue at [github.com/mattbaya/PingHeim](https://github.com/mattbaya/PingHeim) and attach `BepInEx/LogOutput.log`.

## Works with

- **[PortalHeim SG1](https://thunderstore.io/c/valheim/p/Svaha/PortalHeim_SG1/)**: ping your gates' surroundings before you step through.
- **[GearHeim](https://thunderstore.io/c/valheim/p/Svaha/GearHeim/)** and **[Bag O' Heim](https://thunderstore.io/c/valheim/p/Svaha/BagOHeim/)**: keep your loadout and supplies ready for whatever the ping shows.
