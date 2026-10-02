# MVP04 | Nocturne Chapel

Open `Scenes/MVP04_NocturneChapel.unity` and press Play. **WASD / arrow keys** move the sprite character in four directions; the camera follows and keeps the character centred. **Space** fires the existing 3D magic bolt. The chapel reuses MVP03 movement, focus, stone PBR and pixel finishing.

## Character and camera controls

Click the **Game** view after entering Play Mode so it receives keyboard input. A small bar along the bottom displays the controls.

| Input | Action |
| --- | --- |
| WASD / arrow keys | Walk forward, backward, left or right relative to the camera. The most recently pressed axis wins; movement stays in four directions. |
| Hold Q / E | Smoothly move the camera closer / farther away. |
| Mouse wheel up / down | Move the camera closer / farther away in steps. |
| Space | Cast a 3D magic bolt in the character's facing direction. |

Camera distance is clamped to **5–15 metres**. Zoom preserves the viewing angle, keeps the character centred and updates depth-of-field focus with the character. The camera follows movement directly; only zoom distance is smoothed. Zoom is opt-in on the shared `PixelFollowCamera`, so existing MVP03 scenes keep their previous behaviour.

**MVP04 > Apply Player Controls** reconnects the existing character and camera and adds zoom and the control hint without rebuilding the chapel or changing its lighting. New chapel builds include these controls automatically.

### Four-direction character animation

The chapel pilgrim now uses a **24-frame atlas**: six frames each for front, left, right and back. **W / Up** shows the back, **S / Down** the front, **A / Left** the left profile and **D / Right** the right profile. On release the character keeps its last facing and returns to that row's standing frame. Initial facing is back, matching the existing forward-facing magic attack.

`PixelWalkAnimation` advances the walk cycle by actual horizontal displacement after `CharacterController.SimpleMove`. The default cycle spans **2.25 metres**, about nine frames per second at the normal 3.4 m/s walk speed. Stopping or pressing into an obstacle with no displacement holds idle. The frames supply body, arm, boot and robe movement; the old whole-sprite bob is bypassed when a walk set is assigned. Each direction has a shared foot pivot, and the billboard's horizontal axis matches the camera to avoid left/right mirroring.

The atlas is `Textures/PilgrimWalk.png`, with point filtering, no mipmaps, no compression and alpha cutout. `Textures/PilgrimWalk.asset` contains the directional sprite arrays and cycle distance. `NocturneTravelerWalk.mat` keeps the chapel character tint and depth/shadow settings. The shared MVP03 movement script falls back to its existing static portrait behaviour when no animation component is present.

**MVP04 > Apply Four Direction Walk Animation** imports the atlas and connects the current character; new chapel builds include it. The built-in image generation tool created the atlas using the existing traveler as a reference. Its exact prompt is saved in `Previews/MVP04_WalkAnimationPrompt.txt`. `Previews/MVP04_WalkValidation.json` records direction, frame coverage, idle, movement and texture checks; the four `MVP04_Walk_*.png` previews show the chapel renderer output.

Automated Play Mode checks use simulated input to cover all four directions, axis priority, camera centring, both zoom limits, opposing zoom keys, wheel steps and depth-of-field tracking. Results are saved in `Previews/MVP04_ControlsValidation.json`; these checks do not establish that a physical keyboard is registered or receiving events. `MVP04_ZoomNear.png` and `MVP04_ZoomFar.png` show the actual Game output at the two limits.

Movement, zoom and casting prefer an enabled, registered hardware keyboard over a virtual test keyboard and ignore removed devices. If mouse input works but every keyboard key is unresponsive in the Editor, check **Window > Analysis > Input Debugger > Devices** for a native keyboard. An orphaned virtual keyboard cannot receive hardware key presses; save and restart the Editor to rediscover a missing native device. Input tests must preserve hardware devices and verify their presence after cleanup.

`Previews/MVP04_KeyboardRoutingValidation.json` checks hardware selection with a virtual or removed current device and verifies the device list is preserved. `Previews/MVP04_PhysicalInputValidation.json` records movement observed from the native keyboard after recovery, without injecting input.

## Light sources and projection

There are three real exterior spotlights: the existing front source plus a source outside each side wall. The front source retains the user's saved position and intensity in `Rendering/ExteriorLight.asset`. The left and right sources are at **(-24, 14, 12)** and **(24, 14, 12)** metres, aimed at **(0, 1, 1)**, each with intensity **18000**, an **82-degree** cone and **65-metre** range. They are elevated artificial lights; the intensity uses URP relative units rather than calibrated lumens.

Each side source lights the entire corresponding row of four windows. `ChapelWindowLight` projects the real apertures into a 1024? RGB cookie, shared by surface lighting and volumetric scattering. Rays intersect the nearest room-envelope face first; opaque walls and roof block transmission. Shadow maps add occlusion from deep reveals, columns, leading and furniture. Moving a source changes both its shafts and surface projections. The front and opposite-side emitters have independent settings, so light on both facades has an explicit physical origin.

## Participating medium

`WindowVolumeFeature` runs a depth-limited world-space ray march before post processing. The standard preset uses half width and height (one quarter of the pixels), at most 40 camera-ray steps and two light-path samples. Air occupies the chapel envelope from (-8.86, 0, -13) to (8.86, 13.6, 17.57); the exterior is assumed clear.

- **Extinction:** a single density field covers the room, independent of how many lights or shadows are present. Default extinction is 0.028 per metre, scattering albedo 0.9, density noise amplitude 0.2, frequency 0.28.
- **Beer–Lambert attenuation:** `T = exp(-integral(sigma_t ds))` is evaluated along the camera path and the interior portion of the path back to the light. The light path uses the selected quality level?s density samples; uniform density has an analytic evaluation. Unity's previous global fog is disabled to avoid applying extinction twice.
- **Incident light:** sums the three emitters using URP's inverse-square attenuation, finite-range taper, spotlight cone, glass transmission and geometric shadow visibility. Extinction is still applied only once per camera-ray segment.
- **Angular scattering:** normalized Henyey–Greenstein phase function, with default `g = 0.25`.
- **Integration:** each segment adds `T_camera * albedo * (1 - T_segment) * incident_radiance * phase`, then updates camera transmittance once. No independent volume intensity gain is applied.

The implementation follows [Beer–Lambert transmittance](https://pbr-book.org/4ed/Volume_Scattering/Transmittance) and the [Henyey–Greenstein phase function](https://pbr-book.org/4ed/Volume_Scattering/Phase_Functions).

### Moving air

The **空气流动 · 世界空间** section in **MVP04 > 体积光设置** controls slowly drifting density inside the shafts:

| Control | Default | Effect |
| --- | --- | --- |
| 流速 / 米每秒 | 0.28 | Advection speed; zero freezes the field at its current position. |
| 风向 XYZ | (0.8, 0.2, 0.35) | Normalized world-space direction; positive Y rises. A zero vector stops motion. |
| 扭曲幅度 / 米 | 1.1 | A broad, slower vector field bends the moving density. |
| 细层雾丝 | 0.45 | Adds a second, finer density layer. Zero skips that lookup. |

**密度起伏** sets the contrast and **密度噪声频率** sets the size of the formations (lower means larger). Both camera and incoming-light extinction sample this shared field, so the variation affects actual scattering and attenuation. CPU-integrated displacement keeps speed and direction edits continuous; Play Mode pause also freezes motion. The density is anchored in world space rather than to the camera.

`AirFlowNoise.asset` is a repeating 32x32x32 linear RGBA volume with six mip levels: RGB supplies independent warp components and A supplies density. Broad warp, base density and fine density use three filtered texture samples. The integration segment length selects a mip level so structures smaller than the sampling interval average out. Existing spatial denoising remains active. This is procedural advection and domain warping; it does not solve fluid dynamics or react to character motion.

`Previews/MVP04_AirFlow_0s.png` and `MVP04_AirFlow_6s.png` show the same view six seconds apart. `MVP04_AirFlowValidation.json` records motion, zero-speed freezing, shader checks and Editor frame timings with the user's current quality settings.

### Real-time approximations

This is single scattering with finite ray-march sampling and shadow-map resolution, not path tracing. The point emitters use hard geometric shadows; finite source-size penumbrae are not integrated. Frosted glass uses a thin rough-pane approximation: each emitter?s direct-transmission setting divides coloured direct transmission from a complementary rough transmitted contribution on the pane, including distance, incidence angle and cone falloff. Full angular diffusion from the glass into the room is not traced. Surface direct lighting uses URP's clear-air model; interior attenuation along the incoming path is evaluated in the volumetric integral, while the camera-path attenuation applies to the complete image. Ambient colours and local fill lights approximate bounced illumination; they do not generate additional volumetric shafts.

## Performance and settings

Open **MVP04 > 体积光设置**. The **当前光源** selector switches between front, left and right source settings. Positions, targets, intensity, cone, range and transmission remain independently editable. Changes preview live, support Undo/Redo and are saved during Play Mode too. The source positions should remain outside the room envelope.

The **体积光质量** selector changes only the volume pass:

| Preset | Width / height | Camera steps | Light-path samples |
| --- | --- | --- | --- |
| 性能 | 1/3 | 32 | 1 |
| 标准 (default) | 1/2 | 40 | 2 |
| 精细 | Full | 64 | 4 |

The integrator writes scattering RGB and transmittance to an RGBA16F texture. Interleaved sampling offsets distribute ray-march samples across small pixel neighbourhoods, replacing independent white-noise offsets. The **体积光降噪** slider defaults to **0.9**; **0** bypasses filtering for comparison. Reduced-resolution rendering uses two 3x3 spatial filters with strides of one and two texels; full-resolution rendering uses one filter. Depth and relative RGB differences reduce mixing across object silhouettes and coloured shaft boundaries. The filters process only scattering and transmittance. This is spatial reconstruction with no temporal history; extremely fine shaft details can soften at reduced resolution.

A four-tap, depth-weighted upsample combines the filtered volume with the original full-resolution colour. Foreground edges reject background samples; extremely thin geometry with no matching low-resolution depth is left clear to avoid background light bleeding over it. This conservative fallback trades a small amount of fog accuracy at subpixel silhouettes for a clean character outline. Geometry, sprites, shadows and the existing pixel finish retain their normal resolution.

`Previews/MVP04_DenoiseOff.png` and `MVP04_DenoiseOn.png` compare filtering at the same camera pose, both using the interleaved sampling pattern. At 2560x1440 with the performance preset and unchanged light/medium settings, 120-frame Editor averages were **5.73 ms off / 5.85 ms on**. This is a frame-time comparison, not an isolated GPU timing or frame-rate guarantee. See `Previews/MVP04_DenoiseValidation.json`.

A reusable 32x32x32 RGBA texture with mipmaps replaces repeated procedural density hashing and supplies the air-motion layers. Light components no longer rewrite unchanged transforms, properties and shader globals every frame; projection cookies rebuild only when needed.

In the same **2560?1440 Editor view**, 120-frame averages measured **48.13 ms / 20.8 FPS** before the change and **9.59 ms / 104.3 FPS** after it, with all three emitters enabled in the new version. Disabling only the volume pass measured 5.64 ms before and 7.78 ms after; these are frame-time comparisons, not isolated GPU profiler timings or a frame-rate guarantee. See `Previews/MVP04_Performance.json`.

The air controls use `Materials/RoseWindowVolume.mat`; the three source profiles live in `Rendering/ExteriorLight.asset`, `ExteriorLeft.asset` and `ExteriorRight.asset`. The renderer feature stores the selected quality. User-tuned live values may differ from builder defaults. Glass grain and roughness remain in the two glass materials; ACES, bloom, SSAO, focus and the subtle pixel effect are retained.

## Build, upgrade and preview

- **MVP04 > Build Dark Rose Window Chapel:** regenerates the chapel and its generated assets. Existing exterior source settings are retained; medium defaults are reapplied.
- **MVP04 > Apply Optimized Side Lighting:** installs the two side sources, cached noise and standard rendering quality without resetting air or existing source settings.
- **MVP04 > Apply Physical Window Lighting:** upgrades the open MVP04 scene without rebuilding its geometry. Removes legacy sources, configures the three sources, replaces the medium defaults and saves.
- **MVP04 > Add Side Stained Glass Windows:** rebuilds the side walls and apertures, reconnecting them to the exterior sources.
- **MVP04 > Apply Frosted Glass:** updates the glass materials.
- **MVP04 > Apply Air Motion:** installs the cached flow texture and the four motion defaults while retaining existing density, colour, source and quality settings.
- **Capture Dark Chapel Preview / Capture Side Window Preview / Capture Left Window Preview / Capture Frosted Glass Detail:** write the corresponding images under the project-root `Previews/` directory.

`Previews/MVP04_GameView.png` records the actual Play Mode output. Always check this in addition to offscreen `Camera.Render` previews. `Previews/MVP04_LightingSettings.png` shows the controls.

## Validation

C# compilation and shader checks pass; there are no invalid mesh normals. Both side emitters contribute nonzero scattering when tested independently. Source-off rendering contributes no scattering; source intensity remains linear before tone mapping. `Previews/MVP04_PhysicsValidation.json` records the three-source, 480?300 linear HDR check with density noise temporarily disabled; all source and material settings are restored afterwards. Left/right previews and the actual Game capture verify the projections and silhouette reconstruction. The quality selector is checked with Undo/Redo. These checks cover this real-time single-scattering model, not full global illumination.
