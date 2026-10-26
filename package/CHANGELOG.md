# Changelog

## 0.6.6
- **Old buildings and portals no longer all say "(kept)".** Marks saved by older versions (which kept everything for good) now come back as ordinary marks that fade after `DotMinutes`, and the saved list is rewritten once. Only marks you click on the map are kept and labelled "(kept)". If you want an old one back, ping again and click it.

## 0.6.5
- **A diagnostic for hosts**: about 40 seconds after you spawn in a world you host, the log lists the biggest pieces of stored data in the world (`Big object: ...`) and the total. It's to find what stops players from connecting. Turn off with `Diagnostics / LogBigObjects`.

## 0.6.4
- **Fixed: clicking with the Ping Stone equipped punched instead of opening its options.** The game doesn't count the stone as a weapon, so the check now looks at what's in your hand.

## 0.6.3
- **A key opens the Ping Stone's options**: press **P** (`Ping / OpenKey`) while you carry a stone, with no menu open. It works whether or not the stone is equipped, so it's there if clicking with it equipped doesn't open the box.

## 0.6.2
- Each patch is now applied on its own, so one the game has changed can't stop the others (the log names the one that failed).

## 0.6.1
- The Ping Stone no longer looks exactly like a Surtling Core in your hand or on the ground: it, its glow and its light are now a cold blue.

## 0.6.0
- **The Ping Stone is equippable.** Right-click it to equip it in your hand; then **click** and a small box opens with what you can do: **Ping now**, **Aggro ping**, and what a ping shows on the map. (Shift+use still opens the box straight away.)
- **Aggro ping.** Every hostile creature within 60 m (`Aggro / Radius`) is alerted and sent after you. Bosses aren't called unless you turn `Aggro / IncludeBosses` on. It has its own 60-second recharge (`Aggro / CooldownSeconds`) and doesn't use up the normal ping's.

## 0.5.0
- **Spawners.** A ping now also finds creature spawners (nests, bone piles and the like) and marks them with a green skull, labelled with what they make. They fade like everything else unless you click one to keep it. There's a *Spawners* switch in the box on the map and in the options window (`Detect / Spawners`). Fixed creature spawn points inside dungeons may not be found. Everyone needs this version for shared pings.

## 0.4.0
- **Everything fades now**, buildings and portals included, after `DotMinutes` (3 by default). A ping near a building it already knows just renews it. **Click a mark on the map to keep it for good** (it's marked "(kept)" and saved with your character); click it again to let it fade. Buildings and portals you already had are kept.
- **Creatures show how big and strong they are**: tiny (small yellow paw), small (orange), medium (red), large (big crimson), boss (big purple), by health. `Detect / SizeByStrength` turns it off. Shared pings carry it too, so everyone needs this version.

## 0.3.1
- **A shared ping also shows as the caster's own ping**: the word PING and their name appear where they stood, in the world and on the map, exactly as if they had middle-clicked the map.

## 0.3.0
- **Pings are shared.** When one player uses a Ping Stone, every other player with the mod hears the ping and gets the same results on their own map (creatures, traders, buildings, portals, and the player who pinged). They get a message saying who pinged and what was found. Their own map filters still decide what's shown. Switch it off with `Share / ShareWithPlayers` (what you send) and `Share / ReceiveFromPlayers` (what you accept).

## 0.2.4
- **The options window and the filter box on the map can be moved.** Drag either by any empty part; they remember where you left them (`Window / PopupPosition`, `Window / MapBoxPosition`).

## 0.2.3
- **The filter box is easier to read.** A solid dark background, white labels, and a square in each kind's colour, instead of red and blue text on a see-through box. Screenshots added to the description.

## 0.2.2
- Shorter package description (Thunderstore allows at most 256 characters). No changes to how the mod works.

## 0.2.1
- **Clicking in the options window no longer swings your weapon.** The game's player controller has its own input gate, which the window wasn't blocking.

## 0.2.0
- **Filters on the map:** a small "Ping Stone" box appears on the map with switches for Creatures, Traders, Players, Buildings and Portals. Switching one off hides it right away; switching it on brings it back. Hold **Shift** while using the stone to open the same box as a window.
- **New icons:** buildings use the game's **house** icon and portals its **portal** icon (with their names). Creatures are a red **paw print**, traders a white **person** and players a blue **person**.
- **Portals** are now found and marked too.
- Buildings and portals are now remembered in your character's save data instead of as map pins, so they can be switched off and on.
- A new ping replaces the previous ping's creatures, traders and players.
- The game's map icon names are written to the log the first time you open the map, so the icon choice (`Icons / BuildingIcon`, `Icons / PortalIcon`) can be checked.

## 0.1.0
- First version: the Ping Stone, map dots for creatures, traders, players and structures, a recharge timer, and a ping that tells the players you detect.
