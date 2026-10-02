# MVP04 | Nocturne Chapel

Open `Scenes/MVP04_NocturneChapel.unity` and press Play. **WASD / arrow keys** move the pilgrim in four directions; the camera follows and keeps the character centred. **Space** fires the existing 3D light bolt in the last movement direction. Stone, pews and the altar block movement and projectiles.

The dark interior is arranged symmetrically along the nave: bundled stone columns, transverse arches, ribbed vaults, oak pews, a raised altar and a large circular stained-glass window directly ahead. Cold window light contrasts with warm candle pools. Individual beveled masonry and floor pieces reuse MVP03's world-space stone PBR shader and limestone textures. The character, movement, camera, focus and magic prefab also reuse MVP03. Materials, meshes, volume profile and renderer settings for the new environment live in MVP04.

## Rendering

- **Window light:** `WindowVolumeFeature` runs a bounded 48-step world-space ray march before post processing. Scene depth limits the integration to visible air, and the main light shadow map accounts for architectural occlusion. A stained-glass transmission mask, soft beam edges and slowly drifting 3D noise break up the shafts. This is a local volume for this window, not a general volumetric lighting system for every light.
- **Surface light:** the real opening, physical stone frame and bronze tracery cast directional moonlight shadows. Local warm lights illuminate candle stands. A brighter three-colour ambient environment and broad cool bounce lights reveal stone, pews and paving in shadow while retaining the direct window light contrast. **MVP04 > Apply Readable Ambient Lighting** updates and saves only the open chapel's lighting without rebuilding its geometry.
- **Finishing:** SSAO, ACES, bloom and restrained character-focused depth of field. The current MVP03 settings (**6-pixel cells / 32% blend** when this version was generated) process the complete scene and magic after these effects. MVP04 has a separate renderer and a copied pixel material, so later adjustments can be made independently.
- **Controls:** edit `Materials/RoseWindowVolume.mat` to tune dust density, scatter colour and noise scale; `Materials/SubtlePixels.mat` tunes pixel size and blend. `Rendering/NocturneAtmosphere.asset` controls bloom, exposure and focus. The beam origin and its bounds are designed for this chapel layout.

## Rebuild and preview

**MVP04 > Build Dark Rose Window Chapel** regenerates and opens this scene, registers its renderer with the PC URP asset and adds the scene to Build Settings. It uses the existing MVP03 assets as dependencies. Run it outside Play mode; it replaces the generated MVP04 scene and generated asset settings.

**MVP04 > Capture Dark Chapel Preview** saves the active camera at 1600x1000 to `Previews/MVP04_DarkChapel.png`. The project uses Unity 6.3 / URP 17.3 Render Graph; the custom volume feature targets that render path.
