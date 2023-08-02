using System.Text;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Patcher
{
    public class Settings
    {
        /// <summary>
        /// Use manual overrides (for example, handwritten dialogue) over trying to inference dialogue from the LLM
        /// </summary>
        public bool UseOverrides { get; set; } = false;
        public DialogueModelType Model { get; set; } = DialogueModelType.KhajiitSpeak;
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"> {nameof(UseOverrides)}: {UseOverrides}");
            sb.Append($"> {nameof(Model)}: {Model}");

            return sb.ToString();
        }
    }
}
