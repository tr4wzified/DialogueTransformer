using System.Text;
using System.Text.RegularExpressions;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common.Models.DialogueModels
{
    public class KhajiitDialogueModel : ADialogueModel
    {
        public KhajiitDialogueModel(string dataFolderPath) : base(dataFolderPath)
        {
        }

        public override DialogueModelType Type => DialogueModelType.KhajiitSpeak;
        public override string DownloadUrl => "https://www.nexusmods.com/skyrimspecialedition/mods/97650";
        public override string Prefix => "khajiit: ";
        public override string ApplyPostInferencingFixes(string prediction)
        {
            var origPrediction = " " + prediction;
            if (prediction.Length == 0)
                return prediction;

            prediction = Regex.Replace(prediction, @"\w+=\w+>|\w+\.\w+=\w+>|\w+>", "<$0");

            return prediction;
        }
    }
}
