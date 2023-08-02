using CsvHelper;
using CsvHelper.Configuration;
using DialogueTransformer.Common.Interfaces;
using DialogueTransformer.Common.Models;
using Mutagen.Bethesda.Plugins;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using static DialogueTransformer.Common.Enumerations;

namespace DialogueTransformer.Common
{
    public static class Helper
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static Dictionary<FormKey, DialogueTextOverride> GetOverridesFromFile(string path)
        {
            Dictionary<FormKey, DialogueTextOverride> transformations = new();
            if (!File.Exists(path))
                return transformations;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null
            };
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                try
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var record = csv.GetRecord<DialogueTextOverride>();
                        transformations.Add(FormKey.Factory(record.FormKey), record);
                    }
                }
                catch(Exception ex)
                {
                    Console.Write(ex.ToString());
                    return new();
                }
            }
            return transformations;
        }
        public static Dictionary<string, string> GetTextConversionsFromFile(string path)
        {
            Dictionary<string, string> conversions = new();
            if (!File.Exists(path))
                return conversions;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null
            };
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                try
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var conversion = csv.GetRecord<DialogueTextConversion>();
                        if(!string.IsNullOrEmpty(conversion.TargetText))
                            conversions.TryAdd(conversion.SourceText, conversion.TargetText);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return new();
                }
            }
            return conversions;
        }
        public static bool WriteToFile<T>(IEnumerable<T> source, string path)
        {
            try
            {
                if (!File.Exists(path))
                    File.Create(path);

                using (var writer = new StreamWriter(path))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<T>();
                    csv.NextRecord();
                    foreach (var item in source)
                    {
                        csv.WriteRecord(item);
                        csv.NextRecord();
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"> Failed to write to {path}: {ex}");
                return false;
            }
        }

        public static Dictionary<DialogueModelType, IDialogueModel> GetModels(string dataFolderPath) => Assembly.GetExecutingAssembly()
                                                                                                        .GetExportedTypes()
                                                                                                        .Where(x => !x.IsInterface && !x.IsAbstract && typeof(IDialogueModel).IsAssignableFrom(x))
                                                                                                        .Select(x => (IDialogueModel)Activator.CreateInstance(x, dataFolderPath)!)
                                                                                                        .ToDictionary(x => x.Type);
        public static Dictionary<DialogueModelType, IDialogueModel> GetInstalledModels(string dataFolderPath) => GetModels(dataFolderPath).Where(x => x.Value.Installed).ToDictionary(x => x.Key, x => x.Value);

        public static ulong GetTotalMemory()
        {
            MEMORYSTATUSEX memStatus = new();
            if (GlobalMemoryStatusEx(memStatus))
                return memStatus.ullTotalPhys;

            return 0;
        }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(this);
        }
    }
}
