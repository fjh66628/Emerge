# MVP04 | Nocturne Chapel

Open `Scenes/MVP04_NocturneChapel.unity` and press Play. **WASD / arrow keys** move the sprite character in four directions; the camera follows and keeps the character centred. **Space** fires the existing 3D magic bolt. The chapel reuses MVP03 movement, focus, stone PBR and pixel finishing.

## Light source and projection

There is now **one cold exterior spotlight**, positioned at **(-18, 18, 38) metres**, aimed at **(0, 4, 3)**. This is an elevated artificial source outside the front-left corner of the chapel. Its default intensity is 50000 in URP's relative light units, cone angle 75 degrees, and range 100 metres. This is not a calibrated photometric lumen value.

`ChapelWindowLight` projects the actual rose and eight side-window apertures from this source into a 1024² linear RGB cookie. Rays intersect the nearest face of the room envelope first: opaque wall, roof and rear-face rays are blocked. Glass masks include their coloured transmission and dark leading. The stone reveals, columns, tracery, pews and other geometry also occlude the light through the source's shadow map. Moving the source recomputes the projection. The old eight independent window lights and the unrelated rose directional light are removed.

The same URP light, RGB cookie and shadow map illuminate **both surfaces and air**. Window rays diverge from the source position; there is no independent beam direction, artificial widening, transmission contrast remapping, or per-window brightness. At the default position the rose beam crosses toward the right side of the nave. Side windows at grazing incidence admit less light because of their deep reveals. Windows facing away from the source do not generate direct shafts.

## Participating medium

`WindowVolumeFeature` runs a depth-limited 96-step world-space ray march before post processing. Air occupies the chapel envelope from (-8.86, 0, -13) to (8.86, 13.6, 17.57); the exterior is assumed clear.

- **Extinction:** a single density field covers the room, independent of how many lights or shadows are present. Default extinction is 0.028 per metre, scattering albedo 0.9, density noise amplitude 0.2, frequency 0.28.
- **Beer–Lambert attenuation:** `T = exp(-integral(sigma_t ds))` is evaluated along the camera path and the interior portion of the path back to the light. The light path uses six density samples; uniform density has an analytic evaluation. Unity's previous global fog is disabled to avoid applying extinction twice.
- **Incident light:** uses URP's actual inverse-square distance attenuation, finite-range taper, spotlight cone, glass transmission and geometric shadow visibility.
- **Angular scattering:** normalized Henyey–Greenstein phase function, with default `g = 0.25`.
- **Integration:** each segment adds `T_camera * albedo * (1 - T_segment) * incident_radiance * phase`, then updates camera transmittance once. No independent volume intensity gain is applied.

The implementation follows [Beer–Lambert transmittance](https://pbr-book.org/4ed/Volume_Scattering/Transmittance) and the [Henyey–Greenstein phase function](https://pbr-book.org/4ed/Volume_Scattering/Phase_Functions).

### Real-time approximations

This is single scattering with finite ray-march sampling and shadow-map resolution, not path tracing. The point emitter uses hard geometric shadows; finite source-size penumbrae are not integrated. Frosted glass uses a thin rough-pane approximation: 70% of its coloured transmission remains directional, and a complementary rough transmitted contribution shades the pane from the same source, including distance, incidence angle and cone falloff. Full angular diffusion from the glass into the room is not traced. Surface direct lighting uses URP's clear-air model; interior attenuation along the incoming path is evaluated in the volumetric integral, while the camera-path attenuation applies to the complete image. Ambient colours and local fill lights approximate bounced illumination; they do not generate additional volumetric shafts.

## Settings

Open **MVP04 > 体积光设置**.

- **外部光源:** enable, position, aim target, intensity, colour, cone angle, range and the glass's direct-transmission fraction. The source should remain outside the chapel envelope. These settings are stored in `Rendering/ExteriorLight.asset`.
- **空气介质:** extinction, scattering albedo, anisotropy and density noise, stored in `Materials/RoseWindowVolume.mat`.
- **选中光源 / 查看光路:** selects the source; Scene-view gizmos connect it to the windows.

The saved live values can differ from the builder defaults above after tuning. Controls update the scene, support Undo/Redo, and automatically save. Adjustments made during Play Mode also persist. Source strength affects surfaces and volume together. The old artistic edge/spread controls have been replaced by source geometry and physical medium parameters.

Frosted surface grain, roughness and short-range texture blur remain in `LuminousRoseGlass.mat` and `LuminousSideGlass.mat`. ACES, bloom, SSAO, focus and the subtle pixel effect are retained. `Rendering/NocturneAtmosphere.asset` and `Materials/SubtlePixels.mat` control the finishing.

## Build, upgrade and preview

- **MVP04 > Build Dark Rose Window Chapel:** regenerates the chapel and its generated assets. Existing exterior source settings are retained; medium defaults are reapplied.
- **MVP04 > Apply Physical Window Lighting:** upgrades the open MVP04 scene without rebuilding its geometry. Removes legacy sources, configures the common source, replaces the medium defaults and saves.
- **MVP04 > Add Side Stained Glass Windows:** rebuilds the side walls and apertures, reconnecting them to the common exterior source.
- **MVP04 > Apply Frosted Glass:** updates the glass materials.
- **Capture Dark Chapel Preview / Capture Side Window Preview / Capture Frosted Glass Detail:** write the corresponding images under the project-root `Previews/` directory.

`Previews/MVP04_GameView.png` records the actual Play Mode output. Always check this in addition to offscreen `Camera.Render` previews. `Previews/MVP04_LightingSettings.png` shows the controls.

## Validation

The source was switched off and mirrored from left to right in Play Mode. Shafts and surface projections disappear or move together; unlit panes lose their source-driven transmission. No shader errors or invalid mesh normals were found.

`Previews/MVP04_PhysicsValidation.json` records a 480×300 linear HDR comparison with post processing disabled and density noise temporarily set to zero. It compares source intensity 0 / 25000 / 50000 and scattering albedo 0 / 0.9, restoring all settings afterwards. The relative linearity error was approximately 2.6e-7; there were zero NaN/infinite colour components, nonzero volumetric energy with the source on, and zero scattering contribution with the source off. These checks validate the implemented single-scattering model, not full global illumination.
