using DialogueTransformer.Common;
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
            var filePath = Path.Combine(exePath, Consts.INFERENCING_EXE_FILE);
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = filePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = exePath,
                WindowStyle = ProcessWindowStyle.Normal,
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

        public string Inference(string text) => WriteToProcess(text);
    }
}
