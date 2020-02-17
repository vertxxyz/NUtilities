# NUtilities
A collection of various tools and utilities for Unity.

## 🚨 WORK IN PROGRESS 🚨
This is a platform for adding utilities to Unity as I see fit.

## Contents:
##### Utilities:
- [Material Property Remapper](https://github.com/vertxxyz/NUtilities/wiki/MaterialPropertyRemapper)
- [Package Updater](https://github.com/vertxxyz/NUtilities/wiki/Package-Updater) (Requires [Newtonsoft Json](http://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest))
- [Code Generation](https://github.com/vertxxyz/NUtilities/wiki/Code-Generation)

##### Editor Helper Classes:
- EditorUtils
- EditorGUIExtensions
- StyleExtensions

##### Runtime Helper Classes:
- Instance Pool
- Proportional Values

##### Property Attributes:
- [EnumToValue](https://github.com/vertxxyz/NUtilities/wiki/EnumToValue)
- EnumFlags
- CurveDisplay
- ReadOnly
- EditorOnly
- Keycode
- MinMax
- Progress
- Blend2D
- File

##### UIElement Controls:
- Drag and Drop Box
- Help Box

##### Improved Inspectors:
 - Animator
 - Scriptable Object

----
[Utilities relating to timeline](https://github.com/vertxxyz/NTimeline) have their own package separate to this.

Other important tools I have made may remain separate to this:
- [NTexturePreview](https://github.com/vertxxyz/NTexturePreview) - Enhanced Texture Previewer for Unity
- [NSelection](https://github.com/vertxxyz/NSelection) - Simple selection for busy scenes in Unity

----
## Usage:
Open utility windows under: **Window/Vertx/...**

See the [wiki](https://github.com/vertxxyz/NUtilities/wiki) for explanations of each utility.

## Installation
Edit your `manifest.json` file to contain `"com.vertx.nutilities": "https://github.com/vertxxyz/NUtilities.git",`,
or if using 2019.3< the **+** button in **Window/Package Manager**, and *Add package from Git URL.*

To update the package with new changes, remove the lock from the bottom of the `manifest.json` file.
Or add the package to a [Package Updater](https://github.com/vertxxyz/NUtilities/wiki/Package-Updater) Scriptable Object and update via the interface.