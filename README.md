# HaishanTweaks v0.11.1

HaishanTweaks is an unofficial BepInEx 5 mod for 《海山：昆仑镜》 (Mirror of Heaven). It is intended for the game's single-player gameplay.

## Features

### Player

- Infinite Health
- Infinite MP
- No Skill Cooldowns
- Damage Multiplier
- Movement Speed Multiplier

### Ability Effects

- Ability Size Multiplier and Ability Range Multiplier
- Player Projectile Count from 1x to 10x per native projectile emission
- Optional Multishot Delay from 0 to 100 ms
- Anchor-preserving gameplay and visual scaling
- Collider dimension scaling without moving collider centers
- Visual vertical compensation for enlarged attached/forward effects
- Optional ability scaling diagnostics

### Enemies

- Enemy Density from 1x to 15x for ordinary combat-room enemies
- Separate regular, elite, and boss health and outgoing damage multipliers
- Regular enemy locomotion speed multiplier
- Visual-only player, regular enemy, elite, and boss size controls from 0.50x to 3.00x
- Hide Zoom Occluders fallback for large environment meshes without useful colliders
- Infinite Health also blocks HP-cost and sacrifice-style skill resource reductions
- Native encounter registration, AI initialization, death handling, and rewards are preserved
- Bosses, elites, special rooms, plot encounters, summons, companions, and mixed or unknown pools remain native
- Values above 3x display a performance warning

The Enemy Density control is shown in the scrollable Combat menu under `ENEMIES`.

### Starting Build / Progression

- Select, reorder, remove, or add starting skills
- Select, remove, or add starting artifacts
- Add all eligible starting skills or artifacts
- Optional high-rank starting skills and artifacts
- Add selected currencies to the current run
- Level up current artifacts
- Unlock all normal skills or artifacts
- Unlock cultivation schools
- Complete in-game achievements through the native progression path

Progression actions can permanently change a save. Back up saves before using them.

### Camera

- Camera Distance multiplier from 0.5x to 2x
- Consistent room-to-room camera zoom
- Gameplay-room camera lock support
- Protection for cinematic/scripted camera behavior
- Reduce Fog When Zoomed Out
- Reduce Blur When Zoomed Out
- Hide Zoom Occluders

The blur option reduces the game's Depth of Field effect. It does not disable all post-processing.

### UI

- Press `F10` to open or close HaishanTweaks.
- Settings persist through BepInEx configuration.
- The UI provides player, combat, run-start, currency, progression, and artifact controls.
- The Combat menu includes a scrollable `ENEMIES` section for Enemy Density.
- Ability size and range support values from 0.5x to 20x; extreme values may cause visual or performance issues.

## Installation

Requirements:

- Windows x64
- BepInEx 5.x
- A compatible Steam version of the game

1. Install BepInEx 5 into the game's directory.
2. Start the game once so BepInEx initializes.
3. Download `HaishanTweaks.dll` from GitHub Releases.
4. Create or use `BepInEx\plugins\HaishanTweaks\`.
5. Copy `HaishanTweaks.dll` into that directory.
6. Launch the game and press `F10`.

BepInEx is not bundled with this project.

## Configuration

BepInEx generates the configuration file at:

`BepInEx\config\com.jerry.haishantweaks.cfg`

The plugin GUID is `com.jerry.haishantweaks`. Settings persist through BepInEx configuration. The vertical visual compensation setting defaults to 25% of measured downward growth and is capped at 1 world unit. `MultishotDelaySeconds` defaults to 0.025 seconds; setting it to zero restores simultaneous extra-projectile spawning.

`EnemyDensityMultiplier` defaults to 1 and affects future ordinary encounter spawns only. Higher density may increase experience, currency, and drops because additional enemies are native enemy instances. Above 3x, 8x, and 12x the menu displays progressively stronger performance warnings.

Enemy difficulty settings affect hostile `UnitRank.None`, `UnitRank.Elite`, and `UnitRank.Boss` Npcs per instance. Health changes apply to newly initialized Npcs and preserve native current-HP fractions. Damage scaling is applied once at the native `FightBody.CalculationDamage` result for enemy-owned calculated damage. Regular movement uses the native `AgentAuthoring.EntitySteering.Speed` value.

Character Size changes visual model subtrees only. Gameplay roots, colliders, navigation, attack origins, camera targets, and ability anchors are not changed. Models without a safely separable visual root remain at native size.

Hide Zoom Occluders is enabled by default and activates only above 1.25x camera distance during normal gameplay follow. It tests cached environment `MeshRenderer` world bounds against the camera-to-player segment and temporarily uses `ShadowsOnly`, falling back to disabling the renderer. Infinite Health blocks negative current-HP fill changes as well as ordinary damage while allowing positive healing and native max-HP changes.

## Compatibility / Warnings

This is an unofficial community mod and is not affiliated with the game's developers or publisher. Game updates may break compatibility. Very large multipliers may cause visual or performance issues. Please report reproducible problems through GitHub Issues and include the relevant BepInEx log.

## Development

The project targets `.NET Framework 4.0` (`net40`) and is built against BepInEx `5.4.23.5`, Harmony `2.9.0.0`, and the game's Unity 2022.3-era assemblies.

Proprietary game assemblies, BepInEx binaries, Unity DLLs, and game assets are not included. To compile, point MSBuild at a legally installed copy of the game:

```powershell
dotnet build HaishanTweaks.csproj -c Release -p:GameDir="C:\Path\To\HaiShan\HaiShan"
```

`GameDir` must contain `海山_Data\Managed\Assembly-CSharp.dll` and the game's `BepInEx\core` directory. The project has a relative default for the original developer layout, but another developer should pass their own path explicitly.

## License

The original HaishanTweaks source is released under the MIT License. This license does not grant rights to game code, game assets, game trademarks, BepInEx, or Harmony.
