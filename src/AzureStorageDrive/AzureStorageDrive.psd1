@{

# Script module or binary module file associated with this manifest
ModuleToProcess = '.\AzureStorageDrive.dll'

# Version number of this module.
ModuleVersion = '0.1.0'

# ID used to uniquely identify this module
GUID = 'D48CF693-4125-4D2D-8790-1514F44CE325'

# Author of this module
Author = 'Leo Zhou'

# Company or vendor of this module
CompanyName = 'Leo Zhou'

# Copyright statement for this module
Copyright = ''  

# Description of the functionality provided by this module
Description = ''

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '3.0'

# Name of the Windows PowerShell host required by this module
PowerShellHostName = ''

# Minimum version of the Windows PowerShell host required by this module
PowerShellHostVersion = ''

# Minimum version of the .NET Framework required by this module
DotNetFrameworkVersion = '4.0'

# Minimum version of the common language runtime (CLR) required by this module
CLRVersion='4.0'

# Processor architecture (None, X86, Amd64, IA64) required by this module
ProcessorArchitecture = 'None'

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module
ScriptsToProcess = @()

# Type files (.ps1xml) to be loaded when importing this module
TypesToProcess = @(
)

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @(
    'AzureStorageDrive.format.ps1xml'
)

# Modules to import as nested modules of the module specified in ModuleToProcess
NestedModules = '.\Microsoft.WindowsAzure.Commands.SqlDatabase.dll',
                '.\Microsoft.WindowsAzure.Commands.ServiceManagement.dll',
                '.\Microsoft.WindowsAzure.Commands.Storage.dll'

# Functions to export from this module
FunctionsToExport = '*'

# Cmdlets to export from this module
CmdletsToExport = '*'

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module
AliasesToExport = @(

)

# List of all modules packaged with this module
ModuleList = @()

# List of all files packaged with this module
FileList =  @()

# Private data to pass to the module specified in ModuleToProcess
PrivateData = ''

}
