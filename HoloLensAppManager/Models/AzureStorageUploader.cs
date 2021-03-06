﻿using HoloLensAppManager.Helpers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;

namespace HoloLensAppManager.Models
{
    class AzureStorageUploader : IUploader
    {
        CloudStorageAccount storageAccount;
        const string PackageContainerName = "apppackages";
        const string AppInfoTableName = "appinfo";

        CloudBlobClient blobClient;
        CloudTableClient tableClient;
        private StorageFolder localCacheFolder;

        public AzureStorageUploader()
        {
            var settings = ResourceLoader.GetForCurrentView("settings");
            var connectionString = settings.GetString("AZURE_STORAGE_CONNECTION_STRING");
            storageAccount = CloudStorageAccount.Parse(connectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            tableClient = storageAccount.CreateCloudTableClient();

            localCacheFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<(AppInfo appInfo, UploadStatusType status)> Upload(Application application)
        {
            try
            {
                // Blob にパッケージを保存
                CloudBlobContainer container = blobClient.GetContainerReference(PackageContainerName);
                await container.CreateIfNotExistsAsync();

                var appPackageName = GetAppPackageName(application);
                var appPackageId = $"{appPackageName}_{application.AppPackage.Name}";
                CloudBlockBlob blockBlob_upload = container.GetBlockBlobReference(appPackageId);
                await blockBlob_upload.UploadFromFileAsync(application.AppPackage);

                // 依存ファイルを保存
                var dependencyIds = new List<string>();
                foreach (var dep in application.Dependencies)
                {
                    var parent = System.IO.Directory.GetParent(dep.Path);
                    var architecture = SupportedArchitectureHelper.StringToSupportedArchitectureType(parent.Name);
                    var dependencyId = $"{appPackageName}_{architecture}_{dep.Name}";
                    dependencyIds.Add(dependencyId);
                    CloudBlockBlob depBlockBlob = container.GetBlockBlobReference(dependencyId);
                    await depBlockBlob.UploadFromFileAsync(dep);
                }

                // Table にバージョンごとのパッケージのデータを保存
                {
                    CloudTable table = tableClient.GetTableReference(PackageContainerName);
                    await table.CreateIfNotExistsAsync();

                    // Create the TableOperation object that inserts the customer entity.
                    var appPackageEntity = new AppPackageEntity(application.Name, application.Version.ToString())
                    {
                        Developer = application.DeveloperName,
                        Name = application.Name,
                        AppVersion = application.Version,
                        AppPackageId = appPackageId,
                        DependencyIds = dependencyIds,
                        SupportedArchitecture = application.SupportedArchitecture
                    };
                    TableOperation insertOperation = TableOperation.InsertOrReplace(appPackageEntity);
                    // Execute the insert operation.
                    await table.ExecuteAsync(insertOperation);
                }

                var isNewlyUploaded = true;
                AppInfo appInfo;

                // appinfo テーブルにパッケージのデータを保存
                {
                    CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);
                    await appInfoTable.CreateIfNotExistsAsync();

                    // SupportedArchitecture は最新のものに設定
                    var appInfoEntry = new AppInfoEntity(application.Name)
                    {
                        Description = "",
                        Developer = application.DeveloperName,
                        SupportedArchitecture = application.SupportedArchitecture
                    };

                    // すでにデータが保存されているかどうかチェック
                    TableOperation retrieveOperation = TableOperation.Retrieve<AppInfoEntity>(application.Name, "");
                    TableResult retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                    AppInfoEntity updateEntity = (AppInfoEntity)retrievedResult.Result;

                    if (updateEntity == null)
                    {
                        appInfoEntry.CreateAt = DateTime.Now;
                    }
                    else
                    {
                        appInfoEntry.CreateAt = updateEntity.CreateAt;
                        appInfoEntry.Description = updateEntity.Description;
                        appInfoEntry.AppVersions = updateEntity.AppVersions;
                        isNewlyUploaded = false;
                    }

                    if(appInfoEntry.AppVersions == null)
                    {
                        appInfoEntry.AppVersions = new HashSet<AppVersion>();
                    }
                    appInfoEntry.AppVersions.Add(application.Version);

                    TableOperation insertOperation = TableOperation.InsertOrReplace(appInfoEntry);
                    await appInfoTable.ExecuteAsync(insertOperation);

                    appInfo = appInfoEntry.ConvertToAppInfo();
                }

                if (isNewlyUploaded)
                {
                    return (appInfo, UploadStatusType.NewlyUploaded);
                }
                else
                {
                    return (appInfo, UploadStatusType.Updated);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return (null, UploadStatusType.NetworkError);
            }
        }

        public async Task<bool> Delete(AppInfo appInfo)
        {
            // appinfo テーブルにパッケージのデータを保存
            try
            {
                CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);

                // すでにデータが保存されているかどうかチェック
                TableOperation retrieveOperation = TableOperation.Retrieve<AppInfoEntity>(appInfo.Name, "");
                TableResult retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                AppInfoEntity deleteAppInfoEntity = (AppInfoEntity)retrievedResult.Result;

                if (deleteAppInfoEntity == null)
                {
                    return false;
                }

                // バージョンごとに削除
                if (deleteAppInfoEntity.AppVersions != null)
                {
                    foreach (var version in deleteAppInfoEntity.AppVersions)
                    {
                        await DeleteApplication(appInfo.Name, version.ToString());
                    }
                }

                retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                deleteAppInfoEntity = (AppInfoEntity)retrievedResult.Result;

                if (deleteAppInfoEntity == null)
                {
                    return false;
                }
                // AppInfo を削除
                TableOperation deleteOperation = TableOperation.Delete(deleteAppInfoEntity);
                await appInfoTable.ExecuteAsync(deleteOperation);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task<bool> UpdateAppInfo(AppInfo appInfo)
        {
            try
            {
                CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);

                // すでにデータが保存されているかどうかチェック
                TableOperation retrieveOperation = TableOperation.Retrieve<AppInfoEntity>(appInfo.Name, "");
                TableResult retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                AppInfoEntity appInfoEntry = (AppInfoEntity)retrievedResult.Result;

                if (appInfoEntry == null)
                {
                    return false;
                }
                else
                {
                    appInfoEntry.Description = appInfo.Description;
                    appInfoEntry.Developer = appInfo.DeveloperName;

                    TableOperation insertOperation = TableOperation.InsertOrReplace(appInfoEntry);
                    var result = await appInfoTable.ExecuteAsync(insertOperation);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public async Task<List<AppInfo>> GetAppInfoListAsync()
        {
            var appInfoList = new List<AppInfo>();

            try
            {
                CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);

                TableQuery<AppInfoEntity> query = new TableQuery<AppInfoEntity>();

                TableContinuationToken token = null;
                do
                {
                    var queryResult = await appInfoTable.ExecuteQuerySegmentedAsync(new TableQuery<AppInfoEntity>(), token);

                    foreach (var appEntity in queryResult.Results)
                    {
                        appInfoList.Add(appEntity.ConvertToAppInfo());
                    }
                    token = queryResult.ContinuationToken;
                } while (token != null);

            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
            return appInfoList;
        }

        public async Task<bool> DeleteApplication(string appName, string version)
        {
            try
            {
                Application application;
                // Application の情報を取得
                {
                    CloudTable table = tableClient.GetTableReference(PackageContainerName);
                    TableOperation retrieveOperation = TableOperation.Retrieve<AppPackageEntity>(appName, version);

                    // Execute the retrieve operation.
                    TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

                    // Print the phone number of the result.
                    if (retrievedResult.Result == null)
                    {
                        return false;
                    }
                    var appPackageEntity = (AppPackageEntity)retrievedResult.Result;
                    application = appPackageEntity.ConvertToApplication();
                }


                // appinfo テーブルからパッケージの情報を削除
                {
                    CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);

                    // すでにデータが保存されているかどうかチェック
                    TableOperation retrieveOperation = TableOperation.Retrieve<AppInfoEntity>(application.Name, "");
                    TableResult retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                    AppInfoEntity appInfoEntity = (AppInfoEntity)retrievedResult.Result;

                    if (appInfoEntity != null)
                    {
                        appInfoEntity.AppVersions?.Remove(application.Version);
                        TableOperation insertOperation = TableOperation.InsertOrReplace(appInfoEntity);
                        await appInfoTable.ExecuteAsync(insertOperation);
                    }
                }

                // バージョンごとのパッケージのデータを削除
                {
                    CloudTable appPackageTable = tableClient.GetTableReference(PackageContainerName);

                    TableOperation retrieveOperation = TableOperation.Retrieve<AppPackageEntity>(application.Name, application.Version.ToString());
                    TableResult retrievedResult = await appPackageTable.ExecuteAsync(retrieveOperation);
                    AppPackageEntity appPackageEntity = (AppPackageEntity)retrievedResult.Result;

                    if (appPackageEntity != null)
                    {
                        TableOperation deleteOperation = TableOperation.Delete(appPackageEntity);
                        await appPackageTable.ExecuteAsync(deleteOperation);
                    }
                }

                // app package を削除
                var appPackage = await DeleteBrob(application.AppPackageId);

                foreach (var depId in application.DependencyIds)
                {
                    var dep = await DeleteBrob(depId);
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private async Task<bool> DeleteBrob(string filename)
        {
            try
            {
                CloudBlobContainer container = blobClient.GetContainerReference(PackageContainerName);
                CloudBlockBlob blockBlob_remove = container.GetBlockBlobReference(filename);
                //storageFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                //await blockBlob_download.DownloadToFileAsync(storageFile);
                await blockBlob_remove.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<(Application app, DownloadErrorType error)> Download(string appName, string version, SupportedArchitectureType desirableArchitecture, bool useCache = true)
        {
            try
            {
                Application application;

                // Application の情報を取得
                {
                    CloudTable table = tableClient.GetTableReference(PackageContainerName);

                    TableOperation retrieveOperation = TableOperation.Retrieve<AppPackageEntity>(appName, version);

                    // Execute the retrieve operation.
                    TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

                    // Print the phone number of the result.
                    if (retrievedResult.Result == null)
                    {
                        return (null, DownloadErrorType.UnknownError);
                    }
                    var appPackageEntity = (AppPackageEntity)retrievedResult.Result;

                    application = appPackageEntity.ConvertToApplication();
                }

                // app package をダウンロード
                var localFolder = localCacheFolder;
                var appPackage = await DownloadBrob(localFolder, application.AppPackageId, useCache);
                if(appPackage == null)
                {
                    return (null, DownloadErrorType.NetworkError);
                }
                else
                {
                    application.AppPackage = appPackage;
                }

                // インストールするアプリのアーキテクチャタイプを決定
                var appSupportedArchitecture = application.SupportedArchitecture;
                SupportedArchitectureType installArchitecture =
                    DecideInstallArchitecture(appSupportedArchitecture, desirableArchitecture);

                if(installArchitecture == SupportedArchitectureType.None)
                {
                    return (null, DownloadErrorType.NotSupportedArchitecture);
                }

                application.Dependencies = new List<StorageFile>();
                foreach (var depId in application.DependencyIds)
                {
                    // 指定されたアーキテクチャの依存ファイルのみをダウンロード
                    var pattern = @"^.+_(\w+)_([\w\.]+)$";
                    var match = Regex.Match(depId, pattern);

                    if (!match.Success)
                    {
                        continue;
                    }

                    var architectureString = match.Groups[1].Value;

                    var depArchitecture = SupportedArchitectureHelper.StringToSupportedArchitectureType(architectureString);
                    if(depArchitecture != installArchitecture)
                    {
                        continue;
                    }

                    var dep = await DownloadBrob(localFolder, depId, useCache);
                    if (dep == null)
                    {
                        return (null, DownloadErrorType.NetworkError);
                    }
                    else
                    {
                        application.Dependencies.Add(dep);
                    }
                }
                return (application, DownloadErrorType.NoError);
            }
            catch(Exception e)
            {
                return (null, DownloadErrorType.NetworkError);
            }
        }

        private SupportedArchitectureType DecideInstallArchitecture(SupportedArchitectureType appSupportedArchitecture, SupportedArchitectureType desirableArchitecture)
        {
            var architectureOrder = new SupportedArchitectureType[]
            {
                SupportedArchitectureType.Arm64,
                SupportedArchitectureType.Arm,
                SupportedArchitectureType.X64,
                SupportedArchitectureType.X86
            };

            foreach(var architecture in architectureOrder)
            {
                if(appSupportedArchitecture.HasFlag(architecture) && desirableArchitecture.HasFlag(architecture))
                {
                    return architecture;
                }
            }
            return SupportedArchitectureType.None;
        }

        async Task<StorageFile> DownloadBrob(StorageFolder folder, string filename, bool useCache = true)
        {
            StorageFile storageFile = null;

            try
            {
                // ローカルキャッシュをチェック
                var storageItem = await folder.TryGetItemAsync(filename);
                if (storageItem != null)
                {
                    if (storageItem is StorageFolder)
                    {
                        await storageItem.DeleteAsync();
                    }
                    else
                    {
                        storageFile = (StorageFile)storageItem;
                    }
                }

                // ダウンロード
                if (storageFile == null)
                {
                    CloudBlobContainer container = blobClient.GetContainerReference(PackageContainerName);
                    CloudBlockBlob blockBlob_download = container.GetBlockBlobReference(filename);
                    storageFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    await blockBlob_download.DownloadToFileAsync(storageFile);
                }
                return storageFile;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public async Task<bool> ClearCache()
        {
            try
            {
                var files = await localCacheFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    await file.DeleteAsync(StorageDeleteOption.Default);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        string GetAppPackageName(Application app)
        {
            return $"{app.Name}_{app.Version.ToString(".")}";
        }
    }

    public class AppInfoEntity : TableEntity
    {
        public AppInfoEntity(string name)//{, string version)
        {
            this.PartitionKey = name;
            this.RowKey = "";
        }

        public AppInfoEntity() { }

        public string Description { set; get; }
        public string Developer { set; get; }

        public DateTime CreateAt { set; get; }

        #region SupportedArchecture
        // for application
        public SupportedArchitectureType SupportedArchitecture
        {
            get => (SupportedArchitectureType)supportedArchtecture;
            set => supportedArchtecture = (int)value;
        }

        // for table store
        public int supportedArchtecture { get; set; }
        #endregion

        private HashSet<AppVersion> appVersions;
        private string appVersionsString;

        // This is for access by your application, which will work with the array.
        public HashSet<AppVersion> AppVersions
        {
            get
            {
                return appVersions;
            }
            set
            {
                appVersions = value;

                // This check is necessary to make sure we preserve "null" as different than "empty"
                if (appVersions != null)
                {
                    //appVersionsString = appVersions.Aggregate((a, b) => { return (a.ToString() + "," + b.ToString()); });
                    var sb = new StringBuilder();
                    foreach (var v in appVersions) {
                        sb.Append(v.ToString());
                        sb.Append(",");
                    }

                    //末尾の , を削除
                    if (sb.Length > 1)
                    {
                        sb.Length -= 1;
                    }
                    appVersionsString = sb.ToString();
                }
                else
                {
                    appVersionsString = "";
                }
            }
        }

        // This is for storing in table storage
        public string AppVersionsString
        {
            get
            {
                // Just return the private string, which is updated by both setters
                //return appVersionsString;
                if (appVersions != null)
                {
                    //appVersionsString = appVersions.Aggregate((a, b) => { return (a.ToString() + "," + b.ToString()); });
                    var sb = new StringBuilder();
                    foreach (var v in appVersions)
                    {
                        sb.Append(v.ToString());
                        sb.Append(",");
                    }

                    //末尾の , を削除
                    if (sb.Length > 1)
                    {
                        sb.Length -= 1;
                    }
                    appVersionsString = sb.ToString();
                }
                else
                {
                    appVersionsString = "";
                }
                return appVersionsString;
            }
            set
            {
                // Simple assignment of the string read from table storage
                appVersionsString = value;

                // Initialize the array by converting the string.  Make sure that null and empty are treated correctly.
                if (!String.IsNullOrEmpty(appVersionsString))
                {
                    //appVersions = new AppVersion(appVersionString);
                    appVersions = new HashSet<AppVersion>();
                    string[] versions = appVersionsString.Split(',');
                    foreach (var v in versions)
                    {
                        appVersions.Add(new AppVersion(v));
                    }
                }
                else
                {
                    appVersions = new HashSet<AppVersion>();
                }
            }
        }

        public AppInfo ConvertToAppInfo()
        {
            return new AppInfo()
            {
                Name = PartitionKey,
                DeveloperName = Developer,
                Description = Description,
                Versions = AppVersions,
                SupportedArchitecture = SupportedArchitecture,
                CreateAt = CreateAt,
                UpdateAt = Timestamp.DateTime,
            };
        }
    }


    public class AppPackageEntity : TableEntity
    {
        public AppPackageEntity(string name, string version)
        {
            this.PartitionKey = name;
            this.RowKey = version;
        }

        public AppPackageEntity() { }

        public string Developer { get; set; }

        public string Name { get; set; }

        #region SupportedArchecture
        // for application
        public SupportedArchitectureType SupportedArchitecture
        {
            get => (SupportedArchitectureType)supportedArchtecture;
            set => supportedArchtecture = (int)value;
        }

        // for table store
        public int supportedArchtecture { get; set; }
        #endregion

        private AppVersion appVersion;
        private string appVersionString;

        // This is for access by your application, which will work with the array.
        public AppVersion AppVersion
        {
            get
            {
                return appVersion;
            }
            set
            {
                appVersion = value;

                // This check is necessary to make sure we preserve "null" as different than "empty"
                if (appVersion != null)
                {
                    appVersionString = value.ToString(".");
                }
                else
                {
                    // Assign null to the string to preserve the fact that the array was null
                    appVersionString = null;
                }
            }
        }

        // This is for storing in table storage
        public string AppVersionString
        {
            get
            {
                // Just return the private string, which is updated by both setters
                return appVersionString;
            }
            set
            {
                // Simple assignment of the string read from table storage
                appVersionString = value;

                // Initialize the array by converting the string.  Make sure that null and empty are treated correctly.
                if (!String.IsNullOrEmpty(appVersionString))
                {
                    appVersion = new AppVersion(appVersionString);
                }
                else
                {
                    appVersion = null;
                }
            }
        }

        public string AppPackageId { get; set; }

        private List<string> dependencyIds;
        private string dependencyIdsString;

        // This is for access by your application, which will work with the array.
        public List<string> DependencyIds
        {
            get
            {
                return dependencyIds;
            }
            set
            {
                dependencyIds = value;

                // This check is necessary to make sure we preserve "null" as different than "empty"
                if (dependencyIds != null)
                {
                    //dependencyIdsString = dependencyIds.Aggregate((a, b) => a + "," + b);
                    var sb = new StringBuilder();
                    foreach (var id in dependencyIds)
                    {
                        sb.Append(id);
                        sb.Append(",");
                    }

                    //末尾の , を削除
                    if (sb.Length > 1)
                    {
                        sb.Length -= 1;
                    }
                    dependencyIdsString = sb.ToString();

                }
                else
                {
                    // Assign null to the string to preserve the fact that the array was null
                    dependencyIdsString = null;
                }
            }
        }

        // This is for storing in table storage
        public string DependencyIdsString
        {
            get
            {
                // Just return the private string, which is updated by both setters
                return dependencyIdsString;
            }
            set
            {
                // Simple assignment of the string read from table storage
                dependencyIdsString = value;

                // Initialize the array by converting the string.  Make sure that null and empty are treated correctly.
                if (!String.IsNullOrEmpty(dependencyIdsString))
                {
                    dependencyIds = new List<string>();
                    string[] dependencies = dependencyIdsString.Split(',');
                    foreach(var dep in dependencies)
                    {
                        dependencyIds.Add(dep);
                    }
                }
                else
                {
                    dependencyIds = null;
                }
            }
        }

        public Application ConvertToApplication()
        {
            var dependencyIds = DependencyIds;
            if (dependencyIds == null)
            {
                dependencyIds = new List<string>();
            }

            return new Application()
            {
                Name = Name,
                DeveloperName = Developer,
                Version = AppVersion,
                AppPackageId = AppPackageId,
                DependencyIds = dependencyIds,
                SupportedArchitecture = SupportedArchitecture
            };
        }

    }
}
