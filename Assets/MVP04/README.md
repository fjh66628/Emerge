# MVP04 | Nocturne Chapel

Open `Scenes/MVP04_NocturneChapel.unity` and press Play. **WASD / arrow keys** move the sprite character in four directions; the camera follows and keeps the character centred. **Space** fires the existing 3D magic bolt. The chapel reuses MVP03 movement, focus, stone PBR and pixel finishing.

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

### Real-time approximations

This is single scattering with finite ray-march sampling and shadow-map resolution, not path tracing. The point emitters use hard geometric shadows; finite source-size penumbrae are not integrated. Frosted glass uses a thin rough-pane approximation: each emitter?s direct-transmission setting divides coloured direct transmission from a complementary rough transmitted contribution on the pane, including distance, incidence angle and cone falloff. Full angular diffusion from the glass into the room is not traced. Surface direct lighting uses URP's clear-air model; interior attenuation along the incoming path is evaluated in the volumetric integral, while the camera-path attenuation applies to the complete image. Ambient colours and local fill lights approximate bounced illumination; they do not generate additional volumetric shafts.

## Performance and settings

Open **MVP04 > ?????**. The **????** selector switches between front, left and right source settings. Positions, targets, intensity, cone, range and transmission remain independently editable. Changes preview live, support Undo/Redo and are saved during Play Mode too. The source positions should remain outside the room envelope.

The **?????** selector changes only the volume pass:

| Preset | Width / height | Camera steps | Light-path samples |
| --- | --- | --- | --- |
| ?? | 1/3 | 32 | 1 |
| ?? (default) | 1/2 | 40 | 2 |
| ?? | Full | 64 | 4 |

The integrator writes scattering RGB and transmittance to an RGBA16F texture. A four-tap, depth-weighted upsample combines it with the original full-resolution colour. Foreground edges reject background samples; extremely thin geometry with no matching low-resolution depth is left clear to avoid background light bleeding over it. This conservative fallback trades a small amount of fog accuracy at subpixel silhouettes for a clean character outline. Geometry, sprites, shadows and the existing pixel finish retain their normal resolution.

A reusable 32? R8 texture replaces repeated procedural density hashing. Light components no longer rewrite unchanged transforms, properties and shader globals every frame; projection cookies rebuild only when needed.

In the same **2560?1440 Editor view**, 120-frame averages measured **48.13 ms / 20.8 FPS** before the change and **9.59 ms / 104.3 FPS** after it, with all three emitters enabled in the new version. Disabling only the volume pass measured 5.64 ms before and 7.78 ms after; these are frame-time comparisons, not isolated GPU profiler timings or a frame-rate guarantee. See `Previews/MVP04_Performance.json`.

The air controls use `Materials/RoseWindowVolume.mat`; the three source profiles live in `Rendering/ExteriorLight.asset`, `ExteriorLeft.asset` and `ExteriorRight.asset`. The renderer feature stores the selected quality. User-tuned live values may differ from builder defaults. Glass grain and roughness remain in the two glass materials; ACES, bloom, SSAO, focus and the subtle pixel effect are retained.

## Build, upgrade and preview

- **MVP04 > Build Dark Rose Window Chapel:** regenerates the chapel and its generated assets. Existing exterior source settings are retained; medium defaults are reapplied.
- **MVP04 > Apply Optimized Side Lighting:** installs the two side sources, cached noise and standard rendering quality without resetting air or existing source settings.
- **MVP04 > Apply Physical Window Lighting:** upgrades the open MVP04 scene without rebuilding its geometry. Removes legacy sources, configures the three sources, replaces the medium defaults and saves.
- **MVP04 > Add Side Stained Glass Windows:** rebuilds the side walls and apertures, reconnecting them to the exterior sources.
- **MVP04 > Apply Frosted Glass:** updates the glass materials.
- **Capture Dark Chapel Preview / Capture Side Window Preview / Capture Left Window Preview / Capture Frosted Glass Detail:** write the corresponding images under the project-root `Previews/` directory.

`Previews/MVP04_GameView.png` records the actual Play Mode output. Always check this in addition to offscreen `Camera.Render` previews. `Previews/MVP04_LightingSettings.png` shows the controls.

## Validation

C# compilation and shader checks pass; there are no invalid mesh normals. Both side emitters contribute nonzero scattering when tested independently. Source-off rendering contributes no scattering; source intensity remains linear before tone mapping. `Previews/MVP04_PhysicsValidation.json` records the three-source, 480?300 linear HDR check with density noise temporarily disabled; all source and material settings are restored afterwards. Left/right previews and the actual Game capture verify the projections and silhouette reconstruction. The quality selector is checked with Undo/Redo. These checks cover this real-time single-scattering model, not full global illumination.
