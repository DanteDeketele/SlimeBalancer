# SlimeBalancer web game

A browser port of the Unity SlimeBalancer project, so it runs without Unity and can
be played from the SlimeBalancer Duo site (Games tab). Same four games, same rules,
the original art and audio:

| Game | How to play (same as Unity) |
| --- | --- |
| **Slime Ski** | Steer left and right through the flag gates (+10). Lean forward to go faster (up to 170 %), back to brake (down to 45 %); in a race each skier has their own speed. Every 50 points it gets faster. Hit a tree or snowman and it's over. Start speed 3 / 5 / 7 by difficulty. |
| **Tilt It** | Coloured slimes drop on a 6x6 board. Tilt it (up to 13°) so each slime rolls into the pipe of its colour (+10). A slime that leaves the board on a side always drops into that side's pipe. Wrong pipe costs a heart (5 / 3 / 2 hearts). A new slime every 11 / 9 / 7 s, or sooner when the board is empty. |
| **Snake** | The slime glides freely and turns towards the tilt (no grid). Touch slimes to add them to your tail (+10, a bit faster). Walls bounce you back; only your own tail ends it. 4 / 3 / 2 slimes on the field, speed 1.8 / 2.3 / 2.9. |
| **Balancequest** | Keep the slime on the 10x10 board (tilts up to 9°). +10 every 5 s. Falling off costs a heart (5 / 3 / 2) and a new slime drops in. Small / medium / big slime by difficulty. |

The games are tuned easier than the Unity build, because a tilted puck is less precise
than a keyboard: gentler board tilt, slower slimes, wider pipes, hearts instead of
instant game over, and a free-steering Snake.

Menus follow the Unity scenes: game carousel with high scores, info screen with the
tutorial video, a loading poster and 3-2-1-GO countdown, game over with *Opnieuw spelen*
and *Terug naar menu*, and settings (difficulty, volume 0-5, music on/off, clear high
scores, plus a new *Kantelrichting* switch to flip forward/back tilt).

## Controls

- **Dock / pucks:** the dock is one HID gamepad and each puck's tilt is a stick
  (full deflection at 15°). The game reads every stick, D-pad and face button of every
  connected gamepad and uses whichever is pushed furthest, so any paired puck can play.
  Tilt left/right to move through menus, tilt towards you to confirm, away from you to go back.
- **Keyboard:** arrow keys or WASD. Enter or Space confirms, Escape goes back.
- **Mouse:** click cards and buttons in the menus.

The board LED strip feedback from the Unity build is shown as a coloured glow around the screen.

## 2 players (local only)

Tilt up on the main menu (or press Escape / click *Spelers*) to switch between 1 and 2
players. Both players sit at the same screen; nothing goes over the network.

- **Player 1:** first puck (dock left stick), D-pad, WASD.
- **Player 2:** second puck (dock right stick), face buttons, a second gamepad, arrow keys.

| Game | 2-player mode |
| --- | --- |
| Skie | Race, split screen. The last skier standing wins. |
| Tilt It | Together on one board: player 1 tilts left/right, player 2 forward/back. |
| Snake | Duel, slither.io style: the snakes glide freely and turn towards the tilt. Eat drops to grow; hit the wall or the other snake and the other player wins. |
| Balancequest | Together on one board, like Tilt It. |

The site can pass the puck colours with `?colors=e53935,2a9df4`; otherwise player 1 is
teal and player 2 orange.

## Run locally

It is plain static files (ES modules, no build step). Serve the folder and open it:

```
cd WebGame
python3 -m http.server 8080
# http://localhost:8080
```

Handy URL options: `?game=skie|Tilt_It|Snake|BalanceQuest` jumps straight into a game,
`&players=2` starts it in 2-player mode,
and `&fast` skips the loading poster (used for automated checks).

## Publish on the SlimeBalancer Duo site

```
./build-zip.sh 1.1.0      # writes dist/slimebalancer-web-1.1.0.zip
```

Then on the site: **Admin → Firmware updates → Publish a new release**, Type *Web game ZIP*,
version `1.1.0`, pick the ZIP. It shows up under **Games** and runs in the full-screen
player. Hosting stays on the free Static Web App and the existing storage account; the
ZIP is about 11 MB.

## Code map

- `js/main.js` — screens and flow (Unity GameManager, MainMenuUI, InfoUI, CountdownUI, EndSceneUI, SettingsUI)
- `js/input.js` — gamepad + keyboard (Unity InputManager: 0.85 press / 0.5 release thresholds)
- `js/duo.js` — 2-player helpers (coloured board edges for co-op)
- `js/games/snakeio.js` — the 2-player Snake duel (free steering)
- `js/audio.js` — music and effects (Unity SoundManager)
- `js/physics.js` — spheres rolling on a tilting board (stands in for Unity's rigidbodies)
- `js/slime.js` — slime material (after SlimeShader: fresnel side colour + wobble)
- `js/games/*.js` — one file per game
- `js/vendor/` — three.js 0.170.0 (MIT) with FBXLoader, to load the Unity tree and carrot models

Assets in `assets/` are copied from `SlimeBalancer/Assets`, resized or re-encoded to keep the download small.
