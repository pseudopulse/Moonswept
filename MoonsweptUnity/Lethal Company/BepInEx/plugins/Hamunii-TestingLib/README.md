# TestingLib

This is a tool intended for making testing of enemy mods faster in Lethal Company. This is intended to be used in debug builds of your mods. For example:

```cs
// ... in your Plugin class:
private void Awake() {
    Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    var ExampleEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("ExampleEnemy");
    // ...
    #if DEBUG
    TestingLib.Patch.PatchAll();
    TestingLib.OnEvent.PlayerSpawn += OnEvent_PlayerSpawn;
    #endif
}

#if DEBUG
private void OnEvent_PlayerSpawn()
{
    TestingLib.Execute.ToggleTestRoom();
    TestingLib.Tools.GiveItemToSelf(TestingLib.Lookup.Item.Shovel);
    TestingLib.Tools.TeleportSelf(TestingLib.Tools.TeleportLocation.Outside);
    TestingLib.Tools.SpawnEnemyInFrontOfSelf(ExampleEnemy.enemyName);
}
#endif
// ...
```

Currently, this library is used on [DevTools](https://thunderstore.io/c/lethal-company/p/Hamunii/DevTools/) and the [experimental branch of LC-ExampleEnemy](https://github.com/Hamunii/LC-ExampleEnemy/tree/experimental).

## TestingLib Modules

### TestingLib.Patch

Contains methods that patch various things in the game.

`IsEditor()`  
Patches the game to think it is running in Unity Editor, allowing us to use the in-game debug menu.

`SkipSpawnPlayerAnimation()`  
Skips the spawn player animation so you can start moving and looking around as soon as you spawn.

`InfiniteSprint()`  
Patches the game to allow infinite sprinting by always setting SprintMeter to full.

`OnDeathHeal()`  
Instead of dying, set health to full instead.  
This helps with testing taking damage from your enemy, without death being a concern.

`MovementCheat()`  
Allows jumping at any moment and by performing a double jump, the movement will become much  
faster and a lot more responsive, and running will also increase jump height and gravity.  
**Note:** This completely overrides PlayerControllerB's `Jump_performed()` method.

`InfiniteCredits()`  
Credits get always set to `100 000 000`.

`InfiniteShotgunAmmo()`  
Skips the check for ammo when using the shotgun.

`PatchAll()`  
Calls all methods in `TestingLib.Patch`:  
`Patch.IsEditor()`  
`Patch.SkipSpawnPlayerAnimation()`  
`Patch.OnDeathHeal()`  
`Patch.MovementCheat()`  
`Patch.InfiniteSprint()`  
`Patch.InfiniteCredits()`  
`Patch.InfiniteShotgunAmmo()`  

`UnpatchAll()`  
Unpatches all applied patches.

### TestingLib.Execute

Contains actions that can be executed.

`ToggleTestRoom()`  
Toggles the testing room from the debug menu.  
Should be ran on `OnEvent.PlayerSpawn` or later.

### TestingLib.OnEvent

Contains Events that can be subscribed to.

`PlayerSpawn`  
Event for when player spawns.  
Called on `On.GameNetcodeStuff.PlayerControllerB.SpawnPlayerAnimation`.

### TestingLib.Tools

Contains helpful methods for testing.

`TeleportSelf(TeleportLocation location = 0)`  
- `TeleportLocation.Inside = 1`
- `TeleportLocation.Outside = 2`  
Teleports you to the location specified in the test level.

`TeleportSelfToEntrance()`  
Teleport yourself to entrance.

`SpawnEnemyInFrontOfSelf(string enemyName)`  
Will find the enemy by name, and spawn it.  
If name is invalid, prints all valid enemy names to console.

`GiveItemToSelf(string itemName)`  
Will find item by name, and give it to your inventory.  
If name is invalid, prints all valid item names to console.

### TestingLib.Lookup

Get the names of items and enemies in the vanilla game without having to look them up.

#### TestingLib.Lookup.Item
Names of items.
#### TestingLib.Lookup.EnemyInside
Names of inside enemies.
#### TestingLib.Lookup.EnemyOutside
Names of outside enemies.
#### TestingLib.Lookup.EnemyDaytime
Names of daytime Enemies.

### TestingLib.Enemy

Helpful methods for making debugging of enemies easier.

`DrawPath(LineRenderer line, NavMeshAgent agent)` // Consider not using this for now, as this is not an optimal solution.  
Draws the NavMeshAgent's pathfinding. Should be used in `DoAIInterval()`. Do note that you need to add line renderer in your enemy prefab. This can be done as such:
```cs
// ... in your enemy class:

#if DEBUG
LineRenderer line;
#endif

public override void Start()
{
    base.Start();
    // ...
    #if DEBUG
    line = gameObject.AddComponent<LineRenderer>();
    line.widthMultiplier = 0.2f; // reduce width of the line
    #endif
}

public override void DoAIInterval()
{
    base.DoAIInterval();
    // ...
    #if DEBUG
    StartCoroutine(TestingLib.Enemy.DrawPath(line, agent));
    #endif
}
```

## Using This For Yourself

Add this to your into your plugin class:

```diff
 [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
+[BepInDependency(TestingLib.Plugin.ModGUID, BepInDependency.DependencyFlags.SoftDependency)] 
 public class Plugin : BaseUnityPlugin {
     // ...
 }
```
Also include a reference in your csproj file:

```diff
<ItemGroup>
+  <Reference Include="TestingLib"><HintPath>./my/path/to/TestingLib.dll</HintPath></Reference>
</ItemGroup>
```
Also keep the `TestingLib.xml` file next to `TestingLib.dll` to get method documentation.

And lastly, add `TestingLib.dll` to your plugins folder.