# HaishanTweaks v0.12.0

## Removed Experiments

- Character Size controls and runtime model-scale tracking were removed because measured renderer bounds did not change reliably when transform scale changed.
- Enemy health, damage, and movement difficulty scaling were removed. Native enemy health, damage, and locomotion are now untouched.
- Enemy Density remains available from `1x` to `15x` with ordinary-enemy filtering and native encounter accounting.

## Extended Map Visibility

`ExtendedMapVisibility` defaults to `true` and activates only above `1.25x` during normal gameplay follow. It captures camera values per camera and scene, disables baked occlusion culling for the out-of-envelope view, and proportionally extends the native far clip and non-UI nonzero layer-cull distances. Values restore exactly at normal zoom, when disabled, during scripted/cinematic camera behavior, on scene changes, and on unload.

The conservative zoom occluder system remains intact: native `_Dither`, `LikelyZoomBlocker` fallback, textured-environment protection, floor protection, and target-end exclusion. Extended Map Visibility increases environment visibility and does not hide additional renderers.

## Diagnostics

`CameraVisibilityDiagnostics` is default-off and reports transitions only. It records native/applied far clip, occlusion-culling state, adjusted layer-cull count, and whether LOD was changed. Global LOD and lighting settings are not modified.
