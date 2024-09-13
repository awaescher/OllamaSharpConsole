using System.Text;
using OllamaSharp;
using Spectre.Console;

namespace OllamaApiConsole;

public abstract class OllamaConsole(IOllamaApiClient ollama)
{
	public IOllamaApiClient Ollama { get; } = ollama ?? throw new ArgumentNullException(nameof(ollama));

	public abstract Task Run();

	public static string ReadInput(string prompt = "", string additionalInformation = "")
	{
		const char MULTILINE_OPEN = '[';
		const char MULTILINE_CLOSE = ']';

		if (string.IsNullOrEmpty(additionalInformation))
			additionalInformation = $"Use [red]{Markup.Escape(MULTILINE_OPEN.ToString())}[/] to start and [red]{Markup.Escape(MULTILINE_CLOSE.ToString())}[/] to end multiline input.";

		if (!string.IsNullOrEmpty(prompt))
			AnsiConsole.MarkupLine(prompt);

		if (!string.IsNullOrEmpty(additionalInformation))
			AnsiConsole.MarkupLine(additionalInformation);

		var builder = new StringBuilder();
		bool? isMultiLineActive = null;
		var needsCleaning = false;

		while (!isMultiLineActive.HasValue || isMultiLineActive.Value)
		{
			AnsiConsole.Markup("[blue]> [/]");
			var input = Console.ReadLine() ?? "";

			if (!isMultiLineActive.HasValue)
			{
				isMultiLineActive = input.TrimStart().StartsWith(MULTILINE_OPEN);
				needsCleaning = isMultiLineActive.GetValueOrDefault();
			}

			builder.AppendLine(input);

			if (input.TrimEnd().EndsWith(MULTILINE_CLOSE) && isMultiLineActive.GetValueOrDefault())
				isMultiLineActive = false;
		}

		if (needsCleaning)
			return builder.ToString().Trim().TrimStart(MULTILINE_OPEN).TrimEnd(MULTILINE_CLOSE);

		return builder.ToString().TrimEnd();
	}

	protected async Task<string> SelectModel(string prompt, string additionalInformation = "")
	{
		const string BACK = "..";

		var models = await Ollama.ListLocalModels();
		var modelsWithBackChoice = models.OrderBy(m => m.Name).Select(m => m.Name).ToList();
		if (modelsWithBackChoice.Count == 1)
		{
			return modelsWithBackChoice[0];
		}
		else
		{
			modelsWithBackChoice.Insert(0, BACK);

			if (!string.IsNullOrEmpty(additionalInformation))
				AnsiConsole.MarkupLine(additionalInformation);

			var answer = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.PageSize(10)
						.Title(prompt)
						.AddChoices(modelsWithBackChoice));

			return answer == BACK ? "" : answer;
		}
	}
}
