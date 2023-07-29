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
    }
}
