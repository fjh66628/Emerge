# MVP03 | HD2D Church Courtyard

Open `Scenes/MVP03_StainedGlassChapel.unity` and press Play. Move the light-clothed pixel character with WASD or the arrow keys. The dark-clothed companion stays in the courtyard and faces the camera.

The courtyard is rebuilt around the supplied reference: close stone piers and a staircase frame a compact playable route. Beveled blocks, irregular pavers and carved arches use a weathered limestone albedo with normal relief. The `World Stone PBR` shader projects that material in world space so it retains the same scale on walls, stairs and pillars. Geometry is combined into saved mesh assets for a small number of renderers.

The tree uses narrow leaf meshes and tapered branches, with real leaf shadows across the masonry. Warm sunlight, cooler ambient fill, SSAO, ACES grading and bokeh depth of field establish the light and depth. MVP03 now has its own renderer; its index is added to the existing PC pipeline without changing that pipeline's default renderer.

The two original travelers are imported as 56-pixel-high sprites with point filtering and no mipmaps. Their alpha-tested shader writes depth and casts silhouette shadows. `PixelFocus` follows the playable character's depth so the sprite remains in focus while moving; foreground stairs and distant architecture soften. The generated source art and exact prompts are recorded in `ArtGenerationPrompts.txt`.

Use **MVP03 > Build HD2D Church** or **MVP03 > Rebuild Reference Courtyard** to regenerate the scene and assets. **MVP03 > Capture HD2D Preview** writes `Previews/MVP03_Chapel.png`. **MVP03 > Archive > Build Earlier Courtyard** writes the previous courtyard to `Scenes/MVP03_PreviousCourtyard.unity`. The original uniformly pixelated chapel generator remains under **MVP03 > Build Pixel Chapel Archive**.
