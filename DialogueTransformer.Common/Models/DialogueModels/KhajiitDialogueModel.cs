using System.Text;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common.Models.DialogueModels
{
    public class KhajiitDialogueModel : ADialogueModel
    {
        public KhajiitDialogueModel(string dataFolderPath) : base(dataFolderPath)
        {
        }

        public override DialogueModelType Type => DialogueModelType.KhajiitSpeak;
        public override string DownloadUrl => "";
        public override string Prefix => "khajiit: ";
        public override string ApplyPostInferencingFixes(string prediction)
        {
            if (prediction.Length == 0)
                return prediction;

            var outputBuilder = new StringBuilder();
            var words = prediction.Split(' ');
            for(int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var greaterThanIndex = word.IndexOf(">");
                // Correct Alias=Player>, pronouns and <Global> to include the <, model training error
                var aliasIndex = word.IndexOf("Alias=");
                if (aliasIndex != -1 && greaterThanIndex != -1)
                    word = word.Replace("Alias=", "<Alias=");

                var globalIndex = word.IndexOf("Global=");
                if (globalIndex != -1 && greaterThanIndex != -1)
                    word = word.Replace("Global=", "<Global=");

                if (i == words.Length - 1)
                    outputBuilder.Append(word);
                else
                {
                    outputBuilder.Append(word);
                    outputBuilder.Append(" ");
                }
            }
            return outputBuilder.ToString();
        }
    }
}
