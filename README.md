AzureStorageDrive
====================

"Mount" an Azure Storage File service just like a native disk in PowerShell. You can access it anywhere outside the data center.


Will add Blob service support soon.

Usage
-----

With source:

>1. Build the project src/AzureStorageDrive with Visual Studio
>2. In ~\bin\Debug, edit `start.cmd` with your storage account name/key. This is one-off operation.
>3. Run `start.cmd`, and a new window will be opened at the "mounted" file service, e.g. x:\

Supported Operations
--------------------

###Navigation

>`cd <fileShare>\<directory>\<subdirectory>`

>`cd ..\<directory>`

>`cd \`

*Note:*
>you can press `<Tab>` to list files / directories in current context. Try:

>    `cd f<Tab>\h<Tab>`

>if you have a share starting with f, and a directory starting with h, they will be autocompleted.


### Create/Remove file share
>`ni .\<FileShare>` Create a new file share

>`rm .\<FileShare>` Remove a file share


### Create/Remove Directory recursively
>`mkdir .\<directory>\<subdirectory>` Create directories

>`rm .\<directory>\<subdirectory>` Remove subdirectory

>`mkdir <fileshare>\<directory>` Create file share and directory if they don't exist yet

### Create/Remove file
>`ni <filename>`  Create a zero-sized file

>`ni <filename> -Value 'Hello,Content!'` Create a file with text

>`ni <filename> -Type EmptyFile -Value 1024` Create an empty file with 1KB size

>`rm <filename>` Remove a file

### View file content
>`cat <filename>`

### Download/Upload share/directories/files recursively
>`cp <path> /d$/app` Download to local disk: `d:\app`

>`cp /d$/app <path>` Upload directory/file `d:\app` to target share/directory

### Get properties and metadata
>`gp <path>`

### Set metadata
>`sp <path> -name <name> -value <value>`
