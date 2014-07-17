@{

# Script module or binary module file associated with this manifest
ModuleToProcess = '.\AzureStorageDrive.dll'

# Version number of this module.
ModuleVersion = '1.0.1'

# ID used to uniquely identify this module
GUID = 'D48CF693-4125-4D2D-8790-1514F44CE325'

# Author of this module
Author = 'Leo Zhou'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '3.0'


# Minimum version of the .NET Framework required by this module
DotNetFrameworkVersion = '4.0'

# Minimum version of the common language runtime (CLR) required by this module
CLRVersion='4.0'

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @(
    'config\AzureFile.format.ps1xml'
)

TypesToProcess = 'config\AzureFile.types.ps1xml'

# Functions to export from this module
FunctionsToExport = '*'

# Cmdlets to export from this module
CmdletsToExport = '*'

# Variables to export from this module
VariablesToExport = '*'
}
