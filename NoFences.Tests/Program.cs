using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace NoFences.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("same-fence reorder preserves every item", TestReorder);
            Run("collision naming handles files and folders", TestCollisionNaming);
            Run("physical fence moves are safe and collision-aware", TestPhysicalFenceMoves);
            Run("folder sync reconciles content and skips metadata", TestFolderSync);
            Run("corrupt metadata recovers from atomic backup", TestMetadataBackupRecovery);
            Run("fence deletion preserves linked content", TestNonDestructiveFenceDeletion);
            Run("thumbnail scaling preserves bounds", TestThumbnailScaling);

            Console.WriteLine(failures == 0
                ? "All NoFences regression tests passed."
                : $"{failures} NoFences regression test(s) failed.");
            return failures == 0 ? 0 : 1;
        }

        private static void TestReorder()
        {
            var paths = new List<string> { "A", "B", "C" };
            Assert(FenceItemOrder.TryMove(paths, "A", 2, out int originalIndex), "Move should change order.");
            Assert(originalIndex == 0, "Original index should be retained for Undo.");
            AssertSequence(paths, "B", "C", "A");
            Assert(paths.Count == 3, "Reorder must not remove an item.");

            Assert(!FenceItemOrder.TryMove(paths, "C", 1, out _), "Same-slot drop should be a no-op.");
            AssertSequence(paths, "B", "C", "A");
        }

        private static void TestCollisionNaming()
        {
            string root = CreateTemporaryDirectory();
            try
            {
                string file = Path.Combine(root, "report.txt");
                File.WriteAllText(file, "existing");
                string fileCandidate = PathUtil.GetUniqueDestinationPath(file, root, false);
                Assert(Path.GetFileName(fileCandidate) == "report (2).txt", "File suffix should preserve extension.");

                string folder = Path.Combine(root, "Archive");
                Directory.CreateDirectory(folder);
                string folderCandidate = PathUtil.GetUniqueDestinationPath(folder, root, true);
                Assert(Path.GetFileName(folderCandidate) == "Archive (2)", "Folder suffix should be collision-safe.");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static void TestFolderSync()
        {
            string root = CreateTemporaryDirectory();
            string externalRoot = CreateTemporaryDirectory();
            try
            {
                string currentFile = Path.Combine(root, "Current.txt");
                string folder = Path.Combine(root, "Folder");
                string metadata = Path.Combine(root, "__fence_metadata.xml");
                string backup = Path.Combine(root, "__fence_metadata.xml.bak");
                string regularXml = Path.Combine(root, "Document.xml");
                string stale = Path.Combine(root, "Missing.txt");
                string external = Path.Combine(externalRoot, "External.txt");
                File.WriteAllText(currentFile, "content");
                Directory.CreateDirectory(folder);
                File.WriteAllText(metadata, "metadata");
                File.WriteAllText(backup, "backup");
                File.WriteAllText(regularXml, "content");
                File.WriteAllText(external, "external");

                var displayed = new List<string> { stale, external };
                FenceFolderSyncResult result = FenceFolderSynchronizer.Synchronize(displayed, root);

                Assert(result.AddedPaths.Count == 3, "Sync should add ordinary files and folders.");
                Assert(result.RemovedPaths.Count == 1 && result.RemovedPaths[0] == stale, "Sync should remove stale linked entries.");
                Assert(displayed.Contains(external), "Sync must preserve unrelated external references.");
                Assert(!displayed.Contains(metadata) && !displayed.Contains(backup), "Metadata must never become an icon.");
                Assert(displayed.Contains(regularXml), "Ordinary XML content must not be mistaken for fence metadata.");
            }
            finally
            {
                Directory.Delete(root, true);
                Directory.Delete(externalRoot, true);
            }
        }

        private static void TestPhysicalFenceMoves()
        {
            string sourceRoot = CreateTemporaryDirectory();
            string fenceRoot = CreateTemporaryDirectory();
            try
            {
                string sourceFile = Path.Combine(sourceRoot, "Item.txt");
                string existingFile = Path.Combine(fenceRoot, "Item.txt");
                File.WriteAllText(sourceFile, "moved");
                File.WriteAllText(existingFile, "existing");

                Assert(FenceFileMover.TryMove(sourceFile, fenceRoot, out string movedFile, out _),
                    "A valid file move should succeed.");
                Assert(Path.GetFileName(movedFile) == "Item (2).txt", "File collisions should receive a suffix.");
                Assert(File.Exists(movedFile) && !File.Exists(sourceFile), "The file must be physically moved.");

                string sourceFolder = Path.Combine(sourceRoot, "Folder");
                Directory.CreateDirectory(sourceFolder);
                File.WriteAllText(Path.Combine(sourceFolder, "Child.txt"), "child");
                Assert(FenceFileMover.TryMove(sourceFolder, fenceRoot, out string movedFolder, out _),
                    "A valid folder move should succeed.");
                Assert(Directory.Exists(movedFolder) && !Directory.Exists(sourceFolder),
                    "The folder must be physically moved.");

                Assert(!FenceFileMover.TryMove(Path.Combine(sourceRoot, "Missing.txt"), fenceRoot, out _, out _),
                    "A missing source must be rejected.");
                Assert(!FenceFileMover.TryMove(fenceRoot, Path.Combine(fenceRoot, "Nested"), out _, out _),
                    "Moving a folder into its own descendant must be rejected.");
            }
            finally
            {
                Directory.Delete(sourceRoot, true);
                Directory.Delete(fenceRoot, true);
            }
        }

        private static void TestNonDestructiveFenceDeletion()
        {
            string linkedFolder = CreateTemporaryDirectory();
            string contentPath = Path.Combine(linkedFolder, "keep.txt");
            File.WriteAllText(contentPath, "keep");
            var info = new FenceInfo(Guid.NewGuid())
            {
                Name = "Safety test",
                CustomFolderPath = linkedFolder
            };

            FenceManager.Instance.UpdateFence(info);
            FenceManager.Instance.RemoveFence(info, null);

            Assert(File.Exists(contentPath), "Deleting a fence must preserve linked content.");
            Directory.Delete(linkedFolder, true);
        }

        private static void TestMetadataBackupRecovery()
        {
            string linkedFolder = CreateTemporaryDirectory();
            var info = new FenceInfo(Guid.NewGuid())
            {
                Name = "Original",
                CustomFolderPath = linkedFolder
            };

            try
            {
                FenceManager.Instance.UpdateFence(info);
                info.Name = "Current";
                FenceManager.Instance.UpdateFence(info);

                string metadataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NoFences",
                    "Metadata",
                    info.Id.ToString("D"));
                File.WriteAllText(Path.Combine(metadataDirectory, "__fence_metadata.xml"), "not xml");

                FenceInfo recovered = FenceManager.LoadFenceMetadata(metadataDirectory);
                Assert(recovered != null, "Backup recovery should return metadata.");
                Assert(recovered.Name == "Original", "Recovery should use the last valid backup.");
            }
            finally
            {
                FenceManager.Instance.RemoveFence(info, null);
                Directory.Delete(linkedFolder, true);
            }
        }

        private static void TestThumbnailScaling()
        {
            Assert(ThumbnailProvider.GetScaledSize(400, 200, 32, 32) == new System.Drawing.Size(32, 16), "Wide image scaling failed.");
            Assert(ThumbnailProvider.GetScaledSize(200, 400, 32, 32) == new System.Drawing.Size(16, 32), "Tall image scaling failed.");
        }

        private static string CreateTemporaryDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "NoFences.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL: " + name + " - " + ex.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertSequence(List<string> actual, params string[] expected)
        {
            Assert(actual.Count == expected.Length, "Sequence length differs.");
            for (int index = 0; index < expected.Length; index++)
                Assert(actual[index] == expected[index], $"Unexpected item at index {index}.");
        }
    }
}
