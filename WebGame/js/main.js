// SlimeBalancer for the browser. Same flow as the Unity build:
// splash -> menu (game carousel + settings) -> info -> countdown -> game -> game over.
import * as THREE from "three";
import { input, updateInput, connectedGamepads } from "./input.js";
import { audio } from "./audio.js";
import { store } from "./store.js";
import { tickSlimes } from "./slime.js";
import { SkiGame } from "./games/ski.js";
import { TiltGame } from "./games/tilt.js";
import { SnakeGame } from "./games/snake.js";
import { SnakeIoGame } from "./games/snakeio.js";
import { BalanceGame } from "./games/balance.js";

// Unity GameManager.AvailableGames, in the same order with the same texts.
const GAMES = [
  {
    id: "skie",
    name: "Slime Ski",
    genre: "Actie",
    logo: "assets/img/logo-ski.jpg",
    video: "assets/video/ski.mp4",
    text: [
      "Stuur links en rechts om tussen de vlaggen door te gaan en punten te verdienen. Kantel naar voren om sneller te gaan, naar achteren om af te remmen.",
      "Maar pas op! Bomen en sneeuwmannen blokkeren de piste, raak je er één, dan is het spel voorbij.",
    ],
    Game: SkiGame,
    duo: {
      mode: "versus",
      genre: "Race",
      text: ["Twee skiërs op dezelfde piste, elk met een eigen helft van het scherm. Stuur door de vlaggen voor punten. Kantel naar voren om je tegenstander voorbij te steken, naar achteren om te remmen.", "Wie botst, ligt eruit. Wie het langst blijft skiën wint, en daarna telt de score."],
    },
  },
  {
    id: "Tilt_It",
    name: "Tilt It",
    genre: "Puzzle",
    logo: "assets/img/logo-tilt.jpg",
    video: "assets/video/tilt.mp4",
    text: ["Verschillende kleuren slimes vallen naar beneden! Bestuur het bord en zorg ervoor dat de slimes in de juiste buis terecht komen met het overeenkomstige kleur.", "Valt een slime in de verkeerde buis, dan verlies je een hartje."],
    Game: TiltGame,
    duo: {
      mode: "coop",
      genre: "Samen",
      text: ["Samen één bord! Speler 1 kantelt links en rechts, speler 2 kantelt naar voren en naar achteren.", "Werk samen om elke slime in de buis met zijn eigen kleur te krijgen."],
    },
  },
  {
    id: "Snake",
    name: "Snake",
    genre: "Klassieker",
    logo: "assets/img/logo-snake.jpg",
    video: "assets/video/snake.mp4",
    text: ["Kantel je puck en je slime glijdt vrij over het veld in die richting. Tik andere slimes aan om ze achter je te plaatsen.", "Tegen de muur bots je gewoon terug. Word zo lang mogelijk zonder in je eigen staart te rijden!"],
    Game: SnakeGame,
    duo: {
      mode: "versus",
      genre: "Duel",
      Game: SnakeIoGame,
      text: ["Twee slangen glijden vrij over het veld en draaien mee met je puck. Eet slimedruppels om langer te worden.", "Bots je met je kop tegen de muur of de andere slang, dan spat je uit elkaar en wint de ander. Snij je tegenstander de weg af!"],
    },
  },
  {
    id: "BalanceQuest",
    name: "Balancequest",
    genre: "Rustig",
    logo: "assets/img/logo-balance.jpg",
    video: "assets/video/balance.mp4",
    text: ["Hou zo lang mogelijk de slime op het bord zonder hem ervan af te laten vallen.", "Hoe langer op het bord, hoe meer punten! Valt hij eraf, dan verlies je een hartje."],
    Game: BalanceGame,
    duo: {
      mode: "coop",
      genre: "Samen",
      text: ["Samen één bord! Speler 1 kantelt links en rechts, speler 2 kantelt naar voren en naar achteren.", "Hou de slime samen zo lang mogelijk op het bord."],
    },
  },
];

const DIFFICULTIES = ["easy", "medium", "hard"];
const DIFFICULTY_LABEL = { easy: "Makkelijk", medium: "Gemiddeld", hard: "Moeilijk" };

const $ = (sel) => document.querySelector(sel);
const params = new URLSearchParams(location.search);

// Player colours. The site passes each puck's colour (?colors=30d5c8,ff8a3d) in the
// same order as its player list; without it we use teal and orange.
const DEFAULT_COLORS = ["#30d5c8", "#ff8a3d"];
const PLAYER_COLORS = (params.get("colors") || "")
  .split(",")
  .map((c) => c.trim().replace(/^#/, ""))
  .filter((c) => /^[0-9a-f]{6}$/i.test(c))
  .map((c) => `#${c}`);
const playerColor = (i) => PLAYER_COLORS[i] || DEFAULT_COLORS[i];
document.documentElement.style.setProperty("--p1", playerColor(0));
document.documentElement.style.setProperty("--p2", playerColor(1));
if (params.get("players") === "1" || params.get("players") === "2") store.set("players", Number(params.get("players")));
const playerCount = () => (store.get("players", 1) === 2 ? 2 : 1);
const duo = () => playerCount() === 2;
const scoreKey = (game) => (duo() ? `${game.id}.duo` : game.id);

// ---------- renderer ----------
const canvas = $("#gl");
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.05;

function resize() {
  const w = innerWidth;
  const h = innerHeight;
  renderer.setSize(w, h, false);
  if (state.game) {
    state.game.camera.aspect = w / h;
    state.game.camera.updateProjectionMatrix();
    state.game.resize?.(w / h);
  }
}
addEventListener("resize", resize);

// ---------- state ----------
const state = {
  screen: null,
  menuIndex: store.get("lastGame", 0),
  settingsIndex: 0,
  gameData: null,
  game: null,
  active: false,
  timeScale: 1,
  score: 0,
  endReadyAt: 0,
  endChoice: 0,
  unsub: [],
};

const difficulty = () => store.get("difficulty", "medium");
const highScore = (id) => store.get(`highscore.${id}`, 0);
const gameGenre = (g) => (duo() ? g.duo.genre : g.genre);
const gameText = (g) => (duo() ? g.duo.text : g.text);
input.invertY = store.get("invertY", false);

function show(name) {
  for (const fn of state.unsub) fn();
  state.unsub = [];
  document.querySelectorAll(".screen").forEach((el) => el.classList.toggle("is-active", el.id === `screen-${name}`));
  document.body.dataset.screen = name;
  state.screen = name;
}

function onInput(dir, fn) {
  state.unsub.push(input.on(dir, fn));
}

// ---------- splash ----------
function showSplash() {
  show("splash");
  const go = () => {
    audio.unlock();
    showMenu();
  };
  onInput("any", go);
  $("#screen-splash").onclick = go;
}

// ---------- main menu ----------
function buildCarousel() {
  const list = $("#carousel");
  list.innerHTML = "";
  GAMES.forEach((g, i) => list.append(card({ title: g.name, genre: gameGenre(g), img: g.logo, index: i })));
  list.append(card({ title: "Instellingen", genre: "Instellingen", icon: "assets/img/settings.png", index: GAMES.length }));
}

function card({ title, genre, img, icon, index }) {
  const el = document.createElement("button");
  el.className = "game-card";
  el.type = "button";
  el.innerHTML = `
    <div class="game-icon${icon ? " is-settings" : ""}">${img ? `<img src="${img}" alt="">` : `<img class="settings-glyph" src="${icon}" alt="">`}</div>
    <div class="game-title">${title}</div>
    <div class="game-meta"><span class="game-genre">${genre}</span><span class="game-score"><img src="assets/img/trophy.png" alt="Highscore"><b></b></span></div>
    <div class="game-hint"><span>${icon ? "Druk om instellingen te openen" : "Druk om te spelen"}</span><img src="assets/img/board-down.png" alt=""></div>`;
  el.addEventListener("click", () => {
    if (state.menuIndex === index) menuConfirm();
    else {
      state.menuIndex = index;
      audio.play("ui");
      renderMenu();
    }
  });
  return el;
}

function renderMenu() {
  const cards = [...document.querySelectorAll("#carousel .game-card")];
  cards.forEach((c, i) => {
    c.classList.toggle("selected", i === state.menuIndex);
    const score = c.querySelector(".game-score b");
    if (i < GAMES.length) {
      score.textContent = highScore(scoreKey(GAMES[i]));
      c.querySelector(".game-genre").textContent = gameGenre(GAMES[i]);
    }
  });
  const toggle = $("#players-toggle");
  toggle.classList.toggle("is-duo", duo());
  toggle.querySelector(".players-label").textContent = duo() ? "2 spelers" : "1 speler";
  const sel = cards[state.menuIndex];
  const track = $("#carousel");
  if (sel) {
    const offset = sel.offsetLeft + sel.offsetWidth / 2;
    track.style.transform = `translateX(${track.parentElement.clientWidth / 2 - offset}px)`;
  }
  store.set("lastGame", state.menuIndex);
}

function menuConfirm() {
  audio.unlock();
  audio.play("select");
  if (state.menuIndex === GAMES.length) showSettings();
  else showInfo(GAMES[state.menuIndex]);
}

function showMenu() {
  show("menu");
  audio.playMusic("menu");
  renderMenu();
  requestAnimationFrame(renderMenu);
  onInput("left", () => {
    if (state.menuIndex > 0) {
      state.menuIndex--;
      audio.play("ui");
      renderMenu();
    }
  });
  onInput("right", () => {
    if (state.menuIndex < GAMES.length) {
      state.menuIndex++;
      audio.play("ui");
      renderMenu();
    }
  });
  onInput("down", menuConfirm);
  onInput("up", togglePlayers);
  $("#players-toggle").onclick = togglePlayers;
}

function togglePlayers() {
  store.set("players", duo() ? 1 : 2);
  audio.play("select");
  renderMenu();
}

// ---------- info ----------
function showInfo(game) {
  state.gameData = game;
  show("info");
  audio.playMusic("menu");
  $("#info-title").textContent = `Game: ${game.name}${duo() ? (game.duo.mode === "coop" ? " · samen" : " · duel") : ""}`;
  $("#info-text").innerHTML = gameText(game).map((t) => `<p>${t}</p>`).join("") + (duo() ? playersLegend() : "");
  const video = $("#info-video");
  video.src = game.video;
  video.play().catch(() => {});
  const start = () => {
    audio.play("ui");
    video.pause();
    startGame(game);
  };
  const back = () => {
    audio.play("ui");
    video.pause();
    showMenu();
  };
  onInput("down", start);
  onInput("up", back);
  $("#info-start").onclick = start;
  $("#info-back").onclick = back;
}

function playersLegend() {
  return `<div class="players-legend"><span><i style="background:${playerColor(0)}"></i>Speler 1 · puck 1 of WASD</span><span><i style="background:${playerColor(1)}"></i>Speler 2 · puck 2 of pijltjes</span></div>`;
}

// ---------- settings ----------
const SETTINGS = [
  { title: "Game Menu", icon: "assets/img/gamemenu.png" },
  { title: "Moeilijkheid", value: () => DIFFICULTY_LABEL[difficulty()], small: true },
  { title: "Geluid", value: () => String(audio.volumeLevel) },
  { title: "Muziek", value: () => (audio.musicOn ? "Aan" : "Uit") },
  { title: "Highscores", value: () => "Wissen" },
  { title: "Kantelrichting", value: () => (input.invertY ? "Omgekeerd" : "Normaal"), small: true },
];

function buildSettings() {
  const list = $("#settings-carousel");
  list.innerHTML = "";
  SETTINGS.forEach((s, i) => {
    const el = document.createElement("button");
    el.type = "button";
    el.className = "game-card setting-card";
    el.innerHTML = `
      <div class="game-icon is-settings">${s.icon ? `<img class="settings-glyph" src="${s.icon}" alt="">` : `<span class="setting-value${s.small ? " is-small" : ""}"></span>`}</div>
      <div class="game-title">${s.title}</div>
      <div class="game-hint">${i === 0 ? `<span>Druk om terug te gaan</span><img src="assets/img/board-down.png" alt="">` : `<span>Naar voren: hoger · naar je toe: lager</span>`}</div>`;
    el.addEventListener("click", () => {
      if (state.settingsIndex === i) settingsAction("down");
      else {
        state.settingsIndex = i;
        audio.play("ui");
        renderSettings();
      }
    });
    list.append(el);
  });
}

function renderSettings() {
  const cards = [...document.querySelectorAll("#settings-carousel .game-card")];
  cards.forEach((c, i) => {
    c.classList.toggle("selected", i === state.settingsIndex);
    const v = c.querySelector(".setting-value");
    if (v && SETTINGS[i].value) v.textContent = SETTINGS[i].value();
  });
  const sel = cards[state.settingsIndex];
  const track = $("#settings-carousel");
  if (sel) track.style.transform = `translateX(${track.parentElement.clientWidth / 2 - (sel.offsetLeft + sel.offsetWidth / 2)}px)`;
}

function settingsAction(dir) {
  const i = state.settingsIndex;
  const up = dir === "up";
  if (i === 0) {
    if (up) return;
    audio.play("select");
    return showMenu();
  }
  if (i === 1) {
    const d = DIFFICULTIES.indexOf(difficulty());
    store.set("difficulty", DIFFICULTIES[(d + (up ? 1 : 2)) % 3]);
  } else if (i === 2) {
    audio.volumeLevel = audio.volumeLevel + (up ? 1 : -1);
  } else if (i === 3) {
    audio.toggleMusic();
  } else if (i === 4) {
    if (up) return;
    GAMES.forEach((g) => store.remove(`highscore.${g.id}`));
    flashMessage("Highscores verwijderd");
  } else if (i === 5) {
    input.invertY = !input.invertY;
    store.set("invertY", input.invertY);
  }
  audio.play("select");
  renderSettings();
}

function showSettings() {
  show("settings");
  audio.playMusic("menu");
  renderSettings();
  requestAnimationFrame(renderSettings);
  onInput("left", () => {
    if (state.settingsIndex > 0) {
      state.settingsIndex--;
      audio.play("ui");
      renderSettings();
    }
  });
  onInput("right", () => {
    if (state.settingsIndex < SETTINGS.length - 1) {
      state.settingsIndex++;
      audio.play("ui");
      renderSettings();
    }
  });
  onInput("up", () => settingsAction("up"));
  onInput("down", () => settingsAction("down"));
}

function flashMessage(text) {
  const el = $("#toast");
  el.textContent = text;
  el.classList.add("is-visible");
  clearTimeout(flashMessage.t);
  flashMessage.t = setTimeout(() => el.classList.remove("is-visible"), 1800);
}

// ---------- game ----------
const ctx = {
  input,
  audio,
  get difficulty() {
    return difficulty();
  },
  get score() {
    return state.score;
  },
  get playerCount() {
    return playerCount();
  },
  get mode() {
    return duo() ? state.gameData.duo.mode : "solo";
  },
  playerColor,
  scores: [0, 0],
  // Board tilt for Tilt It / Balancequest. Together: player 1 steers left/right,
  // player 2 forward/back.
  boardInput() {
    if (ctx.mode === "coop") return { x: input.players[0].x, y: input.players[1].y };
    return input.vector;
  },
  // Points for the whole game (solo, co-op), or for one player in a duel.
  addScore(n, player = null) {
    let el = $("#score");
    let total;
    if (player === null) {
      state.score += n;
      total = state.score;
      el.textContent = total;
    } else {
      ctx.scores[player] += n;
      total = ctx.scores[player];
      state.score = Math.max(...ctx.scores);
      el = document.querySelector(`#score-p${player + 1} b`);
      el.textContent = total;
    }
    el.classList.remove("pop");
    void el.offsetWidth;
    el.classList.add("pop");
    const key = scoreKey(state.gameData);
    if (total > highScore(key)) store.set(`highscore.${key}`, total);
  },
  // Hearts in the corner (Tilt It, Balancequest). null hides them.
  setLives(left, total = left) {
    const el = $("#lives");
    el.hidden = left === null;
    if (left === null) return;
    el.innerHTML = Array.from({ length: total }, (_, i) => `<i class="${i < left ? "" : "lost"}">♥</i>`).join("");
    el.classList.remove("pop");
    void el.offsetWidth;
    el.classList.add("pop");
  },
  playerOut(player) {
    document.querySelector(`#score-p${player + 1}`).classList.add("is-out");
  },
  // result: { winner } for a duel (null = draw)
  gameOver(result = {}) {
    if (state.active) endGame(result);
  },
  // Stand-in for the board's LED strip: a short coloured glow around the screen.
  flash(color) {
    const el = $("#flash");
    el.style.setProperty("--flash", `#${new THREE.Color(color).getHexString()}`);
    el.classList.remove("on");
    void el.offsetWidth;
    el.classList.add("on");
  },
};

async function startGame(game) {
  disposeGame();
  state.gameData = game;
  state.score = 0;
  ctx.scores = [0, 0];
  state.active = false;
  state.timeScale = 1;
  $("#score").textContent = "0";
  ctx.setLives(null);
  document.querySelectorAll(".score-player").forEach((el) => {
    el.classList.remove("is-out");
    el.querySelector("b").textContent = "0";
  });
  document.body.dataset.mode = duo() ? game.duo.mode : "solo";
  show("countdown");
  audio.stopMusic();
  document.body.dataset.bg = "";
  $("#cd-title").textContent = `Game: ${game.name}${duo() ? (game.duo.mode === "coop" ? " · samen" : " · duel") : ""}`;
  $("#cd-players").innerHTML = duo() ? playersLegend() : "";
  $("#cd-logo").src = game.logo;
  $("#cd-poster").hidden = false;
  $("#cd-number").textContent = "";
  const bar = $("#cd-bar");
  bar.style.transition = "none";
  bar.style.width = "0%";

  const Game = duo() && game.duo.Game ? game.duo.Game : game.Game;
  const instance = new Game(ctx);
  const loading = instance.load();
  const loadSeconds = params.has("fast") ? 0.2 : 4;
  let skipped = false;
  onInput("down", () => (skipped = true));
  requestAnimationFrame(() => {
    bar.style.transition = `width ${loadSeconds}s linear`;
    bar.style.width = "100%";
  });
  const until = performance.now() + loadSeconds * 1000;
  await loading;
  while (performance.now() < until && !skipped) await wait(50);
  if (state.screen !== "countdown") return;

  state.game = instance;
  document.body.dataset.bg = instance.background;
  resize();
  $("#cd-poster").hidden = true;

  const label = $("#cd-number");
  for (let i = 3; i > 0; i--) {
    audio.play("beep");
    label.className = "cd-number";
    label.textContent = String(i);
    void label.offsetWidth;
    label.classList.add("slam");
    await wait(params.has("fast") ? 50 : 1100);
    if (state.game !== instance) return;
  }
  // Unity: OnCountdownFinished fires, then "GO!" shows while the game already runs.
  show("game");
  instance.start();
  state.active = true;
  audio.playMusic(instance.music);
  audio.play("go");
  label.textContent = "GO!";
  label.className = "cd-number go slam";
  await wait(1100);
  label.textContent = "";
}

function endGame(result = {}) {
  state.active = false;
  state.timeScale = 0.5; // Unity slows time down behind the game over screen
  audio.duckMusic(0.5);
  audio.play("gameOver", { delayUntilQuiet: true });
  ctx.flash(0xff3b3b);
  show("end");
  const mode = duo() ? state.gameData.duo.mode : "solo";
  $("#end-title").textContent = "Game Over";
  $("#end-title").style.color = "";
  if (mode === "versus") {
    const w = result.winner;
    $("#end-title").textContent = w === null || w === undefined ? "Gelijkspel!" : `Speler ${w + 1} wint!`;
    if (w !== null && w !== undefined) $("#end-title").style.color = playerColor(w);
    $("#end-score").innerHTML = `<span class="end-p" style="--c:${playerColor(0)}">Speler 1: ${ctx.scores[0]}</span> <span class="end-p" style="--c:${playerColor(1)}">Speler 2: ${ctx.scores[1]}</span>`;
  } else {
    $("#end-score").textContent = mode === "coop" ? `Samen: ${state.score}` : `Score: ${state.score}`;
  }
  $("#end-high").textContent = `High Score: ${highScore(scoreKey(state.gameData))}`;
  state.endChoice = 0;
  state.endReadyAt = performance.now() + 2000;
  renderEnd();
  const ready = () => performance.now() >= state.endReadyAt;
  onInput("left", () => {
    if (!ready() || state.endChoice === 0) return;
    state.endChoice = 0;
    audio.play("ui");
    renderEnd();
  });
  onInput("right", () => {
    if (!ready() || state.endChoice === 1) return;
    state.endChoice = 1;
    audio.play("ui");
    renderEnd();
  });
  onInput("down", () => ready() && endConfirm());
  $("#end-restart").onclick = () => {
    state.endChoice = 0;
    endConfirm(true);
  };
  $("#end-menu").onclick = () => {
    state.endChoice = 1;
    endConfirm(true);
  };
}

function renderEnd() {
  const disabled = performance.now() < state.endReadyAt;
  $("#end-restart").classList.toggle("focused", state.endChoice === 0);
  $("#end-menu").classList.toggle("focused", state.endChoice === 1);
  document.querySelectorAll("#screen-end .end-button").forEach((b) => b.classList.toggle("disabled", disabled));
}

function endConfirm(byClick = false) {
  if (!byClick && performance.now() < state.endReadyAt) return;
  audio.play("select");
  audio.stopMusic();
  if (state.endChoice === 0) startGame(state.gameData);
  else {
    disposeGame();
    showMenu();
  }
}

function disposeGame() {
  if (!state.game) return;
  state.game.scene.traverse((o) => {
    o.geometry?.dispose?.();
  });
  state.game = null;
  state.active = false;
  document.body.dataset.bg = "";
  renderer.clear();
}

const wait = (ms) => new Promise((r) => setTimeout(r, ms));

// ---------- status footer ----------
function renderStatus() {
  const pads = connectedGamepads();
  const online = pads.length > 0;
  document.querySelectorAll(".board-status").forEach((el) => {
    el.classList.toggle("is-online", online);
    el.querySelector(".board-label").textContent = online ? "Dock online" : "Geen dock · pijltjestoetsen";
  });
}
addEventListener("gamepadconnected", renderStatus);
addEventListener("gamepaddisconnected", renderStatus);

// ---------- loop ----------
let last = performance.now();
let statusTimer = 0;
function frame(now) {
  const dt = Math.min(0.05, (now - last) / 1000);
  last = now;
  updateInput();
  statusTimer += dt;
  if (statusTimer > 1) {
    statusTimer = 0;
    renderStatus();
    if (state.screen === "end") renderEnd();
  }
  if (state.screen === "end" && performance.now() >= state.endReadyAt && document.querySelector("#screen-end .disabled")) renderEnd();
  tickSlimes(now / 1000);
  if (state.game) {
    state.game.update(dt * state.timeScale, state.active);
    if (state.game.render) state.game.render(renderer);
    else renderer.render(state.game.scene, state.game.camera);
  }
  requestAnimationFrame(frame);
}

// Clicking anywhere unlocks audio (browsers need a gesture; a gamepad doesn't count).
addEventListener("pointerdown", () => audio.unlock());
addEventListener("keydown", () => audio.unlock());

audio.preload();
buildCarousel();
buildSettings();
resize();
renderStatus();
if (params.get("game")) {
  const g = GAMES.find((x) => x.id.toLowerCase() === params.get("game").toLowerCase());
  if (g) startGame(g);
  else showSplash();
} else showSplash();
requestAnimationFrame(frame);

// for automated checks
window.__slime = { state, GAMES, startGame };
