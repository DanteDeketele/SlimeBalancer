// Input: the dock shows up as one HID gamepad. Each puck's tilt is mapped to a
// stick (full deflection at 15 degrees). We read every connected gamepad and use
// whichever stick or D-pad is pushed furthest, so any paired puck can play.
// Keyboard (arrows / WASD) is the fallback, like the Unity build's Input System.
//
// Convention (same as the Unity InputManager): vector.x > 0 is right,
// vector.y > 0 is "up" (tilt away from you). "Down" (tilt towards you) is OK/confirm.

const DEADZONE = 0.12;
const PRESS = 0.85; // Unity: sensitivity for a direction "press"
const RELEASE = 0.5; // Unity: back to neutral below this magnitude

const keys = new Set();
const listeners = { up: [], down: [], left: [], right: [], any: [] };

export const input = {
  vector: { x: 0, y: 0 },
  source: "keyboard",
  gamepadName: "",
  lastActivity: performance.now(),
  invertY: false,
  on(dir, fn) {
    listeners[dir].push(fn);
    return () => {
      const list = listeners[dir];
      const i = list.indexOf(fn);
      if (i >= 0) list.splice(i, 1);
    };
  },
  clear() {
    for (const k of Object.keys(listeners)) listeners[k].length = 0;
  },
};

let pressedIn = false;
let last = { x: 0, y: 0 };

function emit(dir) {
  input.lastActivity = performance.now();
  for (const fn of [...listeners[dir]]) fn();
  for (const fn of [...listeners.any]) fn(dir);
}

addEventListener("keydown", (e) => {
  const k = e.key.toLowerCase();
  if (["arrowup", "arrowdown", "arrowleft", "arrowright", " "].includes(k)) e.preventDefault();
  if (e.repeat) return;
  keys.add(k);
  input.lastActivity = performance.now();
  // Enter / Space confirm, Escape / Backspace go back.
  if (k === "enter" || k === " ") emit("down");
  if (k === "escape" || k === "backspace") emit("up");
});
addEventListener("keyup", (e) => keys.delete(e.key.toLowerCase()));
addEventListener("blur", () => keys.clear());

function keyboardVector() {
  let x = 0;
  let y = 0;
  if (keys.has("arrowleft") || keys.has("a") || keys.has("q")) x -= 1;
  if (keys.has("arrowright") || keys.has("d")) x += 1;
  if (keys.has("arrowup") || keys.has("w") || keys.has("z")) y += 1;
  if (keys.has("arrowdown") || keys.has("s")) y -= 1;
  return { x, y };
}

function shape(v) {
  const m = Math.abs(v);
  if (m < DEADZONE) return 0;
  return Math.sign(v) * Math.min(1, (m - DEADZONE) / (1 - DEADZONE));
}

function gamepadVector() {
  const pads = navigator.getGamepads ? navigator.getGamepads() : [];
  let best = null;
  let bestMag = 0;
  for (const pad of pads) {
    if (!pad || !pad.connected) continue;
    const candidates = [];
    if (pad.axes.length >= 2) candidates.push({ x: pad.axes[0], y: -pad.axes[1] });
    if (pad.axes.length >= 4) candidates.push({ x: pad.axes[2], y: -pad.axes[3] });
    const b = (i) => (pad.buttons[i] ? pad.buttons[i].value : 0);
    if (pad.buttons.length >= 16) candidates.push({ x: b(15) - b(14), y: b(12) - b(13) });
    // Face buttons (the dock can map a puck to them): A = down, Y = up, X = left, B = right.
    if (pad.buttons.length >= 4) candidates.push({ x: b(1) - b(2), y: b(3) - b(0) });
    for (const c of candidates) {
      const v = { x: shape(c.x), y: shape(c.y) };
      const mag = Math.hypot(v.x, v.y);
      if (!best || mag > bestMag) {
        best = v;
        bestMag = mag;
        input.gamepadName = pad.id;
      }
    }
  }
  return best;
}

export function connectedGamepads() {
  const pads = navigator.getGamepads ? navigator.getGamepads() : [];
  return [...pads].filter((p) => p && p.connected);
}

export function updateInput() {
  const kb = keyboardVector();
  const gp = gamepadVector();
  let v = kb;
  input.source = "keyboard";
  if (gp && Math.hypot(gp.x, gp.y) > Math.hypot(kb.x, kb.y)) {
    v = gp;
    input.source = "gamepad";
  } else if (gp && kb.x === 0 && kb.y === 0) {
    input.source = "gamepad";
  }
  let x = Math.max(-1, Math.min(1, v.x));
  let y = Math.max(-1, Math.min(1, v.y));
  if (input.invertY) y = -y;
  input.vector.x = x;
  input.vector.y = y;

  if (Math.hypot(x - last.x, y - last.y) > 0.1) input.lastActivity = performance.now();

  // Direction events, same rules as the Unity InputManager.
  if (!pressedIn) {
    let fired = false;
    if (y > PRESS && last.y <= PRESS) { emit("up"); fired = true; }
    else if (y < -PRESS && last.y >= -PRESS) { emit("down"); fired = true; }
    if (x > PRESS && last.x <= PRESS) { emit("right"); fired = true; }
    else if (x < -PRESS && last.x >= -PRESS) { emit("left"); fired = true; }
    if (fired) pressedIn = true;
  } else if (Math.hypot(x, y) < RELEASE) {
    pressedIn = false;
  }
  last = { x, y };
}
