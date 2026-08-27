# HaishanTweaks v0.8.1

## Highlights

- Adds subtle bounds-based vertical compensation for enlarged sword, slash, and other attached/forward visual effects.
- Preserves ability anchors, cursor targets, projectile trajectories, and gameplay collider centers.
- Includes persistent ability compensation configuration, defaulting to 25% with a 1-unit cap.

## Installation

Install BepInEx 5.x separately, start the game once, then place the release artifact `HaishanTweaks.dll` in `BepInEx\plugins\HaishanTweaks\`. Launch the game and press `F10`.

## Controls

`F10` opens or closes the HaishanTweaks menu. Configuration persists in `BepInEx\config\com.jerry.haishantweaks.cfg`.

## Known limitations

- Some custom passive, artifact, impact, and explosion visuals do not have reliable player-ability provenance and are not automatically compensated.
- Ground-anchored effects, cursor-ground effects, and projectile trajectories are intentionally excluded from vertical compensation.
- Very large ability multipliers may cause visual or performance issues.

## Compatibility

This is an unofficial community mod for a compatible Steam version of Mirror of Heaven. It is not affiliated with the game's developers or publisher. Game updates may break compatibility.
