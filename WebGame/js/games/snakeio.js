// Snake duel, slither.io style (2 players only; 1 player keeps the Unity grid Snake).
// Each snake glides freely and turns towards where its puck tilts. Eat slime drops
// to grow (+1 point each). Run your head into the wall or the other snake and you
// burst into drops: the other snake wins. Both at once is a draw.
import * as THREE from "three";
import { makeSlime, slimeMaterial, Splashes } from "../slime.js";

const AREA = { w: 30, h: 17 }; // centred on (0, 0)
const SPACING = 0.42; // distance between body balls
const START_LEN = 10;
const MAX_LEN = 400;
const TURN = 4.2; // rad/s
const HEAD_R = 0.45;
const BODY_R = 0.38;
const FOOD_COUNT = 45;

const toWorld = (x, y, out = new THREE.Vector3()) => out.set(x, 0.45, -y);

export class SnakeIoGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "snake";
    this.background = "snake";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(40, 16 / 9, 0.1, 200);
    this.speed = { easy: 3.6, medium: 4.5, hard: 5.4 }[ctx.difficulty];
    this.food = [];
    this.snakes = [];
  }

  resize(aspect) {
    const needW = AREA.w + 2;
    const needH = AREA.h + 3;
    const fov = THREE.MathUtils.degToRad(this.camera.fov);
    const d = Math.max(needH / 2 / Math.tan(fov / 2), needW / 2 / (Math.tan(fov / 2) * aspect));
    this.camera.position.set(0, d, -0.8);
    this.camera.lookAt(0, 0, -0.8);
  }

  async load() {
    const s = this.scene;
    s.add(new THREE.HemisphereLight(0xffffff, 0x445522, 1.4));
    const sun = new THREE.DirectionalLight(0xffffff, 1.4);
    sun.position.set(-6, 20, 8);
    s.add(sun);

    const c = document.createElement("canvas");
    c.width = AREA.w * 16;
    c.height = AREA.h * 16;
    const g = c.getContext("2d");
    for (let x = 0; x < AREA.w; x++) {
      for (let y = 0; y < AREA.h; y++) {
        g.fillStyle = (x + y) % 2 ? "#8fc94a" : "#a5d85a";
        g.fillRect(x * 16, y * 16, 16, 16);
      }
    }
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(AREA.w, AREA.h), new THREE.MeshStandardMaterial({ map: tex, roughness: 0.95 }));
    floor.rotation.x = -Math.PI / 2;
    s.add(floor);
    const border = new THREE.Mesh(new THREE.PlaneGeometry(AREA.w + 0.7, AREA.h + 0.7), new THREE.MeshStandardMaterial({ color: 0xd9dcd2 }));
    border.rotation.x = -Math.PI / 2;
    border.position.y = -0.02;
    s.add(border);

    // food: small slime drops, drawn instanced
    this.foodMesh = new THREE.InstancedMesh(new THREE.SphereGeometry(0.26, 14, 10), slimeMaterial(0xffffff, 0xffffff), 600);
    this.foodMesh.count = 0;
    this.foodMesh.frustumCulled = false;
    s.add(this.foodMesh);

    [
      { x: -9, y: -3, angle: 0 },
      { x: 9, y: 3, angle: Math.PI },
    ].forEach((st, i) => {
      const color = new THREE.Color(this.ctx.playerColor(i));
      const head = makeSlime(color, { eyes: true });
      head.scale.setScalar(HEAD_R * 2);
      s.add(head);
      const body = new THREE.InstancedMesh(new THREE.SphereGeometry(1, 18, 12), slimeMaterial(color, color.clone().offsetHSL(0.06, 0, 0.2)), MAX_LEN * 2);
      body.frustumCulled = false;
      s.add(body);
      this.snakes.push({ index: i, color, head, body, spawn: st });
    });
    this.snakes.forEach((sn) => this.respawn(sn));
    for (let i = 0; i < FOOD_COUNT; i++) this.addFood();
    this.splashes = new Splashes(s);
    this.drawBodies();
  }

  start() {}

  respawn(sn) {
    const st = sn.spawn;
    sn.x = st.x;
    sn.y = st.y;
    sn.angle = st.angle;
    sn.len = START_LEN;
    sn.alive = true;
    sn.trail = [];
    // lay the starting body out behind the head
    for (let i = 0; i <= START_LEN * 4; i++) {
      const d = i * (SPACING / 4);
      sn.trail.push({ x: sn.x - Math.cos(sn.angle) * d, y: sn.y - Math.sin(sn.angle) * d });
    }
  }

  addFood(x, y, value = 1) {
    const f = {
      x: x ?? (Math.random() - 0.5) * (AREA.w - 1.5),
      y: y ?? (Math.random() - 0.5) * (AREA.h - 1.5),
      value,
      color: new THREE.Color().setHSL(Math.random(), 0.85, 0.55),
      born: 0,
      seed: Math.random() * 10,
    };
    this.food.push(f);
    return f;
  }

  update(dt, active) {
    this.splashes.update(dt);
    if (active && !this.finished) {
      for (const sn of this.snakes) if (sn.alive) this.move(sn, dt);
      this.collide();
    }
    this.drawBodies(dt);
  }

  move(sn, dt) {
    const v = this.ctx.input.players[sn.index];
    if (Math.hypot(v.x, v.y) > 0.35) {
      const target = Math.atan2(v.y, v.x);
      let diff = Math.atan2(Math.sin(target - sn.angle), Math.cos(target - sn.angle));
      diff = THREE.MathUtils.clamp(diff, -TURN * dt, TURN * dt);
      sn.angle += diff;
    }
    const step = this.speed * dt;
    sn.x += Math.cos(sn.angle) * step;
    sn.y += Math.sin(sn.angle) * step;
    const last = sn.trail[0];
    if (Math.hypot(sn.x - last.x, sn.y - last.y) >= SPACING / 4) sn.trail.unshift({ x: sn.x, y: sn.y });
    const keep = sn.len * 4 + 8;
    if (sn.trail.length > keep) sn.trail.length = keep;

    // eat
    for (let i = this.food.length - 1; i >= 0; i--) {
      const f = this.food[i];
      if (Math.hypot(f.x - sn.x, f.y - sn.y) < HEAD_R + 0.25) {
        this.food.splice(i, 1);
        sn.len = Math.min(MAX_LEN, sn.len + f.value);
        this.ctx.addScore(f.value, sn.index);
        if (Math.random() < 0.35) this.ctx.audio.play("grow");
        if (this.food.length < FOOD_COUNT) this.addFood();
      }
    }
  }

  // body ball positions: every 2nd trail point, so the balls overlap into one smooth body
  points(sn) {
    const out = [];
    for (let i = 2; i < sn.trail.length && out.length < sn.len * 2; i += 2) out.push(sn.trail[i]);
    return out;
  }

  collide() {
    const dead = new Set();
    for (const sn of this.snakes) {
      if (!sn.alive) continue;
      if (Math.abs(sn.x) > AREA.w / 2 - HEAD_R || Math.abs(sn.y) > AREA.h / 2 - HEAD_R) dead.add(sn);
      for (const other of this.snakes) {
        if (other === sn || !other.alive) continue;
        if (Math.hypot(other.x - sn.x, other.y - sn.y) < HEAD_R * 2) {
          dead.add(sn);
          dead.add(other);
        }
        for (const p of this.points(other)) {
          if (Math.hypot(p.x - sn.x, p.y - sn.y) < HEAD_R + BODY_R) {
            dead.add(sn);
            break;
          }
        }
      }
    }
    for (const sn of dead) this.burst(sn);
    if (dead.size) this.finish();
  }

  burst(sn) {
    sn.alive = false;
    this.ctx.audio.play("crash");
    this.ctx.flash(sn.color.getHex());
    this.ctx.playerOut(sn.index);
    // the body turns into drops for the other player, slither.io style
    const pts = this.points(sn);
    pts.forEach((p, i) => {
      if (i % 4 === 0) this.addFood(p.x + (Math.random() - 0.5) * 0.4, p.y + (Math.random() - 0.5) * 0.4, 1).color.copy(sn.color);
    });
    this.splashes.burst(toWorld(sn.x, sn.y), sn.color, 26, 4);
    sn.head.visible = false;
    sn.body.visible = false;
  }

  // Show the burst for a moment, then the result.
  finish() {
    this.finished = true;
    const alive = this.snakes.filter((s) => s.alive);
    const winner = alive.length === 1 ? alive[0].index : null;
    setTimeout(() => this.ctx.gameOver({ winner }), 1200);
  }

  drawBodies(dt = 0) {
    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion();
    const pos = new THREE.Vector3();
    const sc = new THREE.Vector3();
    for (const sn of this.snakes) {
      const pts = this.points(sn);
      pts.forEach((p, i) => {
        const r = BODY_R * (1 - (i / Math.max(1, pts.length)) * 0.35);
        m.compose(toWorld(p.x, p.y, pos), q, sc.setScalar(r));
        sn.body.setMatrixAt(i, m);
      });
      sn.body.count = pts.length;
      sn.body.instanceMatrix.needsUpdate = true;
      toWorld(sn.x, sn.y, sn.head.position);
      sn.head.position.y = 0.5;
      sn.head.rotation.y = Math.atan2(Math.cos(sn.angle), Math.sin(sn.angle));
    }
    const t = performance.now() / 1000;
    this.food.forEach((f, i) => {
      f.born = Math.min(1, f.born + dt * 3);
      const s = f.born * (0.85 + Math.sin(t * 3 + f.seed) * 0.15) * (f.value > 1 ? 1.4 : 1);
      m.compose(pos.set(f.x, 0.25, -f.y), q, sc.setScalar(s));
      this.foodMesh.setMatrixAt(i, m);
      this.foodMesh.setColorAt(i, f.color);
    });
    this.foodMesh.count = this.food.length;
    this.foodMesh.instanceMatrix.needsUpdate = true;
    if (this.foodMesh.instanceColor) this.foodMesh.instanceColor.needsUpdate = true;
  }
}
