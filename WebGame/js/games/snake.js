// Snake (Unity: SnakeGame.cs, SnakeCollider.cs, Snake.unity), made easier for puck tilt.
// The slime no longer moves cell by cell: it glides freely and turns smoothly towards
// where the puck tilts. Touch other slimes to add them to your tail (+10, a bit faster).
// The walls are soft: the slime bounces back onto the field. Only running into your
// own tail ends the game, and the first few tail pieces behind the head never count.
import * as THREE from "three";
import { makeSlime, Splashes } from "../slime.js";

const AREA = { w: 22, h: 12, cx: 0.5, cy: 0.5 }; // Unity PlayAreaSize, centred at (0.5, 0.5)
const SPACING = 0.8; // distance between tail pieces
const TURN = 2.6; // rad/s, how fast the head turns towards the tilt
const EAT_R = 1.05; // generous pickup reach
const HIT_R = 0.42; // own-tail hit distance (the slimes are ~0.9 wide)
const SAFE_SEGMENTS = 4; // tail pieces right behind the head never hit you

// field (x, y) with y up the screen -> world (x, 0, -y)
const toWorld = (x, y, out = new THREE.Vector3()) => out.set(x, 0.5, -y);

export class SnakeGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "snake";
    this.background = "snake";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(40, 16 / 9, 0.1, 200);
    this.moveSpeed = { easy: 1.8, medium: 2.3, hard: 2.9 }[ctx.difficulty]; // units per second
    this.maxSpeed = this.moveSpeed + 1.2;
    this.angle = Math.PI / 2; // heading up the screen
    this.segments = [];
    this.trail = [];
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
    this.x = AREA.cx;
    this.y = AREA.cy - 3;
    this.trail = [{ x: this.x, y: this.y }];
    const count = { easy: 4, medium: 3, hard: 2 }[this.ctx.difficulty];
    for (let i = 0; i < count; i++) this.spawnSlime();
  }

  spawnSlime() {
    const halfW = AREA.w / 2 - 1;
    const halfH = AREA.h / 2 - 1;
    let x;
    let y;
    let tries = 0;
    // keep new slimes away from the head and the tail so they are easy to reach
    do {
      x = AREA.cx + (Math.random() * 2 - 1) * halfW;
      y = AREA.cy + (Math.random() * 2 - 1) * halfH;
    } while (this.blocked(x, y) && ++tries < 200);
    const hue = Math.random();
    const main = new THREE.Color().setHSL(hue, 0.85, 0.5);
    const side = new THREE.Color().setHSL(Math.random(), 0.9, 0.6);
    const mesh = makeSlime(main, { side });
    mesh.position.copy(toWorld(x, y));
    mesh.scale.setScalar(0.01);
    this.scene.add(mesh);
    this.pickups.push({ x, y, mesh, color: main, born: 0 });
  }

  blocked(x, y) {
    if (Math.hypot(x - this.x, y - this.y) < 3) return true;
    for (const p of this.pickups) if (Math.hypot(x - p.x, y - p.y) < 2) return true;
    for (const seg of this.segments) if (Math.hypot(x - seg.position.x, -y - seg.position.z) < 1.5) return true;
    return false;
  }

  steer(dt) {
    const v = this.ctx.input.vector;
    if (Math.hypot(v.x, v.y) < 0.3) return; // puck level: keep going straight
    const target = Math.atan2(v.y, v.x);
    const diff = Math.atan2(Math.sin(target - this.angle), Math.cos(target - this.angle));
    this.angle += THREE.MathUtils.clamp(diff, -TURN * dt, TURN * dt);
  }

  // Soft walls: reflect the heading and stay on the field.
  bounce() {
    const halfW = AREA.w / 2 - 0.45;
    const halfH = AREA.h / 2 - 0.45;
    let hit = false;
    if (Math.abs(this.x - AREA.cx) > halfW) {
      this.x = AREA.cx + Math.sign(this.x - AREA.cx) * halfW;
      this.angle = Math.PI - this.angle;
      hit = true;
    }
    if (Math.abs(this.y - AREA.cy) > halfH) {
      this.y = AREA.cy + Math.sign(this.y - AREA.cy) * halfH;
      this.angle = -this.angle;
      hit = true;
    }
    if (hit) {
      this.ctx.audio.play("splat");
      this.splashes.burst(this.head.position.clone(), 0x7b3fe4, 6, 2);
    }
  }

  update(dt, active) {
    for (const p of this.pickups) {
      p.born = Math.min(1, p.born + dt * 3);
      p.mesh.scale.setScalar(easeOutBack(p.born));
    }
    this.splashes.update(dt);
    if (!active || this.dead) return;

    this.steer(dt);
    this.x += Math.cos(this.angle) * this.moveSpeed * dt;
    this.y += Math.sin(this.angle) * this.moveSpeed * dt;
    this.bounce();
    this.trail.unshift({ x: this.x, y: this.y });

    toWorld(this.x, this.y, this.head.position);
    this.head.rotation.y = lerpAngle(this.head.rotation.y, Math.atan2(Math.cos(this.angle), Math.sin(this.angle)), Math.min(1, dt * 12));

    this.placeTail();
    this.updateWave(dt);

    // pickups (Unity: trigger collider on the head)
    for (const p of [...this.pickups]) {
      if (Math.hypot(p.x - this.x, p.y - this.y) < EAT_R) this.eat(p);
    }

    // own tail
    for (let i = SAFE_SEGMENTS; i < this.segments.length; i++) {
      const seg = this.segments[i].position;
      if (Math.hypot(seg.x - this.x, -seg.z - this.y) < HIT_R) return this.crash();
    }
  }

  // Each tail piece sits SPACING further back along the path the head took.
  placeTail() {
    const tr = this.trail;
    let seg = 0;
    let dist = 0;
    let want = SPACING;
    for (let i = 1; i < tr.length && seg < this.segments.length; i++) {
      const a = tr[i - 1];
      const b = tr[i];
      const d = Math.hypot(b.x - a.x, b.y - a.y);
      while (seg < this.segments.length && dist + d >= want) {
        const t = d > 0 ? (want - dist) / d : 0;
        const target = toWorld(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        const mesh = this.segments[seg];
        // a new piece slides in from where it was picked up
        if (mesh.userData.joining > 0) {
          mesh.userData.joining = Math.max(0, mesh.userData.joining - 1 / 30);
          mesh.position.lerp(target, 0.15);
        } else mesh.position.copy(target);
        seg++;
        want += SPACING;
      }
      dist += d;
      if (seg >= this.segments.length) {
        tr.length = Math.min(tr.length, i + 2);
        break;
      }
    }
    // keep enough path for the whole tail plus a bit
    const maxPoints = 60 + this.segments.length * 40;
    if (tr.length > maxPoints) tr.length = maxPoints;
  }

  eat(p) {
    this.pickups.splice(this.pickups.indexOf(p), 1);
    this.ctx.addScore(10);
    this.moveSpeed = Math.min(this.maxSpeed, this.moveSpeed + 0.04);
    this.splashes.burst(p.mesh.position.clone(), p.color, 14, 3);
    this.ctx.audio.play("grow");
    this.ctx.flash(0x37c447);
    p.mesh.scale.setScalar(0.9);
    p.mesh.userData.joining = 1;
    this.segments.push(p.mesh);
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
    this.dead = true;
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
