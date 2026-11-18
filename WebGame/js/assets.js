import * as THREE from "three";
import { FBXLoader } from "three/addons/loaders/FBXLoader.js";

const textures = new Map();
const models = new Map();
const texLoader = new THREE.TextureLoader();
// The FBX files point at Unity texture files we don't ship; hand the loader a blank pixel.
const BLANK = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
const modelManager = new THREE.LoadingManager();
modelManager.setURLModifier((url) => (/\.(png|jpe?g|tga|psd)$/i.test(url) ? BLANK : url));
const fbxLoader = new FBXLoader(modelManager);

export function loadTexture(url) {
  if (!textures.has(url)) textures.set(url, texLoader.loadAsync(url));
  return textures.get(url);
}

// Loads one of the Unity project's FBX models and scales it to a given height.
// Returns a fresh clone each call. Resolves to null if the model can't be read,
// so callers can fall back to simple shapes.
export async function loadModel(url) {
  if (!models.has(url)) {
    models.set(
      url,
      fbxLoader.loadAsync(url).catch((error) => {
        console.warn("Model failed to load", url, error);
        return null;
      })
    );
  }
  const model = await models.get(url);
  return model ? model.clone(true) : null;
}

export function fitHeight(object, height) {
  const box = new THREE.Box3().setFromObject(object);
  const size = box.getSize(new THREE.Vector3());
  const s = height / (size.y || 1);
  object.scale.multiplyScalar(s);
  const box2 = new THREE.Box3().setFromObject(object);
  object.position.y -= box2.min.y;
  return object;
}

// Merges every mesh of a model into one geometry per material, so it can be
// drawn many times with an InstancedMesh.
export function flattenModel(object) {
  object.updateMatrixWorld(true);
  const parts = [];
  object.traverse((child) => {
    if (!child.isMesh) return;
    const geo = child.geometry.clone();
    geo.applyMatrix4(child.matrixWorld);
    const mats = Array.isArray(child.material) ? child.material : [child.material];
    if (mats.length === 1 || !geo.groups.length) {
      parts.push({ geometry: geo, material: mats[0] });
    } else {
      for (const group of geo.groups) {
        const sub = geo.clone();
        sub.clearGroups();
        sub.setDrawRange(group.start, group.count);
        parts.push({ geometry: sub, material: mats[group.materialIndex] });
      }
    }
  });
  return parts;
}
