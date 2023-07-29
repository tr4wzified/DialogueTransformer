using DialogueTransformer.Common.Models;
using Mutagen.Bethesda.Plugins;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common.Interfaces
{
    public interface IDialogueModel
    {
        DialogueModelType Type { get; }
        string DownloadUrl { get; }
        DirectoryInfo Directory { get; }
        bool Installed { get; }
        Dictionary<FormKey, DialogueTextOverride> Overrides { get; }
        Dictionary<string, string> PreCache { get; }
        Dictionary<string, string> LocalCache { get; }
        string? Prefix { get; }
    }
}
