global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.EasyEvalLib;
global using NStar.Linq;
global using NStar.MathLib;
global using NStar.MathLib.Extras;
global using NStar.Mpir;
global using System;
global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using static NStar.Core.Extents;
global using static System.Math;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.MemberChecks;
global using static PL051.NStar.MemberConverters;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.NStarUtilityFunctions;
global using static PL051.NStar.TypeChecks;
global using static PL051.NStar.TypeConverters;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RedStarMath;

namespace PL051.NStar;

public sealed partial class SemanticTree
{
	private enum MethodProcessingWay : byte
	{
		None,
		Public,
		General,
		Method,
		User,
		UserMethod,
	}
	private enum ConstructorProcessingWay : byte
	{
		None,
		Typical,
		User,
	}
	private const int MaxInitializerLength = 256;
	private const int MaxLiteralItems = MaxInitializerLength;
	private const string Abstract = "abstract ";
	private const string AsyncContextRun = "AsyncContext.Run(async () => await ";
	private const string AsyncPrefix = "async ";
	private const string Await = "await ";
	private const string ClassMain = nameof(ClassMain);
	private const string ConstantValue = "getting the constant value";
	private const string Construction = "the construction";
	private const string DeclarationAssignment = nameof(DeclarationAssignment);
	private const string DefaultConstEqual = "_ = default";
	private const string DefaultNullEqual = "_ = default!";
	private const string DelegateSuffix = " (delegate)";
	private const string DynamicCast = "(dynamic)";
	private const string DynamicName = "dynamic";
	private const string Internal = "internal ";
	private const string LoopWhileNot = "loop-while!";
	private const string Namespace = "Namespace ";
	private const string OpeningTypeof = "typeof(";
	private const string OutVar = ", out var ";
	private const string Parameter = nameof(Parameter);
	private const string Pattern = nameof(Pattern);
	private const string Private = "private ";
	private const string Protected = "protected ";
	private const string Public = "public ";
	private const string ReturnPrefix = "return ";
	private const string Static = "static ";
	private const string StringConcatenation = nameof(StringConcatenation);
	private const string UnaryAssignment = nameof(UnaryAssignment);
	private const string VariableWay = "Variable";
	private readonly List<Lexem> lexems;
	private readonly String input;
	private readonly TreeBranch topBranch;
	private readonly List<String>? errors;
	private int errorOccurred;
	private BuiltInMemberCollections C;
	private bool noAddAsync = true, containsAsync;
	private readonly String compiledClasses = [];
	private int constantsDepth, indentationUnits, unnamedIndex = 1;
	private String returnCachePrefix = [];
	private readonly List<List<String>> mutableVariables = [["args"]];
	private readonly ListHashSet<String> nestedPrepassClasses = [];
	private readonly Dictionary<(BlockStack, String, int), String> parsedFunctions = [];
	private readonly Dictionary<NStarType, String> parsedTypes = [];
	private readonly List<(BlockStack Container, String Name, UserDefinedMethodOverload Value)> parsingFunctions = [];
	private readonly Dictionary<(NStarType, TreeBranch), (bool flowControl, String value)> parsedUserConstructors = [];
	private readonly Dictionary<String, String> prepassClasses = [];
	private readonly List<TreeBranch> recursiveFunctionLocations = [];
	private readonly ListHashSet<(BlockStack Container, String Name, UserDefinedMethodOverload Value)> recursiveFunctions = [];
	private readonly ListHashSet<(BlockStack Container, String Name, UserDefinedMethodOverload Value)> unoptimizableFunctions = [];
	private readonly List<Dictionary<String, String>> variableNameMapping = [[]];
	private readonly List<Dictionary<String, (String Expr, bool Visited)>> variableExpressionMapping = [[]];

	private static readonly string AlphanumericCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.";
	private static readonly string UIThreadAsyncPrefix = Await + nameof(NStarUtilityFunctions)
		+ '.' + nameof(InvokeAsync) + "(async () => ";
	private static readonly string UIThreadNonAsyncPrefix = nameof(AsyncContext) + '.' + nameof(AsyncContext.Run)
		+ "(async () => await " + nameof(NStarUtilityFunctions) + '.' + nameof(InvokeAsync) + "(async () => ";
	private static readonly ImmutableArray<string> ExprTypes = [nameof(Expr), nameof(List), nameof(XorList), Pattern,
		nameof(Lambda), nameof(SwitchExpr), nameof(Indexes), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr),
		StringConcatenation, nameof(Assignment), DeclarationAssignment, UnaryAssignment,
		nameof(Declaration), nameof(Hypername), nameof(Index), nameof(Range)];
	private static readonly ImmutableArray<string> ArithmeticExprTypes = [nameof(Expr), nameof(PMExpr), nameof(MulDivExpr),
		StringConcatenation];
	private static readonly ImmutableArray<string> BoolOperators = ["^^", "||", "&&", "!"];
	private static readonly ImmutableArray<string> BranchOpeners = ["if", "if!", ElseIf, ElseIfNot, "else",
		"loop", LoopWhile, LoopWhileNot, WhileString, WhileNot, RepeatString, "for"];
	private static readonly ImmutableArray<string> ExprTypesToSearchDeeper = [nameof(Expr), nameof(List), Pattern,
		nameof(Indexes), nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), StringConcatenation,
		nameof(Assignment), DeclarationAssignment, UnaryAssignment];
	private static readonly ImmutableArray<string> BranchesToSearchDeeper = [nameof(Parameters), "if", "if!",
		ElseIf, ElseIfNot, RepeatString, WhileString, WhileNot, "for", ReturnString, .. ExprTypesToSearchDeeper];
	private static readonly ImmutableArray<string> BranchesToSearchDeeperNoReturn = [nameof(Parameters), "if", "if!", ElseIf,
		ElseIfNot, RepeatString, WhileString, WhileNot, "for", nameof(Expr), nameof(List), nameof(Indexes), nameof(Call),
		nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), StringConcatenation, nameof(Assignment),
		DeclarationAssignment, UnaryAssignment];
	private static readonly ImmutableArray<string> HypernameKeySequences = [UIThreadAsyncPrefix, UIThreadNonAsyncPrefix,
		AsyncContextRun, Await];
	private static List<Lexem>? lastLexems;

	public SemanticTree(List<Lexem> lexems, String input, TreeBranch topBranch,
		List<String>? errors, int errorOccurred, BuiltInMemberCollections C)
	{
#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable S3010
		lastLexems = this.lexems = lexems;
#pragma warning restore S3010
#pragma warning restore IDE0079 // Удалить ненужное подавление
		this.input = input;
		this.topBranch = topBranch;
		this.errors = errors;
		this.errorOccurred = errorOccurred;
		this.C = C;
	}

	public SemanticTree((List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList,
		int ErrorOccurred, BuiltInMemberCollections C) x)
		: this(x.Lexems, x.String, x.TopBranch, x.ErrorsList, x.ErrorOccurred, x.C)
	{
	}

	public SemanticTree(LexemStream lexemStream) : this(lexemStream.Parse())
	{
	}

	public static String ExecuteStringPrefix { get; } = @"list() dynamic args = null
";

	public static String ExecuteStringPrefixCompiled
	{
		get
		{
			if (field is not null)
				return field;
			return field = new SemanticTree((LexemStream)new CodeSample(ExecuteStringPrefix)).Parse(out _, out _);
		}
	}

	public String Parse(out List<String>? errors, out String compiledClasses)
	{
		List<String>? innerErrors = [];
		if (errorOccurred == 2)
		{
			errors = this.errors;
			compiledClasses = [];
			return [];
		}
		try
		{
			errors = this.errors;
			var result = ParseAction(topBranch.Name)(topBranch, out innerErrors);
			if ((innerErrors?.Any(x => x.StartsWith("Error")) ?? false) && errorOccurred == 0)
				errorOccurred = 1;
			AddRange(ref errors, innerErrors);
			compiledClasses = this.compiledClasses;
			if (errorOccurred != 0)
				return compiledClasses = [];
			else
				return result;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			const string errorMessage = "Technical wreck F000 in unknown line at unknown position:" +
				" compilation failed because of internal compiler error";
			Add(ref innerErrors, errorMessage + @" (see %TEMP%\PL051.NStar.log for details)");
			var targetLexem = lexems[Max(TreeBranch.LastTreePos, 0)];
			File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
				+ @"\PL051.NStar.log", [errorMessage, "The last visited location was: line " + targetLexem.LineN
				+ ", position " + targetLexem.Pos, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? NullString,
					"The underlying internal exception message was:", ex.InnerException?.Message ?? NullString]);
			errors = innerErrors;
			compiledClasses = [];
			errorOccurred = 2;
			return [];
		}
	}

	private delegate String ParseActionDelegate(TreeBranch branch, out List<String>? errors);

	private ParseActionDelegate ParseAction(String branchName) => errorOccurred == 2 ? Wreck : branchName.ToString() switch
	{
		nameof(Main) => Main,
		nameof(Try) => Try,
		nameof(Class) or nameof(BlockType.Struct) => Class,
		nameof(Record) => Record,
		nameof(Function) => Function,
		nameof(Constructor) => Constructor,
		nameof(Members) => Members,
		nameof(Constant) => Constant,
		"if" or ElseIf or "if!" or ElseIfNot => Condition,
		"loop" or LoopWhile or SemanticTree.LoopWhileNot => Loop,
		WhileString => While,
		RepeatString => Repeat,
		"for" => For,
		nameof(Declaration) => Declaration,
		nameof(Hypername) => Hypername,
		nameof(List) => List,
		nameof(XorList) => XorList,
		nameof(Lambda) => Lambda,
		nameof(SwitchExpr) => SwitchExpr,
		nameof(Range) => Range,
		"typeof" => Typeof,
		ReturnString => Return,
		_ when ExprTypes.Contains(branchName.ToString()) => Expr,
		_ => Default,
	};

	private String Main(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		if (branch.Length != 1 && branch.Parent is not null && branch.Parent.Name == nameof(Lambda))
			result.Add('{');
		var initialExtra = branch.Extra is not null;
		var thisBlockReturns = false;
		var conditionReturns = false;
		var nestedConditions = 0;
		NStarType? extraToReturn = null;
		variableNameMapping.Add([]);
		variableExpressionMapping.Add([]);
		var localUnnamedIndex = unnamedIndex;
		unnamedIndex = 1;
		for (var i = 0; i < branch.Length; i++)
		{
			var x = branch[i];
			var xName = x.Name.ToString();
			if (thisBlockReturns)
			{
				GenerateMessage(ref errors, 0x8005, branch[i].Pos);
				break;
			}
			var localIndentationUnits = indentationUnits;
			indentationUnits += (i != 0 && BranchOpeners.Contains(branch[i - 1].Name.ToString())
				|| xName == nameof(Main) && branch.Length != 1 || xName == nameof(Try))
				&& !(xName == nameof(Main) && x.Length != 0 && x[0].Name.AsSpan() is "if" or "if!" && lexems[x[0].Pos].String == WhileString)
				? 1 : 0;
			if (indentationUnits > 5)
			{
				indentationUnits = localIndentationUnits;
				GenerateMessage(ref errors, 0x9017, x.Pos);
				return [];
			}
			else if (CreateVar(indentationUnits - lexems[x.Pos].Pos, out var indentsBalance) > 0
				&& !(xName == "break" && lexems[x.Pos].String == "}"))
				GenerateMessage(ref errors, 0x800D, x.Pos);
			else if (indentsBalance < 0 && !(x.Pos != 0 && lexems[x.Pos - 1].LineN == lexems[x.Pos].LineN))
				GenerateMessage(ref errors, 0x800E, x.Pos);
			var parsed = ParseAction(x.Name)(x, out var innerErrors);
			if (errorOccurred == 2)
			{
				errors = innerErrors;
				return [];
			}
			if (ExprTypes.Contains(xName) && xName is not (nameof(Assignment) or DeclarationAssignment)
				&& ContainsGUITypes(x))
				WrapIntoUIThread(parsed);
			if (branch[i].Name.AsSpan() is not (nameof(Assignment) or DeclarationAssignment)
				&& parsingFunctions.Length != 0 && (parsingFunctions[^1].Value.Attributes & FunctionAttributes.IO) == 0)
				ReplaceVariableNames(nestedConditions, ref parsed);
			indentationUnits = localIndentationUnits;
			if (xName is nameof(Main) or ReturnString)
			{
				if (!extraToReturn.HasValue && x.Extra is not null)
					extraToReturn ??= (NStarType)x.Extra;
				else if (!extraToReturn.HasValue || x.Extra is not NStarType ReturnNStarType) { }
				else if (TypesAreCompatible(branch, ref errors, ReturnNStarType, extraToReturn.Value,
					out var warning, parsed, out var destExpr, out _) && !warning && destExpr is not null)
					parsed = destExpr;
				else if (!initialExtra && TypesAreCompatible(branch, ref errors, extraToReturn.Value, ReturnNStarType, out warning,
					parsed.Copy(), out destExpr, out _) && !warning && destExpr == parsed)
					extraToReturn = ReturnNStarType;
				else
				{
					GenerateMessage(ref errors, 0x4015, branch[i].Pos, extraToReturn.Value, ReturnNStarType);
					break;
				}
			}
			if (xName == nameof(Main) && x.Container != branch.Container
				|| x.Name == nameof(Class) && x[^1].Container != branch.Container && x.Name != xName)
				unnamedIndex++;
			if (BranchOpeners.Contains(xName))
			{
				nestedConditions++;
				variableNameMapping.Add([]);
				variableExpressionMapping.Add([]);
			}
			if (xName is nameof(Main) or ReturnString && x.Extra is NStarType)
			{
				if (i != 0 && branch[i - 1].Name.AsSpan() is "if" or "if!"
					or LoopWhile or LoopWhileNot or WhileString or WhileNot or RepeatString or "for")
					conditionReturns = true;
				else if (i == 0 || branch[i - 1].Name == "else" && nestedConditions <= 1 && conditionReturns
					|| branch[i - 1].Name == "loop" && nestedConditions <= 1
					|| branch[i - 1].Name.AsSpan() is not (ElseIf or ElseIfNot) && nestedConditions <= 0)
					thisBlockReturns = true;
			}
			if (i != 0 && branch[i - 1].Name.AsSpan() is ElseIf or ElseIfNot && x.Extra is not NStarType)
				conditionReturns = false;
			if (i != 0 && BranchOpeners.Contains(branch[i - 1].Name.ToString()))
			{
				variableExpressionMapping.RemoveAt(^1);
				variableNameMapping.RemoveAt(^1);
				nestedConditions--;
			}
			if (branch.Length == 1 && branch.Parent is not null && branch.Parent.Name == nameof(Lambda)
				&& parsed.StartsWith(ReturnPrefix) && !parsed[..^1].Contains(item: ';'))
				parsed.Remove(0, ReturnPrefix.Length).RemoveEnd(^1);
			if (x.Length == 0 || parsed.Length != 0)
			{
				if (branch.Name == "Main" && x.Name == "Main" && x.Length != 1 && parsed.Length != 0
					&& parsed[..^1].Contains(item: ';'))
					result.Add('{');
				if (parsed.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
					parsed = [];
				if ((parsed.StartsWith('(') || TryReadValue(parsed, out _))
					&& ExprTypes.Contains(xName) && xName is not (nameof(Assignment) or DeclarationAssignment))
					parsed.Insert(0, "_ = ");
				if (xName is nameof(Assignment) or DeclarationAssignment && x.Length >= 3 && x[^2].Length != 0
					&& mutableVariables.Length != 0 && mutableVariables[^1].Contains(x[^2][^1].Name.ToString()))
					result.AddRange(parsed);
				else if (i >= 1 && xName == nameof(Assignment) && BranchOpeners.Contains(branch[i - 1].Name.ToString()))
					result.Add(' ');
				else if (!(xName is nameof(Assignment) or DeclarationAssignment or nameof(Declaration)
					|| xName == nameof(Expr) && x.Length == 1 && x[0].Name == nameof(Declaration))
					|| parsingFunctions.Length == 0 || (parsingFunctions[^1].Value.Attributes & FunctionAttributes.IO) != 0)
					result.AddRange(parsed);
				if (parsed.Length != 0
					&& parsed[^1] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
					result.Add(' ');
				if (parsed.Length == 0 || ExprTypes.Contains(xName) && !parsed.EndsWith(';')
					|| xName is "continue" or "break")
					result.Add(';');
				if (branch.Name == "Main" && x.Name == "Main" && x.Length != 1 && parsed.Length != 0
					&& parsed[..^1].Contains(';'))
					result.Add('}');
			}
			if (i != 0 && branch[i - 1].Name.AsSpan() is LoopWhile or LoopWhileNot)
			{
				result.AddRange(branch[i - 1].Name == LoopWhile ? "while (" : "while (!(");
				parsed = ParseAction(branch[i - 1][0].Name)(branch[i - 1][0], out innerErrors);
				if (parsed.Length != 0)
				{
					result.AddRange(parsed);
					AddRange(ref errors, innerErrors);
				}
				if (branch[i - 1].Name.EndsWith('!'))
					result.Add(')');
				result.AddRange(");");
			}
			if (innerErrors is not null)
				AddRange(ref errors, innerErrors);
		}
		unnamedIndex = localUnnamedIndex;
		variableExpressionMapping.RemoveAt(^1);
		variableNameMapping.RemoveAt(^1);
		if (thisBlockReturns)
			branch.Extra ??= extraToReturn;
		else if (branch.Parent is not null
			&& branch.Parent.Name.AsSpan() is not (nameof(Constructor) or nameof(Main) or nameof(Try))
			&& !(branch.Extra is NStarType ThisBlockNStarType && ThisBlockNStarType.Equals(NullType))
			&& !(branch.Parent.Name == nameof(Function) && parsingFunctions.Length != 0
			&& parsingFunctions[^1].Value.ReturnNStarType.Equals(NullType))
			&& !branch.Parent.Name.StartsWith(Namespace))
		{
			GenerateMessage(ref errors, 0x402A, branch.Pos);
			return branch.Length == 1 && branch.Parent is not null && branch.Parent.Name == nameof(Lambda)
				? DefaultNull : "return default!;";
		}
		if (branch.Length != 1 && branch.Parent is not null && branch.Parent.Name == nameof(Lambda))
			result.Add('}');
		return result;
	}

	private void ReplaceVariableNames(int nestedConditions, ref String parsed)
	{
		var expressionCollection = variableExpressionMapping[^(nestedConditions + 1)..]
			.JoinIntoSingle().Filter(x => !x.Value.Visited).Reverse();
		foreach (var (Key, Value) in expressionCollection)
		{
			var before = parsed.GetBeforeSetAfter(Key);
			var emptyBefore = parsed.Length == 0;
			if (emptyBefore && before.Length != 0)
				parsed = before;
			else
				parsed = before.AddRange(Value.Expr).AddRange(parsed);
			variableExpressionMapping[^1][Key] = (Value.Expr, !emptyBefore);
		}
	}

	private String Try(TreeBranch branch, out List<String>? errors)
	{
		String result = "try { ";
		if (branch.Length >= 2 && branch[1].Name == nameof(Catch))
		{
			result.AddRange(Main(branch[0], out errors));
			result.AddRange(" }");
			for (var i = 1; i < branch.Length; i++)
				result.AddRange(Catch(branch[i], ref errors));
		}
		else
		{
			result.AddRange(Main(branch, out errors));
			result.AddRange(" }catch { }");
		}
		return result;
	}

	private String Catch(TreeBranch branch, ref List<String>? errors)
	{
		String result = "catch (";
		if (branch.Length is not (2 or 3))
		{
			GenerateMessage(ref errors, 0x4000, branch.Pos, 1);
			return [];
		}
		result.AddRange(ParseAction(branch[0].Name)(branch[0], out var innerErrors));
		AddRange(ref errors, innerErrors);
		if (branch.Length == 3 && ExprTypes.Contains(branch[1].Name.ToString()))
		{
			result.AddRange(") when (").AddRange(Expr(branch[1], out innerErrors));
			AddRange(ref errors, innerErrors);
		}
		result.AddRange(") {").AddRange(Main(branch[^1], out innerErrors)).Add('}');
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String Class(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (!Enum.TryParse<BlockType>(branch.Name.ToString(), out var blockType))
			return [];
		var name = branch[0].Name;
		if (prepassClasses.TryGetValue(name, out var pass) && pass.StartsWith("UNPASSED"))
		{
			try
			{
				return pass["UNPASSED".Length..];
			}
			finally
			{
				prepassClasses[name] = "PASSED";
			}
		}
		String result = [];
		var (Restrictions, Attributes, BaseType, _) = C.UserDefinedTypes[(branch.Container, name)];
		if ((Attributes & TypeAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & TypeAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & TypeAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if ((Attributes & (TypeAttributes.Private | TypeAttributes.Protected | TypeAttributes.Internal)) == 0)
			result.AddRange(Public);
		var @static = (Attributes & (TypeAttributes.Struct | TypeAttributes.Static))
			is TypeAttributes.Static or TypeAttributes.Enum;
		if (@static)
			result.AddRange(Static);
		else if ((Attributes & TypeAttributes.Abstract) != 0)
			result.AddRange(Abstract);
		else if ((Attributes & TypeAttributes.Sealed) != 0)
			result.AddRange("sealed ");
		result.AddRange(blockType switch
		{
			BlockType.Class => "class ",
			BlockType.Struct => "struct ",
			_ => throw new NotImplementedException(),
		});
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name);
		var (TypeIndexes, OtherIndexes) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.MainType.Equals(RecursiveBlockStack));
		String whereResult = [];
		if (TypeIndexes.Length != 0)
		{
			result.Add('<').AddRange(String.Join(", ", TypeIndexes.ToArray(x => Restrictions[x].Name))).Add('>');
			foreach (var x in TypeIndexes)
			{
				if (Restrictions[x].RestrictionType.ExtraTypes.Length != 1
					|| Restrictions[x].RestrictionType.ExtraTypes[0].Name != "type"
					|| Restrictions[x].RestrictionType.ExtraTypes[0].Extra is not NStarType WhereNStarType)
					continue;
				whereResult.AddRange(" where ").AddRange(Restrictions[x].Name).AddRange(" : ");
				whereResult.AddRange(Type(ref WhereNStarType, branch, ref errors));
			}
		}
		if (!@static)
		{
			result.AddRange(" : ");
			if (TypeIsPrimitive(BaseType.MainType))
				result.AddRange(nameof(IClass));
			else
				result.AddRange(Type(ref BaseType, branch[0], ref errors));
		}
		result.AddRange(whereResult);
		result.Add('{');
		for (var i = 0; i < OtherIndexes.Length; i++)
		{
			var x = OtherIndexes[i];
			var RestrictionType = Restrictions[x].RestrictionType;
			result.AddRange(Public).AddRange(Type(ref RestrictionType, branch[0], ref errors));
			result.Add(' ').AddRange(Restrictions[x].Name).AddRange(" { get; init; }");
		}
		BlockStack fullContainer = new(branch.Container.Append(new(blockType, name, 1)));
		var properties = C.GetAllProperties(branch[^1].Container);
		var UnsetRequiredProperties = C.UserDefinedConstructors[fullContainer]
			.FindLast(x => x.Parameters.Equals(properties,
			(x, y) => x.Type.Equals(y.Value.NStarType) && x.Name == y.Key)).UnsetRequiredProperties;
		UnsetRequiredProperties?.Replace(new Chain(Restrictions.Length)).ExceptWith(TypeIndexes);
		if (!@static && !(branch[^1].Name == ClassMain && branch[^1].Length != 0
			&& branch[^1].Elements.Any(x => x.Name == "Members")))
		{
			String paramsResult = [], baseResult = [];
			foreach (var property in properties)
				PropertiesConstructor(branch[^1], paramsResult, baseResult, property, ref errors);
			result.AddRange(Public).AddRange(name).Add('(').AddRange(paramsResult);
			if (!TypeEqualsToPrimitive(BaseType, NullString))
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){}");
		}
		C.UserDefinedConstructors[fullContainer][0].UnsetRequiredProperties.Replace(new Chain(Restrictions.Length))
			.ExceptWith(TypeIndexes);
		var localIndentationUnits = indentationUnits;
		indentationUnits++;
		result.AddRange(ParseAction(branch[^1].Name)(branch[^1], out var coreErrors).Add('}'));
		indentationUnits = localIndentationUnits;
		AddRange(ref errors, coreErrors);
		if (IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private void PrepassClass(TreeBranch branch, out List<String>? errors, List<String> typeNames)
	{
		Debug.Assert(typeNames.Length != 0);
		var fullName = String.Join('.', typeNames);
		if (prepassClasses.TryGetValue(fullName, out var pass) && pass == "PASSED")
		{
			errors = null;
			return;
		}
		if (nestedPrepassClasses.Contains(fullName))
		{
			errors = null;
			GenerateMessage(ref errors, 0x4063, branch.Pos, nestedPrepassClasses[0]);
			return;
		}
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		var parent = branch;
		while (parent.Parent is not null)
		{
			indexes.Add(parent.Parent.Elements.FindIndex(x => ReferenceEquals(parent, x)) + 1);
			parent = parent.Parent;
			branches.Add(parent);
		}
		indexes.Reverse();
		branches.Reverse();
		var typeNamesIndex = 0;
		PrepassClassInitial(ref branch, branches, indexes, typeNames[0]);
		while (typeNamesIndex != typeNames.Length - 1)
		{
			typeNamesIndex++;
			PrepassClassIteration(ref branch, typeNames[typeNamesIndex]);
		}
		if (branch.IsAncestorOf(preservedBranch))
		{
			errors = null;
			GenerateMessage(ref errors, 0x4063, preservedBranch.Pos, nestedPrepassClasses[0]);
			return;
		}
		nestedPrepassClasses.Add(fullName);
		var localIndentationUnits = indentationUnits;
		indentationUnits = 0;
		prepassClasses[typeNames[^1]] = Class(branch, out errors).Insert(0, "UNPASSED");
		indentationUnits = localIndentationUnits;
		nestedPrepassClasses.RemoveAt(^1);
	}

	private static void PrepassClassInitial(ref TreeBranch branch, List<TreeBranch> branches, List<int> indexes,
		String typeName)
	{
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (branches[i][j].Name == nameof(Class) && branches[i][j][0].Name == typeName)
				{
					branch = branches[i][j];
					return;
				}
			}
		}
		throw new InvalidOperationException();
	}

	private static void PrepassClassIteration(ref TreeBranch branch, String typeName)
	{
		for (var i = 0; i < branch[^1].Length; i++)
		{
			if (branch[^1][i].Name == nameof(Class) && branch[^1][i][0].Name == typeName)
			{
				branch = branch[^1][i];
				return;
			}
		}
		throw new InvalidOperationException();
	}

	private void PropertiesConstructor(TreeBranch branch, String paramsResult, String coreResult,
		G.KeyValuePair<String, UserDefinedProperty> property, ref List<String>? errors)
	{
		if (coreResult.Length != 0)
		{
			paramsResult.AddRange(", ");
			coreResult.AddRange(", ");
		}
		var NStarType = property.Value.NStarType;
		var typeName = Type(ref NStarType, branch, ref errors);
		paramsResult.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(property.Key))
		{
			paramsResult.Add('@');
			coreResult.Add('@');
		}
		paramsResult.AddRange(property.Key).AddRange(" = default!");
		coreResult.AddRange(property.Key);
	}

	private String Record(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		var name = branch[0].Name;
		String result = [];
		var (Restrictions, Attributes, _, _) = C.UserDefinedTypes[(branch.Container, name)];
		if ((Attributes & TypeAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & TypeAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & TypeAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if ((Attributes & (TypeAttributes.Private | TypeAttributes.Protected | TypeAttributes.Internal)) == 0)
			result.AddRange(Public);
		result.AddRange("readonly record struct ");
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name);
		var (TypeIndexes, _) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.MainType.Equals(RecursiveBlockStack));
		String whereResult = [];
		if (TypeIndexes.Length != 0)
		{
			result.Add('<').AddRange(String.Join(", ", TypeIndexes.ToArray(x => Restrictions[x].Name))).Add('>');
			foreach (var x in TypeIndexes)
			{
				if (Restrictions[x].RestrictionType.ExtraTypes.Length != 1
					|| Restrictions[x].RestrictionType.ExtraTypes[0].Name != "type"
					|| Restrictions[x].RestrictionType.ExtraTypes[0].Extra is not NStarType WhereNStarType)
					continue;
				whereResult.AddRange(" where ").AddRange(Restrictions[x].Name).AddRange(" : ");
				whereResult.AddRange(Type(ref WhereNStarType, branch, ref errors));
			}
		}
		var Parameters = C.UserDefinedConstructors[new(branch.Container.Append(new(BlockType.Struct, name, 1)))][0].Parameters;
		result.Add('(').AddRange(this.Parameters(branch[^1], Parameters, Parameters.ToArray(x => x.Name), out _)).Add(')');
		result.AddRange(whereResult).Add(';');
		if (IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private String Function(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		var container = branch.Container;
		var name = branch[0].Name;
		var start = branch.Pos;
		var index = C.UserDefinedFunctionIndexes[container][start];
		var t = C.UserDefinedMethods[branch.Container][name][index];
		if (parsedFunctions.TryGetValue((branch.Container, name, index), out var result))
			return result;
		result = [];
		String cacheResult = [];
		var (_, _, ReturnNStarType, Attributes, Parameters, _)
			= C.UserDefinedMethods[branch.Container][name][index];
		if ((Attributes & FunctionAttributes.Wrong) != 0 || name.StartsWith('?'))
			return parsedFunctions[(branch.Container, name, index)] = [];
		if ((Attributes & FunctionAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & FunctionAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & FunctionAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch) && (Attributes
			& (FunctionAttributes.Private | FunctionAttributes.Protected | FunctionAttributes.Internal)) == 0)
			result.AddRange(Public);
		if ((Attributes & FunctionAttributes.Static) != 0)
		{
			cacheResult.AddRange(Static);
			result.AddRange(Static);
		}
		else if (container.TryPeek(out var block) && block.BlockType == BlockType.Struct) { }
		else if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
		{
			if (C.UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Abstract) == 0)
			{
				GenerateMessage(ref errors, 0x400A, branch.Pos);
				return parsedFunctions[(branch.Container, name, index)] = [];
			}
			result.AddRange(Abstract);
		}
		else if (branch.Container.Length == 0 || branch.Container.Peek().BlockType
			is not (BlockType.Class or BlockType.Struct or BlockType.Interface)) { }
		else if (!(C.UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& !TypeEqualsToPrimitive(userDefinedType.BaseType, NullString)
			&& C.UserDefinedFunctionExists(userDefinedType.BaseType, name,
			Parameters.ToList(x => x.Type), [], out var baseFunctions) && baseFunctions.Length != 0
			&& CreateVar(baseFunctions.Find(x => (Parameters, x.Parameters).Combine().All(y =>
			y.Item1.Type.Equals(y.Item2.Type))), out var baseFunction) != default))
			result.AddRange((userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Sealed ? "" : "virtual ");
		else if (ReturnNStarType.Equals(baseFunction.ReturnNStarType)
			&& (Attributes & (FunctionAttributes.Static | FunctionAttributes.Private | FunctionAttributes.Protected
			| FunctionAttributes.Internal | FunctionAttributes.Const | FunctionAttributes.Multiconst))
			== (baseFunction.Attributes & (FunctionAttributes.Static | FunctionAttributes.Private
			| FunctionAttributes.Protected | FunctionAttributes.Internal | FunctionAttributes.Const
			| FunctionAttributes.Multiconst)) && (Parameters, baseFunction.Parameters).Combine().All(x =>
			(x.Item1.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out))
			== (x.Item2.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)))
			&& (Attributes & FunctionAttributes.New) != FunctionAttributes.New
			&& (baseFunction.Attributes & FunctionAttributes.New) != FunctionAttributes.Sealed)
			result.AddRange("override ");
		else
		{
			if ((Attributes & FunctionAttributes.New) != FunctionAttributes.New)
				GenerateMessage(ref errors, 0x8008, branch.Pos, t.RealName);
			result.AddRange("new " + ((userDefinedType.Attributes & TypeAttributes.Static)
				== TypeAttributes.Sealed ? "" : "virtual "));
		}
		var asyncAdded = false;
		var asyncInsertionPos = result.Length;
		if (TaskBlockStacks.Contains(ReturnNStarType.MainType) && !result.EndsWith(Abstract))
		{
			result.AddRange(AsyncPrefix);
			asyncAdded = true;
		}
		var targetBranch = branch.Length == 4 ? branch[3] : branch;
		var returnType = Type(ref ReturnNStarType, targetBranch, ref errors);
		result.AddRange(returnType).Add(' ');
		if (EscapedKeywords.Contains(t.RealName))
			result.Add('@');
		result.AddRange(t.RealName).Add('(');
		var RealParameterNames = Parameters.ToArray(x => RandomVarName().ToNString());
		var parameters = this.Parameters(targetBranch, Parameters, RealParameterNames, out var parametersErrors);
		result.AddRange(parameters).Add(')');
		cacheResult.AddRange(nameof(Dictionary<,>)).Add('<');
		if (Parameters.Length == 0)
			cacheResult.AddRange("bool");
		else if (Parameters.Length == 1)
			cacheResult.AddRange(parameters.GetBeforeLast(' '));
		else
			cacheResult.Add('(').AddRange(parameters).Add(')');
		cacheResult.AddRange(", ").AddRange(returnType).AddRange("> ");
		var cacheVarName = RandomVarName();
		cacheResult.AddRange(cacheVarName).AddRange(" = new();");
		if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
			return parsedFunctions[(branch.Container, name, index)] = result.Add(';');
		if (!ReturnNStarType.Equals(NullType) && (Attributes & FunctionAttributes.IO) == 0)
			result.Insert(0, cacheResult);
		result.Add('{');
		AddRange(ref errors, parametersErrors);
		parsingFunctions.Add((branch.Container, name, t));
		if (branch.Length == 4)
		{
			var localReturnCachePrefix = returnCachePrefix;
			variableNameMapping.Add([]);
			Parameters.ForEach((x, index) => variableNameMapping[^1].Add(x.Name, RealParameterNames[index]));
			if (!ReturnNStarType.Equals(NullType) && (Attributes & FunctionAttributes.IO) == 0)
			{
				result.AddRange("if (").AddRange(cacheVarName).Add('.').AddRange(nameof(Interfaces.TryGetValue)).AddRange("((");
				var callParameters = Parameters.Length == 0 ? False : String.Join(", ", RealParameterNames);
				result.AddRange(callParameters).AddRange("), out var ");
				var valueName = RandomVarName();
				result.AddRange(valueName).AddRange(")) return ").AddRange(valueName).Add(';');
				returnCachePrefix = cacheVarName.ToNString().AddRange("[(").AddRange(callParameters).AddRange(")] = ");
			}
			var localIndentationUnits = indentationUnits;
			indentationUnits = lexems[branch[3].Pos].Pos;
			var localNoAddAsync = noAddAsync;
			noAddAsync = !asyncAdded;
			var localContainsAsync = containsAsync;
			containsAsync = false;
			var parsed = ParseAction(branch[3].Name)(branch[3], out var coreErrors);
			AddRange(ref errors, coreErrors);
			if (recursiveFunctions.IndexOf(parsingFunctions[^1]) is var recursiveIndex
				&& recursiveIndex >= 0 && recursiveIndex < recursiveFunctionLocations.Length
				&& !unoptimizableFunctions.Contains(parsingFunctions[^1]))
			{
				mutableVariables.Add([]);
				if (OptimizeRecursion(branch, recursiveIndex, Parameters, ref errors))
					parsed = ParseAction(branch[3].Name)(branch[3], out _);
				mutableVariables.RemoveAt(^1);
			}
			result.AddRange(parsed);
			if (localContainsAsync && !asyncAdded)
				result.Insert(asyncInsertionPos, AsyncPrefix);
			containsAsync = localContainsAsync;
			noAddAsync = localNoAddAsync;
			indentationUnits = localIndentationUnits;
			returnCachePrefix = localReturnCachePrefix;
			variableNameMapping.RemoveAt(^1);
		}
		result.Add('}');
		parsingFunctions.RemoveAt(^1);
		return parsedFunctions[(branch.Container, name, index)] = result;
	}

	private bool OptimizeRecursion(TreeBranch branch, int recursiveIndex, ExtendedMethodParameters parameters,
		ref List<String>? errors)
	{
		TreeBranch declaration = new(nameof(Declaration), branch[3].Pos, branch[3].Container);
		declaration.Add(new("type", branch[3].Pos, branch[3].Container) { Extra = IntType });
		String counterName = new(RandomVarName());
		mutableVariables[^1].Add(counterName);
		declaration.Add(new(counterName, branch[3].Pos, branch[3].Container) { Extra = IntType });
		TreeBranch init = new("0n", branch[3].Pos, branch[3].Container) { Extra = IntType };
		TreeBranch assignment = new(nameof(Assignment), [init, declaration, new("=", branch[3].Pos, branch[3].Container)]);
		TreeBranch newBranch = new(nameof(Main), assignment);
		List<String> newParameterNames = [], parameterListNames = [];
		foreach (var param in parameters)
		{
			mutableVariables[^1].Add(param.Name);
			declaration = new(nameof(Declaration), branch[3].Pos, branch[3].Container);
			declaration.Add(new("type", branch[3].Pos, branch[3].Container) { Extra = param.Type });
			newParameterNames.Add(new(RandomVarName()));
			mutableVariables[^1].Add(newParameterNames[^1]);
			declaration.Add(new(newParameterNames[^1], branch[3].Pos, branch[3].Container) { Extra = param.Type });
			init = new(nameof(Hypername), new(param.Name, branch[3].Pos, branch[3].Container));
			assignment = new(nameof(DeclarationAssignment), [init, declaration, new("=", branch[3].Pos, branch[3].Container)]);
			newBranch.Add(assignment);
			TreeBranch typeBranch = new("type", branch[3].Pos, branch[3].Container) { Extra = param.Type };
			NStarType ListNStarType = new(ListBlockStack, new() { typeBranch });
			declaration = new(nameof(Declaration), branch[3].Pos, branch[3].Container);
			declaration.Add(new("type", branch[3].Pos, branch[3].Container) { Extra = ListNStarType });
			parameterListNames.Add(new(RandomVarName()));
			mutableVariables[^1].Add(parameterListNames[^1]);
			declaration.Add(new(parameterListNames[^1], branch[3].Pos, branch[3].Container) { Extra = ListNStarType });
			init = new(nameof(Hypername), new(param.Name, branch[3].Pos, branch[3].Container));
			assignment = new(nameof(DeclarationAssignment), [init, declaration, new("=", branch[3].Pos, branch[3].Container)]);
			newBranch.Add(assignment);
		}
		var ReturnNStarType = parsingFunctions[^1].Value.ReturnNStarType;
		declaration = new(nameof(Declaration), branch[3].Pos, branch[3].Container);
		declaration.Add(new("type", branch[3].Pos, branch[3].Container) { Extra = ReturnNStarType });
		String resultName = new(RandomVarName());
		mutableVariables[^1].Add(resultName);
		declaration.Add(new(resultName, branch[3].Pos, branch[3].Container) { Extra = ReturnNStarType });
		init = new("null", branch[3].Pos, branch[3].Container);
		assignment = new(nameof(DeclarationAssignment), [init, declaration, new("=", branch[3].Pos, branch[3].Container)]);
		newBranch.Add(assignment);
		newBranch.Add(new(WhileString, new("true", branch[3].Pos, branch[3].Container) { Extra = BoolType }));
		var callBranch = recursiveFunctionLocations[recursiveIndex];
		if (!(callBranch.Name == nameof(Hypername) && callBranch.Length == 2
			&& callBranch[0].Name.Replace(" (function)", "") == parsingFunctions[^1].Name
			&& callBranch[1].Name == nameof(Call) && callBranch[1].Length == parameters.Length)
			&& parameters.Length != 0 && (parameters[^1].Attributes & ParameterAttributes.Params) != ParameterAttributes.Params)
		{
			GenerateMessage(ref errors, 0x8020, callBranch[1].EndPos);
			return false;
		}
		var lockBranch = callBranch;
		while (lockBranch.Parent is not null && !ReferenceEquals(lockBranch.Parent, branch[3]))
			lockBranch = lockBranch.Parent;
		if (!(ReferenceEquals(lockBranch.Parent, branch[3]) && lockBranch.Name != nameof(Main)
			&& !BranchOpeners.Contains(lockBranch.Name.ToString())))
		{
			GenerateMessage(ref errors, 0x8021, callBranch.Pos);
			return false;
		}
		var lockIndex = branch[3].Elements.FindIndex(x => ReferenceEquals(x, lockBranch));
		var beforeLock = branch[3].GetRange(0, lockIndex);
		if (beforeLock.Length != 0 && BranchOpeners.Contains(beforeLock[^1].Name.ToString()))
		{
			GenerateMessage(ref errors, 0x8021, callBranch.Pos);
			return false;
		}
		TreeBranch firstLoop = beforeLock.Length == 0 ? new(nameof(Main), lockBranch.Pos, lockBranch.Container)
			: new(nameof(Main), beforeLock);
		ReplaceExitPoints(firstLoop);
		for (var i = 0; i < parameters.Length; i++)
		{
			TreeBranch variable = new(newParameterNames[i], callBranch.Pos, callBranch.Container);
			variable = new(nameof(Hypername), variable);
			TreeBranch equalSign = new("=", callBranch.Pos, callBranch.Container);
			firstLoop.Add(new(nameof(Assignment), [callBranch[1][i], variable, equalSign]));
			TreeBranch call = new(nameof(Call), variable);
			TreeBranch add = new(nameof(Hypername), [new(nameof(add.Add), callBranch.Pos, callBranch.Container), call]);
			TreeBranch list = new(parameterListNames[i], callBranch.Pos, callBranch.Container);
			list = new(nameof(Hypername), list);
			TreeBranch full = new(nameof(Hypername), [list, new(".", callBranch.Pos, callBranch.Container)]);
			full.Add(add);
			firstLoop.Add(full);
		}
		for (var i = 0; i < parameters.Length; i++)
		{
			TreeBranch variable = new(parameters[i].Name, callBranch.Pos, callBranch.Container);
			variable = new(nameof(Hypername), variable);
			TreeBranch newValue = new(newParameterNames[i], callBranch.Pos, callBranch.Container);
			newValue = new(nameof(Hypername), newValue);
			TreeBranch equalSign = new("=", callBranch.Pos, callBranch.Container);
			firstLoop.Add(new(nameof(Assignment), [newValue, variable, equalSign]));
		}
		TreeBranch counter = new(nameof(Hypername), new(counterName, branch[3].Pos, branch[3].Container));
		TreeBranch increment = new("++", branch[3].Pos, branch[3].Container);
		increment = new(nameof(UnaryAssignment), [counter, increment]);
		firstLoop.Add(increment);
		newBranch.Add(firstLoop);
		newBranch.Add(new(RepeatString, new(nameof(Hypername), new(counterName, branch[3].Pos, branch[3].Container))));
		var afterLock = branch[3].Elements.GetRange(lockIndex);
		TreeBranch secondLoop = new(nameof(Main), afterLock);
		ReplaceCall(secondLoop);
		counter = new(nameof(Hypername), new(counterName, branch[3].Pos, branch[3].Container));
		TreeBranch decrement = new("--", branch[3].Pos, branch[3].Container);
		decrement = new(nameof(UnaryAssignment), [counter, decrement]);
		secondLoop.Add(decrement);
		newBranch.Add(secondLoop);
		TreeBranch resultBranch = new(nameof(Hypername), new(resultName, branch[3].EndPos - 1, branch[3].Container));
		newBranch.Add(new(ReturnString, new(nameof(Expr), resultBranch)));
		branch[3] = newBranch;
		return true;
		void ReplaceExitPoints(TreeBranch branch)
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == nameof(Main))
				{
					ReplaceExitPoints(branch[i]);
					continue;
				}
				else if (branch[i].Name != ReturnString)
					continue;
				TreeBranch variable = new(nameof(Hypername), new(resultName, branch.Pos, branch.Container));
				TreeBranch equalSign = new("=", branch.Pos, branch.Container);
				TreeBranch assignment = new(nameof(Assignment), [branch[i][0], variable, equalSign]);
				TreeBranch @break = new("break", branch.EndPos - 1, branch.Container);
				branch[i] = new(nameof(Main), [assignment, @break]);
			}
		}
		void ReplaceCall(TreeBranch branch)
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == ReturnString)
				{
					ReplaceCall(branch[i][0]);
					TreeBranch resultBranch = new(nameof(Hypername), new(resultName, callBranch.Pos, callBranch.Container));
					TreeBranch equalSign = new("=", branch.Pos, branch.Container);
					branch[i] = new(nameof(Assignment), [branch[i][0], resultBranch, equalSign]);
					continue;
				}
				else if (ReferenceEquals(branch[i], callBranch))
				{
					branch[i] = new(nameof(Hypername), new(resultName, callBranch.Pos, callBranch.Container));
					continue;
				}
				else if (branch[i].Name == nameof(Main)
					|| BranchesToSearchDeeper.Contains(branch[i].Name.ToString())
					|| ExprTypes.Contains(branch[i].Name.ToString())
					&& !(branch[i].Name == nameof(Hypername) && branch[i].Length == 1))
				{
					ReplaceCall(branch[i]);
					continue;
				}
				if (!(branch[i].Name == nameof(Hypername) && branch[i].Length == 1
					&& CreateVar(parameters.FindIndex(x => x.Name == branch[i][0].Name), out var index) >= 0))
					continue;
				TreeBranch variable = new(parameterListNames[index], branch.Pos, branch.Container);
				TreeBranch indexBranch = new(nameof(Hypername), new(counterName, branch.Pos, branch.Container));
				branch[i] = new(nameof(Expr), new(nameof(Hypername), [variable, new(nameof(Indexes), indexBranch)]));
			}
		}
	}

	private String Constructor(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		var parameterTypes = GetParameterTypes(branch[0]);
		if (C.UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& parameterTypes.Length != 0 && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			return [];
		var (Attributes, _, UnsetRequiredProperties, _) = C.UserDefinedConstructors[branch.Container]
			.FindLast(x => x.Parameters.Equals(parameterTypes, (x, y) => x.Name == y.Name && x.Type.Equals(y.Type)));
		var (TypeIndexes, _) = new Chain(userDefinedType.Restrictions?.Length ?? 0).BreakFilter(index =>
			!userDefinedType.Restrictions![index].Package
			&& userDefinedType.Restrictions[index].RestrictionType.MainType.Equals(RecursiveBlockStack));
		UnsetRequiredProperties?.Replace(new Chain(userDefinedType.Restrictions?.Length ?? 0)).ExceptWith(TypeIndexes);
		if ((Attributes & ConstructorAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & ConstructorAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & ConstructorAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if ((Attributes & (ConstructorAttributes.Private | ConstructorAttributes.Protected | ConstructorAttributes.Internal))
			== 0)
			result.AddRange(Public);
		if ((Attributes & ConstructorAttributes.Static) != 0)
			result.AddRange(Static);
		if ((Attributes & ConstructorAttributes.Abstract) != 0)
		{
			result.AddRange(Abstract);
			GenerateMessage(ref errors, 0x9012, branch.Pos);
			return [];
		}
		var name = branch.Container.Peek().Name;
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name).Add('(');
		result.AddRange(Parameters(branch[^1], parameterTypes, parameterTypes.ToArray(x => x.Name), out var parametersErrors));
		AddRange(ref errors, parametersErrors);
		parsingFunctions.Add((branch.Container, nameof(Constructor),
			new([], [], new(branch.Container, []), FunctionAttributes.None, parameterTypes, null)));
		result.AddRange("){");
		if (branch[^1].Name == "Main")
		{
			var localIndentationUnits = indentationUnits;
			indentationUnits++;
			var localNoAddAsync = noAddAsync;
			noAddAsync = true;
			mutableVariables.Add([]);
			mutableVariables[^1].AddRange(C.GetAllProperties(branch.Container).Convert(x => x.Key)
				.Concat(userDefinedType.Restrictions?.Convert(x => x.Name) ?? []));
			result.AddRange(ParseAction(branch[^1].Name)(branch[^1], out var coreErrors));
			mutableVariables.RemoveAt(^1);
			noAddAsync = localNoAddAsync;
			indentationUnits = localIndentationUnits;
			AddRange(ref errors, coreErrors);
		}
		result.Add('}');
		parsingFunctions.RemoveAt(^1);
		return result;
	}

	private ExtendedMethodParameters GetParameterTypes(TreeBranch branch) => [.. branch.Elements.Convert(GetParameterData)];

	private ExtendedMethodParameter GetParameterData(TreeBranch branch)
	{
		if (!(branch[0].Name == "type" && branch[0].Extra is NStarType ParameterNStarType
			&& (branch.Length == 3 && branch[2].Name == "no optional"
			|| branch.Length == 4 && branch[2].Name == "optional" && ExprTypes.Contains(branch[3].Name.ToString()))
			&& branch.Extra is ParameterAttributes Attributes))
			throw new InvalidOperationException();
		return new(ParameterNStarType, branch[1].Name, Attributes, ParseAction(branch[^1].Name)(branch[^1], out _));
	}

	private String Parameters(TreeBranch branch, ExtendedMethodParameters parameters, String[] realParameterNames,
		out List<String>? errors)
	{
		String result = [];
		errors = null;
		for (var i = 0; i < parameters.Length; i++)
		{
			result.AddRange((ReadOnlySpan<char>)((parameters[i].Attributes & ParameterAttributes.Params) switch
			{
				ParameterAttributes.None => [],
				ParameterAttributes.Ref => "ref ",
				ParameterAttributes.Out => "out ",
				ParameterAttributes.Params => "params List<",
				_ => [],
			}));
			var ParameterNStarType = parameters[i].Type;
			result.AddRange(Type(ref ParameterNStarType, branch, ref errors));
			if ((parameters[i].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params)
				result.Add('>');
			result.Add(' ');
			var name = realParameterNames[i];
			if (EscapedKeywords.Contains(name))
				result.Add('@');
			result.AddRange(name);
			if (parameters[i].DefaultValue.AsSpan() is not ("" or "no optional"))
			{
				result.AddRange(" = ");
				if (parameters[i].DefaultValue == NullString)
					result.AddRange((String)DefaultNull);
				else if (parameters[i].DefaultValue == double.PositiveInfinity.ToString())
					result.AddRange((String)"double.PositiveInfinity");
				else if (parameters[i].DefaultValue == double.NegativeInfinity.ToString())
					result.AddRange((String)"double.NegativeInfinity");
				else if (parameters[i].DefaultValue == double.NaN.ToString())
					result.AddRange((String)"double.NaN");
				else
					result.AddRange(parameters[i].DefaultValue);
			}
			if (i != parameters.Length - 1)
				result.AddRange(", ");
		}
		return result;
	}

	private String Members(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (MainParsing.TryParse(branch.Name.ToString(), out var value))
			return value.ToString(true, true);
		if (branch.Length == 0)
			return branch.Name.Copy();
		String result = [], paramsResult = [], baseResult = [], coreResult = [];
		if (C.UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& CreateVar(C.GetAllProperties(userDefinedType.BaseType.MainType), out var properties).Length != 0)
			foreach (var property in properties)
				PropertiesConstructor(branch, paramsResult, baseResult, property, ref errors);
		foreach (var x in branch.Elements)
		{
			List<String>? innerErrors;
			if (x.Name != nameof(Property))
			{
				result.AddRange(ParseAction(x.Name)(x, out innerErrors));
				AddRange(ref errors, innerErrors);
				continue;
			}
			var parsedSubbranch = Property(x, out innerErrors, out var constructorTop, out var constructorCore);
			if (parsedSubbranch.Length != 0)
			{
				result.AddRange(parsedSubbranch);
				AddRange(ref errors, innerErrors);
			}
			if (constructorTop.Length != 0)
			{
				if (paramsResult.Length != 0)
					paramsResult.AddRange(", ");
				paramsResult.AddRange(constructorTop);
			}
			coreResult.AddRange(constructorCore);
		}
		if ((userDefinedType.Attributes & TypeAttributes.Static) != TypeAttributes.Static
			&& (!C.UserDefinedConstructors.TryGetValue(branch.Container, out var constructors)
			|| (properties = C.GetAllProperties(branch.Container)).Length >= 0
			&& constructors.FindAll(x => (x.Attributes & ConstructorAttributes.AutoGenerated) != 0
			&& (x.Parameters.Length != 0 || properties.Length == 0) && x.Parameters.Length <= properties.Length
			&& (x.Parameters, properties.GetSlice(^x.Parameters.Length)).Combine()
			.All(x => x.Item1.Type.Equals(x.Item2.Value.NStarType))).Length != 0))
		{
			result.AddRange(Public).AddRange(branch.Container.Peek().Name).Add('(').AddRange(paramsResult);
			if (baseResult.Length != 0)
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){").AddRange(coreResult).Add('}');
		}
		return result;
	}

	private String Property(TreeBranch branch, out List<String>? errors, out String constructorTop, out String constructorCore)
	{
		errors = null;
		constructorTop = [];
		constructorCore = [];
		if (branch[0].Extra is not NStarType NStarType)
			return [];
		var name = branch[1].Name;
		var (NStarType2, Attributes, _) = C.UserDefinedProperties[branch.Container][name];
		if (!NStarType.Equals(NStarType2))
			return [];
		String result = [];
		if ((Attributes & PropertyAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & PropertyAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & PropertyAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch)
			&& (Attributes & (PropertyAttributes.Private | PropertyAttributes.Protected | PropertyAttributes.Internal)) == 0)
			result.AddRange(Public);
		var @static = (Attributes & PropertyAttributes.Static) != 0;
		if (@static)
			result.AddRange(Static);
		var typeName = Type(ref NStarType, branch[^1], ref errors);
		result.AddRange(typeName).Add(' ');
		constructorTop.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(name))
		{
			result.Add('@');
			constructorTop.Add('@');
		}
		result.AddRange(name).AddRange(" { get; ");
		if ((Attributes & PropertyAttributes.NoSet) == 0)
		{
			if ((Attributes & PropertyAttributes.PrivateSet) != 0)
				result.AddRange(Private);
			if ((Attributes & PropertyAttributes.ProtectedSet) != 0)
				result.AddRange(Protected);
			result.AddRange((Attributes & PropertyAttributes.SetOnce) != 0 ? "init" : "set").AddRange("; ");
		}
		result.AddRange("} = ");
		constructorTop.AddRange(name).AddRange(" = default!");
		branch[^1].Extra ??= NStarType;
		var expr = ParseAction(branch[^1].Name)(branch[^1], out var innerErrors);
		if (branch[^1].Name != NullString && expr.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
		{
			AddRange(ref errors, innerErrors);
			branch[^1].Replace(new(NullString, branch[^1].Pos, branch[^1].EndPos, branch[^1].Container) { Extra = NullType });
			return result.AddRange("default!;");
		}
		else if (TypeEqualsToPrimitive(NStarType, RecursiveTypeName) && name == RecursiveTypeName)
		{
			GenerateMessage(ref errors, 0x4092, branch[1].Pos);
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		else if (branch[^1].Extra is not NStarType ValueNStarType)
		{
			GenerateMessage(ref errors, 0x4014, branch[^1].Pos, null!, NullType, NStarType,
				"getting the property default value");
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		else if (!TypesAreCompatible(branch, ref errors, ValueNStarType, NStarType, out var warning, expr, out _, out var extraMessage) || warning)
		{
			GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, NStarType,
				"getting the property default value");
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		result.AddRange(expr);
		constructorCore.AddRange("if (").AddRange(name).AddRange(" is ");
		if (C.TypeIsFullySpecified(NStarType, branch.Container))
			constructorCore.AddRange("default(").AddRange(typeName).Add(')');
		else
			constructorCore.AddRange(NullString);
		constructorCore.AddRange(")this.").AddRange(name).AddRange(" = ").AddRange(expr);
		constructorCore.AddRange(";else this.").AddRange(name).AddRange(" = ").AddRange(name).Add(';');
		AddRange(ref errors, innerErrors);
		result.Add(';');
		ValidateStatic(constructorTop, constructorCore);
		return result;
		void ValidateStatic(String constructorTop, String constructorCore)
		{
			if (@static)
			{
				constructorTop.Clear();
				constructorCore.Clear();
			}
		}
	}

	private String Constant(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch[0].Extra is not NStarType NStarType)
			return [];
		var name = branch[1].Name;
		var (TestNStarType, Attributes, _) = C.UserDefinedConstants[branch.Container][name];
		if (!NStarType.Equals(TestNStarType))
			return [];
		String result = [];
		if ((Attributes & ConstantAttributes.Private) != 0)
			result.AddRange(Private);
		if ((Attributes & ConstantAttributes.Protected) != 0)
			result.AddRange(Protected);
		if ((Attributes & ConstantAttributes.Internal) != 0)
		{
			result.AddRange(Internal);
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch)
			&& (Attributes & (ConstantAttributes.Private | ConstantAttributes.Protected | ConstantAttributes.Internal)) == 0)
			result.AddRange(Public);
		result.AddRange(TypeIsPrimitive(NStarType.MainType) && NStarType.ExtraTypes.Length == 0
			&& NStarType.MainType.Peek().Name.AsSpan() is BoolTypeName
			or ByteTypeName or ShortCharTypeName or ShortIntTypeName or UnsignedShortIntTypeName
			or CharTypeName or IntTypeName or UnsignedIntTypeName
			or LongCharTypeName or LongIntTypeName or UnsignedLongIntTypeName
			or RealTypeName or DecimalTypeName ? "const " : "static readonly ");
		var typeName = Type(ref NStarType, branch[^1], ref errors);
		result.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name).AddRange(" = ");
		var localConstantsDepth = constantsDepth;
		constantsDepth++;
		try
		{
			if (localConstantsDepth >= 25)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4055, otherPos);
				branch.Parent![branch.Parent.Elements.IndexOf(branch)]
					= new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				return [];
			}
			branch[^1].Extra ??= NStarType;
			var expr = ParseAction(branch[^1].Name)(branch[^1], out var innerErrors);
			if (expr.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			{
				AddRange(ref errors, innerErrors);
				return [];
			}
			else if (TypeEqualsToPrimitive(NStarType, RecursiveTypeName) && name == RecursiveTypeName)
			{
				GenerateMessage(ref errors, 0x4092, branch[1].Pos);
				return [];
			}
			else if (branch[^1].Extra is not NStarType ValueNStarType)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, null!, NullType, NStarType,
				ConstantValue);
				return [];
			}
			else if (!TypesAreCompatible(branch, ref errors, ValueNStarType, NStarType, out var warning, expr, out _, out var extraMessage)
				|| warning)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, NStarType,
				ConstantValue);
				return [];
			}
			result.AddRange(expr);
			AddRange(ref errors, innerErrors);
		}
		finally
		{
			constantsDepth = localConstantsDepth;
		}
		result.Add(';');
		return [];
	}

	private String Condition(TreeBranch branch, out List<String>? errors)
	{
		String result = branch.Name.ToString() switch
		{
			"if" => "if (",
			ElseIf => "else if (",
			"if!" => "if (!(",
			ElseIfNot => "else if (!(",
			_ => throw new InvalidOperationException(),
		};
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		if (branch.Name.EndsWith('!'))
			result.Add(')');
		return result.Add(')');
	}

	private static String Loop(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch.Length == 0)
			return "while (true)";
		else
			return "do";
	}

	private String While(TreeBranch branch, out List<String>? errors)
	{
		String result = "while (";
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		return result.Add(')');
	}

	private String Repeat(TreeBranch branch, out List<String>? errors)
	{
		String result = "var ";
		var lengthName = RandomVarName();
		result.AddRange(lengthName);
		result.AddRange(" = ");
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		var counterName = RandomVarName();
		result.AddRange(";for (var ").AddRange(counterName).AddRange(" = 0; ").AddRange(counterName).AddRange(" < ");
		result.AddRange(lengthName).AddRange("; ").AddRange(counterName).AddRange("++)");
		return result;
	}

	private String For(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (!(branch.Length == 2 && branch[0].Name == nameof(Declaration)))
			return [];
		var parsedCollection = ParseAction(branch[1].Name)(branch[1], out var innerErrors);
		AddRange(ref errors, innerErrors);
		if (parsedCollection.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			parsedCollection = "Array.Empty<int>()";
		if (branch[1].Extra is not NStarType CollectionNStarType)
			return DefaultNull;
		if (branch[0].Name == nameof(Declaration)
			&& branch[0].Length == 2 && branch[0][0].Name == "type" && branch[0][0].Extra is NStarType NStarType
			&& NStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive && block.Name == "var")
			branch[0][0].Extra = GetSubtype(C, CollectionNStarType);
		if (branch[0][0].Extra is not NStarType ItemNStarType || ItemNStarType.Equals(NullType))
			branch[0][0].Extra = ItemNStarType = IntType;
		NStarType TargetNStarType = new(IEnumerableBlockStack, [new("type", 0, []) { Extra = ItemNStarType }]);
		if (!TypesAreCompatible(branch, ref errors, CollectionNStarType, TargetNStarType,
			out var warning, parsedCollection, out _, out var extraMessage) || warning)
		{
			var otherPos = branch[0][0].Pos;
			GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, CollectionNStarType, TargetNStarType,
				"getting the collection for the iterating");
			return DefaultNull;
		}
		if (branch[0].Length == 2 && VariableExists(branch[0], branch[0][1].Name, ref errors))
			return [];
		if (TypeEqualsToPrimitive(CollectionNStarType, TupleName, false) && ItemNStarType.Equals(BoolType))
			parsedCollection.Insert(0, '(').AddRange(").ToList()");
		var result = ((String)"foreach (").AddRange(Declaration(branch[0], out innerErrors));
		AddRange(ref errors, innerErrors);
		result.AddRange(" in ").AddRange(parsedCollection).Add(')');
		return result;
	}

	private String Declaration(TreeBranch branch, out List<String>? errors) =>
		Declaration(branch, out errors, false);

	private String Declaration(TreeBranch branch, out List<String>? errors, bool prepass)
	{
		errors = null;
		if (!(branch.Length == 2 && branch[0].Name == "type" && branch[0].Extra is NStarType NStarType))
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4000, otherPos, 2);
			return "_";
		}
		var varName = branch[1].Name;
		var realName = variableNameMapping[^1].GetOrAdd(branch[1].Name,
			x => varName == "args" ? varName : new(RandomVarName()));
		if (VariableExists(branch, varName, ref errors!))
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))]
				= new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
				{
					Extra = NullType
				};
			return "_";
		}
		else if (C.UserDefinedConstantExists(branch.Container, varName, out var constant, out var matchingContainer, out _)
			&& constant.HasValue && constant.Value.DefaultValue is null)
		{
			if (branch.Parent is null || branch.Parent.Name != DeclarationAssignment)
			{
				GenerateMessage(ref errors, 0x4053, branch.Pos);
				return DefaultNull;
			}
			var prevIndex = branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
			var assignmentIndex = Max(prevIndex + 1, 2);
			if (assignmentIndex != 2 || branch.Parent[assignmentIndex].Name != "=")
			{
				GenerateMessage(ref errors, 0x4054, branch.Parent[assignmentIndex].Pos);
				return DefaultNull;
			}
			if (NStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive && block.Name == "var"
				&& prevIndex >= 1 && branch.Parent.Length > prevIndex + 1 && branch.Parent[prevIndex + 1].Name == "="
				&& branch.Parent[prevIndex - 1].Name == nameof(Hypername) && branch.Parent[prevIndex - 1].Length == 2
				&& branch.Parent[prevIndex - 1][0].Name == "new type"
				&& branch.Parent[prevIndex - 1][0].Extra is NStarType AssigningNStarType
				&& AssigningNStarType.MainType.Equals(DictionaryBlockStack))
			{
				var t = C.UserDefinedConstants[matchingContainer][varName];
				t.NStarType = AssigningNStarType;
				C.UserDefinedConstants[matchingContainer][varName] = t;
				if (TypeEqualsToPrimitive(AssigningNStarType, RecursiveTypeName) && varName == RecursiveTypeName)
				{
					var otherPos = branch[1].Pos;
					GenerateMessage(ref errors, 0x4092, otherPos);
					branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				branch.Extra = branch[0].Extra = NStarType = AssigningNStarType;
			}
			if (NStarType.MainType.Equals(DictionaryBlockStack) && branch.Parent[0].Name == nameof(List)
				&& branch.Parent[0].Length == 1 && !(branch.Parent[0][0].Name == nameof(List) && branch.Parent[0][0].Length == 2
				&& branch.Parent[0][0][0].Name.AsSpan() is Pattern or "type" or "Declaration")
				&& branch.Parent[0][0].Name != ClassMain)
			{
				var KeyNStarType = NStarType.ExtraTypes[0].Extra;
				TreeBranch type = new("type", branch.Parent[0].Pos, branch.Parent[0].Container) { Extra = KeyNStarType };
				TreeBranch @null = new(NullString, branch.Parent[0].Pos, branch.Parent[0].Container);
				TreeBranch pattern = new(Pattern, [type, @null, new("or", branch.Parent[0].Pos, branch.Parent[0].Container)]);
				branch.Parent[0] = new(nameof(List), new(nameof(List), [pattern, branch.Parent[0]]));
			}
			else if (NStarType.MainType.Equals(DictionaryBlockStack) && branch.Parent[0].Name == nameof(Hypername)
				&& branch.Parent[0].Length == 2 && branch.Parent[0][0].Name == "new type"
				&& NStarType.Equals(branch.Parent[0][0].Extra) && branch.Parent[0][1].Name == nameof(ConstructorCall)
				&& (branch.Parent[0][1].Elements.All(x => x.Name == nameof(List) && x.Length == 2)
				&& branch.Parent[0][1].Elements.Any(x => x[0].Name.AsSpan() is Pattern or "type" or "Declaration")
				|| branch.Parent[0][1][0].Name == ClassMain))
				branch.Parent[0] = new(nameof(List), branch.Parent[0][1].Elements, branch.Parent[0][1].Container);
			else if (NStarType.MainType.Equals(DictionaryBlockStack) && branch.Parent[0].Name == nameof(Hypername)
				&& branch.Parent[0].Length == 2 && branch.Parent[0][0].Name == "new type"
				&& NStarType.Equals(branch.Parent[0][0].Extra) && branch.Parent[0][1].Name == nameof(ConstructorCall)
				&& branch.Parent[0][1].Length == 1)
			{
				var KeyNStarType = NStarType.ExtraTypes[0].Extra;
				TreeBranch type = new("type", branch.Parent[0][1].Pos, branch.Parent[0][1].Container) { Extra = KeyNStarType };
				TreeBranch @null = new(NullString, branch.Parent[0].Pos, branch.Parent[0].Container);
				TreeBranch pattern = new(Pattern, [type, @null, new("or", branch.Parent[0].Pos, branch.Parent[0].Container)]);
				branch.Parent[0] = new(nameof(List), new(nameof(List), [pattern, branch.Parent[0][1]]));
			}
			if (NStarType.MainType.Equals(DictionaryBlockStack) && branch.Parent[0].Name == nameof(List)
				&& branch.Parent[0].Length == 1 && branch.Parent[0][0].Name == ClassMain)
				ClassMainToPolymorphClass(branch.Parent[0], ref errors, NStarType);
			if (NStarType.MainType.Equals(DictionaryBlockStack) && branch.Parent[0].Name == nameof(List)
				&& branch.Parent[0].Elements.All(x => x.Name == nameof(List) && x.Length == 2)
				&& branch.Parent[0].Elements.Any(x => x[0].Name.AsSpan() is Pattern or "type" or "Declaration"))
			{
				if (NStarType.ExtraTypes[0].Extra is NStarType KeyNStarType
					&& KeyNStarType.MainType.TryPeek(out var sourceBlock)
					&& (sourceBlock.BlockType == BlockType.Primitive && sourceBlock.Name.AsSpan() is ByteTypeName or ShortIntTypeName
					or UnsignedShortIntTypeName or IntTypeName or UnsignedIntTypeName or LongIntTypeName or UnsignedLongIntTypeName
					or RealTypeName or DecimalTypeName or StringTypeName or ObjectTypeName
					|| sourceBlock.BlockType == BlockType.Class && sourceBlock.Name == "UnsafeString"))
				{
					branch[0].Extra = NStarType = new(FuncBlockStack,
						new([NStarType.ExtraTypes[1], NStarType.ExtraTypes[0]]));
					DictionaryToFunc(branch.Parent[0], ref errors, NStarType);
					variableNameMapping[^1][varName] = realName = varName;
				}
				else
				{
					branch[0].Extra = NStarType = new(FuncDictionaryBlockStack, NStarType.ExtraTypes);
					DictionaryToFuncDictionary(branch.Parent[0], ref errors, NStarType);
					variableNameMapping[^1][varName] = realName = varName;
				}
			}
			C.UserDefinedConstants[matchingContainer][varName] = new(constant.Value.NStarType,
				constant.Value.Attributes, branch.Parent[0]);
		}
		if (TypeEqualsToPrimitive(NStarType, "var"))
		{
			var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is NStarType AssigningNStarType
				&& branch.Parent.Length >= 3 && branch.Parent[prevIndex + 1].Name == "=")
			{
				Type(ref AssigningNStarType, branch, ref errors, true);
				if (TypeEqualsToPrimitive(AssigningNStarType, TupleName, false)
					&& (AssigningNStarType.ExtraTypes.AllEqual() ? AssigningNStarType.ExtraTypes.Length
					: AssigningNStarType.ExtraTypes.Length == 2
					&& int.TryParse(ParseAction(AssigningNStarType.ExtraTypes[1].Name)(AssigningNStarType.ExtraTypes[1], out _)
					.ToString(), out var n)
					? n : -1) is var tupleLength && tupleLength >= 0
					&& AssigningNStarType.ExtraTypes[0].Name == "type"
					&& BoolType.Equals(AssigningNStarType.ExtraTypes[0].Extra) is var @bool)
					C.InlineArrays.TryAdd(@bool ? ~tupleLength : tupleLength, (new(RandomVarName()), false));
				branch.Parent[prevIndex - 1].Extra = AssigningNStarType;
				if (CheckContainer(branch.Container, C.UserDefinedConstants.ContainsKey, out var matchingContainer)
					&& C.UserDefinedConstants[matchingContainer].TryGetValue(varName, out var constant))
					constant.NStarType = AssigningNStarType;
				else if (CheckContainer(branch.Container, C.Variables.ContainsKey, out matchingContainer)
					&& C.Variables[matchingContainer].ContainsKey(varName))
					C.Variables[matchingContainer][varName] = AssigningNStarType;
				if (TypeEqualsToPrimitive(AssigningNStarType, RecursiveTypeName) && varName == RecursiveTypeName)
				{
					var otherPos = branch[1].Pos;
					GenerateMessage(ref errors, 0x4092, otherPos);
					branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				branch.Extra = branch[0].Extra = AssigningNStarType;
			}
			else if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is null && prepass) { }
			else if (IsPattern(branch, out var patternBranch, out var patternIndex)
				|| IsSwitchPattern(branch, out patternBranch, out patternIndex))
				branch.Extra = branch[0].Extra = patternBranch[patternIndex - 2].Extra;
			else
			{
				var otherPos = branch[0].Pos;
				GenerateMessage(ref errors, 0x4011, otherPos);
				branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
				return "_";
			}
		}
		else if (TypeEqualsToPrimitive(NStarType, RecursiveTypeName) && varName == RecursiveTypeName)
		{
			var otherPos = branch[1].Pos;
			GenerateMessage(ref errors, 0x4092, otherPos);
			branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
			return "_";
		}
		else if (C.UserDefinedTypes.TryGetValue(SplitType(NStarType.MainType),
			out var userDefinedType) && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))]
				= new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
				{
					Extra = NullType
				};
			return "_";
		}
		else
		{
			if (TypeEqualsToPrimitive(NStarType, TupleName, false))
			{
				Type(ref NStarType, branch, ref errors, true);
				int tupleLength;
				if (NStarType.ExtraTypes.AllEqual())
					tupleLength = NStarType.ExtraTypes.Length;
				else if (NStarType.ExtraTypes.Length == 2
					&& int.TryParse(ParseAction(NStarType.ExtraTypes[1].Name)(NStarType.ExtraTypes[1], out _).ToString(),
					out var n))
					tupleLength = n;
				else
					tupleLength = -1;
				if (tupleLength >= 0 && NStarType.ExtraTypes[0].Name == "type"
					&& BoolType.Equals(NStarType.ExtraTypes[0].Extra) is var @bool)
					C.InlineArrays.TryAdd(@bool ? ~tupleLength : tupleLength, (new(RandomVarName()), false));
			}
			branch.Extra = NStarType;
			var targetIndex = Max(branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
			branch.Parent[targetIndex].Extra ??= NStarType;
		}
		if (branch.Extra is NStarType NStarType2 && NStarType2.Equals(NullType))
			return "_";
		if (branch.Extra is not NStarType ResultType)
			ResultType = NullType;
		return Type(ref ResultType, branch, ref errors)
			.Copy().Add(' ').AddRange(EscapedKeywords.Contains(realName) ? ((String)"@").AddRange(realName) : realName);
	}

	private String Hypername(TreeBranch branch, out List<String>? errors) => Hypername(branch, out errors, null, false);

	private String Hypername(TreeBranch branch, out List<String>? errors, object? extra, bool prepass)
	{
		String result = [];
		errors = null;
		result.AddRange(Hypername1(branch, out var firstErrors, ref extra, prepass));
		AddRange(ref errors, firstErrors);
		for (var i = 1; i < branch.Length; i++)
		{
			if ((i == 1 || i == 2 && branch[1].Name == nameof(Indexes))
				&& branch[i].Name.AsSpan() is nameof(Call) or nameof(ConstructorCall))
			{
				result.Replace(Hypername2(branch, ref errors, ref extra, ref i));
				continue;
			}
			if (i < branch.Length - 1 && branch[i].Name == nameof(Indexes)
				&& branch[i + 1].Name.AsSpan() is nameof(Call) or nameof(ConstructorCall))
				continue;
			var innerResult = Hypername2(branch, ref errors, ref extra, ref i);
			if (innerResult.AsSpan() is DefaultConst or DefaultNull)
				return DefaultNull;
			if (result.ContainsAnyExcluding(AlphanumericCharacters)
				&& !(branch.Extra is not null && branch.Extra.Equals(NullType)))
				result.Insert(0, '(').Add(')');
			foreach (var keyseq in HypernameKeySequences)
				if (innerResult.StartsWith("(." + keyseq))
				{
					innerResult.ReplaceRange(0, ("(." + keyseq).Length, ".");
					result.Insert(0, "(" + keyseq);
				}
				else if (innerResult.StartsWith("." + keyseq))
				{
					innerResult.Remove(1, keyseq.Length);
					result.Insert(0, keyseq);
				}
			while (innerResult.StartsWith('(') && innerResult.Find(x => x != '(') == '.')
			{
				innerResult.RemoveAt(0);
				result.Insert(0, '(');
			}
			if (innerResult.StartsWith("(await ("))
			{
				innerResult.Remove(0, "(await ".Length);
				result.Insert(0, "(await (").Add(')');
			}
			if (innerResult.StartsWith('.')
				&& TryReadValue(innerResult[1..^(innerResult.EndsWith(')') ? 1 : 0)], out var value))
			{
				branch.Name = value.ToString(true, true);
				branch.Elements.Clear();
				branch.Extra ??= value.GetNStarType();
				return branch.Name.Copy();
			}
			if (branch[i].Name == nameof(Hypername) && branch[i].Length != 0
				&& branch[0].Name == "type" && branch[0].Extra is NStarType ContainerNStarType
				&& C.ConstantExists(ContainerNStarType, branch[i][0].Name, out _)
				|| TryReadValue(innerResult, out _))
				result = innerResult;
			else
				result.AddRange(innerResult);
		}
		return result;
	}

	private String Hypername1(TreeBranch branch, out List<String>? errors, ref object? extra, bool prepass)
	{
		String result = [];
		errors = null;
		if (branch.Name == "Hypername" && branch.Length == 0)
			return DefaultNull;
		var targetBranch = branch.Length == 0 ? branch : branch[0];
		var branchName = targetBranch.Name.GetBefore(" (function)");
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		var callIndex = branch.Length >= 3 && branch[1].Name == nameof(Indexes) && branch[2].Name == nameof(Call) ? 2 : 1;
		var innerErrorsLists = branch.Length <= 1 || branch[0].Name.EndsWith(DelegateSuffix)
			? [] : new List<String>?[branch[callIndex].Length];
		var subbranchValues = branch.Length <= 1 || branch[0].Name.EndsWith(DelegateSuffix)
			? [] : branch[callIndex].Elements.ToList((x, index) =>
			branch[0].Name == "new" && branch.Extra is null && index != 0 ? DefaultNull
			: x.Name == nameof(Hypername) ? Hypername(x, out innerErrorsLists[index], null, true)
			: ParseAction(x.Name)(x, out innerErrorsLists[index]));
		var parameterTypes = branch.Length <= 1 ? [] : branch[callIndex].Elements.ToList((x, index) =>
			branch[0].Name == "new" && branch.Extra is null && index != 0 ? NullType : x.Extra is NStarType NStarType
			? NStarType : NullType);
		for (var i = 0; i < innerErrorsLists.Length; i++)
		{
			var innerErrors = innerErrorsLists[i];
			if (subbranchValues[i].AsSpan() is not ("" or "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual))
				AddRange(ref errors, innerErrors);
		}
		if (extra is null)
		{
			if (MainParsing.TryParse(branchName.ToString(), out var value))
			{
				targetBranch.Extra = value.GetNStarType();
				extra = new List<object> { (String)nameof(Constant), value.GetNStarType() };
				return value.ToString(true, true);
			}
			if (TryReadValue(branchName, out value))
				result.AddRange(value.ToString(true, true));
			else if (branch[0].Length != 0)
			{
				result.AddRange(ParseAction(branch[0].Name)(branch[0], out var innerErrors));
				AddRange(ref errors, innerErrors);
				branch.Extra = branch[0].Extra;
				extra = new List<object> { (String)nameof(Expr), branch.Extra! };
			}
			else if (branchName == "type")
				extra = HypernameType(branch, ref errors, result);
			else if (branchName == "new")
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return DefaultNull;
				}
				if (!ImplicitConstructor(branch, ref errors, parameterTypes, prepass))
					return DefaultNull;
				if (branch[0].Extra is not NStarType NStarType)
					NStarType = NullType;
				Type(ref NStarType, branch, ref errors, true);
				extra = HypernameConstructor(branch, subbranchValues, parameterTypes, ref NStarType, true);
				if (branch[0].Extra is NStarType)
					branch[0].Name = "new type";
				if (NStarType == NullType)
					return DefaultNull;
				else if (C.TypeIsFullySpecified(NStarType, branch.Container))
					result.AddRange("new ").AddRange(Type(ref NStarType, targetBranch, ref errors));
				else
				{
					result.AddRange("Activator.CreateInstance(");
					result.AddRange(TypeReflected(ref NStarType, targetBranch, ref errors));
					result.AddRange(", (List<object>)");
				}
			}
			else if (branchName == "new type")
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return DefaultNull;
				}
				if (branch[0].Extra is not NStarType NStarType)
					NStarType = NullType;
				Type(ref NStarType, branch, ref errors, true);
				branch[0].Extra = NStarType;
				extra = HypernameConstructor(branch, subbranchValues, parameterTypes, ref NStarType);
				result.AddRange("new ").AddRange(branch[0].Extra is NStarType type
					? Type(ref type, targetBranch, ref errors) : DynamicName);
				return result;
			}
			else if (IsConstantDeclared(branch, branchName, out var constantErrors, out var constant))
			{
				if (branch.Parent is not null && branch.Parent.Name.AsSpan() is nameof(Assignment) or UnaryAssignment)
				{
					GenerateMessage(ref errors, 0x4052, branch.Parent[Max(prevIndex + 1, 2)].Pos);
					branch.Parent.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return DefaultNull;
				}
				var localConstantsDepth = constantsDepth;
				constantsDepth++;
				List<String>? innerErrors = null;
				TreeBranch cvalue;
				if (localConstantsDepth >= 25)
					return ConstantsDepthExceeded(ref errors, localConstantsDepth);
				else if (constant.HasValue && (cvalue = constant.Value.DefaultValue) is not null
					&& cvalue.Parent is not null && cvalue.Parent.Name == nameof(Assignment) && cvalue.Parent.Length == 3
					&& ReferenceEquals(cvalue.Parent[0], cvalue)
					&& cvalue.Parent[1].Name == nameof(Declaration) && cvalue.Parent[1].Length == 2
					&& cvalue.Parent[1][0].Name == "type" && cvalue.Parent[1][0].Extra is NStarType NStarType
					&& (NStarType.MainType.Equals(DictionaryBlockStack) && cvalue.Name == nameof(List)
					&& cvalue.Elements.All(x => x.Name == nameof(List) && x.Length == 2)
					|| NStarType.MainType.Equals(FuncBlockStack) && cvalue.Name == nameof(Lambda) && cvalue.Length == 2
					&& (cvalue[0].Length == 1 ? cvalue[0][0] : cvalue[0]).Name is var keyName
					&& cvalue[1].Name == nameof(SwitchExpr) && cvalue[1].Length == 2 && cvalue[1][0].Name == keyName
					&& cvalue[1][1].Name == "switch" && cvalue[1][1].Length != 0
					&& cvalue[1][1].Elements.All(x => x.Name == "case" && x.Length == 2)
					|| NStarType.MainType.Equals(FuncDictionaryBlockStack) && cvalue.Name == nameof(Hypername)
					&& cvalue.Length == 2 && cvalue[0].Name == "new type" && NStarType.Equals(cvalue[0].Extra)
					&& cvalue[1].Name == nameof(ConstructorCall) && NStarType.Equals(cvalue[1].Extra)))
				{
					branch.Extra = branch[0].Extra = NStarType;
					extra = new List<object> { (String)VariableWay, NStarType, subbranchValues };
				}
				else if (constant.HasValue && constant.Value.DefaultValue is not null
					&& TryReadValue(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue,
					out innerErrors).ToString(), out value))
				{
					branchName = branch.Name = value.ToString(true, true);
					branch.Elements.Clear();
					branch.Extra = value.GetNStarType();
					extra = new List<object> { (String)nameof(Constant), value.GetNStarType(), subbranchValues };
				}
				else
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Constant), NullType, subbranchValues };
				}
				if (prepass && branch.Length == 1 && branch.Parent is not null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				result.AddRange(branchName);
				AddRange(ref errors, constantErrors);
				AddRange(ref errors, innerErrors);
				constantsDepth = localConstantsDepth;
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, value.GetNStarType());
			}
			else if (IsVariableDeclared(branch, branchName, out var variableErrors, out var innerExtra))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return DefaultNull;
				}
				if (innerExtra is NStarType NStarType)
				{
					branch.Extra = branch[0].Extra = NStarType;
					extra = new List<object> { (String)VariableWay, NStarType, subbranchValues };
				}
				else
				{
					branch.Extra = branch[0].Extra = NStarType = NullType;
					extra = new List<object> { (String)VariableWay, NStarType, subbranchValues };
				}
				if (prepass && branch.Length == 1 && branch.Parent is not null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				if (EscapedKeywords.Contains(branchName))
					result.Add('@');
				var realName = branchName;
				variableNameMapping.FindLast(x => x.TryGetValue(branchName, out realName));
				result.AddRange(NStarType.Equals(NullType) ? "default(dynamic)" : realName ?? branchName);
				AddRange(ref errors, variableErrors!);
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, NStarType);
			}
			else if (IsPropertyDeclared(branch, branchName, out var propertyErrors, out var property,
				out var inBase, out var actualContainer))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return DefaultNull;
				}
				if (!property.HasValue)
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Property), NullType, subbranchValues };
					return DefaultNull;
				}
				var fullName = String.Join(".", actualContainer.Convert(x => x.Name)
					.Append(branchName).ToArray());
				TreeBranch? assignmentBranch;
				int assignmentIndex;
				if (inBase && (property.Value.Attributes & PropertyAttributes.Private) != 0
					&& (property.Value.Attributes & PropertyAttributes.Protected) == 0
					&& !branch.Container.StartsWith([.. actualContainer]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (inBase && IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					&& (property.Value.Attributes & PropertyAttributes.ProtectedSet) == 0
					&& !branch.Container.StartsWith([.. actualContainer]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& !branch.Container.StartsWith([.. actualContainer,
					new(BlockType.Constructor, "", 1)]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403A, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else
				{
					branch.Extra = branch[0].Extra = property.Value.NStarType;
					extra = new List<object> { (String)nameof(Property), property.Value.NStarType, subbranchValues };
				}
				(BlockStack, String) matchingKey = default!;
				if (!(CheckContainer(branch.Container, x => C.UserDefinedTypes.ContainsKey(matchingKey = SplitType(x)), out _)
					&& C.UserDefinedTypes.TryGetValue(matchingKey, out var userDefinedType)))
					throw new InvalidOperationException();
				if (property.HasValue && (property.Value.Attributes & PropertyAttributes.Required) != 0
					&& IsConstructor(branch, out _, out var overloads)
					&& CreateVar(userDefinedType.Restrictions, out var requiredProperties).Length != 0
					&& CreateVar(requiredProperties?.FindLastIndex(x => x.RestrictionType.Equals(property.Value.NStarType)
					&& x.Name == branchName) ?? -1, out var foundIndex) >= 0)
					overloads[0].UnsetRequiredProperties.RemoveValue(foundIndex);
				if (prepass && branch.Length == 1 && branch.Parent is not null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				if (EscapedKeywords.Contains(branchName))
					result.Add('@');
				result.AddRange(branchName);
				AddRange(ref errors, propertyErrors!);
				if (!(IsAnyAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, property.Value.NStarType);
			}
			else if (IsFunctionDeclared(branch, branchName, out var functionErrors,
				out var functions, out var functionContainer, out _) && functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return DefaultNull;
				}
				if (functionContainer.Length == 0)
					HypernamePublicExtendedMethod(branch, branchName, subbranchValues, ref extra,
						ref errors, prevIndex, functions, MethodProcessingWay.User);
				else if (HypernameExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors,
					prevIndex, new(functionContainer, NoBranches), functions, MethodProcessingWay.UserMethod) is not null)
					return "_";
				result.AddRange(functions[^1].RealName);
				branch.Extra = new NStarType(FuncBlockStack,
					new([new("type", branch.Pos, branch.Container) { Extra = functions[^1].ReturnNStarType },
					.. functions[^1].Parameters.Convert(x =>
					new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type })]));
				AddRange(ref errors, functionErrors!);
			}
			else if (C.ExtendedMethodExists(new(), branchName, parameterTypes, out functions, out var user)
				&& functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return DefaultNull;
				}
				if (branchName.AsSpan() is "ExecuteString" or "Q" && !(branch.Length >= 2 && branch[callIndex].Name == nameof(Call)))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4020, otherPos, branchName);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				HypernamePublicExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors,
					prevIndex, functions, user ? MethodProcessingWay.User : MethodProcessingWay.General);
				result.AddRange(functions[^1].RealName);
				branch.Extra = new NStarType(FuncBlockStack,
					new([new("type", branch.Pos, branch.Container) { Extra = functions[^1].ReturnNStarType },
					.. functions[^1].Parameters.Convert(x =>
					new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type })]));
			}
			else if (branch.Length == 1 && branch.Extra is NStarType RecursiveNStarType
				&& RecursiveNStarType.Equals(RecursiveType) && PrimitiveTypes.ContainsKey(branchName))
			{
				if (branchName == RecursiveTypeName)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4090, otherPos, branchName);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				NStarType primitiveType = (new([new(PrimitiveTypes.ContainsKey(branchName)
					? BlockType.Primitive : BlockType.Extra, branchName, 1)]), NoBranches);
				branch[0] = new("type", branch.Pos, branch.Container) { Extra = primitiveType };
				result.AddRange(OpeningTypeof).AddRange(Type(ref primitiveType, targetBranch, ref errors)).Add(')');
			}
			else if (branchName.AsSpan() is "i" or "ImaginaryUnit")
			{
				branch.Extra = ComplexType;
				result.AddRange("new Complex(0, 1)");
			}
			else if (!prepass)
			{
				var otherPos = branch.FirstPos;
				if (variableErrors is not null && variableErrors.Length != 0)
					AddRange(ref errors, variableErrors);
				else if (propertyErrors is not null && propertyErrors.Length != 0)
					AddRange(ref errors, propertyErrors);
				else if (functionErrors is not null && functionErrors.Length != 0)
					AddRange(ref errors, functionErrors);
				else
					GenerateMessage(ref errors, 0x4001, otherPos, branchName);
				branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				return prevIndex == 0 || branch.Parent.Name == nameof(List) ? DefaultNull : "_";
			}
		}
		else
		{
			if (!(extra is List<object> paramCollection && paramCollection.Length is >= 2 and <= 5
				&& paramCollection[0] is String Category && paramCollection[1] is NStarType ContainerNStarType))
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4000, otherPos, 3);
				return DefaultNull;
			}
			if (C.ConstantExists(ContainerNStarType, branchName, out var constant))
			{
				if (IsAssignment(branch, out var assignmentBranch, out var assignmentIndex))
				{
					GenerateMessage(ref errors, 0x4052, assignmentBranch[assignmentIndex].Pos);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return DefaultNull;
				}
				var localConstantsDepth = constantsDepth;
				constantsDepth++;
				object? value;
				if (localConstantsDepth >= 25)
					return ConstantsDepthExceeded(ref errors, localConstantsDepth);
				else if (!constant.HasValue || (constant.Value.Attributes & ConstantAttributes.Private) != 0
					^ (constant.Value.Attributes & ConstantAttributes.Protected) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
					return InaccessibleConstant(ref errors, localConstantsDepth);
				else if (constant.Value.DefaultValue is not null && constant.Value.DefaultValue.Name == "#value"
					&& constant.Value.DefaultValue.Extra is String literal && MainParsing.TryParse(literal.AsSpan(), out value))
				{
					branchName = literal;
					branch.Extra = branch[0].Extra = constant.Value.NStarType;
					extra = new List<object> { (String)nameof(Constant), branch.Extra, subbranchValues };
				}
				else if (constant.Value.DefaultValue is not null && constant.Value.DefaultValue.Name == "#value"
					&& constant.Value.DefaultValue.Extra is String @string)
				{
					var NStarType = constant.Value.NStarType;
					branchName = Type(ref NStarType, constant.Value.DefaultValue, ref errors).Copy();
					branchName.Add('.').AddRange(nameof(int.Parse)).Add('(').AddRange(@string.TakeIntoQuotes(true)).Add(')');
					value = 0;
					branch.Extra = branch[0].Extra = NStarType;
					extra = new List<object> { (String)nameof(Constant), branch.Extra };
				}
				else if (!(constant.Value.DefaultValue is not null
					&& TryReadValue(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue,
					out _).ToString(), out value)))
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Constant), NullType };
					constantsDepth = localConstantsDepth;
					return "_";
				}
				else if (!TypesAreCompatible(branch, ref errors, value.GetNStarType(),
					CreateVar(constant.Value.NStarType, out var NStarType), out var warning,
					value.ToString(true, true), out var adaptedSource, out var extraMessage)
					|| warning || adaptedSource is null)
				{
					var otherPos = constant.Value.DefaultValue.Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, value.GetNStarType(), NStarType, ConstantValue);
					return "_";
				}
				else if (branch.Length == 1)
				{
					branchName = adaptedSource;
					branch.Extra = value.GetNStarType();
					extra = new List<object> { (String)nameof(Constant), branch.Extra, subbranchValues };
				}
				else
				{
					branchName = adaptedSource;
					branch.Extra = branch[0].Extra = value.GetNStarType();
					extra = new List<object> { (String)nameof(Constant), branch.Extra, subbranchValues };
				}
				result.AddRange(branchName);
				constantsDepth = localConstantsDepth;
				WrapIntoAsync(branch, result, value.GetNStarType());
			}
			else if (C.PropertyExists(ContainerNStarType, PropertyMapping(branchName), Category == "Static", out var property))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return DefaultNull;
				}
				if (!property.HasValue)
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Property), NullType, subbranchValues };
					return "_";
				}
				var fullName = String.Join(".", ContainerNStarType.MainType.Convert(x => x.Name).Append(branchName).ToArray());
				TreeBranch? assignmentBranch;
				int assignmentIndex;
				if ((property.Value.Attributes & PropertyAttributes.Private) != 0
					^ (property.Value.Attributes & PropertyAttributes.Protected) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Value.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType,
					new(BlockType.Constructor, "", 1)]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403A, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					assignmentBranch.Name = NullString;
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				branch.Extra = branch[0].Extra
					= new NStarType(property.Value.NStarType.MainType, property.Value.NStarType.ExtraTypes);
				extra = new List<object> { (String)nameof(Property), branch.Extra, subbranchValues };
				result.AddRange(PropertyMapping(branchName));
				if (!(prepass && IsAnyAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, property.Value.NStarType);
			}
			else if (C.UserDefinedFunctionExists(ContainerNStarType, branchName, parameterTypes, [], out var functions)
				&& functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return DefaultNull;
				}
				branch.Extra = functions;
				if (HypernameExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors, prevIndex,
					ContainerNStarType, functions, MethodProcessingWay.UserMethod) is not null)
					return "_";
				result.AddRange(functions[^1].RealName);
			}
			else if (C.MethodExists(ContainerNStarType, C.FunctionMapping(branchName, parameterTypes, null, null), parameterTypes,
				[], out functions) && functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return DefaultNull;
				}
				branch.Extra = functions;
				if (HypernameMethod(branch, branchName, subbranchValues, ref extra, ref errors, prevIndex,
					ContainerNStarType, functions) is not null)
					return "_";
				result.AddRange(functions[^1].RealName);
			}
			else
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4033, otherPos,
					String.Join(".", ContainerNStarType.MainType.ToArray(x => x.Name)), branchName);
				branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				return "_";
			}
			String InaccessibleConstant(ref List<String>? errors, int constantsDepth)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4030, otherPos,
					String.Join(".", ContainerNStarType.MainType.Convert(x => x.Name).Append(branchName).ToArray()));
				branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				this.constantsDepth = constantsDepth;
				return "_";
			}
		}
		Debug.Assert(prepass || branch.Extra is not null);
		return result;
		String ConstantsDepthExceeded(ref List<String>? errors, int constantsDepth)
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4055, otherPos);
			branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new(NullString, branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = NullType
			};
			this.constantsDepth = constantsDepth;
			return "_";
		}
	}

	private List<object> HypernameType(TreeBranch branch, ref List<String>? errors, String result)
	{
		var targetBranch = branch.Length == 0 ? branch : branch[0];
		if (branch[0].Extra is not NStarType NStarType)
			NStarType = NullType;
		var extra = new List<object> { (String)"Static", NStarType };
		var bTypename = branch.Extra is NStarType OuterNStarType && OuterNStarType.MainType.Equals(RecursiveBlockStack);
		var bExtraType = !C.TypeIsFullySpecified(NStarType, branch.Container);
		if (NStarType.MainType.Equals(RecursiveBlockStack))
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4090, otherPos);
			branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new(NullString, branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = NullType
			};
			result.Replace("_");
			return default!;
		}
		if (branch.Extra is NStarType OuterNStarType2 && OuterNStarType2.MainType.Equals(RecursiveBlockStack)
			&& OuterNStarType2.ExtraTypes.Length == 1 && OuterNStarType2.ExtraTypes[0].Name == "type"
			&& OuterNStarType2.ExtraTypes[0].Extra is NStarType RestrictionNStarType
			&& (!TypesAreCompatible(branch, ref errors, NStarType, RestrictionNStarType, out var warning, [], out _, out _)
			|| warning))
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4094, otherPos, NStarType, OuterNStarType2);
			branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new(NullString, branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = NullType
			};
			result.Replace("_");
			return default!;
		}
		if (bExtraType)
		{
			var fullName = TypeReflected(ref NStarType, branch, ref errors);
			result.AddRange(fullName);
		}
		else
		{
			if (bTypename)
				result.AddRange(OpeningTypeof);
			result.AddRange(Type(ref NStarType, targetBranch, ref errors));
			if (bTypename)
				result.Add(')');
		}
		branch.Extra ??= branch[0].Extra;
		return extra;
	}

	private bool ImplicitConstructor(TreeBranch branch, ref List<String>? errors, List<NStarType> parameterTypes, bool prepass)
	{
		if (branch.Extra is NStarType NStarType)
		{
			var split = SplitType(NStarType.MainType);
			if (!NStarType.MainType.TryPeek(out var block) || block.BlockType is not (BlockType.Primitive or BlockType.Extra
				or BlockType.Class or BlockType.Struct or BlockType.Interface or BlockType.Delegate or BlockType.Enum))
				throw new InvalidOperationException();
			else if (block.BlockType is BlockType.Delegate or BlockType.Enum
				|| block.BlockType == BlockType.Primitive && block.Name.AsSpan() is NullString or BoolTypeName or ByteTypeName
				or ShortCharTypeName or ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName or LongCharTypeName
				or LongIntTypeName or UnsignedLongIntTypeName or LongLongTypeName or RealTypeName or DecimalTypeName or ComplexTypeName or RecursiveTypeName
				or "index" or "range" or "nint" or DynamicName
				|| block.BlockType is BlockType.Class && C.TypeExists(split, out var netType)
				&& netType.IsAbstract && netType.IsSealed
				|| C.UserDefinedTypes.TryGetValue(split, out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4017, otherPos, NStarType.ToString());
				branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new(NullString, branch.Pos,
					branch.EndPos, branch.Container)
				{
					Extra = NullType
				};
				return false;
			}
			else if (block.BlockType == BlockType.Primitive && block.Name == ObjectTypeName || block.BlockType == BlockType.Interface
				|| block.BlockType is BlockType.Class && C.TypeExists(split, out netType)
				&& netType.IsAbstract || C.UserDefinedTypes.TryGetValue(split, out userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Abstract)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4018, otherPos, NStarType.ToString());
				branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new(NullString, branch.Pos,
					branch.EndPos, branch.Container)
				{
					Extra = NullType
				};
				return false;
			}
			branch[0].Extra = NStarType;
		}
		else if (parameterTypes.Length != 0 && parameterTypes[0] is NStarType ParameterNStarType)
		{
			if (prepass)
				return false;
			branch[1].Elements.Skip(1).ForEach(x => x.Extra = ParameterNStarType);
			branch.Extra = branch[0].Extra = GetListType(ParameterNStarType);
		}
		return true;
	}

	private object HypernameConstructor(TreeBranch branch, List<String> parameters, List<NStarType> parameterTypes,
		ref NStarType NStarType, bool @implicit = false)
	{
		object? extra;
		if (C.UserDefinedConstructorsExist(NStarType, parameterTypes, out var constructors) && constructors is not null)
			extra = new List<object>
			{
				(String)nameof(Constructor), NStarType, ConstructorProcessingWay.User, constructors, parameters
			};
		else if (C.ConstructorsExist(NStarType, parameterTypes, out constructors) && constructors is not null)
			extra = new List<object>
			{
				(String)nameof(Constructor), NStarType, ConstructorProcessingWay.Typical, constructors, parameters
			};
		else
		{
			if (!@implicit)
				NStarType = NullType;
			extra = new List<object> { (String)nameof(Constructor), NStarType, parameters };
		}
		branch[1].Elements.ToList((x, index) => x.Extra is NStarType SourceType
			&& (!TypeConverters.TypesAreCompatible(C, SourceType, parameterTypes[index], out var warning,
			parameters[index], out _, out _) || warning));
		return extra;
	}

	private String Hypername2(TreeBranch branch, ref List<String>? errors, ref object? extra, ref int index)
	{
		String result = [];
		if (branch[index].Name == nameof(Call) && extra is List<object> paramCollection)
		{
			var (flowControl, value) = HypernameCall(branch, ref errors, ref extra, index, result, paramCollection);
			if (!flowControl)
				return value;
		}
		else if (branch[index].Name == nameof(ConstructorCall) && extra is List<object> processingWay)
		{
			var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList(x =>
				x.Extra is NStarType NStarType ? NStarType : NullType);
			if (processingWay.Length == 3 && processingWay[1] is NStarType ReflectedNStarType
				&& ReflectedNStarType != NullType && processingWay[2] is List<String> reflectedInnerResults)
			{
				result.AddRange("Activator.CreateInstance(");
				result.AddRange(TypeReflected(ref ReflectedNStarType, branch[0], ref errors));
				result.AddRange(", new object[] { ");
				for (var i = 0; i < reflectedInnerResults.Length; i++)
				{
					reflectedInnerResults[i] = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
					AddRange(ref errors, innerErrors);
				}
				result.AddRange(String.Join(", ", reflectedInnerResults)).AddRange(" })");
				Debug.Assert(branch.Extra is not null);
				return result;
			}
			if (!(processingWay.Length == 5 && processingWay[0] is String elem1 && elem1 == nameof(Constructor)
				&& processingWay[1] is NStarType ConstructingNStarType && processingWay[2] is ConstructorProcessingWay elem3
				&& processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
				&& processingWay[4] is List<String> parameters
				&& (C.ConstructorsExist(ConstructingNStarType, parameterTypes, out var innerConstructors)
				|| C.UserDefinedConstructorsExist(ConstructingNStarType, parameterTypes, out innerConstructors))))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4000, otherPos, 4);
				return DefaultNull;
			}
			ConstructorCall(branch[index], parameters.ToList(x => x.Copy()), out _, extra);
			for (var i = 0; i < parameters.Length; i++)
				if (parameters[i].AsSpan() is "" or "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
				{
					parameters[i] = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
					AddRange(ref errors, innerErrors);
				}
			if (elem3 == ConstructorProcessingWay.Typical)
			{
				processingWay[3] = innerConstructors;
				result.AddRange("new ").AddRange(Type(ref ConstructingNStarType, branch[index], ref errors));
				result.AddRange(ConstructorCall(branch[index], parameters, out var innerErrors, extra));
				AddRange(ref errors, innerErrors);
				branch.Extra = branch[0].Extra;
				if (innerErrors is not null && innerErrors.Any(x => x.StartsWith("Error")))
					return DefaultNull;
			}
			else
			{
				processingWay[3] = innerConstructors;
				var (flowControl, value) = HypernameUserConstructor(branch[index], ref errors, extra);
				branch.Extra = branch.Length == 0 ? NullType : branch[0].Extra;
				result.AddRange(value);
				if (!flowControl)
					return result;
			}
		}
		else if (branch[index].Name == nameof(Indexes))
			result.AddRange(Indexes(branch, ref errors, extra, index));
		else if (branch[index].Name == nameof(Call))
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4038, otherPos);
			return DefaultNull;
		}
		else if (branch[index].Name == ".")
		{
			var innerResult = Hypername(branch[++index], out var innerErrors, extra, false);
			if (errors is not null && errors.Length != 0 && errors.Any(x => x.StartsWith("Error")))
				return DefaultNull;
			AddRange(ref errors, innerErrors);
			if (innerResult.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			{
				branch.Extra = NullType;
				return DefaultNull;
			}
			if (!(branch[index].Name == nameof(Hypername) && branch[index].Length != 0
				&& branch[0].Name == "type" && branch[0].Extra is NStarType ContainerNStarType
				&& C.ConstantExists(ContainerNStarType, branch[index][0].Name, out _)
				|| TryReadValue(innerResult, out _)))
				result.Add('.');
			while (innerResult.StartsWith('('))
			{
				innerResult.RemoveAt(0);
				result.Insert(0, '(');
			}
			result.AddRange(innerResult);
			extra = branch.Extra = branch[index].Extra;
			if (branch.Parent is not null && branch.Parent.Name == nameof(Assignment))
			{
				var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
				branch.Parent[targetIndex].Extra ??= branch.Extra;
			}
		}
		else
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos, 5);
			return DefaultNull;
		}
		Debug.Assert(branch.Extra is not null);
		return result;
	}

	private (bool flowControl, String value) HypernameCall(TreeBranch branch, ref List<String>? errors, ref object? extra,
		int index, String result, List<object> paramCollection)
	{
		List<String>? innerErrors;
		if (paramCollection.Length == 3 && paramCollection[0] is String delegateElem1
			&& delegateElem1.AsSpan() is VariableWay or nameof(Property) or nameof(Expr)
			&& paramCollection[1] is NStarType DelegateNStarType
			&& DelegateNStarType.MainType.Equals(FuncBlockStack)
			&& DelegateNStarType.ExtraTypes.Length != 0 && DelegateNStarType.ExtraTypes[0].Name == "type"
			&& DelegateNStarType.ExtraTypes[0].Extra is NStarType ReturnNStarType)
		{
			var realName = branch[index - 1].Name;
			variableNameMapping.FindLast(x => x.TryGetValue(branch[index - 1].Name, out realName));
			if (index == 1)
				result.AddRange(realName ?? branch[index - 1].Name);
			if (branch[index].Length != DelegateNStarType.ExtraTypes.Length - 1)
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4045, otherPos, DelegateNStarType.ExtraTypes.Length - 1);
				return (false, DefaultNull);
			}
			NStarType ParameterNStarType = default!, CallNStarType = default!;
			result.AddRange(List(branch[index], out innerErrors));
			var wrongParameterIndex = branch[index].Elements.Combine(DelegateNStarType.ExtraTypes.Skip(1))
				.FindIndex(x => x.Item1.Extra is not NStarType ParameterNStarType2
				|| !TypesAreCompatible(branch, ref innerErrors, ParameterNStarType = ParameterNStarType2,
				CallNStarType = x.Item2.Name == "type"
				&& x.Item2.Extra is NStarType NStarType ? NStarType : NullType,
				out var warning, [], out var destExpr, out _) || warning || destExpr is not null && destExpr.Length != 0);
			if (wrongParameterIndex >= 0)
			{
				var otherPos = branch[index][wrongParameterIndex].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, null!, ParameterNStarType, CallNStarType, "the call");
				return (false, DefaultNull);
			}
			AddRange(ref errors, innerErrors);
			branch.Extra = ReturnNStarType;
			if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
				&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
				&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
				WrapIntoAsync(branch, result, ReturnNStarType);
			return (false, result);
		}
		if (!(paramCollection.Length >= 5 && paramCollection.Length <= 6
			&& paramCollection[0] is String functionName && functionName.StartsWith(nameof(Function) + ' ')
			&& paramCollection[1] is MethodProcessingWay processingWay && paramCollection[2] is NStarType ContainerNStarType
			&& paramCollection[4] is List<String> parameters))
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos, 6);
			return (false, DefaultNull);
		}
		List<NStarType> typeParameters = [];
		String typeParametersCache = [];
		if (index == 2)
		{
			typeParametersCache.Add('<');
			for (var i = 0; i < branch[1].Length; i++)
			{
				if (i != 0)
					typeParametersCache.AddRange(", ");
				branch[1][i].Extra = RecursiveType;
				ParseAction(branch[1][i].Name)(branch[1][i], out innerErrors);
				AddRange(ref errors, innerErrors);
				if (!(branch[1][i].Name == nameof(Hypername) && branch[1][i].Length == 1
					&& branch[1][i][0].Name == "type" && branch[1][i][0].Extra is NStarType NStarType))
					NStarType = NullType;
				typeParameters.Add(NStarType);
				typeParametersCache.AddRange(Type(ref NStarType, branch[1][i][0], ref errors));
			}
			typeParametersCache.Add('>');
		}
		for (var i = 0; i < parameters.Length; i++)
			if (parameters[i].AsSpan() is "" or "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			{
				parameters[i] = ParseAction(branch[index][i].Name)(branch[index][i], out innerErrors);
				AddRange(ref errors, innerErrors);
			}
		var parameterTypes = branch.Length <= 1 ? [] : branch[index].Elements.ToList(x =>
			x.Extra is NStarType NStarType ? NStarType : throw new InvalidOperationException());
		var name = functionName[(nameof(Function) + ' ').Length..];
		if (name == "ExecuteString")
		{
			var @string = parameters[0];
			var addParameters = branch[index].Length != 1;
			String? joinedParameters;
			if (addParameters)
			{
				joinedParameters = ((String)", ").AddRange(List(new(nameof(List), branch[index].Elements[1..]),
					out var parametersErrors));
				AddRange(ref errors, parametersErrors);
			}
			else
				joinedParameters = [];
			if (joinedParameters.StartsWith(", (") && joinedParameters.EndsWith(')'))
			{
				joinedParameters.ReplaceRange(2, 1, "new[]{");
				joinedParameters[^1] = '}';
			}
			result.AddRange(nameof(ExecuteProgram)).Add('(').AddRange(nameof(TranslateProgram));
			result.AddRange("(((String)@\"").AddRange(ExecuteStringPrefix).AddRange("\").AddRange(").AddRange(@string);
			result.AddRange("), new()).Wrap(x => (x.Item1.Remove(x.Item1.IndexOf(");
			result.AddRange(nameof(ExecuteStringPrefixCompiled)).AddRange("), ").AddRange(nameof(ExecuteStringPrefixCompiled));
			result.AddRange(""".Length), x.Item2, x.Item3)), new(), out _, out _""").AddRange(joinedParameters).AddRange(").");
			result.AddRange(nameof(Quotes.RemoveQuotes)).AddRange("()");
			branch.Extra = ObjectType;
		}
		else if (name == "Q")
		{
			branch.Extra = StringType;
			return (false, ((String)"((String)@\"").AddRange(input.Replace("\"", "\"\"")).AddRange("\")"));
		}
		else if (processingWay == MethodProcessingWay.Public && name == nameof(RedStarLinq.Fill) && branch[index].Length == 2
			&& branch[index][0].Extra is NStarType FirstParameterType
			&& TypeEqualsToPrimitive(FirstParameterType, BoolTypeName))
		{
			result.AddRange("new ").AddRange(nameof(BitList)).Add('(');
			result.AddRange(ParseAction(branch[index][1].Name)(branch[index][1], out innerErrors));
			AddRange(ref errors, innerErrors);
			result.AddRange(", ");
			result.AddRange(ParseAction(branch[index][0].Name)(branch[index][0], out innerErrors));
			AddRange(ref errors, innerErrors);
			result.Add(')');
			branch.Extra = BitListType;
			extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
		}
		else if (processingWay is not (MethodProcessingWay.User or MethodProcessingWay.UserMethod))
		{
			UnwrapParameters();
			if (C.MethodExists(ContainerNStarType, C.FunctionMapping(name, parameterTypes, null, null), parameterTypes,
				typeParameters, out var functions)
				&& functions.Length != 0
				|| C.ExtendedMethodExists(ContainerNStarType.MainType, name, branch[index].Elements
				.ToList(x => x.Extra is NStarType NStarType ? NStarType : throw new InvalidOperationException()),
				out functions, out _) && functions.Length != 0)
			{
				paramCollection[3] = functions;
				var convertedParameters = Call(branch[index], parameters, out innerErrors, extra);
				result.AddRange(C.FunctionMapping(name, parameterTypes, convertedParameters, typeParametersCache));
				AddRange(ref errors, innerErrors);
				if (convertedParameters is null)
				{
					branch.Extra = NullType;
					return (false, DefaultNull);
				}
				branch.Extra = functions[^1].ReturnNStarType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			if (!result.EndsWith(')') && !result.EndsWith(") + 1"))
				return (false, DefaultNull);
		}
		else if ((processingWay == MethodProcessingWay.User && C.UserDefinedFunctionExists(new(), name, parameterTypes,
			typeParameters, out var functions, out _, out var derived) || processingWay == MethodProcessingWay.UserMethod
			&& C.UserDefinedFunctionExists(ContainerNStarType, name, parameterTypes, typeParameters,
			out functions, out _, out derived)) && functions.Length != 0)
		{
			paramCollection[3] = functions;
			if (derived)
			{
				UnwrapParameters();
				var convertedParameters = Call(branch[index], parameters, out innerErrors, extra);
				result.AddRange(C.FunctionMapping(name, parameterTypes, convertedParameters, typeParametersCache));
				AddRange(ref errors, innerErrors);
			}
			else
			{
				var callResult = CallUser(branch[index], parameters, out innerErrors, extra);
				var realName = functions[^1].RealName;
				if (EscapedKeywords.Contains(realName))
					realName.Insert(0, '@');
				result.AddRange(index > 1 ? [] : realName).AddRange(typeParametersCache);
				result.AddRange(callResult);
				AddRange(ref errors, innerErrors);
			}
			DetectIndirectRecursion(branch,
				ContainerNStarType.Equals(NullType) ? [] : ContainerNStarType.MainType, name, functions, ref errors);
			if (!result.EndsWith(')'))
				return (false, DefaultNull);
			branch.Extra = functions[^1].ReturnNStarType;
			extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
			if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
				&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
				&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
				WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
		}
		else if (processingWay == MethodProcessingWay.UserMethod && branch.Parent?[0].Extra is NStarType ContainerNStarType2)
		{
			if (TypeEqualsToPrimitive(ContainerNStarType2, RecursiveTypeName)
				&& paramCollection.Length == 6 && paramCollection[5] is String elem4 && elem4 == Static
				&& C.UserDefinedFunctionExists(ContainerNStarType2, name, parameterTypes, typeParameters, out functions)
				&& functions.Length != 0)
			{
				paramCollection[3] = functions;
				var callResult = CallUser(branch[index], parameters, out innerErrors, extra);
				var realName = functions[^1].RealName;
				if (EscapedKeywords.Contains(realName))
					realName.Insert(0, '@');
				result.AddRange(index > 1 ? [] : realName).AddRange(typeParametersCache).AddRange(callResult);
				AddRange(ref errors, innerErrors);
				DetectIndirectRecursion(branch, ContainerNStarType.MainType, name, functions, ref errors);
				branch.Extra = functions[^1].ReturnNStarType;
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			else if (C.UserDefinedFunctionExists(ContainerNStarType2, name, parameterTypes, typeParameters,
				out functions, out _, out derived) && functions.Length != 0)
			{
				paramCollection[3] = functions;
				if (derived)
				{
					UnwrapParameters();
					var convertedParameters = Call(branch[index], parameters, out innerErrors, extra);
					result.AddRange(C.FunctionMapping(name, parameterTypes, convertedParameters, typeParametersCache));
				}
				else
				{
					var callResult = CallUser(branch[index], parameters, out innerErrors, extra);
					ContainerUserDefinedFunction(functions, callResult);
				}
				AddRange(ref errors, innerErrors);
				DetectIndirectRecursion(branch, ContainerNStarType.MainType, name, functions, ref errors);
				branch.Extra = functions[^1].ReturnNStarType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			if (!result.EndsWith(')'))
				return (false, DefaultNull);
		}
		else
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos, 10);
			return (false, DefaultNull);
		}
		return (true, result);
		void UnwrapParameters()
		{
			foreach (var x in parameters)
				if (x.StartsWith(AsyncPrefix))
					x.Remove(0, AsyncPrefix.Length);
		}
		void ContainerUserDefinedFunction(UserDefinedMethodOverloads functions, String callResult)
		{
			var realName = functions[^1].RealName;
			if (EscapedKeywords.Contains(realName))
				realName.Insert(0, '@');
			result.AddRange((index > 1 ? [] : realName.Copy()).AddRange(typeParametersCache).AddRange(callResult));
		}
	}

	private void WrapIntoAsync(TreeBranch branch, String result, NStarType ReturnNStarType)
	{
		if (TaskBlockStacks.Contains(ReturnNStarType.MainType))
		{
			if (noAddAsync)
			{
				var prefix = ((String)nameof(AsyncContext)).Add('.').AddRange(nameof(AsyncContext.Run));
				prefix.AddRange("(async () => await ");
				result.Insert(0, prefix).Add(')');
			}
			else
			{
				result.Insert(0, Await);
				containsAsync = true;
			}
		}
		if (branch.Extra is not null && branch.Extra.Equals(ReturnNStarType)
			&& (ReturnNStarType.MainType.Equals(TaskBlockStack) || ReturnNStarType.MainType.Equals(ValueTaskBlockStack))
			&& ReturnNStarType.ExtraTypes.Length == 1 && ReturnNStarType.ExtraTypes[0].Name == "type"
			&& ReturnNStarType.ExtraTypes[0].Extra is NStarType UnderlyingNStarType)
			branch.Extra = UnderlyingNStarType;
	}

	private (bool flowControl, String value) HypernameUserConstructor(TreeBranch branch, ref List<String>? errors,
		object? extra)
	{
		String result = [], requiredProperties = [];
		if (!(extra is List<object> processingWay && processingWay.Length == 5 && processingWay[0] is String elem1
			&& elem1 == nameof(Constructor) && processingWay[1] is NStarType ConstructingNStarType
			&& processingWay[2] is ConstructorProcessingWay
			&& processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
			&& processingWay[4] is List<String> parameters))
			return (false, DefaultNull);
		if (parsedUserConstructors.TryGetValue((ConstructingNStarType, branch), out var parsed))
			return parsed;
		var Restrictions = C.UserDefinedTypes[SplitType(ConstructingNStarType.MainType)].Restrictions ?? [];
		var (TypeIndexes, _) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.MainType.Equals(RecursiveBlockStack));
		TreeBranch? CallRestrictions = null;
		if (ConstructingNStarType.ExtraTypes.Length != 0)
		{
			if (ConstructingNStarType.ExtraTypes.Length != 1)
				throw new InvalidOperationException();
			CallRestrictions = ConstructingNStarType.ExtraTypes[0];
			if (CallRestrictions.Parent is null)
				typeof(TreeBranch).GetProperty("Parent")?.SetValue(CallRestrictions, branch);
			var hs = new ListHashSet<int>(new Chain(ConstructingNStarType.ExtraTypes.Length)).ExceptWith(TypeIndexes);
			ConstructingNStarType.ExtraTypes.Keys.Keys.Filter((x, index) => hs.Contains(index))
				.ForEach(x => ConstructingNStarType.ExtraTypes.Keys.RemoveKey(x));
			ConstructingNStarType.ExtraTypes.FilterInPlace((x, index) => !hs.Contains(index));
		}
		else if (Restrictions.Length != 0)
		{
			var otherPos = branch.Pos;
			GenerateMessage(ref errors, 0x403C, otherPos);
			parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, DefaultNull));
			return (false, DefaultNull);
		}
		result.AddRange("new ").AddRange(Type(ref ConstructingNStarType, branch, ref errors));
		if (CallRestrictions is not null)
		{
			var (flowControl, value) = PolymorphConstructor(branch, ref errors, extra, CallRestrictions);
			if (flowControl)
				requiredProperties.AddRange(value);
			else
			{
				parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, DefaultNull));
				return (false, DefaultNull);
			}
		}
		result.AddRange(ConstructorCall(branch, parameters, out var innerErrors, extra));
		AddRange(ref errors, innerErrors);
		DetectIndirectRecursion(branch, ConstructingNStarType.MainType, constructors, ref errors);
		if (requiredProperties.Length != 0)
			result.AddRange("{ ").AddRange(requiredProperties).AddRange(" }");
		if (innerErrors is not null && innerErrors.Any(x => x.StartsWith("Error")))
		{
			parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, DefaultNull));
			return (false, DefaultNull);
		}
		parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (true, result));
		return (true, result);
	}

	private (bool flowControl, String value) PolymorphConstructor(TreeBranch branch, ref List<String>? errors,
		object? extra, TreeBranch CallRestrictions)
	{
		String result = [];
		if (!(extra is List<object> processingWay && processingWay.Length == 5 && processingWay[0] is String elem1
			&& elem1 == nameof(Constructor) && processingWay[1] is NStarType ConstructingNStarType
			&& processingWay[2] is ConstructorProcessingWay
			&& processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
			&& processingWay[4] is List<String>))
			return (false, DefaultNull);
		var Restrictions = C.UserDefinedTypes[SplitType(ConstructingNStarType.MainType)].Restrictions ?? [];
		var (TypeIndexes, OtherIndexes) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.MainType.Equals(RecursiveBlockStack));
		foreach (var x in TypeIndexes)
		{
			if (!(CallRestrictions.Length > x && CallRestrictions[x].Name == nameof(Hypername)
				&& CallRestrictions[x].Length == 1 && CallRestrictions[x][0].Name == "type"
				&& CallRestrictions[x][0].Extra is NStarType SourceNStarType
				&& Restrictions[x].RestrictionType.ExtraTypes.Length == 1
				&& Restrictions[x].RestrictionType.ExtraTypes[0].Name == "type"
				&& Restrictions[x].RestrictionType.ExtraTypes[0].Extra is NStarType DestinationNStarType))
				continue;
			if (TypesAreCompatible(branch, ref errors, SourceNStarType, DestinationNStarType,
				out var warning, [], out _, out _) && !warning)
				continue;
			String restrictionName = default!;
			if (CheckContainer(branch.Container, stack => C.TempTypes.TryGetValue(stack, out var containerTempTypes)
				&& containerTempTypes.Find(y => Restrictions[x].RestrictionType.ExtraTypes[0].Pos >= y.StartPos
				&& Restrictions[x].RestrictionType.ExtraTypes[0].Pos < y.EndPos)
				is var found && (restrictionName = found.Name) is not null, out _)
				&& TypesAreCompatible(branch, ref errors, SourceNStarType, ReplaceExtraType(DestinationNStarType,
				C.GetNStarReplacementPatterns([restrictionName],
				[new(new(new Block(BlockType.Extra, restrictionName, 1)), NoBranches)],
				[SourceNStarType]).FirstOrDefault()), out warning, [], out _, out _) && !warning)
				continue;
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4094, otherPos, SourceNStarType, DestinationNStarType);
			branch.Parent!.Name = NullString;
			branch.Parent.Elements.Clear();
			branch.Parent.Extra = NullType;
			return (false, DefaultNull);
		}
		var unsetRequiredProperties = constructors[^1].UnsetRequiredProperties;
		if (unsetRequiredProperties.Contains(-1))
		{
			PrepassClass(branch, out var innerErrors,
				ConstructingNStarType.MainType.Skip(ConstructingNStarType.MainType.FindLastIndex(x =>
				x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface)) + 1)
				.ToList(x => x.Name));
			unsetRequiredProperties = constructors[^1].UnsetRequiredProperties;
			if (unsetRequiredProperties.Contains(-1))
			{
				AddRange(ref errors, innerErrors ?? throw new InvalidOperationException());
				return (false, DefaultNull);
			}
		}
		if (CallRestrictions.Name != nameof(List))
		{
			var RestrictionType = Restrictions[0].RestrictionType;
			if (unsetRequiredProperties.Length == 0)
			{
				var properties = C.GetAllProperties(ConstructingNStarType.MainType);
				String propertyName = default!;
				UserDefinedProperty? property = null;
				if (!properties.Any(x => C.PropertyExists(ConstructingNStarType, propertyName = x.Key, false, out property)
					&& property.HasValue && (property.Value.Attributes & (PropertyAttributes.Private
					| PropertyAttributes.Protected | PropertyAttributes.Internal | PropertyAttributes.NoSet
				| PropertyAttributes.PrivateSet | PropertyAttributes.ProtectedSet)) == 0)
					|| !property.HasValue)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x403F, otherPos);
					return (false, DefaultNull);
				}
				CallRestrictions.Extra ??= property.Value.NStarType;
				var parsedRestriction = ParseAction(CallRestrictions.Name)(CallRestrictions, out var innerErrors);
				AddRange(ref errors, innerErrors);
				var fullName = String.Join(".",
					ConstructingNStarType.MainType.Convert(x => x.Name).Append(propertyName).ToArray());
				if ((property.Value.Attributes & PropertyAttributes.Private) != 0
					^ (property.Value.Attributes & PropertyAttributes.Protected) != 0
					&& !CallRestrictions.Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if ((property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if ((property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Value.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !CallRestrictions.Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if ((property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if (CallRestrictions.Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				else if (!TypesAreCompatible(branch, ref errors, NStarType, property.Value.NStarType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, RestrictionType, Construction);
					return (false, DefaultNull);
				}
				result.AddRange(propertyName).AddRange(" = ").AddRange(parsedRestriction);
			}
			else if (TypeIndexes.Equals([0]))
			{
			}
			else if (OtherIndexes.Equals([0]))
			{
				CallRestrictions.Extra ??= Restrictions[0].RestrictionType;
				var parsedRestriction = ParseAction(CallRestrictions.Name)(CallRestrictions, out var innerErrors);
				AddRange(ref errors, innerErrors);
				if (CallRestrictions.Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				if (!TypesAreCompatible(branch, ref errors, NStarType, RestrictionType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, RestrictionType, Construction);
					return (false, DefaultNull);
				}
				result.AddRange(Restrictions[0].Name).AddRange(" = ").AddRange(parsedRestriction);
			}
			else
			{
				var otherPos = CallRestrictions.EndPos;
				GenerateMessage(ref errors, 0x403D, otherPos, Restrictions[1].Name);
				return (false, DefaultNull);
			}
		}
		else
		{
			var unsetRequiredPropertiesCount = unsetRequiredProperties.Length == 0 ? 0 : unsetRequiredProperties.Max() + 1;
			for (var counter = 0; counter < OtherIndexes.Length; counter++)
			{
				var RestrictionType = Restrictions[0].RestrictionType;
				var index = OtherIndexes[counter];
				if (index >= CallRestrictions.Length && index < unsetRequiredPropertiesCount)
				{
					var otherPos = CallRestrictions.EndPos;
					GenerateMessage(ref errors, 0x403D, otherPos, Restrictions[index].Name);
					return (false, DefaultNull);
				}
				else if (index >= CallRestrictions.Length)
					break;
				CallRestrictions[index].Extra ??= Restrictions[index].RestrictionType;
				var parsedRestriction = ParseAction(CallRestrictions[index].Name)(CallRestrictions[index], out var innerErrors);
				AddRange(ref errors, innerErrors);
				if (counter != 0)
					result.AddRange(", ");
				if (CallRestrictions[index].Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				if (!TypesAreCompatible(branch, ref errors, NStarType, Restrictions[index].RestrictionType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, RestrictionType, Construction);
					return (false, DefaultNull);
				}
				result.AddRange(Restrictions[index].Name).AddRange(" = ").AddRange(parsedRestriction);
			}
			var properties = CallRestrictions.Length == Restrictions.Length
				? [] : C.GetAllProperties(ConstructingNStarType.MainType);
			properties.FilterInPlace(property => (property.Value.Attributes & (PropertyAttributes.Private
				| PropertyAttributes.Protected | PropertyAttributes.Internal | PropertyAttributes.NoSet
				| PropertyAttributes.PrivateSet | PropertyAttributes.ProtectedSet)) == 0);
			if (properties.Length < CallRestrictions.Length - Restrictions.Length)
			{
				var otherPos = CallRestrictions[Restrictions.Length + properties.Length].Pos;
				GenerateMessage(ref errors, 0x403F, otherPos);
				return (false, DefaultNull);
			}
			for (var index = Restrictions.Length; index < CallRestrictions.Length; index++)
			{
				var i = index - Restrictions.Length;
				var (propertyName, property) = properties[i];
				CallRestrictions[index].Extra ??= property.NStarType;
				var parsedRestriction = ParseAction(CallRestrictions[index].Name)(CallRestrictions[index], out var innerErrors);
				AddRange(ref errors, innerErrors);
				var fullName = String.Join(".",
					ConstructingNStarType.MainType.Convert(x => x.Name).Append(propertyName).ToArray());
				if ((property.Attributes & PropertyAttributes.Private) != 0
					^ (property.Attributes & PropertyAttributes.Protected) != 0
					&& !CallRestrictions[index].Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if ((property.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !CallRestrictions[index].Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if ((property.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					branch.Parent!.Name = NullString;
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, DefaultNull);
				}
				else if (CallRestrictions[index].Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				else if (!TypesAreCompatible(branch, ref errors, NStarType, property.NStarType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, property.NStarType, Construction);
					return (false, DefaultNull);
				}
				if (index != 0)
					result.AddRange(", ");
				result.AddRange(propertyName).AddRange(" = ").AddRange(parsedRestriction);
			}
		}
		return (true, result);
	}

	private String Indexes(TreeBranch branch, ref List<String>? errors, object? extra, int index)
	{
		String result = [];
		if (branch[index - 1].Extra is not NStarType CollectionNStarType)
			return DefaultNull;
		if (!(extra is List<object> paramCollection && paramCollection.Length == 3 && paramCollection[0] is String elem1
			&& elem1.AsSpan() is VariableWay or nameof(Property) or nameof(Expr)
			&& paramCollection[1] is NStarType CollectionNStarType2
			&& CollectionNStarType.Equals(CollectionNStarType2)
			&& paramCollection[2] is List<String> indexValues))
			return DefaultNull;
		var rangeDepth = 0;
		bool range = false, oldRange = false;
		for (var i = 0; i < indexValues.Length; i++)
		{
			var x = indexValues[i];
			if (oldRange)
			{
				var randomName = RandomVarName();
				result.AddRange(".Convert(").AddRange(randomName).AddRange(" => ").AddRange(randomName);
				rangeDepth++;
			}
			int repeatsCount;
			if (TypeEqualsToPrimitive(CollectionNStarType, TupleName, false)
				&& !((CollectionNStarType.ExtraTypes.AllEqual() ? CollectionNStarType.ExtraTypes.Length
				: CollectionNStarType.ExtraTypes.Length == 2
				&& int.TryParse(ParseAction(CollectionNStarType.ExtraTypes[1].Name)(CollectionNStarType.ExtraTypes[1], out _)
				.ToString(), out var n)
				? n : -1) is var tupleLength && tupleLength >= 0
				&& C.InlineArrays.TryGetValue(CollectionNStarType.ExtraTypes[0].Name == "type"
				&& BoolType.Equals(CollectionNStarType.ExtraTypes[0].Extra) ? ~tupleLength : tupleLength, out _)))
			{
				if (!int.TryParse(x.ToString(), out repeatsCount))
				{
					var otherPos = branch[index].Pos;
					GenerateMessage(ref errors, 0x400B, otherPos);
					return DefaultNull;
				}
				if (repeatsCount <= 0)
				{
					var otherPos = branch[index].Pos;
					GenerateMessage(ref errors, 0x4016, otherPos);
					return DefaultNull;
				}
				result.AddRange(".Item").AddRange(repeatsCount.ToString());
				var minLength = Min(repeatsCount, CollectionNStarType.ExtraTypes.Length
					- (CollectionNStarType.ExtraTypes[1].Name == "type" ? 0 : 1));
				CollectionNStarType = CollectionNStarType.ExtraTypes[minLength - 1].Name == "type"
					&& CollectionNStarType.ExtraTypes[minLength - 1].Extra is NStarType InnerNStarType
					? InnerNStarType : NullType;
				oldRange = false;
				continue;
			}
			if (branch[index][i].Extra is not NStarType IndexNStarType)
			{
				if (IsTrivialIndexType(CollectionNStarType, ref errors, out _))
					branch[index][i].Extra = IndexType;
				else if ((CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Class, nameof(Dictionary<,>), 1)]))
					|| CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
					new(BlockType.Namespace, CollectionsName, 1), new(BlockType.Class, nameof(Dictionary<,>), 1)])))
					&& CollectionNStarType.ExtraTypes.Length == 2
					&& CollectionNStarType.ExtraTypes[0].Extra is NStarType KeyNStarType)
					branch[index][i].Extra = KeyNStarType;
				else if (CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
					new(BlockType.Namespace, UnsafeName, 1), new(BlockType.Class, nameof(FuncDictionary<,>), 1)]))
					&& CollectionNStarType.ExtraTypes.Length == 2
					&& CollectionNStarType.ExtraTypes[0].Extra is NStarType FuncKeyNStarType)
					branch[index][i].Extra = FuncKeyNStarType;
				x = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
				AddRange(ref errors, innerErrors);
				if (branch[index][i].Extra is not NStarType NewIndexNStarType)
					throw new InvalidOperationException();
				IndexNStarType = NewIndexNStarType;
			}
			var trivialIndex = IsTrivialIndexType(CollectionNStarType, ref errors, out var boolTuple)
				&& !IndexNStarType.Equals(IndexType) && !(range = IndexNStarType.Equals(RangeType));
			if (trivialIndex && int.TryParse(x.ToString(), out repeatsCount) && repeatsCount <= 0)
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4016, otherPos);
				return DefaultNull;
			}
			else if (trivialIndex
				&& (!TypesAreCompatible(branch, ref errors, IndexNStarType, IndexType,
				out var warning, x, out var destExpr, out var extraMessage) || warning || destExpr is null))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, IndexNStarType, IndexType, "getting the index");
				return DefaultNull;
			}
			else if ((CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Class, nameof(Dictionary<,>), 1)]))
				|| CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
				new(BlockType.Namespace, CollectionsName, 1), new(BlockType.Class, nameof(Dictionary<,>), 1)])))
				&& CollectionNStarType.ExtraTypes.Length == 2
				&& CollectionNStarType.ExtraTypes[0].Extra is NStarType KeyNStarType
				&& (!TypesAreCompatible(branch, ref errors, IndexNStarType, KeyNStarType,
				out warning, x, out destExpr, out extraMessage) || warning || destExpr is null))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, IndexNStarType, KeyNStarType, "getting the key");
				return DefaultNull;
			}
			else if (CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
				new(BlockType.Namespace, UnsafeName, 1), new(BlockType.Class, nameof(FuncDictionary<,>), 1)]))
				&& CollectionNStarType.ExtraTypes.Length == 2
				&& CollectionNStarType.ExtraTypes[0].Extra is NStarType FuncKeyNStarType
				&& (!TypesAreCompatible(branch, ref errors, IndexNStarType, FuncKeyNStarType,
				out warning, x, out destExpr, out extraMessage) || warning || destExpr is null))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, IndexNStarType, FuncKeyNStarType,
					"getting the key");
				return DefaultNull;
			}
			else if (CollectionNStarType.MainType.Equals(FuncBlockStack)
				&& CollectionNStarType.ExtraTypes.Length == 2
				&& CollectionNStarType.ExtraTypes[1].Extra is NStarType FuncParamNStarType
				&& (!TypesAreCompatible(branch, ref errors, IndexNStarType, FuncParamNStarType,
				out warning, x, out destExpr, out extraMessage) || warning || destExpr is null))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, IndexNStarType, FuncParamNStarType,
					"getting the generic parameter");
				return DefaultNull;
			}
			if (trivialIndex)
				result.AddRange("[(");
			else if (CollectionNStarType.MainType.Equals(FuncBlockStack))
				result.Add('(');
			else
				result.Add('[');
			result.AddRange(x);
			if (trivialIndex && boolTuple)
				result.AddRange(") - 1, false]");
			else if (trivialIndex)
				result.AddRange(") - 1]");
			else if (boolTuple)
				result.AddRange(", false]");
			else if (CollectionNStarType.MainType.Equals(FuncBlockStack))
				result.Add(')');
			else
				result.Add(']');
			if (!range)
			{
				if ((CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Class, nameof(Dictionary<,>), 1)]))
					|| CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
					new(BlockType.Namespace, CollectionsName, 1), new(BlockType.Class, nameof(Dictionary<,>), 1)])))
					&& CollectionNStarType.ExtraTypes.Length == 2)
					CollectionNStarType = (NStarType)CollectionNStarType.ExtraTypes[1].Extra!;
				else if (CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, SystemName, 1),
					new(BlockType.Namespace, UnsafeName, 1), new(BlockType.Class, nameof(FuncDictionary<,>), 1)]))
					&& CollectionNStarType.ExtraTypes.Length == 2)
					CollectionNStarType = (NStarType)CollectionNStarType.ExtraTypes[1].Extra!;
				else if (CollectionNStarType.MainType.Equals(FuncBlockStack))
					CollectionNStarType = (NStarType)CollectionNStarType.ExtraTypes[0].Extra!;
				else
					CollectionNStarType = GetSubtype(C, CollectionNStarType);
			}
			oldRange = range;
		}
		result.AddRange(new(')', rangeDepth));
		branch.Extra = branch[index].Extra = CollectionNStarType;
		paramCollection[1] = CollectionNStarType;
		return result;
		bool IsTrivialIndexType(NStarType CollectionNStarType, ref List<String>? errors, out bool boolTuple)
		{
			boolTuple = false;
			if (TypeEqualsToPrimitive(CollectionNStarType, "list", false))
				return true;
			if (CollectionNStarType.ExtraTypes.Length == 1 && TypesAreCompatible(branch, ref errors, CollectionNStarType,
				new(IEnumerableBlockStack, CollectionNStarType.ExtraTypes),
				out var warning, null, out _, out _) && !warning)
				return true;
			if (TypeEqualsToPrimitive(CollectionNStarType, TupleName, false)
				&& (CollectionNStarType.ExtraTypes.AllEqual() || CollectionNStarType.ExtraTypes.Length == 2
				&& CollectionNStarType.ExtraTypes[1].Length == 0
				&& int.TryParse(CollectionNStarType.ExtraTypes[1].Name.AsSpan(), out _)))
			{
				boolTuple = CollectionNStarType.ExtraTypes[0].Name == "type"
					&& BoolType.Equals(CollectionNStarType.ExtraTypes[0].Extra);
				return true;
			}
			if (CollectionNStarType.ExtraTypes.Length != 2 || CollectionNStarType.ExtraTypes[0].Name != "type"
				|| CollectionNStarType.ExtraTypes[0].Extra is not NStarType FirstNStarType
				|| CollectionNStarType.ExtraTypes[1].Name != "type"
				|| CollectionNStarType.ExtraTypes[1].Extra is not NStarType SecondNStarType)
				return false;
			if (!CollectionNStarType.MainType.Equals(FirstNStarType.MainType))
				return false;
			if (SecondNStarType.ExtraTypes.Length != 1 || SecondNStarType.ExtraTypes[0].Name != "type"
				|| SecondNStarType.ExtraTypes[0].Extra is not NStarType SecondInnerNStarType)
				return false;
			if (!FirstNStarType.Equals(SecondInnerNStarType))
				return false;
			return TypesAreCompatible(branch, ref errors, CollectionNStarType,
				new(BaseIndexableBlockStack, CollectionNStarType.ExtraTypes),
				out warning, null, out _, out _) && !warning;
		}
	}

	private bool? HypernameMethod(TreeBranch branch, String name, List<String> parameters, ref object? refExtra,
		ref List<String>? errors, int prevIndex, NStarType ContainerNStarType, UserDefinedMethodOverloads functions)
	{
		NStarType NStarType;
		foreach (var function in functions)
		{
			if ((function.Attributes & FunctionAttributes.Private) != 0
				^ (function.Attributes & FunctionAttributes.Protected) != 0
				&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				continue;
			else if ((function.Attributes & FunctionAttributes.Static) == 0
				&& !(branch.Length >= 2 && branch[1].Name == nameof(Call)))
				continue;
			NStarType = function.ReturnNStarType;
			List<object> paramCollection = [((String)(nameof(Function) + ' ')).AddRange(name), MethodProcessingWay.Method,
				ContainerNStarType, functions, parameters];
			if ((function!.Attributes & FunctionAttributes.Static) != 0)
				paramCollection.Add(Static);
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			PropagateParameterTypes(branch, ref errors, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params,
				(function.Attributes & FunctionAttributes.Extent) != 0 ? [.. parameterTypes.Skip(1)] : parameterTypes);
			return null;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos,
			String.Join(".", ContainerNStarType.MainType.ToList().Convert(x => x.Name).Append(name)));
		branch.Parent![prevIndex] = new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
		return false;
	}

	private bool? HypernameExtendedMethod(TreeBranch branch, String name, List<String> parameters,
		ref object? refExtra, ref List<String>? errors, int prevIndex, NStarType ContainerNStarType,
		UserDefinedMethodOverloads functions, MethodProcessingWay category)
	{
		NStarType NStarType;
		foreach (var function in functions)
		{
			if ((function.Attributes & FunctionAttributes.Private) != 0
				^ (function.Attributes & FunctionAttributes.Protected) != 0
				&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				continue;
			NStarType = function!.ReturnNStarType;
			List<object> paramCollection = [((String)(nameof(Function) + ' ')).AddRange(name), category,
				ContainerNStarType, functions, parameters];
			if ((function.Attributes & FunctionAttributes.Static) != 0)
				paramCollection.Add(Static);
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			PropagateParameterTypes(branch, ref errors, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params,
				(function.Attributes & FunctionAttributes.Extent) != 0 ? [.. parameterTypes.Skip(1)] : parameterTypes);
			return null;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos,
			String.Join(".", ContainerNStarType.MainType.ToList().Convert(x => x.Name).Append(name)));
		branch.Parent![prevIndex] = new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
		return false;
	}

	private void HypernamePublicExtendedMethod(TreeBranch branch, String name, List<String> parameters,
		ref object? refExtra, ref List<String>? errors, int prevIndex, UserDefinedMethodOverloads functions,
		MethodProcessingWay category)
	{
		foreach (var function in functions)
		{
			var NStarType = function.ReturnNStarType;
			List<object> paramCollection = [((String)(nameof(Function) + ' ')).AddRange(name), category,
				NullType, functions, parameters];
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			if (!PropagateParameterTypes(branch, ref errors, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Params, parameterTypes))
				continue;
			return;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos, name);
		branch.Parent![prevIndex] = new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
	}

	private static void HypernameAddExtra(TreeBranch branch, String realName, NStarType extra, List<object> paramCollection,
		ref object? refExtra, BranchCollection extraTypes)
	{
		if (branch.Length >= 2 && branch[1].Name == nameof(Call)
			|| branch.Length >= 3 && branch[1].Name == nameof(Indexes) && branch[2].Name == nameof(Call))
		{
			branch[0].Name = branch[0].Name.Copy().AddRange(" (function)");
			branch[0].Extra = extra;
			refExtra = paramCollection;
		}
		else
		{
			branch[0].Name.Replace(realName).AddRange(DelegateSuffix);
			branch[0].Extra = new NStarType(FuncBlockStack, extraTypes);
			branch[0].Insert(0, new TreeBranch("data", branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = paramCollection
			});
		}
	}

	private bool PropagateParameterTypes(TreeBranch branch, ref List<String>? errors, String name, bool @params,
		BranchCollection parameterTypes)
	{
		if (branch.Length < 2)
			return true;
		var index = branch.Length >= 3 && branch[1].Name == nameof(Indexes) ? 2 : 1;
		for (var i = 0; i < branch[index].Length; i++)
		{
			var x = branch[index][i];
			if (!@params && i >= parameterTypes.Length)
				return false;
			if (parameterTypes[@params ? ^1 : i].Extra is NStarType DestinationType
				&& (x.Extra is not NStarType SourceType
				|| i >= parameterTypes.Length - 1 && @params && GetSubtype(C, SourceType).Equals(DestinationType)
				|| TypesAreCompatible(branch, ref errors, SourceType, DestinationType, out var warning, [], out _, out _)
				&& !warning && !(name.StartsWith(nameof(branch.Add)) && GetListType(SourceType).Equals(DestinationType))
				&& !(DestinationType.MainType.TryPeek(out var block) && block.Name.Contains("Number"))))
			{
				x.Extra = DestinationType;
				ParseAction(x.Name)(x, out _);
			}
		}
		return true;
	}

	private List<String>? Call(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		List<String> result = [];
		errors = null;
		for (var i = 0; i < branch.Length; i++)
		{
			var innerResult = parameters[i];
			if (innerResult.Length != 0)
				result.Add(innerResult);
		}
		if (!CallCheck(branch, ref errors, parameters, extra))
			return null;
		if (branch.Length != 0 && branch[0].Length == 1 && branch[0][0].Name.EndsWith(DelegateSuffix))
			return branch[0][0].Name[..^DelegateSuffix.Length];
		return result;
	}

	private String CallUser(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		var callResult = Call(branch, parameters, out errors, extra);
		if (callResult is null)
			return [];
		var joined = String.Join(", ", callResult);
		return joined.Insert(0, '(').Add(')');
	}

	private bool CallCheck(TreeBranch branch, ref List<String>? errors, List<String> parameters, object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<NStarType> CallParameterNStarTypes = [];
		List<List<NStarType>> callParameterNStarTypes;
		List<String>? innerErrors = null;
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is NStarType type)
				CallParameterNStarTypes.Add(type);
			else
			{
				GenerateMessage(ref errors, 0x4000, otherPos, 11);
				return false;
			}
		if (!(extra is List<object> paramCollection && paramCollection.Length >= 3 && paramCollection.Length <= 6
			&& paramCollection[0] is String elem1 && elem1.StartsWith(nameof(Function) + ' ')
			&& paramCollection[2] is NStarType ContainerNStarType
			&& paramCollection[3] is UserDefinedMethodOverloads functions && functions.Length != 0))
		{
			GenerateMessage(ref errors, 0x4000, otherPos, 9);
			return false;
		}
		elem1 = elem1[(nameof(Function) + ' ').Length..];
		if (parsingFunctions.Length != 0 && (parsingFunctions[^1].Value.Attributes & FunctionAttributes.IO) == 0
			&& (elem1.Contains("And") || elem1.Contains("Choose") || elem1.Contains("Clear")
			|| elem1.Contains("Copy") && elem1.Length != "Copy".Length
			|| elem1.Contains(nameof(Dictionary<,>)) || elem1.Contains("Except") || elem1.Contains("HashSet")
			|| elem1.Contains("InPlace") || elem1.Contains("Insert") || elem1.Contains("Intersect")
			|| elem1.Contains("Not") || elem1.Contains("Or")
			|| elem1.Contains("Random") || elem1.Contains("Remove") || elem1.Contains("Replace")
			|| elem1.Contains("Resize") || elem1.Contains("Reverse")
			|| elem1.Contains("Set") || elem1.Contains("Shuffle") || elem1.Contains("Sort")
			|| elem1.Contains("ToLower") || elem1.Contains("ToUpper") || elem1.Contains("Trim")
			|| elem1.Contains("Union") || elem1.Contains("Xor")))
		{
			GenerateMessage(ref errors, 0x901C, otherPos, elem1);
			return false;
		}
		var max = functions.Any(x => ProperParameters(x).Length != 0 && (ProperParameters(x)[^1].Attributes
			& ParameterAttributes.Params) == ParameterAttributes.Params)
			? int.MaxValue : functions.Max(x => ProperParameters(x).Length);
		var min = functions.Min(x => ProperParameters(x).Count(y => (y.Attributes & ParameterAttributes.Optional) == 0));
		if (CallParameterNStarTypes.Length > max || CallParameterNStarTypes.Length < min)
		{
			GenerateMessage(ref errors, 0x4022, otherPos, elem1, max, min);
			return false;
		}
		functions.FilterInPlace(x => ProperParameters(x).Length != 0 && (ProperParameters(x)[^1].Attributes
			& ParameterAttributes.Params) != 0 || ProperParameters(x).Length >= CallParameterNStarTypes.Length)
			.FilterInPlace(x => ProperParameters(x).Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			<= CallParameterNStarTypes.Length);
		var warnings = new bool[CallParameterNStarTypes.Length + 1];
		var FunctionParameterNStarTypes = new NStarType[CallParameterNStarTypes.Length + 1];
		var adaptedInnerResults = new String[CallParameterNStarTypes.Length + 1];
		var extraMessages = new String[CallParameterNStarTypes.Length + 1];
		int callIndex = 0, functionIndex = 0;
		if (functions.Length == 1)
		{
			var (_, _, ReturnNStarType, Attributes, Parameters, _) = functions[0];
			var extentOffset = (Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0;
			callParameterNStarTypes = [extentOffset == 1
				? [.. CallParameterNStarTypes.Prepend(ContainerNStarType)] : CallParameterNStarTypes];
			if (extentOffset == 1)
				parameters = [default!, .. parameters];
			if (Parameters.Length == 0 && parameters.Length != 0)
			{
				GenerateMessage(ref errors, 0x4023, otherPos, elem1);
				return false;
			}
			else if (Parameters.Length == 0)
				return true;
			else if (Parameters.Any((x, i) => (callIndex = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Ref && !parameters[callIndex].StartsWith("ref ")))
			{
				otherPos = branch[callIndex].Pos;
				GenerateMessage(ref errors, 0x9013, otherPos, "ref");
				return true;
			}
			else if (Parameters.Any((x, i) => (callIndex = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Out && !parameters[callIndex].StartsWith("out ")))
			{
				otherPos = branch[callIndex].Pos;
				GenerateMessage(ref errors, 0x9013, otherPos, "out");
				return true;
			}
#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable S1121
			else if (!(callParameterNStarTypes[0].Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& callParameterNStarTypes[0].Combine(Parameters).All((x, i) =>
				TypesAreCompatible(branch, ref innerErrors, x.Item1, FunctionParameterNStarTypes[i]
				= i == callParameterNStarTypes[0].Length - 1
				&& (Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndex == Parameters.Length - 1 ? GetListType(x.Item2.Type) : x.Item2.Type,
				out warnings[functionIndex = callIndex = i], parameters[i],
				out adaptedInnerResults[i]!, out extraMessages[i]!) && (i < extentOffset || adaptedInnerResults[i] is not null))
				&& callParameterNStarTypes[0].Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndex == Parameters.Length - 1
				&& callParameterNStarTypes[0].Skip(functionIndex = callIndex)
				.All((x, i) => TypesAreCompatible(branch, ref innerErrors, x,
				Parameters[^1].Type, out warnings[callIndex = functionIndex + i],
				parameters[callIndex], out adaptedInnerResults[callIndex]!, out extraMessages[callIndex]!)
				&& (callIndex < extentOffset || adaptedInnerResults[callIndex] is not null))))
			{
				var shiftedCallIndex = callIndex - ((functions[0].Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0);
				otherPos = shiftedCallIndex >= 0 ? branch[shiftedCallIndex].Pos : branch.Parent![0].Pos;
				GenerateMessage(ref errors, 0x4026, otherPos, extraMessages[callIndex],
					callParameterNStarTypes[0][callIndex], FunctionParameterNStarTypes[functionIndex],
					FunctionParameterNStarTypes[callIndex]);
				return false;
			}
			else if (callIndex < warnings.Length && warnings[callIndex])
			{
				otherPos = branch[callIndex].Pos;
				GenerateMessage(ref errors, 0x4027, otherPos, extraMessages[callIndex],
					callParameterNStarTypes[0][callIndex], FunctionParameterNStarTypes[functionIndex]);
				return false;
			}
			AddRange(ref errors, innerErrors);
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[i]
				? x.Replace(adaptedInnerResults[i] ?? DefaultNull) : "");
			branch.Extra = ReturnNStarType;
			return true;
		}
		var prependedNStarTypes = CallParameterNStarTypes.Prepend(ContainerNStarType).ToList();
		callParameterNStarTypes = functions
			.ToList(x => (x.Attributes & FunctionAttributes.Extent) != 0 ? prependedNStarTypes : CallParameterNStarTypes);
		ListHashSet<int> IncompatibleOverloads = [];
		ListHashSet<(int OverloadIndex, int ParameterIndex)> BadlyCompatibleOverloads = [];
		ListHashSet<(int OverloadIndex, int ParameterIndex)> ConversionOverloads = [];
		var callIndexes = new int[functions.Length];
		var functionIndexes = new int[functions.Length];
		for (var j = 0; j < functions.Length; j++)
		{
			var (_, _, _, Attributes, Parameters, _) = functions[j];
			var extentOffset = (Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0;
			var fullParameters = extentOffset == 1 ? [default!, .. parameters] : parameters;
			if (Parameters.Length == 0 && parameters.Length != 0)
				continue;
			else if (!(callParameterNStarTypes[j].Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& callParameterNStarTypes[j].Combine(Parameters).All((x, i) =>
				TypesAreCompatible(branch, ref innerErrors, x.Item1, FunctionParameterNStarTypes[i] = x.Item2.Type,
				out warnings[functionIndexes[j] = callIndexes[j] = i], fullParameters[i],
				out adaptedInnerResults[i]!, out extraMessages[i]!) && (i < extentOffset || adaptedInnerResults[i] is not null))
				&& callParameterNStarTypes[j].Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndexes[j] == Parameters.Length - 1
				&& callParameterNStarTypes[j].Skip(functionIndexes[j] = callIndexes[j])
				.All((x, i) => TypesAreCompatible(branch, ref innerErrors, x,
				Parameters[^1].Type, out warnings[callIndexes[j] = functionIndexes[j] + i],
				fullParameters[callIndexes[j]], out adaptedInnerResults[callIndexes[j]]!, out extraMessages[callIndexes[j]]!)
				&& (callIndexes[j] < extentOffset || adaptedInnerResults[callIndexes[j]] is not null))))
				IncompatibleOverloads.Add(j);
			else if (warnings.Any(x => x))
				_ = warnings.ToList((x, i) => x ? BadlyCompatibleOverloads.Add((j, i)) : default);
			adaptedInnerResults.Combine(fullParameters)
				.ToList((x, i) => x.Item1 != x.Item2 ? ConversionOverloads.Add((j, i)) : default);
		}
#pragma warning restore S1121
#pragma warning restore IDE0079 // Удалить ненужное подавление
		AddRange(ref errors, innerErrors);
		var thresholdIndexes = callIndexes.IndexesOfMax();
		var incompatibleLength = IncompatibleOverloads.Length;
		if (incompatibleLength == functions.Length)
		{
			if (IncompatibleOverloads
				.All(j => (functions[j].Attributes & FunctionAttributes.Extent) != 0 && callIndexes[thresholdIndexes[0]] == 0))
			{
				otherPos = branch.Parent![0].Pos;
				GenerateMessage(ref errors, 0x4028, otherPos,
					ContainerNStarType, String.Join("\", \"", IncompatibleOverloads.Convert(j =>
					functions[j].Parameters[0].Type.ToString()).ToHashSet()),
					IncompatibleOverloads.Length, functions[IncompatibleOverloads[0]]
					.Parameters[functionIndexes[thresholdIndexes[0]]].Type);
			}
			else
			{
				var wrongParamIndex = callIndexes[thresholdIndexes[0]]
					- ((functions[0].Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0);
				otherPos = branch[wrongParamIndex].Pos;
				GenerateMessage(ref errors, 0x4028, otherPos,
					CallParameterNStarTypes[wrongParamIndex],
					String.Join("\", \"", IncompatibleOverloads.Convert(j =>
					functions[j].Parameters[functionIndexes[thresholdIndexes[j]]].Type.ToString()).ToHashSet()),
					IncompatibleOverloads.Length, functions[IncompatibleOverloads[0]]
					.Parameters[functionIndexes[thresholdIndexes[0]]].Type);
			}
			return false;
		}
		BadlyCompatibleOverloads.FilterInPlace(x => !IncompatibleOverloads.Contains(x.OverloadIndex));
		var bcoGroups = BadlyCompatibleOverloads.Group(x => x.ParameterIndex);
		ConversionOverloads.FilterInPlace(x => !IncompatibleOverloads.Contains(x.OverloadIndex)
			|| BadlyCompatibleOverloads.Exists(y => y.OverloadIndex == x.OverloadIndex));
		var cvoGroups = ConversionOverloads.Group(x => x.ParameterIndex);
		var WellCompatibleOverloads = new Chain(functions.Length).ToHashSet()
			.ExceptWith(IncompatibleOverloads).ExceptWith(bcoGroups.ConvertAndJoin(x => x).Convert(x => x.OverloadIndex))
			.ExceptWith(cvoGroups.ConvertAndJoin(x => x).Convert(x => x.OverloadIndex));
		if (WellCompatibleOverloads.Length != 0)
		{
			_ = parameters.ToList((x, i) =>
			{
				var extentOffset = (functions[WellCompatibleOverloads[^1]].Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0;
				return x != CreateVar(adaptedInnerResults[extentOffset + i], out var adaptedInnerResult)
					? x.Replace(adaptedInnerResult ?? DefaultNull) : "";
			});
			branch.Extra = functions[WellCompatibleOverloads[^1]].ReturnNStarType;
			functions.FilterInPlace((x, index) => WellCompatibleOverloads.Contains(index));
			return true;
		}
		else if (ConversionOverloads.Length != 0)
		{
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[i]
				? x.Replace(adaptedInnerResults[i] ?? DefaultNull) : "");
			branch.Extra = functions[ConversionOverloads[^1].OverloadIndex].ReturnNStarType;
			functions.FilterInPlace((x, index) => ConversionOverloads.Any(x => x.OverloadIndex == index));
			return true;
		}
		if (bcoGroups.Length != 0)
		{
			otherPos = branch[bcoGroups[0].Key].Pos;
			GenerateMessage(ref errors, 0x4029, otherPos, CallParameterNStarTypes[bcoGroups[0].Key],
				String.Join("\", \"", bcoGroups[0].Convert(item => functions[item.OverloadIndex].Parameters
				.GetSlice((functions[item.OverloadIndex].Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0)
				.Wrap(x => x[callIndex = Min(bcoGroups[0].Key, x.Length - 1)].Type.ToString())).ToHashSet()));
			return false;
		}
		_ = parameters.ToList((x, i) => x != adaptedInnerResults[BadlyCompatibleOverloads[^1].OverloadIndex][i]
			? x.Replace(adaptedInnerResults[BadlyCompatibleOverloads[^1].OverloadIndex] ?? DefaultNull) : "");
		branch.Extra = NullType;
		return true;
	}

	private String ConstructorCall(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		String result = "(";
		errors = null;
		if (!ConstructorCallCheck(branch, ref errors, parameters, extra))
			return [];
		return result.AddRange(String.Join(", ", parameters)).Add(')');
	}

	private bool ConstructorCallCheck(TreeBranch branch, ref List<String>? errors, List<String> parameters,
		object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<NStarType> CallParameterNStarTypes = [];
		List<String>? innerErrors = null;
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is NStarType type)
				CallParameterNStarTypes.Add(type);
			else
			{
				GenerateMessage(ref errors, 0x4000, otherPos, 8);
				return false;
			}
		if (!(extra is List<object> paramCollection
			&& paramCollection.Length >= 4 && paramCollection.Length <= 5 && paramCollection[0] is String elem1
			&& elem1 == nameof(Constructor) && paramCollection[1] is NStarType ConstructingNStarType
			&& paramCollection[2] is ConstructorProcessingWay && paramCollection[3] is ConstructorOverloads constructors
			&& constructors.Length != 0))
		{
			GenerateMessage(ref errors, 0x4000, otherPos, 7);
			return false;
		}
		var max = constructors.Any(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
		& ParameterAttributes.Params) != 0) ? int.MaxValue : constructors.Max(x => x.Parameters.Length);
		var min = constructors.Min(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0));
		if (CallParameterNStarTypes.Length > max || CallParameterNStarTypes.Length < min)
		{
			GenerateMessage(ref errors, 0x4060, otherPos, ConstructingNStarType, max, min);
			return false;
		}
		constructors.FilterInPlace(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
			& ParameterAttributes.Params) != 0 || x.Parameters.Length >= CallParameterNStarTypes.Length)
			.FilterInPlace(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			<= CallParameterNStarTypes.Length);
		var warnings = new bool[CallParameterNStarTypes.Length];
		var FunctionParameterNStarTypes = new NStarType[CallParameterNStarTypes.Length];
		var adaptedInnerResults = RedStarLinq.FillArray(constructors.Length, _ => new String[CallParameterNStarTypes.Length]);
		var extraMessages = new String[CallParameterNStarTypes.Length];
		int callIndex = 0, constructorIndex = 0;
		if (constructors.Length == 1)
		{
			var (_, Parameters, _, _) = constructors[0];
			if (Parameters.Length == 0 && parameters.Length != 0)
			{
				GenerateMessage(ref errors, 0x4034, otherPos, ConstructingNStarType);
				return false;
			}
			else if (Parameters.Length == 0)
				return true;
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters)
				.All((x, i) => TypesAreCompatible(branch, ref innerErrors, x.Item1,
				FunctionParameterNStarTypes[i] = x.Item2.Type, out warnings[constructorIndex = callIndex = i],
				parameters[i], out adaptedInnerResults[0][i]!, out extraMessages[i]!) && adaptedInnerResults[0][i] is not null))
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndex == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(constructorIndex = callIndex)
				.All((x, i) => TypesAreCompatible(branch, ref innerErrors, x, Parameters[^1].Type,
				out warnings[callIndex = constructorIndex + i],
				parameters[callIndex], out adaptedInnerResults[0][callIndex]!, out extraMessages[callIndex]!)
				&& adaptedInnerResults[0][callIndex] is not null)))
			{
				otherPos = branch[callIndex].Pos;
				GenerateMessage(ref errors, 0x4061, otherPos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[constructorIndex],
					FunctionParameterNStarTypes[callIndex]);
				return false;
			}
			else if (warnings[callIndex])
			{
				otherPos = branch[callIndex].Pos;
				GenerateMessage(ref errors, 0x4027, otherPos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[constructorIndex]);
				return false;
			}
			AddRange(ref errors, innerErrors);
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[0][i]
				? x.Replace(adaptedInnerResults[0][i] ?? DefaultNull) : "");
			branch.Extra = ConstructingNStarType;
			return true;
		}
		ListHashSet<int> IncompatibleConstructors = [];
		ListHashSet<(int ConstructorIndex, int ParameterIndex)> BadlyCompatibleConstructors = [];
		ListHashSet<(int ConstructorIndex, int ParameterIndex)> ConversionConstructors = [];
		var callIndexes = new int[constructors.Length];
		var constructorIndexes = new int[constructors.Length];
		for (var j = 0; j < constructors.Length; j++)
		{
			var (_, Parameters, _, _) = constructors[j];
			if (Parameters.Length == 0)
				continue;
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters).All((x, i) =>
				TypesAreCompatible(branch, ref innerErrors, x.Item1, FunctionParameterNStarTypes[i] = x.Item2.Type,
				out warnings[constructorIndexes[j] = callIndexes[j] = i], parameters[i].Copy(),
				out adaptedInnerResults[j][i]!, out extraMessages[i]!) && adaptedInnerResults[j][i] is not null)
				&& CallParameterNStarTypes.Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndexes[j] == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(constructorIndexes[j] = callIndexes[j])
				.All((x, i) => TypesAreCompatible(branch, ref innerErrors, x, Parameters[^1].Type,
				out warnings[callIndexes[j] = constructorIndexes[j] + i],
				parameters[callIndexes[j]], out adaptedInnerResults[j][callIndexes[j]]!, out extraMessages[callIndexes[j]]!)
				&& adaptedInnerResults[j][callIndexes[j]] is not null)))
				IncompatibleConstructors.Add(j);
			else if (warnings.Any(x => x))
				_ = warnings.ToList((x, i) => x ? BadlyCompatibleConstructors.Add((j, i)) : default);
			adaptedInnerResults[j].Combine(parameters)
				.ToList((x, i) => x.Item1 != x.Item2 ? ConversionConstructors.Add((j, i)) : default);
		}
		AddRange(ref errors, innerErrors);
		var thresholdIndexes = callIndexes.IndexesOfMax();
		var incompatibleLength = IncompatibleConstructors.Length;
		if (incompatibleLength == constructors.Length)
		{
			otherPos = branch[callIndexes[thresholdIndexes[0]]].Pos;
			GenerateMessage(ref errors, 0x4062, otherPos,
				CallParameterNStarTypes[callIndexes[thresholdIndexes[0]]], String.Join("\", \"",
				IncompatibleConstructors.Convert(j =>
				constructors[j].Parameters[constructorIndexes[thresholdIndexes[0]]].Type.ToString()).ToHashSet()),
				IncompatibleConstructors.Length,
				constructors[IncompatibleConstructors[0]].Parameters[constructorIndexes[thresholdIndexes[0]]].Type);
			return false;
		}
		BadlyCompatibleConstructors.FilterInPlace(x => !IncompatibleConstructors.Contains(x.ConstructorIndex));
		var bccGroups = BadlyCompatibleConstructors.Group(x => x.ParameterIndex);
		ConversionConstructors.FilterInPlace(x => !IncompatibleConstructors.Contains(x.ConstructorIndex)
			|| BadlyCompatibleConstructors.Exists(y => y.ConstructorIndex == x.ConstructorIndex));
		var cvcGroups = ConversionConstructors.Group(x => x.ParameterIndex);
		var WellCompatibleConstructors = new Chain(constructors.Length).ToHashSet()
			.ExceptWith(IncompatibleConstructors).ExceptWith(bccGroups.ConvertAndJoin(x => x).Convert(x => x.ConstructorIndex))
			.ExceptWith(cvcGroups.ConvertAndJoin(x => x).Convert(x => x.ConstructorIndex));
		if (WellCompatibleConstructors.Length != 0)
		{
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[WellCompatibleConstructors[^1]][i]
				? x.Replace(adaptedInnerResults[WellCompatibleConstructors[^1]][i] ?? DefaultNull) : "");
			branch.Extra = ConstructingNStarType;
			return true;
		}
		else if (ConversionConstructors.Length != 0)
		{
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[ConversionConstructors[^1].ConstructorIndex][i]
				? x.Replace(adaptedInnerResults[ConversionConstructors[^1].ConstructorIndex][i] ?? DefaultNull) : "");
			branch.Extra = ConstructingNStarType;
			return true;
		}
		foreach (var bccGroup in bccGroups)
		{
			otherPos = branch[bccGroup.Key].Pos;
			GenerateMessage(ref errors, 0x4029, otherPos, CallParameterNStarTypes[bccGroup.Key],
				String.Join("\", \"", bccGroup.Convert(item => constructors[item.ConstructorIndex].Parameters.Wrap(x =>
				x[callIndex = Min(bccGroup.Key, x.Length - 1)].Type.ToString())).ToHashSet()));
			return false;
		}
		_ = parameters.ToList((x, i) => x != adaptedInnerResults[BadlyCompatibleConstructors[^1].ConstructorIndex][i]
			? x.Replace(adaptedInnerResults[BadlyCompatibleConstructors[^1].ConstructorIndex][i] ?? DefaultNull) : "");
		branch.Extra = ConstructingNStarType;
		return true;
	}

	private String Expr(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		List<String> subbranchValues = [];
		int i;
		if (branch.Name.AsSpan() is nameof(Assignment) or DeclarationAssignment)
		{
			for (i = branch.Length - 2; i > 0; i -= 2)
			{
				if ((branch[i].Name == nameof(Hypername) && branch[i].Length == 0
					|| branch[i].Name == nameof(Declaration)) && branch[i + 1].Name == "=")
					continue;
				i -= 2;
				break;
			}
			List<String>? innerErrors;
			if (branch[i + 2].Name == nameof(Hypername))
				Hypername(branch[i + 2], out innerErrors, null, true);
			else
				Declaration(branch[i + 2], out innerErrors, true);
			AddRange(ref errors, innerErrors);
		}
		if (TryReadValue(branch.Name, out var value))
		{
			branch.Extra = value.GetNStarType();
			return value.ToString(true, true);
		}
		if (branch.Name == nameof(Class))
			return Class(branch, out errors);
		if (branch.Length == 1)
			branch[0].Extra ??= branch.Extra;
		for (i = 0; i < branch.Length; i++)
		{
			var localConstantsDepth = constantsDepth;
			if (i != 0 && i != branch.Length - 1 && BinaryOperators.ContainsKey(branch[i + 1].Name)
				&& branch[i].Extra is null && branch[i - 1].Extra is NStarType PrevNStarType && !PrevNStarType.Equals(NullType))
				branch[i].Extra = PrevNStarType;
			if (branch[i].Name == "type" && branch[0].Extra is NStarType DirectNStarType)
			{
				var typeName = Type(ref DirectNStarType, branch[0], ref errors);
				if (branch.Name == Pattern && branch.Extra is NStarType PatternNStarType
					&& !(PatternNStarType.Equals(ObjectType)
					|| PatternNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
					|| DirectNStarType.MainType.TryPeek(out block) && block.BlockType == BlockType.Extra
					|| C.IsEqualOrDerived(DirectNStarType, PatternNStarType)))
				{
					var otherPos = branch[i].Pos;
					GenerateMessage(ref errors, 0x40A1, otherPos, PatternNStarType, DirectNStarType);
					branch.Name = DefaultNull;
					branch.RemoveEnd(0);
					branch.Extra = NullType;
					return "not object";
				}
				subbranchValues.SetOrAdd(i, branch.Name == Pattern ? typeName : OpeningTypeof + typeName + ")");
				continue;
			}
			else if (branch[i].Name == nameof(Hypername) && branch[i].Length == 1)
			{
				object? none = null;
				subbranchValues.SetOrAdd(i, Hypername1(branch[i], out var innerErrors, ref none, false));
				AddRange(ref errors, innerErrors);
				continue;
			}
			else if (ExprTypes.Contains(branch[i].Name.ToString()))
			{
				if (branch.Length == 3 && i == 1 && branch[2].Name == "is")
					constantsDepth++;
				if (i == 1 && branch[i].Length == 1
					&& branch[i][0].Name == "type" && branch[i][0].Extra is NStarType PatternNStarType)
				{
					subbranchValues.SetOrAdd(i, Type(ref PatternNStarType, branch, ref errors));
					branch[i].Extra ??= PatternNStarType;
				}
				else
				{
					subbranchValues.SetOrAdd(i, ParseAction(branch[i].Name)(branch[i], out var innerErrors));
					AddRange(ref errors, innerErrors);
				}
				constantsDepth = localConstantsDepth;
				continue;
			}
			else if (branch.Length == 3 && i == 1 && branch[2].Name == "is" && branch[i].Name == "_")
			{
				subbranchValues.SetOrAdd(i, "_");
				continue;
			}
			else if (branch[i].Name == "typeof")
			{
				subbranchValues.SetOrAdd(i, Typeof(branch[i], out var innerErrors));
				AddRange(ref errors, innerErrors);
				continue;
			}
			else if (TryReadValue(branch[i].Name, out value))
			{
				branch[i].Extra = value.GetNStarType();
				subbranchValues.SetOrAdd(i, value.ToString(true, true));
				continue;
			}
			else if (i == 1 && subbranchValues.Length == 1 && TryReadValue(branch[0].Name, out value)
				&& branch[i].Name != "^")
			{
				subbranchValues.SetOrAdd(0, ValueExpr(value, branch, ref errors, i--));
				branch.RemoveAt(0);
				if (branch.Length == 1)
				{
					branch.Name = branch[0].Name;
					branch.Extra = branch[0].Extra;
					branch.RemoveAt(0);
				}
				continue;
			}
			else if (i == 0 || i % 2 != 0)
				return branch.Length == 2 && i == 1 ? UnaryExpr(branch, ref errors, i)
					: ListExpr(branch, ref errors, i);
			if (branch[i - 2].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			var resultType = GetResultType(C, LeftNStarType, RightNStarType,
				subbranchValues[^2].Copy(), subbranchValues[^1].Copy());
			String @default = DefaultConst;
			if (!(branch.Parent?.Name == ReturnString
				|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == ReturnString))
			{
				@default.Add('(');
				@default.AddRange(TypeEqualsToPrimitive(resultType, NullString) ? "String"
					: Type(ref resultType, branch, ref errors)).Add(')');
			}
			@default.Add('!');
			if (branch.Name != Pattern && !TryReadValue(branch[i].Name, out _)
				&& branch[i].Name.AsSpan() is not ("pow" or "tetra" or "penta" or "hexa" or "..")
				&& !AssignmentOperators.Contains(branch[i].Name.ToString())
				&& !TernaryOperators.Contains(branch[i].Name.ToString()) && branch[i].Name != ":"
				&& TryReadValue(branch[Max(i - 3, 0)].Name, out var leftValue)
				&& TryReadValue(branch[i - 1].Name, out var rightValue))
			{
				var innerResult = new TwoValuesExpr(C, leftValue, rightValue, branch, lexems, @default)
					.Calculate(ref errors, ref i, ref errorOccurred);
				subbranchValues.SetOrAdd(i, innerResult);
				continue;
			}
			subbranchValues.SetOrAdd(i, branch[i].Name.ToString() switch
			{
				"+" or "-" => PMExpr(branch, subbranchValues, ref errors, ref i),
				"*" or "/" or "%" => MulDivExpr(branch, subbranchValues, ref errors, ref i),
				"pow" or "tetra" or "penta" or "hexa" => PowExpr(branch, subbranchValues, ref errors, i),
				".." => RangeExpr(branch, subbranchValues, ref errors, i),
				"==" or ">" or "<" or ">=" or "<=" or "!=" or "&&" or "||" or "^^" =>
					BoolExpr(branch, subbranchValues, ref errors, i),
				"is" => PatternExpr(branch, subbranchValues, ref errors, i),
				":" => Ternary(branch, subbranchValues, ref errors, i),
				"CombineWith" => CombineWithExpr(branch, subbranchValues, i),
				nameof(List) => ListExpr(branch, ref errors, i),
				_ when AssignmentOperators.Contains(branch[i].Name.ToString()) =>
					Assignment(branch, subbranchValues, ref errors, i),
				_ when TernaryOperators.Contains(branch[i].Name.ToString()) =>
					branch.Length > i + 2 ? branch[i].Name : Ternary(branch, subbranchValues, ref errors, i),
				_ => BinaryNotListExpr(branch, ref errors, subbranchValues, i),
			});
		}
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		if (branch.Name == StringConcatenation)
			branch.Extra = StringType;
		else if (branch.Name == nameof(List))
			branch.Extra = branch.Elements.Progression(GetListType(NullType), (x, y) =>
			GetResultType(C, x, GetListType(y.Extra is NStarType NStarType ? NStarType : NullType), DefaultNull, DefaultNull));
		else if (branch.Name == nameof(Indexes))
		{
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is NStarType NStarType)
				branch.Extra = GetSubtype(C, NStarType, branch.Length);
			else
				branch.Extra = NullType;
		}
		else if (branch.Length == 1 && ArithmeticExprTypes.Contains(branch.Parent.Name.ToString()))
			branch.Replace(branch[0]);
		else if (branch.Length != 0)
		{
			if (branch[^1].Extra is not NStarType NStarType)
				branch.Extra = NullType;
			else if (branch.Extra is NStarType BranchNStarType && branch.Parent.Name != ReturnString
				&& (!TypesAreCompatible(branch, ref errors, NStarType, BranchNStarType, out var warning,
				[], out _, out var extraMessage)
				|| warning))
			{
				GenerateMessage(ref errors, 0x4014, branch.Pos, extraMessage!, NStarType, BranchNStarType,
					"the exprsession substitution");
				return DefaultNull;
			}
			else
				branch.Extra = NStarType;
		}
		return subbranchValues[i - 1];
	}

	private String ValueExpr(object source, TreeBranch branch, ref List<String>? errors, int i)
	{
		var otherPos = branch[i].Pos;
		object result;
		double realValue;
		switch (branch[i].Name.ToString())
		{
			case "+":
			result = source.Plus();
			branch[i].Name = result.GetNStarType().Equals(ComplexType) ? result.ToString(true, true) : result.ToString(true);
			if (branch[0].Name.Length != 0
				&& (branch[0].Name[^1] is 'r' or 'c' or 'i' && double.TryParse(branch[i].Name.ToString(), out _)
				|| branch[0].Name[^1] == 'm' && decimal.TryParse(branch[i].Name.ToString(), out _)))
				branch[i].Name.Add(branch[0].Name[^1]);
			branch[i].Extra = result.GetNStarType();
			return result.ToString(true, true);
			case "-":
			result = source.Minus();
			branch[i].Name = result.GetNStarType().Equals(ComplexType) ? result.ToString(true, true)
				: result.ToString(true).AddRange(branch[0].Name.Length != 0
				&& branch[0].Name[^1] is 'r' or 'm' or 'c' or 'i' ? branch[0].Name[^1] : []);
			if (branch[0].Name.Length != 0
				&& (branch[0].Name[^1] is 'r' or 'c' or 'i' && double.TryParse(branch[i].Name.ToString(), out _)
				|| branch[0].Name[^1] == 'm' && decimal.TryParse(branch[i].Name.ToString(), out _)))
				branch[i].Name.Add(branch[0].Name[^1]);
			branch[i].Extra = result.GetNStarType();
			return result.ToString(true, true);
			case "!":
			result = source.Not();
			branch[i].Name = result.ToString(true);
			branch[i].Extra = result.GetNStarType();
			return result.ToString(true, true);
			case "~":
			result = source.Tilde();
			branch[i].Name = result.ToString(true);
			branch[i].Extra = result.GetNStarType();
			return result.ToString(true, true);
			case "sin":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Sin(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "cos":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Cos(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "tan":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Tan(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "asin":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Asin(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "acos":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Acos(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "atan":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Atan(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "ln":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Log(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "postfix !":
			var unsignedIntValue = (uint)source.ToNumber();
			if (!source.Equals(0) && unsignedIntValue != source.ToNumber())
			{
				GenerateMessage(ref errors, 0x4003, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			try
			{
				result = Factorial(unsignedIntValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.GetNStarType();
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4003, otherPos);
				branch[i].Name = NullString;
				return DefaultNull;
			}
			case "not":
			branch[i].Extra = source.GetNStarType();
			return ((String)"(").AddRange(branch[i].Name).Add(' ')
				.AddRange(source is null ? NullString : source.ToString(true, true)).Add(')');
			case ">=" or "<=" or ">" or "<":
			realValue = source.ToNumber();
			if (realValue == 0 && !LongDecimal.Zero.Equals(source))
			{
				GenerateMessage(ref errors, 0x40A0, otherPos);
				branch[i].Name = NullString;
				return "not object";
			}
			branch[i].Extra = source.GetNStarType();
			return ((String)"(").AddRange(branch[i].Name).Add(' ').AddRange(source.ToString(true, true)).Add(')');
			case "++" or "--" or "!!":
				{
					GenerateMessage(ref errors, 0x4002, otherPos);
					branch[i].Name = NullString;
					return DefaultNull;
				}
		}
		branch[i].Name = NullString;
		return DefaultNull;
	}

	private String UnaryExpr(TreeBranch branch, ref List<String>? errors, int i)
	{
		if (branch[i].Name.AsSpan() is "++" or "--" or "!!")
			branch.Name = UnaryAssignment;
		if (branch[i - 1].Extra is not NStarType NStarType)
			NStarType = NullType;
		if (!(branch[i].Name == "not"
			|| TypeIsPrimitive(NStarType.MainType) && NStarType.MainType.TryPeek(out var block)
			&& (branch[i].Name == "^" ? IsSmallInteger(block.Name.ToString())
			: IsNumeric(block.Name.AsSpan()) || block.Name == BoolTypeName)))
		{
			GenerateMessage(ref errors, 0x4005, branch[i].Pos, branch[i].Name, NStarType);
			return DefaultNull;
		}
		branch[i].Extra = NStarType;
		var valueString = ParseAction(branch[i - 1].Name)(branch[i - 1], out var innerErrors);
		AddRange(ref errors, innerErrors);
		if (valueString.Length == 0
			|| valueString.AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			return DefaultNull;
		if (branch[i].Name == "^" && TryReadValue(valueString, out var value) && value.ToReal() <= 0)
		{
			GenerateMessage(ref errors, 0x4082, branch[i].Pos);
			return DefaultNull;
		}
		branch.Extra = branch[i].Name.ToString() switch
		{
			"+" or "-" or "~" => GetUnaryOperationType(NStarType),
			"!" or ">=" or "<=" or ">" or "<" => BoolType,
			"^" => IndexType,
			"sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "ln" or "postfix !" =>
				TypeEqualsToPrimitive(NStarType, ComplexTypeName) ? ComplexType : RealType,
			"++" or "--" or "!!" or "not" => NStarType,
			_ => NullType,
		};
		return branch[i].Name.ToString() switch
		{
			"+" => valueString.Insert(0, "(+(").AddRange("))"),
			"-" => valueString.Insert(0, NStarType.Equals(UnsignedLongIntType) ? "(-(MpzT)(" : "(-(").AddRange("))"),
			"!" => valueString.Insert(0, "(!(").AddRange("))"),
			"~" => valueString.Insert(0, "(~(").AddRange("))"),
			"^" => valueString.Insert(0, "^(").Add(')'),
			"sin" => valueString.Insert(0, "Sin(").Add(')'),
			"cos" => valueString.Insert(0, "Cos(").Add(')'),
			"tan" => valueString.Insert(0, "Tan(").Add(')'),
			"asin" => valueString.Insert(0, "Asin(").Add(')'),
			"acos" => valueString.Insert(0, "Acos(").Add(')'),
			"atan" => valueString.Insert(0, "Atan(").Add(')'),
			"ln" => valueString.Insert(0, '(').AddRange(").Log()"),
			"postfix !" => valueString.Insert(0, "Factorial(").Add(')'),
			"++" => TypeEqualsToPrimitive(NStarType, BoolTypeName)
				? valueString.Insert(0, '(').AddRange(" = true)") : valueString.AddRange("++"),
			"--" => TypeEqualsToPrimitive(NStarType, BoolTypeName)
				? valueString.Insert(0, '(').AddRange(" = false)") : valueString.AddRange("--"),
			"!!" => valueString.Copy().Insert(0, '(').AddRange(" = !(").AddRange(valueString).AddRange("))"),
			"not" => valueString.Copy().Insert(0, "(not (").AddRange("))"),
			">=" or "<=" or ">" or "<" => valueString.Copy().Insert(0, ((String)"(").AddRange(branch[i].Name).Add('('))
				.AddRange("))"),
			_ => DefaultNull,
		};
		static NStarType GetUnaryOperationType(NStarType NStarType)
		{
			if (TypeEqualsToPrimitive(NStarType, BoolTypeName) || TypeEqualsToPrimitive(NStarType, StringTypeName))
				return RealType;
			else if (TypeEqualsToPrimitive(NStarType, ByteTypeName))
				return ShortIntType;
			else if (TypeEqualsToPrimitive(NStarType, UnsignedShortIntTypeName))
				return IntType;
			else if (TypeEqualsToPrimitive(NStarType, UnsignedIntTypeName))
				return LongIntType;
			else if (TypeEqualsToPrimitive(NStarType, UnsignedLongIntTypeName)
				|| TypeEqualsToPrimitive(NStarType, UnsignedLongLongTypeName))
				return LongLongType;
			else
				return NStarType;
		}
	}

	private String PMExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, ref int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (!LeftNStarType.Equals(NullType) && !RightNStarType.Equals(NullType))
		{
			if (TypesAreCompatible(branch, ref errors, RightNStarType, LeftNStarType, out var warning, subbranchValues[^1],
				out var destExpr, out _) && !warning && destExpr is not null)
			{
				RightNStarType = LeftNStarType;
				subbranchValues[^1].Replace(destExpr);
			}
			else if (TypesAreCompatible(branch, ref errors, LeftNStarType, RightNStarType, out warning, subbranchValues[^2], out destExpr, out _)
				&& !warning && destExpr is not null)
			{
				LeftNStarType = RightNStarType;
				subbranchValues[^2].Replace(destExpr);
			}
		}
		NStarType resultType;
		if (LeftNStarType.Equals(DateTimeType) && RightNStarType.Equals(TimeSpanType)
			|| branch[i].Name == "+" && LeftNStarType.Equals(TimeSpanType) && RightNStarType.Equals(DateTimeType))
			resultType = DateTimeType;
		else if (branch[i].Name == "-" && LeftNStarType.Equals(DateTimeType) && RightNStarType.Equals(DateTimeType))
			resultType = TimeSpanType;
		else if (LeftNStarType.Equals(DateTimeType) || RightNStarType.Equals(DateTimeType))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return DefaultNull;
		}
		else
			resultType = GetResultType(C, LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		String @default = DefaultConst;
		if (!(branch.Parent?.Name == ReturnString
			|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == ReturnString))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, NullString) ? "String"
				: Type(ref resultType, branch, ref errors)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType)
			&& ((LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName or StringTypeName
			|| IsNumeric(LeftNStarType.MainType.Peek().Name.AsSpan()))
			&& (RightNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName or StringTypeName
			|| IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan()))
			|| LeftNStarType.MainType.Peek().Name.AsSpan() is nameof(DateTime) or "TimeSpan"
			&& RightNStarType.MainType.Peek().Name.AsSpan() is nameof(DateTime) or "TimeSpan"
			&& !(LeftNStarType.MainType.Peek().Name == nameof(DateTime)
			&& RightNStarType.MainType.Peek().Name == nameof(DateTime)
			&& branch[i].Name == "+"))))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is NStarType PrevNStarType))
			PrevNStarType = NullType;
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, StringTypeName);
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, StringTypeName) || TypeEqualsToPrimitive(RightNStarType, CharTypeName);
		var isStringPrev = TypeEqualsToPrimitive(PrevNStarType, StringTypeName);
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, NullString);
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, NullString);
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		if (branch[i].Name == "-" && (isStringLeft || isStringRight))
		{
			GenerateMessage(ref errors, 0x4007, branch[i].Pos);
			return @default;
		}
		if (isStringPrev && !isStringRight || branch[i].Name == "-" && (isStringLeft || isStringRight))
		{
			if (branch[Max(i - 3, 0)].Name == nameof(PMExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				branch[Max(i - 3, 0)] = new(nameof(PMExpr), [branch[Max(i - 3, 0)], branch[i - 1], branch[i]])
				{
					Extra = resultType
				};
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (i >= 4 && !isStringLeft && isStringRight)
		{
			branch[0] = new TreeBranch(nameof(PMExpr), branch.GetRange(0, i - 1));
			branch.Remove(1, i - 2);
			i = 2;
		}
		else if (branch.Name == nameof(Expr) && isStringLeft && isStringRight)
			branch.Name = StringConcatenation;
		branch[i].Extra = resultType;
		if (isStringLeft && isStringRight)
		{
			var result = subbranchValues[^2].Copy();
			if (isStringPrev)
				result.AddRange(".Copy()");
			return result.AddRange(".AddRange(").AddRange(subbranchValues[^1]).Add(')');
		}
		else if (isStringLeft || isStringRight)
		{
			var result = ((String)"((").AddRange(nameof(String)).AddRange(")(").AddRange(subbranchValues[^2]).AddRange(").");
			result.AddRange(nameof(TranslateTimeOperations.Plus)).Add('(').AddRange(subbranchValues[^1]).AddRange("))");
			return result;
		}
		else
		{
			if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
				subbranchValues[^2].Insert(0, '(').Add(')');
			if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
				subbranchValues[^1].Insert(0, '(').Add(')');
			if (i < 2)
				return branch[i][^1].Name;
			return subbranchValues[^2].Copy().Add(' ').AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
		}
	}

	private String MulDivExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, ref int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		NStarType resultType;
		if (branch[i].Name == "/" && TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType))
			resultType = GetPrimitiveType(TypePromotionRules.GetQuotientType(LeftNStarType.MainType.Peek().Name,
				TryReadValue(branch[i - 1].Name, out var value) ? value : 5, RightNStarType.MainType.Peek().Name));
		else if (branch[i].Name == "%" && TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType))
			resultType = GetPrimitiveType(TypePromotionRules.GetRemainderType(LeftNStarType.MainType.Peek().Name,
				TryReadValue(branch[i - 1].Name, out var value) ? value : 12345678901234567890,
				RightNStarType.MainType.Peek().Name));
		else
			resultType = GetResultType(C, LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		String @default = DefaultConst;
		if (!(branch.Parent?.Name == ReturnString
			|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == ReturnString))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, NullString) ? "String"
				: Type(ref resultType, branch, ref errors)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(LeftNStarType.MainType)
			&& (LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or StringTypeName
			|| IsNumeric(LeftNStarType.MainType.Peek().Name.AsSpan()))
			&& TypeIsPrimitive(RightNStarType.MainType)
			&& ((RightNStarType.MainType.Peek().Name.AsSpan() is StringTypeName
			|| IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan()))
			&& (branch[i].Name != "%"
			|| LeftNStarType.MainType.Peek().Name.AsSpan() is not (ComplexTypeName or LongComplexTypeName)
			&& RightNStarType.MainType.Peek().Name.AsSpan() is not (ComplexTypeName or LongComplexTypeName))
			|| branch[i].Name == "*" && RightNStarType.MainType.Peek().Name == NullString)))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is NStarType PrevNStarType))
			PrevNStarType = NullType;
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, StringTypeName);
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, StringTypeName);
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, NullString);
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, NullString);
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		if (branch[i].Name != "*" || isNullLeft || isNullRight)
		{
			if (isStringLeft || isStringRight)
			{
				GenerateMessage(ref errors, 0x4009, branch[i].Pos);
				return @default;
			}
		}
		else if (isStringLeft || isStringRight)
		{
			if (isStringLeft && isStringRight)
			{
				GenerateMessage(ref errors, 0x4008, branch[i].Pos);
				return @default;
			}
		}
		else if (!TypesAreCompatible(branch, ref errors, LeftNStarType, resultType, out var warning, subbranchValues[^2],
			out var destExpr, out _) || warning
			|| !TypesAreCompatible(branch, ref errors, RightNStarType, resultType, out warning, subbranchValues[^1],
			out var destExpr2, out _) || warning)
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return @default;
		}
		else if (destExpr is not null && destExpr2 is not null)
		{
			subbranchValues[^2] = destExpr;
			subbranchValues[^1] = destExpr2;
		}
		if (TypeEqualsToPrimitive(PrevNStarType, StringTypeName) && !isStringRight)
		{
			if (branch[Max(i - 3, 0)].Name == nameof(MulDivExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				branch[Max(i - 3, 0)] = new(nameof(MulDivExpr), [branch[Max(i - 3, 0)], branch[i - 1], branch[i]])
				{
					Extra = resultType
				};
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		branch[i].Extra = resultType;
		if (branch[i].Name.AsSpan() is "/" or "%"
			&& !TypeEqualsToPrimitive(LeftNStarType, RealTypeName) && !TypeEqualsToPrimitive(RightNStarType, RealTypeName)
			&& !TypeEqualsToPrimitive(LeftNStarType, DecimalTypeName) && !TypeEqualsToPrimitive(RightNStarType, DecimalTypeName)
			&& subbranchValues[^1].AsSpan() is "0" or "0i" or "0u" or "0L" or "0uL" or "0LL" or "\"0\"")
		{
			GenerateMessage(ref errors, 0x4004, branch[i].Pos);
			branch[Max(i - 3, 0)] = new(@default, branch.Pos, branch.EndPos, branch.Container);
		}
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		if (isStringLeft)
			return subbranchValues[^2].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(subbranchValues[^1]).Add(')');
		if (isStringRight)
			return subbranchValues[^1].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(subbranchValues[^2]).Add(')');
		if (branch[i].Name.AsSpan() is "/" or "%" && TypeEqualsToPrimitive(LeftNStarType, DecimalTypeName)
			&& !TypeEqualsToPrimitive(RightNStarType, DecimalTypeName))
			subbranchValues[^2].Insert(0, "(decimal)(").Add(')');
		if (branch[i].Name.AsSpan() is "/" or "%" && TypeEqualsToPrimitive(LeftNStarType, RealTypeName)
			&& !TypeEqualsToPrimitive(RightNStarType, RealTypeName))
			subbranchValues[^2].Insert(0, "(double)(").Add(')');
		var result = i < 2 ? branch[i].Name : subbranchValues[^2].Copy().Add(' ')
			.AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
		if (branch[i].Name.AsSpan() is "/" or "%" && !LeftNStarType.Equals(resultType))
		{
			if (!TypeIsPrimitive(resultType.MainType) || !resultType.MainType.TryPeek(out _))
				return result;
			return result.Insert(0, ((String)"(").AddRange(Type(ref resultType, branch, ref errors)).AddRange(")(")).Add(')');
		}
		return result;
	}

	private String PowExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 1].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 2].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		string leftPrimitiveType;
		if (!(TypeIsPrimitive(LeftNStarType.MainType)
			&& IsNumeric(leftPrimitiveType = LeftNStarType.MainType.Peek().Name.ToString())
			&& TypeIsPrimitive(RightNStarType.MainType) && IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan()))
			|| leftPrimitiveType is LongLongTypeName or UnsignedLongLongTypeName
			&& (!TypesAreCompatible(branch, ref errors, RightNStarType, IntType, out var warning, subbranchValues[^1], out _, out _) || warning))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return "default(double)!";
		}
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, NullString);
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, NullString);
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		if (leftPrimitiveType == LongLongTypeName)
			branch[i].Extra = LongLongType;
		else if (leftPrimitiveType == UnsignedLongLongTypeName)
			branch[i].Extra = UnsignedLongLongType;
		else
			branch[i].Extra = RealType;
		if (leftPrimitiveType is LongLongTypeName or UnsignedLongLongTypeName)
			return ((String)"(").AddRange(subbranchValues[^1]).AddRange(").").AddRange(nameof(MpzT.One.Power))
				.Add('(').AddRange(subbranchValues[^2]).Add(')');
		return i < 2 ? branch[i].Name : ((String)"Pow(").AddRange(subbranchValues[^1])
			.AddRange(", ").AddRange(subbranchValues[^2]).Add(')');
	}

	private String RangeExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (TryReadValue(subbranchValues[^2], out var value) && value.ToReal() <= 0
			|| TryReadValue(subbranchValues[^1], out value) && value.ToReal() <= 0)
		{
			GenerateMessage(ref errors, 0x4082, branch[i].Pos);
			return DefaultNull;
		}
		if (subbranchValues[^2].AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual
			|| subbranchValues[^1].AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			return DefaultNull;
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is ByteTypeName
			or ShortIntTypeName or UnsignedShortIntTypeName or IntTypeName or "index"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is ByteTypeName
			or ShortIntTypeName or UnsignedShortIntTypeName or IntTypeName or "index"))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return "default(double)!";
		}
		branch[i].Extra = RangeType;
		String result = [];
		if (subbranchValues[^2].StartsWith('^'))
			result.AddRange(subbranchValues[^2]);
		else if (LeftNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^2]).AddRange(OutVar);
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^").AddRange(varName).AddRange(".Value : (");
			result.AddRange(varName).AddRange(".Value - 1))");
		}
		else
			result.AddRange("((").AddRange(subbranchValues[^2]).AddRange(") - 1)");
		result.AddRange("..");
		if (subbranchValues[^1].StartsWith('^'))
			result.AddRange("^((").AddRange(subbranchValues[^1][1..]).AddRange(") - 1)");
		else if (RightNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^1]).AddRange(OutVar);
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^(").AddRange(varName).AddRange(".Value - 1) : ");
			result.AddRange(varName).AddRange(".Value)");
		}
		else
			result.AddRange(subbranchValues[^1]);
		return result;
	}

	private String BoolExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (!LeftNStarType.Equals(NullType) && !RightNStarType.Equals(NullType))
		{
			if (TypesAreCompatible(branch, ref errors, RightNStarType, LeftNStarType, out var warning, subbranchValues[^1],
				out var destExpr, out _) && !warning && destExpr is not null)
			{
				RightNStarType = LeftNStarType;
				subbranchValues[^1].Replace(destExpr);
			}
			else if (TypesAreCompatible(branch, ref errors, LeftNStarType, RightNStarType, out warning, subbranchValues[^2], out destExpr, out _)
				&& !warning && destExpr is not null)
			{
				LeftNStarType = RightNStarType;
				subbranchValues[^2].Replace(destExpr);
			}
		}
		if (!(LeftNStarType.Equals(RightNStarType) || (branch[i].Name.AsSpan() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(LeftNStarType.MainType)
			&& (LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			|| IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan()))
			|| branch[i].Name.AsSpan() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name == BoolTypeName)
			&& (branch[i].Name.AsSpan() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(RightNStarType.MainType)
			&& (RightNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
			|| IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan()))
			|| branch[i].Name.AsSpan() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name == BoolTypeName)))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return False;
		}
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, NullString);
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, NullString);
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace(DynamicCast, "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		branch[i].Extra = BoolType;
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		return i < 2 ? branch[i].Name : subbranchValues[^2].Copy().Add(' ')
			.AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
	}

	private String Assignment(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (constantsDepth != 0 && !(branch.Length == 3 && branch[1].Name == nameof(Declaration) && branch[2].Name == "="))
		{
			GenerateMessage(ref errors, 0x4052, branch[i].Pos);
			branch.Name = NullString;
			branch.Elements.Clear();
			branch.Extra = NullType;
			return DefaultNull;
		}
		if (branch[i].Name == "=" && TryReadValue(branch[Max(0, i - 3)].Name, out _) && branch.Parent is not null
			&& (branch.Parent.Name == "if" || branch.Parent.Name == nameof(Expr)
			&& BoolOperators.Contains(branch.Parent[Min(Max(branch.Parent.Elements.FindIndex(x =>
			ReferenceEquals(x, branch)) + 1, 2), branch.Parent.Length - 1)].Name.ToString())))
			GenerateMessage(ref errors, 0x8009, branch[i].Pos);
		else if (branch[i].Name == "=" && branch[i - 1].Name == nameof(Hypername) && branch[Max(0, i - 3)] == branch[i - 1])
			GenerateMessage(ref errors, 0x8007, branch[i].Pos);
		branch.Name = nameof(Assignment);
		if (branch[i - 2].Extra is not NStarType SrcNStarType)
			SrcNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType DestNStarType)
			DestNStarType = NullType;
		var powWarning = false;
		if (branch[i].Name == "pow="
			&& TypesAreCompatible(branch, ref errors, DestNStarType, LongLongType, out powWarning, subbranchValues[^2], out _, out _)
			&& !powWarning && TypesAreCompatible(branch, ref errors, SrcNStarType, IntType, out powWarning, subbranchValues[^2],
			out var adaptedSource, out _) && !powWarning && adaptedSource is not null)
			subbranchValues[^2] = ((String)"(").AddRange(subbranchValues[^1]).AddRange(").").AddRange(nameof(MpzT.One.Power))
				.Add('(').AddRange(adaptedSource).Add(')');
		else if (branch[i].Name == "pow=" && TypesAreCompatible(branch, ref errors, DestNStarType, RealType, out powWarning, subbranchValues[^2],
			out adaptedSource, out _) && adaptedSource is not null
			&& TypesAreCompatible(branch, ref errors, SrcNStarType, RealType, out powWarning, subbranchValues[^2],
			out adaptedSource, out _) && adaptedSource is not null)
		{
			SrcNStarType = RealType;
			subbranchValues[^2] = ((String)"Pow(").AddRange(subbranchValues[^1])
				.AddRange(", ").AddRange(adaptedSource).Add(')');
		}
		else
			adaptedSource = subbranchValues[^2];
		var srcBelowInt = TypeIsPrimitive(SrcNStarType.MainType)
			&& SrcNStarType.MainType.Peek().Name.AsSpan() is ByteTypeName
			or ShortCharTypeName or ShortIntTypeName or UnsignedShortIntTypeName;
		if (!C.TypeIsFullySpecified(DestNStarType, branch.Container) && !DestNStarType.Equals(SrcNStarType)
			|| branch[i].Name.AsSpan() is "+=" or "-=" && TypeEqualsToPrimitive(SrcNStarType, nameof(TimeSpan))
			&& TypeEqualsToPrimitive(DestNStarType, nameof(DateTime)))
			branch[i].Extra = DestNStarType;
		else if (srcBelowInt && (!TypesAreCompatible(branch, ref errors, IntType, SrcNStarType,
			out var notPowWarning, subbranchValues[^2], out adaptedSource, out var extraMessage) || adaptedSource is null)
			|| !TypesAreCompatible(branch, ref errors, SrcNStarType, DestNStarType,
			out notPowWarning, adaptedSource, out adaptedSource, out extraMessage) || adaptedSource is null)
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcNStarType, DestNStarType, "the assignment");
			branch.Name = DefaultNull;
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return DefaultNull;
		}
		else if (!srcBelowInt && (powWarning || notPowWarning)
			&& (branch.Name != DeclarationAssignment || subbranchValues[^2].ContainsAnyExcluding("-0123456789")))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4027, otherPos, extraMessage!, SrcNStarType, DestNStarType);
			branch.Name = DefaultNull;
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return DefaultNull;
		}
		else if (SrcNStarType.MainType.Equals(TupleBlockStack) && DestNStarType.MainType.Equals(ListBlockStack))
		{
			adaptedSource = ((String)"(").AddRange(Type(ref DestNStarType, branch[i - 1], ref errors));
			adaptedSource.AddRange(")(").AddRange(subbranchValues[^2]).Add(')');
			branch[i].Extra = DestNStarType;
		}
		else
			branch[i].Extra = DestNStarType;
		var oldAdaptedSource = adaptedSource;
		var isDestinationGUI = branch[i - 1].Name != nameof(Declaration) && ContainsGUITypes(branch[1]);
		if (ContainsGUITypes(branch[0]) && !isDestinationGUI
			&& branch.Parent is not null && branch.Parent.Name == nameof(Main) && i == branch.Length - 1)
		{
			oldAdaptedSource = oldAdaptedSource.Copy();
			WrapIntoUIThread(adaptedSource);
		}
		if (subbranchValues[^1].AsSpan() is "_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			return IsCall(branch[0]) ? adaptedSource : DefaultNullEqual;
		else if (branch[i].Name == "pow=")
			return i < 2 ? branch[i].Name : subbranchValues[^1].Copy().AddRange(" = ").AddRange(adaptedSource);
		else if (branch[i].Name == "+=" && TypeEqualsToPrimitive(DestNStarType, StringTypeName))
			return i < 2 ? branch[i].Name : subbranchValues[^1].Copy().AddRange(".AddRange(").AddRange(adaptedSource).Add(')');
		if (parsingFunctions.Length != 0 && (parsingFunctions[^1].Value.Attributes & FunctionAttributes.IO) == 0
			&& !(branch[i - 1].Name == nameof(Declaration)
			|| branch[i - 1].Name == nameof(Hypername) && branch[i - 1].Length == 1 && branch[i - 1][0].Length == 0))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x901A, otherPos);
			branch.Name = DefaultNull;
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return DefaultNull;
		}
		if (adaptedSource == "_")
			adaptedSource = DefaultNull;
		String? result, typeName = Type(ref DestNStarType, branch[i - 1], ref errors);
		if (branch[i - 1].Name == nameof(Declaration) || branch[i - 1].Length >= 3 || branch[i - 1][^1].Name == nameof(Indexes)
			|| mutableVariables.Length != 0 && mutableVariables[^1].Contains(branch[i - 1][^1].Name))
			result = subbranchValues[^1].Copy();
		else
		{
			var realName = variableNameMapping[^1][branch[i - 1][0].Name] = new(RandomVarName());
			result = typeName.Copy().Add(' ').AddRange(realName);
		}
		if (branch[i].Name != "=")
		{
			var baseOperatorExpr = ((String)"(").AddRange(typeName).AddRange(")(").AddRange(subbranchValues[^1]);
			baseOperatorExpr.Add(' ').AddRange(branch[i].Name.GetSlice(..^1)).Add(' ');
			var parsed = ((String)nameof(CreateVar)).Add('(').AddRange(baseOperatorExpr).AddRange(oldAdaptedSource);
			if (branch[i - 1].Length < 3 && branch[i - 1][^1].Name != nameof(Indexes))
			{
				var varName = variableNameMapping[^1][branch[i - 1][0].Name];
				parsed.AddRange("), out ").AddRange(typeName).Add(' ').AddRange(varName).Add(')');
				variableExpressionMapping[^1][varName] = (parsed, false);
			}
			if (branch[i - 1].Name == nameof(Hypername) && branch[i - 1].Length == 1 && branch[i - 1][0].Length == 0)
				result.AddRange(" = ").AddRange(baseOperatorExpr).AddRange(adaptedSource).Add(')');
			else
				result.Add(' ').AddRange(branch[i].Name).Add(' ').AddRange(adaptedSource);
		}
		else if (branch[i - 1].Length < 3 && branch[i - 1][^1].Name != nameof(Indexes)
			&& (mutableVariables.Length == 0 || !mutableVariables[^1].Contains(branch[i - 1][^1].Name)))
		{
			var parsed = ((String)nameof(CreateVar)).Add('(').AddRange(adaptedSource);
			var varName = variableNameMapping[^1][branch[i - 1][^1].Name];
			parsed.AddRange(", out ").AddRange(typeName).Add(' ').AddRange(varName).Add(')');
			variableExpressionMapping[^1][varName] = (parsed, false);
			result.Replace(parsed);
		}
		else
		{
			if (parsingFunctions.Length != 0 && (parsingFunctions[^1].Value.Attributes & FunctionAttributes.IO) == 0)
				ReplaceVariableNames(1, ref adaptedSource);
			result.AddRange(" = ").AddRange(adaptedSource == "_" ? DefaultNull : adaptedSource);
		}
		if (isDestinationGUI && branch.Parent is not null && branch.Parent.Name == nameof(Main) && i == branch.Length - 1)
			WrapIntoUIThread(result);
		return result;
	}

	private String PatternExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (ContainsTypes(branch[i - 1]) && !(LeftNStarType.Equals(ObjectType)
			|| LeftNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
			|| RightNStarType.MainType.TryPeek(out block) && block.BlockType == BlockType.Extra
			|| C.IsEqualOrDerived(RightNStarType, LeftNStarType)))
		{
			if (!(branch[i - 1].Name == Pattern && (branch[i - 1].Length == 1 && branch[i - 1][0].Name == "type"
				|| branch[i - 1].Length == 2 && branch[i - 1][1].Name == "not"
				&& branch[i - 1][0].Name.AsSpan() is "type" or Pattern)
				&& branch[i - 1][0].Extra is NStarType SingleNStarType))
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x40A1, otherPos, LeftNStarType, RightNStarType);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else if (C.IsEqualOrDerived(SingleNStarType, LeftNStarType))
				return subbranchValues[^2].Copy().AddRange(" is ").AddRange(subbranchValues[^1]);
			else if (LeftNStarType.MainType.Equals(RecursiveBlockStack)
				&& (LeftNStarType.ExtraTypes.Length != 1
				|| LeftNStarType.ExtraTypes[0].Name == "type" && LeftNStarType.ExtraTypes[0].Extra is NStarType RestrictionType
				&& TypesAreCompatible(branch, ref errors, SingleNStarType, RestrictionType, out var warning, [], out _, out _) && !warning))
			{
				var typeName = Type(ref SingleNStarType, branch, ref errors);
				return subbranchValues[^2].Copy().AddRange(" == typeof(").AddRange(typeName).Add(')');
			}
			else
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x40A1, otherPos, LeftNStarType, RightNStarType);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
		}
		branch[i].Extra = BoolType;
		if (subbranchValues[^1] == "_")
			return "true";
		else if (subbranchValues[^1].AsSpan() is DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual)
			return subbranchValues[^2].Copy().AddRange(" is null");
		else
			return subbranchValues[^2].Copy().Add(' ').AddRange(branch[i].Name)
				.Add(' ').AddRange(subbranchValues[^1] == "_" ? DefaultNull : subbranchValues[^1]);
	}

	private String Ternary(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		branch.Name = nameof(Ternary);
		if (i == 2 && branch[i].Name == ":")
		{
			if (branch[0].Name == nameof(Declaration) && branch[0].Length == 2
				&& branch[0][0].Name == "type" && branch[0][0].Extra is NStarType LeftNStarType
				&& LeftNStarType.Equals(RecursiveType)
				&& branch[1].Name == "type" && branch[1].Extra is NStarType RightNStarType)
			{
				BranchCollection extraTypes = [new("type", branch[1].Pos, branch[1].Container)
				{
					Extra = RightNStarType
				}];
				LeftNStarType.ExtraTypes.Replace(extraTypes);
				branch.Replace(branch[0]);
				return Declaration(branch, out errors);
			}
			else if (!(branch.Extra is NStarType TupleNStarType && TupleNStarType.MainType.Equals(TupleBlockStack)
				&& TupleNStarType.ExtraTypes.Length == 2
				&& TupleNStarType.ExtraTypes[0].Extra is NStarType DestKeyNStarType
				&& TupleNStarType.ExtraTypes[1].Extra is NStarType DestValueNStarType
				&& branch[0].Extra is NStarType SrcKeyNStarType && branch[1].Extra is NStarType SrcValueNStarType))
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4080, otherPos, branch[i].Name);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else if (!TypesAreCompatible(branch, ref errors, SrcKeyNStarType, DestKeyNStarType, out var warning, subbranchValues[^2], out _,
				out var extraMessage) || warning)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcKeyNStarType, DestKeyNStarType,
					"the ternary operator translation");
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else if (!TypesAreCompatible(branch, ref errors, SrcValueNStarType, DestValueNStarType, out warning, subbranchValues[^1], out _,
				out extraMessage) || warning)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcValueNStarType, DestValueNStarType,
					"the ternary operator translation");
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else
			{
				branch[i].Extra = TupleNStarType;
				return ((String)"(").AddRange(subbranchValues[^2]).AddRange(", ").AddRange(subbranchValues[^1]).Add(')');
			}
		}
		if ((i < 4 || branch.Length <= i + 2) && branch[i].Name != ":")
		{
			if (branch[i].Name != "?")
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x400E, otherPos, branch[i].Name);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else if (i < 2)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x400F, otherPos, branch[i].Name);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else
				return subbranchValues[^2].Copy().AddRange(" ? ").AddRange(subbranchValues[^1]).AddRange(" : default!");
		}
		if (branch[i - 2].Name == "?")
		{
			if (branch[i - 3].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			NStarType ResultNStarType;
			if (branch.Parent is not null && branch.Parent.Name == ReturnString && branch.Parent.Parent is not null
				&& branch.Parent.Parent.Name == nameof(Main) && branch.Parent.Parent.Parent is null)
				branch[i].Extra = ResultNStarType = LeftNStarType;
			else if (TypesAreCompatible(branch, ref errors, LeftNStarType, RightNStarType, out var warning, subbranchValues[^3],
				out var destExpr, out _)
				&& !warning && destExpr is not null)
			{
				branch[i].Extra = ResultNStarType = RightNStarType;
				if (!ReferenceEquals(subbranchValues[^3], destExpr))
					subbranchValues[^3].Replace(destExpr);
			}
			else if (TypesAreCompatible(branch, ref errors, RightNStarType, LeftNStarType, out warning, subbranchValues[^1], out destExpr, out _)
				&& !warning && destExpr is not null)
			{
				branch[i].Extra = ResultNStarType = LeftNStarType;
				if (!ReferenceEquals(subbranchValues[^1], destExpr))
					subbranchValues[^1].Replace(destExpr);
			}
			else
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, LeftNStarType.ToString(), RightNStarType.ToString());
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			var result = subbranchValues[^4].Copy().AddRange(" ? ").AddRange(subbranchValues[^3]);
			result.AddRange(" : ").AddRange(subbranchValues[^1]);
			if (ResultNStarType.Equals(ByteType))
				result.Insert(0, "(byte)(").Add(')');
			else if (ResultNStarType.Equals(ShortIntType))
				result.Insert(0, "(short)(").Add(')');
			else if (ResultNStarType.Equals(UnsignedShortIntType))
				result.Insert(0, "(ushort)(").Add(')');
			else if (ResultNStarType.Equals(CharType))
				result.Insert(0, "(char)(").Add(')');
			return result;
		}
		else
		{
			if (branch[i - 4].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 3].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType NStarType3)
				NStarType3 = NullType;
			var checksEquality = branch[i - 2].Name.AsSpan() is "?=" or "?!=";
			if (!((checksEquality && TypeEqualsToPrimitive(LeftNStarType, StringTypeName)
				|| TypeIsPrimitive(LeftNStarType.MainType)
				&& (LeftNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName
				|| IsNumeric(LeftNStarType.MainType.Peek().Name.AsSpan())))
				&& (checksEquality && TypeEqualsToPrimitive(RightNStarType, StringTypeName)
				|| TypeIsPrimitive(RightNStarType.MainType)
				&& (RightNStarType.MainType.Peek().Name.AsSpan() is NullString or BoolTypeName or ByteTypeName
				|| IsNumeric(RightNStarType.MainType.Peek().Name.AsSpan())))))
			{
				var otherPos = branch[i - 2].Pos;
				GenerateMessage(ref errors, 0x4006, otherPos, branch[i - 2].Name,
					LeftNStarType.ToString(), RightNStarType.ToString());
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			if (branch.Parent is not null && branch.Parent.Name == ReturnString && branch.Parent.Parent is not null
				&& branch.Parent.Parent.Name == nameof(Main) && branch.Parent.Parent.Parent is null)
				branch[i].Extra = LeftNStarType;
			else if (TypesAreCompatible(branch, ref errors, LeftNStarType, NStarType3, out var warning, subbranchValues[^3], out var outExpr, out _)
				&& !warning && outExpr is not null)
			{
				branch[i].Extra = NStarType3;
				if (!ReferenceEquals(subbranchValues[^3], outExpr))
					subbranchValues[^3].Replace(outExpr);
			}
			else if (TypesAreCompatible(branch, ref errors, NStarType3, LeftNStarType, out warning, subbranchValues[^1], out outExpr, out _)
				&& !warning && outExpr is not null)
			{
				branch[i].Extra = LeftNStarType;
				if (!ReferenceEquals(subbranchValues[^1], outExpr))
					subbranchValues[^1].Replace(outExpr);
			}
			else
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, LeftNStarType.ToString(), NStarType3.ToString());
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			var result = ((String)"NStar.Core.").AddRange(nameof(Extents)).Add('.').AddRange(nameof(CreateVar));
			result.Add('(').AddRange(subbranchValues[^4]).AddRange(OutVar);
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(") ").AddRange(branch[i - 2].Name[1..]);
			if (branch[i - 2].Name == "?=")
				result.Add('=');
			result.Add(' ').AddRange(subbranchValues[^3]).AddRange(" ? ").AddRange(varName);
			result.AddRange(" : ").AddRange(subbranchValues[^1]);
			return result;
		}
	}

	private static String CombineWithExpr(TreeBranch branch, List<String> subbranchValues, int i)
	{
		if (branch[i - 1].Extra is not NStarType NStarType)
			NStarType = NullType;
		branch[i].Extra = NStarType;
		return subbranchValues[^1];
	}

	private String ListExpr(TreeBranch branch, ref List<String>? errors, int i)
	{
		var result = ParseAction(branch[i].Name)(branch[i], out var innerErrors);
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String BinaryNotListExpr(TreeBranch branch, ref List<String>? errors, List<String> subbranchValues, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (branch[i].Name.AsSpan() is "<<" or ">>" or "<<<" or ">>>"
			&& (!TypesAreCompatible(branch, ref errors, RightNStarType, IntType, out var warning, subbranchValues[^1], out _, out _) || warning))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, branch[i].Name);
			branch[i].Extra = NullType;
			return DefaultNull;
		}
		else if (branch[i].Name.AsSpan() is "<<<" or ">>>"
			&& (!TypesAreCompatible(branch, ref errors, LeftNStarType, LongLongType, out warning, subbranchValues[^1], out _, out _) || warning))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4083, otherPos, branch[i].Name);
			branch[i].Extra = NullType;
			return DefaultNull;
		}
		else if (branch[i].Name.AsSpan() is "or" or "xor"
			&& (ContainsDeclarations(branch[i - 2]) || ContainsDeclarations(branch[i - 1])))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x40A2, otherPos, branch[i].Name);
			branch[i].Extra = NullType;
			return DefaultNull;
		}
		else if (branch[i].Name.AsSpan() is "and" or "xor"
			&& (branch[i - 2].Length == 2 && branch[i - 2][1].Name == "not" && ContainsDeclarations(branch[i - 2][0])
			|| branch[i - 1].Length == 2 && branch[i - 1][1].Name == "not" && ContainsDeclarations(branch[i - 1])))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x40A3, otherPos, branch[i].Name);
			branch[i].Extra = NullType;
			return DefaultNull;
		}
		if (branch[i].Name.AsSpan() is "and" or "or" or "xor")
		{
			if (LeftNStarType.Equals(NullType))
			{
				subbranchValues[^2] = NullString;
				branch[i].Extra = RightNStarType;
			}
			else if (RightNStarType.Equals(NullType))
			{
				subbranchValues[^1] = NullString;
				branch[i].Extra = LeftNStarType;
			}
		}
		branch[i].Extra ??= branch[i].Name.AsSpan() is "<<" or ">>" or "<<<" or ">>>" ? LeftNStarType
			: GetResultType(C, LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		if (i < 2)
			return branch[i].Name;
		if (branch[i].Name.AsSpan() is "<<" or ">>" && LeftNStarType.Equals(RealType))
		{
			var result = subbranchValues[^2].Copy().AddRange(" * Pow(2, ");
			if (branch[i].Name.AsSpan() is "<<<" or "<<")
				result.AddRange(subbranchValues[^1]);
			else
				result.AddRange("-(").AddRange(subbranchValues[^1]).Add(')');
			result.Add(')');
			return result;
		}
		else if (branch[i].Name == "<<<")
		{
			if (!(LeftNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive
				&& block.Name.AsSpan() is ByteTypeName or ShortCharTypeName or ShortIntTypeName or UnsignedShortIntTypeName
				or CharTypeName or IntTypeName or UnsignedIntTypeName
				or LongCharTypeName or LongIntTypeName or UnsignedLongIntTypeName))
				return subbranchValues[^2].Copy().AddRange(" << ").AddRange(subbranchValues[^1]);
			String result = "(";
			result.AddRange(nameof(CreateVar)).Add('(');
			result.AddRange(subbranchValues[^2]).AddRange(OutVar);
			var leftVarName = RandomVarName();
			result.AddRange(leftVarName).AddRange(") << (int)unchecked((uint)");
			var rightVarName = RandomVarName();
			result.AddRange(nameof(CreateVar)).Add('(');
			result.AddRange(subbranchValues[^1]).AddRange(OutVar);
			result.AddRange(rightVarName).AddRange(") % (sizeof(");
			result.AddRange(Type(ref LeftNStarType, branch, ref errors)).AddRange(") * 8)) | ");
			result.AddRange(leftVarName).AddRange(" >>> (int)unchecked((uint)-");
			result.AddRange(rightVarName).AddRange(" % (sizeof(");
			result.AddRange(Type(ref LeftNStarType, branch, ref errors)).AddRange(") * 8)))");
			return result;
		}
		return subbranchValues[^2].Copy().Add(' ').AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
	}

	private String List(TreeBranch branch, out List<String>? errors)
	{
		String result = "(";
		List<String> listItemValues = [];
		errors = null;
		if (branch.Extra is NStarType MainNStarType)
		{
			if (TypeEqualsToPrimitive(MainNStarType, "list", false))
			{
				var innerType = GetSubtype(C, MainNStarType);
				for (var i = 0; i < branch.Length; i++)
					branch[i].Extra = innerType;
			}
			else if (TypeEqualsToPrimitive(MainNStarType, TupleName, false))
			{
				if (MainNStarType.ExtraTypes
					.Any(x => !(x.Name == "type" && x.Extra is NStarType
					|| x.Length == 0 && int.TryParse(x.Name.AsSpan(), out _))))
					Type(ref MainNStarType, branch, ref errors, true);
				if (MainNStarType.ExtraTypes
					.Any(x => !(x.Name == "type" && x.Extra is NStarType
					|| x.Length == 0 && int.TryParse(x.Name.AsSpan(), out _))))
					return DefaultNull;
				var maxIndex = MainNStarType.ExtraTypes.Length == 2 && MainNStarType.ExtraTypes[1].Length == 0
					&& MainNStarType.ExtraTypes[1].Extra is null ? 0 : int.MaxValue;
				for (var i = 0; i < MainNStarType.ExtraTypes.Length && i < branch.Length; i++)
					branch[i].Extra = (NStarType)MainNStarType.ExtraTypes[Min(i, maxIndex)].Extra!;
			}
			else if (MainNStarType.MainType.Equals(DictionaryBlockStack))
			{
				if (MainNStarType.ExtraTypes.Length != 2)
					throw new InvalidOperationException();
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					Type(ref MainNStarType, branch, ref errors, true);
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					throw new InvalidOperationException();
				NStarType itemType = new(TupleBlockStack, MainNStarType.ExtraTypes);
				for (var i = 0; i < branch.Length; i++)
					branch[i].Extra = itemType;
			}
			else if (MainNStarType.MainType.Equals(FuncDictionaryBlockStack))
			{
				if (MainNStarType.ExtraTypes.Length != 2)
					throw new InvalidOperationException();
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					Type(ref MainNStarType, branch, ref errors, true);
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					throw new InvalidOperationException();
				TreeBranch boolBranch = new("type", MainNStarType.ExtraTypes[0].Pos, MainNStarType.ExtraTypes[0].Container)
				{
					Extra = BoolType
				};
				TreeBranch keyBranch = new("type", MainNStarType.ExtraTypes[0].Pos, MainNStarType.ExtraTypes[0].Container)
				{
					Extra = new NStarType(FuncBlockStack, new([boolBranch, MainNStarType.ExtraTypes[0]]))
				};
				TreeBranch valueBranch = new("type", MainNStarType.ExtraTypes[0].Pos, MainNStarType.ExtraTypes[0].Container)
				{
					Extra = new NStarType(FuncBlockStack, new([MainNStarType.ExtraTypes[1], MainNStarType.ExtraTypes[0]]))
				};
				NStarType itemType = new(TupleBlockStack, new([keyBranch, valueBranch]));
				for (var i = 0; i < branch.Length; i++)
					branch[i].Extra = itemType;
			}
		}
		if (branch.Length > MaxLiteralItems)
		{
			GenerateMessage(ref errors, 0x401A, branch[MaxLiteralItems].Pos, MaxLiteralItems);
			branch.Name = DefaultNull;
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return DefaultNull;
		}
		for (var i = 0; i < branch.Length; i++)
		{
			if (i > 0)
				result.AddRange(", ");
			if (TryReadValue(branch[i].Name, out var value))
			{
				branch[i].Extra = value.GetNStarType();
				listItemValues.Add(value.ToString(true, true));
				result.AddRange(listItemValues[^1]);
			}
			else
			{
				var innerResult = ParseAction(branch[i].Name)(branch[i], out var innerErrors);
				if (innerResult.AsSpan() is not "_" and not DefaultConst and not DefaultNull
					and not DefaultConstEqual and not DefaultNullEqual
					&& (branch[i].Name != nameof(Hypername)
					|| branch[i].Extra is not NStarType ExprNStarType || !ExprNStarType.Equals(NullType)))
					listItemValues.Add(innerResult);
				else if (branch.Parent is not null && branch.Parent.Name == nameof(Expr) && branch.Parent.Parent is not null
					&& branch.Parent.Parent.Name == ReturnString && branch.Parent.Parent.Parent is not null
					&& branch.Parent.Parent.Parent.Name == nameof(Main)
					&& branch.Parent.Parent.Parent.Parent is null)
					listItemValues.Add((String)"default(object)!");
				else
					listItemValues.Add((String)DefaultNull);
				result.AddRange(listItemValues[^1]);
				AddRange(ref errors, innerErrors);
			}
		}
		if (branch.Name == nameof(List) && listItemValues.Length != 0 && listItemValues.All(x =>
			x.AsSpan() is DefaultNull or "default(object)!"))
		{
			branch.Extra = NullType;
			return DefaultNull;
		}
		branch.Extra = new NStarType(TupleBlockStack, new(branch.Elements
			.Convert(x => new TreeBranch("type", branch.Pos, branch.Container)
		{
			Extra = x.Extra is NStarType NStarType ? NStarType : NullType
		})));
		return result.Add(')');
	}

	private String XorList(TreeBranch branch, out List<String>? errors)
	{
		var result = ((String)nameof(NStarUtilityFunctions)).Add('.').AddRange(nameof(NStarUtilityFunctions.XorList)).Add('(');
		errors = null;
		for (var i = 0; i < branch.Length; i++)
		{
			if (i > 0)
				result.AddRange(", ");
			if (TryReadValue(branch[i].Name, out var value))
			{
				if (!TypesAreCompatible(branch, ref errors, value.GetNStarType(), BoolType, out var warning, value.ToString(true, true),
					out var destExpr, out _) || warning || destExpr is null)
				{
					var otherPos = branch[i].Pos;
					GenerateMessage(ref errors, 0x4084, otherPos);
					branch.Extra = NullType;
					return DefaultNull;
				}
				branch[i].Extra = value.GetNStarType();
				result.AddRange(destExpr);
			}
			else
			{
				var parsed = ParseAction(branch[i].Name)(branch[i], out var innerErrors);
				if (branch[i].Extra is not NStarType NStarType
					|| !TypesAreCompatible(branch, ref errors, NStarType, BoolType, out var warning, parsed, out var destExpr, out _)
					|| warning || destExpr is null)
				{
					var otherPos = branch[i].Pos;
					GenerateMessage(ref errors, 0x4084, otherPos);
					branch.Extra = NullType;
					return DefaultNull;
				}
				result.AddRange(destExpr);
				AddRange(ref errors, innerErrors);
			}
		}
		branch.Extra = BoolType;
		return result.Add(')');
	}

	private String Lambda(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		var otherPos = branch.FirstPos;
		if (branch.Parent is null || branch.Parent.Name.AsSpan() is not (nameof(Call) or nameof(ConstructorCall)))
		{
			if (branch.Extra is not NStarType FunctionType)
				return Default(ref errors);
			else if (FunctionType.MainType.Equals(EventHandlerBlockStack))
				return LambdaDeterminedType(branch, ref errors);
			else if (!FunctionType.MainType.Equals(FuncBlockStack))
				return Default(ref errors);
			else
				return LambdaDeterminedType(branch, ref errors);
		}
		return LambdaUndeterminedType(branch, ref errors, result, otherPos);
		String Default(ref List<String>? errors)
		{
			GenerateMessage(ref errors, 0x4040, otherPos);
			branch.Extra = NullType;
			return DefaultNull;
		}

		String LambdaUndeterminedType(TreeBranch branch, ref List<String>? errors, String result, int otherPos)
		{
			Debug.Assert(branch.Parent is not null);
			var parentIndex = branch.Parent.Elements.FindIndex(x => ReferenceEquals(x, branch));
			if (parentIndex < 0)
				return Default(ref errors);
			var grandParent = branch.Parent.Parent;
			if (grandParent is null)
				return Default(ref errors);
			var grandParentIndex = grandParent.Elements.FindIndex(x => ReferenceEquals(x, branch.Parent));
			if (grandParentIndex < 1 || grandParent.Extra is not UserDefinedMethodOverloads functions)
				return Default(ref errors);
			List<NStarType> parameterTypes;
			List<TreeBranch> parameterBranches = [];
			String[] parameterNames;
			int foundIndex;
			var success = false;
			variableNameMapping.Add([]);
			for (var i = 0; i < functions.Length; i++)
			{
				var parameters = functions[i].Parameters
					.GetSlice((functions[i].Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0);
				if (parameters.Length <= parentIndex)
					continue;
				var ContainerNStarType = parameters[parentIndex].Type;
				if (!ContainerNStarType.MainType.Equals(FuncBlockStack))
					continue;
				if (ContainerNStarType.ExtraTypes.Skip(1).Any(x => x.Name != "type" || x.Extra is not NStarType))
					continue;
				parameterTypes = ContainerNStarType.ExtraTypes.Skip(1).ToList(x => (NStarType)x.Extra!);
				parameterBranches = ContainerNStarType.ExtraTypes.Skip(1).ToList(x => x);
				if (parameterTypes.Length == 1 && LambdaIsValidParameter(branch[0], out var singleParameterName))
				{
					result.AddRange(AsyncPrefix).AddRange(singleParameterName).AddRange(" => ");
					variableNameMapping[^1].Add(singleParameterName, singleParameterName);
					branch[0].Extra = parameterTypes[0];
					success = true;
					break;
				}
				if (branch[0].Name != nameof(List) || parameterTypes.Length != branch[0].Length)
				{
					GenerateMessage(ref errors, 0x4042, otherPos);
					branch.Extra = NullType;
					variableNameMapping.RemoveAt(^1);
					return DefaultNull;
				}
				parameterNames = new String[branch[0].Length];
				foundIndex = branch[0].Elements.FindIndex((x, index) => !LambdaIsValidParameter(x, out parameterNames[index]));
				if (foundIndex >= 0)
				{
					GenerateMessage(ref errors, 0x4043, otherPos, foundIndex + 1);
					branch.Extra = NullType;
					variableNameMapping.RemoveAt(^1);
					return DefaultNull;
				}
				result.AddRange("async (");
				result.AddRange(String.Join(", ", parameterNames));
				foreach (var parameterName in parameterNames)
					variableNameMapping[^1].Add(parameterName, parameterName);
				result.Add(')').AddRange(" => ");
				for (var j = 0; j < branch[0].Length; j++)
					branch[0][j].Extra = parameterTypes[j];
				success = true;
				break;
			}
			if (!success)
			{
				GenerateMessage(ref errors, 0x4041, otherPos);
				branch.Extra = NullType;
				return DefaultNull;
			}
			result = LambdaClosing(branch, ref errors, result, parameterBranches);
			variableNameMapping.RemoveAt(^1);
			return result;
		}
	}

	private String LambdaDeterminedType(TreeBranch branch, ref List<String>? errors)
	{
		String result = [];
		var otherPos = branch.FirstPos;
		if (branch.Extra is not NStarType FunctionNStarType)
			throw new InvalidOperationException();
		else if (FunctionNStarType.MainType.Equals(EventHandlerBlockStack))
			FunctionNStarType = new(FuncBlockStack, new([new("type", 0, []) { Extra = NullType },
			new("type", 0, []) { Extra = ObjectType }, .. FunctionNStarType.ExtraTypes]));
		else if (!FunctionNStarType.MainType.Equals(FuncBlockStack))
			throw new InvalidOperationException();
		List<NStarType> parameterTypes;
		if (FunctionNStarType.ExtraTypes.Skip(1).Any(x => x.Name != "type" || x.Extra is not NStarType))
		{
			GenerateMessage(ref errors, 0x4044, otherPos);
			branch.Extra = NullType;
			return DefaultNull;
		}
		parameterTypes = FunctionNStarType.ExtraTypes.Skip(1).ToList(x => (NStarType)x.Extra!);
		List<TreeBranch> parameterBranches;
		parameterBranches = FunctionNStarType.ExtraTypes.Skip(1).ToList(x => x);
		if (parameterTypes.Length == 1 && LambdaIsValidParameter(branch[0], out var singleParameterName))
		{
			variableNameMapping[^1].Add(singleParameterName, singleParameterName);
			result.AddRange(singleParameterName).AddRange(" => ");
			branch[0].Extra = parameterTypes[0];
			branch[1].Extra = (NStarType)FunctionNStarType.ExtraTypes[0].Extra!;
			result = LambdaClosing(branch, ref errors, result, parameterBranches);
			variableNameMapping[^1].Remove(singleParameterName);
			return result;
		}
		if (branch[0].Name != nameof(List) || parameterTypes.Length != branch[0].Length)
		{
			GenerateMessage(ref errors, 0x4042, otherPos);
			branch.Extra = NullType;
			return DefaultNull;
		}
		var parameterNames = new String[branch[0].Length];
		var foundIndex = branch[0].Elements.FindIndex((x, index) => !LambdaIsValidParameter(x, out parameterNames[index]));
		if (foundIndex >= 0)
		{
			GenerateMessage(ref errors, 0x4043, otherPos, foundIndex + 1);
			branch.Extra = NullType;
			return DefaultNull;
		}
		result.Add('(');
		result.AddRange(String.Join(", ", parameterNames));
		result.Add(')').AddRange(" => ");
		for (var j = 0; j < branch[0].Length; j++)
			branch[0][j].Extra = parameterTypes[j];
		branch[1].Extra ??= NullType;
		parameterNames.ForEach(x => variableNameMapping[^1].Add(x, x));
		result = LambdaClosing(branch, ref errors, result, parameterBranches);
		parameterNames.ForEach(x => variableNameMapping[^1].Remove(x));
		return result;
	}
	private static bool LambdaIsValidParameter(TreeBranch branch, out String branchName)
	{
		if (branch.Length == 0)
		{
			branchName = branch.Name;
			return true;
		}
		if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
		{
			branchName = default!;
			return false;
		}
		branchName = branch[0].Name;
		return true;
	}

	private String LambdaClosing(TreeBranch branch, ref List<String>? errors, String result, List<TreeBranch> parameterBranches)
	{
		var localIndentationUnits = indentationUnits;
		indentationUnits++;
		var innerResult = ParseAction(branch[1].Name)(branch[1], out var innerErrors);
		indentationUnits = localIndentationUnits;
		if (branch.Extra is NStarType FunctionNStarType && FunctionNStarType.MainType.Equals(FuncBlockStack)
			&& FunctionNStarType.ExtraTypes.Length != 0 && FunctionNStarType.ExtraTypes[0].Name == "type"
			&& FunctionNStarType.ExtraTypes[0].Extra is NStarType ReturnNStarType
			&& C.TypeIsFullySpecified(ReturnNStarType, branch.Container))
		{
			if (branch[1].Extra is not NStarType ValueNStarType)
			{
				GenerateMessage(ref errors, 0x4014, branch[1].Pos, null!, NullType, ReturnNStarType,
					"the lambda translation");
				return result.AddRange(DefaultNull);
			}
			else if (!TypesAreCompatible(branch, ref errors, ValueNStarType, ReturnNStarType,
				out var warning, innerResult, out _, out var extraMessage) || warning)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, ReturnNStarType,
					"the lambda translation");
				return result.AddRange(DefaultNull);
			}
		}
		result.AddRange(innerResult);
		AddRange(ref errors, innerErrors);
		if (branch[1].Extra is not NStarType ReturnNStarType2)
			throw new InvalidOperationException();
		if (branch.Extra is not NStarType BranchNStarType
			|| !BranchNStarType.MainType.Equals(EventHandlerBlockStack)
			&& (BranchNStarType.Equals(NullType) || !C.TypeIsFullySpecified(BranchNStarType, branch.Container)))
			branch.Extra = new NStarType(FuncBlockStack, new([new TreeBranch("type", branch.Pos, branch.Container)
			{
				Extra = ReturnNStarType2
			}, .. parameterBranches]));
		return result;
	}

	private String SwitchExpr(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		if (branch.Length == 0)
			return DefaultNull;
		result.AddRange(ParseAction(branch[0].Name)(branch[0], out errors));
		if (branch.Length == 1 || branch[1].Name != "switch")
			return result;
		if (branch[0].Extra is not NStarType SourceNStarType || !SourceNStarType.MainType.TryPeek(out var sourceBlock)
			|| !(sourceBlock.BlockType == BlockType.Primitive && sourceBlock.Name.AsSpan() is ByteTypeName or ShortIntTypeName
			or UnsignedShortIntTypeName or CharTypeName or IntTypeName or UnsignedIntTypeName
			or LongIntTypeName or UnsignedLongIntTypeName or RealTypeName or DecimalTypeName
			or StringTypeName or ObjectTypeName
			|| sourceBlock.BlockType == BlockType.Class && sourceBlock.Name == "UnsafeString"))
		{
			GenerateMessage(ref errors, 0x4019, branch[0].FirstPos);
			branch.Extra = NullType;
			return DefaultNull;
		}
		if (sourceBlock.Name == StringTypeName)
			result.Insert(0, '(').AddRange(").").AddRange(nameof(RedStarLinq.ToString)).AddRange("()");
		result.AddRange(" switch { ");
		String innerResult = [], prevResult = [], caseResult = [], prevCaseResult = [];
		List<String>? innerErrors;
		var ReturnNStarType = NullType;
		for (var i = 0; i < Max(3, branch[1].Length); i++)
		{
			if (i == 1)
			{
				(innerResult, prevResult) = ([], innerResult);
				(caseResult, prevCaseResult) = ([], caseResult);
			}
			else if (i == 2)
				innerResult = result.AddRange(prevResult).AddRange(innerResult);
			if (i >= branch[1].Length)
			{
				if (i == 1)
					innerResult.AddRange(prevCaseResult);
				continue;
			}
			var x = branch[1][i];
			if (x.Length < 2)
				continue;
			var localConstantsDepth = constantsDepth;
			constantsDepth++;
			if (x[0].Name.AsSpan() is nameof(Declaration) or Pattern && x[0].Length != 0
				&& x[0][0].Name == "type" && x[0][0].Extra is NStarType PatternNStarType)
			{
				if (x.Length == 2 && C.IsEqualOrDerived(SourceNStarType, PatternNStarType) && i != branch[1].Length - 1)
				{
					var otherPos = branch[1][i + 1].FirstPos;
					GenerateMessage(ref errors, 0x801E, otherPos);
					branch[1].RemoveEnd(i + 1);
				}
				if (PatternNStarType.Equals(StringType))
					x[0].Extra = x[0][0].Extra = UnsafeStringType;
			}
			x[0].Extra ??= SourceNStarType;
			String parseResult;
			if (x[0].Name.AsSpan() is "_" or NullString)
				innerResult.AddRange(x[0].Name);
			else if (TryReadValue(parseResult = ParseAction(x[0].Name)(x[0], out innerErrors), out var value))
			{
				innerResult.AddRange(value.ToString(true));
				AddRange(ref errors, innerErrors);
			}
			else
			{
				innerResult.AddRange(parseResult);
				AddRange(ref errors, innerErrors);
			}
			constantsDepth = localConstantsDepth;
			if (x.Length >= 3)
			{
				innerResult.AddRange(" when ").AddRange(ParseAction(x[^2].Name)(x[^2], out innerErrors));
				AddRange(ref errors, innerErrors);
			}
			if (i != 0)
				x[^1].Extra ??= ReturnNStarType;
			innerResult.AddRange(" => ");
			caseResult = ParseAction(x[^1].Name)(x[^1], out innerErrors);
			AddRange(ref errors, innerErrors);
			if (x[^1].Extra is not NStarType NStarType)
				return DefaultNull;
			if (i == 0)
				ReturnNStarType = NStarType;
			else if (TypesAreCompatible(branch, ref errors, ReturnNStarType, NStarType,
				out var warning, prevCaseResult.Copy(), out var outExpr, out _)
				&& !warning && outExpr is not null && (i == 1 || prevCaseResult == outExpr))
			{
				x[^1].Extra = ReturnNStarType = NStarType;
				if (!ReferenceEquals(prevCaseResult, outExpr))
					prevCaseResult.Replace(outExpr);
				prevResult.AddRange(prevCaseResult).AddRange(", ");
			}
			else if (i == 1)
			{
				var otherPos = x[^1].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, ReturnNStarType, NStarType);
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			else if (TypesAreCompatible(branch, ref errors, NStarType, ReturnNStarType,
				out warning, caseResult, out outExpr, out var extraMessage)
				&& !warning && outExpr is not null)
			{
				x[^1].Extra = ReturnNStarType;
				if (!ReferenceEquals(caseResult, outExpr))
					caseResult.Replace(outExpr);
			}
			else
			{
				var otherPos = x[^1].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, ReturnNStarType,
					"the switch expression translation");
				branch.Name = DefaultNull;
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return DefaultNull;
			}
			if (i != 0)
				innerResult.AddRange(caseResult).AddRange(", ");
		}
		branch.Extra = ReturnNStarType;
		return result.AddRange(" }");
	}

	private String Range(TreeBranch branch, out List<String>? errors)
	{
		if (branch.Parent is not null && branch.Parent.Name == "for")
			return Expr(branch, out errors);
		String result = nameof(Chain) + '(';
		errors = null;
		if (branch.Length != 3)
			return DefaultNull;
		List<String> subbranchValues = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		AddRange(ref errors, innerErrors);
		subbranchValues.Add(ParseAction(branch[1].Name)(branch[1], out innerErrors));
		AddRange(ref errors, innerErrors);
		if (!(branch[0].Extra is NStarType LeftNStarType && branch[1].Extra is NStarType RightNStarType))
			return DefaultNull;
		if (subbranchValues[^2].StartsWith('^'))
			result.AddRange(subbranchValues[^2]);
		else if (LeftNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^2]).AddRange(OutVar);
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^").AddRange(varName).AddRange(".Value : (");
			result.AddRange(varName).AddRange(".Value - 1))");
		}
		else
			result.AddRange("((").AddRange(subbranchValues[^2]).AddRange(") - 1)");
		result.AddRange("..");
		if (subbranchValues[^1].StartsWith('^'))
			result.AddRange("^((").AddRange(subbranchValues[^1][1..]).AddRange(") - 1)");
		else if (RightNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^1]).AddRange(OutVar);
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^(").AddRange(varName).AddRange(".Value - 1) : ");
			result.AddRange(varName).AddRange(".Value)");
		}
		else
			result.AddRange(subbranchValues[^1]);
		branch.Extra = ChainType;
		return result.Add(')');
	}

	private String Typeof(TreeBranch branch, out List<String>? errors)
	{
		branch.Extra = RecursiveType;
		if (branch.Length == 0)
		{
			errors = null;
			return "typeof(dynamic)";
		}
		var parseResult = ParseAction(branch[0].Name)(branch[0], out errors);
		if (branch[0].Extra is NStarType NStarType && NStarType.MainType.Equals(RecursiveBlockStack))
		{
			GenerateMessage(ref errors, 0x4091, branch.FirstPos);
			branch.Extra = NullType;
			return DefaultNull;
		}
		if (TryReadValue(parseResult, out var value) || TryReadValue(branch[0].Name, out value))
		{
			var InnerNStarType = value.GetNStarType();
			return ((String)OpeningTypeof).AddRange(Type(ref InnerNStarType, branch, ref errors)).Add(')');
		}
		return parseResult.Insert(0, '(').AddRange(").GetType()");
	}

	private String Return(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		branch[0].Extra ??= parsingFunctions.Length != 0 ? parsingFunctions[^1].Value.ReturnNStarType : null;
		var expr = Expr(branch[0], out var innerErrors);
		if (ContainsGUITypes(branch[0]) && branch.Parent is not null && branch.Parent.Name == nameof(Main))
			WrapIntoUIThread(expr);
		var otherPos = branch.FirstPos;
		if (parsingFunctions.Length == 0 || branch[0].Extra is not NStarType ExprNStarType)
		{
			result.AddRange(ReturnPrefix).AddRange(returnCachePrefix);
			result.AddRange(expr == "_" || branch[0].Extra is NStarType ExprNStarType2
				&& ExprNStarType2.Equals(NullType) ? DefaultNull : expr);
		}
		else if (parsingFunctions[^1].Value.ReturnNStarType.Equals(NullType)
			|| TaskBlockStacks.Contains(parsingFunctions[^1].Value.ReturnNStarType.MainType)
			&& (parsingFunctions[^1].Value.ReturnNStarType.ExtraTypes.Length == 0
			|| parsingFunctions[^1].Value.ReturnNStarType.ExtraTypes[0].Name == "type"
			&& parsingFunctions[^1].Value.ReturnNStarType.ExtraTypes[0].Extra is NStarType TaskNStarType
			&& TaskNStarType.Equals(NullType)))
		{
			branch.Extra ??= NullType;
			result.Add('{');
			if (expr.AsSpan() is not ("_" or DefaultConst or DefaultNull or DefaultConstEqual or DefaultNullEqual))
				result.AddRange(expr).Add(';');
			return result.AddRange("return;}");
		}
		else if (!TypesAreCompatible(branch, ref errors, ExprNStarType, parsingFunctions[^1].Value.ReturnNStarType,
			out var warning, expr, out var adapterExpr, out var extraMessage))
		{
			GenerateMessage(ref errors, 0x402B, otherPos, extraMessage!, ExprNStarType,
				parsingFunctions[^1].Value.ReturnNStarType);
			result.AddRange(ReturnPrefix).AddRange(returnCachePrefix).AddRange("default!");
		}
		else
		{
			if (warning)
				GenerateMessage(ref errors, 0x800A, otherPos, extraMessage!, ExprNStarType,
					parsingFunctions[^1].Value.ReturnNStarType);
			result.AddRange(ReturnPrefix).AddRange(returnCachePrefix).AddRange(adapterExpr ?? DefaultNull);
		}
		result.Add(';');
		branch.Extra ??= branch[0].Extra;
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String Default(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch.Name == nameof(TreeBranch.DoNotAdd))
			return DefaultNull;
		if (MainParsing.TryParse(branch.Name.ToString(), out var value))
		{
			branch.Extra = value.GetNStarType();
			return value.ToString(true, true);
		}
		if (branch.Length == 0)
		{
			if (branch.Name == "type" && branch.Extra is NStarType NStarType)
				return Type(ref NStarType, branch, ref errors);
			return branch.Name == ClassMain ? [] : branch.Name.Copy();
		}
		String result = [];
		if (branch.Name.AsSpan() is "ref" or "out")
		{
			if (branch.Length != 1)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x400C, otherPos, branch.Name.ToString());
				return [];
			}
			result.AddRange(branch.Name).Add(' ').AddRange(Hypername(branch, out var innerErrors, null, false));
			AddRange(ref errors, innerErrors);
			return result;
		}
		if (branch.Name.StartsWith(Namespace))
		{
			result.Add('n').AddRange(branch.Name[1..]).Add('{');
			indentationUnits++;
		}
		foreach (var x in branch.Elements)
		{
			var parsedSubbranch = ParseAction(x.Name)(x, out var innerErrors);
			if (parsedSubbranch.Length != 0)
				result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		if (branch.Name.StartsWith(Namespace))
		{
			indentationUnits--;
			result.Add('}');
		}
		if (!branch.Name.StartsWith(Namespace) || IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private static String Wreck(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		return [];
	}

	private String Type(ref NStarType type, TreeBranch branch, ref List<String>? errors, bool earlyReturn = false)
	{
		if (parsedTypes.TryGetValue(type, out var parsed))
			return parsed;
		String result = [];
		List<String>? innerErrors = null;
		type.ExtraTypes.Filter(x => x.Parent is null)
			.ForEach(x => typeof(TreeBranch).GetProperty("Parent")?.SetValue(x, branch));
		if (type.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
			&& !C.UserDefinedTypes.ContainsKey((branch.Container, block.Name)))
		{
			var name = type.MainType.Peek().Name;
			if (C.UserDefinedPolymorphTypeExists(branch.Container, name, out _)
				|| CheckContainer(branch.Container, stack => C.TempTypes.TryGetValue(stack, out var containerTempTypes)
				&& containerTempTypes.Any(x => x.Name == name), out _))
				return name;
			if (!((C.ConstantExists(new(new(type.MainType.SkipLast(1)), NoBranches), name, out var constant)
				|| C.UserDefinedConstantExists(branch.Container, name, out constant, out _, out _))
				&& constant.HasValue && constant.Value.DefaultValue is not null))
				return DynamicName;
			result.AddRange(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue, out innerErrors));
			if (result == DefaultNull)
				return result;
			if (!(result.StartsWith(OpeningTypeof) && result.EndsWith(')')))
				throw new InvalidOperationException();
			AddRange(ref errors, innerErrors);
			var targetBranch = constant.Value.DefaultValue;
			if (targetBranch.Length != 0)
				targetBranch = targetBranch[0];
			if (targetBranch.Name == "type" && targetBranch.Extra is NStarType NStarType)
				type = NStarType;
			return result[OpeningTypeof.Length..^1];
		}
		else if (earlyReturn)
			return [];
		else if (TypeEqualsToPrimitive(type, "list", false))
		{
			var localConstantsDepth = constantsDepth;
			constantsDepth++;
			int levelsCount;
			if (type.ExtraTypes.Length == 1)
				levelsCount = 1;
			else if (int.TryParse(ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0],
				out innerErrors).ToString(), out var n))
			{
				levelsCount = n;
				AddRange(ref errors, innerErrors);
			}
			else
			{
				GenerateMessage(ref errors, 0x4057, type.ExtraTypes[0].Pos);
				constantsDepth = localConstantsDepth;
				return DynamicName;
			}
			constantsDepth = localConstantsDepth;
			if (type.ExtraTypes.Length == 2)
			{
				type.ExtraTypes[0].Name = levelsCount.ToString();
				type.ExtraTypes[0].Elements.Clear();
				type.ExtraTypes[0].Extra = IntType;
			}
			if (levelsCount == 0)
			{
				if (type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				result.AddRange(Type(ref InnerNStarType, branch, ref errors));
				type.ExtraTypes[^1].Extra = InnerNStarType;
			}
			else
			{
				result.AddRange(((String)"List<").Repeat(levelsCount - 1));
				if (type.ExtraTypes[^1].Name != "type" || type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
				{
					GenerateMessage(ref errors, 0x4056, type.ExtraTypes[^1].Pos);
					result.AddRange(DynamicName);
				}
				else
				{
					var innerTypeName = Type(ref InnerNStarType, branch, ref errors);
					type.ExtraTypes[^1].Extra = InnerNStarType;
					AddListType(innerTypeName, InnerNStarType);
				}
				result.AddRange(((String)">").Repeat(levelsCount - 1));
			}
			parsedTypes.TryAdd(type, result);
			return result;
		}
		else if (TypeEqualsToPrimitive(type, TupleName, false))
		{
			BranchCollection newBranches = [];
			if (type.ExtraTypes.Length == 0)
				return "void";
			if (type.ExtraTypes[0].Extra is not NStarType FirstNStarType)
				throw new InvalidOperationException();
			var first = Type(ref FirstNStarType, branch, ref errors);
			type.ExtraTypes[0].Extra = FirstNStarType;
			if (type.ExtraTypes.Length == 1)
				return first;
			if ((type.ExtraTypes.AllEqual() ? type.ExtraTypes.Length
				: type.ExtraTypes.Length == 2
				&& int.TryParse(ParseAction(type.ExtraTypes[1].Name)(type.ExtraTypes[1], out _).ToString(), out var n)
				? n : -1) is var tupleLength && tupleLength >= 0
				&& FirstNStarType.Equals(BoolType) is var @bool
				&& C.InlineArrays.TryGetValue(@bool ? ~tupleLength : tupleLength, out var @struct))
			{
				result.AddRange(@struct.Name);
				if (!@bool)
					result.Add('<').AddRange(first).Add('>');
				if (@struct.Specified)
				{
					parsedTypes.TryAdd(type, result);
					return result;
				}
				ProcessSingularTuple(tupleLength, @bool, @struct.Name);
				parsedTypes.TryAdd(type, result);
				return result;
			}
			var innerType = type.ExtraTypes[0];
			newBranches.Add(innerType);
			var innerResult = first.Copy();
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name == "type" && type.ExtraTypes[i].Extra is NStarType InnerNStarType)
				{
					result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult);
					innerType = type.ExtraTypes[i];
					innerResult.Replace(Type(ref InnerNStarType, branch, ref errors));
					type.ExtraTypes[i].Extra = InnerNStarType;
					newBranches.Add(innerType);
					continue;
				}
				if (!int.TryParse(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors).ToString(), out var repeats))
				{
					GenerateMessage(ref errors, 0x4050, type.ExtraTypes[i].Pos);
					return DynamicName;
				}
				if (repeats > 256)
				{
					GenerateMessage(ref errors, 0x4058, type.ExtraTypes[i].Pos);
					return DynamicName;
				}
				BranchCollection innerTypeCollection = new(RedStarLinq.FillArray(innerType, repeats));
				innerType = new("type", innerType.Pos, innerType.Container)
				{
					Extra = new NStarType(TupleBlockStack, innerTypeCollection)
				};
				newBranches[^1] = innerType;
				var innerNameCollection = String.Join(", ", RedStarLinq.FillArray(innerResult, repeats));
				AddRange(ref errors, innerErrors);
				if (i >= 2 && type.ExtraTypes[i - 1].Name != "type")
					innerResult.Replace(((String)'(').AddRange(innerNameCollection).Add(')'));
				else
					innerResult.Replace(innerNameCollection);
			}
			type.ExtraTypes.Replace(newBranches);
			result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult).Add(')');
			parsedTypes.TryAdd(type, result);
			return result;
		}
		else if (TypeIsPrimitive(type.MainType) && type.MainType.TryPeek(out block))
		{
			return block.Name.ToString() switch
			{
				NullString => "void",
				ShortCharTypeName => ByteTypeName,
				ShortIntTypeName => "short",
				UnsignedShortIntTypeName => "ushort",
				UnsignedIntTypeName => "uint",
				LongCharTypeName => "(char, char)",
				LongIntTypeName => "long",
				UnsignedLongIntTypeName => "ulong",
				RealTypeName => "double",
				LongLongTypeName => nameof(MpzT),
				UnsignedLongLongTypeName => nameof(MpuT),
				ComplexTypeName => "Complex",
				StringTypeName => nameof(String),
				"index" => nameof(Index),
				"range" => nameof(Range),
				RecursiveTypeName => "Type",
				"universal" => ObjectTypeName,
				_ => type.MainType.Peek().Name,
			};
		}
		else if (type.MainType.Equals(FuncBlockStack))
		{
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType ReturnNStarType)
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				return DynamicName;
			}
			var noReturn = TypeEqualsToPrimitive(ReturnNStarType, NullString);
			if (noReturn && type.ExtraTypes.Length == 1)
			{
				return result.AddRange("Action");
			}
			result.AddRange(noReturn ? "Action<" : "Func<");
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType)
					result.AddRange(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i], out innerErrors));
				else
				{
					result.AddRange(Type(ref InnerNStarType, branch, ref errors));
					type.ExtraTypes[i].Extra = InnerNStarType;
				}
				AddRange(ref errors, innerErrors);
				if (!(noReturn && i == type.ExtraTypes.Length - 1))
					result.AddRange(", ");
			}
			if (!noReturn)
			{
				result.AddRange(Type(ref ReturnNStarType, branch, ref errors));
				type.ExtraTypes[0].Extra = ReturnNStarType;
			}
			result.Add('>');
			parsedTypes.TryAdd(type, result);
			return result;
		}
		var typeIndex = type.MainType.FindLastIndex(x =>
			x.BlockType is not (BlockType.Class or BlockType.Struct
			or BlockType.Interface or BlockType.Delegate)) + 1;
		var @namespace = new BlockStack(type.MainType.Take(typeIndex)).ToString();
		if (!C.ExplicitlyConnectedNamespaces.Contains(@namespace) && !Namespaces.Contains(@namespace)
			&& !IONamespaces.Contains(@namespace) && !ImportedNamespaces.Contains(@namespace))
			result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(type.MainType.FindLastIndex(x =>
				x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct
				or BlockType.Interface or BlockType.Delegate)) + 1)).ToString()));
		else
			result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(typeIndex))
			.ToString()));
		C.ExplicitlyConnectedNamespaces.Reverse<String>().Prepend("System.Unsafe")
			.ForEach(x => result.ReplaceInPlace(x.Copy().Add('.'), ""));
		if (result == nameof(G.IEnumerable<>))
			result.Insert(0, "G.");
		if (result == nameof(ListHashSet<>) && type.ExtraTypes.Length == 1
			&& (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType))
		{
			GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
			result.AddRange(DynamicName);
			return DynamicName;
		}
		if (type.ExtraTypes.Length == 0)
		{
			parsedTypes.TryAdd(type, result);
			return result;
		}
		if (type.ExtraTypes.Length == 1 && type.ExtraTypes[0].Name == nameof(List))
		{
			ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0], out innerErrors);
			AddRange(ref errors, innerErrors);
			String innerResult = [];
			var preservedErrors = errors;
			for (var i = 0; i < type.ExtraTypes[0].Length; i++)
			{
				var x = type.ExtraTypes[0][i];
				if (x.Name == "Hypername" && x.Length == 1)
					x = x[0];
				if (x.Name != "type" || x.Extra is not NStarType NStarType)
					continue;
				if (innerResult.Length != 0)
					innerResult.AddRange(", ");
				innerResult.AddRange(Type(ref NStarType, branch, ref preservedErrors));
			}
			if (innerResult.Length != 0)
				result.Add('<').AddRange(innerResult).Add('>');
			parsedTypes.TryAdd(type, result);
			return result;
		}
		if (type.ExtraTypes.All(x => x.Name == "type" && x.Extra is NStarType NullNStarType
			&& NullNStarType.Equals(NullType)))
			return result;
		result.Add('<');
		for (var i = 0; i < type.ExtraTypes.Length; i++)
		{
			if (type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType)
				result.AddRange(type.ExtraTypes[i].Name);
			else
			{
				result.AddRange(Type(ref InnerNStarType, branch, ref errors));
				type.ExtraTypes[i].Extra = InnerNStarType;
			}
			if (i != type.ExtraTypes.Length - 1)
				result.AddRange(", ");
		}
		result.Add('>');
		parsedTypes.TryAdd(type, result);
		return result;
		void AddListType(String innerTypeName, NStarType NStarType)
		{
			if (!C.TypeIsFullySpecified(NStarType, branch.Container))
			{
				result.AddRange(nameof(List<>)).Add('<');
				result.AddRange(innerTypeName);
				result.Add('>');
				return;
			}
			var DotNetType = TypeConverters.TypeMapping(C, NStarType);
			if (DotNetType == typeof(bool))
				result.AddRange(nameof(BitList));
			else
			{
				result.AddRange(nameof(List<>)).Add('<');
				result.AddRange(innerTypeName);
				result.Add('>');
			}
		}
	}

	private static String TypeMapping(String typeName)
	{
		var after = typeName.GetAfter(((String)"System.Collections.").AddRange(nameof(G.LinkedList<>)));
		if (after.Length != 0)
			return "G.LinkedList" + after;
		after = typeName.GetAfter("System.Collections.");
		if (after.Length != 0)
			return after;
		if (typeName.StartsWith('I') && typeName.EndsWith("Raw"))
			return typeName[..^"Raw".Length];
		if (typeName.AsSpan() is "UnsafeString" or "System.Unsafe.UnsafeString")
			return StringTypeName;
		return typeName;
	}

	private String TypeReflected(ref NStarType type, TreeBranch branch, ref List<String>? errors)
	{
		if (C.TypeIsFullySpecified(type, branch.Container))
			return ((String)OpeningTypeof).AddRange(Type(ref type, branch, ref errors)).Add(')');
		String result = [];
		List<String>? innerErrors = null;
		for (var i = 0; i < type.ExtraTypes.Length; i++)
			if (type.ExtraTypes[i].Parent is null)
				typeof(TreeBranch).GetProperty("Parent")?.SetValue(type.ExtraTypes[i], branch);
		if (type.MainType.Peek().BlockType == BlockType.Extra)
		{
			String visualName = type.MainType.ToString();
			var realName = visualName;
			variableNameMapping.FindLast(x => x.TryGetValue(visualName, out realName));
			return realName ?? visualName;
		}
		if (TypeEqualsToPrimitive(type, "list", false))
		{
			var localConstantsDepth = constantsDepth;
			constantsDepth++;
			int levelsCount;
			if (type.ExtraTypes.Length == 1)
				levelsCount = 1;
			else if (int.TryParse(ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0],
				out innerErrors).ToString(), out var n))
			{
				levelsCount = n;
				AddRange(ref errors, innerErrors);
			}
			else
			{
				GenerateMessage(ref errors, 0x4057, type.ExtraTypes[0].Pos);
				constantsDepth = localConstantsDepth;
				return DynamicName;
			}
			constantsDepth = localConstantsDepth;
			if (type.ExtraTypes.Length == 2)
			{
				type.ExtraTypes[0].Name = levelsCount.ToString();
				type.ExtraTypes[0].Elements.Clear();
				type.ExtraTypes[0].Extra = IntType;
			}
			if (levelsCount == 0)
			{
				if (type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				result.AddRange(TypeReflected(ref InnerNStarType, branch, ref errors));
			}
			else
			{
				result.AddRange(((String)"typeof(List<>).MakeGenericType(").Repeat(levelsCount - 1));
				if (type.ExtraTypes[^1].Name != "type" || type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
				{
					GenerateMessage(ref errors, 0x4056, type.ExtraTypes[^1].Pos);
					result.AddRange(DynamicName);
				}
				else
				{
					var innerTypeName = TypeReflected(ref InnerNStarType, branch, ref errors);
					result.AddRange(nameof(ConstructListType)).Add('(').AddRange(innerTypeName).Add(')');
				}
				result.AddRange(((String)")").Repeat(levelsCount - 1));
			}
		}
		else if (TypeEqualsToPrimitive(type, TupleName, false))
		{
			BranchCollection newBranches = [];
			if (type.ExtraTypes.Length == 0)
				return "void";
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType FirstNStarType)
				throw new InvalidOperationException();
			var prefix = ((String)nameof(ConstructTupleType)).AddRange("(new Type[] { ");
			var suffix = ((String)" }.").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("())");
			var singularPrefix = ((String)nameof(ConstructTupleType)).Add('(');
			singularPrefix.AddRange(nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)).Add('(');
			var singularSuffix = ((String)").").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("())");
			var first = TypeReflected(ref FirstNStarType, branch, ref errors);
			if (type.ExtraTypes.Length == 1)
				return first;
			var innerType = type.ExtraTypes[0];
			newBranches.Add(innerType);
			var innerResult = first.Copy();
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name == "type" && type.ExtraTypes[i].Extra is NStarType InnerNStarType)
				{
					result.AddRange(result.Length == 0 ? prefix : ", ").AddRange(innerResult);
					innerType = type.ExtraTypes[i];
					innerResult.Replace(TypeReflected(ref InnerNStarType, branch, ref errors));
					newBranches.Add(innerType);
					continue;
				}
				if (!int.TryParse(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors).ToString(), out var n))
					n = 1;
				BranchCollection innerTypeCollection = new(RedStarLinq.FillArray(innerType, n));
				innerType = new("type", innerType.Pos, innerType.Container)
				{
					Extra = new NStarType(TupleBlockStack, innerTypeCollection)
				};
				newBranches[^1] = innerType;
				AddRange(ref errors, innerErrors);
				innerResult.Insert(0, singularPrefix).AddRange(", ").AddRange(n.ToString()).AddRange(singularSuffix);
			}
			type.ExtraTypes.Replace(newBranches);
			result.AddRange(result.Length == 0 ? prefix : ", ").AddRange(innerResult).AddRange(suffix);
		}
		else if (TypeIsPrimitive(type.MainType))
		{
			return type.MainType.Peek().Name.ToString() switch
			{
				NullString => "void",
				ShortCharTypeName => ByteTypeName,
				ShortIntTypeName => "short",
				UnsignedShortIntTypeName => "ushort",
				UnsignedIntTypeName => "uint",
				LongCharTypeName => "(char, char)",
				LongIntTypeName => "long",
				UnsignedLongIntTypeName => "ulong",
				RealTypeName => "double",
				LongLongTypeName => nameof(MpzT),
				UnsignedLongLongTypeName => nameof(MpuT),
				ComplexTypeName => "Complex",
				StringTypeName => nameof(String),
				"index" => nameof(Index),
				"range" => nameof(Range),
				RecursiveTypeName => "Type",
				"universal" => ObjectTypeName,
				_ => type.MainType.Peek().Name,
			};
		}
		else if (type.MainType.Equals(FuncBlockStack))
		{
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType ReturnNStarType)
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				return DynamicName;
			}
			var noReturn = TypeEqualsToPrimitive(ReturnNStarType, NullString);
			result.AddRange(nameof(ConstructFuncType)).Add('(');
			result.AddRange(TypeReflected(ref ReturnNStarType, branch, ref errors));
			if (type.ExtraTypes.Length >= 3)
				result.AddRange(", new Type[] { ");
			else if (type.ExtraTypes.Length == 2)
				result.AddRange(", ");
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType
					? ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors) : TypeReflected(ref InnerNStarType, branch, ref errors));
				AddRange(ref errors, innerErrors);
				if (!(noReturn && i == type.ExtraTypes.Length - 1))
					result.AddRange(", ");
			}
			if (type.ExtraTypes.Length >= 3)
				result.AddRange(" }.").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("()");
			result.Add(')');
		}
		else
		{
			result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(type.MainType.FindLastIndex(x =>
				x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface)) + 1))
				.ToString()));
			if (result == nameof(G.IEnumerable<>))
				result.Insert(0, "G.");
			if (result == nameof(ListHashSet<>) && type.ExtraTypes.Length == 1
				&& (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType))
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				return "typeof(dynamic)";
			}
			if (type.ExtraTypes.Length == 0)
				return result;
			result.Insert(0, OpeningTypeof).AddRange("<>).MakeGenericType(");
			for (var i = 0; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType
					? type.ExtraTypes[i].Name : TypeReflected(ref InnerNStarType, branch, ref errors));
				if (i != type.ExtraTypes.Length - 1)
					result.AddRange(", ");
			}
			result.Add(')');
		}
		return result;
	}

	private void ProcessSingularTuple(int number, bool @bool, String structName)
	{
		var internalNumber = @bool ? GetArrayLength(number, BitsPerInt) : number;
		compiledClasses.AddRange("[InlineArray(").AddRange("" + internalNumber);
		compiledClasses.AddRange(")] public struct ").AddRange(@structName);
		var generic = @bool ? "" : "<T>";
		compiledClasses.AddRange(generic);
		var innerTypeName = @bool ? "uint" : "T";
		var actualTypeName = @bool ? "bool" : "T";
		compiledClasses.AddRange("{private ").AddRange(innerTypeName).AddRange(" _element0;");
		if (number <= MaxInitializerLength)
		{
			compiledClasses.AddRange("private ").AddRange(@structName).AddRange("((");
			var typeTuple = ((String)actualTypeName).AddRange(", ").Repeat(number - 1).AddRange(actualTypeName);
			compiledClasses.AddRange(typeTuple);
			compiledClasses.AddRange(") x) => (");
			var thisTuple = String.Join(", ", new Chain(number).Convert(x => "this[" + x + (@bool ? ", false" : "") + ']'));
			compiledClasses.AddRange(thisTuple).AddRange(") = x;public static implicit operator ");
			compiledClasses.AddRange(@structName).AddRange(generic).AddRange("((");
			compiledClasses.AddRange(typeTuple).AddRange(") x) => new(x);");
		}
		if (@bool)
		{
			compiledClasses.AddRange("private const int _size = ").AddRange("" + number).Add(';');
			compiledClasses.AddRange("public bool this[Index index, bool _] { get => this[index.GetOffset(_size), _];");
			compiledClasses.AddRange(" set => this[index.GetOffset(_size), _] = value; }");
			compiledClasses.AddRange("public bool this[int index, bool _] {");
			compiledClasses.AddRange(" get => (MemoryMarshal.CreateReadOnlySpan(in _element0, ");
			compiledClasses.AddRange("" + internalNumber).AddRange(")[index >> 5] & 1u << (index & 31)) != 0;");
			compiledClasses.AddRange(" set { if (value) MemoryMarshal.CreateSpan(ref _element0, ");
			compiledClasses.AddRange("" + internalNumber).AddRange(")[index >> 5] |= 1u << (index % 32);");
			compiledClasses.AddRange(" else MemoryMarshal.CreateSpan(ref _element0, ");
			compiledClasses.AddRange("" + internalNumber).AddRange(")[index >> 5] &= ~(1u << (index % 32)); } }");
			compiledClasses.AddRange("public ").AddRange(nameof(BitList));
			compiledClasses.AddRange(" ToList() => new BitList(MemoryMarshal.CreateReadOnlySpan(in _element0, ");
			compiledClasses.AddRange("" + internalNumber).AddRange(")).Resize(_size);");
			compiledClasses.AddRange("public static implicit operator ").AddRange(nameof(BitList));
			compiledClasses.Add('(').AddRange(@structName).AddRange(generic).AddRange(" x) => x.ToList();");
		}
		else
		{
			compiledClasses.AddRange("public ").AddRange(nameof(List<>)).AddRange("<T>");
			compiledClasses.AddRange(" ToList() => MemoryMarshal.CreateReadOnlySpan(in _element0, ");
			compiledClasses.AddRange("" + internalNumber).AddRange(").").AddRange(nameof(RedStarLinq.ToList)).AddRange("();");
			compiledClasses.AddRange("public static implicit operator ").AddRange(nameof(List<>)).AddRange(generic);
			compiledClasses.Add('(').AddRange(@structName).AddRange(generic).AddRange(" x) => x.ToList();");
		}
		compiledClasses.AddRange("public override bool Equals(object obj) => ToList().Equals(obj);");
		compiledClasses.AddRange("public override int GetHashCode() => ToList().GetHashCode(); }");
		C.InlineArrays[@bool ? ~number : number] = (@structName, true);
	}

	private void DictionaryToFunc(TreeBranch branch, ref List<String>? errors, NStarType DictionaryNStarType)
	{
		TreeBranch key = new("key", branch.Pos, branch.Container)
		{
			Extra = DictionaryNStarType.ExtraTypes[1].Extra
		};
		TreeBranch lambda = new(nameof(Lambda), new(nameof(Hypername), key))
		{
			Extra = DictionaryNStarType
		};
		TreeBranch switchExpr = new(nameof(SwitchExpr), key)
		{
			Extra = DictionaryNStarType.ExtraTypes[0].Extra
		};
		lambda.Add(switchExpr);
		TreeBranch @switch = new("switch", branch.Pos, branch.Container);
		switchExpr.Add(@switch);
		for (var i = 0; i < branch.Length; i++)
		{
			Debug.Assert(branch[i].Length == 2);
			if (branch[i][0].Name == nameof(Hypername) && branch[i][0].Length == 1
				&& PrimitiveTypes.ContainsKey(branch[i][0][0].Name))
			{
				if (branch[i][0][0].Name == RecursiveTypeName)
				{
					var otherPos = branch[i][0].FirstPos;
					GenerateMessage(ref errors, 0x4093, otherPos);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return;
				}
				NStarType primitiveType = (new([new(PrimitiveTypes.ContainsKey(branch[i][0][0].Name)
					? BlockType.Primitive : BlockType.Extra, branch[i][0][0].Name, 1)]), NoBranches);
				branch[i][0] = new(Pattern, new("type", branch[i][0].Pos, branch[i][0].Container) { Extra = primitiveType });
			}
			@switch.Add(new("case", branch[i].Elements));
		}
		branch.Replace(lambda);
	}

	private void DictionaryToFuncDictionary(TreeBranch branch, ref List<String>? errors, NStarType DictionaryNStarType)
	{
		for (var i = 0; i < branch.Length; i++)
		{
			Debug.Assert(branch[i].Length == 2);
			if (branch[i][0].Name == nameof(Hypername) && branch[i][0].Length == 1
				&& PrimitiveTypes.ContainsKey(branch[i][0][0].Name))
			{
				if (branch[i][0][0].Name == RecursiveTypeName)
				{
					var otherPos = branch[i][0].FirstPos;
					GenerateMessage(ref errors, 0x4093, otherPos);
					branch.Replace(new(NullString, branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return;
				}
				NStarType primitiveType = (new([new(PrimitiveTypes.ContainsKey(branch[i][0][0].Name)
					? BlockType.Primitive : BlockType.Extra, branch[i][0][0].Name, 1)]), NoBranches);
				branch[i][0] = new(Pattern, new("type", branch[i][0].Pos, branch[i][0].Container) { Extra = primitiveType });
			}
			if (!(branch[i][0].Name.AsSpan() is nameof(Declaration) or Pattern && branch[i][0].Length != 0
				&& DictionaryNStarType.ExtraTypes[0].Extra is NStarType KeyNStarType))
				continue;
			String restrictionName = default!;
			if (branch.Length == 1 && KeyNStarType.MainType.Equals(RecursiveBlockStack)
				&& DictionaryNStarType.ExtraTypes[1].Extra is NStarType ValueNStarType
				&& ValueNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Other
				&& block.Name == nameof(Class) && branch.Parent is not null && branch.Parent.Length == 3
				&& ReferenceEquals(branch.Parent[0], branch) && branch.Parent[1].Name == nameof(Declaration)
				&& branch.Parent[1].Length == 2 && branch.Parent[1][0].Name == "type"
				&& DictionaryNStarType.Equals(branch.Parent[1][0].Extra) && branch.Parent[1][1].Name is var className
				&& C.UnnamedTypeStartIndexes.TryGetValue(branch.Container, out var containerStartIndexes)
				&& containerStartIndexes.Find(x => int.TryParse(x[1..].ToString(), out var otherUnnamedIndex)
				&& otherUnnamedIndex == unnamedIndex) is var startIndex && startIndex is not null
				&& C.UserDefinedTypes.TryGetValue((branch.Container, startIndex), out var userDefinedType)
				&& (branch[i][0].Name == nameof(Declaration) && branch[i][0].Length == 2
				? restrictionName = branch[i][0][1].Name : branch[i][0].Name == Pattern && branch[i][0].Length == 3
				&& branch[i][0][2].Name == "or" && branch[i][0][1].Name == NullString && branch[i][0][0].Name == "type"
				&& branch[i][0][0].Extra is NStarType RecursiveNStarType
				&& RecursiveNStarType.MainType.Equals(RecursiveBlockStack)
				&& CheckContainer(branch.Container, stack => C.TempTypes.TryGetValue(stack, out var containerTempTypes)
				&& containerTempTypes.Find(x => branch[i][0][0].Pos >= x.StartPos && branch[i][0][0].Pos < x.EndPos)
				is var found && (restrictionName = found.Name) is not null, out _) ? restrictionName : [])
				is not null && branch[i][1].Name == ClassMain)
			{
				userDefinedType.Restrictions?.Insert(0, new ExtendedRestriction(false, KeyNStarType, restrictionName));
				startIndex.Replace(className);
				branch.Parent.Replace(new(nameof(Class),
					new(className, branch.Parent[1][1].Pos, branch.Parent[1][1].Container)));
				var baseType = userDefinedType.BaseType;
				if (!baseType.Equals(NullType))
				{
					var baseTypeBranch = ValueNStarType.ExtraTypes[0];
					branch.Parent.Add(new("type", baseTypeBranch.Pos, baseTypeBranch.Container) { Extra = baseType });
				}
				branch.Parent.Add(branch[i][1]);
				return;
			}
			var keyName = C.TempTypes.TryGetValue(branch[i].Container, out var containerTempTypes)
				&& containerTempTypes.Find(x => branch[i].Pos >= x.StartPos && branch[i].Pos < x.EndPos)
				is var kvp && kvp.Name is not null ? kvp.Name : "key";
			TreeBranch key = new("key", branch[i].Pos, branch[i].Container) { Extra = KeyNStarType };
			TreeBranch namedKey = new(keyName, branch[i].Pos, branch[i].Container) { Extra = KeyNStarType };
			NStarType KeyFuncNStarType = new(FuncBlockStack,
				new([new("type", branch[i].Pos, branch[i].Container) { Extra = BoolType }, DictionaryNStarType.ExtraTypes[0]]));
			branch[i][0] = new(nameof(Lambda),
				[new(nameof(Hypername), key), new(nameof(Expr),
				[new(nameof(Hypername), key), branch[i][0], new("is", branch[i].Pos, branch[i].Container)])])
			{
				Extra = KeyFuncNStarType
			};
			NStarType ValueFuncNStarType = new(FuncBlockStack,
				new([DictionaryNStarType.ExtraTypes[1], DictionaryNStarType.ExtraTypes[0]]));
			branch[i][1] = new(nameof(Lambda), [new(nameof(Hypername), namedKey), branch[i][1]])
			{
				Extra = ValueFuncNStarType
			};
		}
		branch.Replace(new(nameof(Hypername), [new("new type", branch.Pos, branch.Container)
		{
			Extra = DictionaryNStarType
		}, new(nameof(ConstructorCall), branch.Elements)]));
	}

	private void ClassMainToPolymorphClass(TreeBranch branch, ref List<String>? errors, NStarType DictionaryNStarType)
	{
		var i = 0;
		if (!(DictionaryNStarType.MainType.Equals(DictionaryBlockStack) && DictionaryNStarType.ExtraTypes.Length == 2
			&& DictionaryNStarType.ExtraTypes[0].Name == "type"
			&& DictionaryNStarType.ExtraTypes[0].Extra is NStarType KeyNStarType))
			throw new InvalidOperationException();
		List<String> restrictionNames = default!;
		if (branch.Length == 1 && DictionaryNStarType.ExtraTypes[1].Extra is NStarType ValueNStarType
			&& ValueNStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Other
			&& block.Name == nameof(Class) && branch.Parent is not null && branch.Parent.Length == 3
			&& ReferenceEquals(branch.Parent[0], branch) && branch.Parent[1].Name == nameof(Declaration)
			&& branch.Parent[1].Length == 2 && branch.Parent[1][0].Name == "type"
			&& DictionaryNStarType.Equals(branch.Parent[1][0].Extra) && branch.Parent[1][1].Name is var className
			&& C.UnnamedTypeStartIndexes.TryGetValue(branch.Container, out var containerStartIndexes)
			&& containerStartIndexes.Find(x => int.TryParse(x[1..].ToString(), out var otherUnnamedIndex)
			&& otherUnnamedIndex == unnamedIndex) is var startIndex && startIndex is not null
			&& C.UserDefinedTypes.TryGetValue((branch.Container, startIndex), out var userDefinedType)
			&& CheckContainer(branch.Container, stack => C.TempTypes.TryGetValue(stack, out var containerTempTypes)
			&& containerTempTypes.FindAll(x => branch[i].Pos >= x.StartPos && branch[i].Pos < x.EndPos) is var found
			&& found.Length != 0 && (restrictionNames = found.ToList(x => x.Name)) is not null, out _)
			&& branch[i].Name == ClassMain)
		{
			userDefinedType.Restrictions?.Insert(0, KeyNStarType.MainType.Equals(TupleBlockStack)
				? restrictionNames.Combine(KeyNStarType.ExtraTypes
				.Filter(x => x.Name == "type" && x.Extra is NStarType))
				.Convert(x => new ExtendedRestriction(false, (NStarType)x.Item2.Extra!, x.Item1))
				: [new ExtendedRestriction(false, KeyNStarType, restrictionNames[0])]);
			startIndex.Replace(className);
			branch.Parent.Replace(new(nameof(Class),
				new(className, branch.Parent[1][1].Pos, branch.Parent[1][1].Container)));
			var baseType = userDefinedType.BaseType;
			if (!baseType.Equals(NullType))
			{
				var baseTypeBranch = ValueNStarType.ExtraTypes[0];
				branch.Parent.Add(new("type", baseTypeBranch.Pos, baseTypeBranch.Container) { Extra = baseType });
			}
			branch.Parent.Add(branch[i]);
		}
	}

	private bool VariableExists(TreeBranch branch, String name, ref List<String>? errors)
	{
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		while (branch.Parent is not null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == Parameter)
					&& branches[i][j][1].Name == name && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					var otherPos = branches[i][j].FirstPos;
					return Error(ref errors, otherPos);
				}
				else if (BranchesToSearchDeeper.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2)
					&& VariableExistsInsideExpr(branches[i][j], name, out var otherPos, out _)
					&& !(i == indexes.Length - 1 && j >= indexes[^1]))
					return Error(ref errors, otherPos);
			}
			if (branches[i].Name == nameof(Function))
				break;
		}
		return false;
		bool Error(ref List<String>? errors, int otherPos)
		{
			GenerateMessage(ref errors, 0x4013, preservedBranch.Pos, name,
				lexems[otherPos].LineN.ToString(), lexems[otherPos].Pos.ToString());
			return true;
		}
	}

	private bool IsVariableDeclared(TreeBranch branch, String name, out List<String>? errors, out object? extra)
	{
		errors = default;
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		var parent = branch;
		while (parent.Parent is not null)
		{
			indexes.Add(parent.Parent.Elements.FindIndex(x => ReferenceEquals(parent, x)) + 1);
			branches.Add(parent = parent.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& C.UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _)
				&& (functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
			{
				GenerateMessage(ref errors, 0x4010, preservedBranch.FirstPos, name);
				extra = null;
				return false;
			}
			for (var j = 0; j < indexes[i] - 1; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == Parameter)
					&& branches[i][j][1].Name == name)
				{
					extra = branches[i][j][0].Extra;
					return true;
				}
				else if (branches[i].Name == nameof(Lambda) && branches[i].Length == 2
					&& (IsValidLambdaParameter(branches[i][0], name, out var innerExtra) || branches[i][0].Name == nameof(List)
					&& branches[i][0].Elements.Any(x => IsValidLambdaParameter(x, name, out innerExtra))))
				{
					extra = innerExtra;
					return true;
				}
				else if (BranchesToSearchDeeperNoReturn.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2
					|| branches[i].Elements[(j + 1)..(indexes[i] - 1)].All(x => x.Name.AsSpan() is "if" or "if!"))
					&& VariableExistsInsideExpr(branches[i][j], name, out _, out innerExtra))
				{
					extra = innerExtra;
					return true;
				}
			}
			for (var j = indexes[i]; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == Parameter)
					&& branches[i][j][1].Name == name)
				{
					var otherPos = branches[i][j].FirstPos;
					return Error(ref errors, out extra, otherPos);
				}
				else if (BranchesToSearchDeeper.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2)
					&& VariableExistsInsideExpr(branches[i][j], name, out var otherPos, out _))
					return Error(ref errors, out extra, otherPos);
			}
		}
		if (errors is null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		extra = null;
		return false;
		static bool IsValidLambdaParameter(TreeBranch branch, String branchName, out object? extra)
		{
			if (branch.Length == 0)
			{
				extra = branch.Extra;
				return branch.Name == branchName;
			}
			if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
			{
				extra = null;
				return false;
			}
			extra = branch.Extra;
			return branch[0].Name == branchName;
		}
		bool Error(ref List<String>? errors, out object? extra, int otherPos)
		{
			GenerateMessage(ref errors, 0x4012, preservedBranch.FirstPos, name,
				lexems[otherPos].LineN.ToString(), lexems[otherPos].Pos.ToString());
			extra = null;
			return false;
		}
	}

	private static bool VariableExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Name == nameof(Declaration) || branch[i].Name == Parameter) && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (ExprTypesToSearchDeeper.Contains(branch[i].Name.ToString())
					&& VariableExistsInsideExpr(branch[i], name, out pos, out extra))
					return true;
			}
		}
		catch (StackOverflowException)
		{
		}
		pos = -1;
		extra = null;
		return false;
	}

	private bool IsPropertyDeclared(TreeBranch branch, String name, out List<String>? errors,
		out UserDefinedProperty? property, out bool inBase, out BlockStack actualContainer)
	{
		errors = default;
		(BlockStack Container, String Type) matchingKey = default;
		if (CheckContainer(branch.Container, x => C.UserDefinedTypes.ContainsKey(matchingKey = SplitType(x)),
			out _) && CreateVar(CreateVar(C.UserDefinedTypes[matchingKey].Restrictions, out var restrictions)
			?.FindIndex(x => x.Name == name) ?? -1, out var foundIndex) >= 0)
		{
			property = new(restrictions[foundIndex].RestrictionType, PropertyAttributes.Required, []);
			inBase = false;
			actualContainer = branch.Container;
			return true;
		}
		else if (!C.UserDefinedPropertyExists(branch.Container, name, false, out property, out _, out inBase,
			out actualContainer))
		{
			if (errors is null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			return false;
		}
		else if (inBase)
			return true;
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		while (branch.Parent is not null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& C.UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _))
			{
				if ((functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
				{
					GenerateMessage(ref errors, 0x4031, preservedBranch.FirstPos, name);
					return false;
				}
				else if ((functions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (property?.Attributes & PropertyAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4032, preservedBranch.FirstPos, name);
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Name == nameof(Property) && branches[i][j].Length == 3 && branches[i][j][1].Name == name)
				{
					return true;
				}
				else if ((branches[i][j].Name == ClassMain || branches[i][j].Name == "Members")
					&& PropertyExistsInsideExpr(branches[i][j], name, out _, out _))
				{
					return true;
				}
			}
		}
		if (errors is null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		return false;
	}

	private bool IsConstantDeclared(TreeBranch branch, String name, out List<String>? errors, out UserDefinedConstant? constant)
	{
		errors = default;
		if (!C.UserDefinedConstantExists(branch.Container, name, out constant, out _, out var inBase))
		{
			if (errors is null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			return false;
		}
		else if (inBase)
			return true;
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		while (branch.Parent is not null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& C.UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _))
			{
				if ((functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
				{
					GenerateMessage(ref errors, 0x4031, preservedBranch.FirstPos, name);
					return false;
				}
				else if ((functions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (constant?.Attributes & ConstantAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4032, preservedBranch.FirstPos, name);
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == Parameter)
					&& branches[i][j][1].Name == name)
					return true;
				else if (branches[i].Name == nameof(Lambda) && branches[i].Length == 2
					&& (IsValidLambdaParameter(branches[i][0], name) || branches[i][0].Name == nameof(List)
					&& branches[i][0].Elements.Any(x => IsValidLambdaParameter(x, name))))
					return true;
				else if (BranchesToSearchDeeperNoReturn.Contains(branches[i][j].Name.ToString())
					&& ConstantExistsInsideExpr(branches[i][j], name, out _, out _))
					return true;
				if (branches[i][j].Name == nameof(Constant) && branches[i][j].Length == 3 && branches[i][j][1].Name == name)
					return true;
				else if ((branches[i][j].Name == ClassMain || branches[i][j].Name == "Members")
					&& ConstantExistsInsideExpr(branches[i][j], name, out _, out _))
					return true;
			}
		}
		if (errors is null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		return false;
		static bool IsValidLambdaParameter(TreeBranch branch, String branchName)
		{
			if (branch.Length == 0)
				return branch.Name == branchName;
			if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
				return false;
			return branch[0].Name == branchName;
		}
	}

	private static bool PropertyExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == nameof(Property) && branch[i].Length == 3 && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
			}
		}
		catch (StackOverflowException)
		{
		}
		pos = -1;
		extra = null;
		return false;
	}

	private static bool ConstantExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == nameof(Constant) && branch[i].Length == 3 && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
			}
		}
		catch (StackOverflowException)
		{
		}
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Name == nameof(Declaration) || branch[i].Name == Parameter) && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (ExprTypesToSearchDeeper.Contains(branch[i].Name.ToString())
					&& ConstantExistsInsideExpr(branch[i], name, out pos, out extra))
					return true;
			}
		}
		catch (StackOverflowException)
		{
		}
		pos = -1;
		extra = null;
		return false;
	}

	private bool IsFunctionDeclared(TreeBranch branch, String name, out List<String>? errors,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out object? extra)
	{
		errors = default;
		if (!C.UserDefinedNonDerivedFunctionExists(branch.Container, name, out functions, out matchingContainer))
		{
			if (errors is null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			extra = null;
			return false;
		}
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		while (branch.Parent is not null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Name == nameof(Function) && branches[i].Length >= 3
				&& C.UserDefinedNonDerivedFunctionExists(branches[i].Container,
				branches[i][0].Name, out var innerFunctions, out _))
			{
				if ((innerFunctions[^1].Attributes & FunctionAttributes.Multiconst) != 0
					&& (functions[^1].Attributes & FunctionAttributes.Multiconst) == 0)
				{
					GenerateMessage(ref errors, 0x4024, preservedBranch.FirstPos, name);
					extra = null;
					return false;
				}
				else if ((innerFunctions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (functions[^1].Attributes & FunctionAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4025, preservedBranch.FirstPos, name);
					extra = null;
					return false;
				}
				else if (branches[i][0].Name == name)
				{
					extra = branches[i];
					return true;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Name == nameof(Function) && branches[i][j].Length >= 3 && branches[i][j][0].Name == name)
				{
					extra = branches[i][j];
					return true;
				}
			}
		}
		if (errors is null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		extra = null;
		return false;
	}

	private static UserDefinedMethodOverload ConstructorToFunction(TreeBranch branch, ConstructorOverload x) =>
		new([], [], new(branch.Container, []), FunctionAttributes.None, x.Parameters, x.Location);

	private static bool ContainsDeclarations(TreeBranch branch)
	{
		if (branch.Name == nameof(Declaration))
			return true;
		for (var i = 0; i < branch.Length; i++)
			if (ContainsDeclarations(branch[i]))
				return true;
		return false;
	}

	private bool ContainsGUITypes(TreeBranch branch)
	{
		if (branch.Extra is NStarType NStarType && C.IsGUIType(NStarType))
			return true;
		for (var i = 0; i < branch.Length; i++)
			if (ContainsGUITypes(branch[i]))
				return true;
		return false;
	}

	private static bool ContainsTypes(TreeBranch branch)
	{
		if (branch.Name == "type")
			return true;
		for (var i = 0; i < branch.Length; i++)
			if (ContainsTypes(branch[i]))
				return true;
		return false;
	}

	private void DetectIndirectRecursion(TreeBranch branch, BlockStack container,
		ConstructorOverloads constructors, ref List<String>? errors)
	{
		var constructor = ConstructorToFunction(branch, constructors[^1]);
		if (parsingFunctions.Length != 0 && parsingFunctions[^1].Container.Equals(container)
			&& parsingFunctions[^1].Value == constructor) { }
		else if (parsingFunctions.LastIndexOf((container, nameof(Constructor), constructor)) is var recursionIndex && recursionIndex >= 0)
		{
			var recursion = parsingFunctions.GetSlice(recursionIndex);
			List<String> joined;
			if (recursion.AllUnique(x => x.Name))
				joined = recursion.ToList(x => x.Name.Copy().AddRange("()"));
			else
			{
				joined = [];
				foreach (var (Container, Name, Value) in recursion)
				{
					var parameters = String.Join(", ", Value.Parameters.ToArray(x => x.Type.ToString()));
					joined.Add(Name.Copy().Add('(').AddRange(parameters).Add(')'));
				}
			}
			GenerateMessage(ref errors, 0x801D, branch.Pos, String.Join(", ", joined[..^1]), joined[^1]);
			unoptimizableFunctions.AddRange(recursion);
		}
		else if (constructor.Location is not null)
		{
			ParseAction(constructor.Location.Name)(constructor.Location, out var innerErrors);
			AddRange(ref errors, innerErrors);
		}
	}

	private void DetectIndirectRecursion(TreeBranch branch, BlockStack container, String name,
		UserDefinedMethodOverloads functions, ref List<String>? errors)
	{
		if (parsingFunctions.Length != 0 && parsingFunctions[^1].Container.Equals(container)
			&& parsingFunctions[^1].Value == functions[^1])
		{
			if (recursiveFunctions.TryAdd(parsingFunctions[^1])
				&& (functions[^1].Attributes & FunctionAttributes.IO) == 0)
				recursiveFunctionLocations.Add(branch);
			else
				unoptimizableFunctions.Add(parsingFunctions[^1]);
		}
		else if (parsingFunctions.LastIndexOf((container, name, functions[^1])) is var recursionIndex && recursionIndex >= 0)
		{
			var recursion = parsingFunctions.GetSlice(recursionIndex);
			List<String> joined;
			if (recursion.AllUnique(x => x.Name))
				joined = recursion.ToList(x => x.Name.Copy().AddRange("()"));
			else
			{
				joined = [];
				foreach (var (Container, Name, Value) in recursion)
				{
					var parameters = String.Join(", ", Value.Parameters.ToArray(x => x.Type.ToString()));
					joined.Add(Name.Copy().Add('(').AddRange(parameters).Add(')'));
				}
			}
			GenerateMessage(ref errors, 0x801D, branch.Pos, String.Join(", ", joined[..^1]), joined[^1]);
			unoptimizableFunctions.AddRange(recursion);
		}
		else if (functions[^1].Location is not null)
		{
			ParseAction(functions[^1].Location!.Name)(functions[^1].Location!, out var innerErrors);
			AddRange(ref errors, innerErrors);
		}
	}

	private static bool IsAnyAssignment(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch assignmentBranch,
		out int assignmentIndex)
	{
		var parent = branch.Parent;
		while (parent is not null)
		{
			if (parent.Name.AsSpan() is nameof(Assignment) or DeclarationAssignment or UnaryAssignment or nameof(List))
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				assignmentBranch = parent;
				assignmentIndex = Max(prevIndex + 1, 2);
				return true;
			}
			parent = parent.Parent;
		}
		assignmentBranch = null;
		assignmentIndex = -1;
		return false;
	}

	private static bool IsAssignment(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch assignmentBranch,
		out int assignmentIndex)
	{
		var parent = branch.Parent;
		while (parent is not null)
		{
			if (parent.Name.AsSpan() is nameof(Assignment) or UnaryAssignment)
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				assignmentBranch = parent.Name == UnaryAssignment ? new("", 0, []) : parent;
				assignmentIndex = Max(prevIndex + 1, 2);
				return true;
			}
			parent = parent.Parent;
		}
		assignmentBranch = null;
		assignmentIndex = -1;
		return false;
	}

	private static bool IsCall(TreeBranch branch)
	{
		if (branch.Name != nameof(Hypername) || branch.Length == 0)
			return false;
		if (branch[^1].Name.AsSpan() is nameof(Call) or nameof(ConstructorCall))
			return true;
		if (IsCall(branch[^1]))
			return true;
		return false;
	}

	private bool IsConstructor(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch constructorBranch,
		[MaybeNullWhen(false)] out ConstructorOverloads overloads)
	{
		var parent = branch.Parent;
		while (parent is not null)
		{
			if (parent.Name != nameof(Constructor))
			{
				parent = parent.Parent;
				continue;
			}
			constructorBranch = parent;
			if (parent.Length == 0 || parent[0].Elements.Any(x => x.Length == 0)
				|| !C.UserDefinedConstructors.TryGetValue(parent.Container, out overloads))
			{
				constructorBranch = null;
				overloads = null;
				return false;
			}
			overloads = [overloads.FindLast(x => parent[0].Elements.Length == x.Parameters.Length
				&& parent[0].Elements.Combine(x.Parameters)
				.All(x => x.Item1[0].Extra is NStarType NStarType && NStarType.Equals(x.Item2.Type)))];
			return true;
		}
		constructorBranch = null;
		overloads = null;
		return false;
	}

	private static bool IsPattern(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch patternBranch,
		out int patternIndex)
	{
		var parent = branch.Parent;
		while (parent is not null)
		{
			if (parent.Name == nameof(Expr) && parent.Length >= 3 && parent[^1].Name == "is")
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				patternBranch = parent;
				patternIndex = Max(prevIndex + 1, 2);
				return true;
			}
			branch = parent;
			parent = parent.Parent;
		}
		patternBranch = null;
		patternIndex = -1;
		return false;
	}

	private static bool IsSwitchPattern(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch patternBranch,
		out int patternIndex)
	{
		var parent = branch.Parent;
		while (parent is not null)
		{
			if (parent.Name == nameof(SwitchExpr) && parent.Length == 2 && parent[0].Extra is NStarType NStarType)
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				patternBranch = parent;
				patternIndex = Max(prevIndex + 1, 2);
				if (NStarType.Equals(StringType))
					parent[0].Extra = UnsafeStringType;
				return true;
			}
			branch = parent;
			parent = parent.Parent;
		}
		patternBranch = null;
		patternIndex = -1;
		return false;
	}

	private bool TypesAreCompatible(TreeBranch branch, ref List<String>? errors,
		NStarType sourceType, NStarType destinationType,
		out bool warning, String? srcExpr, out String? destExpr, out String? extraMessage)
	{
		warning = false;
		extraMessage = null;
		List<String>? innerErrors = null;
		var destinationTypeString = destinationType.MainType.ToString();
		if ((TypeEqualsToPrimitive(destinationType, "list", false) || destinationType.MainType.Length != 0
			&& destinationType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
			&& CollectionTypesList.Contains(item: destinationTypeString.ToNString().GetAfterLast(".")))
			&& TypeEqualsToPrimitive(sourceType, TupleName, false))
		{
			var subtype = GetSubtype(C, destinationType);
			if (subtype.Equals(sourceType))
			{
				destExpr = srcExpr;
				return true;
			}
			if (sourceType.ExtraTypes.Length > 16)
			{
				destExpr = DefaultNull;
				extraMessage = "list can be constructed from tuple of up to 16 elements,"
					+ " if you need more, use the other ways like Chain() or Fill()";
				return false;
			}
			var lambdaVarName = RandomVarName();
			var itemName = new String[sourceType.ExtraTypes.Length];
			if (srcExpr is not null)
			{
				itemName[0] = ((String)nameof(CreateVar)).Add('(').AddRange(srcExpr).AddRange(", out var ");
				itemName[0].AddRange(lambdaVarName).AddRange(").Item1");
				for (var i = 1; i < itemName.Length; i++)
					itemName[i] = lambdaVarName.ToNString().AddRange(".Item").AddRange((i + 1).ToString());
			}
			var adaptedItemName = new String[sourceType.ExtraTypes.Length];
			if (!sourceType.ExtraTypes.All((x, index) => x.Name == "type" && x.Extra is NStarType ValueType
				&& TypesAreCompatible(branch, ref innerErrors, ValueType, subtype, out var innerWarning,
				itemName[index], out adaptedItemName[index]!, out _) && !innerWarning
				|| x.Length == 0 && int.TryParse(x.Name.AsSpan(), out _)))
			{
				AddRange(ref errors, innerErrors);
				destExpr = DefaultNull;
				return false;
			}
			AddRange(ref errors, innerErrors);
			if (srcExpr is null)
			{
				destExpr = null;
				return true;
			}
			destExpr = ((String)"(").AddRange(nameof(List<>)).Add('<');
			destExpr.AddRange(Type(ref subtype, branch, ref errors)).AddRange(">)(").AddRange(srcExpr).Add(')');
			if (!((sourceType.ExtraTypes.AllEqual() ? sourceType.ExtraTypes.Length
				: sourceType.ExtraTypes.Length == 2 && sourceType.ExtraTypes[1].Length == 0
				&& int.TryParse(sourceType.ExtraTypes[1].Name.AsSpan(), out var n)
				? n : -1) is var tupleLength && tupleLength >= 0
				&& sourceType.ExtraTypes[0].Name == "type" && sourceType.ExtraTypes[0].Extra is NStarType ItemNStarType
				&& C.InlineArrays.ContainsKey(ItemNStarType.Equals(BoolType) ? ~tupleLength : tupleLength)))
				return true;
			destExpr.AddRange(".ToList()");
			if (RedStarLinq.Equals(itemName, adaptedItemName))
				return true;
			if (!TypesAreCompatible(branch, ref errors, ItemNStarType, subtype, out warning,
				new(lambdaVarName), out var adaptedLambdaVarName, out _) || warning || adaptedLambdaVarName is null)
			{
				AddRange(ref errors, innerErrors);
				destExpr = DefaultNull;
				return false;
			}
			destExpr.Add('.').AddRange(nameof(RedStarLinq.ToList)).Add('(').AddRange(lambdaVarName).AddRange(" => ");
			destExpr.AddRange(adaptedLambdaVarName).Add(')');
			return true;
		}
		//if (TypeEqualsToPrimitive(destinationType, TupleName, false))
		//{
		//	if (!TypeEqualsToPrimitive(sourceType, TupleName, false))
		//	{
		//		destExpr = DefaultNull;
		//		return false;
		//	}
		//	if (sourceType.ExtraTypes.Length != destinationType.ExtraTypes.Length)
		//	{
		//		destExpr = DefaultNull;
		//		return false;
		//	}
		//	var lambdaVarName = RandomVarName();
		//	var itemName = new String[sourceType.ExtraTypes.Length];
		//	if (srcExpr is not null)
		//	{
		//		itemName[0] = ((String)nameof(CreateVar)).Add('(').AddRange(srcExpr).AddRange(", out var ");
		//		itemName[0].AddRange(lambdaVarName).AddRange(").Item1");
		//		for (var i = 1; i < itemName.Length; i++)
		//			itemName[i] = lambdaVarName.ToNString().AddRange(".Item").AddRange((i + 1).ToString());
		//	}
		//	var adaptedItemName = new String[sourceType.ExtraTypes.Length];
		//	var result = sourceType.ExtraTypes.Combine(destinationType.ExtraTypes.Values)
		//		.All((x, index) => x.Item1.Name == "type" && x.Item1.Extra is NStarType LeftType
		//		&& x.Item2.Name == "type" && x.Item2.Extra is NStarType RightType
		//		&& TypesAreCompatible(branch, ref innerErrors, LeftType, RightType, out var innerWarning,
		//		itemName[index], out adaptedItemName[index]!, out _)
		//		&& !innerWarning);
		//	AddRange(ref errors, innerErrors);
		//	if (srcExpr is null)
		//	{
		//		destExpr = null;
		//		return result;
		//	}
		//	if (RedStarLinq.Equals(itemName, adaptedItemName))
		//	{
		//		destExpr = srcExpr;
		//		return result;
		//	}
		//	destExpr = ((String)"(").AddRange(String.Join(", ", adaptedItemName)).Add(')');
		//	return result;
		//}
		return TypeConverters.TypesAreCompatible(C, sourceType, destinationType, out warning, srcExpr, out destExpr,
			out extraMessage);
	}

	private void WrapIntoUIThread(String result)
	{
		if (noAddAsync)
		{
			result.Insert(0, UIThreadNonAsyncPrefix).AddRange("))");
		}
		else
		{
			result.Insert(0, UIThreadAsyncPrefix).Add(')');
			containsAsync = true;
		}
	}

	private void GenerateMessage(ref List<String>? errors, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, lexems[pos].LineN, lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			errorOccurred = 2;
	}

	private static void Add<T>(ref List<T>? source, T item)
	{
		source ??= [];
		source.Add(item);
	}

	private static void AddRange<T>(ref List<T>? source, G.IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			source ??= [];
			source.AddRange(collection);
		}
	}

	private static bool IsTypeContext(TreeBranch branch) =>
		branch.Container.TryPeek(out var nearestBlock)
		&& nearestBlock.BlockType is BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface;

	private static void ClearUserDefinedLists()
	{
		//C.ExplicitlyConnectedNamespaces.Clear();
		ImportedNamespaces.Clear();
		ImportedTypes.Clear();
		//C.InlineArrays.Clear();
		//C.TempTypes.Clear();
		//C.UnnamedTypeStartIndexes.Clear();
		//C.UserDefinedConstants.Clear();
		//C.UserDefinedConstructors.Clear();
		//C.UserDefinedConstructorIndexes.Clear();
		//C.UserDefinedMethods.Clear();
		//C.UserDefinedFunctionIndexes.Clear();
		//C.UserDefinedImplementedInterfaces.Clear();
		//C.UserDefinedIndexers.Clear();
		//C.UserDefinedNamespaces.Clear();
		//C.UserDefinedProperties.Clear();
		//C.UserDefinedPropertiesMapping.Clear();
		//C.UserDefinedPropertiesOrder.Clear();
		//C.UserDefinedTypes.Clear();
		//C.Variables.Clear();
	}

	public static String CompileProgram(String program, List<string> packages)
	{
		try
		{
			ClearUserDefinedLists();
			var (sourceCode, _, translatedClasses) = TranslateProgram(program, packages);
			return GetSourceCode(sourceCode, translatedClasses);
		}
		catch
		{
			return [];
		}
	}

	private static (byte[] Bytes, List<String> ErrorsList) CompileProgram((String s, List<String>? errors,
		String translatedClasses) translated, List<String> packages)
	{
		var (sourceCode, errors, translatedClasses) = translated;
		var bytes = EasyEval.Compile(GetSourceCode(sourceCode, translatedClasses),
			[.. GetExtraAssemblies(), .. packages], out var compileErrors);
		if (bytes is null || bytes.Length <= 2 || compileErrors != "Compilation done without any error.\r\n")
			throw new EvaluationFailedException();
		return (bytes, errors ?? []);
	}

	public static String ExecuteProgram(String program, List<string> packages, out String errors, params dynamic?[] args) =>
		TranslateAndExecuteProgram(program, packages, out errors, out _, args);

	public static String ExecuteProgram((String s, List<String>? errors, String translatedClasses) translated,
		List<string> packages, out String errors, out Assembly? assembly, params dynamic?[] args)
	{
		var (bytes, errorsInListForm) = CompileProgram(translated, packages.ToList(RedStarLinq.ToNString));
		assembly = EasyEval.GetAssembly(bytes);
		var task = (Task<object>?)assembly?.GetType("Program")?.GetMethod("F")?.Invoke(null, [args]);
		var result = task is null ? NullString : JsonConvert.SerializeObject(AsyncContext.Run(async () =>
		{
			try
			{
				return await task;
			}
			catch (Exception ex)
			{
				return ProgramExecutionFailed(ref errorsInListForm, ex);
			}
		}), JsonConverters.SerializerSettings);
		errors = errorsInListForm is null || errorsInListForm.Length == 0 ? "Ошибок нет" :
			String.Join("\r\n", errorsInListForm.ToHashSet().Append([]));
		return result;
	}

	public static G.IEnumerable<String> GetExtraAssemblies() =>
		["Avalonia", "Avalonia.Base", "Avalonia.Controls", "Avalonia.Desktop", "Avalonia.FreeDesktop",
		"Avalonia.Fonts.Inter", "Avalonia.Markup.Xaml", "Avalonia.Markup.Xaml.Loader", "Avalonia.Native",
		"Avalonia.Remote.Protocol", "Avalonia.Skia", "Avalonia.Themes.Fluent", "Avalonia.Win32", "Avalonia.X11",
		"BuiltInMemberCollections", "BuiltInTypeCollections", "CodeSample", "DynamicData", "HarfBuzzSharp", "MainParsing",
		"MemberChecks", "MemberConverters", "MicroCom.Runtime", "Nito.AsyncEx.Context",
		"NStar.EasyEval", "NStarType", "NStarUtilityFunctions", "ObjectConverters",
		"PanAndZoom", "QuotesAndTreeBranch", "ReactiveUI", "ReactiveUI.Avalonia", "RedStarMath", "RedStarMath.Complex",
		"SemanticTree", "SkiaSharp", "Splat", "Splat.Builder", "Splat.Core", "Splat.Logging",
		"System.Collections", "System.Net.Primitives", "System.Net.Sockets",
		"System.ObjectModel", "System.Private.Uri", "System.Reactive",
		"System.Runtime.Numerics", "System.Text.Encoding.CodePages",
		"System.Threading.Tasks.Parallel", "Tmds.DBus.Protocol", "TranslateTimeOperations",
		"TypeChecks", "TypeConverters", "TypePromotionRules"];

	private static object? ProgramExecutionFailed(ref List<String>? errorsInListForm, Exception ex)
	{
		var errorMessage = ((String)ex.GetType().Name).GetBefore("`").AddRange(": ").AddRange(ex.Message).AddRange("\r\n");
		try
		{
			var targetLexem = TreeBranch.LastTreePos < 0 || lastLexems is null
				|| TreeBranch.LastTreePos >= lastLexems.Length
				? new([], LexemType.Int, 0, 0) : lastLexems[TreeBranch.LastTreePos];
			File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
				+ @"\PL051.NStar.log", [errorMessage.ToString(), "The last visited location was: line "
				+ targetLexem.LineN + ", position " + targetLexem.Pos, "The internal exception was:", ex.GetType().Name,
				"The internal exception message was:", ex.Message,
				"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? NullString,
				"The underlying internal exception message was:", ex.InnerException?.Message ?? NullString]);
			Add(ref errorsInListForm, errorMessage + @" (see %TEMP%\PL051.NStar.log for details)");
		}
		catch
		{
			Add(ref errorsInListForm,
				errorMessage + " (also could not write to the log, check your environment TEMP variable)");
		}
		return null;
	}

	public static String TranslateAndExecuteProgram(String program, List<string> packages, out String errors,
		out Assembly? assembly, params dynamic?[] args)
	{
		List<String>? errorsInListForm = null;
		try
		{
			ClearUserDefinedLists();
			var translated = TranslateProgram(program, packages);
			AddRange(ref errorsInListForm, translated.errors);
			return ExecuteProgram(translated, packages, out errors, out assembly, args);
		}
		catch (OutOfMemoryException)
		{
			Add(ref errorsInListForm, "Technical wreck F002 in unknown line at unknown position:" +
				" memory limit exceeded during compilation, translation or execution; program has not been executed\r\n");
			errors = String.Join("\r\n", errorsInListForm?.ToHashSet().Append([]) ?? []);
			assembly = null;
			return NullString;
		}
		catch (Exception ex)
		{
			const string errorMessage = "Technical wreck F003 in unknown line at unknown position:" +
				" a serious error occurred during compilation, translation or execution; program has not been executed\r\n";
			try
			{
				var targetLexem = TreeBranch.LastTreePos < 0 || lastLexems is null
					|| TreeBranch.LastTreePos >= lastLexems.Length
					? new([], LexemType.Int, 0, 0) : lastLexems[TreeBranch.LastTreePos];
				File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
					+ @"\PL051.NStar.log", [errorMessage, "The last visited location was: line " + targetLexem.LineN
				+ ", position " + targetLexem.Pos, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? NullString,
					"The underlying internal exception message was:", ex.InnerException?.Message ?? NullString]);
				Add(ref errorsInListForm, errorMessage + @" (see %TEMP%\PL051.NStar.log for details)");
			}
			catch
			{
				Add(ref errorsInListForm,
					errorMessage + " (also could not write to the log, check your environment TEMP variable)");
			}
			errors = String.Join("\r\n", errorsInListForm?.ToHashSet().Append([]) ?? []);
			assembly = null;
			return NullString;
		}
	}

	public static (String s, List<String>? errors, String translatedClasses) TranslateProgram(String program,
		List<string> packages)
	{
		List<String>? packageErrors = null;
		foreach (var package in packages)
		{
			try
			{
				AsyncContext.Run(async () => await DownloadPackage(package));
			}
			catch (NonExistentPackageException)
			{
				Messages.GenerateMessage(ref packageErrors, 0xF010, 0, 0, package);
				break;
			}
			catch (WrongSignatureException)
			{
				Messages.GenerateMessage(ref packageErrors, 0xF011, 0, 0, package);
				break;
			}
		}
		if (packageErrors is not null)
		{
			packages.Clear();
			return ([], packageErrors, []);
		}
		var s = new SemanticTree((LexemStream)new CodeSample(program)).Parse(out var errors, out var translatedClasses);
		return (s, errors, translatedClasses);
	}

	private static String GetSourceCode(String main, String translatedClasses) => ((String)@"using Avalonia;
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Animation)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.ApplicationLifetimes)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.PanAndZoom)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.Primitives)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Input)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Interactivity)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Layout)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Markup)).Add('.').AddRange(nameof(Avalonia.Markup.Xaml)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Media)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Media)).Add('.').AddRange(nameof(Avalonia.Media.Imaging)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Threading)).AddRange(@";
using PL051.NStar;
using ").AddRange(nameof(Nito)).Add('.').AddRange(nameof(Nito.AsyncEx)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.BufferLib)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Core)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Dictionaries)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Linq)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.MathLib)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.MathLib)).Add('.').AddRange(nameof(global::NStar.MathLib.Extras)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Mpir)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.RemoveDoubles)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.SumCollections)).AddRange(@";
using ").AddRange(nameof(ReactiveUI)).AddRange(@";
using ").AddRange(nameof(ReactiveUI)).Add('.').AddRange(nameof(ReactiveUI.Avalonia)).AddRange(@";
using RedStarMath;
using System;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;").AddRange(ImportedNamespaces.Filter(x => x.Length != 0).Convert(x => "using " + x + ";\r\n").ConvertAndJoin(x => x)).AddRange(@"
using static ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Core)).Add('.').AddRange(nameof(Extents)).AddRange(@";
using static ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.EasyEvalLib)).Add('.').AddRange(nameof(EasyEval)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(BuiltInMemberCollections)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(NStarUtilityFunctions)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(MemberConverters)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(Quotes)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(SemanticTree)).AddRange(@";
using static PL051.NStar.").AddRange(nameof(TypeConverters)).AddRange(@";
using static NStar.Mpir.").AddRange(nameof(MpzT)).AddRange(@";
using static RedStarMath.Complex;
using static System.Math;
using G = System.Collections.Generic;
using Complex = RedStarMath.Complex;
using Math = RedStarMath.Math;
using String = ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Core)).Add('.').AddRange(nameof(String)).AddRange(@";

").AddRange(translatedClasses).AddRange(@"
public static class Program
{
	public static string[] args = [];

public static async Task<dynamic?> F(params dynamic?[] args)
{
").AddRange(main).AddRange(""""

				return null;
			}

				public static void Main(string[] args)
				{
					Program.args = args;
					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
					var result = AsyncContext.Run(async () => await F(args));
					if (result is not null)
						Console.WriteLine(result);
				}

				// Avalonia configuration, don't remove; also used by visual designer.
				public static AppBuilder BuildAvaloniaApp()
					=> AppBuilder.Configure<App>()
					.UsePlatformDetect()
					.WithInterFont()
					.LogToTrace()
					.UseReactiveUI(x => { });
			}

			public partial class App : Application
			{
				public override void Initialize()
				{
					AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(this, """
						<Application xmlns="https://github.com/avaloniaui"
									 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
									 xmlns:fluent="clr-namespace:Avalonia.Themes.Fluent;assembly=Avalonia.Themes.Fluent"
									 x:Class="App"
									 RequestedThemeVariant="Light">
									 <!-- "Default" ThemeVariant follows system theme variant. "Dark" or "Light" are other available options. -->

							<Application.Styles>
								<fluent:FluentTheme />
							</Application.Styles>
						</Application>

						"""));
					dynamic? result = null;
					Task.Run(() =>
					{
						var thread = new System.Threading.Thread(() =>
							result = AsyncContext.Run(async () => await Program.F(Program.args)), int.MaxValue);
						thread.Start();
						thread.IsBackground = true;
						thread.Join();
						if (result is not null)
							Console.WriteLine(result);
					});
				}

				public override void OnFrameworkInitializationCompleted()
				{
					if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
						return;
					desktop.MainWindow = null;
					base.OnFrameworkInitializationCompleted();
				}
			}

			"""");

	private static bool TryReadValue(String s, [MaybeNullWhen(false)] out object value)
	{
		if (MainParsing.TryParse(s.ToString(), out value)
			|| s.EndsWith('d') && MainParsing.TryParse(s[..^1].ToString(), out value)
			|| s.StartsWith("(String)") && MainParsing.TryParse(s["(String)".Length..].ToString(), out value)
			|| s.StartsWith("((String)") && s.EndsWith(')')
			&& MainParsing.TryParse(s["((String)".Length..^1].ToString(), out value))
			return true;
		if (DateTime.TryParse(s.ToString(), CultureInfo.InvariantCulture, out var time))
		{
			value = time;
			return true;
		}
		return false;
	}

	[GeneratedRegex(@"\$(@?[A-Za-z_][0-9A-Za-z_]*)\$")]
	private static partial Regex RecursiveTypeRegex();
}
