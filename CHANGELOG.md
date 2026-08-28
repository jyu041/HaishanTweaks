# Changelog

## 0.11.1

- Reworked character visual-root resolution using runtime renderer hierarchy discovery with bounded retries.
- Added one-shot character hierarchy diagnostics and removed unresolved-NPC log spam.
- Added throttled renderer-bounds zoom occluder fallback with `HideZoomOccluders` toggle.
- Protected HP fill-cost and sacrifice paths when Infinite Health is enabled.
- Deferred camera obstruction behavior is preserved; the new fallback only hides zoom-exposed environment meshes.

## 0.11.0

- Added persistent regular, elite, and boss health/damage scaling.
- Added regular enemy locomotion speed scaling without changing animator speed or attack timing.
- Added visual-only player and enemy character size controls with 0.05x increments.
- Added default-off enemy difficulty and character size diagnostics.
- Deferred the v0.10.2 zoom obstruction issue unchanged.

## 0.10.2

- Increased Enemy Density from 1x-5x to 1x-15x.
- Added stronger performance warnings above 3x, 8x, and 12x.
- Added conservative SkillBox coverage diagnostics; persistent attached hitboxes remain native.
- Added renderer-level fallback handling for zoomed-out obstruction hits that cannot use `_Dither`.

## 0.10.1

- Fixed Enemy Density visibility by adding a scrollable Combat menu and explicit `ENEMIES` section.
- Added coverage diagnostics for SkillBox-based player-attached hitboxes without treating them as projectiles.
- Extended zoom obstruction handling to the actual modded camera-to-player cast using the game's `_Dither` material convention.
- Added scene-aware restoration and optional zoom geometry diagnostics.

## 0.10.0

- Added conservative Enemy Density from 1x to 5x for ordinary combat-room enemy rules.
- Preserved native enemy initialization and room/wave registration.
- Excluded bosses, elites, scripted/special encounters, and mixed or unknown pools.

## 0.9.1

- Added optional non-blocking staggered spawning for additional player projectiles.
- Added a persistent `MultishotDelaySeconds` setting from 0 to 100 ms.
- Removed routine camera, fog, and Depth of Field informational log spam.

## 0.9.0

- Added conservative player-only projectile count/multishot for supported moving projectiles.
- Added small symmetric horizontal spread while preserving native projectile creation and skill costs.
- Added multishot diagnostics through `AbilityScalingDiagnostics`.

## 0.8.1

- Added conservative, bounds-based vertical compensation for enlarged attached and forward ability visuals.

## 0.8.0

- Added anchor-preserving ability size scaling.
- Fixed cursor-target displacement caused by scaling positional hierarchies.
- Scaled collider dimensions while preserving collider centers.
- Expanded supported active-skill and projectile visual scaling.

## 0.7.9

- Added zoom-aware Depth of Field reduction.

## 0.7.8

- Added gameplay-room camera lock handling.

## 0.7.7

- Stabilized the camera baseline distance.

## 0.7.x

- Added camera distance controls.
- Added ability size and range controls.
- Added fog handling when zoomed out.

## 0.5.x and earlier

- Added gameplay cheats.
- Added starting skill, artifact, and currency tools.
- Added artifact and progression utilities.

The workspace contains no Git history, so earlier milestone details are limited to the source-backed feature record above.
