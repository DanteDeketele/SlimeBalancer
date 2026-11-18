// Snake (Unity: SnakeGame.cs, SnakeCollider.cs, Snake.unity).
// A slime moves cell by cell over a 22x12 field. Tilt to turn (only sideways turns,
// one per cell). Touch other slimes to add them to your tail (+10, a bit faster).
// Hitting the wall or your own tail ends the game.
import * as THREE from "three";
import { makeSlime, Splashes } from "../slime.js";

const AREA = { w: 22, h: 12, cx: 0.5, cy: 0.5 }; // Unity PlayAreaSize, centred at (0.5, 0.5)
const DIRS = { up: [0, 1], down: [0, -1], left: [-1, 0], right: [1, 0] };

// grid (x, y) with y up the screen -> world (x, 0, -y)
const toWorld = (x, y, out = new THREE.Vector3()) => out.set(x, 0.5, -y);

export class SnakeGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "snake";
    this.background = "snake";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(40, 16 / 9, 0.1, 200);
    this.moveSpeed = 1; // cells per second, as in the Unity scene
    this.dir = DIRS.up;
    this.segments = [];
    this.pending = [];
    this.history = [];
    this.canMove = true;
    this.pickups = [];
    this.wave = { active: false, t: 0 };
  }

  resize(aspect) {
    // Fit the whole field (plus border) like the Unity top-down camera.
    const needW = AREA.w + 2.5;
    const needH = AREA.h + 2.5;
    const fov = THREE.MathUtils.degToRad(this.camera.fov);
    const distH = needH / 2 / Math.tan(fov / 2);
    const distW = needW / 2 / (Math.tan(fov / 2) * aspect);
    const d = Math.max(distH, distW);
    this.camera.position.set(AREA.cx, d, -AREA.cy - 0.6);
    this.camera.lookAt(AREA.cx, 0, -AREA.cy - 0.6);
  }

  async load() {
    const s = this.scene;
    s.add(new THREE.HemisphereLight(0xffffff, 0x445522, 1.4));
    const sun = new THREE.DirectionalLight(0xffffff, 1.5);
    sun.position.set(-6, 20, 8);
    sun.castShadow = true;
    sun.shadow.mapSize.set(2048, 2048);
    Object.assign(sun.shadow.camera, { left: -14, right: 14, top: 10, bottom: -10, near: 1, far: 50 });
    s.add(sun);

    // checkerboard floor, one tile per cell
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
    tex.magFilter = THREE.NearestFilter;
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(AREA.w, AREA.h), new THREE.MeshStandardMaterial({ map: tex, roughness: 0.95 }));
    floor.rotation.x = -Math.PI / 2;
    floor.position.set(AREA.cx, 0, -AREA.cy);
    floor.receiveShadow = true;
    s.add(floor);
    const border = new THREE.Mesh(
      new THREE.PlaneGeometry(AREA.w + 0.7, AREA.h + 0.7),
      new THREE.MeshStandardMaterial({ color: 0xd9dcd2, roughness: 0.8 })
    );
    border.rotation.x = -Math.PI / 2;
    border.position.set(AREA.cx, -0.02, -AREA.cy);
    s.add(border);
    const ground = new THREE.Mesh(new THREE.PlaneGeometry(200, 200), new THREE.MeshStandardMaterial({ color: 0x3f5a1c, roughness: 1 }));
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = -8;
    s.add(ground);

    this.head = makeSlime(0x7b3fe4, { side: 0x3fd0ff, eyes: true });
    this.head.position.copy(toWorld(0, 0));
    s.add(this.head);
    this.splashes = new Splashes(s);
  }

  start() {
    this.cellStart = [0, 0];
    this.cellEnd = [0 + this.dir[0], 0 + this.dir[1]];
    this.lastDir = this.dir;
    this.progress = 0;
    this.history = [[0, 0]];
    const count = { easy: 3, medium: 2, hard: 1 }[this.ctx.difficulty];
    for (let i = 0; i < count; i++) this.spawnSlime();
  }

  occupied() {
    const set = new Set(this.history.map(([x, y]) => `${x},${y}`));
    set.add(`${this.cellEnd[0]},${this.cellEnd[1]}`);
    for (const p of this.pickups) set.add(`${p.x},${p.y}`);
    return set;
  }

  spawnSlime() {
    const used = this.occupied();
    const minX = AREA.cx - AREA.w / 2 + 0.5;
    const minY = AREA.cy - AREA.h / 2 + 0.5;
    let x;
    let y;
    let tries = 0;
    do {
      x = minX + Math.floor(Math.random() * AREA.w);
      y = minY + Math.floor(Math.random() * AREA.h);
    } while (used.has(`${x},${y}`) && ++tries < 500);
    const hue = Math.random();
    const main = new THREE.Color().setHSL(hue, 0.85, 0.5);
    const side = new THREE.Color().setHSL(Math.random(), 0.9, 0.6);
    const mesh = makeSlime(main, { side });
    mesh.position.copy(toWorld(x, y));
    mesh.scale.setScalar(0.01);
    this.scene.add(mesh);
    this.pickups.push({ x, y, mesh, color: main, born: 0 });
  }

  turn(v) {
    // pick the main axis, then only allow a perpendicular turn
    const horizontal = Math.abs(v.x) > Math.abs(v.y);
    if (this.lastDir[1] !== 0) {
      if (horizontal) this.dir = v.x > 0 ? DIRS.right : DIRS.left;
    } else if (!horizontal) {
      this.dir = v.y > 0 ? DIRS.up : DIRS.down;
    }
    this.lastDir = this.dir;
  }

  update(dt, active) {
    for (const p of this.pickups) {
      p.born = Math.min(1, p.born + dt * 3);
      p.mesh.scale.setScalar(easeOutBack(p.born));
    }
    this.splashes.update(dt);
    if (!active) return;

    const v = this.ctx.input.vector;
    if (Math.hypot(v.x, v.y) > 0.4 && this.canMove) {
      this.turn(v);
      this.canMove = false;
    }

    this.progress += this.moveSpeed * dt;
    if (this.progress >= 1) {
      this.progress -= 1;
      this.cellStart = this.cellEnd;
      this.cellEnd = [this.cellStart[0] + this.lastDir[0], this.cellStart[1] + this.lastDir[1]];
      this.history.push(this.cellStart);
      for (const seg of this.pending) {
        const tail = this.segments.length ? this.segments[this.segments.length - 1].position.clone() : toWorld(...this.history[0]);
        seg.position.copy(tail);
        this.segments.push(seg);
      }
      this.pending = [];
      while (this.history.length > this.segments.length + 2) this.history.shift();
      this.canMove = true;
    }

    const t = Math.min(1, this.progress);
    const hx = this.cellStart[0] + (this.cellEnd[0] - this.cellStart[0]) * t;
    const hy = this.cellStart[1] + (this.cellEnd[1] - this.cellStart[1]) * t;
    toWorld(hx, hy, this.head.position);
    const targetYaw = Math.atan2(this.lastDir[0], -this.lastDir[1]);
    this.head.rotation.y = lerpAngle(this.head.rotation.y, targetYaw, Math.min(1, dt * 10));

    const h = this.history;
    const a = new THREE.Vector3();
    const b = new THREE.Vector3();
    this.segments.forEach((seg, i) => {
      const idx = h.length - i - 2;
      if (idx < 0) return;
      toWorld(...h[idx], a);
      toWorld(...h[idx + 1], b);
      seg.position.lerpVectors(a, b, t);
    });
    this.updateWave(dt);

    // pickups (Unity: trigger collider on the head)
    for (const p of [...this.pickups]) {
      if (Math.hypot(p.x - hx, p.y - hy) < 0.6) this.eat(p);
    }

    // walls
    if (Math.abs(hx - AREA.cx) > AREA.w / 2 || Math.abs(hy - AREA.cy) > AREA.h / 2) return this.crash();
    // own tail
    for (const seg of this.segments) {
      if (seg.position.distanceTo(this.head.position) < 0.5) return this.crash();
    }
  }

  eat(p) {
    this.pickups.splice(this.pickups.indexOf(p), 1);
    this.ctx.addScore(10);
    this.moveSpeed += 0.01;
    this.splashes.burst(p.mesh.position.clone(), p.color, 14, 3);
    this.ctx.audio.play("grow");
    this.ctx.flash(0x37c447);
    p.mesh.scale.setScalar(0.9);
    this.pending.push(p.mesh);
    this.wave = { active: true, t: 0 };
    this.spawnSlime();
  }

  updateWave(dt) {
    if (!this.wave.active) return;
    const count = this.segments.length;
    const duration = 0.5 + count * 0.05;
    const speed = Math.max(1, count) / duration;
    const width = 2;
    this.segments.forEach((seg, i) => {
      const d = Math.abs(i - this.wave.t);
      const influence = Math.exp(-(d * d) / (width * width));
      seg.scale.setScalar(THREE.MathUtils.lerp(1, 1.3, influence) * 0.9);
    });
    const hi = Math.exp(-(this.wave.t ** 2) / (width * width));
    this.head.scale.setScalar(THREE.MathUtils.lerp(1, 1.3, hi));
    this.wave.t += dt * speed;
    if (this.wave.t > count + width) {
      this.wave.active = false;
      this.head.scale.setScalar(1);
      this.segments.forEach((s) => s.scale.setScalar(0.9));
    }
  }

  crash() {
    this.splashes.burst(this.head.position.clone().add(new THREE.Vector3(0, 1, 0)), 0x7b3fe4, 24, 4);
    this.moveSpeed = 0;
    this.ctx.audio.play("crash");
    this.ctx.gameOver();
  }
}

function lerpAngle(a, b, t) {
  let d = ((b - a + Math.PI) % (Math.PI * 2)) - Math.PI;
  if (d < -Math.PI) d += Math.PI * 2;
  return a + d * t;
}

function easeOutBack(t) {
  const c1 = 1.70158;
  const c3 = c1 + 1;
  return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
}
