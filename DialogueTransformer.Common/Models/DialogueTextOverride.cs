using CsvHelper.Configuration.Attributes;

namespace DialogueTransformer.Common.Models
{
    
    public struct DialogueTextOverride
    {
        /// <summary>
        /// A dialogue text override to convert a dialogue record, identified by FormKey
        /// </summary>
        /// <param name="formKey">The FormKey identifier of the dialogue record</param>
        /// <param name="sourceText">Not actually used anywhere, but kept in the data format to make it easier for people translating their dialogue in an exported file</param>
        /// <param name="targetText">The text the dialogue record should have</param>
        public DialogueTextOverride(string formKey, string sourceText, string targetText)
        {
            FormKey = formKey;
            SourceText = targetText;
            TargetText = targetText;
        }

        /// <summary>
        /// Not actually used anywhere, but it's kept in the data format as to make it easier for people translating their dialogue in the exported data files.
        /// </summary>
        [Name("source_text")]
        public string SourceText { get; set; }

        /// <summary>
        /// The text this dialogue record should have.
        /// </summary>
        [Name("target_text")]
        public string TargetText { get; set; }

        /// <summary>
        /// The FormKey identifier of the dialogue record
        /// </summary>
        [Name("formkey")]
        public string FormKey { get; set; }
    }

}