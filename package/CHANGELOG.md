# Changelog

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
