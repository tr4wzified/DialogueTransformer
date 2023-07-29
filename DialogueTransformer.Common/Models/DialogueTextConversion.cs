using CsvHelper.Configuration.Attributes;

namespace DialogueTransformer.Common.Models
{
    public struct DialogueTextConversion
    {
        public DialogueTextConversion(string sourceText, string targetText)
        {
            SourceText = sourceText;
            TargetText = targetText;
        }

        /// <summary>
        /// The text that should be converted
        /// </summary>
        [Name("source_text")]
        public string SourceText { get; set; }
        /// <summary>
        /// The text that <cref>SourceText</cref> should receive
        /// </summary>

        [Name("target_text")]
        public string TargetText { get; set; }
    }
}
