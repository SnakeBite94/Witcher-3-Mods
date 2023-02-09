using System.IO;

namespace Witcher3UpdateHelper
{
    internal class Config
    {
        public bool SkipWelcome { get; set; }
        public string Witcher3ScriptsRepository { get; set; }
        public string Witcher3GamePath { get; set; }
        public string PrevVersionBranch { get; set; }
        public string CurrentVersionBranch { get; set; }
        public string KDiffPath { get; set; }
        public string Witcher3ModsFolder => Path.Combine(Witcher3GamePath, "Mods");
        public string Mode { get; set; }
        public string Filter { get; set; }
    }
}