using Octokit;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: CoverageAnnotator <lcov-file>");
                Environment.Exit(1);
            }

            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".";
            var lcovFile = Path.Combine(workspace, args[0]);
            if (!File.Exists(lcovFile))
            {
                Console.Error.WriteLine($"LCOV file not found: {lcovFile}");
                Environment.Exit(1);
            }

            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var client = new GitHubClient(new ProductHeaderValue("coverage-reporter"))
            {
                Credentials = new Credentials(token)
            };

            var repoOwner = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_OWNER");
            var repoName = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")!.Split('/')[1];
            var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");

            var annotations = new List<NewCheckRunAnnotation>();

            string? currentFile = null;
            foreach (var line in File.ReadAllLines(lcovFile))
            {
                if (line.StartsWith("SF:"))
                {
                    currentFile = line[3..];
                }
                else if (line.StartsWith("DA:"))
                {
                    var parts = line[3..].Split(',');
                    var lineNumber = int.Parse(parts[0]);
                    var count = int.Parse(parts[1]);

                    if (count == 0 && currentFile != null)
                    {
                        annotations.Add(new NewCheckRunAnnotation(currentFile, lineNumber, lineNumber,
                            CheckAnnotationLevel.Warning, "Line not covered by tests"));
                    }
                }
            }

            var newCheckRun = new NewCheckRun("Coverage Report", sha)
            {
                Status = CheckStatus.Completed,
                Conclusion = CheckConclusion.Neutral,
                Output = new NewCheckRunOutput("Uncovered Lines", $"{annotations.Count} uncovered lines")
                {
                    Annotations = annotations.Count > 50 ? annotations.GetRange(0, 50) : annotations
                }
            };

            await client.Check.Run.Create(repoOwner, repoName, newCheckRun);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to create check run: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
