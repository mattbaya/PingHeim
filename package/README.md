# PingHeim

**A craftable Ping Stone that lights up everything around you on the map: creatures, traders, other players and buildings.**

Use the stone and a sonar-style ping rolls out across your area. Everything it finds appears as a coloured dot on your map:

| Map icon | What it is |
| --- | --- |
| **Paw print** | Creatures: monsters and animals. The size and colour show how big and strong they are (see below) |
| **White person** | Traders (Haldor, Hildir, the Bog Witch) |
| **Blue person** | Other players |
| **House** (the game's own icon) | Buildings that players have built |
| **Portal** (the game's own icon, with the portal's name) | Portals that players have built |
| **Green skull** | Creature spawners: nests, bone piles and the like, labelled with what they make |

**Everything fades.** Whatever a ping finds stays on your map for 3 minutes (`Ping / DotMinutes`), then disappears. **Click any of them on the map to keep it for good**: it's marked "(kept)" and saved with your character. Click it again to let it fade.

**Creatures by size and strength.** The paw gets bigger and warmer in colour the tougher the creature is: **tiny** (small yellow paw: deer, boar, necks), **small** (orange), **medium** (red), **large** (big crimson paw: trolls, lox and the like) and **bosses** (big purple). It goes by the creature's health, so a starred creature shows a tier higher. Switch it off with `Detect / SizeByStrength` for plain red paws.

Then the stone needs to recharge (10 minutes by default). A status icon with a countdown shows when it's ready again.

![The map after a ping: red paw prints for creatures, house icons for buildings, and the Ping Stone filter box on the left](https://raw.githubusercontent.com/mattbaya/PingHeim/main/docs/images/map-ping.jpg)

## Contents

- [Getting started](#getting-started)
- [Choosing what to show](#choosing-what-to-show)
- [Pinging other players](#pinging-other-players)
- [Examples](#examples)
- [What a ping can see](#what-a-ping-can-see)
- [Multiplayer](#multiplayer)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

## Equip it, then choose

Right-click the Ping Stone to **equip it** in your hand. Now **click**: a small box opens with your choices.

- **Ping now**: the normal ping, described below.
- **Aggro ping**: every hostile creature within 60 m hears you and comes for you. It's a way to pull a camp or a nest to where you want to fight, so use it on purpose. Bosses stay put unless you turn `Aggro / IncludeBosses` on. It has its own recharge (60 seconds), separate from the normal ping's.
- The switches for what a ping shows on your map.

**No click? Press P.** While you carry a Ping Stone, **P** (change it with `Ping / OpenKey`) opens the same box, equipped or not. Close the inventory first: clicks inside the inventory screen only move items.

## Getting started

1. Craft a **Ping Stone** at a **forge**: 3 Bronze, 5 Fine Wood, 1 Surtling Core.
2. Carry it. **Right-click it** to equip it, then **click** to open its choices (or Shift+use it in your inventory).
3. You hear the ping. Open your map: creatures, traders and players show as icons, and any player-built buildings and portals nearby are marked with the game's house and portal icons.

The stone is **not used up**. After a ping it shows a recharge timer in your status effects, and using it too early just tells you how many minutes are left.

![The Ping Stone in the inventory, with its recharge countdown at the top right](https://raw.githubusercontent.com/mattbaya/PingHeim/main/docs/images/ping-stone.jpg)

## Choosing what to show

Don't want every kind of find? Open the map and you'll see a small **Ping Stone** box with a switch for each kind: **Creatures, Traders, Players, Buildings, Portals, Spawners**. Click one to hide or show it straight away. You don't need to ping again: everything the last ping found is kept, and the switches only decide what's displayed.

You can also open the same box from the stone: **hold Shift while you use it**. It has a **Ping now** button too. Your choices are saved in the config file.

![The Ping Stone filter box on the map](https://raw.githubusercontent.com/mattbaya/PingHeim/main/docs/images/map-filters.jpg)

## Pinging other players

A ping isn't silent. Any **other player it detects** gets a **ping at your position**, just like a middle-click on the map: the word PING appears in the world and on their map. They are told "*Your name* pinged you", and **you show up on their map as a blue dot for 3 minutes**, so a ping reveals you as well as them.

You can switch that off with `Detect / NotifyPlayers`, so the ping stays your secret (you will still see them).

## Examples

**Before you enter a swamp crypt:** ping from the doorway to see what's wandering nearby, and which of your friends are close.

**Scouting a base site:** ping where you're thinking of building. The house icons show whether someone's already built close by.

**Checking on the farm at night:** a ping tells you whether anything is lurking around the pens.

**Finding Haldor:** he moves. A ping in a Black Forest clearing shows a white person if he's within range.

**Group hunts:** one player pings and everyone nearby sees them. Use it to meet up.

**Quiet scouting:** turn `NotifyPlayers` off and ping without alerting anyone you detect.

## What a ping can see

- It reaches **120 metres** around you by default (`Ping / Radius`). Valheim only keeps track of the areas close to you, so in practice a ping finds things within roughly **64 to 128 metres**, depending on where you stand in your area. You'll see more if you stand near the middle of an area than at its edge.
- **Creatures** include animals and monsters. It skips creatures you have tamed unless you turn that on (`IncludeTamedCreatures`), and you can limit it to hostile creatures (`OnlyHostileMonsters`) or add the creature's name beside each dot (`LabelMonsters`).
- **Buildings** are grouped into one house icon per 16 metres (`StructureClusterMeters`), so a whole base isn't covered in markers. Repeated pings don't stack up duplicates: a ping near a building it already knows just renews it. They fade like everything else unless you click one to keep it. Only things that players built count, not ruins or dungeons.
- **Players** show as blue people with their names. **Portals** show with their names, so you can find your way back to one.

## Multiplayer

**Pings are shared.** When one player uses a Ping Stone, every other player with PingHeim hears the ping and sees the same results on their own map, along with a message saying who pinged. The caster's spot also shows as a normal ping with their name (PING in the world and on the map), as if they'd pinged from the map themselves. Each player's own map filters still apply. You don't need a stone of your own to receive it. Turn it off with `Share / ShareWithPlayers` (what you send) and `Share / ReceiveFromPlayers` (what you accept).

Install **PingHeim on the server as well as every player**: the Ping Stone is a new item, and a server without the mod removes items it doesn't recognise. Versions must match. Everything the ping does (the dots, the notice other players get) is between players' own games, so there is nothing for the server to do beyond having the mod installed.

## Configuration

`BepInEx/config/mbaya.PingHeim.cfg`, or Gale's config editor:

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| Ping | Radius | 120 | Metres around you (limited by what the game knows, see above) |
| Ping | CooldownMinutes | 10 | Recharge time (restart the game to apply) |
| Ping | DotMinutes | 3 | How long everything a ping finds stays on the map, unless you click it to keep it |
| Aggro | Radius / IncludeBosses / CooldownSeconds | 60 / false / 60 | How the aggro ping works |
| Detect | SizeByStrength | true | Creatures' paws are bigger and warmer in colour the stronger they are |
| Detect | Monsters | true | Show creatures (red paw prints) |
| Detect | OnlyHostileMonsters | false | Skip peaceful animals |
| Detect | IncludeTamedCreatures | false | |
| Detect | LabelMonsters | false | Show names beside red dots |
| Detect | Traders | true | Show traders (white people) |
| Detect | Players | true | Show players (blue people) |
| Detect | Structures | true | Show buildings (house icon) |
| Detect | Portals | true | Show portals (portal icon, with names) |
| Detect | Spawners | true | Show creature spawners (green skull) |
| Icons | BuildingIcon | Icon1 | Which of the game's map icons marks a building (the house) |
| Icons | PortalIcon | Icon4 | Which of the game's map icons marks a portal |
| Detect | StructureClusterMeters | 16 | One house icon per this many metres |
| Detect | NotifyPlayers | true | Detected players see your ping and you appear on their map |
| Share | ShareWithPlayers | true | Everyone else hears your ping and sees what it found |
| Share | ReceiveFromPlayers | true | Hear other players' pings and show what they found |
| Window | PopupPosition / MapBoxPosition | -1, -1 | Where you left the options window and the map filter box (drag them) |
| Sound | PingVolume | 0.8 | |

## Troubleshooting

- **The stone isn't in the forge's crafting list.** Check the mod is in the profile you launch from, and that you're at a forge with the Bronze, Fine Wood and Surtling Core.
- **Nothing happens when I use it.** Right-click it in your inventory. A message tells you if it's still recharging.
- **I see no dots.** Dots appear on the map: open it (M). Only things within the area the game keeps track of can be found, so try again from a more central spot.
- **The icons disappeared.** Everything a ping finds fades after `DotMinutes` (3 by default) unless you clicked it on the map to keep it. Also check the switches in the Ping Stone box on the map.
- **The house or portal icon looks wrong.** The game's icons aren't named in its files, so `Icons / BuildingIcon` and `Icons / PortalIcon` pick them by number. The game's icon names are written to `BepInEx/LogOutput.log` when you first open the map; send it to me if they're wrong.
- **A building icon is in the wrong place.** Icons sit at the middle of each group of buildings (`StructureClusterMeters`).
- **The server removes my Ping Stone.** The server needs the mod too, with the same version.
- **Something else.** Open an issue at [github.com/mattbaya/PingHeim](https://github.com/mattbaya/PingHeim) and attach `BepInEx/LogOutput.log`.

## Works with

- **[PortalHeim SG1](https://thunderstore.io/c/valheim/p/Svaha/PortalHeim_SG1/)**: ping your gates' surroundings before you step through.
- **[GearHeim](https://thunderstore.io/c/valheim/p/Svaha/GearHeim/)** and **[Bag O' Heim](https://thunderstore.io/c/valheim/p/Svaha/BagOHeim/)**: keep your loadout and supplies ready for whatever the ping shows.
