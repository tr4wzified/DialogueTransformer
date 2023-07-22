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
    internal class PredictionClient : IDisposable
    {
        private Process _Process { get; set; }
        public PredictionClient(string exePath)
        {
            var filePath = Path.Combine(exePath, "Predict.exe");
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = exePath,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            _Process = new Process() { StartInfo = startInfo, EnableRaisingEvents = false };
            _Process.Start();
        }
        private string PredictInternal(string text)
        {
            _Process.StandardInput.WriteLine(text);
            _Process.StandardInput.Flush();
            return _Process.StandardOutput.ReadLine() ?? string.Empty;
        }
        private string FixPredictionErrors(string prediction)
        {
            var outputBuilder = new StringBuilder();
            var words = prediction.Split(' ');
            for(int i = 0; i < words.Length; i++)
            {
                // Fix array chars at start and end
                if (i == 0)
                    words[i] = words[i].Substring(2);
                else if (i == words.Length - 1)
                    words[i] = words[i].Substring(0, words[i].Length - 2);

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

        public string Predict(string text) => FixPredictionErrors(PredictInternal(text));

        public void Dispose() => _Process.Kill();
    }
}
