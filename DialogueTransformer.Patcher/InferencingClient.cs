using Noggog.Utility;
using System.Diagnostics;
using System.Text;

namespace DialogueTransformer.Patcher
{
    /// <summary>
    /// Inferencing client, basically a class around the Python inferencing executable
    /// </summary>
    internal class InferencingClient
    {
        private Process _Process { get; set; }
        public InferencingClient(string exePath, string modelPath, string prefix)
        {
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
                Arguments = $@"""{modelPath}"" ""{prefix}""",
            };
            _Process = new Process() { StartInfo = startInfo, EnableRaisingEvents = false };
            _Process.Start();
            ChildProcessTracker.AddProcess(_Process);
        }
        ~InferencingClient()
        {
            _Process.Kill();
        }
        private string WriteToProcess(string text)
        {
            _Process.StandardInput.WriteLine(text);
            _Process.StandardInput.Flush();
            return _Process.StandardOutput.ReadLine() ?? string.Empty;
        }
        private string FixInferencingErrors(string prediction)
        {
            if (prediction.Length == 0)
                return prediction;

            var outputBuilder = new StringBuilder();
            var words = prediction.Split(' ');
            for(int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                var greaterThanIndex = word.IndexOf(">");
                // Correct Alias=Player>, pronouns and <Global> to include the <, model training error
                var aliasIndex = word.IndexOf("Alias");
                if (aliasIndex != -1 && greaterThanIndex != -1)
                    word = word.Replace("Alias", "<Alias");

                var globalIndex = word.IndexOf("Global");
                if (globalIndex != -1 && greaterThanIndex != -1)
                    word = word.Replace("Global", "<Global");

                if (i == words.Length - 1)
                    outputBuilder.Append(word);
                else
                {
                    outputBuilder.Append(word);
                    outputBuilder.Append(" ");
                }
            }
            return outputBuilder.ToString();
        }

        public string Inference(string text) => FixInferencingErrors(WriteToProcess(text));
    }
}
