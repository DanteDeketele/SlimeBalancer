// Slime Ski (Unity: SkiGame.cs, Skie.unity).
// Ski down an endless slope. Steer left and right through the flag gates (+10).
// Every 50 points the slope gets faster. Hit a tree or a snowman and it's over.
import * as THREE from "three";
import { loadModel, loadTexture, fitHeight, flattenModel } from "../assets.js";

const SPAWN_AHEAD = 120; // Unity SpawnAheadDistance
const GATE = 20; // a flag gate every 20 m
const TREE_RADIUS = 2.06; // Unity Tree prefab SphereCollider
const SNOWMAN_RADIUS = 0.56; // Unity "Tree 1" (snowman) prefab SphereCollider
const PLAYER_RADIUS = 0.5;
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
    this.speed = { easy: 3, medium: 5, hard: 7 }[ctx.difficulty];
    this.playerX = 0;
    this.travelled = 0;
    this.flagX = 0;
    this.flagDistance = 0;
    this.flags = [];
    this.obstacles = [];
    this.camPitch = 12;
    this.camRoll = 0;
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
    snow.repeat.set(60, 60);
    this.snow = snow;
    const floor = new THREE.Mesh(
      new THREE.PlaneGeometry(600, 600),
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
    const skiMat = new THREE.MeshStandardMaterial({ color: 0xc0262d, roughness: 0.45 });
    for (const side of [-1, 1]) {
      const ski = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.05, 1.6), skiMat);
      ski.position.set(side * 0.26, -1.25, -1.35);
      ski.rotation.x = 0.12;
      this.camera.add(ski);
    }
    s.add(this.camera);
    this.preSpawn();
    this.placeCamera(0);
  }

  preSpawn() {
    const segments = Math.floor(SPAWN_AHEAD / GATE);
    for (let i = 1; i <= segments; i++) this.spawnSegment(i * GATE, true);
    this.flagDistance = (segments + 1) * GATE;
  }

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

  placeCamera(dt) {
    const v = this.ctx.input.vector;
    const k = Math.min(1, dt * 2);
    this.camPitch += (12 + v.y * 9 - this.camPitch) * k;
    this.camRoll += (-v.x * 9 - this.camRoll) * k;
    this.camera.position.set(this.playerX, 1.7, 0.6);
    this.camera.rotation.set(-THREE.MathUtils.degToRad(this.camPitch), 0, THREE.MathUtils.degToRad(this.camRoll), "YXZ");
  }

  update(dt, active) {
    if (active) this.step(dt);
    this.placeCamera(dt);
    this.drawTrees();
  }

  step(dt) {
    const input = this.ctx.input.vector;
    this.playerX += input.x * dt * 5;
    this.speed += dt * 0.01;
    const move = this.speed * dt;
    this.travelled += move;

    // scroll the snow and keep the backdrop with the player
    this.snow.offset.y = (this.travelled / 10) % 1;
    this.floor.position.x = this.playerX;
    this.snow.offset.x = (this.playerX / 10) % 1;
    this.backdrop.position.x = this.playerX;

    for (let i = this.obstacles.length - 1; i >= 0; i--) {
      const ob = this.obstacles[i];
      ob.z += move;
      if (ob.mesh) ob.mesh.position.z = ob.z;
      if (ob.z > 0) {
        if (Math.abs(ob.x - this.playerX) < ob.radius + PLAYER_RADIUS) {
          this.ctx.audio.play("crash");
          this.ctx.gameOver();
        }
        if (ob.mesh) this.scene.remove(ob.mesh);
        this.obstacles.splice(i, 1);
      }
    }

    while (this.travelled > this.flagDistance - SPAWN_AHEAD) {
      this.flagDistance += GATE;
      this.spawnSegment(SPAWN_AHEAD, false);
    }

    for (let i = this.flags.length - 1; i >= 0; i--) {
      const flag = this.flags[i];
      flag.position.z += move;
      if (flag.position.z > 0) {
        if (Math.abs(flag.position.x - this.playerX) < 3) {
          this.ctx.addScore(10);
          this.ctx.audio.play("ui");
          this.ctx.flash(0x30d596);
          if (this.ctx.score % 50 === 0) this.speed += 1;
        }
        this.scene.remove(flag);
        this.flags.splice(i, 1);
      }
    }
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
