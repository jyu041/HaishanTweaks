# HaishanTweaks v0.11.0

## Highlights

- Regular, elite, and boss health multipliers: `0.25x-20.00x`.
- Regular, elite, and boss outgoing damage multipliers: `0.25x-10.00x`.
- Regular enemy locomotion speed: `0.50x-3.00x`.
- Player, regular enemy, elite, and boss visual size: `0.50x-3.00x` in `0.05x` increments.

## Native boundaries

Enemy categories use hostile Npc ownership and native `UnitRank.None`, `UnitRank.Elite`, and `UnitRank.Boss`. Friendly Npcs and the controlled player are excluded. Enemy health is adjusted per Npc after native initialization and preserves the native HP fraction. Enemy calculated damage is adjusted once in `FightBody.CalculationDamage` after native mitigation and modifiers.

Regular movement changes the native `AgentAuthoring.EntitySteering.Speed` value. Elite and boss movement, animator speed, attack intervals, casting, cooldowns, and scripted timing are unchanged.

Character Size uses a safely resolved renderer/model subtree below `NpcView.ViewShow`. Gameplay roots, transforms, colliders, navigation, attack origins, camera targets, and ability anchors remain native. If no safe visual root is found, scaling is skipped and can be diagnosed with `CharacterSizeDiagnostics`.

## Configuration

Settings are stored in `BepInEx\config\com.jerry.haishantweaks.cfg` and persist across launches. Health changes apply to newly initialized enemies. Damage changes apply immediately to future calculated damage events. Character size changes update tracked active Npcs without multiplying already-scaled transforms.

## Known limitations

- Direct damage paths that bypass `FightBody.CalculationDamage` are not altered.
- Existing enemy movement settings are applied on initialization; newly spawned enemies always use current settings.
- Bosses with no safely separable visual model root remain at native size.
- The v0.10.2 zoomed-out white-rectangle/roof issue is deferred unchanged.

## Installation

Place `HaishanTweaks.dll` in `BepInEx\plugins\HaishanTweaks\`. Launch the game and press `F10`.
