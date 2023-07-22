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

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var modKeys = state.LoadOrder.Select(x => x.Key).ToList();
            var availableModels = Directory.GetDirectories(Path.Combine(state.DataFolderPath, "DialogueTransformer")).Select(d => new DirectoryInfo(d)).ToList();

            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine($"DialogueTransformer - CSV Override Template Generator {version} by trawzified");
            Console.WriteLine("-----------------------------------------------------------------------------");

            int selectedModelNumber = 0;
            while (selectedModelNumber <= 0)
            {
                Console.WriteLine("Select a model to generate a CSV override template for: ");
                for(int i = 0; i < availableModels.Count; i++)
                {
                    var model = availableModels[i];
                    Console.WriteLine($"({i + 1}) - {model.Name}");
                }
                int.TryParse(Console.ReadLine(), out selectedModelNumber);
            }
            var selectedModel = availableModels[selectedModelNumber - 1];

            int selectedModKeyNumber = 0;
            while (selectedModKeyNumber <= 0)
            {
                Console.WriteLine("Select the plugin to generate a CSV override template for: ");
                Thread.Sleep(2500);
                for(int i = 0; i < modKeys.Count; i++)
                {
                    var model = modKeys[i];
                    Console.WriteLine($"({i + 1}) - {model.FileName}");
                }
                int.TryParse(Console.ReadLine(), out selectedModKeyNumber);
            }

            var selectedModKey = modKeys[selectedModKeyNumber - 1];

            List<DialogueTransformation> dialogueTransformations = new();

            foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
            {
                if (!state.LinkCache.TryResolve<IDialogTopicGetter>(dialogTopic.Record.FormKey, out var baseRecord, ResolveTarget.Origin))
                    continue;

                if (baseRecord.FormKey.ModKey == selectedModKey)
                {
                    var name = dialogTopic.Record.Name?.String ?? string.Empty;
                    var baseRecordName = baseRecord.Name?.String ?? string.Empty;
                    if (string.IsNullOrEmpty(name))
                        continue;

                    // Check if this is a sentence or just some unused keyword kinda thing, if none of these characters are in the string just skip it to save useless processing time on the language model
                    if (baseRecordName.StartsWith('(') || baseRecordName.IndexOf(' ') == -1 || baseRecordName.IndexOfAny(new char[] { '.', '?', '!' }) == -1)
                        continue;

                    //var sourceDialogue = baseRecord.Name?.String;
                    dialogueTransformations.Add(new DialogueTransformation()
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
                }
            }
            if (!dialogueTransformations.Any())
            {
                Console.WriteLine($"No player dialogue found in mod {selectedModKey.FileName}!");
                return;
            }

            var groupedDialogueTransformations = dialogueTransformations.GroupBy(dt => FormKey.Factory(dt.FormKey).ModKey, (key, dts) => new { ModKey = key, Transformations = dts });

            foreach (var groupedDialogueTransformation in groupedDialogueTransformations)
            {
                using (var writer = new StreamWriter(Path.Combine(state.DataFolderPath, "DialogueTransformer", selectedModel.Name, $"{groupedDialogueTransformation.ModKey.Name}.csv")))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<DialogueTransformation>();
                    csv.NextRecord();
                    foreach (var dialogueTransformation in groupedDialogueTransformation.Transformations)
                    {
                        csv.WriteRecord(dialogueTransformation);
                        csv.NextRecord();
                    }
                }
            }
        }
    }
}
