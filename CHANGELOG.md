# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.3.0]
 - Bumped version to 2019.1
 - Moved Vertx.Extensions to Vertx.Editor.Extensions
 - Instance Pool
    - Added warm-up methods
 - Action Window
 	- Animation - Repair Animation Paths in Animation
 	- Component - Remove Missing Components in Project
 	- Fixed non-centered screen position
 - Tests
 	- AnimationReferenceTests
 		- Currently just object paths, will be improved in the future
 	- MissingBehaviourTests
 		- UnityEvent
 		- Components
 - EnumToValue
 	- Alt-Click expand
 - EditorUtils
 	- BuildSceneScope, scope to iterate over all build scenes.
 	- GetPathForObject has moved from ReferenceTests to here
 - Added DebugUtils, a class for drawing complex wireframes for debugging
    - BoxCast
    - SphereCast
    - RaycastHit array
 - Fixed multi-editing for custom inspectors

## [1.2.0]
 - Unified all utility classes to use the Utils suffix
 - Asset List
 	- TSV Export
 	- Optional Path column
 	- General fixes and improvements
 - Action Window
 	- Material cleanup, removes unused properties from materials.
 	- Trim GameObject Names Of Duplicate Indices (removes the (1) from duplicated gameobjects)
 - ScriptableObjectInspector
 	- Search optionally includes inherited types
 - Tests
 	- ObjectReferenceTests now includes the scene in the log

## [1.1.0]
 - Added Asset List, a window utility for managing and creating lists of asset types.
 - Added UXML support to DragAndDropBox
 - Fixes for asset loading in EditorUtils.
 - Fixes for EnumFlagsAttribute Drawer
 - EnumFlags are now nicified.

## [1.0.6]
 - Added InstancePool, a generic Component instancing class
 - Increased initial polling time
 - Fixed Package Manager UI Extension NRE
 - Added CurveDisplay Attribute
 - Package Updater's dependency on Newtonsoft.Json is optional and requires the package to be manually installed.

## [1.0.5]
 - Package Updater changes:
 	- No longer polls in Play Mode
 	- Added Package Manager UI extension
 	- Disabled automatic polling
 	- Added setting for auto-update
 - EnumToValue has tooltips
 - Blend2D property attribute

## [1.0.4]
 - Added git support to Package Updater
 - Dependency on Newtonsoft.Json
 - Minor fixes and optimisations

## [1.0.3]
 - Added EnumToValue and EnumToValueDictionary
 - Added an Inspector for the Animator that helps selecting humanoid bones
 - Added many Property Drawers/Attributes:
 	- ReadOnly, disabled always.
 	- EditorOnly, disabled at runtime.
 	- EnumFlags, improved inspector for displaying a small combination of enum flags. Names are actually listed instead of saying mixed.
 	- Keycode, displays a small button that can be used to record a key binding.
 	- MinMax, shows a min-max slider. Apply to one float, hide the following float in the inspector.
 	- Progress, shows a progress bar for a float.
 - Added tests for Missing Object references in scenes and assets. Define VERTX_INCLUDE_TESTS to use.
 - Added various utility APIs for editor and IMGUI.
 - Added Package Updater. A ScriptableObject you can use to define packages that you wish to be automatically checked for updates. This does not yet support packages from git repos.

## [1.0.2]
 - Early for CodeGenerator and Material Property Remapper

## [1.0.1]
 - Added Code Generator
 - Fixes for Material Property Remapper

## [1.0.0]
 - Initial release
 - Added Material Property Remapper
 - Added HelpBox, DragAndDropBox, and StyleExtensions