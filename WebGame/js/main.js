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
      "Stuur links en rechts om tussen de vlaggen door te gaan en punten te verdienen. Hoe langer je blijft skiën, hoe hoger je score!",
      "Maar pas op! Bomen en sneeuwmannen blokkeren de piste, raak je er één, dan is het spel voorbij.",
    ],
    Game: SkiGame,
  },
  {
    id: "Tilt_It",
    name: "Tilt It",
    genre: "Puzzle",
    logo: "assets/img/logo-tilt.jpg",
    video: "assets/video/tilt.mp4",
    text: ["Verschillende kleuren slimes vallen naar beneden! Bestuur het bord en zorg ervoor dat de slimes in de juiste buis terecht komen met het overeenkomstige kleur."],
    Game: TiltGame,
  },
  {
    id: "Snake",
    name: "Snake",
    genre: "Klassieker",
    logo: "assets/img/logo-snake.jpg",
    video: "assets/video/snake.mp4",
    text: ["Beweeg over het veld om andere slimes aan te tikken en achter jou te plaatsen. Word zo lang mogelijk zonder tegen de muur of jezelf te botsen."],
    Game: SnakeGame,
  },
  {
    id: "BalanceQuest",
    name: "Balancequest",
    genre: "Rustig",
    logo: "assets/img/logo-balance.jpg",
    video: "assets/video/balance.mp4",
    text: ["Hou zo lang mogelijk de slime op het bord zonder hem ervan af te laten vallen.", "Hoe langer op het bord, hoe meer punten!"],
    Game: BalanceGame,
  },
];

const DIFFICULTIES = ["easy", "medium", "hard"];
const DIFFICULTY_LABEL = { easy: "Makkelijk", medium: "Gemiddeld", hard: "Moeilijk" };

const $ = (sel) => document.querySelector(sel);
const params = new URLSearchParams(location.search);

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
  GAMES.forEach((g, i) => list.append(card({ title: g.name, genre: g.genre, img: g.logo, index: i })));
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
    if (i < GAMES.length) score.textContent = highScore(GAMES[i].id);
  });
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
}

// ---------- info ----------
function showInfo(game) {
  state.gameData = game;
  show("info");
  audio.playMusic("menu");
  $("#info-title").textContent = `Game: ${game.name}`;
  $("#info-text").innerHTML = game.text.map((t) => `<p>${t}</p>`).join("");
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
  addScore(n) {
    state.score += n;
    $("#score").textContent = state.score;
    $("#score").classList.remove("pop");
    void $("#score").offsetWidth;
    $("#score").classList.add("pop");
    if (state.score > highScore(state.gameData.id)) store.set(`highscore.${state.gameData.id}`, state.score);
  },
  gameOver() {
    if (state.active) endGame();
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
  state.active = false;
  state.timeScale = 1;
  $("#score").textContent = "0";
  show("countdown");
  audio.stopMusic();
  document.body.dataset.bg = "";
  $("#cd-title").textContent = `Game: ${game.name}`;
  $("#cd-logo").src = game.logo;
  $("#cd-poster").hidden = false;
  $("#cd-number").textContent = "";
  const bar = $("#cd-bar");
  bar.style.transition = "none";
  bar.style.width = "0%";

  const instance = new game.Game(ctx);
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

function endGame() {
  state.active = false;
  state.timeScale = 0.5; // Unity slows time down behind the game over screen
  audio.duckMusic(0.5);
  audio.play("gameOver", { delayUntilQuiet: true });
  ctx.flash(0xff3b3b);
  show("end");
  $("#end-score").textContent = `Score: ${state.score}`;
  $("#end-high").textContent = `High Score: ${highScore(state.gameData.id)}`;
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
    renderer.render(state.game.scene, state.game.camera);
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
