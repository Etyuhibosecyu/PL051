global using NStar.Core;
global using NStar.Linq;
global using NStar.Mpir;
global using System;
global using static System.Math;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using String = NStar.Core.String;
using System.Collections.Immutable;
using System.Diagnostics;

namespace PL051.NStar;

public enum LexemType
{
	Int,
	Real,
	Complex,
	Identifier,
	Keyword,
	Operator,
	UnsignedInt,
	LongInt,
	UnsignedLongInt,
	LongLong,
	UnsignedLongLong,
	Decimal,
	OtherNumber,
	String,
	Other,
}

[DebuggerDisplay("{ToString()}")]
public sealed class Lexem(String newString, LexemType newType, int newLineN, int newPos)
{
	public String String { get; set; } = newString;
	public LexemType Type { get; set; } = newType;
	public int LineN { get; set; } = newLineN;
	public int Pos { get; set; } = newPos;

	public Lexem() : this([], LexemType.Other, 1, 0)
	{
	}

	public override string ToString() => $"{Type}: {String}";
}

public class CodeSample(String newString)
{
	private static readonly ImmutableArray<string> BranchOpeners = ["if", "loop", WhileString, RepeatString, "for", "try", "catch"];
	private static readonly ImmutableArray<LexemTree> LexemTree = [DoubleEqualLexemTree('^'), DoubleEqualLexemTree('|'),
		DoubleEqualLexemTree('&'), TripleEqualLexemTree('>'), TripleEqualLexemTree('<'), DoubleEqualLexemTree('!'),
		new('?', [new('!', ['='], allowNone: false), EqualLexemTree('>'), EqualLexemTree('<'),
			'=', '?', '.', '[']), ',', ':', '@', '#', '$', '~', DoubleEqualLexemTree('+'), DoubleEqualLexemTree('-'),
		EqualLexemTree('*'), EqualLexemTree('/'), EqualLexemTree('%'), new LexemTree('=', ['=', '>']), TripleLexemTree('.')];
	private static readonly ImmutableArray<string> NoSpaceBefore = [".", "..", ",", "++", "--", "!!", ")", "]", ";", "\r\n"];
	private static readonly ImmutableArray<string> NoSpaceAfter = [".", "..", "!", "~", "#", "(", "[", "$", "\r\n"];
	private readonly List<Lexem> lexems = [];
	private readonly List<String> lexemTexts = [];
	private readonly List<int> lexemFullStarts = [];
	private readonly String input = newString.Length == 0 ? "return null;" : newString;
	private int pos, lineStart, nestedConditions, errorOccurred;
	private int lineN = 1;
	private bool condition, elseCondition;
	private readonly List<String> errors = [];

	private static LexemTree EqualLexemTree(char c) => new(c, ['=']);

	private static LexemTree DoubleEqualLexemTree(char c) => new(c, [c, '=']);

	private static ImmutableArray<LexemTree> DoubleEqualLexemTreeList(char c) => [DoubleEqualLexemTree(c)];

	private static ImmutableArray<LexemTree> DoubleLexemTreeList(char c) => [new(c, [c])];

	private static LexemTree TripleLexemTree(char c) => new(c, DoubleLexemTreeList(c));

	private static LexemTree TripleEqualLexemTree(char c) => new(c, [.. DoubleEqualLexemTreeList(c), '='], true);

	public (List<Lexem> Lexems, String String, List<String> ErrorsList, int errorOccurred) Disassemble(bool format = false)
	{
		while (IsNotEnd())
		{
			while (true)
			{
				var (flowControl, @return, value) = DisassembleIteration();
				if (lexemTexts.Length >= 2 && lexemTexts[^2] == "\r\n"
					&& !(BranchOpeners.Contains(lexemTexts[^1].ToString()) || lexemTexts[^1] == "else")
					&& nestedConditions > 0)
				{
					nestedConditions--;
					condition = false;
					elseCondition = false;
				}
				if (flowControl)
					continue;
				else if (!@return)
					break;
				if (format && !errors.Any(x => !x.StartsWith("Warning")) && errorOccurred != 2)
					input.Replace(Format());
				return value;
			}
		}
		if (format && !errors.Any(x => !x.StartsWith("Warning")) && errorOccurred != 2)
			input.Replace(Format());
		return (lexems, input, errors, errorOccurred);
	}

	private (bool flowControl, bool @return,
		(List<Lexem> Lexems, String String, List<String> ErrorsList, int ErrorOccurred) value) DisassembleIteration()
	{
		if (errorOccurred == 2)
			return (false, true, (lexems, input, errors, errorOccurred));
		SkipSpacesAndComments();
		if (errorOccurred == 2)
			return (true, false, default);
		if (!IsNotEnd() || input[pos] is '\r' or '\n')
		{
			var lineLength = pos - lineStart + input[lineStart..pos].Count('\t') * 3;
			if (lineLength > CodeStyleRules.MaxCharactersInLine)
				GenerateMessage(0x800F, lineStart, CodeStyleRules.MaxCharactersInLine, lineLength);
		}
		if (ValidateChar(';'))
		{
			SkipSpacesAndComments();
			var notEnd = IsNotEnd();
			var rcharAfterSemicolon = ValidateChar('\r');
			var ncharAfterSemicolon = ValidateChar('\n');
			if (rcharAfterSemicolon || ncharAfterSemicolon)
			{
				AddLexem("\r\n", "\r\n", LexemType.Other, (rcharAfterSemicolon ? 1 : 0) + (ncharAfterSemicolon ? 1 : 0));
				SkipLineBreaks();
				return (true, false, default);
			}
			else if (notEnd)
			{
				GenerateMessage(0x901D, pos - 1);
				return (true, false, default);
			}
		}
		if (!IsNotEnd())
		{
			return (false, false, default);
		}
		var rchar = ValidateChar('\r');
		var nchar = ValidateChar('\n');
		if (rchar || nchar)
		{
			AddLexem("\r\n", "\r\n", LexemType.Other, (rchar ? 1 : 0) + (nchar ? 1 : 0));
			SkipLineBreaks();
			return (true, false, default);
		}
		void AddLexem(String string_, String lexemText, LexemType lexemType, int offset)
		{
			lexems.Add(new(string_, lexemType, lineN, pos - offset - lineStart));
			lexemTexts.Add(lexemText);
			lexemFullStarts.Add(pos - offset);
		}
		void AddOperatorLexem(String string_) => AddLexem(string_, string_, LexemType.Operator, string_.Length);
		void AddOtherLexem(String string_) => AddLexem(string_, string_, LexemType.Other, string_.Length);
		String s;
		if (CheckChar('\'') || CheckChar('\"') || CheckChar('@'))
		{
			s = GetString(out var s2);
			if (s.Length != 0)
			{
				AddLexem(s, s2, LexemType.String, s2.Length);
				return (true, false, default);
			}
		}
		else if (GetRawString(out var s3) is var s2 && s2.Length != 0)
		{
			AddLexem(s2, s3, LexemType.String, s3.Length);
			return (true, false, default);
		}
		else if (CheckDigit())
		{
			s = GetBinOrHexNumber(out s2, out var numberType);
			if (s.Length != 0)
			{
				AddLexem(s, s2, numberType, s2.Length);
				return (true, false, default);
			}
			s = GetNumber(out s2, out numberType);
			if (s.Length != 0)
			{
				AddLexem(s, s2, numberType, s2.Length);
				return (true, false, default);
			}
		}
		else if (CheckLetter() || CheckChar('_'))
		{
			s = GetWord();
			if (s.Length != 0)
			{
				if (Keywords.Contains(s))
				{
					ValidateNestedConditions(s);
					AddLexem(s, s, LexemType.Keyword, s.Length);
				}
				else if (s.AsSpan() is "and" or "or" or "xor" or "is" or "typeof" or "sin" or "cos" or "tan"
					or "asin" or "acos" or "atan" or "ln" or "CombineWith" or "CloseOnReturnWith")
					AddOperatorLexem(s);
				else if (s.AsSpan() is "pow" or "tetra" or "penta" or "hexa")
				{
					ValidateEquality(ref s);
					AddOperatorLexem(s);
				}
				else
					AddLexem(s, s, LexemType.Identifier, s.Length);
				return (true, errorOccurred == 2, default);
			}
		}
		else if (";()[]{}".Contains(input[pos]))
		{
			if (input[pos] == '{' && nestedConditions > 1)
			{
				GenerateMessage(0x9021, pos);
				return (true, true, default);
			}
			if (input[pos] is '{' or '}')
			{
				nestedConditions = 0;
				condition = false;
				elseCondition = false;
			}
			AddOtherLexem((String)input[pos++]);
			return (true, false, default);
		}
		var l = 0;
		if (ValidateChar('-'))
		{
			var s3 = GetWord();
			if (s3 == "Infty")
			{
				l += s3.Length + 1;
				s = input.GetRange(pos - l, l);
				AddLexem(s, s, LexemType.Identifier, s.Length);
				return (true, false, default);
			}
			else
				pos -= s3.Length + 1;
		}
		pos += l;
		s = new String(32, ValidateLexemTree(new LexemTree('\0', LexemTree), out var success_));
		if (s.Length != 0 && success_)
		{
			AddOperatorLexem(s);
			return (true, false, default);
		}
		s = GetUnformatted();
		if (s.Length != 0)
		{
			GenerateMessage(0x0003, pos - s.Length);
			return (true, false, default);
		}
		return (false, false, default);
		String ValidateLexemTree(LexemTree lexemTree, out bool success)
		{
			success = false;
			int start = pos, found = 0, lexemIndex = -1;
			var c = input[pos++];
			String result = [];
			String Empty()
			{
				pos = start;
				return [];
			}
			var nextTree = lexemTree.NextTree.ToList();
			while ((lexemIndex = nextTree.FindIndex(lexemIndex + 1, x => x.Char == c)) != -1)
			{
				found++;
				if (found > 1 && !lexemTree.AllowAll)
					return Empty();
				result.Add(c);
				var result2 = ValidateLexemTree(lexemTree.NextTree[lexemIndex], out success);
				if (!success)
					return Empty();
				result.AddRange(result2);
			}
			if (found == 0)
			{
				success = lexemTree.AllowNone;
				return Empty();
			}
			return result;
		}
	}

	private void SkipSpacesAndComments()
	{
		while (true)
		{
			SkipSpaces();
			if (errorOccurred == 2 || !(pos <= input.Length - 2 && input[pos] == '/'))
				return;
			var c = input[pos + 1];
			if (c == '/')
			{
				pos += 2;
				while (IsNotEnd() && input[pos] is not ('\r' or '\n'))
					pos++;
			}
			else if (c == '*')
			{
				pos += 3;
				while (IsNotEnd() && !(input[pos - 1] == '*' && input[pos] == '/'))
					IncreasePosSmoothly();
				if (IsNotEnd())
					pos++;
				else
				{
					GenerateMessage(0x9005, pos);
					return;
				}
			}
			else if (c == '{')
			{
				pos += 2;
				SkipNestedComments();
			}
			else
				return;
		}
	}

	private void SkipLineBreaks()
	{
		var start = pos;
		lineN++;
		lineStart = pos;
		SkipSpaces();
		var rchar = ValidateChar('\r');
		var nchar = ValidateChar('\n');
		if (!(rchar || nchar))
			return;
		if (start != 0 && "()[]{,.<>+-*/!%^&|".Contains(input[start - 1]))
		{
			GenerateMessage(0x8011, lineStart);
			return;
		}
		lineN++;
		lineStart = pos;
		SkipSpaces();
		if (IsNotEnd() && (")]},.<>+-!%^&|\r\n".Contains(input[pos])
			|| input[pos] is '*' or '/' && !(pos + 1 < input.Length && input[pos + 1] is '*' or '/')))
			GenerateMessage(0x8011, lineStart);
	}

	private void SkipSpaces()
	{
		var bStart = lineStart == pos;
		var spaces = 0;
		var redundantSpaces = false;
		var tabs = 0;
		var totalWhitespaces = 0;
		while (IsNotEnd())
		{
			if (input[pos] is ' ' or (char)160)
			{
				if (bStart)
				{
					GenerateMessage(0x9016, pos);
					return;
				}
				if (spaces > 0 && !redundantSpaces)
				{
					GenerateMessage(0x800C, pos);
					redundantSpaces = true;
				}
				spaces++;
				totalWhitespaces++;
			}
			else if (input[pos] == '\t')
			{
				spaces = 0;
				tabs++;
				if (tabs > 5)
				{
					GenerateMessage(0x9014, pos);
					return;
				}
				totalWhitespaces++;
			}
			else
				return;
			if (totalWhitespaces > 8)
			{
				GenerateMessage(0x9015, pos);
				return;
			}
			pos++;
		}
	}

	private bool IsNotEnd() => pos < input.Length;

	private bool CheckChar(char tc) => IsNotEnd() && input[pos] == tc;

	private bool ValidateCondition(bool condition)
	{
		if (condition)
		{
			pos++;
			return true;
		}
		else
			return false;
	}

	private bool ValidateChar(char tc) => ValidateCondition(IsNotEnd() && input[pos] == tc);

	private bool ValidateCharList(String tcl) => ValidateCondition(IsNotEnd() && tcl.Contains(item: input[pos]));

	private String FromStart(int start) => input[start..pos];

	private bool CheckRange(char start, char end) => input[pos] >= start && input[pos] <= end;

	private bool CheckDigit() => IsNotEnd() && CheckRange('0', '9');

	private bool CheckLetter() => IsNotEnd() && (CheckRange('A', 'Z') || CheckRange('a', 'z')
		|| CheckRange('А', 'Я') || CheckRange('а', 'я'));

	private bool CheckLD() => CheckLetter() || CheckDigit();

	private void IncreasePosSmoothly()
	{
		var rchar = ValidateChar('\r');
		var nchar = ValidateChar('\n');
		if (rchar || nchar)
		{
			lineN++;
			lineStart = pos;
		}
		else
			pos++;
	}

	private String GetNumber(out String s2, out LexemType lexemType)
	{
		var start = pos;
		List<String> numberParts = [GetNumber2(out lexemType)];
		if (CheckOverflow(numberParts[0], ref lexemType) is String s)
		{
			s2 = input[start..pos];
			return s;
		}
		if (ValidateChar('.'))
		{
			if (CheckChar('.'))
			{
				pos--;
				return s2 = input.GetRange(start..pos, true);
			}
			if (lexemType == LexemType.LongLong)
			{
				GenerateMessage(0x0001, start);
				lexemType = LexemType.Keyword;
				s2 = input[start..pos];
				return NullString;
			}
			lexemType = LexemType.Real;
			numberParts.Add(input[(pos - 1)..pos]);
			numberParts.Add(GetNumber2(out _));
			if (CheckOverflow(numberParts[^1], ref lexemType) is String s3)
			{
				s2 = input[start..pos];
				return s3;
			}
			if (numberParts[0].Length == 0 && numberParts[2].Length == 0)
			{
				pos = start;
				return s2 = [];
			}
		}
		if (numberParts[0].Length != 0 && ValidateCharList("Ee") && ValidateCharList("+-"))
		{
			lexemType = LexemType.Real;
			numberParts.Add(input[(pos - 2)..pos]);
			numberParts.Add(GetNumber2(out _));
			if (CheckOverflow(numberParts[^1], ref lexemType) is String s3)
			{
				s2 = input[start..pos];
				return s3;
			}
		}
		if (numberParts[0].Length != 0)
		{
			if (ValidateChar('L'))
			{
				if (!ValidateLong(out lexemType))
				{
					s2 = input[start..pos];
					return NullString;
				}
			}
			else if (ValidateChar('u'))
			{
				if (!ValidateUnsigned(out lexemType))
				{
					s2 = input[start..pos];
					return NullString;
				}
			}
			else if (ValidateChar('n'))
			{
				numberParts.Add("n");
				if (!int.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0018, start, int.MinValue, int.MaxValue);
					lexemType = LexemType.Keyword;
					s2 = input[start..pos];
					return NullString;
				}
				lexemType = LexemType.Int;
			}
			else if (ValidateChar('r'))
			{
				if (!double.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0018, start, "±4.940656E-324", "±1.797693E+308");
					lexemType = LexemType.Keyword;
					s2 = input[start..pos];
					return NullString;
				}
				lexemType = LexemType.Real;
				numberParts.Add("r");
			}
			else if (ValidateChar('m'))
			{
				if (!decimal.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0018, start, "±1.0E-28", "±7.922816E+28");
					lexemType = LexemType.Keyword;
					s2 = input[start..pos];
					return NullString;
				}
				lexemType = LexemType.Decimal;
				numberParts.Add("m");
			}
			else if (ValidateChar('c'))
			{
				if (lexemType == LexemType.LongLong)
				{
					GenerateMessage(0x0001, start);
					lexemType = LexemType.Keyword;
					s2 = input[start..pos];
					return NullString;
				}
				lexemType = LexemType.Complex;
				numberParts.Add("c");
			}
			else if (ValidateChar('i'))
			{
				if (lexemType == LexemType.LongLong)
				{
					GenerateMessage(0x0001, start);
					lexemType = LexemType.Keyword;
					s2 = input[start..pos];
					return NullString;
				}
				lexemType = LexemType.Complex;
				numberParts.Add("i");
			}
		}
		if (numberParts.Length > 1 && numberParts[^1] != "LL" && lexemType == LexemType.LongLong)
		{
			GenerateMessage(0x0001, start);
			lexemType = LexemType.Keyword;
			s2 = input[start..pos];
			return NullString;
		}
		s2 = input[start..pos];
		return String.Join([], numberParts);
		String? CheckOverflow(String s, ref LexemType lexemType)
		{
			if (s == NullString)
			{
				GenerateMessage(0x0001, start);
				lexemType = LexemType.Keyword;
				return NullString;
			}
			else
				return null;
		}
		bool ValidateLong(out LexemType lexemType)
		{
			numberParts.Add("L");
			if (ValidateChar('L'))
			{
				if (!MpzT.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0019, start);
					lexemType = LexemType.Keyword;
					return false;
				}
				lexemType = LexemType.LongLong;
				numberParts[^1].Add('L');
				return true;
			}
			else if (ValidateChar('r'))
			{
				lexemType = LexemType.OtherNumber;
				numberParts[^1].Add('r');
				return true;
			}
			else if (ValidateChar('m'))
			{
				lexemType = LexemType.OtherNumber;
				numberParts[^1].Add('m');
				return true;
			}
			else if (!long.TryParse(numberParts[0].AsSpan(), out _))
			{
				GenerateMessage(0x0018, start, long.MinValue, long.MaxValue);
				lexemType = LexemType.Keyword;
				return false;
			}
			else
			{
				lexemType = LexemType.LongInt;
				return true;
			}
		}
		bool ValidateUnsigned(out LexemType lexemType)
		{
			numberParts.Add("u");
			if (!ValidateChar('L'))
			{
				if (!uint.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0018, start, uint.MinValue, uint.MaxValue);
					lexemType = LexemType.Keyword;
					return false;
				}
				lexemType = LexemType.UnsignedInt;
				return true;
			}
			numberParts[^1].Add('L');
			if (!ValidateChar('L'))
			{
				if (ValidateChar('r'))
				{
					lexemType = LexemType.OtherNumber;
					numberParts[^1].Add('r');
					return true;
				}
				else if (ValidateChar('m'))
				{
					lexemType = LexemType.OtherNumber;
					numberParts[^1].Add('m');
					return true;
				}
				else if (!ulong.TryParse(numberParts[0].AsSpan(), out _))
				{
					GenerateMessage(0x0018, start, ulong.MinValue, ulong.MaxValue);
					lexemType = LexemType.Keyword;
					return false;
				}
				lexemType = LexemType.UnsignedLongInt;
				return true;
			}
			else if (!MpuT.TryParse(numberParts[0].AsSpan(), out _))
			{
				GenerateMessage(0x0019, start);
				lexemType = LexemType.Keyword;
				return false;
			}
			numberParts[^1].Add('L');
			lexemType = LexemType.UnsignedLongLong;
			return true;
		}
	}

	private String GetNumber2(out LexemType lexemType)
	{
		var start = pos;
		while (CheckDigit() || CheckChar('_'))
			pos++;
		var s = new String(32, FromStart(start)).FilterInPlace(x => x != '_').ToString();
		if (s.Length == 0)
		{
			lexemType = LexemType.Other;
			return s;
		}
		if (int.TryParse(s, out _))
			lexemType = LexemType.Int;
		else if (uint.TryParse(s, out _))
			lexemType = LexemType.UnsignedInt;
		else if (long.TryParse(s, out _))
			lexemType = LexemType.LongInt;
		else if (ulong.TryParse(s, out _))
			lexemType = LexemType.UnsignedLongInt;
		else if (MpuT.TryParse(s, out _))
			lexemType = LexemType.UnsignedLongLong;
		else if (MpzT.TryParse(s, out _))
			lexemType = LexemType.LongLong;
		else
		{
			lexemType = LexemType.Keyword;
			return NullString;
		}
		return s;
	}

	private String GetBinOrHexNumber(out String s2, out LexemType lexemType)
	{
		var start = pos;
		if (!ValidateChar('0'))
		{
			lexemType = LexemType.Other;
			s2 = [];
			return [];
		}
		if (ValidateChar('B') || ValidateChar('b'))
		{
			while (CheckChar('0') || CheckChar('1') || CheckChar('_'))
				pos++;
			var s = new String(32, FromStart(start)).FilterInPlace(x => x != '_').ToString()[2..];
			if (s.Length == 0)
			{
				pos = start;
				lexemType = LexemType.Other;
				return s2 = s;
			}
			if (!s.ContainsAnyExcept('0'))
			{
				lexemType = LexemType.Int;
				s2 = input[start..pos];
				return "0";
			}
			else if (!(new MpzT(s, 2) is var ll && ll != 0))
			{
				lexemType = LexemType.Keyword;
				s2 = input[start..pos];
				return NullString;
			}
			else
			{
				var li = (long)ll;
				if (li != ll)
				{
					lexemType = ll <= ulong.MaxValue ? LexemType.UnsignedLongInt : LexemType.LongLong;
					s2 = input[start..pos];
					return ll.ToString();
				}
				if (li is >= int.MinValue and <= int.MaxValue)
					lexemType = LexemType.Int;
				else if (li <= uint.MaxValue)
					lexemType = LexemType.UnsignedInt;
				else
					lexemType = LexemType.LongInt;
				s2 = input[start..pos];
				return li.ToString();
			}
		}
		else if (ValidateChar('X') || ValidateChar('x'))
		{
			while (CheckRange('0', '9') || CheckRange('A', 'F') || CheckRange('a', 'f') || CheckChar('_'))
				pos++;
			var s = new String(32, FromStart(start)).FilterInPlace(x => x != '_').ToString()[2..];
			if (s.Length == 0)
			{
				pos = start;
				lexemType = LexemType.Other;
				return s2 = s;
			}
			if (!s.ContainsAnyExcept('0'))
			{
				lexemType = LexemType.Int;
				s2 = input[start..pos];
				return "0";
			}
			else if (!(new MpzT(s, 16) is var ll && ll != 0))
			{
				lexemType = LexemType.Keyword;
				s2 = input[start..pos];
				return NullString;
			}
			else
			{
				var li = (long)ll;
				if (li != ll)
				{
					lexemType = ll <= ulong.MaxValue ? LexemType.UnsignedLongInt : LexemType.LongLong;
					s2 = input[start..pos];
					return ll.ToString();
				}
				if (li is >= int.MinValue and <= int.MaxValue)
					lexemType = LexemType.Int;
				else if (li <= uint.MaxValue)
					lexemType = LexemType.UnsignedInt;
				else
					lexemType = LexemType.LongInt;
				s2 = input[start..pos];
				return li.ToString();
			}
		}
		else
		{
			pos = start;
			lexemType = LexemType.Other;
			return s2 = [];
		}
	}

	private String GetWord(bool firstLetter = true)
	{
		var start = pos;
		var b = false;
		void Validate(bool parameter = false)
		{
			if (CheckLetter() || parameter && CheckDigit() || CheckChar('_'))
				b = true;
			pos++;
		}
		if ((firstLetter ? CheckLetter() : CheckLD()) || CheckChar('_'))
			Validate();
		while (CheckLD() || CheckChar('_'))
			Validate(true);
		if (!b)
			pos = start;
		return new(32, FromStart(start));
	}

	private String GetString(out String s2)
	{
		var start = pos;
		String result = [];
		String buffer = [];
		var hex = 0;
		void AddChar(String target) => target.Add(input[pos++]);
		String EscapeSequence() => ['\\', input[pos - 1]];
		bool ValidateAndAdd(char c)
		{
			if (ValidateChar(c))
			{
				result.Add(c);
				return true;
			}
			return false;
		}
		bool HexSequence(char c, int length)
		{
			if (ValidateChar(c))
			{
				buffer.AddRange(EscapeSequence());
				hex = length;
				return true;
			}
			return false;
		}
		bool IsCharAfterBackslash(char c, bool addChar = true)
		{
			if (input[pos - 1] != '\\' && CheckChar(c))
			{
				if (addChar)
				{
					pos++;
					result.Add(c);
				}
				return true;
			}
			return false;
		}
		void GenerateEscapeSequenceError(int posDiff) => GenerateMessage(0x0002, pos++ - posDiff - lineStart);
		String AddAndReturn(out String s2, char toAdd)
		{
			result.Add(toAdd);
			s2 = input[start..pos];
			return new(result);
		}
		bool ValidateEscapeSequence()
		{
			if (!ValidateChar('\\'))
				return false;
			if (ValidateCharList("0abfnqrtv'\"!"))
			{
				result.AddRange(EscapeSequence());
				hex = 0;
			}
			else if (!(HexSequence('x', 2) || HexSequence('u', 4)))
				GenerateEscapeSequenceError(1);
			else
				hex = 0;
			AddEscapeSequenceChars(ref buffer, hex);
			if (buffer.Length != 0)
				result.AddRange(buffer);
			return true;
		}
		void AddEscapeSequenceChars(ref String tempResult, int hex)
		{
			for (var i = 0; i < hex; i++)
			{
				if (IsNotEnd() && (CheckRange('0', '9') || CheckRange('A', 'F') || CheckRange('a', 'f')))
					AddChar(tempResult);
				else
				{
					GenerateEscapeSequenceError(i + 2);
					tempResult = [];
					break;
				}
			}
		}
		String GenerateQuoteWreck(out String s2, ushort code,
			string text = "unexpected end of code reached; expected: single quote", bool double_ = false)
		{
			GenerateMessage(code, pos, text);
			return AddAndReturn(out s2, double_ ? '\"' : '\'');
		}
		String GenerateDoubleQuoteWreck(out String s2) =>
			GenerateQuoteWreck(out s2, 0x9002, "unexpected end of code reached; expected: double quote", true);
		if (ValidateAndAdd('\"'))
		{
			while (IsNotEnd() && input[pos] is not ('\r' or '\n') && !IsCharAfterBackslash('\"', false))
			{
				if (!ValidateEscapeSequence())
					AddChar(result);
			}
			if (IsNotEnd() && input[pos] is '\r' or '\n')
				return GenerateQuoteWreck(out s2, 0x9003);
			if (!IsCharAfterBackslash('\"'))
				return GenerateDoubleQuoteWreck(out s2);
			return Default(out s2);
		}
		else if (ValidateAndAdd('\''))
		{
			if (!ValidateEscapeSequence())
			{
				if (!IsNotEnd())
					GenerateQuoteWreck(out _, 0x9000);
				else if (!CheckChar('\''))
					AddChar(result);
			}
			if (!ValidateAndAdd('\''))
			{
				if (!IsNotEnd())
					GenerateQuoteWreck(out _, 0x9000);
				else
					GenerateQuoteWreck(out _, 0x9001);
			}
			return Default(out s2);
		}
		else if (!ValidateAndAdd('@'))
			return Default(out s2);
		if (!ValidateChar('\"'))
		{
			pos = start;
			return s2 = [];
		}
		result.Add('\"');
		while (true)
		{
			if (ValidateAndAdd('\"'))
			{
				if (!ValidateAndAdd('\"'))
					break;
			}
			else if (!IsNotEnd())
				return GenerateDoubleQuoteWreck(out s2);
			else
			{
				result.Add(input[pos]);
				IncreasePosSmoothly();
			}
		}
		return Default(out s2);
		String Default(out String s2)
		{
			s2 = input[start..pos];
			return new(32, result);
		}
	}

	private String GetRawString(out String s2)
	{
		var start = pos;
		String result = [];
		if (!ValidateAndAdd('/'))
			return s2 = [];
		if (!ValidateAndAdd('\"'))
		{
			pos = start;
			return s2 = [];
		}
		var depth = 0;
		var state = RawStringState.Normal;
		while (true)
		{
			if (errorOccurred != 0)
			{
				s2 = input[start..pos];
				return result.AddRange(((String)"\"\\").Repeat(depth + 1));
			}
			else if (pos >= input.Length)
			{
				GenerateMessage(0x9004, pos, depth + 1);
				s2 = input[start..pos];
				return result.AddRange(((String)"\"\\").Repeat(depth + 1));
			}
			else if (ValidateAndAdd('/'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.ForwardSlash;
					continue;
				}
				var pos2 = pos;
				while (IsNotEnd() && input[pos] is not ('\r' or '\n'))
					pos++;
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('*'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				var pos2 = pos;
				pos++;
				while (IsNotEnd() && !(input[pos - 1] == '*' && input[pos] == '/'))
					IncreasePosSmoothly();
				if (IsNotEnd())
					pos++;
				else
				{
					GenerateMessage(0x9005, pos);
					s2 = input[start..pos];
					return result.AddRange("*/").AddRange(((String)"\"\\").Repeat(depth + 1));
				}
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('{'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				var pos2 = pos;
				SkipNestedComments();
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('\\'))
			{
				if (state is not (RawStringState.Quote or RawStringState.ForwardSlashAndQuote))
					state = RawStringState.Normal;
				else if (depth == 0 || state == RawStringState.ForwardSlashAndQuote && depth == 1)
					break;
				else if (state == RawStringState.ForwardSlashAndQuote)
				{
					depth -= 2;
					state = RawStringState.Normal;
				}
				else
				{
					depth--;
					state = RawStringState.Normal;
				}
			}
			else if (ValidateAndAdd('\"'))
			{
				if (state == RawStringState.ForwardSlash)
				{
					depth++;
					state = RawStringState.ForwardSlashAndQuote;
				}
				else if (state == RawStringState.EmailSign)
					result.AddRange(GetVerbatimStringInsideRaw());
				else
					state = RawStringState.Quote;
			}
			else if (ValidateAndAdd('@'))
				state = RawStringState.EmailSign;
			else
			{
				if (input[pos] is '\r' or '\n')
					result.AddRange("\r\n");
				else
					result.Add(input[pos]);
				state = RawStringState.Normal;
				IncreasePosSmoothly();
			}
		}
		s2 = input[start..pos];
		return new(32, result);
		bool ValidateAndAdd(char c)
		{
			if (ValidateChar(c))
			{
				result.Add(c);
				return true;
			}
			return false;
		}
	}

	private String GetVerbatimStringInsideRaw()
	{
		String result = [];
		while (true)
		{
			if (ValidateChar('\"'))
			{
				result.Add(input[pos - 1]);
				if (!ValidateChar('\"'))
					break;
				result.Add(input[pos - 1]);
			}
			else if (!IsNotEnd())
			{
				GenerateMessage(0x9002, pos);
				return result.Add('\"');
			}
			else
			{
				result.Add(input[pos]);
				IncreasePosSmoothly();
			}
		}
		return result;
	}

	private void SkipNestedComments()
	{
		int depth = 0, state = 0;
		while (IsNotEnd())
		{
			var c = input[pos];
			if (c == '/')
			{
				if (state != 2)
					state = 1;
				else if (depth == 0)
				{
					pos++;
					return;
				}
				else
				{
					depth--;
					state = 0;
				}
			}
			else if (c == '{')
			{
				if (state == 1)
					depth++;
				state = 0;
			}
			else if (c == '}')
				state = 2;
			else
				state = 0;
			IncreasePosSmoothly();
		}
		GenerateMessage(0x9006, pos, depth + 1);
	}

	private void ValidateNestedConditions(String s)
	{
		if (BranchOpeners.Contains(s.ToString())
			&& (lexems.Length == 0 || lexemTexts[^1] == "\r\n"))
		{
			if (elseCondition)
			{
				GenerateMessage(0x9020, pos - s.Length);
			}
			else
			{
				nestedConditions++;
				condition = true;
			}
		}
		else if (s == "else")
		{
			if (condition)
			{
				GenerateMessage(0x901B, pos - s.Length);
			}
			else if (nestedConditions > 0)
			{
				GenerateMessage(0x9020, pos - s.Length);
			}
			else
			{
				nestedConditions++;
				condition = true;
				elseCondition = true;
			}
		}
	}

	private String GetUnformatted()
	{
		var start = pos;
		while (IsNotEnd() && !CheckLD() && !("_\"';()[]{} \t\r\n" + (char)160).Contains(input[pos]))
			pos++;
		return input[start..pos];
	}

	private void ValidateEquality(ref String s)
	{
		if (ValidateChar('='))
			s.Add('=');
	}

	private void GenerateMessage(ushort code, int pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(errors, code, lineN, pos - lineStart, parameters);
		if (code >> 12 != 0x8 && errorOccurred == 0)
			errorOccurred = 1;
		if (code >> 12 == 0x9)
			errorOccurred = 2;
	}

	private String Format()
	{
		String result = [];
		var figureBk = 0;
		nestedConditions = 0;
		elseCondition = false;
		for (var i = 0; i < lexems.Length; i++)
		{
			var prevPos = i == 0 ? 0 : lexemFullStarts[i - 1] + lexemTexts[i - 1].Length;
			var interLexem = input.GetRange(prevPos, Max(lexemFullStarts[i] - prevPos, 0));
			if (interLexem.IndexOfAnyExcluding(" \t\xA0;") is not (-1 or 0))
			{
				if (i == 0 || lexemTexts[i - 1] == "\r\n")
				{
					var indentLevel = figureBk + nestedConditions - (nestedConditions != 0 && lexemTexts[i] == "{" ? 1 : 0);
					result.AddSeries('\t', indentLevel);
				}
				else
					result.Add(' ');
			}
			result.AddRange(interLexem.Trim(" \t\xA0;"));
			if (i >= 1 && !NoSpaceBefore.Contains(lexemTexts[i].ToString())
				&& !NoSpaceAfter.Contains(lexemTexts[i - 1].ToString())
				&& !(lexemTexts[i - 1].AsSpan() is "+" or "-" or "^"
				&& (i < 2 || lexems[i - 2].Type is LexemType.Operator || lexemTexts[i - 2].AsSpan() is "(" or "[" or "\r\n"))
				&& !(lexemTexts[i].AsSpan() is "(" or "["
				&& !(lexems[i - 1].Type is LexemType.Operator && lexemTexts[i - 1] != "typeof"
				|| lexemTexts[i - 1].AsSpan() is "if" or WhileString or RepeatString or "for" or "catch" or ReturnString
				or "(" or "[" or "{" or "}"))
				&& !(lexemTexts[i] == ":" && lexems[i].Pos - lexems[i - 1].Pos == lexemTexts[i - 1].Length))
				result.Add(' ');
			if (lexemTexts[i] == "}" && figureBk > 0)
				figureBk--;
			if (i == 0 || lexemTexts[i - 1] == "\r\n")
			{
				result.AddSeries('\t', figureBk + nestedConditions - (nestedConditions != 0 && lexemTexts[i] == "{" ? 1 : 0));
				if (BranchOpeners.Contains(lexemTexts[i].ToString()))
					nestedConditions++;
				else if (lexemTexts[i] == "else")
				{
					nestedConditions++;
					elseCondition = true;
				}
				else if (nestedConditions > 0)
				{
					nestedConditions--;
					elseCondition = false;
				}
			}
			if (lexemTexts[i] == "{")
				figureBk++;
			result.AddRange(lexemTexts[i]);
		}
		return result;
	}

	public static implicit operator (List<Lexem> Lexems, String String, List<String> ErrorsList,
		int errorOccurred)(CodeSample x) => x.Disassemble();
}
