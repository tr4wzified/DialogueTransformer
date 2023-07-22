using CsvHelper.Configuration.Attributes;
using Mutagen.Bethesda.Plugins;

namespace DialogueTransformer.Common.Models
{
    
    public class DialogueTransformation
    {
        [Name("source_text")]
        public string SourceText { get; set; } = string.Empty;

        [Name("target_text")]
        public string TargetText { get; set; } = string.Empty;

        [Name("formkey")]
        public string FormKey { get; set; } = string.Empty;
    }

}