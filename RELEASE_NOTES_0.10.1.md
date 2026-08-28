# HaishanTweaks v0.10.1

## Highlights

- Makes Enemy Density visible in the scrollable Combat menu under `ENEMIES`.
- Adds conservative zoomed-out obstruction cleanup using the native scene-tag and `_Dither` material convention.
- Adds projectile coverage diagnostics for player-attached SkillBox hitboxes without duplicating movement or AoE effects.

## Installation

Install BepInEx 5.x separately, start the game once, then place `HaishanTweaks.dll` in `BepInEx\plugins\HaishanTweaks\`. Launch the game and press `F10`.

## Configuration

Enemy density remains `EnemyDensityMultiplier`, integer `1` to `5`, in `BepInEx\config\com.jerry.haishantweaks.cfg`. `CameraGeometryDiagnostics` is default-off and can log candidate renderer details for zoomed-out obstruction investigation.

## Known limitations

- Projectile Count continues to support standard discrete moving projectiles; SkillBox-only movement and attached hitbox effects are not projectiles.
- Homing and unsafe projectile categories remain conservative/native.
- Zoom obstruction cleanup only affects non-Standard materials with the game's `_Dither` property and colliders tagged `Scene` while normal follow is zoomed beyond 1.25x.

## Compatibility

This is an unofficial community mod for a compatible Steam version of Mirror of Heaven. It is not affiliated with the game's developers or publisher. Game updates may break compatibility.
