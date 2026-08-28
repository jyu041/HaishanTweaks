# HaishanTweaks v0.11.3

## Character Scale

Character visual baselines are captured once per visual-root instance. Slider changes and temporary renderer/equipment changes never become a new native scale. The selected visual root is restored to its captured native scale and position before each multiplier application, and measured native/scaled renderer bounds are recorded by `CharacterSizeDiagnostics` without per-frame log spam.

## Zoom Occluders

The successful white-blocker and floor protections remain active. Renderer-only fallback now protects ordinary textured environment materials and only considers likely blocker meshes: untextured, light/simple, large, thin meshes that intersect the camera-to-player segment before the 2-unit target exclusion zone. Native `_Dither` obstruction handling remains preferred. Floors and protected current-ground renderers are ignored and restored immediately if classification changes.

## Metadata

The BepInEx plugin declaration, startup log, and UI title all report `0.11.3`.

## Compatibility

No installation, commit, or push is performed. Decompiled source, game DLLs, assets, `bin`, and `obj` are not included as project inputs.
