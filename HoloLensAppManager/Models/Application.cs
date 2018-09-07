using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace HoloLensAppManager.Models
{
    public class AppInfo
    {
        public string Name;
        public string DeveloperName;
        public string Description;
        public HashSet<AppVersion> Versions = new HashSet<AppVersion>();
    }

    // バージョンごとのアプリケーション
    public class Application
    {
        public string DeveloperName;
        public string Name;
        public AppVersion Version;

        // Azure 上のファイルを示すときは、AppPackage を null, AppPackageId にファイル情報を入れる
        public string AppPackageId;
        public StorageFile AppPackage;

        public List<string> DependencyIds;
        public List<StorageFile> Dependencies;

        public bool IsValid
        {
            get
            {
                return !String.IsNullOrEmpty(DeveloperName)
                    && !String.IsNullOrEmpty(Name)
                    && Version.IsValid
                    && AppPackage != null;
            }
        }
    }

    public class AppVersion : IComparable
    {
        public uint? Version1 { set; get; }
        public uint? Version2 { set; get; }
        public uint? Version3 { set; get; }
        public uint? Version4 { set; get; }

        public bool IsValid
        {
            get
            {
                return Version1.HasValue
                    && Version2.HasValue
                    && Version3.HasValue
                    && Version4.HasValue;

            }
        }

        public string Display
        {
            get
            {
                return ToString(".");
            }
        }

        public AppVersion()
        {

        }

        public AppVersion(string version, char splitter = '.')
        {
            string[] words = version.Split(splitter);

            for (var i = 0; i < words.Length; i++)
            {
                int.TryParse(words[i], out int versionNum);

                switch (i)
                {
                    case 0:
                        Version1 = (uint)versionNum;
                        break;
                    case 1:
                        Version2 = (uint)versionNum;
                        break;
                    case 2:
                        Version3 = (uint)versionNum;
                        break;
                    case 3:
                        Version4 = (uint)versionNum;
                        break;
                }
            }
            foreach (var word in words)
            {
                System.Console.WriteLine($"<{word}>");
            }
        }

        public string ToString(string splitter = ".")
        {
            return $"{Version1.Value}{splitter}{Version2.Value}{splitter}{Version3.Value}{splitter}{Version4.Value}";
        }

        public override int GetHashCode()
        {
            return Version1.GetHashCode() * 13 + Version2.GetHashCode() * 17 + Version3.GetHashCode() * 19 + Version4.GetHashCode() * 23;
        }

        public override bool Equals(object obj)
        {
            // asでは、objがnullでも例外は発生せずにnullが入ってくる
            var other = obj as AppVersion;
            if (other == null)
            {
                return false;
            }

            // 何が同じときに、「同じ」と判断してほしいかを記述する
            return Version1 == other.Version1
                && Version2 == other.Version2
                && Version3 == other.Version3
                && Version4 == other.Version4;
        }

        public int CompareTo(object obj)
        {
            var other = obj as AppVersion;
            if (other == null)
            {
                return 1;
            }

            if (Version1 != other.Version1)
            {
                return (int)(Version1 - other.Version1);
            }
            if (Version2 != other.Version2)
            {
                return (int)(Version2 - other.Version2);
            }
            if (Version3 != other.Version3)
            {
                return (int)(Version3 - other.Version3);
            }
            if (Version4 != other.Version4)
            {
                return (int)(Version4 - other.Version4);
            }
            return 0;
        }
    }
}
