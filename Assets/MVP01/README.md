# MVP01 — Brutalist Walk

Open `Scenes/MVP01_BrutalistWalk.unity` and press Play. Click the Game view to capture the mouse; press Escape to release it. Move with WASD or arrow keys, hold Shift to move faster, and look with the mouse.

The scene is a 74-box first-person visual test: a compressed entrance, column field, open courtyard, central cantilevered monolith, and side return route. All geometry uses cube meshes with face UVs measured in world units. Concrete base color is built from four tileable Perlin noise octaves. Directionally stretched Perlin noise drives its specular/smoothness map and fine normal grooves through URP/Lit's specular workflow. Steel and oxide retain metallic PBR maps. A custom skybox shader draws layered warm and cool whites; stronger linear distance fog matches its horizon.

Use **MVP01 > Build Brutalist Walk** in the Unity Editor to regenerate the scene and the three PNGs in the project's `Previews` directory. Use **MVP01 > Capture Preview** to refresh only the PNGs.
