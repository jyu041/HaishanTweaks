# HaishanTweaks v0.12.1

## Visibility investigation

- Added `CameraVisibilityDiagnostics`, disabled by default.
- When extended visibility activates, the diagnostic records active cameras, their component stacks, active Fog-of-War components, and native `MapHelper` view-distance values.
- Fog-of-War units and team maps are not modified because the active gameplay ownership path has not been confirmed.
- Extended visibility remains limited to normal player-follow gameplay and restores native camera values outside that scope.
