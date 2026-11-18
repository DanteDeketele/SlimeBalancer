// Balancequest (Unity: Balancequest.cs, BalanceQuest.unity).
// One slime drops onto a 10x10 board by a moonlit lake. Keep it on the board:
// every 5 seconds it stays on gives +10. If it falls off, the game is over.
// Difficulty picks the slime: small (easy), medium or big (hard).
import * as THREE from "three";
import { makeSlime, Splashes } from "../slime.js";
import { Ball, BoardWorld, smoothRotation, tiltTarget } from "../physics.js";
import { loadTexture } from "../assets.js";

export class BalanceGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "balance";
    this.background = "nature";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(22, 16 / 9, 0.1, 300); // Unity FOV 22
    // Unity camera: (0, 9.87, -30.8), pitched 13.7 degrees down.
    this.camera.position.set(0, 9.87, 30.8);
    this.camera.lookAt(0, 9.87 - 30.8 * Math.tan(THREE.MathUtils.degToRad(13.7)), 0);
    this.size = { easy: 1, medium: 2, hard: 3 }[ctx.difficulty];
    this.timer = 0;
    this.delay = 5;
  }

  async load() {
    const s = this.scene;
    s.add(new THREE.HemisphereLight(0x9cc8ff, 0x0b1a2a, 1.1));
    const moon = new THREE.DirectionalLight(0xdfeaff, 2.2);
    moon.position.set(8, 25, 10);
    moon.castShadow = true;
    moon.shadow.mapSize.set(1024, 1024);
    Object.assign(moon.shadow.camera, { left: -10, right: 10, top: 10, bottom: -10, near: 1, far: 60 });
    s.add(moon);
    const rim = new THREE.PointLight(0x30d596, 40, 40);
    rim.position.set(-6, 6, 6);
    s.add(rim);

    const wood = await loadTexture("assets/img/wood.jpg");
    wood.colorSpace = THREE.SRGBColorSpace;
    this.board = new THREE.Group();
    const plank = new THREE.Mesh(
      new THREE.BoxGeometry(10, 0.2, 10),
      new THREE.MeshStandardMaterial({ map: wood, roughness: 0.8, color: 0xc9b6a8 })
    );
    plank.receiveShadow = true;
    plank.castShadow = true;
    this.board.add(plank);
    s.add(this.board);
    this.world = new BoardWorld(this.board, 5, 0.1, 5);
    this.splashes = new Splashes(s);
    this.fireflies = makeFireflies(s);
  }

  start() {
    const mesh = makeSlime(0x7fd6ff, { side: 0xd8fbff, eyes: true });
    mesh.scale.setScalar(this.size);
    const ball = new Ball(new THREE.Vector3(0, 10, 0), 0.5 * this.size);
    ball.mesh = mesh;
    mesh.position.copy(ball.pos);
    this.scene.add(mesh);
    this.world.balls.push(ball);
    this.slime = ball;
  }

  update(dt, active) {
    smoothRotation(this.board, tiltTarget(active ? this.ctx.input.vector : { x: 0, y: 0 }, 18), dt);
    this.world.step(dt);
    const b = this.slime;
    if (b) {
      b.mesh.position.copy(b.pos);
      // The slime keeps facing the camera, it slides and wobbles instead of rolling.
      const lean = new THREE.Euler(b.vel.z * 0.04, 0, -b.vel.x * 0.04);
      b.mesh.rotation.x += (lean.x - b.mesh.rotation.x) * Math.min(1, dt * 6);
      b.mesh.rotation.z += (lean.z - b.mesh.rotation.z) * Math.min(1, dt * 6);
      if (b.touched && !b.splashed) {
        b.splashed = true;
        this.splashes.burst(b.pos.clone(), 0x7fd6ff, 16, 3);
        this.ctx.audio.play("splat");
      }
      if (active && b.pos.y < -10) {
        this.ctx.gameOver();
      }
    }
    if (active) {
      this.timer += dt;
      if (this.timer >= this.delay) {
        this.timer = 0;
        this.ctx.addScore(10);
        this.ctx.audio.play("score");
        this.ctx.flash(0x30d596);
      }
    }
    this.splashes.update(dt);
    this.fireflies(dt);
  }
}

function makeFireflies(scene) {
  const count = 90;
  const geo = new THREE.BufferGeometry();
  const pos = new Float32Array(count * 3);
  const seeds = [];
  for (let i = 0; i < count; i++) {
    seeds.push([(Math.random() - 0.5) * 60, Math.random() * 25 - 5, -Math.random() * 40 + 5, Math.random() * 10]);
  }
  geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
  const sprite = document.createElement("canvas");
  sprite.width = sprite.height = 64;
  const g = sprite.getContext("2d");
  const grad = g.createRadialGradient(32, 32, 0, 32, 32, 32);
  grad.addColorStop(0, "rgba(255,240,170,1)");
  grad.addColorStop(0.3, "rgba(255,210,120,0.6)");
  grad.addColorStop(1, "rgba(255,200,100,0)");
  g.fillStyle = grad;
  g.fillRect(0, 0, 64, 64);
  const mat = new THREE.PointsMaterial({
    size: 0.9,
    map: new THREE.CanvasTexture(sprite),
    transparent: true,
    depthWrite: false,
    blending: THREE.AdditiveBlending,
  });
  scene.add(new THREE.Points(geo, mat));
  let t = 0;
  return (dt) => {
    t += dt;
    seeds.forEach(([x, y, z, s], i) => {
      pos[i * 3] = x + Math.sin(t * 0.3 + s) * 2;
      pos[i * 3 + 1] = y + Math.sin(t * 0.5 + s * 2) * 1.5;
      pos[i * 3 + 2] = z + Math.cos(t * 0.25 + s) * 2;
    });
    geo.attributes.position.needsUpdate = true;
  };
}
