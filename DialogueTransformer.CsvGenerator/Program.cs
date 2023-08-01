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
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using System.Reflection;
using System.Threading;

namespace DialogueTransformer.CsvGenerator
{
    public class Program
    {
        public static string DataFolderPath { get; set; } = string.Empty;
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var patched = await SynthesisPipeline.Instance
                    .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                    .SetTypicalOpen(GameRelease.SkyrimSE, "CsvGen.esp")
                    .Run(args);

                if (File.Exists(Path.Combine(DataFolderPath, "CsvGen.esp")))
                    File.Delete(Path.Combine(DataFolderPath, "CsvGen.esp"));
                return patched;
            }
            catch(Exception ex)
            {

                Console.WriteLine("An error occurred! ");
                Console.WriteLine(ex.ToString());
                return 0;
            }
        }

        public static async void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            DataFolderPath = state.DataFolderPath.Path;
            int maxSourceLength = 0;
            int maxTargetLength = 0;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var modKeys = state.LoadOrder.Select(x => x.Key).ToList();
            //var availableModels = Directory.GetDirectories(Path.Combine(state.DataFolderPath, "DialogueTransformer")).Select(d => new DirectoryInfo(d)).ToList();

            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine($"DialogueTransformer - CSV Generator 1.0.4 by trawzified");
            Console.WriteLine("-----------------------------------------------------------------------------");

            int selectedModelNumber = 0;
            List<DialogueTextOverride> dialogueNeedingConversion = new();
            HashSet<string> espsToGetDialogueFrom = new()
            {
                "gdoWo.esp",
                "savetheicewunnew.esp",
                "SLWF uwuified.esp",
                "uwuspeaktheweuwuing.esp",
                "weawm of wowkhan.esp",
                "impwovedcowwegeentwy.esp"
            };

            foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
            {
                if (!espsToGetDialogueFrom.Contains(dialogTopic.ModKey.FileName))
                    continue;
                var recordToUse = dialogTopic.Record;
                if (!state.LinkCache.TryResolve<IDialogTopicGetter>(dialogTopic.Record.FormKey, out var baseRecord, ResolveTarget.Origin))
                    recordToUse = baseRecord;
                //continue;

                /*
                if (baseRecord.FormKey.ModKey == selectedModKey)
                {
                */
                var name = recordToUse?.Name?.String ?? string.Empty;
                var baseRecordName = baseRecord?.Name?.String ?? string.Empty;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(baseRecordName))
                    continue;

                // Check if this is a sentence or just some unused keyword kinda thing, if none of these characters are in the string just skip it to save useless processing time on the language model
                if (name.StartsWith('(') || name.IndexOf(' ') == -1 || name.IndexOfAny(new char[] { '.', '?', '!' }) == -1)
                    continue;

                //var sourceDialogue = baseRecord.Name?.String;
                dialogueNeedingConversion.Add(new DialogueTextOverride()
                {
                    FormKey = dialogTopic.Record.FormKey.ToString(),
                    SourceText = baseRecordName,
                    TargetText = name
                });


                /*
                if (!string.IsNullOrWhiteSpace(sourceDialogue) && !string.IsNullOrWhiteSpace(translatedDialogue) && sourceDialogue != translatedDialogue)
                {
                    dialogTransformations.Add(new DialogTransformation()
                    {
                        FormKey = baseRecord.FormKey.ToString(),
                        SourceText = sourceDialogue,
                        TargetText = translatedDialogue,
                    });
                }
                */

                /*
                if ((sourceDialogue?.Length ?? 0) > maxSourceLength)
                    maxSourceLength = sourceDialogue?.Length ?? 0;
                if ((sourceDialogue?.Length ?? 0) > maxTargetLength)
                    maxTargetLength = sourceDialogue?.Length ?? 0;
                */
                //}
            }
            if (!dialogueNeedingConversion.Any())
            {
                //Console.WriteLine($"No player dialogue found in mod {selectedModKey.FileName}!");
                Console.WriteLine($"No player dialogue found");
                return;
            }

            //var groupedDialogueTransformations = dialogueTransformations.GroupBy(dt => FormKey.Factory(dt.FormKey).ModKey, (key, dts) => new { ModKey = key, Transformations = dts });

            //foreach (var groupedDialogueTransformation in groupedDialogueTransformations)
            using (var writer = new StreamWriter(Path.Combine(state.DataFolderPath, "DialogueOutput.csv")))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<DialogueTextOverride>();
                csv.NextRecord();
                foreach (var dialogueTransformation in dialogueNeedingConversion)
                {
                    {
                        //using (var writer = new StreamWriter(Path.Combine(state.DataFolderPath, "DialogueTransformer", selectedModel.Name, $"{groupedDialogueTransformation.ModKey.Name}.csv")))
                        csv.WriteRecord(dialogueTransformation);
                        csv.NextRecord();
                    }
                }
            }
            Console.WriteLine($"CSV should have generated, it should be located here: {state.DataFolderPath}/DialogueOutput.csv\nPlease send this to trawzified on Discord. Thanks <3");
            Console.WriteLine($"If you have a 'CsvGen.esp' now, you can remove that. It's just an empty esp.");
            //Console.ReadKey();
        }
    }
}
