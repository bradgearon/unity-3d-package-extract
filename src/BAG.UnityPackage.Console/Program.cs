using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;
using SharpCompress.Archive;
using System.Diagnostics;
using System.IO;

namespace BAG.UnityPackage.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<Options>(args);
                if (parsed.Debug)
                {
                    Debugger.Launch();
                }
                new UnityPackageMetadataProvider().Extract(parsed);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GetStyledUsage<Options>().Write();
            }
            catch (Exception ex)
            {
                ArgUsage.GetStyledUsage<Options>().Write();
            }
        }
    }



    interface IPackageMetadata
    {
        string path { get; }
        string name { get; }

        IQueryable<IPackageMetadata> items { get; }
    }

    static class UnityPackageExtensions
    {
        public static IQueryable<IPackageMetadata> Extract(this IPackageMetadata package)
        {
            return null;
        }
    }

    public class UnityPackageMetadataProvider
    {
        // open the package depending on what type of archive it is
        // use the appropriate metadata
        public void Extract(Options options)
        {
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
                foreach (var volume in archive.Entries.ToArray())
                {
                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        using (var tempStream = File.Open(tempFile, FileMode.Open))
                        {
                            volume.WriteTo(tempStream);
                            tempStream.Flush();
                        }

                        Dictionary<string, string> toExtract = null;

                        using (var tempArchive = ArchiveFactory.Open(tempFile))
                        {
                            if (tempArchive != null)
                            {
                                var pathEntries = from entry in tempArchive.Entries.ToArray()
                                                  where Path.GetFileName(entry.FilePath).Contains("pathname") 
                                                    && !entry.IsDirectory
                                                  select entry; 
                                toExtract = pathEntries.ToDictionary(
                                    pathEntry => Path.GetDirectoryName(pathEntry.FilePath), 
                                    pathEntry => getFirstLine(pathEntry));
                            }
                        }

                        using (var tempArchive = ArchiveFactory.Open(tempFile))
                        {
                            if (tempArchive != null)
                            {
                                var assets = from entry in tempArchive.Entries.ToArray()
                                             where Path.GetFileName(entry.FilePath).Contains("asset")
                                                && !entry.IsDirectory
                                             select new { 
                                                 entry = entry, 
                                                 path = toExtract[Path.GetDirectoryName(entry.FilePath)] };
                                
                                foreach (var asset in assets)
                                {
                                    var destPath = Path.Combine(Path.GetFullPath(options.Out),
                                        asset.path.Replace('/', Path.DirectorySeparatorChar));

                                    var destDir = Path.GetDirectoryName(destPath);
                                    if (!Directory.Exists(destDir))
                                    {
                                        Directory.CreateDirectory(destDir);
                                    }

                                    asset.entry.WriteToFile(destPath);
                                }
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        if (options.Debug)
                        {
                            Console.Error.WriteLine(e.ToString());
                        }
                        throw;
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

        }

    }


}
