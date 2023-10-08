UPDATE (v1.7.1): Assembly definitions have been disabled by default. To use assembly definitions remove the .bak extension from the 
.asmdef file. This will have to be done outside of Unity.

----

Since all the NWH assets have been updated to use assembly definitions here is a disclaimer to avoid confusion when updating: 

This asset uses Assembly Definition (.asmdef) files. There are many benefits to assembly definitions but a downside is that 
the whole project needs to use them or they should not be used at all.

  * If the project already uses assembly definitions accessing a script that belongs to this asset can be done by adding an reference to 
  the assembly definition of the script that needs to reference the asset. E.g. to access VehicleController adding a 
  NWH.VehiclePhysics2.VehicleController reference to MyProject.asmdef is required.

  * If the project does not use assembly definitions simply remove all the .asmdef files from the asset after import.

Using LogitechSDK (which does not fature assembly definitions) will therefore require an addition of .asmdef file inside the LogitechSDK 
directory and a reference inside NWH.VehiclePhysics2.VehicleController or removal of all .asmdef files from the asset if you do not 
wish to use assembly definitions.

More about Assembly Definitions: https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html