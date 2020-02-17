# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.6]
 - Added InstancePool, a generic Component instancing class
 - Increased initial polling time
 - Fixed Package Manager UI Extension NRE
 - Added CurveDisplay Attribute

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