# HaishanTweaks v0.10.2

## Highlights

- Enemy Density now supports integer values from 1x to 15x.
- Added high-density warnings at 4x, 9x, and 13x.
- Extended zoom obstruction cleanup to ditherless and Standard-shader obstruction renderers using renderer-level fallback suppression.
- Added diagnostics distinguishing SkillBox-attached hitboxes from supported projectile emissions.

## Installation

Install BepInEx 5.x separately, start the game once, then place `HaishanTweaks.dll` in `BepInEx\plugins\HaishanTweaks\`. Launch the game and press `F10`.

## Configuration

`EnemyDensityMultiplier` is stored in `BepInEx\config\com.jerry.haishantweaks.cfg`, defaults to `1`, and supports integer values from `1` to `15`. It affects future ordinary encounter waves only.

## Known limitations

- Bosses, elites, scripted/special encounters, mixed pools, and unknown pools remain native.
- Persistent player-attached hitboxes, auras, and movement contact colliders are not treated as multishot projectiles.
- Homing and unsafe projectile categories remain conservative/native.
- Very high enemy densities and large ability multipliers may significantly reduce performance.

## Compatibility

This is an unofficial community mod for a compatible Steam version of Mirror of Heaven. It is not affiliated with the game's developers or publisher. Game updates may break compatibility.
