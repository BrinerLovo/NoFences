using NoFences.Model;
using NoFences.History;
using NoFences.Interaction;
using NoFences.Layout;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;

namespace NoFences.Tests
{
    internal static class Program
    {
        private static int failures;

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 2 && string.Equals(args[0], "--render-settings", StringComparison.OrdinalIgnoreCase))
                return RenderSettingsPreviews(args[1]);

            Run("same-fence reorder preserves every item", TestReorder);
            Run("batch reorder preserves selection order", TestBatchReorder);
            Run("layout collapses missing entries without deleting metadata", TestMissingEntryLayout);
            Run("multi-selection supports toggle and range selection", TestMultiSelection);
            Run("fence sorting supports name, type, date, and custom order", TestSorting);
            Run("undo restores complete item snapshots", TestUndoSnapshots);
            Run("repository isolates and recovers fence metadata", TestRepository);
            Run("collision naming handles files and folders", TestCollisionNaming);
            Run("directory normalization preserves filesystem roots", TestRootPathNormalization);
            Run("physical fence moves are safe and collision-aware", TestPhysicalFenceMoves);
            Run("fence folder migration moves files and folders safely", TestFenceFolderMigration);
            Run("fence settings normalization repairs invalid metadata", TestFenceSettingsNormalization);
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

        private static void TestBatchReorder()
        {
            var paths = new List<string> { "A", "B", "C", "D", "E" };
            Assert(FenceItemOrder.TryMoveMany(paths, new[] { "B", "D" }, 5),
                "Moving a selection should change order.");
            AssertSequence(paths, "A", "C", "E", "B", "D");

            Assert(!FenceItemOrder.TryMoveMany(paths, new[] { "B", "D" }, 5),
                "Dropping an already-adjacent selection in place should be a no-op.");
            AssertSequence(paths, "A", "C", "E", "B", "D");

            var pathsWithHiddenEntry = new List<string> { "Missing", "A", "B", "C" };
            Assert(FenceItemOrder.TryMoveMany(
                    pathsWithHiddenEntry,
                    new[] { "A", "B", "C" },
                    new[] { "C" },
                    0),
                "Visible items should reorder around hidden metadata entries.");
            AssertSequence(pathsWithHiddenEntry, "Missing", "C", "A", "B");
        }

        private static void TestMissingEntryLayout()
        {
            string root = CreateTemporaryDirectory();
            try
            {
                string missing = Path.Combine(root, "Missing.txt");
                string first = Path.Combine(root, "First.txt");
                string second = Path.Combine(root, "Second.txt");
                File.WriteAllText(first, "first");
                File.WriteAllText(second, "second");

                var source = new List<string> { missing, first, second };
                var layout = new FenceLayout();
                FenceLayoutSnapshot snapshot = layout.CreateSnapshot(
                    source,
                    FenceSortMode.Custom,
                    false,
                    300,
                    240,
                    35,
                    0);

                Assert(source.Count == 3 && source[0] == missing,
                    "Layout must not delete unavailable metadata entries.");
                AssertSequence(snapshot.OrderedPaths, first, second);
                Assert(snapshot.Items.Count == 2, "Only renderable entries should reserve grid cells.");
                Assert(snapshot.Items[0].Bounds.Top == 35 + FenceLayout.ItemPadding,
                    "The first visible entry should occupy the first grid cell.");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static void TestMultiSelection()
        {
            var controller = new FenceDragDropController();
            string[] order = { "A", "B", "C", "D" };
            controller.Select("B", order, false, false);
            controller.Select("D", order, true, false);
            AssertSequence(controller.GetSelectedInDisplayOrder(order), "B", "D");

            controller.Select("C", order, false, true);
            AssertSequence(controller.GetSelectedInDisplayOrder(order), "C", "D");

            controller.SelectAll(order);
            Assert(controller.SelectedPaths.Count == 4, "Select all should select every displayed item.");
            var remaining = new List<string>(order);
            Assert(controller.RemoveSelected(remaining, order) == 4 && remaining.Count == 0,
                "Batch removal should remove every selected item exactly once.");

            DataObject dragData = controller.CreateDragData(order, "A");
            controller.MarkHandledByFence(dragData, new[] { "A", "C" });
            AssertSequence(controller.GetPathsHandledByFence(dragData), "A", "C");
        }

        private static void TestSorting()
        {
            string root = CreateTemporaryDirectory();
            try
            {
                string folder = Path.Combine(root, "Folder");
                string alphaText = Path.Combine(root, "Alpha.txt");
                string betaZip = Path.Combine(root, "Beta.zip");
                Directory.CreateDirectory(folder);
                File.WriteAllText(alphaText, "alpha");
                File.WriteAllText(betaZip, "beta");
                File.SetLastWriteTimeUtc(alphaText, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(betaZip, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                Directory.SetLastWriteTimeUtc(folder, new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                string[] custom = { betaZip, folder, alphaText };

                AssertSequence(FenceLayout.GetOrderedPaths(custom, FenceSortMode.Custom, false), custom);
                AssertSequence(FenceLayout.GetOrderedPaths(custom, FenceSortMode.Name, false), alphaText, betaZip, folder);
                AssertSequence(FenceLayout.GetOrderedPaths(custom, FenceSortMode.Type, false), folder, alphaText, betaZip);
                AssertSequence(FenceLayout.GetOrderedPaths(custom, FenceSortMode.Date, false), folder, alphaText, betaZip);
                AssertSequence(FenceLayout.GetOrderedPaths(custom, FenceSortMode.Date, true), betaZip, alphaText, folder);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static void TestUndoSnapshots()
        {
            var history = new FenceUndoManager(2);
            var items = new List<string> { "A", "B" };
            history.Record("remove item", items);
            items.Remove("A");
            Assert(history.CanUndo && history.NextDescription == "remove item", "Undo should expose its next action.");
            Assert(history.TryUndo(items), "Undo should restore a recorded snapshot.");
            AssertSequence(items, "A", "B");
            Assert(!history.TryUndo(items), "Consumed undo entries should not run twice.");
        }

        private static void TestRepository()
        {
            string root = CreateTemporaryDirectory();
            try
            {
                var repository = new FenceRepository(root);
                var fence = new FenceInfo(Guid.NewGuid()) { Name = "First" };
                repository.Save(fence);
                fence.Name = "Second";
                repository.Save(fence);

                string metadataDirectory = Path.Combine(root, "Metadata", fence.Id.ToString("D"));
                File.WriteAllText(Path.Combine(metadataDirectory, "__fence_metadata.xml"), "invalid");
                IReadOnlyList<FenceInfo> loaded = repository.LoadAll();
                Assert(loaded.Count == 1 && loaded[0].Name == "First",
                    "Repository loading should recover the last valid atomic backup.");

                repository.Delete(fence);
                Assert(!Directory.Exists(metadataDirectory), "Deleting metadata should remove its empty repository directory.");
            }
            finally
            {
                Directory.Delete(root, true);
            }
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

        private static void TestRootPathNormalization()
        {
            string root = Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()));
            Assert(PathUtil.NormalizeDirectoryPath(root) == root, "A drive root must not become drive-relative.");
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

        private static void TestFenceFolderMigration()
        {
            string sourceRoot = CreateTemporaryDirectory();
            string destinationRoot = CreateTemporaryDirectory();
            try
            {
                string sourceFile = Path.Combine(sourceRoot, "Item.txt");
                string sourceFolder = Path.Combine(sourceRoot, "Folder");
                string metadata = Path.Combine(sourceRoot, "__fence_metadata.xml");
                File.WriteAllText(sourceFile, "source");
                Directory.CreateDirectory(sourceFolder);
                File.WriteAllText(Path.Combine(sourceFolder, "Nested.txt"), "nested");
                File.WriteAllText(metadata, "metadata");
                File.WriteAllText(Path.Combine(destinationRoot, "Item.txt"), "existing");

                Assert(FenceFolderMigration.TryMoveContents(
                        sourceRoot,
                        destinationRoot,
                        out FenceFolderMigrationResult result,
                        out _),
                    "Folder migration should succeed.");
                Assert(result.MovedPaths.Count == 2, "Migration should move the ordinary file and folder.");
                Assert(File.Exists(Path.Combine(destinationRoot, "Item (2).txt")), "Migration should resolve file collisions.");
                Assert(Directory.Exists(Path.Combine(destinationRoot, "Folder")), "Migration should move subfolders.");
                Assert(File.Exists(metadata), "Migration should leave legacy metadata behind.");
            }
            finally
            {
                Directory.Delete(sourceRoot, true);
                Directory.Delete(destinationRoot, true);
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
                Assert(FenceManager.LoadFenceMetadata(metadataDirectory)?.Name == "Original",
                    "Backup recovery should repair the primary metadata file.");
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

        private static void TestFenceSettingsNormalization()
        {
            var info = new FenceInfo(Guid.NewGuid())
            {
                Name = "   ",
                Width = -20,
                Height = 0,
                TitleHeight = 500,
                Files = new List<string> { " A ", "a", null, "" },
                WatchedExtensions = new List<string> { "PDF", ".pdf", " zip " }
            };

            SettingsValidator.NormalizeFence(info);
            Assert(info.Name == "Fence", "An empty fence name should receive a safe default.");
            Assert(info.Width >= SettingsValidator.MinimumFenceWidth, "Fence width should be clamped.");
            Assert(info.Height >= SettingsValidator.MinimumFenceHeight, "Fence height should be clamped.");
            Assert(info.TitleHeight == SettingsValidator.MaximumTitleHeight, "Title height should be clamped.");
            Assert(info.Files.Count == 1 && info.Files[0] == "A", "Fence paths should be trimmed and de-duplicated.");
            AssertSequence(info.WatchedExtensions, ".PDF", ".zip");
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

        private static void AssertSequence(IReadOnlyList<string> actual, params string[] expected)
        {
            Assert(actual.Count == expected.Length, "Sequence length differs.");
            for (int index = 0; index < expected.Length; index++)
                Assert(actual[index] == expected[index], $"Unexpected item at index {index}.");
        }

        private static int RenderSettingsPreviews(string outputDirectory)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Directory.CreateDirectory(outputDirectory);

            using (var globalSettings = new SettingsWindow())
            {
                globalSettings.Show();
                Application.DoEvents();
                SaveFormPreview(globalSettings, Path.Combine(outputDirectory, "global-settings.png"));
                ListBox navigation = FindControl<ListBox>(globalSettings);
                navigation.SelectedIndex = 1;
                Application.DoEvents();
                SaveFormPreview(globalSettings, Path.Combine(outputDirectory, "global-settings-behavior.png"));
                navigation.SelectedIndex = 2;
                Application.DoEvents();
                SaveFormPreview(globalSettings, Path.Combine(outputDirectory, "global-settings-appearance.png"));
                globalSettings.Close();
            }

            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = "Projects",
                Width = 320,
                Height = 300,
                WatchedExtensions = new List<string> { ".pdf", ".zip" }
            };
            using (var fenceSettings = new FenceSettingsWindow(fenceInfo, Path.Combine(outputDirectory, "Projects")))
            {
                fenceSettings.Show();
                Application.DoEvents();
                SaveFormPreview(fenceSettings, Path.Combine(outputDirectory, "fence-settings.png"));
                FlowLayoutPanel scroller = FindControl<FlowLayoutPanel>(fenceSettings);
                scroller.AutoScrollPosition = new Point(0, 420);
                Application.DoEvents();
                SaveFormPreview(fenceSettings, Path.Combine(outputDirectory, "fence-settings-lower.png"));
                fenceSettings.Close();
            }

            return 0;
        }

        private static void SaveFormPreview(Form form, string outputPath)
        {
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }

        private static T FindControl<T>(WinFormsControl root) where T : WinFormsControl
        {
            if (root is T match)
                return match;

            foreach (WinFormsControl child in root.Controls)
            {
                T nested = FindControl<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
