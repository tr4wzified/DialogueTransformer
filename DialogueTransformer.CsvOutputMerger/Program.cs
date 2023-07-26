// See https://aka.ms/new-console-template for more information
using CsvHelper;
using DialogueTransformer.Common;
using DialogueTransformer.Common.Models;

public class Program {
    public static void Main(string[] args)
    {
        var path = args[0];
        var mergedDictionary = new Dictionary<string, DialogueTransformation>();
        int totalCount = 0;
        foreach(var csvFile in Directory.GetFiles(path, "*.csv"))
        {
            var dictionary = Helper.GetCachedTransformationsFromCsv(csvFile);
            foreach(var (sourceDialogue, transformation) in dictionary)
            {
                mergedDictionary[sourceDialogue] = transformation;
            }
            totalCount += dictionary.Count;
        }
        Helper.WriteToCsv(mergedDictionary.Values, "./MergedTransformations.csv");
    }
}

