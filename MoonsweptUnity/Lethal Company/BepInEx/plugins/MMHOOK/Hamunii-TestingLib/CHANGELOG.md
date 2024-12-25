## 1.2.0

Changes:
- More attribute options for DevTools
- Implemented `TeleportSelfToEntrance()` (thanks Nyxchrono!)
- Deprecate `RunAllPatchAndExecuteMethods()`

Fixes:
- `UnpatchAll()` now unpatches `IsEditor()` 

## 1.1.0

Changes:
- Attributes for DevTools, so that DevTools knows which methods it should call and when

Other:
- Initial release on Thunderstore

## 1.0.1

Fixes:
- Fix bug with teleporting inside the facility and not registering that the player is in the facility, partly breaking enemy AI

## 1.0.0

Changes:
- TestingLib is structured a bit differently
- Added various patch methods, changed how some existing methods worked

Fixes:
- Fixed bug with "grab" text appearing with objects with gotten with GiveItemToSelf()

## 0.2.0

Changes:
- Enemy names must now be exact for spawing them
- Implement GiveItemToSelf()
- Made TestingLib.Lookup, can be used for getting the names of vanilla items and enemies

Fixes:
- Invisible enemies are no longer a thing


## 0.1.0

- Initial release of TestingLib