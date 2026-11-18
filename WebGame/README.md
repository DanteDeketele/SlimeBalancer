# SlimeBalancer web game

A browser port of the Unity SlimeBalancer project, so it runs without Unity and can
be played from the SlimeBalancer Duo site (Games tab). Same four games, same rules,
the original art and audio:

| Game | How to play (same as Unity) |
| --- | --- |
| **Slime Ski** | Steer left and right through the flag gates (+10). Every 50 points it gets faster. Hit a tree or snowman and it's over. Start speed 3 / 5 / 7 by difficulty. |
| **Tilt It** | Coloured slimes drop on a 6x6 board. Tilt it (up to 27°) so each slime rolls into the pipe of its colour (+10). Wrong pipe ends the game. A new slime every 7 / 5 / 3 s. |
| **Snake** | Move cell by cell on a 22x12 field, only sideways turns. Touch slimes to add them to your tail (+10, a bit faster). Wall or own tail ends it. 3 / 2 / 1 slimes on the field. |
| **Balancequest** | Keep the slime on the 10x10 board (tilts up to 18°). +10 every 5 s. Small / medium / big slime by difficulty. |

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

## Run locally

It is plain static files (ES modules, no build step). Serve the folder and open it:

```
cd WebGame
python3 -m http.server 8080
# http://localhost:8080
```

Handy URL options: `?game=skie|Tilt_It|Snake|BalanceQuest` jumps straight into a game,
and `&fast` skips the loading poster (used for automated checks).

## Publish on the SlimeBalancer Duo site

```
./build-zip.sh 1.0.0      # writes dist/slimebalancer-web-1.0.0.zip
```

Then on the site: **Admin → Firmware updates → Publish a new release**, Type *Web game ZIP*,
version `1.0.0`, pick the ZIP. It shows up under **Games** and runs in the full-screen
player. Hosting stays on the free Static Web App and the existing storage account; the
ZIP is about 11 MB.

## Code map

- `js/main.js` — screens and flow (Unity GameManager, MainMenuUI, InfoUI, CountdownUI, EndSceneUI, SettingsUI)
- `js/input.js` — gamepad + keyboard (Unity InputManager: 0.85 press / 0.5 release thresholds)
- `js/audio.js` — music and effects (Unity SoundManager)
- `js/physics.js` — spheres rolling on a tilting board (stands in for Unity's rigidbodies)
- `js/slime.js` — slime material (after SlimeShader: fresnel side colour + wobble)
- `js/games/*.js` — one file per game
- `js/vendor/` — three.js 0.170.0 (MIT) with FBXLoader, to load the Unity tree and carrot models

Assets in `assets/` are copied from `SlimeBalancer/Assets`, resized or re-encoded to keep the download small.
