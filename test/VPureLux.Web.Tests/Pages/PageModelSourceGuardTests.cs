using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace VPureLux.Pages;

public class PageModelSourceGuardTests
{
    [Fact]
    public void Web_PageModels_Should_Not_Use_Low_Fixed_MaxResultCount_Caps()
    {
        var pagesDirectory = GetRepoDirectory("src/VPureLux.Web/Pages");
        var offenders = Directory
            .GetFiles(pagesDirectory, "*.cshtml.cs", SearchOption.AllDirectories)
            .Select(file => new
            {
                File = file,
                Matches = Regex
                    .Matches(File.ReadAllText(file), @"MaxResultCount\s*=\s*(100|199|500|1000)\b")
                    .Select(match => match.Value)
                    .ToArray()
            })
            .Where(result => result.Matches.Length > 0)
            .Select(result => $"{Path.GetRelativePath(pagesDirectory, result.File)}: {string.Join(", ", result.Matches)}")
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    private static string GetRepoDirectory(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
