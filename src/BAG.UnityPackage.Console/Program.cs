using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;
using SharpCompress.Archive;
using System.Diagnostics;
using System.IO;
using SharpCompress.Common;
using System.IO.MemoryMappedFiles;

namespace BAG.UnityPackage.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.DomainUnload += CleanupBeforeExit;
                var parsed = Args.Parse<Options>(args);
                if (parsed.Debug)
                {
                    Debugger.Launch();
                }
                var success = new UnityPackageMetadataProvider().Extract(parsed);
                if (success)
                {
                    Console.WriteLine(Environment.NewLine + "Successfully extracted");
                }

            }
            catch (ArgException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid cmd line arguments: " + ex.Message + Environment.NewLine);
                Console.ResetColor();

                ArgUsage.GetStyledUsage<Options>().Write();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + "An error occurred while extracting: " + ex.Message + Environment.NewLine);
                Console.ResetColor();
            }
            Console.ResetColor();
        }

        private static void CleanupBeforeExit(object sender, EventArgs e)
        {
            Console.ResetColor();
        }
    }

    public class UnityPackageMetadataProvider
    {
        public bool Extract(Options options)
        {
            var errors = 0;

            if (string.IsNullOrEmpty(options.Out))
            {
                options.Out = Path.GetFileNameWithoutExtension(options.In);
            }
            var archive = ArchiveFactory.Open(options.In);
            Func<IArchiveEntry, string> getFirstLine = (entry) =>
            {
                var path = string.Empty;
                using (var sr = new StreamReader(entry.OpenEntryStream()))
                {
                    path = sr.ReadLine();
                }
                return path;
            };

            if (archive != null)
            {
                foreach (var volume in archive.Entries)
                {
                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        using (var tempStream = File.Open(tempFile, FileMode.Open))
                        {


                            volume.WriteTo(tempStream);
                            tempStream.Flush();

                            using (var tempArchive = ArchiveFactory.Open(tempStream))
                            {
                                Dictionary<string, string> toExtract = null;

                                var pathEntries = from entry in tempArchive.Entries.ToArray()
                                                  where Path.GetFileName(entry.FilePath).Contains("pathname")
                                                    && !entry.IsDirectory
                                                  select entry;

                                toExtract = pathEntries.ToDictionary(
                                    pathEntry => Path.GetDirectoryName(pathEntry.FilePath),
                                    pathEntry => getFirstLine(pathEntry));

                                var assets = from entry in tempArchive.Entries.ToArray()
                                             where Path.GetFileName(entry.FilePath).Contains("asset")
                                                && !entry.IsDirectory
                                             select new
                                             {
                                                 entry = entry,
                                                 path = toExtract[Path.GetDirectoryName(entry.FilePath)]
                                             };

                                var fullOut = Path.GetFullPath(options.Out);
                                Console.WriteLine("\nExtracting to:\n{0}\n", fullOut);

                                foreach (var asset in assets)
                                {
                                    try
                                    {
                                        var destPath = Path.Combine(fullOut,
                                            asset.path.Replace('/', Path.DirectorySeparatorChar));

                                        var destDir = Path.GetDirectoryName(destPath);
                                        if (!Directory.Exists(destDir))
                                        {
                                            Directory.CreateDirectory(destDir);
                                        }

                                        Console.Write("{0}", asset.path);

                                        var exOptions = ExtractOptions.None;
                                        if (options.Force)
                                        {
                                            exOptions |= ExtractOptions.Overwrite;
                                        }

                                        asset.entry.WriteToFile(destPath, exOptions);

                                        Console.WriteLine("\tOk");
                                        Console.ResetColor();
                                    }
                                    catch (Exception ex)
                                    {
                                        errors++;
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(Environment.NewLine + "error occurred while extracting: " + ex.Message + Environment.NewLine);
                                    }
                                }
                            }

                        }
                    }
                    finally
                    {
                        if (tempFile != null && File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                    }

                }

            }

            return errors == 0;

        }

    }


}
