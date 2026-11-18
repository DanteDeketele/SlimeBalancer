// Tilt It (Unity: TiltGame.cs, PipeTrigger.cs, PlayerTrigger.cs, Tilt_It.unity).
// Coloured slimes drop onto a 6x6 wooden board. Tilt the board so each slime rolls
// off into the pipe of its own colour. Right colour: +10. Wrong pipe: game over.
import * as THREE from "three";
import { makeSlime, Splashes } from "../slime.js";
import { Ball, BoardWorld, smoothRotation, tiltTarget } from "../physics.js";
import { loadTexture } from "../assets.js";

// Unity enum order: Green, Blue, Red, Yellow
const COLORS = {
  green: { main: 0x37c447, side: 0xb5ff6b },
  blue: { main: 0x2a9df4, side: 0x7ff3ff },
  red: { main: 0xe53935, side: 0xff9e6b },
  yellow: { main: 0xf4d21f, side: 0xfff6a8 },
};
// Unity positions (x, z) mapped to three.js (x, -z): right green, left blue,
// far red, near yellow, all at y = -5.47.
const PIPES = [
  { color: "green", x: 4.5, z: 0 },
  { color: "blue", x: -4.5, z: 0 },
  { color: "red", x: 0, z: -4.5 },
  { color: "yellow", x: 0, z: 4.5 },
];
const PIPE_Y = -5.47;
const PIPE_INNER = 2.05;
const PIPE_OUTER = 2.55;

export class TiltGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "tilt";
    this.background = "city";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(22, 16 / 9, 0.1, 200); // Unity FOV 22
    // Unity camera: (0, 34.1, -13.5), pitched 69.6 degrees down.
    this.camera.position.set(0, 34.1, 13.5);
    this.camera.lookAt(0, 0, 0.8);
    this.delay = { easy: 7, medium: 5, hard: 3 }[ctx.difficulty];
    this.timer = 0;
    this.slimes = [];
  }

  async load() {
    const s = this.scene;
    s.add(new THREE.HemisphereLight(0xcfe6ff, 0x203040, 1.2));
    const sun = new THREE.DirectionalLight(0xffffff, 1.6);
    sun.position.set(-4, 20, 8);
    sun.castShadow = true;
    sun.shadow.mapSize.set(1024, 1024);
    Object.assign(sun.shadow.camera, { left: -8, right: 8, top: 8, bottom: -8, near: 1, far: 50 });
    s.add(sun);
    const glow = new THREE.PointLight(0x9fd8ff, 30, 30);
    glow.position.set(0.35, 3.37, 0.35);
    s.add(glow);

    const wood = await loadTexture("assets/img/wood.jpg");
    wood.colorSpace = THREE.SRGBColorSpace;
    this.board = new THREE.Group();
    const plank = new THREE.Mesh(
      new THREE.BoxGeometry(6, 0.2, 6),
      new THREE.MeshStandardMaterial({ map: wood, roughness: 0.75 })
    );
    plank.receiveShadow = true;
    plank.castShadow = true;
    this.board.add(plank);
    s.add(this.board);
    this.world = new BoardWorld(this.board, 3, 0.1, 3);

    for (const p of PIPES) s.add(makePipe(COLORS[p.color].main, p.x, p.z));
    this.splashes = new Splashes(s);
    this.confetti = makeConfetti(s);
  }

  start() {
    this.spawn();
  }

  spawn() {
    const keys = Object.keys(COLORS);
    const color = keys[Math.floor(Math.random() * keys.length)];
    const mesh = makeSlime(COLORS[color].main, { side: COLORS[color].side });
    const pos = new THREE.Vector3((Math.random() * 2 - 1), 12, (Math.random() * 2 - 1));
    const ball = new Ball(pos, 0.5);
    ball.color = color;
    ball.mesh = mesh;
    ball.age = 0;
    mesh.position.copy(pos);
    this.scene.add(mesh);
    this.world.balls.push(ball);
    this.ctx.flash(COLORS[color].main);
  }

  remove(ball) {
    this.scene.remove(ball.mesh);
    const i = this.world.balls.indexOf(ball);
    if (i >= 0) this.world.balls.splice(i, 1);
  }

  update(dt, active) {
    const input = this.ctx.input.vector;
    // Unity multiplies the tilt by 1.5 here: up to 27 degrees.
    smoothRotation(this.board, tiltTarget(active ? input : { x: 0, y: 0 }, 18 * 1.5), dt);
    this.world.step(dt);

    for (const ball of [...this.world.balls]) {
      ball.age += dt;
      ball.mesh.position.copy(ball.pos);
      if (ball.touched && !ball.splashed) {
        ball.splashed = true;
        this.splashes.burst(ball.pos.clone(), COLORS[ball.color].main, 10, 2.5);
        this.ctx.audio.play("splat");
      }
      // rolling look
      const v = ball.vel;
      ball.mesh.rotation.x += v.z * dt * 1.5;
      ball.mesh.rotation.z -= v.x * dt * 1.5;
      if (!ball.scored && ball.pos.y < PIPE_Y + 1.2) {
        for (const p of PIPES) {
          if (Math.hypot(ball.pos.x - p.x, ball.pos.z - p.z) < PIPE_INNER) {
            ball.scored = true;
            if (!active) break;
            if (p.color === ball.color) this.correct(ball);
            else this.wrong(ball);
            break;
          }
        }
      }
      if (ball.age > 20 || ball.pos.y < -30) this.remove(ball);
    }

    if (active) {
      this.timer += dt;
      if (this.timer >= this.delay) {
        this.spawn();
        this.timer = 0;
      }
    }
    this.splashes.update(dt);
    this.confetti(dt);
  }

  correct(ball) {
    this.ctx.addScore(10);
    this.ctx.audio.play("score");
    this.splashes.burst(new THREE.Vector3(ball.pos.x, PIPE_Y + 1, ball.pos.z), COLORS[ball.color].main, 18, 4);
  }

  wrong() {
    this.ctx.gameOver();
  }
}

function makePipe(color, x, z) {
  const g = new THREE.Group();
  const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.35, metalness: 0.05, side: THREE.DoubleSide });
  const length = 14;
  const outer = new THREE.Mesh(new THREE.CylinderGeometry(PIPE_OUTER, PIPE_OUTER, length, 48, 1, true), mat);
  outer.position.y = -length / 2;
  g.add(outer);
  const inner = new THREE.Mesh(
    new THREE.CylinderGeometry(PIPE_INNER, PIPE_INNER, length, 48, 1, true),
    new THREE.MeshStandardMaterial({ color: new THREE.Color(color).multiplyScalar(0.35), roughness: 0.9, side: THREE.BackSide })
  );
  inner.position.y = -length / 2;
  g.add(inner);
  const rim = new THREE.Mesh(new THREE.TorusGeometry((PIPE_INNER + PIPE_OUTER) / 2, (PIPE_OUTER - PIPE_INNER) / 2 + 0.12, 16, 64), mat);
  rim.rotation.x = Math.PI / 2;
  g.add(rim);
  const hole = new THREE.Mesh(new THREE.CircleGeometry(PIPE_INNER, 48), new THREE.MeshBasicMaterial({ color: 0x000000 }));
  hole.rotation.x = -Math.PI / 2;
  hole.position.y = -1.6;
  g.add(hole);
  g.position.set(x, PIPE_Y + 0.6, z);
  return g;
}

// Floating glowing squares like the Unity particle system behind the board.
function makeConfetti(scene) {
  const count = 60;
  const mesh = new THREE.InstancedMesh(
    new THREE.PlaneGeometry(0.6, 0.6),
    new THREE.MeshBasicMaterial({ color: 0xffe08a, transparent: true, opacity: 0.75, side: THREE.DoubleSide }),
    count
  );
  const items = [];
  const m = new THREE.Matrix4();
  const palette = [0xffe08a, 0xffb3c7, 0x9ff0ff, 0xfff3c4];
  for (let i = 0; i < count; i++) {
    items.push({
      p: new THREE.Vector3((Math.random() - 0.5) * 50, -12 - Math.random() * 6, (Math.random() - 0.5) * 30),
      s: 0.5 + Math.random() * 0.8,
      r: Math.random() * Math.PI,
      v: 0.3 + Math.random() * 0.6,
    });
    mesh.setColorAt(i, new THREE.Color(palette[i % palette.length]));
  }
  scene.add(mesh);
  const q = new THREE.Quaternion();
  const e = new THREE.Euler();
  const sc = new THREE.Vector3();
  return (dt) => {
    items.forEach((it, i) => {
      it.p.z -= it.v * dt;
      it.r += dt * 0.5;
      if (it.p.z < -18) it.p.z = 15;
      q.setFromEuler(e.set(-Math.PI / 2 + Math.sin(it.r) * 0.6, it.r, 0));
      m.compose(it.p, q, sc.setScalar(it.s));
      mesh.setMatrixAt(i, m);
    });
    mesh.instanceMatrix.needsUpdate = true;
  };
}
