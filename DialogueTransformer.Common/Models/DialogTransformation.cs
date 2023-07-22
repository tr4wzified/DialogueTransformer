using CsvHelper.Configuration.Attributes;

namespace DialogueTransformer.Common.Models
{
    public enum TransformationAction
    {
        Replace,
        Append,
        Prepend,
        AppendBeforeLastChar
    }
    
    public class DialogTransformation
    {
        [Name("source_text")]
        public string SourceText { get; set; } = string.Empty;

        [Name("target_text")]
        public string TargetText { get; set; } = string.Empty;

        [Name("modified_by")]
        public string ModifiedBy { get; set; } = string.Empty;

        [Name("formkey")]
        public string FormKey { get; set; } = string.Empty;
    }

}