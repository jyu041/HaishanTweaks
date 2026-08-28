# HaishanTweaks v0.10.0

## Highlights

- Adds player-configurable Enemy Density from 1x to 5x for ordinary combat-room enemies.
- Uses the game's native encounter rule, NPC initialization, AI, and room accounting path.
- Leaves bosses, elites, scripted encounters, summons, and unknown or mixed pools native.

## Installation

Install BepInEx 5.x separately, start the game once, then place `HaishanTweaks.dll` in `BepInEx\plugins\HaishanTweaks\`. Launch the game and press `F10`.

## Configuration

`EnemyDensityMultiplier` is stored in `BepInEx\config\com.jerry.haishantweaks.cfg`, defaults to `1`, and affects future ordinary enemy spawns. Existing enemies are unchanged.

## Known limitations

- Mixed or unknown monster pools are intentionally excluded.
- Elite/miniboss classification is conservative: reliable elite data excludes the entire affected pool.
- Special-room and plot-driven spawns are excluded.
- Higher density may increase rewards because duplicates are native enemy instances.
- Values above 3x may significantly reduce performance.

## Compatibility

This is an unofficial community mod for a compatible Steam version of Mirror of Heaven. It is not affiliated with the game's developers or publisher. Game updates may break compatibility.
