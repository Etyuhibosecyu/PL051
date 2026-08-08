#define VERIFY
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Utils;
using MsBox.Avalonia;
using NStar.Dictionaries;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Xml;
using static PL051.NStar.NStarType;
using static PL051.NStar.SemanticTree;

namespace PL051.NStar.Views;

public partial class MainView : UserControl
{
	private Assembly? compiledAssembly;
	private int inputHighlightTarget = -1;
	private readonly String enteredText = [];

	private static readonly ImmutableArray<string> minorVersions = ["0.9"];
	private static readonly ImmutableArray<string> langs = ["C#"];
	private static readonly string AlphanumericCharactersWithoutDot
		= "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
	private static readonly G.SortedSet<String> AutoCompletionList = [.. new List<String>(Abstract, "break", "case", "Class",
		"const", "Constructor", "continue", DefaultConst, "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent",
		"extern", False, "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "Megaclass",
		"multiconst", "Namespace", "new", NullString, "Operator", "out", "override", "params", "private", "protected",
		"public", "readonly", "ref", RepeatString, ReturnString, Sealed, Static, "Struct", "switch", "this", "throw", "true",
		"using", WhileString, "and", "or", "xor", "is", "typeof", "sin", "cos", "tan", "asin", "acos", "atan", "ln",
		"Infty", "Uncty", "Pi", "E", "CloseOnReturnWith", "pow", "tetra", "penta", "hexa").AddRange(PrimitiveTypes.Keys)
		.AddRange(ExtraTypes.Convert(x => x.Key.Namespace.Concat(".").AddRange(x.Key.Type)))
		.AddRange(PublicFunctions.Keys)];
	private static readonly G.SortedSet<string> AutoCompletionAfterDotList = [.. PrimitiveTypes.Values.ToList()
		.AddRange(ExtraTypes.Values).ConvertAndJoin(x =>
		x.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
		.ToList(x => PropertyMappingBack(x.Name))
		.AddRange(x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
		.ToList(x => FunctionMappingBack(x.Name)))).Filter(x => !x.Contains('_'))];
	private static readonly Mirror<char, char> AutoPairingList = new()
	{
		{ '\'', '\'' }, { '\"', '\"' }, { '(', ')' }, { '[', ']' }, { '{', '}' }
	};

	public MainView()
	{
		CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		InitializeComponent();
		DragDrop.SetAllowDrop(TextBoxInput, true);
		TextBoxInput.Options.EnableTextDragDrop = true;
		TextBoxInput.AddHandler(PointerReleasedEvent, TextBoxInput_PointerReleased, handledEventsToo: true);
		TextBoxInput.TextArea.TextEntering += TextBoxInput_TextArea_TextEntering;
		TextBoxInput.TextArea.TextEntered += TextBoxInput_TextArea_TextEntered;
		using (var stream = new MemoryStream(NStar.Resources.SyntaxHighlighting))
		{
			using var reader = new XmlTextReader(stream);
			TextBoxInput.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
			var spans = TextBoxInput.SyntaxHighlighting.MainRuleSet.Spans;
			var nestedCommentSpans = spans.FindAll(x => x.SpanColor.Foreground.ToString()?.Contains("#ffbfbf00") ?? false);
			nestedCommentSpans[^1].RuleSet.Spans.AddRange(nestedCommentSpans);
			var rules = TextBoxInput.SyntaxHighlighting.MainRuleSet.Rules;
			var stringRule = rules.Find(x => x.Color.Foreground.ToString()?.Contains("#ffbf4000") ?? false);
			var stringSpans = spans.FindAll(x => x.SpanColor.Foreground.ToString()?.Contains("#ffbf4000") ?? false);
			stringSpans[^1].RuleSet.Rules.Add(stringRule ?? throw new InvalidOperationException());
			stringSpans[^1].RuleSet.Spans.AddRange(stringSpans);
			stringSpans[^1].RuleSet.Spans.AddRange(nestedCommentSpans);
		}
		_ = TextBoxInput.TextArea.TextView.GetDocumentLineByVisualTop(TextBoxInput.TextArea.TextView.ScrollOffset.Y).LineNumber;
	}

	private void UserControl_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
		ScrollViewerMain.Width = TopLevel.GetTopLevel(this)?.Width ?? 1024;
		ScrollViewerMain.Height = TopLevel.GetTopLevel(this)?.Height ?? 768;
		CopyrightsView.Width = TopLevel.GetTopLevel(this)?.Width ?? 1024;
		CopyrightsView.Height = TopLevel.GetTopLevel(this)?.Height ?? 768;
		TextBoxInput.Height = TextBoxInput.MaxHeight = TextBoxInput.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 5 / 13;
		ButtonExecute.Height = ButtonExecute.MaxHeight = ButtonExecute.MinHeight
			= ButtonSaveExe.Height = ButtonSaveExe.MaxHeight = ButtonSaveExe.MinHeight
			= ButtonOpenCode.Height = ButtonOpenCode.MaxHeight = ButtonOpenCode.MinHeight
			= ButtonSaveCode.Height = ButtonSaveCode.MaxHeight = ButtonSaveCode.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 1 / 13;
		TextBoxOutput.Height = TextBoxOutput.MaxHeight = TextBoxOutput.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 2 / 13;
		TextBoxErrors.Height = TextBoxErrors.MaxHeight = TextBoxErrors.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 3 / 13;
	}

	private static string FunctionMappingBack(String function) => function.ToString() switch
	{
		nameof(function.AddRange) => "Add",
		nameof(DateTime.IsDaylightSavingTime) => "IsSummertime",
		_ => function.ToString(),
	};

	private static string PropertyMappingBack(String property) => property.ToString() switch
	{
		nameof(DateTime.UtcNow) => "UTCNow",
		_ => property.ToString(),
	};

	private void UpdateInputPos()
	{
		var line = TextBoxInput.Document.GetLineByOffset(TextBoxInput.SelectionStart).LineNumber;
		TextBlockLine.Text = "Line " + line;
		TextBlockPos.Text = "Pos " + (TextBoxInput.SelectionStart - TextBoxInput.Document.Lines[line - 1].Offset);
	}

	private void SetupEnteredText()
	{
		var textBeforeCursor = TextBoxInput.Text.AsSpan(..TextBoxInput.SelectionStart);
		var i = textBeforeCursor.Length - 1;
		for (; i >= 0; i--)
			if (!AlphanumericCharactersWithoutDot.Contains(textBeforeCursor[i]))
				break;
		if (i >= 0 && textBeforeCursor[i] == '.')
			enteredText.Replace(textBeforeCursor[i..]);
		else
			enteredText.Replace(textBeforeCursor[(i + 1)..]);
	}

	private void TextBoxInput_KeyUp(object? sender, KeyEventArgs e)
	{
		if (e.KeyModifiers == KeyModifiers.Control)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				ButtonExecute_Click(ButtonExecute, e);
			}
			else if (e.Key is Key.Y or Key.Z)
			{
				UpdateInputPos();
				SetupEnteredText();
			}
		}
	}

	private void TextBoxInput_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		UpdateInputPos();
		SetupEnteredText();
		if (inputHighlightTarget >= 0)
		{
			TextBoxInput.SelectionStart = inputHighlightTarget;
			TextBoxInput.SelectionLength = 0;
			inputHighlightTarget = -1;
		}
	}

	private void TextBoxInput_TextChanged(object? sender, EventArgs e)
	{
		UpdateInputPos();
		compiledAssembly = null;
	}

	private CompletionWindow? completionWindow;

	private void TextBoxInput_TextArea_TextEntered(object? sender, TextInputEventArgs e)
	{
		if (TextBoxInput.SelectionLength == 0
			&& (TextBoxInput.SelectionStart >= TextBoxInput.Text.Length
			|| TextBoxInput.Text[TextBoxInput.SelectionStart] is '\r' or '\n' or ',' or ')' or ']' or '}')
			&& e.Text is not null && e.Text.Length != 0)
		{
			if (AutoPairingList.TryGetValue(e.Text[^1], out var pair))
			{
				TextBoxInput.Document.Insert(TextBoxInput.SelectionStart, "" + pair);
				TextBoxInput.SelectionStart--;
			}
			else if (TextBoxInput.SelectionStart < TextBoxInput.Text.Length
				&& e.Text[^1] == TextBoxInput.Text[TextBoxInput.SelectionStart])
				TextBoxInput.Document.Remove(TextBoxInput.SelectionStart, 1);
		}
		// Open code completion after the user has pressed dot:
		completionWindow = new CompletionWindow(TextBoxInput.TextArea);
		var completionData = completionWindow.CompletionList.CompletionData;
		if (e.Text == ".")
			enteredText.Replace(e.Text);
		else
			enteredText.AddRange(e.Text ?? "");
		if (enteredText.StartsWith('.'))
			completionData.AddRange(AutoCompletionAfterDotList.Filter(x => x.StartsWith(enteredText[1..])).Convert(x =>
				new MyCompletionData(x.ToString(), enteredText.Length - 1)));
		else
			completionData.AddRange(AutoCompletionList.Filter(x => x.StartsWith(enteredText)).Convert(x =>
				new MyCompletionData(x.ToString(), enteredText.Length)));
		if (completionData.Count == 0)
			return;
		completionWindow.Show();
		completionWindow.Closed += (_, _) => completionWindow = null;
	}

	private void TextBoxInput_TextArea_TextEntering(object? sender, TextInputEventArgs e)
	{
		if (TextBoxInput.SelectionLength == 0
			&& (TextBoxInput.SelectionStart >= TextBoxInput.Text.Length
			|| TextBoxInput.Text[TextBoxInput.SelectionStart] is '\r' or '\n'))
			e.Text = e.Text?.Replace(";", "");
		if (e.Text?.Length > 0 && completionWindow is not null && !char.IsLetterOrDigit(e.Text[0]))
		{
			// Whenever a non-letter is typed while the completion window is open,
			// insert the currently selected element.
			completionWindow.CompletionList.RequestInsertion(e);
		}
		// Do not set e.Handled=true.
		// We still want to insert the character that was typed.
	}

	private void TextBoxErrors_DoubleTapped(object? sender, TappedEventArgs e)
	{
		var before = TextBoxErrors.Text?.ToNString().RemoveEnd(Min(TextBoxErrors.SelectionStart,
			TextBoxErrors.Text.Length));
		var after = TextBoxErrors.Text?.ToNString().Skip(TextBoxErrors.SelectionStart).GetBefore("\r\n");
		if (before is null || after is null)
			return;
		before.AddRange(after);
		var line = before.GetAfterLast("\r\n");
		if (line.Length == 0)
			line.Replace(before);
		line.GetBeforeSetAfter(" in line ");
		if (line.Length == 0)
			return;
		var lineN = line.GetBeforeSetAfter(" at position ");
		var position = line.GetBefore(": ");
		if (!(int.TryParse(lineN.ToString(), out var y) && int.TryParse(position.ToString(), out var x)))
			return;
		TextBoxInput.ScrollTo(y, x);
		inputHighlightTarget = TextBoxInput.Document.Lines[y - 1].Offset + x;
	}

	private void ButtonExecute_Click(object? sender, RoutedEventArgs e) => Execute();

	private async void ButtonOpenCode_Click(object? sender, RoutedEventArgs e) => await OpenCode();

	private async void ButtonSaveCode_Click(object? sender, RoutedEventArgs e) => await SaveCode();

	private async void ButtonSaveExe_Click(object? sender, RoutedEventArgs e) => await SaveExe();

	private void Settings_Click(object? sender, RoutedEventArgs e) => SettingsView.IsVisible = true;

	private void Copyrights_Click(object? sender, RoutedEventArgs e) => CopyrightsView.IsVisible = true;

	private void Execute()
	{
		ButtonSaveExe.IsEnabled = ButtonSaveCode.IsEnabled = ButtonOpenCode.IsEnabled = ButtonExecute.IsEnabled = false;
		try
		{
			TextBoxInput.Text = new CodeSample(TextBoxInput.Text).Disassemble(true).String.ToString();
		}
		catch
		{
		}
		String input = TextBoxInput.Text;
		String result = [], errors = [];
		var packages = SettingsView.TextBoxNuGet.Text
			?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
		Task.Run(async () =>
		{
			var thread = new System.Threading.Thread(() =>
				result = TranslateAndExecuteProgram(input, packages, out errors, out compiledAssembly), int.MaxValue)
			{
				Name = "Program translation"
			};
			thread.Start();
			thread.IsBackground = true;
			thread.Join();
			await Dispatcher.UIThread.InvokeAsync(async () =>
			{
				TextBoxOutput.Text = result.ToString();
				TextBoxErrors.Text = errors.ToString();
				ButtonSaveExe.IsEnabled = ButtonSaveCode.IsEnabled = ButtonOpenCode.IsEnabled = ButtonExecute.IsEnabled = true;
			});
		});
	}

	private async Task OpenCode()
	{
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.OpenFilePickerAsync(new()
			{
				Title = "Select the PL051 code file",
				FileTypeFilter = MainViewModel.NStarFileTypeFiter,
			})!);
		if (fileResult?.Count == 0)
			return;
		var filename = fileResult?[0]?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(filename))
			return;
		String content;
		try
		{
			content = await File.ReadAllTextAsync(filename);
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Произошла ошибка при попытке открыть файл. Вероятно, он был удален или используется другим приложением.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		var prefix = ".NStar Alpha-";
		if (!content.StartsWith(prefix))
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Alpha или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		content.Remove(0, prefix.Length);
		var versionIndex = 0;
		for (; versionIndex < minorVersions.Length; versionIndex++)
		{
			prefix = minorVersions[versionIndex] + '\n';
			if (content.StartsWith(prefix))
			{
				content.Remove(0, prefix.Length);
				break;
			}
		}
		if (versionIndex == minorVersions.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Alpha совместимой ревизии или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		var langIndex = 0;
		for (; langIndex < langs.Length; langIndex++)
		{
			prefix = langs[langIndex] + '\n';
			if (content.StartsWith(prefix))
			{
				content.Remove(0, prefix.Length);
				break;
			}
		}
		if (langIndex == langs.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Alpha совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		prefix = "<Project>\n";
		if (!content.StartsWith(prefix))
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Alpha совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		content.Remove(0, prefix.Length);
		prefix = "\n</Project>\n";
		var settings = content.GetBefore(prefix);
		if (settings.Length + prefix.Length > content.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Alpha совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		XmlDocument doc = new();
		doc.LoadXml(((String)"<Project>\n").AddRange(settings).AddRange(prefix).ToString());
		var root = doc.DocumentElement;
		if (root is not null)
		{
			var properties = root.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "Properties");
			var codeStyle = properties.ConvertAndJoin(x => x.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "CodeStyle"));
			var CharactersInLine = codeStyle.ConvertAndJoin(x => x.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "CharactersInLine")
				.ConvertAndJoin(x => x.ChildNodes.OfType<XmlText>()
				.Filter(y => y.NodeType == XmlNodeType.Text))).LastOrDefault();
			var LinesInFunction = codeStyle.ConvertAndJoin(x => x.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "LinesInFunction")
				.ConvertAndJoin(x => x.ChildNodes.OfType<XmlText>()
				.Filter(y => y.NodeType == XmlNodeType.Text))).LastOrDefault();
			var FunctionsInClass = codeStyle.ConvertAndJoin(x => x.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "FunctionsInClass")
				.ConvertAndJoin(x => x.ChildNodes.OfType<XmlText>()
				.Filter(y => y.NodeType == XmlNodeType.Text))).LastOrDefault();
			var TestEnvironment = codeStyle.ConvertAndJoin(x => x.ChildNodes.OfType<XmlElement>()
				.Filter(x => x.NodeType == XmlNodeType.Element && x.Name == "TestEnvironment")
				.ConvertAndJoin(x => x.ChildNodes.OfType<XmlText>()
				.Filter(y => y.NodeType == XmlNodeType.Text))).LastOrDefault();
			if (Enum.TryParse(typeof(RuleStrictness), CharactersInLine?.Value, out var strictness)
				&& strictness is RuleStrictness CharactersInLineStrictness)
				CodeStyleRules.CharactersInLineStrictness = CharactersInLineStrictness;
			if (Enum.TryParse(typeof(RuleStrictness), LinesInFunction?.Value, out strictness)
				&& strictness is RuleStrictness LinesInFunctionStrictness)
				CodeStyleRules.LinesInFunctionStrictness = LinesInFunctionStrictness;
			if (Enum.TryParse(typeof(RuleStrictness), FunctionsInClass?.Value, out strictness)
				&& strictness is RuleStrictness FunctionsInClassStrictness)
				CodeStyleRules.FunctionsInClassStrictness = FunctionsInClassStrictness;
			if (bool.TryParse(TestEnvironment?.Value, out var enable))
				CodeStyleRules.TestEnvironment = enable;
		}
		TextBoxInput.Text = content.Remove(0, settings.Length + prefix.Length).ToString();
		Execute();
	}

	private async Task SaveCode()
	{
		if (compiledAssembly is null)
		{
			TextBoxOutput.Text = "Чтобы сохранить код, сначала выполните программу!";
			return;
		}
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.SaveFilePickerAsync(new()
			{
				Title = "Select the path to save a PL051 code file",
				FileTypeChoices = MainViewModel.NStarFileTypeFiter,
				DefaultExtension = "n-star-alpha",
				SuggestedFileName = "Program",
			})!);
		var filename = fileResult?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(filename))
			return;
		try
		{
			await File.WriteAllTextAsync(filename, ".NStar Alpha-" + minorVersions[^1]
				+ "\nC#\n<Project>\n\t<Properties>\n\t\t<CodeStyle>\n"
				+ "\t\t\t<CharactersInLine>" + CodeStyleRules.CharactersInLineStrictness + "</CharactersInLine>\n"
				+ "\t\t\t<LinesInFunction>" + CodeStyleRules.LinesInFunctionStrictness + "</LinesInFunction>\n"
				+ "\t\t\t<FunctionsInClass>" + CodeStyleRules.FunctionsInClassStrictness + "</FunctionsInClass>\n"
				+ "\t\t\t<TestEnvironment>" + CodeStyleRules.TestEnvironment + "</TestEnvironment>\n"
				+ "\t\t</CodeStyle>\n\t</Properties>\n</Project>\n"
				+ TextBoxInput.Text);
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Произошла ошибка при попытке сохранить файл. Вероятно, файл с таким именем"
				+ " используется другим приложением или у приложения нет прав на запись по этому пути.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
		}
	}

	private async Task SaveExe()
	{
		if (compiledAssembly is null)
		{
			TextBoxOutput.Text = "Чтобы сохранить EXE, сначала выполните программу!";
			return;
		}
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.SaveFilePickerAsync(new()
			{
				Title = "Select the path to save an EXE file",
				DefaultExtension = ".exe",
				SuggestedFileName = "Program.exe",
			})!);
		var outputPath = fileResult?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(outputPath))
			return;
		var outputDir = Path.GetDirectoryName(outputPath);
		var sourceCode = CompileProgram(TextBoxInput.Text, SettingsView.TextBoxNuGet.Text
			?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? []);
		var tempDir = Environment.GetEnvironmentVariable("temp") ?? throw new IOException();
		tempDir += @"\Program";
		if (!Directory.Exists(tempDir))
			Directory.CreateDirectory(tempDir);
		await File.WriteAllBytesAsync(tempDir + @"\7za.exe", NStar.Resources._7za);
		await File.WriteAllBytesAsync(tempDir + @"\bat.bat", NStar.Resources.bat);
		await File.WriteAllBytesAsync(tempDir + @"\bat2.bat", NStar.Resources.bat2);
		File.Copy(AppDomain.CurrentDomain.BaseDirectory + @"\dotnet.7z", tempDir + @"\dotnet.7z", true);
		tempDir += @"\Program";
		if (!Directory.Exists(tempDir))
			Directory.CreateDirectory(tempDir);
		await Process.Start(tempDir + @"\..\bat.bat").WaitForExitAsync();
		await File.WriteAllTextAsync(tempDir + @"\Program.cs", sourceCode.ToString());
		var translatorPath = Path.GetDirectoryName(Path.GetFullPath("SemanticTree.dll"))!;
		await File.WriteAllTextAsync(tempDir + @"\Program.csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">

	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net10.0</TargetFramework>
		<Nullable>enable</Nullable>
" + (CodeStyleRules.MinimumFiles ? @"		<PublishSingleFile>true</PublishSingleFile>
" : "") + @"	</PropertyGroup>

	<ItemGroup>
" + string.Join("", Directory.GetFiles(Directory.GetDirectories(tempDir
			+ @"\..\shared\Microsoft.NETCore.App")[^1], "*.dll").ToHashSet()
			.AddRange(Directory.GetFiles(translatorPath, "*.dll")).ToArray(x =>
		{
			var filename = File.Exists(x) ? x
			: Path.Combine(Directory.GetDirectories(Path.Combine(tempDir, "..", "shared", "Microsoft.NETCore.App"))[^1], x);
			x = Path.GetFileName(x);
			File.Copy(filename, Path.Combine(tempDir, x), true);
			if (x is "PL051.NStar.dll" or "PL051.NStar.Desktop.dll" or "System.Private.CoreLib.dll"
				|| x.ToLower() == x && CodeStyleRules.MinimumFiles)
				return "";
			return $@"		<Reference Include=""{x}"">
		  <HintPath>./{x}</HintPath>
		</Reference>
";
		})) + @"	  <None Update=""libHarfBuzzSharp.dll"">
	    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	  <None Update=""libonigwrap.dll"">
	    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	  <None Update=""libSkiaSharp.dll"">
	    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
	</ItemGroup>
</Project>
");
		await Process.Start(tempDir + @"\..\bat2.bat").WaitForExitAsync();
		if (!Directory.Exists(tempDir + @"\publish"))
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Произошла ошибка при попытке сохранить EXE. Возможно, у вас установлена конфликтующая версия .NET."
				+ " Если после ее удаления проблема остается, обратитесь к разработчикам приложения.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		try
		{
			new[] { "libHarfBuzzSharp.dll", "libonigwrap.dll", "libSkiaSharp.dll" }
				.Filter(x => File.Exists(Path.Combine(translatorPath, x)))
				.ForEach(x => File.Copy(Path.Combine(translatorPath, x), tempDir + @"\publish\" + x, true));
			Directory.GetFiles(tempDir + @"\publish", "*.pdb").ForEach(File.Delete);
			Directory.GetFiles(tempDir + @"\publish").ForEach(x =>
				File.Copy(x, Path.Combine(outputDir!, Path.GetFileName(x)), true));
			Directory.Delete(Path.GetDirectoryName(tempDir)!, true);
		}
		catch
		{
		}
	}

	private sealed class MyCompletionData(string text, int offset) : ICompletionData
	{
		public IImage Image => null!;

		public string Text { get; private set; } = text;

		// Use this property if you want to show a fancy UIElement in the list.
		public object Content => Text;

		public object Description => "Description for " + Text;

		public double Priority { get; }

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs) =>
			textArea.Document.Replace(completionSegment, Text[Min(offset, Text.Length)..]);
	}
}
