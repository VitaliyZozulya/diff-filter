using System.IO;
using System.Text.RegularExpressions;
using CommandLine;

namespace DiffFilter
{
    public class Arguments
    {
        [Option('s', Required = true, HelpText = "Input diff file.")]
        public string InputFile { get; set; }

        [Option('d', Required = true, HelpText = "Output diff file.")]
        public string OutputFile { get; set; }

        [Option('i', Required = false, Default = false, HelpText = "Inverse mode.")]
        public bool IsInversed { get; set; }

        [Option('r', Required = true, HelpText = "Hunk regex pattern.")]
        public string RegexPattern { get; set; }
    }

    class Program
    {
        private const string WindowsNewLine = "\r\n";
        private const string UnixNewLine = "\n";

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Arguments>(args).WithParsed(FilterDiff);
        }

        private static void FilterDiff(Arguments args)
        {
            string diffRegex = @"(?s)^(diff --git.+?\n.+?\n.+?\n.+?\n)(.+?)(?=(diff --git)|\z)";
            string hunkRegex = @"(?s)^@@.+?\n(.+?)(?=(@@)|\z)";

            string input = File.ReadAllText(args.InputFile);
            MatchCollection diffs = Regex.Matches(input, diffRegex, RegexOptions.Multiline);
            if (diffs.Count > 0)
            {
                using (TextWriter writer = File.CreateText(args.OutputFile))
                {
                    writer.NewLine = UnixNewLine;
                    foreach (Match diff in diffs)
                    {
                        string diffHeader = diff.Groups[1].Value;
                        MatchCollection hunks = Regex.Matches(diff.Groups[2].Value, hunkRegex, RegexOptions.Multiline);
                        if (hunks.Count > 0)
                        {
                            bool isHeaderWritten = false;
                            foreach (Match hunk in hunks)
                            {
                                Match hunkMatch = Regex.Match(hunk.Groups[1].Value, args.RegexPattern, RegexOptions.Multiline);

                                bool result = hunkMatch.Success;
                                if (args.IsInversed)
                                {
                                    result = !result;
                                }

                                if (result)
                                {
                                    if (!isHeaderWritten)
                                    {
                                        writer.Write(diffHeader.Replace(WindowsNewLine, UnixNewLine));
                                        isHeaderWritten = true;
                                    }

                                    writer.Write(hunk.Value.Replace(WindowsNewLine, UnixNewLine));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
