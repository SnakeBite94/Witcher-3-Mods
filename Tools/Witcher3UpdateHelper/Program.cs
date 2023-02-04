using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Witcher3UpdateHelper
{
    internal class Program
    {
        public const bool VERBOSE = false;

        private static void Main(string[] args)
        {
            new Program().Main();
        }

        private void Main()
        {
            var cfg = this.GetConfig();

            this.Welcome(cfg);

            var changedFiles = this.GetChangedFiles(cfg);
            using (this.DoWithPreviousWitcherVersion(cfg))
            {
                var modAffectedFiles = this.GetModAffectedFiles(cfg);
                var vanillaFiles = this.GetVanillaFiles(cfg);
                var defs = changedFiles
                    .SelectMany(cf =>
                    {
                        var moded = modAffectedFiles.Where(m => m.EndsWith(cf)).ToArray();
                        if (!moded.Any())
                        {
                            return Enumerable.Empty<MergeDefinition>();
                        }

                        var vanilla = vanillaFiles.Single(v => v.EndsWith(cf));
                        var cdefs = moded.Select(m => new MergeDefinition()
                        {
                            Vanilla = vanilla,
                            OriginalModded = m,
                            RealModded = Symlink.GetRealPath(m), // Vortex deploys symlinks
                            Base = Path.Combine(cfg.Witcher3ScriptsRepository, cf)
                        }).ToArray();
                        return cdefs;
                    })
                    .ToArray();

                ProcesDefinitions(defs, cfg);
            }
            this.Done(cfg);
        }

        private void ProcesDefinitions(MergeDefinition[] defs, Config cfg)
        {
            var all = false;
            foreach (var def in defs)
            {
                this.CreateBackup(def, cfg);
                var relative = def.OriginalModded.Replace(cfg.Witcher3ModsFolder, "").TrimStart('\\');
                var modName = relative.Substring(0, relative.IndexOf("\\"));
                var fileName = Path.GetFileName(def.OriginalModded);

                var r = default(string);
                if (!all)
                {
                    Console.Write($"Merge '{fileName}' from mod '{modName}'? [y,n,a,all,q]:");
                    r = Console.ReadLine();
                    if (r == "all")
                    {
                        all = true;
                        r = "a";
                    }
                }
                else
                {
                    Console.WriteLine($"Auto merging '{fileName}' from mod '{modName}'");
                    r = "a";
                }
                

                if (r == "q")
                {
                    break;
                }
                else if (r == "y" || r == "a")
                {
                    var kdiff = Path.Combine(cfg.KDiffPath, "kdiff3");
                    this.RunCommand(kdiff, $"\"{def.Base}\" \"{def.Vanilla}\" \"{def.RealModded}\" -o \"{def.RealModded}\" {(r == "a" ? "-auto" : "")}", null);
                    var orig = $"{def.RealModded}.orig";
                    if (File.Exists(orig))
                    {
                        File.Delete(orig);
                    }
                }
                else
                {
                    Console.WriteLine("skipped");
                }
            }
        }

        private void Welcome(Config cfg)
        {
            Console.WriteLine("Witcher3 Update helper");
            Console.WriteLine("----------------------");
        }


        private Config GetConfig()
        {
            var cfgFile = "config.json";
            if (!File.Exists(cfgFile))
            {
                var defaultConfig = new Config()
                {
                    SkipWelcome = false,
                    Witcher3ScriptsRepository = @"C:\!programy\Witcher3-Scripts",
                    Witcher3GamePath = @"G:\Steam\steamapps\common\The Witcher 3",
                    KDiffPath = @"C:\Program Files\KDiff3",
                    PrevVersionBranch = "v4.0-steam",
                    CurrentVersionBranch = "v4.0.1-steam",
                };
                File.WriteAllText(cfgFile, JsonConvert.SerializeObject(defaultConfig));
            }
            var cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText(cfgFile));
            return cfg;
        }

        private IEnumerable<string> GetChangedFiles(Config cfg)
        {
            var changes = this.RunCommand("git", $"diff-tree --no-commit-id --name-only {cfg.CurrentVersionBranch} -r", cfg.Witcher3ScriptsRepository);
            var files = changes
                .Replace("/", "\\")
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            return files;
        }

        private IEnumerable<string> GetVanillaFiles(Config cfg)
        {
            var mods = Path.Combine(cfg.Witcher3GamePath, "Content");
            var files = Directory.EnumerateFiles(mods, "*.ws", SearchOption.AllDirectories).ToArray();
            return files;
        }

        private IEnumerable<string> GetModAffectedFiles(Config cfg)
        {
            var files = Directory.EnumerateFiles(cfg.Witcher3ModsFolder, "*.ws", SearchOption.AllDirectories)
                .Where(p => !p.Contains("mod0000_MergedFiles")) // skip Witcher3 Script Merger files
                .ToArray();
            return files;
        }

        private IDisposable DoWithPreviousWitcherVersion(Config cfg)
        {
            Console.WriteLine($"Checking out {cfg.PrevVersionBranch}");
            this.RunCommand("git", $"checkout {cfg.PrevVersionBranch} -f", cfg.Witcher3ScriptsRepository);

            return Disposable.Create(() =>
            {
                Console.WriteLine($"Reverting back to branch {cfg.CurrentVersionBranch}");
                return this.RunCommand("git", $"checkout {cfg.CurrentVersionBranch} -f", cfg.Witcher3ScriptsRepository);
            });
        }

        private void CreateBackup(MergeDefinition def, Config cfg)
        {
            var bak = def.RealModded + $".{cfg.PrevVersionBranch}.bak";
            if (File.Exists(bak))
            {
                File.Copy(bak, def.RealModded, true); // restore bak before merge
            }
            else
            {
                File.Copy(def.RealModded, bak); // create bak
            }
        }

        private void Done(Config cfg)
        {
            var vortexMergerDir = Path.Combine(cfg.Witcher3GamePath, "WitcherScriptMerger");
            var vortexMergerExe = "WitcherScriptMerger.exe";
            var vortexMergerPath = Path.Combine(vortexMergerDir, vortexMergerExe);
            if (File.Exists(vortexMergerPath))
            {
                Console.WriteLine("Done, running Witcher 3 Script Merger");
                this.RunCommand(vortexMergerPath, "");
            }
            else
            {
                Console.WriteLine("Done. Please run Witcher 3 script merger now.");
            };
            Console.ReadLine();
        }

        private string RunCommand(string command, string args, string inDir = null)
        {
            if (VERBOSE)
            {
                Console.WriteLine($"Run: {command} {args} in {inDir}");
            }
            // Start the child process.
            var p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = command;
            p.StartInfo.WorkingDirectory = inDir ?? Path.GetDirectoryName(command);
            p.StartInfo.Arguments = args;
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        private class MergeDefinition
        {
            public string Vanilla { get; set; }
            public string RealModded { get; set; }
            public string Base { get; set; }
            public string OriginalModded { get; internal set; }
        }
    }
}