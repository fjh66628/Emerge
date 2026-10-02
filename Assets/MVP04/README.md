# MVP04 | Nocturne Chapel

Open `Scenes/MVP04_NocturneChapel.unity` and press Play. **WASD / arrow keys** move the pilgrim in four directions; the camera follows and keeps the character centred. **Space** fires the existing 3D light bolt in the last movement direction. Stone, pews and the altar block movement and projectiles.

The dark interior is arranged symmetrically along the nave: bundled stone columns, transverse arches, ribbed vaults, oak pews, a raised altar and a large circular stained-glass window directly ahead. Cold window light contrasts with warm candle pools. Individual beveled masonry and floor pieces reuse MVP03's world-space stone PBR shader and limestone textures. The character, movement, camera, focus and magic prefab also reuse MVP03. Materials, meshes, volume profile and renderer settings for the new environment live in MVP04.

## Rendering

### Interactive lighting controls

Open **MVP04 > 体积光设置** for a dedicated settings window. Sliders update the Game and Scene views immediately, save to the MVP04 volume material after a short debounce, and support Undo/Redo. Changes made during Play Mode are persistent material edits. The **柔和 / 清晰 / 锐利** presets change edge softness, rose-beam spread, transmission contrast and shadow definition without changing brightness or density. The **立即保存** button also saves pending edits.

- **边缘锐利度:** larger values narrow the aperture transition for both the rose and side windows.
- **窗格分束对比 / 阴影锐利度:** strengthen the gaps between shafts and tighten the shadow penumbra.
- **圆窗光束扩散:** smaller values keep the rose beam narrow along its length.
- **光束亮度:** scales the scattering from all windows, independently of surface lighting.
- **圆窗 / 侧窗雾中散射:** adjust the amount of scattering in the air; **尘雾纹理频率** adjusts its noise detail.

The builder's sharp preset uses edge softness **0.008**, rose spread **0.001**, transmission contrast **2.05**, and shadow sharpness **0.9**. `Previews/MVP04_LightingSettings.png` shows the settings window. Its live values can differ from the builder defaults after editing.

### Shading

- **Window light:** `WindowVolumeFeature` runs one bounded 72-step world-space ray march before post processing for the rose and eight side windows. Scene depth limits integration to visible air. Main and additional-light shadow maps account for architectural occlusion. Narrower beam edges, reduced spreading, stronger transmission contrast and remapped shadow penumbrae make the shafts more defined. Four arched windows on each side have actual wall openings, carved stone surrounds and blue/teal/amber glass. Each `SideWindowLight` associates a shadow-casting spotlight with its aperture; glass transmission and drifting 3D noise colour the diagonal shafts. Camera-specific visible-light indices select the correct shadow map. The volume is tailored to this chapel's fixed window dimensions.
- **Frosted glass:** `FrostedGlass.shader` shades both rose and side panes with PBR, rough micro-normals, mottled scattering and fine grain that fades below pixel resolution. A short texture blur softens transmitted colour while the opaque lead remains dark. This approximates light diffusing through ground glass; it does not trace exterior refraction. The original transmission masks remain available to the light beams and projected cookies.
- **Side illumination:** matching RGB spotlight cookies project the glass pattern onto stone and furniture. MVP04 uses Forward+ to keep window lights, ambient bounce and candles active together. The shared stone PBR shader supports clustered lights and cookies, while retaining the MVP03 forward variant.
- **Surface light:** the real opening, physical stone frame and bronze tracery cast directional moonlight shadows. Local warm lights illuminate candle stands. A brighter three-colour ambient environment and broad cool bounce lights reveal stone, pews and paving in shadow while retaining the direct window light contrast. **MVP04 > Apply Readable Ambient Lighting** updates and saves only the open chapel's lighting without rebuilding its geometry.
- **Finishing:** SSAO, ACES, bloom and restrained character-focused depth of field. The current MVP03 settings (**6-pixel cells / 32% blend** when this version was generated) process the complete scene and magic after these effects. MVP04 has a separate renderer and a copied pixel material, so later adjustments can be made independently.
- **Controls:** edit `Materials/RoseWindowVolume.mat` to tune rose dust density, side-window dust density (default **0.11**), beam edge softness, spread, transmission contrast and shadow definition. Each side light's `SideWindowLight.scattering` controls its individual shaft strength. `LuminousRoseGlass.mat` and `LuminousSideGlass.mat` expose frost amount, transmission blur, etched grain, roughness and transmitted light. `Materials/SubtlePixels.mat` tunes pixel size and blend. `Rendering/NocturneAtmosphere.asset` controls bloom, exposure and focus. The beam origins and bounds are designed for this chapel layout.

## Rebuild and preview

**MVP04 > Build Dark Rose Window Chapel** regenerates and opens this scene, registers its renderer with the PC URP asset and adds the scene to Build Settings. It uses the existing MVP03 assets as dependencies. Run it outside Play mode; it replaces the generated MVP04 scene and generated asset settings.

**MVP04 > Capture Dark Chapel Preview** saves the active camera at 1600x1000 to `Previews/MVP04_DarkChapel.png`. The project uses Unity 6.3 / URP 17.3 Render Graph; the custom volume feature targets that render path.

**MVP04 > Add Side Stained Glass Windows** updates the side walls, windows and their lights in the open chapel, preserving its furniture, player, camera and ambient settings. **MVP04 > Capture Side Window Preview** temporarily turns the camera towards the side aisle, writes `Previews/MVP04_SideWindows.png`, and restores the camera.

**MVP04 > Apply Frosted Glass and Sharper Beams** updates the three window materials without rebuilding the scene. **MVP04 > Capture Frosted Glass Detail** saves `Previews/MVP04_FrostedGlass.png` and restores the camera and focus afterwards.

Validate lighting in the actual Game window as well as the off-screen preview. `ChapelMesh.Box` supplies face normals for zero-bevel boxes: zero-length normals on the side-wall backing caused invalid HDR lighting that bloom and depth of field spread into large white regions. The repaired backing meshes retain the original light and post-processing settings. `Previews/MVP04_GameView.png` records the corrected Play Mode output.
