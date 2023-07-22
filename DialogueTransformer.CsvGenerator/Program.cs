using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Cache;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using CsvHelper;
using System;
using DialogueTransformer.Common.Models;

namespace DialogueTransformer.CsvGenerator
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "CsvGen.esp")
                .Run(args);
        }

        public static async void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            int maxSourceLength = 0;
            int maxTargetLength = 0;

            using(var writer = new StreamWriter("KhajiitTranslations.csv"))
            using(var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<DialogTransformation>();
                csv.NextRecord();
                foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
                {
                    //dialogTopic.TryGetParent<IDialogTopicGetter>(out var parent);

                    var modifyingModKey = dialogTopic.ModKey.ToString();
                    if (modifyingModKey.StartsWith("BA_") || modifyingModKey.StartsWith("mjhKhajiitSpeak") || modifyingModKey.StartsWith("Chr_KhajiitSpeak"))
                    {

                        if (dialogTopic.Record.Name == null)
                            continue;

                        if (!state.LinkCache.TryResolve<IDialogTopicGetter>(dialogTopic.Record.FormKey, out var baseRecord, ResolveTarget.Origin))
                            continue;

                        var sourceDialogue = baseRecord.Name?.String;
                        var translatedDialogue = dialogTopic.Record.Name.String;


                        if (!string.IsNullOrWhiteSpace(sourceDialogue) && !string.IsNullOrWhiteSpace(translatedDialogue) && sourceDialogue != translatedDialogue)
                        {
                            csv.WriteRecord(new DialogTransformation()
                            {
                                FormKey = baseRecord.FormKey.ToString(),
                                ModifiedBy = modifyingModKey,
                                SourceText = sourceDialogue,
                                TargetText = translatedDialogue,

                            });
                            csv.NextRecord();
                        }
                        if((sourceDialogue?.Length ?? 0) > maxSourceLength)
                            maxSourceLength= sourceDialogue?.Length ?? 0;
                        if((translatedDialogue?.Length ?? 0) > maxTargetLength)
                            maxTargetLength = translatedDialogue?.Length ?? 0;
                    }
                }
            }
        }
    }
}
