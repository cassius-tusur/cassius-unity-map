# Map-Unity

The Unity package is in `src/Packages/com.ang-gpo.map.unity`. It uses precompiled managed DLLs, so changing a submodule does not automatically change the code Unity loads.

Initialize dependencies with `git submodule update --init --recursive`, then run `bash refresh-dependencies.sh` to rebuild and copy the DLLs from the pinned Map and ProceduralGenerators revisions. The script needs the .NET SDK and the projects' existing NuGet dependencies. Review the resulting changes, including DLLs and Unity `.meta` files, and run the package's editor tests in a Unity project before committing.

The `../Map` and `../ProceduralGenerators` submodule URLs expect sibling repositories. Update `.gitmodules` when remote repositories become available.
