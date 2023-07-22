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
            Dictionary<FormKey, IDialogTopicGetter> dialogRecordsToPredict = new();

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
                dialogRecordsToPredict.Add(dialogTopic.Record.FormKey, dialogTopic.Record);
            }

            var predictorPath = Path.Combine(state.InternalDataPath, "DialoguePredictor");


            // Split dictionary workload for each thread
            ConcurrentDictionary<FormKey, DialogueTransformation> cachedPredictions = new();
            var cachedPredictionsPath = Path.Combine(selectedModelPath.FullName, "CachedOverrides.csv");
            if (File.Exists(cachedPredictionsPath))
            {
                cachedPredictions = new ConcurrentDictionary<FormKey, DialogueTransformation>(Helper.GetTransformationsFromCsv(cachedPredictionsPath));
                Console.WriteLine($"Found {cachedPredictions.Count} cached predictions");
                Console.WriteLine($"Removing the cached predictions from the prediction queue ({dialogRecordsToPredict.Count}...");
                int cachedCount = 0;
                foreach(var cachedPrediction in cachedPredictions)
                {
                    if(dialogRecordsToPredict.TryGetValue(cachedPrediction.Key, out var dialogTopicGetter))
                    {
                        var recordCopy = dialogTopicGetter.DeepCopy();
                        recordCopy.Name = cachedPrediction.Value.TargetText;
                        state.PatchMod.DialogTopics.GetOrAddAsOverride(recordCopy);
                        cachedCount++;
                        dialogRecordsToPredict.Remove(cachedPrediction.Key);
                    }
                }
            }

            if (dialogRecordsToPredict.Any())
            {
                var memoryAmount = Helper.GetTotalMemory();
                var maxMemoryAllowedToTakeUpInGB = (((memoryAmount - 2048000000) / 1024000000) / 2);
                var predictionClientMemoryNeededInGB = 2;
                var threadAmount = (int)(maxMemoryAllowedToTakeUpInGB / (ulong)predictionClientMemoryNeededInGB);
                //var threadAmount = 20;
                var chunkedDialogTopics = dialogRecordsToPredict.Values.Chunk(dialogRecordsToPredict.Count / threadAmount).Select(chunk => chunk.ToList()).ToList();
                Console.WriteLine($"Starting to predict {dialogRecordsToPredict.Count} records spread over {threadAmount} threads...");
                var sw = Stopwatch.StartNew();
                Task[] tasks = new Task[chunkedDialogTopics.Count];
                int predictedAmount = 0;
                for (int i = 0; i < chunkedDialogTopics.Count; i++)
                {
                    var currentDictionary = chunkedDialogTopics[i];
                    tasks[i] = Task.Run(() =>
                    {
                        var client = new PredictionClient(predictorPath, Path.Combine(selectedModelPath.FullName, "Model"), File.ReadAllText(Path.Combine(selectedModelPath.FullName, "model_prefix.txt")));
                        var predictions = client.Predict(currentDictionary.Select(dt => dt.Name!.String ?? string.Empty)).ToList();
                        for (int j = 0; j < currentDictionary.Count; j++) {
                            var record = currentDictionary[j];
                            var deepCopy = record.DeepCopy();
                            deepCopy.Name = predictions[j];
                            state.PatchMod.DialogTopics.GetOrAddAsOverride(deepCopy);
                            cachedPredictions.TryAdd(record.FormKey, new DialogueTransformation()
                            {
                                SourceText = record.Name?.String ?? string.Empty,
                                TargetText = predictions[j],
                                FormKey = record.FormKey.ToString()
                            });
                            Interlocked.Increment(ref predictedAmount);
                        }
                    });
                }
                /*
                _ = Task.Run(() =>
                {
                    while (predictedAmount < dialogRecordsToPredict.Count)
                    {
                        Thread.Sleep(30000);
                        Console.WriteLine($"Predicting... {(int)(predictedAmount / dialogRecordsToPredict.Count * 100)}% done");
                    }
                });
                */
                Task.WhenAll(tasks).Wait();
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed.TotalSeconds} sec to predict {dialogRecordsToPredict.Count} records.");
            }

            if (cachedPredictions.Any())
            {
                Console.WriteLine($"Saving cache for {cachedPredictions.Count} records...");
                var cachedOverridesPath = Path.Combine(selectedModelPath.FullName, "CachedOverrides.csv");
                if (!File.Exists(cachedOverridesPath))
                    File.Create(cachedOverridesPath);

                using (var writer = new StreamWriter(cachedOverridesPath))
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
            }
            Console.WriteLine($"Done! Press any key to exit.");
            Console.ReadKey();
        }
    }
}
