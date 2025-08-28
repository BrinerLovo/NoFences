using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Model
{
    public class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private const string MetaFileName = "__fence_metadata.xml";

        private readonly string basePath;

        public List<FenceWindow> Fences { get; } = new List<FenceWindow>();

        public FenceManager()
        {
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoFences");
            EnsureDirectoryExists(basePath);
        }

        public void LoadFences()
        {
            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var metaFile = Path.Combine(dir, MetaFileName);

                // check if the meta file exists
                if (!File.Exists(metaFile))
                    continue;

                var serializer = new XmlSerializer(typeof(FenceInfo));
                var reader = new StreamReader(metaFile);
                var fence = serializer.Deserialize(reader) as FenceInfo;
                reader.Close();

                var instance = new FenceWindow(fence);
                Fences.Add(instance);
                instance.Show();
            }
        }

        public void AddNewFence(FenceWindow fence)
        {
            Fences.Add(fence);
        }

        public void RemoveFence(FenceWindow fence)
        {
            Fences.Remove(fence);
        }

        public void CreateFence(string name)
        {
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = name,
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };

            UpdateFence(fenceInfo);
            new FenceWindow(fenceInfo).Show();
        }

        public void RemoveFence(FenceInfo info, FenceWindow window)
        {
            // Use the window's fenceFolderPath property to get the correct path
            string folderPath = !string.IsNullOrEmpty(info.CustomFolderPath) 
                ? info.CustomFolderPath 
                : Path.Combine(basePath, info.Id.ToString());
                
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            RemoveFence(window);
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            // Use custom folder path if specified, otherwise use default
            string path = !string.IsNullOrEmpty(fenceInfo.CustomFolderPath) 
                ? fenceInfo.CustomFolderPath 
                : Path.Combine(basePath, fenceInfo.Id.ToString());
            EnsureDirectoryExists(path);

            var metaFile = Path.Combine(path, MetaFileName);
            var serializer = new XmlSerializer(typeof(FenceInfo));
            var writer = new StreamWriter(metaFile);
            serializer.Serialize(writer, fenceInfo);
            writer.Close();
        }

        private void EnsureDirectoryExists(string dir)
        {
            var di = new DirectoryInfo(dir);
            if (!di.Exists)
                di.Create();
        }

        private string GetFolderPath(FenceInfo fenceInfo)
        {
            // Use custom folder path if specified, otherwise use default
            return !string.IsNullOrEmpty(fenceInfo.CustomFolderPath) 
                ? fenceInfo.CustomFolderPath 
                : Path.Combine(basePath, fenceInfo.Id.ToString());
        }
    }
}
