using System.Text;
using System.Text.RegularExpressions;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common.Models.DialogueModels
{
    public class UwuDialogueModel : ADialogueModel
    {
        public UwuDialogueModel(string dataFolderPath) : base(dataFolderPath)
        {
        }

        public override DialogueModelType Type => DialogueModelType.UwuSpeak;
        public override string DownloadUrl => "https://www.nexusmods.com/skyrimspecialedition/mods/97654";
        public override string Prefix => "uwu: ";
        public override string ApplyPostInferencingFixes(string prediction)
        {
            if (prediction.Length == 0)
                return prediction;

            prediction = Regex.Replace(prediction, @"\w+=\w+>|\w+\.\w+=\w+>|\w+>", "<$0");

            return prediction;

        }
    }
}
