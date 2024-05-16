# VSQuickOpenFile

Quickly display a list of files in your solution for opening. Supports searching by filename or path. Functions just like `Shift`+`Alt`+`O` in Visual Assist.

![Quick Open File](SharedContent/openfileinsolution.png?raw=true "Quick Open File")

This extension will parse the projects in your solution and look for files. Once it has built a list of these files, it allows you to search through them for the file you want to open and open it quickly. It supports searching for pieces of the filename in any order and can easily highlight multiple files with just the keyboard to open all at once.

## Usage
- Use the "Open File In Solution" entry at the top of the Tools menu.
- Set a key binding for Tools.OpenFileInSolution 

## Installation
[Download](https://marketplace.visualstudio.com/items?itemName=PerniciousGames.OpenFileInSolution) this extension for Visual Studio 2012, 2013, 2015, 2017, 2019 from the Visual Studio Marketplace.

[Download](https://marketplace.visualstudio.com/items?itemName=PerniciousGames.OpenFileInSolution2022) this extension for Visual Studio 2022 from the Visual Studio Marketplace.

## Future improvements:
- Need to cache the list of files in the solution so it doesn't have to re-parse them every time. This means updating the cached list when new files/projects are added or loaded.
- More options for controlling how files are listed and searched, such as the ability to only search through open files. 

