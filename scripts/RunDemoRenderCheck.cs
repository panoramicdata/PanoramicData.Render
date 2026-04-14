#:package Microsoft.Playwright
#:property JsonSerializerIsReflectionEnabledByDefault=true

#pragma warning disable CA2007

using System.Text.RegularExpressions;
using Microsoft.Playwright;

var options = ParseArgs(args);
if (!options.TryGetValue("docx", out var docxPath) || string.IsNullOrWhiteSpace(docxPath))
{
	PrintUsage("Missing required argument: --docx <path>");
	return 1;
}

if (!File.Exists(docxPath))
{
	Console.Error.WriteLine($"DOCX file not found: {docxPath}");
	return 1;
}

var url = options.TryGetValue("url", out var urlValue) && !string.IsNullOrWhiteSpace(urlValue)
	? urlValue
	: "http://localhost:5240";

var pdfPath = options.TryGetValue("pdf", out var pdfValue) ? pdfValue : null;
if (!string.IsNullOrWhiteSpace(pdfPath) && !File.Exists(pdfPath))
{
	Console.Error.WriteLine($"PDF file not found: {pdfPath}");
	return 1;
}

var timeoutSeconds = options.TryGetValue("timeout", out var timeoutValue) && int.TryParse(timeoutValue, out var parsedTimeout)
	? parsedTimeout
	: 90;

var headless = !options.ContainsKey("headed");
var screenshotPath = options.TryGetValue("screenshot", out var screenshotValue) && !string.IsNullOrWhiteSpace(screenshotValue)
	? screenshotValue
	: Path.Combine(Path.GetTempPath(), "demo-render-check.png");

Console.WriteLine($"Opening demo: {url}");
Console.WriteLine($"DOCX: {docxPath}");
if (!string.IsNullOrWhiteSpace(pdfPath))
{
	Console.WriteLine($"PDF:  {pdfPath}");
}

try
{
	using var playwright = await Playwright.CreateAsync();
	await using var browser = await LaunchBrowserAsync(playwright, headless);

	var page = await browser.NewPageAsync();
	await page.GotoAsync(url, new PageGotoOptions
	{
		WaitUntil = WaitUntilState.NetworkIdle,
		Timeout = 30_000
	});

	var inputs = page.Locator("input[type='file']");
	if (await inputs.CountAsync() < 1)
	{
		Console.Error.WriteLine("Could not find upload inputs on the demo page.");
		return 2;
	}

	await inputs.Nth(0).SetInputFilesAsync(docxPath);
	if (!string.IsNullOrWhiteSpace(pdfPath))
	{
		if (await inputs.CountAsync() < 2)
		{
			Console.Error.WriteLine("Could not find the PDF upload input on the demo page.");
			return 2;
		}

		await inputs.Nth(1).SetInputFilesAsync(pdfPath);
	}

	var renderButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Render" });
	await renderButton.ClickAsync();

	var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
	while (DateTime.UtcNow < deadline)
	{
		var errorAlert = page.Locator(".alert-danger");
		if (await errorAlert.CountAsync() > 0)
		{
			var message = (await errorAlert.First.InnerTextAsync()).Trim();
			Console.Error.WriteLine($"Render failed: {message}");
			await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
			Console.Error.WriteLine($"Screenshot saved: {screenshotPath}");
			return 3;
		}

		var infoAlert = page.Locator(".alert-info");
		var isRendering = await infoAlert.CountAsync() > 0;
		if (!isRendering)
		{
			var successAlert = page.Locator(".alert-success");
			if (await successAlert.CountAsync() > 0)
			{
				break;
			}
		}

		await Task.Delay(500);
	}

	var success = page.Locator(".alert-success");
	if (await success.CountAsync() == 0)
	{
		Console.Error.WriteLine($"Render did not complete within {timeoutSeconds}s.");
		await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
		Console.Error.WriteLine($"Screenshot saved: {screenshotPath}");
		return 4;
	}

	var status = (await success.First.InnerTextAsync()).Trim();
	Console.WriteLine($"Status: {status}");

	var html = await page.ContentAsync();
	var matches = Regex.Matches(html, @"\d+(?:\.\d+)?% match")
		.Select(m => m.Value)
		.Distinct(StringComparer.Ordinal)
		.ToArray();

	if (matches.Length > 0)
	{
		Console.WriteLine("Page matches:");
		foreach (var match in matches)
		{
			Console.WriteLine($"- {match}");
		}
	}

	await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
	Console.WriteLine($"Screenshot saved: {screenshotPath}");
	return 0;
}
catch (PlaywrightException ex)
{
	Console.Error.WriteLine(ex.Message);
	Console.Error.WriteLine("If browsers are not installed, run: pwsh ./bin/Debug/net10.0/playwright.ps1 install chromium");
	return 5;
}

static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, bool headless)
{
	try
	{
		return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = headless
		});
	}
	catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
	{
		Console.WriteLine("Playwright browser not found; installing Chromium...");
		Microsoft.Playwright.Program.Main(["install", "chromium"]);

		return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = headless
		});
	}
}

static Dictionary<string, string> ParseArgs(string[] rawArgs)
{
	var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	for (var i = 0; i < rawArgs.Length; i++)
	{
		var arg = rawArgs[i];
		if (!arg.StartsWith("--", StringComparison.Ordinal))
		{
			continue;
		}

		var key = arg[2..];
		if (string.Equals(key, "headed", StringComparison.OrdinalIgnoreCase))
		{
			result[key] = "true";
			continue;
		}

		if (i + 1 < rawArgs.Length && !rawArgs[i + 1].StartsWith("--", StringComparison.Ordinal))
		{
			result[key] = rawArgs[i + 1];
			i++;
		}
		else
		{
			result[key] = "true";
		}
	}

	return result;
}

static void PrintUsage(string? error = null)
{
	if (!string.IsNullOrWhiteSpace(error))
	{
		Console.Error.WriteLine(error);
	}

	Console.WriteLine("Usage:");
	Console.WriteLine("  dotnet run --file scripts/RunDemoRenderCheck.cs -- --docx <path> [--pdf <path>] [--url <http://localhost:5240>] [--timeout <seconds>] [--headed] [--screenshot <path>]");
}
