# MVP03 | HD2D Church Courtyard

Open `Scenes/MVP03_StainedGlassChapel.unity` and press Play. Move the light-clothed pixel character with WASD or the arrow keys. Movement is limited to four directions relative to the camera; when two axes are held, the most recently pressed axis takes priority. Opposing keys on one axis cancel. The camera uses MVP04's follow and distance zoom: **Q moves closer, E moves farther, and the mouse wheel zooms smoothly between 5 and 15 metres**. It retains the courtyard's oblique angle and keeps the character's torso at screen centre, including when climbing stairs. The dark-clothed companion stays in the courtyard and faces the camera.

Press **Space** to fire a 3D light bolt in the last movement direction (camera-forward before moving). Each press fires once, with a 0.35-second cooldown. The bolt has a solid emissive sphere, a Fresnel shell, rotating mesh rings, a short ribbon and mesh sparks. Its cyan point light illuminates nearby stone. Swept sphere collision triggers a surface ripple, an expanding flash and a short particle burst; effects clean themselves up. This is a visual attack prototype without health or damage. Prefabs are in `Prefabs/MagicBolt.prefab` and `Prefabs/MagicImpact.prefab`. Use **MVP03 > Add or Rebuild Magic Attack** to update the assets and attach the caster to the open courtyard; save the scene afterwards. **MVP03 > Capture Magic Preview** captures the current camera to `Previews/MVP03_Magic.png` (pause during a shot to capture the effect).

The courtyard is rebuilt around the supplied reference: close stone piers and a staircase frame a compact playable route. Beveled blocks, irregular pavers and carved arches use a weathered limestone albedo with normal relief. The `World Stone PBR` shader projects that material in world space so it retains the same scale on walls, stairs and pillars. Geometry is combined into saved mesh assets for a small number of renderers.

The tree uses narrow leaf meshes and tapered branches, with real leaf shadows across the masonry. Warm sunlight, cooler ambient fill, SSAO, ACES grading and bokeh depth of field establish the light and depth. MVP03 now has its own renderer; its index is added to the existing PC pipeline without changing that pipeline's default renderer.

## Warm volumetric sunlight (MVP04 migration)

MVP03 shares MVP04's `WindowVolumeFeature`, single-scattering ray-march shader, cached 3D noise, moving air, edge-aware denoising and depth-aware upsampling. Its open courtyard uses the **main directional sun** as its volumetric source. Golden light arrives along `(0.18, -0.64, -0.746)` (normalized), passing through the actual tree canopy and architecture. The volume samples the same cascaded shadow atlas as the scene, using hardware comparison filtering and subsequent volume denoising. Shadow strength is bound per camera. Sunlight is parallel; it has no inverse-square falloff. Both incoming and camera paths use Beer-Lambert attenuation and the same density field.

The medium occupies `(-13.9, 0, -30.4)` to `(12, 15, 3.4)`. Unity's old linear fog is disabled. Opaque mortar cores behind the separated stone blocks close light leaks through solid wall joints. The existing geometry, sprites, magic and mild pixel finish are retained. Focus still tracks the character, with a 70 mm / f4 lens setting to keep the close zoom usable.

| Parameter | Preset |
| --- | --- |
| Sun RGB / intensity | `(1, 0.90, 0.72)` / `3.4` |
| Extinction / scattering albedo / HG g | `0.022 m^-1` / `0.92` / `0.35` |
| Density variation / frequency | `0.7` / `0.38` |
| Air speed / warp / fine layer | `0.18 m/s` / `1.1 m` / `0.35` |
| Volume resolution / steps | One third width and height / 32 view steps / 1 light-path sample |
| Denoising | `0.9` |

**MVP03 > Select Warm Volume Settings** selects `Materials/WarmAirVolume.mat`: adjust density, scattering and air motion in the Inspector. Select **Sun / warm diagonal daylight** for colour, intensity and direction. Volume quality and bounds are on **Warm courtyard / MVP04 single scattering** in `Rendering/RebuiltRenderer.asset`.

**MVP03 > Apply Warm Volumetric Light and Camera** upgrades the open scene without rebuilding geometry. It reapplies the warm sun, camera and focus preset, preserves existing medium/quality values, and does not duplicate wall cores or renderer features. Future reference-courtyard builds include this migration. MVP04 keeps its own medium material, bounds and three window sources; directional scattering is opt-in and remains disabled there.

`Previews/MVP03_WarmVolumeGame.png` shows actual Play output. `Previews/MVP03_WarmVolumeValidation.json` records linear-HDR checks for warm scattering, geometric occlusion, source-off extinction, finite pixels, unchanged MVP04 source selection/bounds, character follow, zoom limits and live Editor frame timings. This is real-time single scattering with finite shadow-map and ray-step resolution.

## Pixel finish and assets

A subtle full-screen pixel pass runs after post processing, covering the scene and transparent magic together. It uses **2x2 screen-pixel cells at 55% blend** with the original image, retaining fine detail, bloom gradients and the existing colour palette. Adjust **Pixel Size** and **Pixel Blend** on `Materials/SubtlePixels.mat`; blend 0 disables the visual effect, while 0.35–0.65 is the intended mild range. The effect belongs to the dedicated MVP03 renderer. **MVP03 > Apply Subtle Pixel Post Process** installs it without rebuilding the scene and preserves existing material settings. **MVP03 > Capture Subtle Pixel Comparison** captures the same frame with and without the effect to `Previews/MVP03_SubtlePixels.png` and `Previews/MVP03_SubtlePixels_Original.png` (pause during a spell to include the magic).

The two original travelers are imported as 56-pixel-high sprites with point filtering and no mipmaps. Their alpha-tested shader writes depth and casts silhouette shadows. `PixelFocus` follows the playable character's depth so the sprite remains in focus while moving; foreground stairs and distant architecture soften. The generated source art and exact prompts are recorded in `ArtGenerationPrompts.txt`.

Use **MVP03 > Build HD2D Church** or **MVP03 > Rebuild Reference Courtyard** to regenerate the scene and assets. **MVP03 > Capture HD2D Preview** writes `Previews/MVP03_Chapel.png`. **MVP03 > Archive > Build Earlier Courtyard** writes the previous courtyard to `Scenes/MVP03_PreviousCourtyard.unity`. The original uniformly pixelated chapel generator remains under **MVP03 > Build Pixel Chapel Archive**.
