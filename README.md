# HoloLensAppManager
HoloLensAppManager is a UWP application for sharing and installing HoloLens application packages.

## Setup
### Copy template files
- Copy `HoloLensAppManager\Properties\settings.resw.template` to `HoloLensAppManager\Properties\settings.resw`
- Copy `HoloLensAppManager\Package.appxmanifest.template` to `HoloLensAppManager\Package.appxmanifest`
- Copy `HoloLensAppManager\AssetsTemplate` to `HoloLensAppManager\Assets`

### Create Azure Storage Account
![Create Storage Account](https://github.com/HoloLabInc/HoloLensAppManager/blob/images/images/2018-09-08-15-27-05.png)

In the Account Kind dropdown list, select StorageV2 (general purpose v2).

After your storage account is created, you can get connection string.

![Connection String](https://github.com/HoloLabInc/HoloLensAppManager/blob/images/images/2018-09-08-15-33-17.png)

### Write connection string in setting.resw
Open `HoloLensAppManager.sln` in Visual Stduio.  
Open `Properties > setting.resw`.  
Copy connection string and paste it into AZURE_STORAGE_CONNECTION_STRING.

![Create Storage Account](https://github.com/HoloLabInc/HoloLensAppManager/blob/images/images/2018-09-08-15-51-30.png)

## Enable USB Connection
In order to install applications to HoloLens via USB connection, you need to enable local loopback.

Open command prompt and run the following command.  
Package family name is shown in `Package.appxmanifest`.

```bat
> checknetisolation loopbackexempt -a -n=<package family name>
```

## License
MIT
