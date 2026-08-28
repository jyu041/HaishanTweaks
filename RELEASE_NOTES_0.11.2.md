# HaishanTweaks v0.11.2

## Fixes

- Character Size now uses the same authoritative hostile `UnitRank` classifier as Enemy Difficulty and prioritizes `Plugin.IsPlayer`. Player, regular, elite, and boss models receive their configured multipliers instead of being reported as Unsupported.
- Category resolution retries for the same bounded initialization window when visual model creation precedes team/rank readiness.
- Floor geometry is no longer treated as a zoom occluder by the renderer-bounds fallback.

## Character Categories

The shared category mapping is `Player`, hostile `UnitRank.None` as `Regular`, hostile `UnitRank.Elite` as `Elite`, hostile `UnitRank.Boss` as `Boss`, and `Unsupported` for friendly/neutral/unknown Npcs. Visual-root discovery, model replacement handling, native baseline scale tracking, and foot compensation are preserved.

## Floor-Safe Zoom Filtering

`HideZoomOccluders` remains enabled by default above `1.25x` during normal follow. Renderer-only intersections must occur before `targetDistance - 2.0` world units. A downward ground ray protects the current floor renderer, while renderers at/below player-ground level are classified as `GroundSurface` and ignored. Roof and wall meshes remain eligible. All modified renderer/material state restores on inactivity, scene changes, scripted/cinematic cameras, and unload.

## Compatibility

No installation, commit, or push is performed. Decompiled source, game DLLs, assets, `bin`, and `obj` are not included as project inputs.
