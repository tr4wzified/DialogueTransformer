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
                .SetTypicalOpen(GameRelease.SkyrimSE, $"SDT_{PATCHER_TYPE}.esp")
                .Run(args);
        }

        public static async void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (state.InternalDataPath == null)
                throw new Exception("InternalDataPath was null - patcher cannot function!");

            var path = Path.Combine(state.InternalDataPath, CSV_FILE_NAME);
            var handTranslatedRecords = Helper.GetTranslationsFromCsv(Path.Combine(path, CSV_FILE_NAME));
            Dictionary<IDialogTopicGetter, string> dialogRecordsToPredict = new();


            foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
            {
                var name = dialogTopic.Record.Name?.String;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // First try to use the previously translated records from analyzed Khajiit patches
                if (handTranslatedRecords.TryGetValue(dialogTopic.Record.FormKey, out var dialogTranslation))
                {
                    var translatedDialog = dialogTopic.Record.DeepCopy();
                    translatedDialog.Name = dialogTranslation.TargetText;
                    //state.PatchMod.DialogTopics.GetOrAddAsOverride(translatedDialog);
                    continue;
                }

                // Check if this is a sentence or just some unused keyword kinda thing, if none of these characters are in the string just skip it to save useless processing time on the language model
                if (name.StartsWith('(') || name.IndexOf(' ') == -1 || name.IndexOfAny(new char[] { '.', '?', '!' }) == -1)
                    continue;

                // Fallback to translation algorithm
                dialogRecordsToPredict.Add(dialogTopic.Record, name);
            }

            var predictPath = Path.Combine(path, "dist", "Predict");
            Console.WriteLine($"Starting to predict {dialogRecordsToPredict.Count} records");

            var memoryAmount = Helper.GetTotalMemory();
            var maxMemoryAllowedToTakeUpInGB = (((memoryAmount - 2048000000) / 1024000000) / 2);
            var predictionClientMemoryNeededInGB = 3;
            var threadAmount = (int)(maxMemoryAllowedToTakeUpInGB / (ulong)predictionClientMemoryNeededInGB);
            var chunkedDictionaries = dialogRecordsToPredict.Chunk(dialogRecordsToPredict.Count / threadAmount).ToList();

            // Split dictionary workload for each thread
            ConcurrentDictionary<string, string> cachedPredictions = new();
            //if(Path.Combine(state.DataFolderPath, "Khajiitifier"))
            var sw = Stopwatch.StartNew();
            Task[] tasks = new Task[chunkedDictionaries.Count];
            for(int i = 0; i < chunkedDictionaries.Count; i++)
            {
                var currentDictionary = chunkedDictionaries[i];
                tasks[i] = Task.Run(() =>
                {
                    var client = new PredictionClient(predictPath);
                    foreach (var recordNamePair in currentDictionary)
                    {
                        var recordCopy = recordNamePair.Key.DeepCopy();
                        string? prediction;
                        if (!cachedPredictions.TryGetValue(recordNamePair.Value, out prediction))
                        {
                            prediction = client.Predict(recordNamePair.Value);
                            cachedPredictions.TryAdd(recordNamePair.Value, prediction);
                        }

                        prediction ??= string.Empty;
                        recordCopy.Name = prediction;
                        state.PatchMod.DialogTopics.GetOrAddAsOverride(recordCopy);
                    }
                    client.Dispose();
                });
            }
            Task.WhenAll(tasks).Wait();
            sw.Stop();

            Console.WriteLine($"Took {sw.Elapsed.TotalSeconds} sec to predict {dialogRecordsToPredict.Count} records.");
        }

    }
}
