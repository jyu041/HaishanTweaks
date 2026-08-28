# HaishanTweaks v0.12.2

## Extended Map Visibility III

- Far-clip coverage now uses the larger of proportional native coverage and native far clip plus measured camera retreat and a 10-world-unit margin.
- Nonzero non-UI layer cull distances use the same bounded retreat-aware formula.
- `CameraVisibilityDiagnostics` now reports camera coverage parameters, `layerCullSpherical`, active Fog-of-War component fields, bounded LOD-group samples, and native LOD bias.

## Fog-of-War findings

- Decompiled `FogOfWarLegacy` renders a fullscreen `Hidden/FogOfWarLegacy` pass using `_FogTex`, `_FogTextureSize`, `_MapSize`, `_MapOffset`, `_FoWInverseView`, `_FoWInverseProj`, `_OutsideFogStrength`, and `_CameraWorldPosition`.
- `fogFarPlane` only enables the shader keyword `FOGFARPLANE`; the package source does not expose a numeric far-plane distance.
- No gameplay source references the decompiled FoW components. `MapHelper.GetMaxViewDis()` has one call site in `RoomDoor`, where it clamps a UI indicator.
- FoW units, team maps, map size, map offset, outside fog strength, LOD bias, and secondary fog-camera settings are not changed.
