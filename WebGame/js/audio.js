// Sound, ported from the Unity SoundManager: one looping music track at a time,
// one-shot effects on top, volume 0-5 and a music on/off switch kept in localStorage.
import { store } from "./store.js";

const base = "assets/audio/";
export const SOUNDS = {
  menu: "music-menu.mp3",
  tilt: "music-tilt.mp3",
  ski: "music-ski.mp3",
  snake: "music-snake.mp3",
  balance: "music-balance.mp3",
  gameOver: "game-over.mp3",
  select: "select.mp3",
  ui: "ui.mp3",
  beep: "beep.mp3",
  go: "go.mp3",
  score: "score.mp3",
  crash: "crash.mp3",
  grow: "grow.mp3",
  splat: "splat.mp3",
};

const effects = new Map();
let music = null;
let musicName = null;
let musicGain = 1;
let unlocked = false;

export const audio = {
  get volumeLevel() {
    return store.get("volume", 5);
  },
  set volumeLevel(v) {
    store.set("volume", Math.max(0, Math.min(5, v)));
    if (music) music.volume = this.volume * musicGain;
  },
  get volume() {
    return this.volumeLevel / 5;
  },
  get musicOn() {
    return store.get("musicOn", true);
  },
  toggleMusic() {
    store.set("musicOn", !this.musicOn);
    if (!this.musicOn) {
      if (music) music.pause();
    } else if (musicName) {
      const name = musicName;
      musicName = null;
      this.playMusic(name);
    }
  },
  preload() {
    for (const key of ["select", "ui", "beep", "go", "score", "crash", "grow", "gameOver", "splat"]) {
      const el = new Audio(base + SOUNDS[key]);
      el.preload = "auto";
      effects.set(key, el);
    }
  },
  unlock() {
    if (unlocked) return;
    unlocked = true;
    if (music && this.musicOn && music.paused) music.play().catch(() => { unlocked = false; });
  },
  play(key, { delayUntilQuiet = false } = {}) {
    if (this.volume <= 0) return;
    const src = effects.get(key);
    const el = src ? src.cloneNode() : new Audio(base + SOUNDS[key]);
    el.volume = this.volume;
    const start = () => el.play().catch(() => {});
    if (delayUntilQuiet) setTimeout(start, 350);
    else start();
  },
  playMusic(name) {
    if (musicName === name && music && !music.paused) return;
    this.stopMusic();
    musicName = name;
    musicGain = 1;
    if (!this.musicOn) return;
    music = new Audio(base + SOUNDS[name]);
    music.loop = true;
    music.volume = this.volume;
    music.play().catch(() => {});
  },
  duckMusic(gain = 0.5) {
    musicGain = gain;
    if (music) music.volume = this.volume * gain;
  },
  stopMusic() {
    if (music) {
      music.pause();
      music.src = "";
    }
    music = null;
    musicName = null;
  },
};
