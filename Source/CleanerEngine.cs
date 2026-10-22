// Zen Cleaner - Taşınabilir Sistem ve Disk Temizleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZenCleaner
{
    public class CleanCategory
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
        public long BytesFound { get; set; }
        public int FilesCount { get; set; }
        public List<string> Paths { get; set; }

        public CleanCategory(string id, string title, string desc, bool defaultSelected, params string[] paths)
        {
            Id = id;
            Title = title;
            Description = desc;
            IsSelected = defaultSelected;
            Paths = new List<string>(paths);
        }

        public string GetFormattedSize()
        {
            return CleanerEngine.FormatSize(BytesFound);
        }
    }

    public static class CleanerEngine
    {
        [DllImport("Shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);
        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        public static List<CleanCategory> GetCategories()
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string tempDir = Path.GetTempPath();

            List<CleanCategory> list = new List<CleanCategory>
            {
                new CleanCategory("temp", "🧹 Kullanıcı ve Sistem Temp Dosyaları", "Geçici uygulama ve yükleme dosyaları", true,
                    tempDir,
                    Path.Combine(winDir, "Temp")
                ),
                new CleanCategory("recycle", "🗑️ Geri Dönüşüm Kutusu", "Silinen masaüstü ve disk öğeleri", true),
                new CleanCategory("browsers", "🌐 Web Tarayıcı Önbellekleri", "Chrome, Edge, Brave, Firefox geçici önbellekleri", true,
                    Path.Combine(localApp, "Google", "Chrome", "User Data", "Default", "Cache"),
                    Path.Combine(localApp, "Microsoft", "Edge", "User Data", "Default", "Cache"),
                    Path.Combine(localApp, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Cache")
                ),
                new CleanCategory("prefetch", "📦 Windows Prefetch & Hata Günlükleri", "Eski uygulama önbellekleri ve çökme raporları", true,
                    Path.Combine(winDir, "Prefetch"),
                    Path.Combine(localApp, "CrashDumps")
                ),
                new CleanCategory("logs", "📝 Windows Güncelleme & Sistem Logları", "Eski log ve geçici kurulum kalıntıları", true,
                    Path.Combine(winDir, "Logs"),
                    Path.Combine(winDir, "SoftwareDistribution", "Download")
                )
            };

            return list;
        }

        public static void ScanCategory(CleanCategory cat)
        {
            cat.BytesFound = 0;
            cat.FilesCount = 0;

            if (cat.Id == "recycle")
            {
                // Recycle bin estimated
                cat.FilesCount = 1;
                return;
            }

            foreach (string p in cat.Paths)
            {
                if (!Directory.Exists(p)) continue;
                ScanDirRecursive(p, cat);
            }
        }

        private static void ScanDirRecursive(string dir, CleanCategory cat)
        {
            try
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string f in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(f);
                        cat.BytesFound += fi.Length;
                        cat.FilesCount++;
                    }
                    catch { }
                }

                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string d in subDirs)
                {
                    ScanDirRecursive(d, cat);
                }
            }
            catch { }
        }

        public static long CleanCategory(CleanCategory cat)
        {
            long freed = 0;

            if (cat.Id == "recycle")
            {
                try
                {
                    SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                }
                catch { }
                return cat.BytesFound;
            }

            foreach (string p in cat.Paths)
            {
                if (!Directory.Exists(p)) continue;
                freed += CleanDirRecursive(p);
            }

            return freed;
        }

        private static long CleanDirRecursive(string dir)
        {
            long freed = 0;
            try
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string f in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(f);
                        long len = fi.Length;
                        fi.Attributes = FileAttributes.Normal;
                        fi.Delete();
                        freed += len;
                    }
                    catch { }
                }

                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string d in subDirs)
                {
                    freed += CleanDirRecursive(d);
                    try
                    {
                        Directory.Delete(d, false);
                    }
                    catch { }
                }
            }
            catch { }

            return freed;
        }

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 KB";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
