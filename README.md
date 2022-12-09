# Exoscript
Scripting system for I Was a Teenage Exocolonist

## About

Author: Sarah Northway - SarahNorthway@gmail.com

I based this scripting system very loosely on Ink (https://github.com/inkle/ink) and other scripting languages. Exoscript is simpler and less robust, but easily extended and integrated into Unity. This distribution includes Unity integration and compilation for faster loading.

This version of Exoscript has been roughly ripped out of the game, and includes commented-out code for interfacing with game concepts like characters, locations and activities. Validation will fail on most Exoscript files actually used in I Was a Teenage Exocolonist, but I've included an example .EXO file which will load and run.

Article about Exoscript:
https://www.gamedeveloper.com/programming/deep-dive-the-narrative-octopus-of-i-was-a-teenage-exocolonist

## Installation

Add the Assets folder to Unity, open the sample scene and run. 

Assets\Scripts\ExoscriptTest.cs will load Assets\StreamingAssets\Stories\example.exo then execute the simpleExample story from it. Press 0-9 keys to interact.

For syntax highlighting, see ExocolonistNotepadPlusLanguage.xml for Notepad++ integration, or this project for SublimeText/tmLanguage:
https://github.com/mcgrue/exoscript-language-definition

See example.exo for examples and further documentation.

## License

Distributed under The Unlicense (public domain, use at your own risk)
This distribution does not include Exoscript files used in the game, those are copyright Northway Games Corp
