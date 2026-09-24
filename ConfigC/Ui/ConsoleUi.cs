using ConfigC.Copy;
using ConfigC.Steam;
using Spectre.Console;

namespace ConfigC.Ui;

/// <summary>Drives the interactive terminal flow: pick a Steam profile, pick a target, copy.</summary>
public sealed class ConsoleUi
{
    public void Run()
    {
        PrintHeader();

        var userDataPath = ResolveUserDataPath();
        if (userDataPath is null)
        {
            AnsiConsole.MarkupLine("[red]No Steam \"userdata\" folder could be found. Exiting.[/]");
            return;
        }

        var keepGoing = true;
        while (keepGoing)
        {
            keepGoing = RunOneCopy(userDataPath);
        }

        AnsiConsole.Write(new Rule("[grey]Goodbye[/]").RuleStyle("grey"));
    }

    private static void PrintHeader()
    {
        AnsiConsole.Write(new FigletText("ConfigC").Color(Color.SteelBlue1));
        AnsiConsole.MarkupLine("[grey]Copy Steam per-account config & local save data between profiles on this PC.[/]");
        AnsiConsole.WriteLine();
    }

    private static string? ResolveUserDataPath()
    {
        var autoDetected = SteamPathResolver.FindUserDataPath();
        if (autoDetected is not null)
        {
            AnsiConsole.MarkupLine($"[green]Found Steam userdata:[/] {autoDetected.EscapeMarkup()}");
            return autoDetected;
        }

        AnsiConsole.MarkupLine("[yellow]Could not auto-detect your Steam installation.[/]");
        var manualPath = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the full path to your Steam [bold]userdata[/] folder ([grey]leave empty to quit[/]):")
                .AllowEmpty()
                .Validate(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return ValidationResult.Success();
                    return Directory.Exists(path)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]That folder does not exist.[/]");
                }));

        return string.IsNullOrWhiteSpace(manualPath) ? null : manualPath;
    }

    /// <returns>Whether the user wants to copy another profile pair.</returns>
    private bool RunOneCopy(string userDataPath)
    {
        var accounts = SteamAccountRepository.GetAccounts(userDataPath);
        if (accounts.Count < 2)
        {
            AnsiConsole.MarkupLine("[red]At least two Steam profiles are required to copy between them.[/]");
            return false;
        }

        ShowAccountsTable(accounts);

        var source = AnsiConsole.Prompt(
            new SelectionPrompt<SteamAccount>()
                .Title("Copy [bold]from[/] which profile?")
                .UseConverter(a => a.DisplayName)
                .AddChoices(accounts));

        var destinationChoices = accounts.Where(a => a.SteamId != source.SteamId).ToList();
        var destination = AnsiConsole.Prompt(
            new SelectionPrompt<SteamAccount>()
                .Title("Copy [bold]to[/] which profile?")
                .UseConverter(a => a.DisplayName)
                .AddChoices(destinationChoices));

        var fileCount = FileCopyService.EnumerateFiles(source.FolderPath).Count;
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]{fileCount}[/] file(s) will be copied from [aqua]{source.DisplayName.EscapeMarkup()}[/] to [aqua]{destination.DisplayName.EscapeMarkup()}[/].");
        AnsiConsole.MarkupLine("[yellow]Existing files with the same name in the destination profile will be overwritten.[/]");

        if (!AnsiConsole.Confirm("Continue?"))
        {
            return AskToContinue();
        }

        var result = CopyWithProgress(source, destination, fileCount);
        ShowSummary(result);

        return AskToContinue();
    }

    private static void ShowAccountsTable(IReadOnlyList<SteamAccount> accounts)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Profile");
        table.AddColumn("SteamID");

        foreach (var account in accounts)
        {
            table.AddRow(account.PersonaName ?? "[grey]unknown[/]", account.SteamId);
        }

        AnsiConsole.Write(table);
    }

    private static CopyResult CopyWithProgress(SteamAccount source, SteamAccount destination, int fileCount)
    {
        CopyResult? result = null;

        AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask("Copying files", maxValue: Math.Max(fileCount, 1));

                result = FileCopyService.CopyRecursively(
                    source.FolderPath,
                    destination.FolderPath,
                    onFileCopied: (index, _, _, _) => task.Value = index);
            });

        return result!;
    }

    private static void ShowSummary(CopyResult result)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(
            $"[green]{result.FilesCopied}[/] file(s) copied ([bold]{FormatBytes(result.BytesCopied)}[/]).")
        {
            Header = new PanelHeader("Done"),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);

        if (result.Failures.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]{result.Failures.Count} file(s) could not be copied:[/]");
            foreach (var failure in result.Failures.Take(10))
            {
                AnsiConsole.MarkupLine($"  [red]-[/] {failure.EscapeMarkup()}");
            }
        }
    }

    private static bool AskToContinue()
    {
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("Copy another profile pair?", defaultValue: false);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
