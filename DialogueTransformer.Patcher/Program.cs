using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using CsvHelper;
using System.Globalization;
using Mutagen.Bethesda.Plugins;
using DialogueTransformer.Common.Models;
using DialogueTransformer.Common;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Reflection;

namespace DialogueTransformer.Patcher
{
    public class Program
    {
        private const string CSV_FILE_NAME = "KhajiitTranslations.csv";
        private const string PATCHER_TYPE = "Khajiit";
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, $"DialogueTransformer.esp")
                .Run(args);
        }

        public static async void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            if (state.InternalDataPath == null)
                throw new Exception("InternalDataPath was null - patcher cannot function!");

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var availableModels = Directory.GetDirectories(Path.Combine(state.DataFolderPath, "DialogueTransformer")).Select(d => new DirectoryInfo(d)).ToList();

            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine($"DialogueTransformer {version} by trawzified (PRE-RELEASE)");
            Console.WriteLine("----------------------------------------------------------");

            int selectedModelNumber = 0;
            while (selectedModelNumber <= 0)
            {
                Console.WriteLine("Select a model to transform dialogue to: ");
                for(int i = 0; i < availableModels.Count; i++)
                {
                    var model = availableModels[i];
                    Console.WriteLine($"({i + 1}) - {model.Name}");
                }
                int.TryParse(Console.ReadLine(), out selectedModelNumber);
            }

            var selectedModelPath = availableModels[selectedModelNumber - 1];

            // Get CSV overrides
            var csvOverrides = Directory.GetFiles(Path.Combine(selectedModelPath.FullName, "CSVOverrides"), "*.csv");
            List<Dictionary<FormKey, DialogueTransformation>> overrideDialogTransformations = new();
            foreach(var csvOverride in csvOverrides)
            {
                overrideDialogTransformations.Add(Helper.GetTransformationsFromCsv(csvOverride));
            }

            var joinedTransformationOverrides = overrideDialogTransformations.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
            Dictionary<IDialogTopicGetter, string> dialogRecordsToPredict = new();

            foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
            {
                var name = dialogTopic.Record.Name?.String;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // First try to use the previously translated records from analyzed Khajiit patches
                if (joinedTransformationOverrides.TryGetValue(dialogTopic.Record.FormKey, out var dialogTranslation))
                {
                    var translatedDialog = dialogTopic.Record.DeepCopy();
                    translatedDialog.Name = dialogTranslation.TargetText;
                    state.PatchMod.DialogTopics.GetOrAddAsOverride(translatedDialog);
                    continue;
                }

                // Check if this is a sentence or just some unused keyword kinda thing, if none of these characters are in the string just skip it to save useless processing time on the language model
                if (name.StartsWith('(') || name.IndexOf(' ') == -1 || name.IndexOfAny(new char[] { '.', '?', '!' }) == -1)
                    continue;

                // Fallback to translation algorithm
                dialogRecordsToPredict.Add(dialogTopic.Record, name);
            }

            var predictorPath = Path.Combine(state.InternalDataPath, "DialoguePredictor");

            var memoryAmount = Helper.GetTotalMemory();
            var maxMemoryAllowedToTakeUpInGB = (((memoryAmount - 2048000000) / 1024000000) / 2.5);
            var predictionClientMemoryNeededInGB = 3;
            var threadAmount = (int)(maxMemoryAllowedToTakeUpInGB / (ulong)predictionClientMemoryNeededInGB);
            var chunkedDictionaries = dialogRecordsToPredict.Chunk(dialogRecordsToPredict.Count / threadAmount).ToList();

            // Split dictionary workload for each thread
            ConcurrentDictionary<FormKey, DialogueTransformation> cachedPredictions = new();
            var cachedOverrides = Path.Combine(selectedModelPath.FullName, "CachedOverrides.csv");
            if (File.Exists(cachedOverrides))
            {
                cachedPredictions = new ConcurrentDictionary<FormKey, DialogueTransformation>(Helper.GetTransformationsFromCsv(cachedOverrides));
                Console.WriteLine($"Found {cachedPredictions.Count} cached predictions");
            }

            Console.WriteLine($"Starting to predict {dialogRecordsToPredict.Count} ({cachedPredictions.Count} cached) records");
            var sw = Stopwatch.StartNew();
            Task[] tasks = new Task[chunkedDictionaries.Count];
            for(int i = 0; i < chunkedDictionaries.Count; i++)
            {
                var currentDictionary = chunkedDictionaries[i];
                tasks[i] = Task.Run(() =>
                {
                    var client = new PredictionClient(predictorPath, Path.Combine(selectedModelPath.FullName, "Model"), File.ReadAllText(Path.Combine(selectedModelPath.FullName, "model_prefix.txt")));
                    foreach (var recordNamePair in currentDictionary)
                    {
                        var recordCopy = recordNamePair.Key.DeepCopy();
                        DialogueTransformation? prediction = null;
                        if (!cachedPredictions.TryGetValue(recordNamePair.Key.FormKey, out prediction))
                        {
                            var targetText = client.Predict(recordNamePair.Value);
                            prediction = new DialogueTransformation()
                            {
                                SourceText = recordNamePair.Value,
                                TargetText = client.Predict(recordNamePair.Value),
                                FormKey = recordNamePair.Key.FormKey.ToString()
                            };
                            cachedPredictions.TryAdd(recordNamePair.Key.FormKey, prediction);
                        }

                        recordCopy.Name = prediction.TargetText;
                        state.PatchMod.DialogTopics.GetOrAddAsOverride(recordCopy);
                    }
                    client.Dispose();
                });
            }
            Task.WhenAll(tasks).Wait();
            sw.Stop();

            Console.WriteLine($"Saving cache for {cachedPredictions.Count} records...");
            using (var writer = new StreamWriter(Path.Combine(selectedModelPath.FullName, "CachedOverrides.csv")))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<DialogueTransformation>();
                csv.NextRecord();
                foreach (var dialogueTransformation in cachedPredictions.Values)
                {
                    csv.WriteRecord(dialogueTransformation);
                    csv.NextRecord();
                }
            }
            

            Console.WriteLine($"Took {sw.Elapsed.TotalSeconds} sec to predict {dialogRecordsToPredict.Count} records.");
        }

    }
}
