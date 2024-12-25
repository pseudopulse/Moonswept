# Dev Tools

This mod uses TestingLib to apply patches and other stuff that mod offers. Use `DevTools.cfg` for configuring which TestingLib methods this mod should call. Make sure to run the game once with this mod for that file to generate. You can also call methods from the DevTools menu that appears when `esc` or `tab` is pressed to open the quick menu.

Note that `TestingLib.xml` needs to be alongside `TestingLib.dll` for this to work (so no need to do anything other than not delete that file), as it contains method documentation and I'm too lazy to make my code not depend on having that. This mod generates the config file based on what methods it finds in `TestingLib.dll` and pulls the method documetation for it from the `xml` file. 

Also, `TestingLib.dll` itself has metadata that this mod reads to know what methods should be listed and when they can be called. This mostly means that TestingLib can be updated without updating this mod (assuming no major breaking changes of course), and you can still use the new methods with this mod.

**Note:** This mod get disabled for non-host players when Online, so not in LAN mode. Also note that not all cheats work for clients. For now, this mod does not take that into account in any way.

## Why is TestingLib separate from DevTools?

While these two projects are very much related, I wanted to make a neutral tool for testing, and that is TestingLib. It is not restricted by UI or the limitations of this mod. However, what this mod is, is an easy way to use TestingLib as if it were just a normal cheat mod.