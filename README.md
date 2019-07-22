# ArchersRally Build Apprentice

[![Build status](https://ci.appveyor.com/api/projects/status/2e4uwppscxw1cnb5?svg=true)](https://ci.appveyor.com/project/CharlesWillis3/archersrally-apprentice)

<!-- Update the VS Gallery link after you upload the VSIX-->
<!-- Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/[GuidFromGallery]) -->
Get the [CI build](http://vsixgallery.com/extension/ArchersRally.Apprentice.Charles%20Willis.439b61e2-6855-404e-9e21-ffbbf61d44bf/).

---------------------------------------

Useful IDE tools for MSBuildUseful tools for handling 

See the [change log](CHANGELOG.md) for changes and road map.

## Features

- Solution Imports Watcher

### Solution Imports Watcher
Have a complex and super-custom build, with lots of special .props and .targets? Ever find yourself in Visual Studio, editing some of the build properties, and wishing VS would know it needs to re-load the solution? With Solution Imports Watcher, the first tool in the ArchersRally Apprentice extension, now VS will know!

With this feature enabled, when you open a solution it will read all of the `<Import>` elements from each of the projects in the solution. It will then monitor each of the imported files for writes, and when any of the imports changes, VS will prompt you to re-load the solution.

A list of all the files being watched can be found in the Watched Imports virtual folder in Solution Explorer.

To enable this feature, go to `Tools -> Options -> ArchersRally -> Apprentice'.

## Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)
