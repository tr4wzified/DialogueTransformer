using DialogueTransformer.Common;
using DialogueTransformer.Common.Models;

public class Program {
    public static void Main(string[] args)
    {
        var path = args[0];
        var mergedDictionary = new Dictionary<string, string>();
        int totalCount = 0;
        foreach(var csvFile in Directory.GetFiles(path, "*.csv"))
        {
            Console.WriteLine($"Picked up conversions from {csvFile}");
            var dictionary = Helper.GetTextConversionsFromFile(csvFile);
            foreach(var (sourceDialogue, targetText) in dictionary)
            {
                // Don't overwrite transformations with filled target text
                if (mergedDictionary.TryGetValue(sourceDialogue, out var existingTargetText) && !string.IsNullOrEmpty(existingTargetText))
                    continue;
                mergedDictionary[sourceDialogue] = targetText;
            }
            totalCount += dictionary.Count;
        }
        Console.WriteLine($"Cache contains {mergedDictionary.Count} records. There are currently {mergedDictionary.Count((pair) => string.IsNullOrEmpty(pair.Value))} unpredicted records.");
        Helper.WriteToFile(mergedDictionary.Select(s => new DialogueTextConversion(s.Key, s.Value)), Path.Combine(path, "_PregeneratedCache.csv"));
    }
}

