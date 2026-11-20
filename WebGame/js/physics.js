// A small rigid-body stand-in for the Unity physics used by Tilt It and Balancequest:
// spheres rolling on a tilting box. Gravity is Unity's 9.81, and a sphere that rolls
// without slipping accelerates at 5/7 of the slope's pull, so it feels like a ball.
import * as THREE from "three";

const G = 9.81;
const tmpM = new THREE.Matrix4();
const local = new THREE.Vector3();
const q = new THREE.Vector3();
const qWorld = new THREE.Vector3();
const qPrev = new THREE.Vector3();
const n = new THREE.Vector3();
const vs = new THREE.Vector3();
const rel = new THREE.Vector3();

export class Ball {
  constructor(pos, radius) {
    this.pos = pos.clone();
    this.vel = new THREE.Vector3();
    this.r = radius;
    this.normal = null; // contact normal last step, or null in the air
    this.touched = false; // ever touched the board
  }
}

// Board-tilt target in radians: "downhill" follows the input direction
// (x right, y away from the camera), as the board mirrors the puck.
export function tiltTarget(input, maxDeg) {
  const k = THREE.MathUtils.degToRad(maxDeg);
  return new THREE.Euler(-input.y * k, 0, -input.x * k, "XYZ");
}

// Unity lerped the board rotation by Time.deltaTime * 15 every FixedUpdate (50 Hz).
export function smoothRotation(object, targetEuler, dt) {
  const target = new THREE.Quaternion().setFromEuler(targetEuler);
  const t = 1 - Math.pow(1 - 0.3, dt / 0.02);
  object.quaternion.slerp(target, Math.min(1, t));
}

export class BoardWorld {
  // half extents of the board box, in its own (unscaled) frame
  constructor(board, hx, hy, hz) {
    this.board = board;
    this.h = new THREE.Vector3(hx, hy, hz);
    this.prevMatrix = new THREE.Matrix4();
    board.updateMatrixWorld(true);
    this.prevMatrix.copy(board.matrixWorld);
    this.balls = [];
    this.drag = 0.25; // rolling resistance per second; higher = slimes roll slower
  }

  step(dt, substeps = 4) {
    const board = this.board;
    board.updateMatrixWorld(true);
    const now = board.matrixWorld;
    const inv = tmpM.copy(now).invert();
    const h = dt / substeps;
    for (let s = 0; s < substeps; s++) {
      for (const b of this.balls) {
        // gravity, with the tangential part reduced for rolling contact
        const ax = 0;
        let ay = -G;
        let az = 0;
        let gx = ax;
        let gz = az;
        if (b.normal) {
          const dn = ay * b.normal.y;
          const anx = dn * b.normal.x;
          const any = dn * b.normal.y;
          const anz = dn * b.normal.z;
          gx = anx + (ax - anx) * (5 / 7);
          ay = any + (ay - any) * (5 / 7);
          gz = anz + (az - anz) * (5 / 7);
        }
        b.vel.x += gx * h;
        b.vel.y += ay * h;
        b.vel.z += gz * h;
        b.pos.addScaledVector(b.vel, h);
        b.normal = null;
        this.collide(b, now, inv, dt, h);
      }
      this.ballBall();
    }
    this.prevMatrix.copy(now);
  }

  collide(b, now, inv, frameDt, h) {
    local.copy(b.pos).applyMatrix4(inv);
    const hx = this.h.x;
    const hy = this.h.y;
    const hz = this.h.z;
    q.set(
      THREE.MathUtils.clamp(local.x, -hx, hx),
      THREE.MathUtils.clamp(local.y, -hy, hy),
      THREE.MathUtils.clamp(local.z, -hz, hz)
    );
    const inside = q.equals(local);
    if (inside) q.y = hy; // centre inside the box: push out through the top
    qWorld.copy(q).applyMatrix4(now);
    n.copy(b.pos).sub(qWorld);
    let d = n.length();
    if (inside) {
      n.set(0, 1, 0).transformDirection(now);
      d = -d;
    } else if (d < 1e-6) {
      return;
    } else {
      n.divideScalar(d);
    }
    if (d >= b.r) return;
    b.pos.addScaledVector(n, b.r - d);
    // velocity of the board surface at the contact point
    qPrev.copy(q).applyMatrix4(this.prevMatrix);
    vs.copy(qWorld).sub(qPrev).divideScalar(Math.max(frameDt, 1e-4));
    rel.copy(b.vel).sub(vs);
    const vn = rel.dot(n);
    if (vn < 0) b.vel.addScaledVector(n, -vn); // bounciness 0
    // rolling resistance, a little drag along the surface
    rel.copy(b.vel).sub(vs);
    const vnn = rel.dot(n);
    rel.addScaledVector(n, -vnn);
    b.vel.addScaledVector(rel, -Math.min(1, this.drag * h));
    if (n.y > 0.3) b.normal = n.clone();
    b.touched = true;
  }

  ballBall() {
    const list = this.balls;
    for (let i = 0; i < list.length; i++) {
      for (let j = i + 1; j < list.length; j++) {
        const a = list[i];
        const c = list[j];
        n.copy(c.pos).sub(a.pos);
        const d = n.length();
        const min = a.r + c.r;
        if (d >= min || d < 1e-6) continue;
        n.divideScalar(d);
        const push = (min - d) / 2;
        a.pos.addScaledVector(n, -push);
        c.pos.addScaledVector(n, push);
        const va = a.vel.dot(n);
        const vc = c.vel.dot(n);
        if (va - vc > 0) {
          a.vel.addScaledVector(n, vc - va);
          c.vel.addScaledVector(n, va - vc);
        }
      }
    }
  }
}
