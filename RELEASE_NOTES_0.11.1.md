# HaishanTweaks v0.11.1

## Fixes

- Character Size now discovers the runtime renderer hierarchy instead of requiring direct hard-coded bone names. Model appearance can be resolved after initialization using bounded retries for up to three seconds.
- Added one-shot hierarchy diagnostics for unresolved models and removed repeated failure logging.
- Added a renderer-bounds zoom fallback for large environment `MeshRenderer` surfaces without useful colliders.
- Infinite Health now blocks negative HP fill/resource changes used by HP-cost and sacrifice-style abilities.

## Character Size

Player and enemy size remains visual-only. The resolver finds the common ancestor of runtime character renderers below `NpcView.ViewShow`, avoids controller/collider roots, captures native scale and position, and applies a bounds-based visual foot lift. If no safe root exists after retry, the model remains native size.

## Zoom Occluders

`HideZoomOccluders` defaults to `true`. Above `1.25x`, during normal player-follow gameplay, cached environment mesh renderers are tested with `Bounds.IntersectRay` against the camera-to-player segment. Obstructing renderers use `ShadowsOnly` or are disabled if necessary. State is restored when zoom hiding is inactive, the scene changes, scripted/cinematic camera mode is active, or the plugin unloads.

## Infinite Health

Negative player HP changes through `AddHP`, HP fill costs, and direct HP fill resource paths are blocked while Infinite Health is enabled. Positive healing and native maximum-HP changes remain allowed. HP-cost skills continue their normal execution path.

## Compatibility

No game assets, decompiled source, or proprietary binaries are included. No installation, commit, or push is performed by the build process.
