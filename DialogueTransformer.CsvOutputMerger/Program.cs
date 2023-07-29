using DialogueTransformer.Common;
using DialogueTransformer.Common.Models;

public class Program {
    public static void Main(string[] args)
    {
        var path = args[0];
        var mergedDictionary = new Dictionary<string, DialogueTextConversion>();
        int totalCount = 0;
        foreach(var csvFile in Directory.GetFiles(path, "*.csv"))
        {
            var dictionary = Helper.GetTextConversionsFromFile(csvFile);
            foreach(var (sourceDialogue, transformation) in dictionary)
            {
                // Don't overwrite transformations with filled target text
                if (mergedDictionary.TryGetValue(sourceDialogue, out var existingTransformation) && !string.IsNullOrEmpty(existingTransformation.TargetText))
                    continue;
                mergedDictionary[sourceDialogue] = transformation;
            }
            totalCount += dictionary.Count;
        }
        Console.WriteLine($"Cache contains {mergedDictionary.Count} records. There are currently {mergedDictionary.Count((pair) => string.IsNullOrEmpty(pair.Value.TargetText))} unpredicted records.");
        Helper.WriteToFile(mergedDictionary.Values, Path.Combine(path, "_PregeneratedCache.csv"));
    }
}

