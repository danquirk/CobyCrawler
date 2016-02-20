using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobyCrawler
{
    class Program
    {
        // crawl directories under <targetDir> (E:\GitHub\CSharp) which has GitHub C# repos
        // find all .csproj files
        // run 'codeformatter.exe format <csprojTarget>' > <fileNameAnalyzerResults>.txt
        // crawl directories for *AnalyzerResults.txt and aggregate results

        private static readonly string rootTargetDir = @"E:\GitHub\CSharp\";
        private static readonly string codeformatterExe = @"E:\GitHub\codeformatter\bin\codeformatter.exe";
        private static readonly string codeformatterArgs = "format";
        private static readonly string resultsDir = @"E:\CodeformatterResults\";
        private static readonly string resultFile = resultsDir + "_Results.txt";
        private static readonly string projFileListPath = rootTargetDir + "ProjectFiles.txt";

        static void Main(string[] args)
        {
            RunAnalyzers(rootTargetDir);
            AggregateResults(rootTargetDir);
        }

        private static void AggregateResults(string targetDir)
        {
            var resultFiles = GetFiles(resultsDir, "*CodeFormatterResults.txt");
            var resultText = "ProjectName\tAnalyzerName\tFilesInProject\tLinesOfCodeInProject\tDiagnosticCount\r\n";
            foreach(var file in resultFiles)
            {
                var projName = file.Substring(0, file.LastIndexOf('_'));
                var text = File.ReadAllLines(file).Select(line => String.Format("{0}\t{1}", projName, line));
                resultText += text.Aggregate((x,y) => x + "\r\n" + y) + "\r\n";
            }
            File.WriteAllText(resultFile, resultText);
        }

        private static void RunAnalyzers(string targetDir)
        {
            var targetProjFiles = File.Exists(projFileListPath) ? 
                File.ReadAllLines(projFileListPath).ToList()
                : GetFiles(targetDir, "*.csproj", projFileListPath).ToList();

            Parallel.For(0, targetProjFiles.Count(), i =>
            {
                var projFilePath = targetProjFiles[i];
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo(codeformatterExe, codeformatterArgs + " " + projFilePath);
                    startInfo.RedirectStandardInput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;

                    var p = System.Diagnostics.Process.Start(startInfo);
                    Console.WriteLine(projFilePath);
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        Console.WriteLine(String.Format("Failed: {0} with exit code {1}", projFilePath, p.ExitCode));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Exception {0}", e.Message));
                }
            });
        }

        public static HashSet<string> GetFiles(string targetDir, string searchPattern, string logPath = null)
        {
            var projPaths = new HashSet<string>();
            
            var dirs = Directory.GetDirectories(targetDir, "*", SearchOption.AllDirectories).ToList();
            dirs.Add(targetDir);
            foreach(var dir in dirs)
            {
                Console.WriteLine(dir);
                Directory.GetFiles(dir, searchPattern)
                    .ToList().ForEach(proj => projPaths.Add(proj));
            }

            if(logPath != null)
            {
                File.WriteAllLines(logPath, projPaths.ToArray());
            }

            return projPaths;           
        }
    }
}
