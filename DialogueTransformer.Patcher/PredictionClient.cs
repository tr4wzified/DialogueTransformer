using DialogueTransformer.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DialogueTransformer.Patcher
{
    /// <summary>
    /// Khajiit Speak Prediction Engine, calling the compiled python Predict executable directly
    /// </summary>
    internal class PredictionClient
    {
        private Process _Process { get; set; }
        private string Separator { get; set; }
        public PredictionClient(string exePath, string modelPath, string prefix, string separator = "||")
        {
            Separator = separator;
            var filePath = Path.Combine(exePath, "DialoguePredictor.exe");
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = exePath,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $@"""{modelPath}"" ""{prefix}"" ""{separator}"""
            };
            _Process = new Process() { StartInfo = startInfo, EnableRaisingEvents = false };
            _Process.Start();
        }
        ~PredictionClient()
        {
            _Process.Kill();
        }
        private IEnumerable<string> PredictInternal(IEnumerable<string> text)
        {
            _Process.StandardInput.WriteLine(string.Join(Separator, text));
            _Process.StandardInput.Flush();
            var line = _Process.StandardOutput.ReadLine() ?? string.Empty;
            return line.Split(Separator);
        }
        private string FixPredictionErrors(string prediction)
        {
            if (prediction.Length == 0)
                return prediction;

            var outputBuilder = new StringBuilder();
            var words = prediction.Split(' ');
            for(int i = 0; i < words.Length; i++)
            {
                // Correct Alias=Player> and pronouns to include the <, model training error
                var aliasIndex = words[i].IndexOf("Alias");
                var greaterThanIndex = words[i].IndexOf(">");
                if (aliasIndex != -1 && greaterThanIndex != -1)
                    words[i] = words[i].Replace("Alias", "<Alias");

                if (i == words.Length - 1)
                    outputBuilder.Append(words[i]);
                else
                {
                    outputBuilder.Append(words[i]);
                    outputBuilder.Append(" ");
                }
            }
            return outputBuilder.ToString();
        }

        public IEnumerable<string> Predict(IEnumerable<string> text) => PredictInternal(text).Select(prediction => FixPredictionErrors(prediction));

        public void Dispose() => _Process.Kill();
    }
}
