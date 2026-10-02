# MVP03 | HD2D Church Courtyard

Open `Scenes/MVP03_StainedGlassChapel.unity` and press Play. Move the light-clothed pixel character with WASD or the arrow keys. The dark-clothed companion stays in the courtyard and faces the camera.

The main scene separates rendering styles in the same camera: the church entrance, arcade, staircase, paving, tree, and flowers render at full resolution with URP Lit materials and real-time shadows; only the character sprites use a 32 x 48 pixel source image with point filtering and no mipmaps. The stone textures are procedural color maps with companion normal maps. A restrained volume adds color grading, bloom, and depth of field. The camera uses the fog-free URP renderer assigned to MVP02, so MVP01's renderer features do not affect this scene.

Use **MVP03 > Build HD2D Church** to regenerate the scene and assets. **MVP03 > Capture HD2D Preview** writes `Previews/MVP03_Chapel.png`. The earlier uniformly pixelated chapel is preserved as **MVP03 > Build Pixel Chapel Archive**, which writes a separate scene at `Scenes/MVP03_PixelChapelArchive.unity` if needed for comparison.
