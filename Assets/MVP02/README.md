# MVP02 | Binary Walk

Open `Scenes/MVP02_BinaryWalk.unity`. Walk with WASD or arrow keys, hold Shift to move faster, click to capture the mouse, and press Escape to release it.

The scene uses a softly separated four-band off-white skybox and a three-tone URP shader. A separate cool gray ground material separates the walkable floor from the white cubes. Light-facing cube surfaces stay white, side faces become warm gray, and cast shadows fall to charcoal with a narrow soft edge. A small number of acid-yellow caps and seams mark the focal points. The main cube, hovering yellow crown, dark cutouts and offset slabs echo the supplied reference image.

The camera selects the separate `MVP02_Renderer`, so the radial fog feature used by MVP01 does not soften these hard edges. Use **MVP02 > Build Binary Walk** to regenerate and **MVP02 > Capture Preview** to write PNGs to the project's `Previews` folder.
