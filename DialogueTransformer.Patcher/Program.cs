using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using DialogueTransformer.Common.Models;
using DialogueTransformer.Common;
using System.Diagnostics;
using Humanizer;
using System.Collections.Concurrent;
using Mutagen.Bethesda.Plugins.Exceptions;

namespace DialogueTransformer.Patcher
{
    public class Program
    {
        private static readonly object localCacheWriteLock = new();
        public static Lazy<Settings> Settings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, $"DialogueTransformer.esp")
                .Run(args);
        }

        public static async void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            var settings = Settings.Value;
            Console.WriteLine($"<-------------------------------------------------->");
            Console.WriteLine($"< Running DialogueTransformer v1.0.2 by trawzified >");
            Console.WriteLine($"<---------------- Settings ------------------------>");
            Console.WriteLine(settings.ToString());
            Console.WriteLine($"<-------------------------------------------------->");

            var availableModels = Helper.GetModels(state.DataFolderPath);
            var selectedModel = availableModels[settings.Model];
            if (!selectedModel.Installed)
                throw new Exception($"> Selected model {settings.Model}, but it's not installed! Exiting. You can download it here: {selectedModel.DownloadUrl}");

            Dictionary<string, List<IDialogTopicGetter>> dialogueNeedingInferencing = new();

            foreach (var dialogTopic in state.LoadOrder.PriorityOrder.DialogTopic().WinningContextOverrides())
            {
                var name = dialogTopic.Record.Name?.String;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // First try to use the previously translated records from analyzed Khajiit patches
                if (settings.UseOverrides && selectedModel.Overrides.TryGetValue(dialogTopic.Record.FormKey, out var dialogTranslation))
                {
                    try
                    {
                        var translatedDialog = dialogTopic.Record.DeepCopy();
                        translatedDialog.Name = dialogTranslation.TargetText;
                        state.PatchMod.DialogTopics.GetOrAddAsOverride(translatedDialog);
                    }
                    catch(Exception ex)
                    {
                        throw RecordException.Enrich(ex, dialogTopic.ModKey, dialogTopic.Record);
                    }
                    continue;
                }

                // Check if this is a sentence or just some unused keyword kinda thing, if none of these characters are in the string just skip it to save useless processing time on the language model
                if (name.StartsWith('(') || name.IndexOf(' ') == -1 || name.IndexOfAny(new char[] { '.', '?', '!' }) == -1)
                    continue;

                if (dialogueNeedingInferencing.TryGetValue(name, out var dialogTopics))
                    dialogTopics.Add(dialogTopic.Record);
                else
                    dialogueNeedingInferencing[name] = new List<IDialogTopicGetter>() { dialogTopic.Record };
            }

            if (!dialogueNeedingInferencing.Any())
            {
                Console.WriteLine("> Patching complete as no dialogue needs inferencing.");
                return;
            }
            Console.WriteLine($"> {dialogueNeedingInferencing.Count} total dialogue records need inferencing");

            bool preCacheHasRecords = selectedModel.PreCache.Any();
            bool localCacheHasRecords = selectedModel.LocalCache.Any();
            if (preCacheHasRecords || localCacheHasRecords)
            {
                int preCachedCount = 0, localCachedCount = 0;
                Console.WriteLine($"> Found {selectedModel.PreCache.Count} pre-cached dialogue records");
                Console.WriteLine($"> Found {selectedModel.LocalCache.Count} locally cached dialogue records");
                foreach (var (sourceText, dialogTopicGetters) in dialogueNeedingInferencing)
                {
                    bool foundRecordInCache = false;
                    string? inferencedText = null;
                    if (preCacheHasRecords)
                    {
                        foundRecordInCache = selectedModel.PreCache.TryGetValue(sourceText, out inferencedText);
                        if(foundRecordInCache) preCachedCount++;
                    }
                    if (!foundRecordInCache && localCacheHasRecords)
                    {
                        foundRecordInCache = selectedModel.LocalCache.TryGetValue(sourceText, out inferencedText);
                        if(foundRecordInCache) localCachedCount++;
                    }

                    if (!foundRecordInCache)
                        continue;

                    foreach (var dialogTopicGetter in dialogTopicGetters)
                    {
                        try
                        {
                            var recordCopy = dialogTopicGetter.DeepCopy();
                            recordCopy.Name!.Set(Mutagen.Bethesda.Strings.Language.English, inferencedText);
                            state.PatchMod.DialogTopics.GetOrAddAsOverride(recordCopy);
                        }
                        catch(Exception ex)
                        {
                            throw RecordException.Enrich(ex, dialogTopicGetter.FormKey.ModKey, dialogTopicGetter);
                        }
                    }
                    dialogueNeedingInferencing.Remove(sourceText);
                }
                Console.WriteLine($"> Resolved {preCachedCount} lines from pre-cache, {localCachedCount} from local cache - {dialogueNeedingInferencing.Count} records yet to be inferenced");
            }

            if (dialogueNeedingInferencing.Any())
            {
                // Download inferencing client if it doesn't exist in the internal data path yet
                var inferencingPath = Path.Combine(state.DataFolderPath.Path, Consts.DATA_SUBDIR_NAME, Consts.INFERENCING_EXE_FOLDER);
                if(!File.Exists(Path.Combine(inferencingPath, Consts.INFERENCING_EXE_FILE)))
                {
                    Console.WriteLine("> DialogueInferencingClient not found, downloading now (approximately 400MB, may take a bit depending on your connection)...");
                    Console.WriteLine("> If you're having trouble getting past this step, try downloading the DialogueInferencingClient manually from the Nexus page and install it through your mod manager.");
                    var zd = new ZipDownloader();
                    await zd.DownloadAndExtractZip(Consts.INFERENCING_DOWNLOAD_URL, inferencingPath);
                }

                // Calculate amount of threads to use
                var memoryAmount = Helper.GetTotalMemory();
                if (memoryAmount <= 4096000000)
                    throw new Exception("You have less than 4GB of RAM, DialogueTransformer does not support systems with 4GB of RAM or less.");

                // Half of the installed memory in the system divided by 2, in GB
                var maxAllocatedMemory = (((memoryAmount - 2048000000) / 1024000000) / 2);
                var reservedMemoryPerClient = 3;

                var threadAmount = Math.Min((int)(maxAllocatedMemory / (ulong)reservedMemoryPerClient), Environment.ProcessorCount / 4);
                if (Environment.ProcessorCount < 4)
                    throw new Exception("You have a CPU with less than 4 threads, DialogueTransformer does not support systems with these CPUs.");

                // Don't spawn multiple threads when very few bits of dialogue need to be generated
                if (threadAmount >= dialogueNeedingInferencing.Count)
                    threadAmount = 1;

                var chunkedDialogTopics = dialogueNeedingInferencing.Chunk(dialogueNeedingInferencing.Count / threadAmount).Select(chunk => chunk.ToDictionary(x => x.Key, x => x.Value)).ToList();
                Console.WriteLine($"> Inferencing {dialogueNeedingInferencing.Count} dialogue lines using LLM spread over {threadAmount} threads...");
                Task[] tasks = new Task[chunkedDialogTopics.Count];
                int inferencedAmount = 0;

                // Print every 5%
                int printPercentageStep = dialogueNeedingInferencing.Count <= 20 ? 1 : dialogueNeedingInferencing.Count / 20;
                var sw = Stopwatch.StartNew();
                var localCache = new ConcurrentDictionary<string, string>(selectedModel.LocalCache);
                for (int i = 0; i < chunkedDialogTopics.Count; i++)
                {
                    var currentDictionary = chunkedDialogTopics[i];
                    tasks[i] = Task.Run(() =>
                    {
                        var client = new InferencingClient(inferencingPath, Path.Combine(selectedModel.Directory.FullName, Consts.MODEL_SUBDIR_NAME), selectedModel.Prefix ?? string.Empty);
                        foreach (var (sourceText, dialogTopics) in currentDictionary)
                        {
                            var inferencedText = selectedModel.ApplyPostInferencingFixes(client.Inference(sourceText));
                            foreach (var dialogTopic in dialogTopics)
                            {
                                try {
                                    var copiedTopic = dialogTopic.DeepCopy();
                                    copiedTopic.Name?.Set(Mutagen.Bethesda.Strings.Language.English, inferencedText);
                                    state.PatchMod.DialogTopics.GetOrAddAsOverride(copiedTopic);
                                }
                                catch(Exception ex)
                                {
                                    throw RecordException.Enrich(ex, dialogTopic.FormKey.ModKey, dialogTopic);
                                }
                            }
                            localCache.TryAdd(sourceText, inferencedText);
                            Interlocked.Increment(ref inferencedAmount);

                            // Progress tracking & saving to local cache in between predictions
                            if (inferencedAmount % printPercentageStep == 0)
                            {
                                decimal percentage = (100 * inferencedAmount) / dialogueNeedingInferencing.Count;
                                double iterationsPerSecond = inferencedAmount / sw.Elapsed.TotalSeconds;
                                TimeSpan estimatedTimeToCompletion = TimeSpan.FromSeconds((double)((dialogueNeedingInferencing.Count - inferencedAmount) / iterationsPerSecond));
                                Console.WriteLine($"> Processed {inferencedAmount}/{dialogueNeedingInferencing.Count} records ({percentage}% done). {Math.Round(iterationsPerSecond, 2)}it/s, est. time to completion: {estimatedTimeToCompletion.Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second)}");
                                // Save every 10% if processing > 50 records
                                if (inferencedAmount % (printPercentageStep * 2) == 0 && dialogueNeedingInferencing.Count > 50)
                                {
                                    lock (localCacheWriteLock)
                                    {
                                        Helper.WriteToFile(selectedModel.LocalCache.Select(x => new DialogueTextConversion(x.Key, x.Value)), Path.Combine(selectedModel.Directory.FullName, $"{Consts.LOCAL_CACHE_FILENAME}.{Consts.DATA_FORMAT}"));
                                    }
                                }
                            }
                        }
                    });
                }
                Task.WhenAll(tasks).Wait();
                sw.Stop();
                Console.WriteLine($"> Took {sw.Elapsed.TotalSeconds} sec to inference {dialogueNeedingInferencing.Count} records.");
                Console.WriteLine($"> Saving local cache for {selectedModel.LocalCache.Count} records...");
                lock (localCacheWriteLock)
                {
                    Helper.WriteToFile(localCache.Select(x => new DialogueTextConversion(x.Key, x.Value)), Path.Combine(selectedModel.Directory.FullName, $"{Consts.LOCAL_CACHE_FILENAME}.{Consts.DATA_FORMAT}"));
                }
                Console.WriteLine($"> Saved!");
            }
        }
    }
}
