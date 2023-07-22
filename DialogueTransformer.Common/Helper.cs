using CsvHelper;
using DialogueTransformer.Common.Models;
using Mutagen.Bethesda.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DialogueTransformer.Common
{
    public static class Helper
    {


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static Dictionary<FormKey, DialogTransformation> GetTranslationsFromCsv(string path)
        {
            Dictionary<FormKey, DialogTransformation> dialogTranslations = new();
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var record = csv.GetRecord<DialogTransformation>();
                    if (record != null)
                        dialogTranslations.Add(FormKey.Factory(record.FormKey), record);
                }
            }
            return dialogTranslations;
        }

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
