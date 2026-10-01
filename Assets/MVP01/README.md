# MVP01 — Metal Column Field

Open `Scenes/MVP01_BrutalistWalk.unity` and press Play. Click the Game view to capture the mouse; press Escape to release it. Move with WASD or arrow keys, hold Shift to move faster, and look with the mouse.

The scene contains one 100 × 100 m metal Plane for the floor and 14 rectangular metal columns arranged around its center. Both use URP/Lit metallic PBR materials with procedural color, normal, metallic/smoothness, and occlusion maps. A deterministic combined mesh places dark grass, tall pale flowers, and dense clusters of small ground flowers on the middle of the metal, thinning towards the perimeter. Ground blossoms vary among carmine, rust, amber, violet, teal, rose, and ivory. The plants gently sway and do not block walking. Cool light and a layered white sky give the field an uncanny mood. The previous concrete texture study stays in the project assets but is not used by this scene.

Fog is a URP full-screen pass on `PC_Renderer`: it reconstructs each visible point from the depth texture, measures its radial distance from the camera, and distorts that distance with slowly drifting world-space noise. Adjust the clear radius, opaque radius, noise scale, and distortion in `Materials/Camera_Radial_Fog.mat`. Unity's scene-wide linear fog is disabled to avoid applying fog twice.

Use **MVP01 > Build Metal Field** in the Unity Editor to regenerate the scene and four PNGs in the project's `Previews` directory. Use **MVP01 > Capture Preview** to refresh only the PNGs. `MVP01_FloraDetail.png` shows the central growth close up.
