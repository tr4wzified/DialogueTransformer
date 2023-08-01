using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Patcher
{
    public class Settings
    {
        /// <summary>
        /// Use manual overrides (for example, handwritten dialogue) over trying to inference dialogue from the LLM
        /// </summary>
        public bool UseOverrides { get; set; } = false;
        public DialogueModelType Model { get; set; } = DialogueModelType.UwuSpeak;
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"> {nameof(UseOverrides)}: {UseOverrides}");
            sb.Append($"> {nameof(Model)}: {Model}");

            return sb.ToString();
        }
    }
}
