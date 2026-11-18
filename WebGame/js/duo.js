// Helpers for the 2-player modes.
import * as THREE from "three";

// Co-op boards: coloured rims show who steers what. Player 1's colour on the
// left and right edges (left/right tilt), player 2's on the near and far edges.
export function addAxisStrips(board, half, ctx) {
  if (ctx.mode !== "coop") return;
  const mat1 = new THREE.MeshStandardMaterial({ color: ctx.playerColor(0), emissive: ctx.playerColor(0), emissiveIntensity: 0.35 });
  const mat2 = new THREE.MeshStandardMaterial({ color: ctx.playerColor(1), emissive: ctx.playerColor(1), emissiveIntensity: 0.35 });
  const t = 0.14;
  for (const side of [-1, 1]) {
    const lr = new THREE.Mesh(new THREE.BoxGeometry(t, 0.24, half * 2 - 0.3), mat1);
    lr.position.set(side * (half - t / 2), 0.03, 0);
    board.add(lr);
    const fb = new THREE.Mesh(new THREE.BoxGeometry(half * 2 - 0.3, 0.24, t), mat2);
    fb.position.set(0, 0.03, side * (half - t / 2));
    board.add(fb);
  }
}
