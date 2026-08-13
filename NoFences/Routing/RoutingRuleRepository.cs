using NoFences.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Routing
{
    internal sealed class RoutingRuleRepository
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<RoutingRule>));
        private readonly string filePath;

        public RoutingRuleRepository(string dataDirectoryPath)
        {
            filePath = Path.Combine(dataDirectoryPath, "routing_rules.xml");
        }

        public List<RoutingRule> Load()
        {
            if (!File.Exists(filePath))
                return new List<RoutingRule>();
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return Serializer.Deserialize(stream) as List<RoutingRule> ?? new List<RoutingRule>();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                AppLogger.Error("Unable to load routing rules.", ex);
                return new List<RoutingRule>();
            }
        }

        public void Save(IReadOnlyList<RoutingRule> rules)
        {
            string temporaryPath = filePath + ".tmp";
            string backupPath = filePath + ".bak";
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    Serializer.Serialize(stream, new List<RoutingRule>(rules));
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Replace(temporaryPath, filePath, backupPath, true);
                    }
                    catch (IOException)
                    {
                        File.Copy(filePath, backupPath, true);
                        File.Copy(temporaryPath, filePath, true);
                    }
                }
                else
                    File.Move(temporaryPath, filePath);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temporaryPath, filePath, true);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
