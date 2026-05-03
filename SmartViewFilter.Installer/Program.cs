using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SmartViewFilter.Installer
{
    internal static class Program
    {
        private static readonly string[] SupportedVersions = { "2022", "2023", "2026" };

        private static int Main(string[] args)
        {
            Console.Title = "Smart View Filter Installer";
            Console.WriteLine("Smart View Filter 2.0 Installer");
            Console.WriteLine();

            try
            {
                if (args.Any(arg => string.Equals(arg, "/uninstall", StringComparison.OrdinalIgnoreCase)))
                {
                    Uninstall();
                    return 0;
                }

                Install();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Installation failed:");
                Console.ResetColor();
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Close Revit and run the installer again.");
                PauseIfInteractive();
                return 1;
            }
        }

        private static void Install()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "SmartViewFilterInstaller-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            try
            {
                string zipPath = ExtractEmbeddedBundle(tempFolder);
                ZipFile.ExtractToDirectory(zipPath, tempFolder);

                string bundleRoot = Path.Combine(tempFolder, "SmartViewFilter.bundle");
                if (!Directory.Exists(bundleRoot))
                {
                    throw new DirectoryNotFoundException("SmartViewFilter.bundle was not found inside the embedded package.");
                }

                var installedVersions = new List<string>();
                foreach (string version in SupportedVersions)
                {
                    if (!IsRevitInstalled(version))
                    {
                        Console.WriteLine("Skipping Revit {0}: not installed.", version);
                        continue;
                    }

                    InstallForVersion(bundleRoot, version);
                    installedVersions.Add(version);
                    Console.WriteLine("Installed for Revit {0}.", version);
                }

                Console.WriteLine();
                if (installedVersions.Count == 0)
                {
                    Console.WriteLine("No supported Revit version was found. Supported versions: 2022, 2023, 2026.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Installation complete.");
                    Console.ResetColor();
                    Console.WriteLine("Restart Revit and open: Smart Revit > Filters > Live Filter");
                }

                Console.WriteLine();
                Console.WriteLine("To uninstall later, run:");
                Console.WriteLine("SmartViewFilter.Installer.exe /uninstall");
            }
            finally
            {
                TryDeleteDirectory(tempFolder);
                PauseIfInteractive();
            }
        }

        private static string ExtractEmbeddedBundle(string tempFolder)
        {
            string zipPath = Path.Combine(tempFolder, "SmartViewFilter.bundle.zip");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SmartViewFilter.bundle.zip"))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Embedded SmartViewFilter.bundle.zip resource was not found.");
                }

                using (FileStream file = File.Create(zipPath))
                {
                    stream.CopyTo(file);
                }
            }

            return zipPath;
        }

        private static void InstallForVersion(string bundleRoot, string version)
        {
            string contentFolder = Path.Combine(bundleRoot, "Contents", version);
            if (!Directory.Exists(contentFolder))
            {
                throw new DirectoryNotFoundException("Package content missing for Revit " + version + ".");
            }

            string addinsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Autodesk",
                "Revit",
                "Addins",
                version);

            string installFolder = Path.Combine(addinsRoot, "SmartViewFilter");
            Directory.CreateDirectory(installFolder);

            foreach (string sourceFile in Directory.GetFiles(contentFolder))
            {
                if (string.Equals(Path.GetExtension(sourceFile), ".addin", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string targetFile = Path.Combine(installFolder, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, targetFile, overwrite: true);
            }

            string assemblyPath = Path.Combine(installFolder, "SmartViewFilter.Revit.dll");
            string manifestPath = Path.Combine(addinsRoot, "SmartViewFilter.addin");
            string manifest = BuildManifest(assemblyPath);
            File.WriteAllText(manifestPath, manifest, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static string BuildManifest(string assemblyPath)
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<RevitAddIns>
  <AddIn Type=""Application"">
    <Name>Smart View Filter</Name>
    <Assembly>" + EscapeXml(assemblyPath) + @"</Assembly>
    <AddInId>8D83C886-B739-4ACD-A9DB-3D1E3B6D1F11</AddInId>
    <FullClassName>SmartViewFilter.Revit.App</FullClassName>
    <VendorId>ASHP</VendorId>
    <VendorDescription>CAD Automation by Ashish</VendorDescription>
  </AddIn>
</RevitAddIns>
";
        }

        private static void Uninstall()
        {
            foreach (string version in SupportedVersions)
            {
                string addinsRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk",
                    "Revit",
                    "Addins",
                    version);

                string manifestPath = Path.Combine(addinsRoot, "SmartViewFilter.addin");
                string installFolder = Path.Combine(addinsRoot, "SmartViewFilter");

                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                    Console.WriteLine("Removed manifest for Revit {0}.", version);
                }

                if (Directory.Exists(installFolder))
                {
                    Directory.Delete(installFolder, recursive: true);
                    Console.WriteLine("Removed files for Revit {0}.", version);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Uninstall complete.");
            PauseIfInteractive();
        }

        private static bool IsRevitInstalled(string version)
        {
            string installFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Autodesk",
                "Revit " + version);

            return Directory.Exists(installFolder);
        }

        private static string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static void TryDeleteDirectory(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
            catch
            {
            }
        }

        private static void PauseIfInteractive()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to close.");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
