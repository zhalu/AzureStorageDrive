@echo off
REM ############################################
REM #  Do not edit this file in Visual Studio  #
REM #  If drive x: is occupied, please change  #
REM #    it to another.                        #
REM ############################################

set ROOT=%~dp0
set DRIVE=N
set ACCOUNT=
set KEY=


set ACCOUNT_Ali=
set KEY_Ali=

set S3ACCOUNT=
set S3KEY=
set S3REGION=

@echo on
powershell -noexit -ExecutionPolicy Unrestricted -command "import-module %ROOT%\GeniusDrive.psd1; New-PSDrive -name %DRIVE% -psprovider GeniusDrive -root /; %DRIVE%:; ni f -type AzureFile -value account=%ACCOUNT%`&key=%KEY%; ni ali -type AliOss -value account=%ACCOUNT_Ali%`&key=%KEY_Ali%; ni s3 -type AwsS3File -value account=%S3ACCOUNT%`&key=%S3KEY%`&region=%S3REGION%"
