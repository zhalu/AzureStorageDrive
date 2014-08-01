GeniusDrive
====================

Most of us haven’t heard about GeniusDrive, but I guess most of us heard about Cloud Storage, especially Azure Storage. When you store your data on cloud, you get the A, B, C, D, E benefits that cloud offers to you. But the question is how you accessing those files stored on your cloud storage with a parity experience of access files stored on your local file system. Well, here we have GeniusDrive. 

GeniusDrive enables you to mount a cloud storage account as a PowerShell Drive. So you could access your cloud data resources like a local file system drive in PowerShell console. For example, if you have azure storage account “azurestore110”, you could mount the blob storage in this account as a local directory “blob1” in GeniusDrive. After that, you could navigate into this directory using “cd blob1” and browse all blobs using “ls” or “dir” just like what you would in a local folder. Sounds cool? Well, this is not the coolest. The coolest thing is that you could mount as many as storage accounts on different cloud platforms (Azure, AWS S3, Aliyun OSS) to one single GeniusDrive and accessing the resources with a uniform experience. For example, you could mount your AWS S3 storage to a directory “awss3” and then mount an Ali OSS storage to another directory “oss”. After all this is done, you could easily copy files among them using the same command which you would use when copying files from folder to folder. 

Usage
-----

If you start with source code, please first build the project `src/AzureStorageDrive` with Visual Studio.

For quickstart:

>1. In bin folder, edit `start.cmd` with your storage account name/key. This is one-off operation.
>2. Run `start.cmd`, and a new window will be opened at the genius drive, e.g. x:\. And your file service is mounted at x:\f
>3. You can mount more Azure File account with: 
  `ni <mountedLabel> -type AzureFile -value http://<account>.file.core.windows.net/?account=<account>&key=<key>`
  or Azure Blob account with:
  `ni <mountedLabel> -type AzureBlob -value http://<account>.blob.core.windows.net/?account=<account>&key=<key>`
  

In your other PowerShell session, you can follow these steps:

>1. `import-module <path>\GeniusDrive.psd1`
>2. `New-PSDrive -name <DriveLabelToMountAt, e.g. x> -psprovider GeniusDrive -root /;`
>3. `cd <DriveLabelToMountAt, e.g. x>:`
>4. `ni <mountedLabel> -type AzureFile -value http://<account>.file.core.windows.net/?account=<account>&key=<key>`
>5. `ni <mountedLabel> -type AzureBlob -value http://<account>.blob.core.windows.net/?account=<account>&key=<key>`

Supported Operations
--------------------
mount Azure Blob, Azure File, AWS S3, Aliyun OSS as a directory in Genius Drive.

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
