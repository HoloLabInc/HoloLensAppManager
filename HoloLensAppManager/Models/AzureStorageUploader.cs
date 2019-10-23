using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

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

        public async Task<bool> Upload(Application application)
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
                var dependencyIds = new List<String>();
                foreach (var dep in application.Dependencies)
                {
                    var dependencyId = $"{appPackageName}_{dep.Name}";
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
                        DependencyIds = dependencyIds
                    };
                    TableOperation insertOperation = TableOperation.InsertOrReplace(appPackageEntity);
                    // Execute the insert operation.
                    await table.ExecuteAsync(insertOperation);
                }

                // appinfo テーブルにパッケージのデータを保存
                {
                    CloudTable appInfoTable = tableClient.GetTableReference(AppInfoTableName);
                    await appInfoTable.CreateIfNotExistsAsync();

                    var appInfoEntry = new AppInfoEntity(application.Name)
                    {
                        Description = "",
                        Developer = application.DeveloperName
                    };

                    // すでにデータが保存されているかどうかチェック
                    TableOperation retrieveOperation = TableOperation.Retrieve<AppInfoEntity>(application.Name, "");
                    TableResult retrievedResult = await appInfoTable.ExecuteAsync(retrieveOperation);
                    AppInfoEntity updateEntity = (AppInfoEntity)retrievedResult.Result;

                    if (updateEntity != null)
                    {
                        appInfoEntry.Description = updateEntity.Description;
                        appInfoEntry.AppVersions = updateEntity.AppVersions;
                    }

                    if(appInfoEntry.AppVersions == null)
                    {
                        appInfoEntry.AppVersions = new HashSet<AppVersion>();
                    }
                    appInfoEntry.AppVersions.Add(application.Version);

                    TableOperation insertOperation = TableOperation.InsertOrReplace(appInfoEntry);
                    await appInfoTable.ExecuteAsync(insertOperation);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
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

                // Construct the query operation for all customer entities where PartitionKey="Smith".
                TableQuery<AppInfoEntity> query = new TableQuery<AppInfoEntity>();//.Where(); TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Smith"));

                TableContinuationToken token = null;
                //var entities = new List<AppInfoEntity>();
                do
                {
                    var queryResult = await appInfoTable.ExecuteQuerySegmentedAsync(new TableQuery<AppInfoEntity>(), token);

                    foreach (var appEntity in queryResult.Results)
                    {
                        appInfoList.Add(appEntity.ConvertToAppInfo());
                    }
                    //entities.AddRange(queryResult.Results);
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

        public async Task<Application> Download(string appName, string version, bool useCache = true)
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
                        return null;
                    }
                    var appPackageEntity = (AppPackageEntity)retrievedResult.Result;

                    application = appPackageEntity.ConvertToApplication();
                    //var appPackageId = appPackage.AppPackageId;
                }

                // app package をダウンロード
                var localFolder = localCacheFolder;
                var appPackage = await DownloadBrob(localFolder, application.AppPackageId, useCache);
                if(appPackage == null)
                {
                    return null;
                }
                else
                {
                    application.AppPackage = appPackage;
                }

                application.Dependencies = new List<StorageFile>();
                foreach (var depId in application.DependencyIds)
                {
                    var dep = await DownloadBrob(localFolder, depId, useCache);
                    if (dep == null)
                    {
                        return null;
                    }
                    else
                    {
                        application.Dependencies.Add(dep);
                    }
                }
                return application;
            }
            catch(Exception e)
            {
                return null;
            }

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
            }catch(Exception e)
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

            /*
            public bool UploadFile(string name, StoredFile file)
            {
                throw new NotImplementedException();
            }

            public bool UploadPackageInfo(AppPackageInfo package)
            {
                throw new NotImplementedException();
            }
            */
        }

        public Task<List<AppInfo>> GetAppInfoListAsync(bool isSearching)
        {
            throw new NotImplementedException();
        }

        public Task<List<AppInfo>> SearchInAppList(string keyword)
        {
            throw new NotImplementedException();
        }

        public Task<List<AppInfo>> GetAppInfoListAsync(bool isSearching, string keyword)
        {
            throw new NotImplementedException();
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
                Versions = AppVersions
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
            return new Application()
            {
                Name = Name,
                DeveloperName = Developer,
                Version = AppVersion,
                AppPackageId = AppPackageId,
                DependencyIds = DependencyIds
            };
        }

    }
}
