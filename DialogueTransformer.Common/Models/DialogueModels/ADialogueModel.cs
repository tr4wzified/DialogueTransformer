using DialogueTransformer.Common.Interfaces;
using Mutagen.Bethesda.Plugins;
using System.IO;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common.Models.DialogueModels
{
    public abstract class ADialogueModel : IDialogueModel
    {
        public ADialogueModel(string dataFolderPath)
        {
            Directory = new DirectoryInfo(Path.Combine(dataFolderPath, Consts.DATA_SUBDIR_NAME, Type.ToString()));
            Overrides = System.IO.Directory.GetFiles(Path.Combine(Directory.FullName, Consts.DATA_SUBDIR_OVERRIDES_NAME), $"*.{Consts.DATA_FORMAT}")
                                           .SelectMany(x => Helper.GetOverridesFromFile(x))
                                           .ToDictionary(x => x.Key, x => x.Value);
            PreCache = Helper.GetTextConversionsFromFile(Path.Combine(Directory.FullName, $"{Consts.PREGENERATED_CACHE_FILENAME}.{Consts.DATA_FORMAT}"));
            LocalCache = Helper.GetTextConversionsFromFile(Path.Combine(Directory.FullName, $"{Consts.LOCAL_CACHE_FILENAME}.{Consts.DATA_FORMAT}"));
        }

        public abstract string DownloadUrl { get; }
        public abstract string Prefix { get; }
        public DirectoryInfo Directory { get; }
        public bool Installed => Directory.Exists;

        public Dictionary<FormKey, DialogueTextOverride> Overrides { get; }

        public Dictionary<string, string> PreCache { get; }

        public Dictionary<string, string> LocalCache { get; }

        public abstract DialogueModelType Type { get; }

    }
}
