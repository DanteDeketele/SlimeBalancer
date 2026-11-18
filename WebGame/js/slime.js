// The slime look, after the Unity SlimeShader graph: a glossy body whose rim blends
// to a second "side" colour (fresnel), with a slow noise wobble on the vertices.
import * as THREE from "three";

const shared = { time: { value: 0 } };

export function tickSlimes(t) {
  shared.time.value = t;
}

export function slimeMaterial(main, side = null, seed = Math.random() * 100) {
  const mainColor = new THREE.Color(main);
  const sideColor = side ? new THREE.Color(side) : mainColor.clone().offsetHSL(0.06, 0, 0.18);
  const mat = new THREE.MeshPhysicalMaterial({
    color: mainColor,
    roughness: 0.18,
    metalness: 0,
    clearcoat: 1,
    clearcoatRoughness: 0.1,
    sheen: 0.4,
    sheenColor: sideColor,
    transparent: true,
    opacity: 0.93,
  });
  mat.userData.side = sideColor;
  mat.onBeforeCompile = (shader) => {
    shader.uniforms.uTime = shared.time;
    shader.uniforms.uSeed = { value: seed };
    shader.uniforms.uSide = { value: sideColor };
    shader.vertexShader = shader.vertexShader
      .replace(
        "#include <common>",
        `#include <common>
        uniform float uTime; uniform float uSeed;`
      )
      .replace(
        "#include <begin_vertex>",
        `#include <begin_vertex>
        float wob = sin(position.x * 7.0 + uTime * 2.3 + uSeed) * cos(position.z * 6.0 + uTime * 1.7 + uSeed * 1.3) +
                    sin(position.y * 5.0 + uTime * 2.9 + uSeed * 0.7);
        transformed += normal * wob * 0.025;`
      );
    shader.fragmentShader = shader.fragmentShader
      .replace(
        "#include <common>",
        `#include <common>
        uniform vec3 uSide;`
      )
      .replace(
        "#include <emissivemap_fragment>",
        `#include <emissivemap_fragment>
        float fres = pow(1.0 - abs(dot(normalize(-vViewPosition), normal)), 2.2);
        totalEmissiveRadiance += uSide * fres * 0.75 + diffuseColor.rgb * 0.12;`
      );
  };
  return mat;
}

const bodyGeo = new THREE.SphereGeometry(0.5, 40, 28);

// A slime blob of diameter 1 (scale it for bigger slimes). Sits with its centre at y=0.
export function makeSlime(main, { side = null, eyes = false } = {}) {
  const group = new THREE.Group();
  const body = new THREE.Mesh(bodyGeo, slimeMaterial(main, side));
  body.scale.set(1, 0.86, 1);
  body.castShadow = true;
  group.add(body);
  group.userData.body = body;
  if (eyes) addEyes(group);
  return group;
}

const eyeWhite = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.3 });
const eyeBlack = new THREE.MeshStandardMaterial({ color: 0x111111, roughness: 0.2 });
const eyeGeo = new THREE.SphereGeometry(0.1, 16, 12);
const pupilGeo = new THREE.SphereGeometry(0.06, 12, 10);

export function addEyes(group) {
  for (const side of [-1, 1]) {
    const eye = new THREE.Mesh(eyeGeo, eyeWhite);
    eye.position.set(side * 0.16, 0.1, 0.4);
    eye.scale.set(1, 1.25, 0.6);
    const pupil = new THREE.Mesh(pupilGeo, eyeBlack);
    pupil.position.set(0, -0.01, 0.07);
    eye.add(pupil);
    group.add(eye);
  }
}

// Little splash of droplets, used when slimes land or get eaten.
export class Splashes {
  constructor(scene) {
    this.scene = scene;
    this.parts = [];
    this.geo = new THREE.SphereGeometry(0.08, 8, 6);
  }
  burst(pos, color, count = 14, power = 3) {
    const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.2, transparent: true });
    for (let i = 0; i < count; i++) {
      const m = new THREE.Mesh(this.geo, mat);
      m.position.copy(pos);
      const a = Math.random() * Math.PI * 2;
      const up = 0.6 + Math.random();
      const v = new THREE.Vector3(Math.cos(a) * power * Math.random(), up * power, Math.sin(a) * power * Math.random());
      const s = 0.5 + Math.random();
      m.scale.setScalar(s);
      this.scene.add(m);
      this.parts.push({ m, v, life: 0.9 + Math.random() * 0.5, mat });
    }
  }
  update(dt) {
    for (let i = this.parts.length - 1; i >= 0; i--) {
      const p = this.parts[i];
      p.life -= dt;
      p.v.y -= 9.81 * dt;
      p.m.position.addScaledVector(p.v, dt);
      p.mat.opacity = Math.min(1, p.life * 2);
      if (p.life <= 0) {
        this.scene.remove(p.m);
        this.parts.splice(i, 1);
      }
    }
  }
}
