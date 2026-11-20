// Slime Ski (Unity: SkiGame.cs, Skie.unity).
// Ski down an endless slope. Steer left and right through the flag gates (+10).
// Every 50 points the slope gets faster. Hit a tree or a snowman and it's over.
// Lean the puck forward to ski faster, back to brake (each skier has their own speed).
//
// 2 players (race): both skiers on the same slope, split screen. A skier who hits
// something is out; the last one still skiing wins.
import * as THREE from "three";
import { loadModel, loadTexture, fitHeight, flattenModel } from "../assets.js";
import { makeSlime } from "../slime.js";

const SPAWN_AHEAD = 120; // Unity SpawnAheadDistance
const GATE = 20; // a flag gate every 20 m
const TREE_RADIUS = 2.06; // Unity Tree prefab SphereCollider
const SNOWMAN_RADIUS = 0.56; // Unity "Tree 1" (snowman) prefab SphereCollider
const PLAYER_RADIUS = 0.5;
// Forward/back lean scales the speed: full lean back = 45 %, full lean forward = 170 %.
const LEAN_SLOW = 0.55;
const LEAN_FAST = 0.7;
const MAX_TREES = 1400;
const POLE_GEO = new THREE.CylinderGeometry(0.06, 0.06, 2.2, 10);
const POLE_MAT = new THREE.MeshStandardMaterial({ color: 0x2b2e33, roughness: 0.5 });

export class SkiGame {
  constructor(ctx) {
    this.ctx = ctx;
    this.music = "ski";
    this.background = "ski";
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(60, 16 / 9, 0.1, 400);
    const speed = { easy: 3, medium: 5, hard: 7 }[ctx.difficulty];
    this.race = ctx.mode === "versus";
    const count = this.race ? 2 : 1;
    this.skiers = Array.from({ length: count }, (_, i) => ({
      index: i,
      x: this.race ? (i === 0 ? -1.4 : 1.4) : 0,
      alive: true,
      camera: i === 0 ? this.camera : new THREE.PerspectiveCamera(60, 16 / 9, 0.1, 400),
      pitch: 12,
      roll: 0,
      d: 0, // metres down the slope; the world stays put and the skiers move forward
      base: speed, // slope speed, rises over time and every 50 points
      speed,
      lean: 1,
    }));
    this.flagX = 0;
    this.nextGate = 0;
    this.flags = [];
    this.obstacles = [];
  }

  // centre of the skiers still going (obstacles spawn around it, like around the Unity player)
  get playerX() {
    const alive = this.skiers.filter((s) => s.alive);
    const list = alive.length ? alive : this.skiers;
    return list.reduce((a, s) => a + s.x, 0) / list.length;
  }

  resize(aspect) {
    for (const sk of this.skiers) {
      sk.camera.aspect = this.race ? aspect / 2 : aspect;
      sk.camera.updateProjectionMatrix();
    }
  }

  render(renderer) {
    if (!this.race) {
      this.follow(this.skiers[0]);
      return renderer.render(this.scene, this.camera);
    }
    const size = renderer.getSize(new THREE.Vector2());
    const half = Math.floor(size.x / 2);
    renderer.setScissorTest(true);
    this.skiers.forEach((sk, i) => {
      // floor and mountains follow whoever is looking
      this.follow(sk);
      renderer.setViewport(i * half, 0, half, size.y);
      renderer.setScissor(i * half, 0, half, size.y);
      renderer.render(this.scene, sk.camera);
    });
    renderer.setScissorTest(false);
    renderer.setViewport(0, 0, size.x, size.y);
  }

  async load() {
    const s = this.scene;
    const skyColor = new THREE.Color(0xdfe6ee);
    s.background = skyColor;
    s.fog = new THREE.Fog(skyColor, 25, 150);
    s.add(new THREE.HemisphereLight(0xffffff, 0xb8c4d6, 1.6));
    const sun = new THREE.DirectionalLight(0xffffff, 1.4);
    sun.position.set(-20, 30, 10);
    s.add(sun);

    const snow = await loadTexture("assets/img/snow.jpg");
    snow.colorSpace = THREE.SRGBColorSpace;
    snow.wrapS = snow.wrapT = THREE.RepeatWrapping;
    snow.repeat.set(200, 60);
    this.snow = snow;
    const floor = new THREE.Mesh(
      new THREE.PlaneGeometry(2000, 600),
      new THREE.MeshStandardMaterial({ map: snow, color: new THREE.Color(1.6, 1.6, 1.65), roughness: 1 })
    );
    floor.rotation.x = -Math.PI / 2;
    this.floor = floor;
    s.add(floor);

    // Far backdrop: the mountain and a strip of forest, as flat cut-outs.
    const [mountainTex, treesTex] = await Promise.all([loadTexture("assets/img/mountain.png"), loadTexture("assets/img/trees.png")]);
    mountainTex.colorSpace = treesTex.colorSpace = THREE.SRGBColorSpace;
    const backdrop = new THREE.Group();
    const mountain = new THREE.Mesh(
      new THREE.PlaneGeometry(520, 237),
      new THREE.MeshBasicMaterial({ map: mountainTex, transparent: true, fog: false, color: 0xd6dce6 })
    );
    mountain.position.set(0, 60, -330);
    backdrop.add(mountain);
    s.add(backdrop);
    this.backdrop = backdrop;
    this.treeStripTex = treesTex;

    // Trees: the Unity tree model, drawn instanced. Falls back to cones.
    let treeParts = null;
    const tree = await loadModel("assets/models/tree1.fbx");
    if (tree) {
      fitHeight(tree, 6.5);
      treeParts = flattenModel(tree).map(({ geometry, material }) => {
        const name = (material?.name || "").toLowerCase();
        const trunk = /trunk|bark|wood|stam|brown/.test(name);
        return { geometry, material: new THREE.MeshStandardMaterial({ color: trunk ? 0x6b4a2b : 0x2f8f3a, roughness: 0.9, flatShading: true }) };
      });
    }
    if (!treeParts || !treeParts.length) treeParts = proceduralTree();
    this.treeMeshes = treeParts.map(({ geometry, material }) => {
      const m = new THREE.InstancedMesh(geometry, material, MAX_TREES);
      m.count = 0;
      m.frustumCulled = false;
      s.add(m);
      return m;
    });

    const carrot = await loadModel("assets/models/carrot.fbx");
    this.carrotTemplate = carrot;

    // Skis, fixed to the camera like in the Unity first-person view.
    for (const sk of this.skiers) {
      const skiMat = new THREE.MeshStandardMaterial({ color: this.race ? sk.color ?? this.ctx.playerColor(sk.index) : 0xc0262d, roughness: 0.45 });
      for (const side of [-1, 1]) {
        const ski = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.05, 1.6), skiMat);
        ski.position.set(side * 0.26, -1.25, -1.35);
        ski.rotation.x = 0.12;
        sk.camera.add(ski);
      }
      s.add(sk.camera);
      if (this.race) {
        // The other player sees you as a slime on skis. Each camera hides its own.
        const color = this.ctx.playerColor(sk.index);
        const avatar = new THREE.Group();
        const slime = makeSlime(color, { eyes: true });
        slime.position.y = 0.6;
        slime.rotation.y = Math.PI;
        avatar.add(slime);
        for (const side of [-1, 1]) {
          const ski = new THREE.Mesh(new THREE.BoxGeometry(0.14, 0.05, 1.5), skiMat);
          ski.position.set(side * 0.2, 0.05, 0);
          avatar.add(ski);
        }
        avatar.traverse((o) => o.layers.set(sk.index + 1));
        s.add(avatar);
        sk.avatar = avatar;
      }
    }
    if (this.race) {
      this.skiers.forEach((sk) => {
        sk.camera.layers.enableAll();
        sk.camera.layers.disable(sk.index + 1);
      });
    }
    this.preSpawn();
    this.placeCamera(0);
  }

  // Snow floor and far backdrop travel with whoever is looking.
  follow(sk) {
    this.backdrop.position.set(sk.x, 0, -sk.d);
    this.floor.position.z = -sk.d;
    this.snow.offset.y = (sk.d / 10) % 1;
  }

  get front() {
    return Math.max(...this.skiers.map((s) => s.d));
  }

  get back() {
    return Math.min(...this.skiers.map((s) => s.d));
  }

  preSpawn() {
    const segments = Math.floor(SPAWN_AHEAD / GATE);
    for (let i = 1; i <= segments; i++) this.spawnSegment(i * GATE, true);
    this.nextGate = (segments + 1) * GATE;
  }

  // distanceAhead: metres from the start of the slope
  spawnSegment(distanceAhead, pre) {
    const flagX = this.flagX + (Math.random() * 20 - 10);
    this.flagX = flagX;
    this.flags.push(this.makeGate(flagX, -distanceAhead));
    let snowmen = 0;
    const attempts = 5 + Math.floor(Math.random() * 95);
    for (let j = 0; j < attempts; j++) {
      const x = Math.floor(Math.random() * 120) - 60 + this.playerX;
      const snowman = Math.random() < 0.5;
      if (snowman) {
        if (snowmen >= 3) continue;
        snowmen++;
      }
      if (Math.abs(flagX - x) > 13 || snowman) {
        const z = -(distanceAhead + Math.random() * 20);
        const ob = { x, z, snowman, rot: pre ? Math.random() * Math.PI * 2 : 0, radius: snowman ? SNOWMAN_RADIUS : TREE_RADIUS, scale: 0.85 + Math.random() * 0.35 };
        if (snowman) {
          ob.mesh = this.makeSnowman();
          ob.mesh.position.set(x, 0, z);
          ob.mesh.rotation.y = ob.rot + Math.PI;
          this.scene.add(ob.mesh);
        }
        this.obstacles.push(ob);
      }
    }
  }

  makeGate(x, z) {
    const g = new THREE.Group();
    const flagMat = new THREE.MeshStandardMaterial({ color: 0x30d5c8, roughness: 0.5, side: THREE.DoubleSide });
    for (const side of [-1, 1]) {
      const pole = new THREE.Mesh(POLE_GEO, POLE_MAT);
      pole.position.y = 1.1;
      const holder = new THREE.Group();
      holder.add(pole);
      const flag = new THREE.Mesh(new THREE.PlaneGeometry(0.8, 0.55), flagMat);
      flag.position.set(-side * 0.42, 1.85, 0);
      holder.add(flag);
      holder.position.x = side * 3;
      g.add(holder);
    }
    g.position.set(x, 0, z);
    this.scene.add(g);
    return g;
  }

  makeSnowman() {
    const g = new THREE.Group();
    const white = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.9 });
    const coal = new THREE.MeshStandardMaterial({ color: 0x1c1c1c, roughness: 0.6 });
    const sizes = [[0.6, 0.55], [0.42, 1.38], [0.3, 1.95]];
    for (const [r, y] of sizes) {
      const ball = new THREE.Mesh(new THREE.SphereGeometry(r, 20, 14), white);
      ball.position.y = y;
      g.add(ball);
    }
    for (const side of [-1, 1]) {
      const eye = new THREE.Mesh(new THREE.SphereGeometry(0.04, 8, 6), coal);
      eye.position.set(side * 0.1, 2.02, 0.27);
      g.add(eye);
    }
    let nose;
    if (this.carrotTemplate) {
      nose = fitHeight(this.carrotTemplate.clone(), 0.3);
      nose.traverse((c) => {
        if (c.isMesh) c.material = new THREE.MeshStandardMaterial({ color: 0xff7a1a, roughness: 0.6 });
      });
      nose.rotation.x = Math.PI / 2;
      nose.position.set(0, 1.93, 0.27);
    } else {
      nose = new THREE.Mesh(new THREE.ConeGeometry(0.05, 0.28, 10), new THREE.MeshStandardMaterial({ color: 0xff7a1a }));
      nose.rotation.x = Math.PI / 2;
      nose.position.set(0, 1.93, 0.4);
    }
    g.add(nose);
    const hat = new THREE.Mesh(new THREE.CylinderGeometry(0.2, 0.2, 0.3, 16), coal);
    hat.position.y = 2.3;
    g.add(hat);
    const brim = new THREE.Mesh(new THREE.CylinderGeometry(0.32, 0.32, 0.04, 16), coal);
    brim.position.y = 2.16;
    g.add(brim);
    return g;
  }

  start() {}

  inputFor(sk) {
    return this.race ? this.ctx.input.players[sk.index] : this.ctx.input.vector;
  }

  placeCamera(dt) {
    for (const sk of this.skiers) {
      const v = sk.alive ? this.inputFor(sk) : { x: 0, y: 0 };
      const k = Math.min(1, dt * 2);
      sk.pitch += (12 + v.y * 9 - sk.pitch) * k;
      sk.roll += (-v.x * 9 - sk.roll) * k;
      sk.camera.position.set(sk.x, 1.7, 0.6 - sk.d);
      // a slightly wider view when going fast
      const fov = 60 + (sk.lean - 1) * 12;
      if (Math.abs(sk.camera.fov - fov) > 0.05) {
        sk.camera.fov = fov;
        sk.camera.updateProjectionMatrix();
      }
      sk.camera.rotation.set(-THREE.MathUtils.degToRad(sk.pitch), 0, THREE.MathUtils.degToRad(sk.roll), "YXZ");
      if (sk.avatar) {
        sk.avatar.position.set(sk.x, 0, 0.6 - sk.d);
        sk.avatar.rotation.z = THREE.MathUtils.degToRad(sk.roll) * 0.6;
        sk.avatar.visible = sk.alive;
      }
    }
  }

  update(dt, active) {
    if (active) this.step(dt);
    this.placeCamera(dt);
    this.drawTrees();
  }

  step(dt) {
    for (const sk of this.skiers) {
      if (!sk.alive) continue;
      const v = this.inputFor(sk);
      sk.x += v.x * dt * 5;
      sk.base += dt * 0.01;
      const lean = v.y >= 0 ? 1 + v.y * LEAN_FAST : 1 + v.y * LEAN_SLOW;
      sk.lean += (lean - sk.lean) * Math.min(1, dt * 3);
      sk.speed = sk.base * sk.lean;
      const prev = sk.d;
      sk.d += sk.speed * dt;
      this.pass(sk, prev);
    }

    while (this.front > this.nextGate - SPAWN_AHEAD) {
      this.spawnSegment(this.nextGate, false);
      this.nextGate += GATE;
    }

    // drop what every skier has left behind
    const behind = this.back - 2;
    for (let i = this.obstacles.length - 1; i >= 0; i--) {
      const ob = this.obstacles[i];
      if (-ob.z < behind) {
        if (ob.mesh) this.scene.remove(ob.mesh);
        this.obstacles.splice(i, 1);
      }
    }
    for (let i = this.flags.length - 1; i >= 0; i--) {
      if (-this.flags[i].position.z < behind) {
        this.scene.remove(this.flags[i]);
        this.flags.splice(i, 1);
      }
    }

    if (this.crashedThisStep) this.checkEnd();
  }

  // Everything this skier went past since the last frame: obstacles and gates.
  pass(sk, prev) {
    const crossed = (z) => -z > prev && -z <= sk.d;
    for (const ob of this.obstacles) {
      if (crossed(ob.z) && Math.abs(ob.x - sk.x) < ob.radius + PLAYER_RADIUS) {
        this.crash(sk);
        return;
      }
    }
    for (const flag of this.flags) {
      if (!crossed(flag.position.z) || Math.abs(flag.position.x - sk.x) >= 3) continue;
      this.ctx.addScore(10, this.race ? sk.index : null);
      this.ctx.audio.play("ui");
      this.ctx.flash(this.race ? this.ctx.playerColor(sk.index) : 0x30d596);
      const score = this.race ? this.ctx.scores[sk.index] : this.ctx.score;
      if (score % 50 === 0) sk.base += 1;
    }
  }

  crash(sk) {
    sk.alive = false;
    this.ctx.audio.play("crash");
    if (this.race) {
      this.ctx.playerOut(sk.index);
      this.ctx.flash(0xff3b3b);
    }
    this.crashedThisStep = true;
  }

  checkEnd() {
    this.crashedThisStep = false;
    const alive = this.skiers.filter((s) => s.alive);
    if (!this.race) return this.ctx.gameOver();
    if (alive.length > 1) return;
    let winner = alive.length === 1 ? alive[0].index : null;
    if (winner === null) {
      const [a, b] = this.ctx.scores;
      winner = a === b ? null : a > b ? 0 : 1;
    }
    // In a race the survivor keeps skiing for a moment, then the result shows.
    if (alive.length === 1 && !this.finishing) {
      this.finishing = true;
      setTimeout(() => this.ctx.gameOver({ winner }), 1500);
      return;
    }
    if (!this.finishing) this.ctx.gameOver({ winner });
  }

  drawTrees() {
    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion();
    const p = new THREE.Vector3();
    const s = new THREE.Vector3();
    const up = new THREE.Vector3(0, 1, 0);
    let n = 0;
    for (const ob of this.obstacles) {
      if (ob.snowman || n >= MAX_TREES) continue;
      q.setFromAxisAngle(up, ob.rot);
      m.compose(p.set(ob.x, 0, ob.z), q, s.setScalar(ob.scale));
      for (const mesh of this.treeMeshes) mesh.setMatrixAt(n, m);
      n++;
    }
    for (const mesh of this.treeMeshes) {
      mesh.count = n;
      mesh.instanceMatrix.needsUpdate = true;
    }
  }
}

function proceduralTree() {
  const parts = [];
  const trunk = new THREE.CylinderGeometry(0.18, 0.25, 1.2, 8);
  trunk.translate(0, 0.6, 0);
  parts.push({ geometry: trunk, material: new THREE.MeshStandardMaterial({ color: 0x6b4a2b, flatShading: true }) });
  const leaves = [];
  [[1.7, 2.2, 1.6], [1.35, 1.9, 3.0], [0.95, 1.6, 4.3]].forEach(([r, h, y]) => {
    const c = new THREE.ConeGeometry(r, h, 9);
    c.translate(0, y, 0);
    leaves.push(c);
  });
  for (const geometry of leaves) parts.push({ geometry, material: new THREE.MeshStandardMaterial({ color: 0x2f8f3a, flatShading: true }) });
  return parts;
}
