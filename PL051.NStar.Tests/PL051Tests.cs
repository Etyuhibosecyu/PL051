global using System;
global using String = NStar.Core.String;
using NStar.Core;
using NStar.Mpir;
using NStar.RemoveDoubles;
using System.Globalization;
using static PL051.NStar.BuiltInMemberCollections;
using static PL051.NStar.NStarType;
using static PL051.NStar.SemanticTree;
using static System.Math;
using E = System.Linq.Enumerable;

namespace PL051.NStar.Tests;

[TestClass]
public class PL051Tests
{
	private const string A10 = "AAAAAAAAAA";
	private const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
	private const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
	private const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
	private const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
	private const string A1000000 = "\"" + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + "\"";
	private const string Five16 = "5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, ";
	private const string Five64 = Five16 + Five16 + Five16 + Five16;
	private const string Five256 = Five64 + Five64 + Five64 + Five64;
	private const string T17 = "T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, ";
	private const string T85 = T17 + T17 + T17 + T17 + T17;
	private const string T255 = T85 + T85 + T85;

	[TestMethod]
	[DataRow("""
		return ("7" * "2", "7" * 2, 7 * "2", "7" / "2", "7" / 2, 7 / "2", "7" % "2", "7" % 2, 7 % "2", "7" - "2", "7" - 2, 7 - "2")

		""", NullString, @"Error 4008 in line 1 at position 12: the string cannot be multiplied by the string
Error 4009 in line 1 at position 41: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 59: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 81: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 88: the strings cannot be divided or give the remainder (%)
Error 4007 in line 1 at position 99: the strings cannot be subtracted
Error 4007 in line 1 at position 110: the strings cannot be subtracted
Error 4007 in line 1 at position 117: the strings cannot be subtracted
")]
	[DataRow("""
		var a = 7
		var b = 2
		var aq = "7"
		var bq = "2"
		return (aq * bq, aq * b, a * bq, aq / bq, aq / b, a / bq, aq % bq, aq % b, a % bq, aq - bq, aq - b, a - bq)

		""", NullString, @"Error 4008 in line 5 at position 11: the string cannot be multiplied by the string
Error 4009 in line 5 at position 36: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 45: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 61: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 77: the strings cannot be divided or give the remainder (%)
Error 4007 in line 5 at position 86: the strings cannot be subtracted
Error 4007 in line 5 at position 95: the strings cannot be subtracted
Error 4007 in line 5 at position 102: the strings cannot be subtracted
")]
	[DataRow("""
		return (("A", 77777, 3.14159) + 5, ("A", 77777, 3.14159) - 5, ("A", 77777, 3.14159) * 5, ("A", 77777, 3.14159) / 5, ("A", 77777, 3.14159) % 5)

		""", NullString, @"Warning 800F in line 1 at position 0: too long line (128 characters are supported, actually 142)
Error 4006 in line 1 at position 30: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 57: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 84: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 111: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 138: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
")]
	[DataRow("""
		return (5 + ("A", 77777, 3.14159), 5 - ("A", 77777, 3.14159), 5 * ("A", 77777, 3.14159), 5 / ("A", 77777, 3.14159), 5 % ("A", 77777, 3.14159))

		""", NullString, """
			Warning 800F in line 1 at position 0: too long line (128 characters are supported, actually 142)
			Error 4006 in line 1 at position 10: cannot apply the operator "+" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 37: cannot apply the operator "-" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 64: cannot apply the operator "*" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 91: cannot apply the operator "/" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 118: cannot apply the operator "%" to the types "byte" and "(string, int, real)"
			
			""")]
	[DataRow("""
		return (5 + null, 5 - null, 5 * null, 5 / null, 5 % null, null + 5, null - 5, null * 5, null / 5, null % 5)

		""", NullString, @"Error 4006 in line 1 at position 40: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 1 at position 50: cannot apply the operator ""%"" to the types ""byte"" and ""null""
")]
	[DataRow("""
		var a = ("A", 77777, 3.14159)
		var b = 5
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a)

		""", NullString, @"Error 4006 in line 3 at position 10: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 17: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 24: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 45: cannot apply the operator ""+"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 52: cannot apply the operator ""-"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 59: cannot apply the operator ""*"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 66: cannot apply the operator ""/"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 73: cannot apply the operator ""%"" to the types ""byte"" and ""(string, int, real)""
")]
	[DataRow("""return (sin "Infty", tan "Uncty", asin "2.71828", acos "-42", ln "-5", 1000000000000!, Infty!, 2.5!)""",
NullString, @"Error 4002 in line 1 at position 8: cannot apply this operator to this constant
Error 4002 in line 1 at position 21: cannot apply this operator to this constant
Error 4002 in line 1 at position 34: cannot apply this operator to this constant
Error 4002 in line 1 at position 50: cannot apply this operator to this constant
Error 4002 in line 1 at position 62: cannot apply this operator to this constant
Error 4003 in line 1 at position 84: cannot compute factorial of this constant
Error 4003 in line 1 at position 92: cannot compute factorial of this constant
Error 4003 in line 1 at position 98: cannot compute factorial of this constant
")]
	[DataRow("""
		var a = 5
		var b = null
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a)

		""", NullString, @"Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""byte"" and ""null""
")]
	[DataRow(@"return (IntToReal(5), IntToReal(77777), IntToReal(777777777777))
", @"(5, 77777, 777777777777)", "Ошибок нет")]
	[DataRow(@"var a = 5
var b = 3
return (a / b, IntToReal(a) / b)
", @"(1, 1.6666666666666667)", "Ошибок нет")]
	[DataRow(@"var x = 5
var y = 3
var a = x > y
var b = x < y + 2
var c = x > y && x < y + 2
var d = x > y || x < y + 2
return (a, b, c, d)
", @"(true, false, false, true)", "Ошибок нет")]
	[DataRow(@"var a = 1_0_0_00000_0_00_0
return a
", "100000000000", "Ошибок нет")]
	[DataRow(@"var a = 0x1_0_0_00000_0_00_0
return a
", "17592186044416", "Ошибок нет")]
	[DataRow(@"var a = 0X1_0_0_00000_0_00_0
return a
", "17592186044416", "Ошибок нет")]
	[DataRow(@"var a = 0b1_0_0_00000_0_00_0
return a
", "2048", "Ошибок нет")]
	[DataRow(@"var a = 0B1_0_0_00000_0_00_0
return a
", "2048", "Ошибок нет")]
	[DataRow(@"var a = 12_34_56_78_90
return a
", "1234567890", "Ошибок нет")]
	[DataRow(@"var a = 12_34_56_78_9A
return a
", NullString, @"Error 2008 in line 1 at position 21: expected: end of the line
Error 2007 in line 1 at position 21: unrecognized construction
Error 4001 in line 2 at position 7: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"var a = 12_34_56_78_9a
return a
", NullString, @"Error 2008 in line 1 at position 21: expected: end of the line
Error 2007 in line 1 at position 21: unrecognized construction
Error 4001 in line 2 at position 7: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"var a = 0x12_34_56_78_9A
return a
", "78187493530", "Ошибок нет")]
	[DataRow(@"var a = 0X12_34_56_78_9a
return a
", "78187493530", "Ошибок нет")]
	[DataRow(@"var a = 0x12_34_56_78_9a_bc_de_f0_12_34_56_78_9a_bc_de_f0_
return a
", "24197857203266734864793317670504947440", "Ошибок нет")]
	[DataRow(@"var a = 0x_12_34_56_78_9a_bc_de_f0_12_34_56_78_9a_bc_de_f0
return a
", "24197857203266734864793317670504947440", "Ошибок нет")]
	[DataRow(@"var a = 0x12_34_56_78_9G
return a
", NullString, @"Error 2008 in line 1 at position 23: expected: end of the line
Error 2007 in line 1 at position 23: unrecognized construction
Error 4001 in line 2 at position 7: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"var a = 0x12_34_56_78_9w
return a
", NullString, @"Error 2008 in line 1 at position 23: expected: end of the line
Error 2007 in line 1 at position 23: unrecognized construction
Error 4001 in line 2 at position 7: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"var a = 0b1030
return a
", NullString, @"Error 2008 in line 1 at position 12: expected: end of the line
Error 2007 in line 1 at position 12: unrecognized construction
Error 4001 in line 2 at position 7: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"return Max(3)
", "3", "Ошибок нет")]
	[DataRow(@"return Max(3, 1)
", "3", "Ошибок нет")]
	[DataRow(@"return Max(3, 1, 4)
", "4", "Ошибок нет")]
	[DataRow(@"return Max(3, 1, 4, 2)
", "4", "Ошибок нет")]
	[DataRow(@"return Mean(3)
", "3", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1)
", "2", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1, 3.5)
", "2.5", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1, 4, 2)
", "2.5", "Ошибок нет")]
	[DataRow(@"return Min(2)
", "2", "Ошибок нет")]
	[DataRow(@"return Min(2, 4)
", "2", "Ошибок нет")]
	[DataRow(@"return Min(2, 4, 1)
", "1", "Ошибок нет")]
	[DataRow(@"return Min(2, 4, 1, 3)
", "1", "Ошибок нет")]
	[DataRow(@"var a = 1 ?> 2 : 3 ?> 2 : 1
return a
", "3", "Ошибок нет")]
	[DataRow(@"return ""A"" ?= ""B"" : ""C""
", "\"C\"", "Ошибок нет")]
	[DataRow(@"var a = ""A"" ?= ""B"" : ""C""
return a
", "\"C\"", "Ошибок нет")]
	[DataRow(@"return ""A"" ?!= ""B"" : ""C""
", "\"A\"", "Ошибок нет")]
	[DataRow(@"var a = ""A"" ?!= ""B"" : ""C""
return a
", "\"A\"", "Ошибок нет")]
	[DataRow(@"return ""A"" ?> ""B"" : ""C""
", NullString, @"Error 4006 in line 1 at position 11: cannot apply the operator ""?>"" to the types ""string"" and ""string""
")]
	[DataRow(@"var a = ""A"" ?> ""B"" : ""C""
return a
", NullString, @"Error 4006 in line 1 at position 12: cannot apply the operator ""?>"" to the types ""string"" and ""string""
")]
	[DataRow(@"return 3 ?> 2 : ""A""
", "3", "Ошибок нет")]
	[DataRow(@"var a = 3 ?> 2 : ""A""
return a
", NullString, @"Error 4015 in line 1 at position 15: there is no implicit conversion between the types ""byte"" and ""string""
")]
	[DataRow(@"real Function F(real x, real y)
{
	return x * x + x * y + y * y
}
real Function Max2(real x, real y)
{
	return Max(x, y)
}
System.Func[real, real, real] f
f = F
real a = f(3.14159, 2.71828)
f = Max2
real b = f(3.14159, 2.71828)
return (a, b)
", @"(25.798355151699997, 3.14159)", "Ошибок нет")]
	[DataRow(@"null Function F()
{
}
int Function F()
{
	return 5
}
return F()
", NullString, @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F(int x)
{
}
int Function F(int x)
{
	return x * x
}
return F(5)
", NullString, @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F()
{
}
int Function F(int x)
{
	return x * x
}
F()
return F(5)
", "25", "Ошибок нет")]
	[DataRow(@"{
	F()
	null Function F()
	{
	}
}
int Function F(int x)
{
	return x * x
}
return F(5)
", "25", "Ошибок нет")]
	[DataRow(@"null Function F(int x)
{
}
int Function F(int x)
{
	return x * x
}
F(5)
return F(5)
", NullString, @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"{
	F(5)
	null Function F(int x)
	{
	}
}
int Function F(int x)
{
	return x * x
}
return F(5)
", "25", "Ошибок нет")]
	[DataRow(@"null Function F()
{
	return null
}
int Function F()
{
	return 5
}
return F()
", NullString, @"Error 2032 in line 5 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F(int x)
{
	return null
}
int Function F(int x)
{
	return x * x
}
return F(5)
", NullString, @"Error 2032 in line 5 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F()
{
	return null
}
int Function F(int x)
{
	return x * x
}
F()
return F(5)
", "25", "Ошибок нет")]
	[DataRow(@"{
	F()
	null Function F()
	{
		return null
	}
}
int Function F(int x)
{
	return x * x
}
return F(5)
", "25", "Ошибок нет")]
	[DataRow(@"int int int int int = 5
", NullString, @"Error 2008 in line 1 at position 8: expected: end of the line
Error 2007 in line 1 at position 8: unrecognized construction
")]
	[DataRow(@"var var var var var = 5
", NullString, @"Error 2008 in line 1 at position 8: expected: end of the line
Error 2007 in line 1 at position 8: unrecognized construction
")]
	[DataRow(@"int a = 3.14159
byte b = 77777
real c = ""2.71828""
return (a, b, c)
", NullString, @"Error 4027 in line 1 at position 6: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 2 at position 7: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 3 at position 7: cannot convert from the type ""string"" to the type ""real"" during the assignment
Error 4001 in line 4 at position 8: the identifier ""a"" is not defined in this location
Error 4001 in line 4 at position 11: the identifier ""b"" is not defined in this location
Error 4001 in line 4 at position 14: the identifier ""c"" is not defined in this location
")]
	[DataRow(@"int a = 0
byte b = 0
real c = 0
a = 3.14159
b = 77777
c = ""2.71828""
return (a, b, c)
", NullString, @"Error 4027 in line 4 at position 2: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 5 at position 2: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 6 at position 2: cannot convert from the type ""string"" to the type ""real"" during the assignment
")]
	[DataRow(@"() byte bytes = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25)
return bytes
", NullString, @"Error 4014 in line 1 at position 14: list can be constructed from tuple of up to 16 elements, if you need more, use the other ways like Chain() or Fill()
Error 4001 in line 2 at position 7: the identifier ""bytes"" is not defined in this location
")]
	[DataRow(@"bool bool=bool
", NullString, @"Error 4012 in line 1 at position 10: one cannot use the local variable ""bool"" before it is declared or inside such declaration in line 1 at position 0
")]
	[DataRow(@"bool Function One()
{
	int Function Two()
	{
		return -1
	}
	return Two()
}
return One()
", False, @"Warning 800A in line 7 at position 8: the type of the returning value ""int"" and the function return type ""bool"" are badly compatible, you may lost data
")]
	[DataRow(@"System.Func[int] Function F()
{
	int Function F2()
	{
		return 100
	}
	return F2
}
return F()()
", "100", "Ошибок нет")]
	[DataRow(@"return /""Hello, world!""ssssssssssssssss\
", NullString, @"Wreck 9004 in line 2 at position 0: unexpected end of code reached; expected: 1 pairs ""double quote - reverse slash"" (starting with quote)
")]
	[DataRow(@"return /""Hello, world!/""\
", "\"Hello, world!/\"", "Ошибок нет")]
	[DataRow(@"return /""Hell@""/""o, world!""\
", @"/""Hell@""/""o, world!""\", "Ошибок нет")]
	[DataRow(@"return /""Hell@""/{""o, world!""\
", @"/""Hell@""/{""o, world!""\", "Ошибок нет")]
	[DataRow(@"return /""Hell@""\""""\""o, world!""\
", @"/""Hell@""\""""\""o, world!""\", "Ошибок нет")]
	[DataRow(@"return 'Hello, world!'
", NullString, @"Wreck 9001 in line 1 at position 9: there must be a single character or a single escape-sequence in the single quotes
")]
	[DataRow(@"return 'H

", NullString, @"Wreck 9001 in line 1 at position 9: there must be a single character or a single escape-sequence in the single quotes
")]
	[DataRow(@"return '", NullString, @"Wreck 9000 in line 1 at position 8: unexpected end of code reached; expected: single quote
")]
	[DataRow(@"var x = 5
return 5 pow x += 3
", NullString, @"Error 201D in line 2 at position 15: only the variables can be assigned
")]
	[DataRow(@"return 
", NullString, @"Warning 8002 in line 1 at position 7: the syntax ""return;"" is deprecated; consider using ""return null;"" instead
")]
	[DataRow(@"var a = false
var b = 5
return a + b
", "5", "Ошибок нет")]
	[DataRow(@"var a = false
var b = 5
return a * b
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""*"" to the types ""bool"" and ""byte""
")]
	[DataRow(@"int Function F(real n)
{
	return Truncate(n * n)
}
return Fill(F(3.14159) >= 10, 100)
", "(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false)", "Ошибок нет")]
	[DataRow(@"using System
Func[int, int, int] sum = (x, y) => x + y
int result = sum(5, 3)
return result
", "8", "Ошибок нет")]
	[DataRow(@"using System
int multiplier = 2
Func[int, int] multiply = x => x * multiplier
int result = multiply(5)
return result
", "10", "Ошибок нет")]
	[DataRow(@"using System
Func[real, int] f = x => x * x
var a = f(5)
f = x => 1r / x
var b = f(5)
return (a, b)
", "(25, 0.2)", "Ошибок нет")]
	[DataRow(@"using System
Func[int, int] invalidFunc = x => { x + 1 }
", NullString, @"Error 402A in line 2 at position 36: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"using System
string s = null
Func[null, int] wrongParams = (x, y) => s = x
return s
", NullString, @"Error 4042 in line 3 at position 31: incorrect list of the parameters of the lambda expression
")]
	[DataRow(@"using System
Func[string, int] typeMismatch = x => x + 1
return typeMismatch(5)
", NullString, @"Error 4014 in line 2 at position 38: cannot convert from the type ""int"" to the type ""string"" during the exprsession substitution - use an addition of zero-length string for this
")]
	[DataRow(@"using System
Func[string, string] typeMismatch = x => x + 1
return typeMismatch()
", NullString, @"Error 4045 in line 3 at position 19: this lambda must have 1 parameters
")]
	[DataRow(@"using System
Func[string, string] typeMismatch = x => x + 1
return typeMismatch(5, 8, 12)
", NullString, @"Error 4045 in line 3 at position 19: this lambda must have 1 parameters
")]
	[DataRow(@"using System
Func[string, string] typeMismatch = x => x + 1
return typeMismatch(5)
", NullString, @"Error 4014 in line 3 at position 20: cannot convert from the type ""byte"" to the type ""string"" during the call - use an addition of zero-length string for this
")]
	[DataRow(@"int a = 5
a = new(8)
return a
", NullString, @"Error 4017 in line 2 at position 4: the type ""int"" cannot be created via the constructor
Error 4000 in line 2 at position 7: internal compiler error #5
")]
	[DataRow(@"const int a = 5
const real b = 3.14159
const string c = ""A""
const bool d = true
return (a, b, c, d)
", "(5, 3.14159, \"A\", true)", "Ошибок нет")]
	[DataRow(@"const int a
const real b = 3.14159
const string c = ""A""
const bool d = true
return (a, b, c, d)
", NullString, @"Error 203D in line 1 at position 11: the constant must have a value
Error 4001 in line 5 at position 8: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"const int a = 5
const real b = 3.14159
const string c = ""A""
return (a = 8, b *= 2, c)
", NullString, @"Error 4052 in line 4 at position 10: cannot assign a value to the constant
Error 4052 in line 4 at position 17: cannot assign a value to the constant
")]
	[DataRow(@"const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10
string A10 = ""AAAAAAAAAA""
return A100
", NullString, @"Error 4012 in line 1 at position 20: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 26: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 32: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 38: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
")]
	[DataRow(@"string A10 = ""AAAAAAAAAA""
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10
return A100
", NullString, @"Error 4050 in line 2 at position 20: this expression must be constant but it isn't
Error 4050 in line 2 at position 26: this expression must be constant but it isn't
Error 4050 in line 2 at position 32: this expression must be constant but it isn't
Error 4050 in line 2 at position 38: this expression must be constant but it isn't
Error 4050 in line 2 at position 44: this expression must be constant but it isn't
Error 4050 in line 2 at position 50: this expression must be constant but it isn't
Error 4050 in line 2 at position 56: this expression must be constant but it isn't
Error 4050 in line 2 at position 62: this expression must be constant but it isn't
Error 4050 in line 2 at position 68: this expression must be constant but it isn't
Error 4050 in line 2 at position 74: this expression must be constant but it isn't
")]
	[DataRow(@"const string A10 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10
return A10
", NullString, @"Error 4055 in line 1 at position 19: too deep constant definition tree
Error 4055 in line 1 at position 26: too deep constant definition tree
Error 4055 in line 1 at position 33: too deep constant definition tree
Error 4055 in line 1 at position 40: too deep constant definition tree
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 => 10,
	6 => 12,
	7 => 14,
	_ => 16,
}
", "10", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16,
}
", "16", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 => 10,
	6 => 12,
	7 => 14,
	_ => 16
}
", NullString, @"Error 2008 in line 12 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
}
", NullString, @"Error 2008 in line 8 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch
{
	0 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
}
", NullString, @"Error 2008 in line 3 at position 3: expected: ""if"" or =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
}
", NullString, @"Error 2008 in line 3 at position 7: expected: comma
")]
	[DataRow(@"return 12345678905 switch
{
	12345678900 => 12345678900,
	12345678901 => 12345678902,
	12345678902 => 12345678904,
	12345678903 => 12345678906,
	12345678904 => 12345678908,
	12345678905 => 123456789010,
	12345678906 => 123456789012,
	12345678907 => 123456789014,
	_ => 123456789016,
}
", "123456789010", "Ошибок нет")]
	[DataRow(@"return 12345678905 switch
{
	12345678900 => 12345678900,
	12345678901 => 12345678902,
	12345678902 => 12345678904,
	12345678903 => 12345678906,
	_ => 123456789016,
}
", "123456789016", "Ошибок нет")]
	[DataRow(@"return 5.1 switch
{
	0.1 => 0.1,
	1.1 => 2.1,
	2.1 => 4.1,
	3.1 => 6.1,
	4.1 => 8.1,
	5.1 => 10.1,
	6.1 => 12.1,
	7.1 => 14.1,
	_ => 16.1,
}
", "10.1", "Ошибок нет")]
	[DataRow(@"return 5.1 switch
{
	0.1 => 0.1,
	1.1 => 2.1,
	2.1 => 4.1,
	3.1 => 6.1,
	_ => 16.1,
}
", "16.1", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	""4"" => ""8"",
	""5"" => ""10"",
	""6"" => ""12"",
	""7"" => ""14"",
	_ => ""16"",
}
", @"""10""", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	_ => ""16"",
}
", @"""16""", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	""4"" => ""8"",
	""5"" => ""10"",
	""6"" => ""12"",
	""7"" => ""14"",
	_ => ""16""
}
", NullString, @"Error 2008 in line 12 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	_ => ""16""
}
", NullString, @"Error 2008 in line 8 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"const string s = """"
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	s + ""4"" => ""8"",
	s + ""5"" => ""10"",
	s + ""6"" => ""12"",
	s + ""7"" => ""14"",
	_ => ""16"",
}
", @"""10""", "Ошибок нет")]
	[DataRow(@"const string s = """"
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	_ => ""16"",
}
", @"""16""", "Ошибок нет")]
	[DataRow(@"const string s = ""A""
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	s + ""4"" => ""8"",
	s + ""5"" => ""10"",
	s + ""6"" => ""12"",
	s + ""7"" => ""14"",
	_ => ""16"",
}
", @"""10""", "Ошибок нет")]
	[DataRow(@"const string s = ""A""
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	_ => ""16"",
}
", @"""16""", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 if false => 10,
	6 => 12,
	7 => 14,
	_ => 16,
}
", "16", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false => 16,
	_ => -42,
}
", "-42", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => -5,
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => -42,
}
", "-42", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => -5,
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => ""error"",
}
", NullString, @"Error 4014 in line 8 at position 6: cannot convert from the type ""string"" to the type ""real"" during the switch expression translation
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => ""error"",
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => -42,
}
", NullString, @"Error 4015 in line 4 at position 6: there is no implicit conversion between the types ""byte"" and ""string""
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 if false 10,
	6 => 12,
	7 => 14,
	_ => 16,
}
", NullString, @"Error 2008 in line 8 at position 12: expected: =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false 16,
	_ => -42,
}
", NullString, @"Error 2008 in line 7 at position 12: expected: =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false => 16,
	_ => -42,

", NullString, @"Wreck 9007 in line 8 at position 10: unpaired bracket; expected: }
")]
	[DataRow(@"return 5 switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 }
", "16", "Ошибок нет")]
	[DataRow(@"return 5
switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 }
", "5", @"Warning 8005 in line 2 at position 0: the unreachable code has been detected
")]
	[DataRow(@"return 5 switch
{ 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 }
", NullString, @"Error 2008 in line 2 at position 42: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16
}
", NullString, @"Error 2008 in line 2 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch { }
", NullString, @"Error 2033 in line 1 at position 18: the switch expression cannot be empty
")]
	[DataRow(@"return typeof(5)
", "byte", "Ошибок нет")]
	[DataRow(@"return typeof(123456)
", "int", "Ошибок нет")]
	[DataRow(@"return typeof(5.1)
", "real", "Ошибок нет")]
	[DataRow(@"return typeof(""5"")
", "string", "Ошибок нет")]
	[DataRow(@"var x = 5
return typeof(x)
", "byte", "Ошибок нет")]
	[DataRow(@"var x = 123456
return typeof(x)
", "int", "Ошибок нет")]
	[DataRow(@"var x = 5.1
return typeof(x)
", "real", "Ошибок нет")]
	[DataRow(@"var x = ""5""
return typeof(x)
", "string", "Ошибок нет")]
	[DataRow(@"return typeof ""5""
", NullString, @"Error 200A in line 1 at position 14: expected: (
Error 2002 in line 1 at position 14: expected: end of the line
")]
	[DataRow(@"var x = ""5""
return typeof x
", NullString, @"Error 200A in line 2 at position 14: expected: (
Error 2002 in line 2 at position 14: expected: end of the line
")]
	[DataRow(@"return typeof()
", NullString, @"Error 200E in line 1 at position 14: expected: expression
")]
	[DataRow(@"var x = 5.1
typename T = typeof(x)
T y = 8
return typeof(y)
", "real", "Ошибок нет")]
	[DataRow(@"var x = ""5""
typename T = typeof(x)
T y = 8
return typeof(y)
", "string", "Ошибок нет")]
	[DataRow(@"var x = 5
typename T = typeof(x)
T y = ""8""
return typeof(y)
", "byte", "Ошибок нет")]
	[DataRow(@"typename T = typename
return T
", NullString, @"Error 4090 in line 1 at position 13: the recursive type cannot be value of itself
")]
	[DataRow(@"const typename T = typename
return T
", NullString, @"Error 4090 in line 1 at position 19: the recursive type cannot be value of itself
")]
	[DataRow(@"typename real = int
typename T = typeof(real)
return T
", NullString, @"Error 4091 in line 2 at position 20: cannot get type of the type
")]
	[DataRow(@"typename typename = int
return typename
", NullString, @"Error 4092 in line 1 at position 9: the recursive type variable, property or constant cannot have the name ""typename""
Error 4001 in line 1 at position 20: the identifier ""int"" is not defined in this location
Error 4001 in line 2 at position 7: the identifier ""typename"" is not defined in this location
")]
	[DataRow(@"return Sqrt(i)
", "0.7071067811865476+0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"return Sqrt(-i)
", "0.7071067811865476-0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"return Exp(i)
", "0.5403023058681398+0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"return Exp(-i)
", "0.5403023058681398-0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"return ln i
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"return ln (-i)
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"return Log(E, i)
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"return Log(E, -i)
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"return Log(i, i)
", NullString, @"Error 4026 in line 1 at position 11: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"return Log(i, -i)
", NullString, @"Error 4026 in line 1 at position 11: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"return ln (-5c)
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"return Log(E, -5c)
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"return ln (+5c)
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"return Log(E, +5c)
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"var x = Sqrt(i)
return x
", "0.7071067811865476+0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"var x = Sqrt(-i)
return x
", "0.7071067811865476-0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"var x = Exp(i)
return x
", "0.5403023058681398+0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"var x = Exp(-i)
return x
", "0.5403023058681398-0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"var x = ln i
return x
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = ln (-i)
return x
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = Log(E, i)
return x
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = Log(E, -i)
return x
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = Log(i, i)
return x
", NullString, @"Error 4026 in line 1 at position 12: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"var x = Log(i, -i)
return x
", NullString, @"Error 4026 in line 1 at position 12: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"var x = ln (-5c)
return x
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"var x = Log(E, -5c)
return x
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"var x = ln (+5c)
return x
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"var x = Log(E, +5c)
return x
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"var x = i
return Sqrt(x)
", "0.7071067811865476+0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"var x = -i
return Sqrt(x)
", "0.7071067811865476-0.7071067811865476i", "Ошибок нет")]
	[DataRow(@"var x = i
return Exp(x)
", "0.5403023058681398+0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"var x = -i
return Exp(x)
", "0.5403023058681398-0.8414709848078965i", "Ошибок нет")]
	[DataRow(@"var x = i
return ln x
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = -i
return ln x
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = i
return Log(E, x)
", "0+1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = -i
return Log(E, x)
", "0-1.5707963267948966i", "Ошибок нет")]
	[DataRow(@"var x = i
return Log(i, x)
", NullString, @"Error 4026 in line 2 at position 11: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"var x = -i
return Log(i, x)
", NullString, @"Error 4026 in line 2 at position 11: incompatibility between the type of the parameter of the call ""complex"" and the type of the parameter of the function ""real""
")]
	[DataRow(@"var x = -5c
return ln x
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"var x = -5c
return Log(E, x)
", "1.6094379124341003+3.141592653589793i", "Ошибок нет")]
	[DataRow(@"var x = +5c
return ln x
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"var x = +5c
return Log(E, x)
", "1.6094379124341003+0i", "Ошибок нет")]
	[DataRow(@"return 100000000000000000*100000000000000000000
", "10000000000000000000000000000000000000", "Ошибок нет")]
	[DataRow(@"var x = 100000000000000000*100000000000000000000
return x
", "10000000000000000000000000000000000000", "Ошибок нет")]
	[DataRow(@"var x = 100000000000000000000
return 100000000000000000*x
", "10000000000000000000000000000000000000", "Ошибок нет")]
	[DataRow(@"var x = 100000000000000000
return x*100000000000000000000
", "10000000000000000000000000000000000000", "Ошибок нет")]
	[DataRow(@"return 1LL << 100
", @"1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"var x = 1LL
return x << 100
", "1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"var x = 1LL << 100
return x
", "1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"var x = 1LL << 100
return x & x - 1
", "0", "Ошибок нет")]
	[DataRow(@"return 1LL << 100LL
", NullString, @"Error 4081 in line 1 at position 11: the second operand of the operator ""<<"" must be of the type, convertible to int
")]
	[DataRow(@"var x = 1LL
return x << 100LL
", NullString, @"Error 4081 in line 2 at position 9: the second operand of the operator ""<<"" must be of the type, convertible to int
")]
	[DataRow(@"return 1LL >> 100LL
", NullString, @"Error 4081 in line 1 at position 11: the second operand of the operator "">>"" must be of the type, convertible to int
")]
	[DataRow(@"var x = 1LL
return x >> 100LL
", NullString, @"Error 4081 in line 2 at position 9: the second operand of the operator "">>"" must be of the type, convertible to int
")]
	[DataRow(@"long long a = 123456789012345678901234567890
long long b = 1000
long long c = a + b
long long d = a * b
long long e = a - b
long long f = a / b // целочисленное деление
long long g = a % b // остаток
bool isGreater = a > b
bool isEqual = a == b
long long abs = Abs(a)
long long pow_ = a pow 3
int sign = a.Sign // -1, 0, 1
int sign2 = (-a).Sign // -1, 0, 1
int x = a % 2147483648
string s = """" + a
return (a, b, c, d, e, f, g, isGreater, isEqual, abs, pow_, sign, sign2, x, s)
", "(123456789012345678901234567890, 1000, 123456789012345678901234568890, 123456789012345678901234567890000,"
		+ " 123456789012345678901234566890, 123456789012345678901234567, 890, true, false, 123456789012345678901234567890,"
		+ " 1881676372353657772546716040589641726257477229849409426207693797722198701224860897069000, 1, -1, 1312754386,"
		+ @" ""123456789012345678901234567890"")", "Ошибок нет")]
	[DataRow(@"unsigned long long a = 123456789012345678901234567890
unsigned long long b = 1000
unsigned long long c = a + b
unsigned long long d = a * b
unsigned long long e = a - b
unsigned long long f = a / b // целочисленное деление
unsigned long long g = a % b // остаток
bool isGreater = a > b
bool isEqual = a == b
unsigned long long abs = Abs(a)
unsigned long long negative = -a
var negativeType = typeof(-a)
unsigned long long pow_ = a pow 3
int sign = a.Sign // -1, 0, 1
int sign2 = (-a).Sign // -1, 0, 1
unsigned int x = a % 2147483648
string s = """" + a
return (a, b, c, d, e, f, g, isGreater, isEqual, abs, negative, negativeType, pow_, sign, sign2, x, s)
", NullString, @"Error 4027 in line 11 at position 28: the conversion from the type ""long long"" to the type ""unsigned long long"" is possible only in the function return, not in the direct assignment and not in the call
Error 4001 in line 18 at position 54: the identifier ""negative"" is not defined in this location
")]
	[DataRow(@"return 2LL pow 100
", @"1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"var x = 2LL
return x pow 100
", "1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"var x = 2LL pow 100
return x
", "1267650600228229401496703205376", "Ошибок нет")]
	[DataRow(@"return 2LL pow 100LL
", NullString, @"Error 4006 in line 1 at position 11: cannot apply the operator ""pow"" to the types ""long long"" and ""long long""
")]
	[DataRow(@"var x = 2LL
return x pow 100LL
", NullString, @"Error 4006 in line 2 at position 9: cannot apply the operator ""pow"" to the types ""long long"" and ""long long""
")]
	[DataRow(@"complex c1 = 3.0+4.0i
complex c2 = 5.0
complex sum = c1 + c2
complex diff = c1 - c2
complex prod = c1 * c2
complex quot = c1 / c2
complex conjugate = complex.Conjugate(c1)
complex sqrt = complex.Sqrt(c1)
complex log = complex.Ln(c1)
real abs = c1.Magnitude
real arg = c1.Phase
bool eq = c1 == c2
bool ne = c1 != c2
complex polar = complex.FromPolarCoordinates(5.0, Pi / 4)
string str = """" + c1
return (c1, c2, sum, diff, prod, quot, conjugate, sqrt, log, abs, arg, eq, ne, polar, str)
", "(3+4i, 5+0i, 8+4i, -2+4i, 15+20i, 0.6+0.8i, 3-4i, 2+1i, 1.6094379124341003+0.9272952180016122i, 5, 0.9272952180016122,"
		+ @" false, true, 3.5355339059327378+3.5355339059327378i, ""3+4i"")", "Ошибок нет")]
	[DataRow(@"complex c1 = 3.0+4.0i
complex c2 = 5.0
complex zero = complex.Zero
complex divByZero = c1 / zero
complex nan = Uncty
complex inf = Infty
complex bad = nan + inf
int i = c1 % 2147483648
real r = c1
complex pow_ = c1 ** 2
complex badPolar = complex.FromPolarCoordinates(-1.0, Math.PI) // Модуль < 0 — неявно обрабатывается, но может быть неочевидно
complex c4 = new complex(0.1 + 0.2, 0)
complex c5 = new complex(0.3, 0)
bool almostEqual = c4 == c5 // Может быть false из-за погрешностей FP!
return (c1, c2, zero, divByZero, nan, inf, bad, i, r, pow_, badPolar, almostEqual)
", NullString, @"Error 2012 in line 10 at position 19: expected: identifier or basic expression or expression in round brackets
Error 4006 in line 8 at position 11: cannot apply the operator ""%"" to the types ""complex"" and ""unsigned int""
Error 4014 in line 9 at position 7: cannot convert from the type ""complex"" to the type ""real"" during the assignment
Error 4001 in line 15 at position 51: the identifier ""r"" is not defined in this location
")]
	[DataRow(@"var x = 7c
var y = 2
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""byte""
")]
	[DataRow(@"var x = 7i
var y = 2
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""byte""
")]
	[DataRow(@"var x = 7c
var y = 2r
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""real""
")]
	[DataRow(@"var x = 7i
var y = 2r
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""real""
")]
	[DataRow(@"var x = 7c
var y = 2c
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""complex""
")]
	[DataRow(@"var x = 7i
var y = 2c
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""complex""
")]
	[DataRow(@"var x = 7c
var y = 2i
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""complex""
")]
	[DataRow(@"var x = 7i
var y = 2i
return x % y
", NullString, @"Error 4006 in line 3 at position 9: cannot apply the operator ""%"" to the types ""complex"" and ""complex""
")]
	[DataRow(@"var x = 200 >>> 3
return x
", "25", "Ошибок нет")]
	[DataRow(@"var x = -281470681808896 >>> 16
return x
", "281470681808895", "Ошибок нет")]
	[DataRow(@"int x = 200
var y = x >>> 3
return y
", "25", "Ошибок нет")]
	[DataRow(@"long int x = -281470681808896
var y = x >>> 16
return y
", "281470681808895", "Ошибок нет")]
	[DataRow(@"var z = 0 >>> 10
return z
", "0", "Ошибок нет")]
	[DataRow(@"int zero = 0
var z = zero >>> 10
return z
", "0", "Ошибок нет")]
	[DataRow(@"unsigned int zero = 0
var z = zero >>> 10
return z
", "0", "Ошибок нет")]
	[DataRow(@"var x = 200r >>> 3
return x
", NullString, @"Error 4083 in line 1 at position 13: the first operand of the operators ""<<<"" and "">>>"" must be of the type byte, short char, short int, unsigned short int, char, int, unsigned int, long char, long int, unsigned long int, long long or unsigned long long
")]
	[DataRow(@"real zero = 0
var z = zero >>> 10
return z
", NullString, @"Error 4083 in line 2 at position 13: the first operand of the operators ""<<<"" and "">>>"" must be of the type byte, short char, short int, unsigned short int, char, int, unsigned int, long char, long int, unsigned long int, long long or unsigned long long
")]
	[DataRow(@"real zero = 1 << 28
var z = zero >>> 10
return z
", NullString, @"Error 4083 in line 2 at position 13: the first operand of the operators ""<<<"" and "">>>"" must be of the type byte, short char, short int, unsigned short int, char, int, unsigned int, long char, long int, unsigned long int, long long or unsigned long long
")]
	[DataRow(@"unsigned int x = 100 >>> 2.5
return x
", NullString, @"Error 4081 in line 1 at position 21: the second operand of the operator "">>>"" must be of the type, convertible to int
")]
	[DataRow(@"int a = 10
unsigned int b = 5
unsigned int c = a >>> b
return c
", NullString, @"Error 4081 in line 3 at position 19: the second operand of the operator "">>>"" must be of the type, convertible to int
")]
	[DataRow(@"unsigned int x = 100
unsigned int y = x >>> 2.5
return y
", NullString, @"Error 4081 in line 2 at position 19: the second operand of the operator "">>>"" must be of the type, convertible to int
")]
	[DataRow(@"var x = 200r << 3
return x
", "1600", "Ошибок нет")]
	[DataRow(@"var x = 200r <<< 3
return x
", NullString, @"Error 4083 in line 1 at position 13: the first operand of the operators ""<<<"" and "">>>"" must be of the type byte, short char, short int, unsigned short int, char, int, unsigned int, long char, long int, unsigned long int, long long or unsigned long long
")]
	[DataRow(@"var x = 200r >> 3
return x
", "25", "Ошибок нет")]
	[DataRow(@"real zero = 0
var z = zero << 10
return z
", "0", "Ошибок нет")]
	[DataRow(@"real r = 1 << 8
var z = r << 10
return z
", "262144", "Ошибок нет")]
	[DataRow(@"real r = 1 << 8
var z = r <<< 10
return z
", NullString, @"Error 4083 in line 2 at position 10: the first operand of the operators ""<<<"" and "">>>"" must be of the type byte, short char, short int, unsigned short int, char, int, unsigned int, long char, long int, unsigned long int, long long or unsigned long long
")]
	[DataRow(@"real zero = 0
var z = zero >> 10
return z
", "0", "Ошибок нет")]
	[DataRow(@"real r = 1 << 28
var z = r >> 10
return z
", "262144", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_1100_1010_0000_0000_0000_0000_0110_1001 <<< 4
return """" + Convert.ToUnsafeString(a, 2)
", @"""10100000000000000000011010011100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001 <<< 26
return """" + Convert.ToUnsafeString(a, 2)
", @"""10100101""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001 <<< int.MaxValue
return """" + Convert.ToUnsafeString(a, 2)
", @"""10110100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001 <<< -1
return """" + Convert.ToUnsafeString(a, 2)
", @"""10110100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001 <<< -int.MaxValue
return """" + Convert.ToUnsafeString(a, 2)
", @"""11010010""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001_0110_1001 <<< 26
return """" + Convert.ToUnsafeString(a, 2)
", @"""1010010110100101""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_1011_0100_1011_0100 <<< 26
return """" + Convert.ToUnsafeString(a, 2)
", @"""1101001011010010""", "Ошибок нет")]
	[DataRow(@"using System
long long a = 0b_1100_1010_0000_0000_0000_0000_0110_1001_1100_1010_0000_0000_0000_0000_0110_1001 <<< 4
return """" + a.ToUnsafeString(2)
", @"""1010000000000000000001101001110010100000000000000000011010011100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_1100_1010_0000_0000_0000_0000_0110_1001
var b = a <<< 4
return """" + Convert.ToUnsafeString(b, 2)
", @"""10100000000000000000011010011100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001
var b = a <<< 26
return """" + Convert.ToUnsafeString(b, 2)
", @"""10100101""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001
var b = a <<< int.MaxValue
return """" + Convert.ToUnsafeString(b, 2)
", @"""10110100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001
var b = a <<< -1
return """" + Convert.ToUnsafeString(b, 2)
", @"""10110100""", "Ошибок нет")]
	[DataRow(@"using System
var a = 0b_0110_1001
var b = a <<< -int.MaxValue
return """" + Convert.ToUnsafeString(b, 2)
", @"""11010010""", "Ошибок нет")]
	[DataRow(@"using System
long long a = 0b_1100_1010_0000_0000_0000_0000_0110_1001_1100_1010_0000_0000_0000_0000_0110_1001
var b = a <<< 4
return """" + b.ToUnsafeString(2)
", @"""11001010000000000000000001101001110010100000000000000000011010010000""", "Ошибок нет")]
	[DataRow(@"return (false ^^ false, false ^^ true, true ^^ false, true ^^ true, false ^^ false ^^ false, false ^^ true ^^ false, true ^^ false ^^ false, true ^^ true ^^ false, false ^^ false ^^ true, false ^^ true ^^ true, true ^^ false ^^ true, true ^^ true ^^ true)
", "(false, true, true, false, false, true, true, false, true, false, false, false)",
		@"Warning 800F in line 1 at position 0: too long line (128 characters are supported, actually 255)
")]
	[DataRow(@"return (false ^^ 5, 5 ^^ false, 5 ^^ 5, false ^^ 5 ^^ false, 5 ^^ false ^^ false, 5 ^^ 5 ^^ false, false ^^ false ^^ 5, false ^^ 5 ^^ 5, 5 ^^ false ^^ 5, 5 ^^ 5 ^^ 5)
", NullString, @"Warning 800F in line 1 at position 0: too long line (128 characters are supported, actually 166)
Error 4084 in line 1 at position 17: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 20: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 32: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 49: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 61: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 82: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 117: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 129: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 137: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 1 at position 154: the operator ""^^"" works only with the operands of the type, convertible to bool
")]
	[DataRow(@"return 8++
", NullString, @"Error 4002 in line 1 at position 8: cannot apply this operator to this constant
")]
	[DataRow(@"return 3--
", NullString, @"Error 4002 in line 1 at position 8: cannot apply this operator to this constant
")]
	[DataRow(@"return false!!
", NullString, @"Error 4002 in line 1 at position 12: cannot apply this operator to this constant
")]
	[DataRow(@"var f = false
var t = true
return (f ^^ f, f ^^ t, t ^^ f, t ^^ t, f ^^ f ^^ f, f ^^ t ^^ f, t ^^ f ^^ f, t ^^ t ^^ f, f ^^ f ^^ t, f ^^ t ^^ t, t ^^ f ^^ t, t ^^ t ^^ t)
", "(false, true, true, false, false, true, true, false, true, false, false, false)",
		@"Warning 800F in line 3 at position 0: too long line (128 characters are supported, actually 143)
")]
	[DataRow(@"var f = false
var t = 5
return (f ^^ t, t ^^ f, t ^^ t, f ^^ t ^^ f, t ^^ f ^^ f, t ^^ t ^^ f, f ^^ f ^^ t, f ^^ t ^^ t, t ^^ f ^^ t, t ^^ t ^^ t)
", NullString, @"Error 4084 in line 3 at position 13: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 16: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 24: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 37: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 45: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 58: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 81: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 89: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 97: the operator ""^^"" works only with the operands of the type, convertible to bool
Error 4084 in line 3 at position 110: the operator ""^^"" works only with the operands of the type, convertible to bool
")]
	[DataRow(@"return  Q()
", @"""return  Q()\r\n""", @"Warning 800C in line 1 at position 7: redundant space(s)
")]
	[DataRow(@"						return Q()
", NullString, @"Wreck 9014 in line 1 at position 5: too many sequential tabs (only 5 are supported)
")]
	[DataRow("return   " + "   " + "   " + @"Q()
", NullString, @"Warning 800C in line 1 at position 7: redundant space(s)
Wreck 9015 in line 1 at position 14: too many sequential whitespaces
")]
	[DataRow("return \t \t \t \t " + @"Q()
", NullString, @"Wreck 9015 in line 1 at position 14: too many sequential whitespaces
")]
	[DataRow(@" return Q()
", NullString, @"Wreck 9016 in line 1 at position 0: spaces instead of tabs at the line start
")]
	[DataRow("\t" + @" return Q()
", NullString, @"Wreck 9016 in line 1 at position 1: spaces instead of tabs at the line start
")]
	[DataRow(@"real temperature = 35.2
string category = temperature switch 
{
	< 0 => ""Freezing"",
	>= 0 and < 20 => ""Cold"",
	>= 20 and <= 30 => ""Warm"",
	> 30 => ""Hot"",
	_ => ""Unknown"",
}
return category
", @"""Hot""", "Ошибок нет")]
	[DataRow(@"object item = ""Welcome!"";
if (item is string text)
	return text;
", @"""Welcome!""", "Ошибок нет")]
	[DataRow(@"int age = 25;
bool bool = age is >= 18 and <= 30;
if (""A"" is not null)
	return bool;
", "true", "Ошибок нет")]
	[DataRow(@"real temperature = 35.2;
string category = temperature switch 
{
	< 0 => ""Freezing"",
	>= 0 and < 20 => ""Cold"",
	>= 20 and <= 30 => ""Warm"",
	> 30 => ""Hot"",
	_ => ""Unknown"",
};
return category;
", @"""Hot""", "Ошибок нет")]
	[DataRow(@"() int list = (5, 10, 15, 20, 25);
if (list is var data && data.Length > 0) 
	return ""Data retrieved!"";
", @"""Data retrieved!""", "Ошибок нет")]
	[DataRow(@"return null is _;
", "true", "Ошибок нет")]
	[DataRow(@"var x = null is _;
return x;
", "true", "Ошибок нет")]
	[DataRow(@"object obj = 5;
return (obj is byte, obj is real, IntToReal(obj) is real);
", NullString, @"Error 4026 in line 2 at position 44: incompatibility between the type of the parameter of the call ""object"" and the type of the parameter of the function ""System.IIntegerNumber[object]""
Error 40A1 in line 2 at position 49: the expression of the type ""null"" cannot be matched with the pattern of the type ""real""
")]
	[DataRow(@"int value = 10;
if (value is 10)
	return true;
", "true", "Ошибок нет")]
	[DataRow(@"object value = 42;
string result = value switch 
{
	byte i if i > 100 => ""Large number"",
	byte i => ""Number: "" + i,
	string s => ""Text: "" + s,
	null => ""Null value"",
	_ => ""Unknown type"",
};
return result;
", @"""Number: 42""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
ListHashSet[string] set = new(""A"", ""B"", ""C"");
if (set is MyClass)
	return false;
set = new MyClass();
if (set is MyClass)
	return true;
", "true", "Ошибок нет")]
	[DataRow(@"string text = ""hello"";
if (text is >= ""a"" and <= ""z"")
	return true;
", NullString, @"Error 40A0 in line 2 at position 12: the relational pattern matching can be only applied to the numbers
Error 40A0 in line 2 at position 23: the relational pattern matching can be only applied to the numbers
")]
	[DataRow(@"if (3.5 is not int)
	return true;
", NullString, @"Error 40A1 in line 1 at position 8: the expression of the type ""real"" cannot be matched with the pattern of the type ""int""
")]
	[DataRow(@"object obj = 42;
if (obj is var 123abc)
	return true;
", NullString, @"Error 200B in line 2 at position 15: expected: )
")]
	[DataRow(@"int num = 5;
string result = num switch
{
	_ if num > 10 => ""Big"",
	_ => ""Small"", 
	5 => ""Five"",
};
return result;
", NullString, @"Error 2034 in line 6 at position 1: the switch expression cannot contain cases after ""_""
")]
	[DataRow(@"int x = 5;
if (x is int i and real j)
	return true;
", NullString, @"Error 40A1 in line 2 at position 6: the expression of the type ""int"" cannot be matched with the pattern of the type ""real""
")]
	[DataRow(@"() int items = (1, 2, 3, 4, 5);
if (items is not () real list)
	return true;
", NullString, @"Error 40A1 in line 2 at position 10: the expression of the type ""list() int"" cannot be matched with the pattern of the type ""list() real""
")]
	[DataRow(@"object obj = 42;
if (obj is 5 or string s)
	return true;
", NullString, @"Error 40A2 in line 2 at position 13: the declaration patterns cannot be used with the operator ""or""
")]
	[DataRow(@"object obj = 42;
if (obj is not 5 and not string s)
	return true;
", NullString, @"Error 40A3 in line 2 at position 17: the negative declaration patterns cannot be used with the operator ""and""
")]
	[DataRow(@"var obj = 42;
if (obj is var x and >= 10)
	return true;
", "true", "Ошибок нет")]
	[DataRow(@"Record Chain(int Start, int End)
Chain chain = new(101, 200)
return chain.End - chain.Start + 1
", "100", "Ошибок нет")]
	[DataRow(@"Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
return pair
", @"(""Status"", 100)", "Ошибок нет")]
	[DataRow(@"Record Empty()
Empty obj = new()
return obj
", "()", "Ошибок нет")]
	[DataRow(@"Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
Pair pair2 = new(""Status"", 100)
Pair pair3 = new(""Status"", 200)
return (pair.Equals(pair2), pair.Equals(pair3))
", "(true, false)", "Ошибок нет")]
	[DataRow(@"Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
pair.Value = 200
return pair
", NullString, @"Error 4070 in line 3 at position 5: the property ""Pair.Value"" is get-only and cannot be set
")]
	[DataRow(@"Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
pair.Value++
return pair
", NullString, @"Error 4070 in line 3 at position 5: the property ""Pair.Value"" is get-only and cannot be set
")]
	[DataRow(@"abstract Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
return pair
", NullString, @"Error 0005 in line 1 at position 0: incorrect word or order of words in construction declaration
")]
	[DataRow(@"static Record Pair(string Name, int Value)
Pair pair = new(""Status"", 100)
return pair
", NullString, @"Error 0005 in line 1 at position 0: incorrect word or order of words in construction declaration
")]
	[DataRow(@"const string A1000000 = A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000;
const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
const string A10 = ""AAAAAAAAAA"";
return A1000000;
", A1000000, "Ошибок нет")]
	[DataRow(@"var a = 5
if (a < 0)
	a += 1
else if (a > 0)
	a -= 1
return a", "5", "Ошибок нет")]
	[DataRow(@"var a = -5
if (a >= 0)
	return a
else
{
	a += 1
	return -a
}", "4", "Ошибок нет")]
	[DataRow(@"var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"var a = -5;
if (a >= 0);
	return a;
else;
{;
	a += 1;
	return -a;
};", "4", "Ошибок нет")]
	[DataRow(@"var x = 5; // Single-line comment
x += 3; /*
;* Multi-line
*; comment
*/
return x; /{ There; /{ are; /{ nested; }/ comments; }/ here; }/", "8", "Ошибок нет")]
	[DataRow(@"IO null Function F()
{
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"IO null Function F(int n)
{
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"IO null Function F(IO int n)
{
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"null Function F(IO int n)
{
}
var x = 5;
x += 3;
return x;", NullString, @"Wreck 901E in line 1 at position 16: the IO context can be introduced only inside the IO function
")]
	[DataRow(@"IO int Function F()
{
	return 5;
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"IO int Function F(int n)
{
	return n * n;
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"IO int Function F(IO int n)
{
	return n * n;
}
var x = 5;
x += 3;
return x;", "8", "Ошибок нет")]
	[DataRow(@"int Function F(IO int n)
{
	return n * n;
}
var x = 5;
x += 3;
return x;", NullString, @"Wreck 901E in line 1 at position 15: the IO context can be introduced only inside the IO function
")]
	[DataRow(@"IO null Function F(int n)
{
	System.RedStarLinq.ToList((5, 10, 15, 20, 25));
}
var x = 5;
x += 3;
return x;", NullString, @"Error 4028 in line 3 at position 28: incompatibility between the type of the parameter of the call ""byte[5]"" and all possible types of the parameter of the function (""System.Collections.IEnumerable[T]"")
")]
	[DataRow(@"IO int Function F(int n)
{
	System.RedStarLinq.ToList((5, 10, 15, 20, 25));
	return n * n;
}
var x = 5;
x += 3;
return x;", NullString, @"Error 4028 in line 3 at position 28: incompatibility between the type of the parameter of the call ""byte[5]"" and all possible types of the parameter of the function (""System.Collections.IEnumerable[T]"")
")]
	[DataRow(@"IO null Function F(int n)
{
	(IO System.RedStarLinq).ToList[int]((5, 10, 15, 20, 25));
}
var x = 5;
x += 3;
return x;
", "8", "Ошибок нет")]
	[DataRow(@"IO int Function F(int n)
{
	(IO System.RedStarLinq).ToList[int]((5, 10, 15, 20, 25));
	return n * n;
}
var x = 5;
x += 3;
return x;
", "8", "Ошибок нет")]
	[DataRow(@"null Function F(int n)
{
	(IO System.RedStarLinq).ToList[int]((5, 10, 15, 20, 25));
}
var x = 5;
x += 3;
return x;", NullString, @"Wreck 901E in line 3 at position 2: the IO context can be introduced only inside the IO function
")]
	[DataRow(@"int Function F(int n)
{
	(IO System.RedStarLinq).ToList[int]((5, 10, 15, 20, 25));
	return n * n;
}
var x = 5;
x += 3;
return x;", NullString, @"Wreck 901E in line 3 at position 2: the IO context can be introduced only inside the IO function
")]
	[DataRow(@"using System;
using System.IO;
var a = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var b = @""Visual Studio 18\Projects\Добавить эту строку в .csproj всех проектов.txt"";
() byte list = (IO File).ReadAllBytes((IO Path).Combine(a, b));
(IO File).WriteAllBytes(@""D:\aaa.txt"", list);
(IO File).Delete(@""D:\aaa.txt"");
", NullString, "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
() byte bytes = (0, 0, 0)
(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
new IO FileInfo(@""D:\aaa.txt"").LastWriteTime = (IO DateTime).Now
(IO File).Delete(@""D:\aaa.txt"");
", NullString, "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
() byte bytes = (0, 0, 0)
(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
var a = new IO FileInfo(@""D:\aaa.txt"")
a.LastWriteTime += new TimeSpan(24, 0, 0)
return a.LastWriteTime - a.LastWriteTime
", "0", "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
() byte bytes = (0, 0, 0)
(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
var a = new IO FileInfo(@""D:\aaa.txt"")
var b = a.LastWriteTime
var c = b + new TimeSpan(24, 0, 0)
(IO File).Delete(@""D:\aaa.txt"");
return c - c
", "0", "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
() byte bytes = (0, 0, 0)
(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
var a = new IO FileInfo(@""D:\aaa.txt"")
var list = new(a, a, a)
list.Add(a)
list[4].LastWriteTime += new TimeSpan(24, 0, 0)
return list[4].LastWriteTime - list[4].LastWriteTime
", "0", "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
() byte bytes = (0, 0, 0)
(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
var a = new IO FileInfo(@""D:\aaa.txt"")
var list = new(a, a, a)
list.Add(a)
var b = list[4].LastWriteTime
var c = b + new TimeSpan(24, 0, 0)
(IO File).Delete(@""D:\aaa.txt"");
return c - c
", "0", "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
IO TimeSpan Function F()
{
	() byte bytes = (0, 0, 0)
	(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
	var a = new IO FileInfo(@""D:\aaa.txt"")
	a.LastWriteTime += new TimeSpan(24, 0, 0)
	return a.LastWriteTime - a.LastWriteTime
}
return (F(), F())
", "(0, 0)", "Ошибок нет")]
	[DataRow(@"using System;
using System.IO;
IO TimeSpan Function F()
{
	() byte bytes = (0, 0, 0)
	(IO File).WriteAllBytes(@""D:\aaa.txt"", bytes);
	var a = new IO FileInfo(@""D:\aaa.txt"")
	var b = a.LastWriteTime
	var c = b + new TimeSpan(24, 0, 0)
	(IO File).Delete(@""D:\aaa.txt"");
	return c - c
}
return (F(), F())
", "(0, 0)", "Ошибок нет")]
	[DataRow(@"using System.GUI
if (1 == 1)
	return null
var a = new IO GUIWindow()
a.Show()
a.Close()
", NullString, "Ошибок нет")]
	[DataRow(@"using System.GUI
if (1 == 1)
	return 1026
var a = new IO GUIWindow()
var b = a.Width
var c = b + 2
return c
", "1026", "Ошибок нет")]
	[DataRow(@"using System.GUI
if (1 == 1)
	return 1024
var a = new IO GUIWindow()
return a.Width
", "1024", "Ошибок нет")]
	[DataRow(@"using System.GUI
if (1 == 1)
	return 1152
var a = new IO GUIWindow()
a.Width += 128
return a.Width
", "1152", "Ошибок нет")]
	[DataRow(@"using System.GUI
if (1 == 1)
	return 1152
var a = new IO GUIWindow()
() IO GUIWindow windows = (a, a, a)
windows.Add(a)
windows[4].Position = new(windows[4].Position.X / 2, windows[4].Position.Y * 2)
windows[4].Show()
", "1152", "Ошибок нет")]
	[DataRow(@"() int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Insert(1, 8)
	return list
}
return F()
", NullString, @"Wreck 901C in line 4 at position 13: the method ""Insert"" cannot be used outside of the IO context
")]
	[DataRow(@"() int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Remove(2, 2)
	return list
}
return F()
", NullString, @"Wreck 901C in line 4 at position 13: the method ""Remove"" cannot be used outside of the IO context
")]
	[DataRow(@"() int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Replace(5, 8)
	return list
}
return F()
", NullString, @"Wreck 901C in line 4 at position 14: the method ""Replace"" cannot be used outside of the IO context
")]
	[DataRow(@"() int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list[1] = -42
	return list
}
return F()
", NullString, @"Wreck 901A in line 4 at position 9: the assignment to the expression, other than the declaration or the single variable, cannot be used outside of the IO context
")]
	[DataRow(@"() int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list[1] *= -1
	return list
}
return F()
", NullString, @"Wreck 901A in line 4 at position 9: the assignment to the expression, other than the declaration or the single variable, cannot be used outside of the IO context
")]
	[DataRow(@"IO () int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Insert(1, 8)
	return list
}
return F()
", "(5, 8, 10, 15, 20, 25)", "Ошибок нет")]
	[DataRow(@"IO () int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Remove(2, 2)
	return list
}
return F()
", "(5, 20, 25)", "Ошибок нет")]
	[DataRow(@"IO () int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list.Replace(5, 8)
	return list
}
return F()
", "(5, 10, 15, 20, 25)", "Ошибок нет")]
	[DataRow(@"IO () int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list[1] = -42
	return list
}
return F()
", "(-42, 10, 15, 20, 25)", "Ошибок нет")]
	[DataRow(@"IO () int Function F()
{
	() int list = (5, 10, 15, 20, 25)
	list[1] *= -1
	return list
}
return F()
", "(-5, 10, 15, 20, 25)", "Ошибок нет")]
	[DataRow(@"using System.Threading
var a = (IO DateTime).Now
Task.Delay(100)
return (IO DateTime).Now == a
", False, "Ошибок нет")]
	[DataRow(@"if (1 == 1)
	if (2 == 2)
		return 1
	else
		return 2
", NullString, @"Wreck 9020 in line 4 at position 1: only one nesting style is allowed without blocks: either a pure depth chain (nested if/loop/...) or a linear else-if chain - mixing them requires { }
")]
	[DataRow(@"if (1 == 1)
	return 1
else if (2 == 2)
	if (3 == 3)
		return 2
", NullString, @"Wreck 9020 in line 4 at position 1: only one nesting style is allowed without blocks: either a pure depth chain (nested if/loop/...) or a linear else-if chain - mixing them requires { }
")]
	[DataRow(@"if (1 == 1)
	if (2 == 2)
	{
		return 2
	}
", NullString, @"Wreck 9021 in line 3 at position 1: nested control flow without an enclosing block is not allowed. Add { } around the outer statement
")]
	[DataRow(@"return true & true == true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""=="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return true | true == true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""=="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return true ^ true == true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""=="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return true == true & true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""=="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return true == true | true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""=="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return true == true ^ true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""=="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return true == (true & true)
", "true", "Ошибок нет")]
	[DataRow(@"return true == (true | true)
", "true", "Ошибок нет")]
	[DataRow(@"return true == (true ^ true)
", False, "Ошибок нет")]
	[DataRow(@"return (true & true) == true
", "true", "Ошибок нет")]
	[DataRow(@"return (true | true) == true
", "true", "Ошибок нет")]
	[DataRow(@"return (true ^ true) == true
", False, "Ошибок нет")]
	[DataRow(@"return (true == true) & true
", "true", "Ошибок нет")]
	[DataRow(@"return (true == true) | true
", "true", "Ошибок нет")]
	[DataRow(@"return (true == true) ^ true
", False, "Ошибок нет")]
	[DataRow(@"return true & (true == true)
", "true", "Ошибок нет")]
	[DataRow(@"return true | (true == true)
", "true", "Ошибок нет")]
	[DataRow(@"return true ^ (true == true)
", False, "Ошибок нет")]
	[DataRow(@"return true & true != true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""!="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return true | true != true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""!="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return true ^ true != true
", NullString, @"Error 2009 in line 1 at position 19: ambiguous operator combination ""!="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return true != true & true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""!="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return true != true | true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""!="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return true != true ^ true
", NullString, @"Error 2009 in line 1 at position 26: ambiguous operator combination ""!="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return true != (true & true)
", False, "Ошибок нет")]
	[DataRow(@"return true != (true | true)
", False, "Ошибок нет")]
	[DataRow(@"return true != (true ^ true)
", "true", "Ошибок нет")]
	[DataRow(@"return (true & true) != true
", False, "Ошибок нет")]
	[DataRow(@"return (true | true) != true
", False, "Ошибок нет")]
	[DataRow(@"return (true ^ true) != true
", "true", "Ошибок нет")]
	[DataRow(@"return (true != true) & true
", False, "Ошибок нет")]
	[DataRow(@"return (true != true) | true
", "true", "Ошибок нет")]
	[DataRow(@"return (true != true) ^ true
", "true", "Ошибок нет")]
	[DataRow(@"return true & (true != true)
", False, "Ошибок нет")]
	[DataRow(@"return true | (true != true)
", "true", "Ошибок нет")]
	[DataRow(@"return true ^ (true != true)
", "true", "Ошибок нет")]
	[DataRow(@"return 1 & 1 >= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 | 1 >= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 ^ 1 >= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 >= 1 & 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 >= 1 | 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 >= 1 ^ 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 >= (1 & 1)
", "true", "Ошибок нет")]
	[DataRow(@"return 1 >= (1 | 1)
", "true", "Ошибок нет")]
	[DataRow(@"return 1 >= (1 ^ 1)
", "true", "Ошибок нет")]
	[DataRow(@"return (1 & 1) >= 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 | 1) >= 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 ^ 1) >= 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 >= 1) & 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 >= 1) | 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 >= 1) ^ 1
", "0", "Ошибок нет")]
	[DataRow(@"return 1 & (1 >= 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 | (1 >= 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 ^ (1 >= 1)
", "0", "Ошибок нет")]
	[DataRow(@"return 1 & 1 <= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 | 1 <= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 ^ 1 <= 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 <= 1 & 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<="" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 <= 1 | 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<="" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 <= 1 ^ 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<="" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 <= (1 & 1)
", "true", "Ошибок нет")]
	[DataRow(@"return 1 <= (1 | 1)
", "true", "Ошибок нет")]
	[DataRow(@"return 1 <= (1 ^ 1)
", False, "Ошибок нет")]
	[DataRow(@"return (1 & 1) <= 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 | 1) <= 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 ^ 1) <= 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 <= 1) & 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 <= 1) | 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 <= 1) ^ 1
", "0", "Ошибок нет")]
	[DataRow(@"return 1 & (1 <= 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 | (1 <= 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 ^ (1 <= 1)
", "0", "Ошибок нет")]
	[DataRow(@"return 1 & 1 > 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">"" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 | 1 > 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">"" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 ^ 1 > 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination "">"" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 > 1 & 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">"" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 > 1 | 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">"" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 > 1 ^ 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination "">"" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 > (1 & 1)
", False, "Ошибок нет")]
	[DataRow(@"return 1 > (1 | 1)
", False, "Ошибок нет")]
	[DataRow(@"return 1 > (1 ^ 1)
", "true", "Ошибок нет")]
	[DataRow(@"return (1 & 1) > 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 | 1) > 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 ^ 1) > 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 > 1) & 1
", "0", "Ошибок нет")]
	[DataRow(@"return (1 > 1) | 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 > 1) ^ 1
", "1", "Ошибок нет")]
	[DataRow(@"return 1 & (1 > 1)
", "0", "Ошибок нет")]
	[DataRow(@"return 1 | (1 > 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 ^ (1 > 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 & 1 < 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<"" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 | 1 < 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<"" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 ^ 1 < 1
", NullString, @"Error 2009 in line 1 at position 13: ambiguous operator combination ""<"" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 < 1 & 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<"" and ""&"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 < 1 | 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<"" and ""|"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 < 1 ^ 1
", NullString, @"Error 2009 in line 1 at position 9: ambiguous operator combination ""<"" and ""^"" detected - use parentheses for clarification
")]
	[DataRow(@"return 1 < (1 & 1)
", False, "Ошибок нет")]
	[DataRow(@"return 1 < (1 | 1)
", False, "Ошибок нет")]
	[DataRow(@"return 1 < (1 ^ 1)
", False, "Ошибок нет")]
	[DataRow(@"return (1 & 1) < 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 | 1) < 1
", False, "Ошибок нет")]
	[DataRow(@"return (1 ^ 1) < 1
", "true", "Ошибок нет")]
	[DataRow(@"return (1 < 1) & 1
", "0", "Ошибок нет")]
	[DataRow(@"return (1 < 1) | 1
", "1", "Ошибок нет")]
	[DataRow(@"return (1 < 1) ^ 1
", "1", "Ошибок нет")]
	[DataRow(@"return 1 & (1 < 1)
", "0", "Ошибок нет")]
	[DataRow(@"return 1 | (1 < 1)
", "1", "Ошибок нет")]
	[DataRow(@"return 1 ^ (1 < 1)
", "1", "Ошибок нет")]
	[DataRow(@"if (1 == 1)
	1
else if (2 == 2)
	2
", NullString, "Ошибок нет")]
	[DataRow(@"if (1 == 1)
else if (2 == 2)
	2
", NullString, @"Wreck 901B in line 2 at position 0: either condition without the core or ""else"" without the ""if"" detected
")]
	[DataRow(@"var n = 5
return 1 << n + 3
", "256", @"Warning 801F in line 2 at position 9: the operator combination ""<<"" and ""+"" may work not as intended - consider using parentheses for clarification
")]
	[DataRow(@"var n = 5
return 1 + 3 << n
", "128", @"Warning 801F in line 2 at position 13: the operator combination ""<<"" and ""+"" may work not as intended - consider using parentheses for clarification
")]
	[DataRow(@"real Function Factorial(int n)
{
	if (n <= 0)
		return 1
	return n * Factorial(n - 1)
}
return (Factorial(1), Factorial(5), Factorial(100), Factorial(-1))
", "(1, 120, 9.33262154439441E+157, 1)", "Ошибок нет")]
	[DataRow(@"real Function Sum(real n)
{
	if (n <= 1)
		return 1
	return n * n + Sum(Sqrt(n))
}
return (Sum(1), Sum(5), Sum(100), Sum(-1))
", "(1, 84.16238509777835, 10166.576295984165, 1)", "Ошибок нет")]
	[DataRow(@"real Function Power(real base_, int exp)
{
	if (exp <= 0)
		return 1
	real tmp = base_ * base_
	return base_ * Power(base_, exp - 1)
}
return (Power(2, 0), Power(2, 1), Power(2, 5), Power(3, 4))
", "(1, 2, 32, 81)", "Ошибок нет")]
	[DataRow(@"real Function FibTailLike(int n, int a, int b)
{
	int next = a + b
	if (n <= 0)
		return a
	return FibTailLike(n - 1, b, next)
}
return (FibTailLike(0, 0, 1), FibTailLike(1, 0, 1), FibTailLike(5, 0, 1))
", "(0, 1, 5)", "Ошибок нет")]
	[DataRow(@"IO int Function CountAndDec(int x)
{
	if (x <= 0)
		return 0
	return CountAndDec(x - 1)
}
return (CountAndDec(3), CountAndDec(0))
", "(0, 0)", "Ошибок нет")]
	[DataRow(@"int Function BlockRec(int x)
{
	int n = 1
	{
		if (x <= 0)
			return 0
		return BlockRec(x - 1)
	}
}
return (BlockRec(3), BlockRec(0))
", "(0, 0)", @"Warning 8021 in line 7 at position 9: if you wanted to create an optimized recursion, place the call directly in the function core block, without any conditions, loops, try-catch or subblocks, otherwise the optimization will not work
")]
	[DataRow(@"real Function ListRec(int n, () real outList)
{
	if (n <= 0)
		return n
	real tmp = ListRec(n - 1, outList) // <-- точно не хвост
	outList.Add(tmp)
	if (tmp > 10)
		return tmp + 1
	else
		return tmp - 1
}
() real results = new()
return (ListRec(5, results), results)
", "(-5, (0, -1, -2, -3, -4))", "Ошибок нет")]
	[DataRow(@"real Function BuildPath(int n, () real path)
{
	if (n <= 0)
		return 0
	real tmp = BuildPath(n - 1, path)
	path.Add(n)
	return tmp + n
}
() real route = new()
return (BuildPath(3, route), route)
", "(6, (1, 2, 3))", "Ошибок нет")]
	[DataRow(@"real Function BuildPath(int n, () real path)
{
	path.Add(n)
	if (n <= 0)
		return 0
	real tmp = BuildPath(n - 1, path)
	return tmp + n
}
() real route = new()
return (BuildPath(3, route), route)
", "(6, (3, 2, 1, 0))", "Ошибок нет")]
	[DataRow(@"int Function F(int x)
{
	while (x < 0)
	{
		if (x % 2 == 0)
			return 0;
		else
			return 1;
	}
	x++;
}
return F(-5);
", NullString, @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function F(int x)
{
	loop
	{
		if (x % 2 == 0)
			return 0;
		else
			return 1;
	}
	x++;
}
return F(-5);
", "1", @"Warning 8005 in line 10 at position 1: the unreachable code has been detected
")]
	[DataRow(@"int Function F(int x)
{
	if (x < 0)
	{
		if (x % 2 == 0)
			return 0;
		else
			return 1;
	}
	else
	{
		x++;
		if (x > 1)
		{
			for (int x in Chain(1, 20))
			{
				x++;
				return x;
			}
		}
	}
	return x * (x +/*=*/ 1);
}
return (F(-5), F(3), F(0));
", NullString, @"Error 4013 in line 15 at position 8: the variable ""x"" is already defined in this location or in the location that contains this in line 1 at position 15
")]
	[DataRow(@"int Function ForLoopFunction(list() int list)
{
	for (int i in Chain(1, list.Length))
		if (list[i] > 0)
			return list[i];
}
", NullString, @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function ComplexFunction(int x, list() int list)
{
	if (x > 0)
	{
		for (int i in Chain(1, list.Length))
			if (list[i] > x)
				return list[i];
	}
	else
	{
		while (x < 10)
			x++;
	}
	if (x % 2 == 0)
		return x;
}
", NullString, @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"using System;
Func[bool, int] isPrime = (number) => {
	if (number < 2)
		return false;
	for (int i in Chain(2, number - 2)) {
		if(number % i == 0) return false;
	}
	return true;
};
return (isPrime(1), isPrime(2), isPrime(3), isPrime(4), isPrime(5), isPrime(6), isPrime(7), isPrime(8));
", "(false, true, true, false, true, false, true, false)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (int i in Chain(0, 10)) while (i * i < 10)
{
	list.Add(i);
}
return list;
", "(0, 1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (int i in Chain(0, 10)) while! (i * i >= 10)
{
	list.Add(i);
}
return list;
", "(0, 1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (int i in Chain(0, 10)) while (i * i % 20 < 10)
{
	list.Add(i);
}
return list;
", "(0, 1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (int i in Chain(0, 10)) while! (i * i % 20 >= 10)
{
	list.Add(i);
}
return list;
", "(0, 1, 2, 3)", "Ошибок нет")]
	[DataRow(@"int n = 0;
for (i in 1..1000)
{
	n++;
}
return n;
", "1000", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (i in 1..10) while (i * i < 10)
{
	list.Add(i);
}
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (i in 1..10) while! (i * i >= 10)
{
	list.Add(i);
}
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (i in 1..10) while (i * i % 20 < 10)
{
	list.Add(i);
}
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (i in 1..10) while! (i * i % 20 >= 10)
{
	list.Add(i);
}
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"() int list = new();
for (i in 0..10)
{
	list.Add(i);
}
return list;
", NullString, @"Error 4082 in line 2 at position 11: the index operator and the range operator work only with the positive numbers
")]
	[DataRow(@"() int list = new();
for (i in ^10..^0)
{
	list.Add(i);
}
return list;
", NullString, @"Error 4082 in line 2 at position 15: the index operator and the range operator work only with the positive numbers
")]
	[DataRow(@"() int list = new();
for (i in 0..^0)
{
	list.Add(i);
}
return list;
", NullString, @"Error 4082 in line 2 at position 13: the index operator and the range operator work only with the positive numbers
Error 4082 in line 2 at position 11: the index operator and the range operator work only with the positive numbers
")]
	[DataRow(@"using System
return RedStarLinq.ToList[char](""Hello, world!"").Convert(x => x + 1)
", "(73, 102, 109, 109, 112, 45, 33, 120, 112, 115, 109, 101, 34)", "Ошибок нет")]
	[DataRow(@"using System
return ""Hello, world!"".ToList[char]().Convert(x => x + 1)
", "(73, 102, 109, 109, 112, 45, 33, 120, 112, 115, 109, 101, 34)", "Ошибок нет")]
	[DataRow(@"using System
return ""Hello, world!"".ToList[char]().Filter(x => x + 1)
", NullString, @"Error 4014 in line 2 at position 50: cannot convert from the type ""char"" to the type ""bool"" during the lambda translation
")]
	[DataRow(@"() real nums = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
var evens = nums.Filter(x => x % 2 == 0)
// evens == [2, 4, 6, 8, 10]
var large = nums.Filter(x => x > 7)
// large == [8, 9, 10]
return (evens, large)
", "((2, 4, 6, 8, 10), (8, 9, 10))", "Ошибок нет")]
	[DataRow(@"() real empty = new()
empty = empty.Filter(x => true)
() real nothing = (1, 2, 3)
nothing = nothing.Filter(x => false)
return (empty, nothing)
", "((), ())", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4)
var doubled = nums.Convert(x => x * 2)
var squared = nums.Convert(x => x * x)
return (doubled, squared)
", "((2, 4, 6, 8), (1, 4, 9, 16))", "Ошибок нет")]
	[DataRow(@"() int ints = (1, 2, 3)
() string strs = ints.Convert(x => x.ToUnsafeString().ToNString()).ToList()
return strs
", @"(""1"", ""2"", ""3"")", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4, 5)
real sumViaProgression = nums.Progression(0r, (acc, x) => acc + x)
real productViaProgression = nums.Progression(1r, (acc, x) => acc * x)
return (sumViaProgression, productViaProgression)
", "(15, 120)", "Ошибок нет")]
	[DataRow(@"() real empty = new()
real zero = empty.Progression(0r, (acc, x) => acc + x)
real one = empty.Progression(1r, (acc, x) => acc * x)
return (zero, one)
", "(0, 1)", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4, 5)
real s = nums.Sum()
return s
", "15", "Ошибок нет")]
	[DataRow(@"() real single = 42
real singleSum = single.Sum()
() real empty = new()
real emptySum = empty.Sum()
return (singleSum, emptySum)
", "(42, 0)", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4)
real p = nums.Product()
return p
", "24", "Ошибок нет")]
	[DataRow(@"() real withZero = (1, 2, 0, 4)
real hasZero = withZero.Product()
() real negatives = (-1, -2, -3)
real negProd = negatives.Product()
return (hasZero, negProd)
", "(0, -6)", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4, 5, 6)
real result = nums.Filter(x => x % 2 == 0).Convert(x => x * 2).Sum()
return result
", "24", "Ошибок нет")]
	[DataRow(@"() real single = (42.0)
() real singleJoined = single.ConvertAndJoin(x => new(x, x + 1)).ToList()
() real nums = (1, 2, 3, 4, 5, 6)
real numsJoined = nums.Filter(x => x % 2 == 0).Convert(x => x * 2).ConvertAndJoin(x => new(x, x + 1)).Sum()
return (singleJoined, numsJoined)
", "((42, 43), 51)", "Ошибок нет")]
	[DataRow(@"() real single = (42.0)
() real singleJoined = single.ConvertAndJoin[real, real](x => new(x, x + 1)).ToList()
() real nums = (1, 2, 3, 4, 5, 6)
real numsJoined = nums.Filter(x => x % 2 == 0).Convert(x => x * 2).ConvertAndJoin[real, real](x => new(x, x + 1)).Sum()
return (singleJoined, numsJoined)
", "((42, 43), 51)", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3, 4, 5)
bool hasNegative = nums.Any(x => x < 0)
bool allPositive = nums.All(x => x > 0)
return (hasNegative, allPositive)
", "(false, true)", "Ошибок нет")]
	[DataRow(@"() real nums = (10, 20, 30, 40)
real firstBig = nums.Find(x => x > 25)
real maybe = nums.Find(x => x > 100)
return (firstBig, maybe)
", "(30, 0)", "Ошибок нет")]
	[DataRow(@"using System.Collections
() real nums = (1, 2, 3, 4, 5, 6, 7)
Slice[real] firstThree = nums.Take(3)
Slice[real] afterTwo = nums.Skip(2)
Slice[real] combo = nums.Skip(1).Take(3)
return (firstThree, afterTwo, combo)
", "((1, 2, 3), (3, 4, 5, 6, 7), (2, 3, 4))", "Ошибок нет")]
	[DataRow(@"() real nums = (1, 2, 3)
real r = nums.NonExistent(x => true)
", NullString, @"Error 4033 in line 2 at position 14: the type ""list"" does not contain member ""NonExistent""
")]
	[DataRow(@"real x = 42
() real r = x.Filter(v => v > 0)
", NullString, @"Error 4033 in line 2 at position 14: the type ""real"" does not contain member ""Filter""
")]
	[DataRow(@"() real nums = (1, 2, 3)
real r = nums.Filter(true)
", NullString, @"Error 4028 in line 2 at position 21: incompatibility between the type of the parameter of the call ""bool"" and all possible types of the parameter of the function (""System.Func[bool, real]"", ""System.Func[bool, real, int]"")
")]
	[DataRow(@"() real nums = (1, 2, 3, 4)
real r = nums.Filter(x => x > 0).NonExistent(x => x * 2).Sum()
", NullString, @"Error 4033 in line 2 at position 33: the type ""System.Collections.IEnumerable"" does not contain member ""NonExistent""
")]
	[DataRow(@"() real nums = (1, 2, 3)
() real bad = nums.Filter()
() real bad2 = nums.Filter(x => x > 0, true)
", NullString, @"Error 4022 in line 2 at position 25: the function ""Filter"" must have 1 parameters
Error 4022 in line 3 at position 27: the function ""Filter"" must have 1 parameters
")]
	[DataRow(@"() int nums = (1, 2, 3)
real r = nums.Progression(""hello"", (acc, x) => acc + x)
return r
", NullString, @"Error 4014 in line 2 at position 47: cannot convert from the type ""int"" to the type ""char"" during the lambda translation
Error 4026 in line 2 at position 26: incompatibility between the type of the parameter of the call ""string"" and the type of the parameter of the function ""char""
")]
	[DataRow(@"() real nums = (1.0, 2.0, 3.0)
real r = nums.Progression(0r, (acc, x) => ""oops"")
return r
", NullString, @"Error 4014 in line 2 at position 42: cannot convert from the type ""string"" to the type ""real"" during the lambda translation
")]
	[DataRow(@"return ((5..10).ToList(), (..3).ToList(), (^10..^5).ToList(), (^3..).ToList(), (3..^3).Length, (..).Length)
", "((5, 6, 7, 8, 9, 10), (1, 2, 3), (2147483638, 2147483639, 2147483640, 2147483641, 2147483642, 2147483643), (2147483645, 2147483646, 2147483647), 2147483643, 2147483647)", "Ошибок нет")]
	[DataRow(@"private Enum PhaseOfMoon : byte
{
	NewMoon = 0,
	FirstQuarter = 1,
	FullMoon = 2,
	LastQuarter = 3,
}
return (PhaseOfMoon.FirstQuarter, typeof(PhaseOfMoon.FirstQuarter))
", NullString, @"Error 000C in line 1 at position 0: the private and protected types are allowed only inside the other types
")]
	[DataRow(@"private Enum PhaseOfMoon
{
	NewMoon = 0,
	FirstQuarter = 1,
	FullMoon = 2,
	LastQuarter = 3,
}
return (PhaseOfMoon.FirstQuarter, typeof(PhaseOfMoon.FirstQuarter))
", NullString, @"Error 000C in line 1 at position 0: the private and protected types are allowed only inside the other types
")]
	[DataRow(@"private Enum PhaseOfMoon : byte
{
	NewMoon = 0,
	FirstQuarter = 1,
	FullMoon = 2,
	LastQuarter = 3,
}
PhaseOfMoon x = PhaseOfMoon.FirstQuarter
return (x, typeof(x))
", NullString, @"Error 000C in line 1 at position 0: the private and protected types are allowed only inside the other types
")]
	[DataRow(@"private Enum PhaseOfMoon
{
	NewMoon = 0,
	FirstQuarter = 1,
	FullMoon = 2,
	LastQuarter = 3,
}
PhaseOfMoon x = PhaseOfMoon.FirstQuarter
return (x, typeof(x))
", NullString, @"Error 000C in line 1 at position 0: the private and protected types are allowed only inside the other types
")]
	[DataRow(@"Enum PhaseOfMoon : string
{
	NewMoon = ""NewMoon"",
	FirstQuarter = ""FirstQuarter"",
	FullMoon = ""FullMoon"",
	LastQuarter = ""LastQuarter"",
}
PhaseOfMoon x = PhaseOfMoon.FirstQuarter
return (x, typeof(x))
", @"(""FirstQuarter"", string)", "Ошибок нет")]
	[DataRow(@"Enum PhaseOfMoon : string
{
	NewMoon = ""NewMoon"",
	FirstQuarter = ""FirstQuarter"",
	FullMoon = ""FullMoon"",
	LastQuarter = ""LastQuarter"",
}
PhaseOfMoon x = PhaseOfMoon.FirstQuarter.Add('s')
return (x, typeof(x))
", @"(""FirstQuarters"", string)", "Ошибок нет")]
	[DataRow(@"Enum PhaseOfMoon : byte
{
	NewMoon = 0,
	FirstQuarter = 1,
	FullMoon = 2,
	LastQuarter = 3,
}
PhaseOfMoon x = PhaseOfMoon.FirstQuarter
real r = IntToReal(x)
() byte vals = (PhaseOfMoon.NewMoon, PhaseOfMoon.FullMoon)
bool ok = (x == PhaseOfMoon.LastQuarter)
() PhaseOfMoon list = (PhaseOfMoon.NewMoon, PhaseOfMoon.FirstQuarter, PhaseOfMoon.FullMoon)
real sum = list.Progression(0, (acc, v) => acc + v)
return (r, ok, sum)
", "(1, false, 3)", "Ошибок нет")]
	[DataRow(@"Enum ResourceType
{
	Wood = 0,
	Stone = 1,
	Iron = 2,
}
ResourceType t = ResourceType.Iron
() ResourceType types = (ResourceType.Wood, ResourceType.Stone)
bool anyIron = types.Any(x => x == ResourceType.Iron)
return anyIron
", "false", "Ошибок нет")]
	[DataRow(@"Enum EnemyState
{
	Idle = 0,
	Chasing = 1,
	Attacking = 2,
}
EnemyState current = EnemyState.Chasing
() real result = current switch
{
	(EnemyState.Idle) => 0,
	(EnemyState.Chasing) => 1,
	_ => -1,
}
return result
", "(1)", "Ошибок нет")]
	[DataRow(@"Enum A
{
	X = 0,
}
Enum B : A
{
	Y = 1,
}
", NullString, @"Error 203F in line 5 at position 9: the enum cannot be derived from the enum
")]
	[DataRow(@"Enum Color
{
	Red = 0,
	Green = 1,
	Red = 2,
}
", NullString, @"Error 2021 in line 5 at position 1: the property or constant ""Red"" is already defined in this region
")]
	[DataRow(@"Enum Season
{
	Spring = 0,
	Summer = 1,
}
Season s = Season.Autumn
", NullString, @"Error 4033 in line 6 at position 18: the type ""Season"" does not contain member ""Autumn""
")]
	[DataRow(@"Enum Empty { }
", NullString, @"Error 2040 in line 1 at position 0: the enum must have at least one member
")]
	[DataRow(@"Enum Flags : byte
{
	None = 0,
	Flag1 = -1,
}
return Flags.Flag1
", NullString, @"Error 4014 in line 4 at position 9: cannot convert from the type ""short int"" to the type ""byte"" during getting the constant value
")]
	[DataRow(@"using System
using System.Net
using System.Text
if (1 == 1)
	return null
IO TcpListener tcpListener = new((IO IPAddress).Any, 11000)
tcpListener.Start()
var client = tcpListener.AcceptTcpClient()
var stream = client.GetStream()
() byte receiveLen = RedStarLinq.EmptyList[byte](4)
stream.ReadExactly(receiveLen)
var receiveLen2 = BitConverter.ToInt(receiveLen)
var receive = RedStarLinq.EmptyList[byte](receiveLen2)
stream.ReadExactly(receive)
return Encoding.UTF8.GetString(receive).ToString()
", NullString, "Ошибок нет")]
	[DataRow(@"using System
using System.Net
using System.Text
if (1 == 1)
	return null
IO TcpClient client = new()
client.Connect(new((IO IPAddress).Loopback, 11000))
var netStream = client.GetStream()
var toSend = Encoding.UTF8.GetBytes(""Привет!"")
var toSendLen = BitConverter.GetBytes(toSend.Length)
netStream.Write(toSendLen)
netStream.Write(toSend)
netStream.Flush()
", NullString, "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass a1 = new MyClass();
MyClass a2 = new MyClass(8, 2.71828, ""$"");
MyClass a3 = new MyClass(8, 2.71828);
MyClass a4 = new MyClass(true);
return (a1, a2, a3, a4);
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	Namespace MyNamespace
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";

			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace.MyClass a1 = new MyNamespace.MyNamespace.MyClass();
MyNamespace.MyNamespace.MyClass a2 = new MyNamespace.MyNamespace.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace.MyClass a3 = new MyNamespace.MyNamespace.MyClass(8, 2.71828);
MyNamespace.MyNamespace.MyClass a4 = new MyNamespace.MyNamespace.MyClass(true);
return (a1, a2, a3, a4);
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	abstract Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";

			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace2.MyClass a1 = new MyNamespace.MyNamespace2.MyClass();
MyNamespace.MyNamespace2.MyClass a2 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace2.MyClass a3 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828);
MyNamespace.MyNamespace2.MyClass a4 = new MyNamespace.MyNamespace2.MyClass(true);
return (a1, a2, a3, a4);
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	static Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";

			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace2.MyClass a1 = new MyNamespace.MyNamespace2.MyClass();
MyNamespace.MyNamespace2.MyClass a2 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace2.MyClass a3 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828);
MyNamespace.MyNamespace2.MyClass a4 = new MyNamespace.MyNamespace2.MyClass(true);
return (a1, a2, a3, a4);
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"(int, int)[2] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int Function F1()
	{
		return 0;
	}

	int Function F2(int n)
	{
		return n * n;
	}

	static int Function G1()
	{
		return 0;
	}

	static int Function G2(int n)
	{
		return n * n;
	}
}
int Function F1()
{
	return 0;
}

int Function F2(int n)
{
	return n * n;
}
var a = new MyClass();
return (F1(10), F2(10, 10), F2(10.01), a.F1(10), a.F2(10, 10), a.F2(10.01), MyClass.G1(10), MyClass.G2(10, 10), MyClass.G2(10.01));
", NullString, @"Error 4022 in line 33 at position 11: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 19: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 31: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 44: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 54: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 68: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 87: the function ""G1"" must have 0 parameters
Error 4022 in line 33 at position 103: the function ""G2"" must have 1 parameters
Error 4027 in line 33 at position 123: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
")]
	[DataRow(@"static Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
MyClass.F();
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"static abstract Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
MyClass.F();
return MyClass.F(5);
", NullString, @"Error 0005 in line 1 at position 7: incorrect word or order of words in construction declaration
")]
	[DataRow(@"static sealed Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
MyClass.F();
return MyClass.F(5);
", NullString, @"Error 0005 in line 1 at position 7: incorrect word or order of words in construction declaration
")]
	[DataRow(@"static Class MyClass
{
	int Function F(int x)
	{
		F();
		null Function F()
		{
		}
		return x * x;
	}
}
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	int Function F(int x)
	{
		null Function F()
		{
		}
		F();
		return x * x;
	}
}
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
new MyClass().F();
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int Function F(int x)
	{
		F();
		null Function F()
		{
		}
		return x * x;
	}
}
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int Function F(int x)
	{
		null Function F()
		{
		}
		F();
		return x * x;
	}
}
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
var hs = new MyClass();
hs.Add("1");
hs.Add("2");
hs.Add("3");
hs.Add("2");
return hs;

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().Length;

""", "3", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().RemoveAt(2);

""", """("1", "3")""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
var hs = new MyClass2();
hs.Add("1");
hs.Add("2");
hs.Add("3");
hs.Add("2");
return hs;

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().Length;
", "3", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().RemoveAt(2);
", """("1", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
var hs = new MyClass2();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs;
", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F();
", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().Length;
", "3", "Ошибок нет")]
	[DataRow("""
using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().RemoveAt(2);

""", """("1", "3")""", "Ошибок нет")]
	[DataRow("""
using System.Collections;
abstract Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", NullString, """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error #4
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
var hs = new MyClass();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;

""", "3", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);

""", """(1, 3)""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
var hs = new MyClass2();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
using System.Collections;

Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;
", "3", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);
", """(1, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
var hs = new MyClass2();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;
", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();
", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;
", "3", "Ошибок нет")]
	[DataRow("""
using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);

""", """(1, 3)""", "Ошибок нет")]
	[DataRow("""
using System.Collections;
abstract Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", NullString, """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error #4
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""")]
	[DataRow(@"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass a1 = new MyClass2();
MyClass a2 = new MyClass2(8, 2.71828, ""$"");
MyClass a3 = new MyClass2(8, 2.71828);
MyClass a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	Namespace MyNamespace
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	static Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace2.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
{
	sealed Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace2.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int a = 5;
	real b = 3.14159;
}

Class MyClass2 : MyClass
{
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", NullString, @"Error 2015 in line 8 at position 17: expected: non-sealed class or interface
Error 4001 in line 13 at position 3: the identifier ""a"" is not defined in this location
Error 4060 in line 17 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
Error 4060 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
")]
	[DataRow(@"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

static Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", NullString, @"Error 0009 in line 8 at position 22: a static class cannot be derived
Error 2024 in line 16 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 17 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 18 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 19 at position 18: cannot create an instance of the static type ""MyClass2""
Error 4000 in line 16 at position 26: internal compiler error #4
Error 4000 in line 17 at position 26: internal compiler error #4
Error 4000 in line 18 at position 26: internal compiler error #4
Error 4000 in line 19 at position 26: internal compiler error #4
Error 4001 in line 20 at position 8: the identifier ""a1"" is not defined in this location
Error 4001 in line 20 at position 12: the identifier ""a2"" is not defined in this location
Error 4001 in line 20 at position 16: the identifier ""a3"" is not defined in this location
Error 4001 in line 20 at position 20: the identifier ""a4"" is not defined in this location
")]
	[DataRow(@"Class MyClass
{
	static Class N
	{
		MyClass S = new MyClass();
	}
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}
return (MyClass.N.S);
", @"new MyClass(5, 3.14159, ""A"")", "Ошибок нет")]
	[DataRow(@"Class MyClass2 : MyClass
{
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}

Class MyClass
{
	int a = 5;
	real b = 3.14159;
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", NullString, @"Error 4060 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 2 parameters
")]
	[DataRow(@"Class Person
{
	private string name;
	private int age;

	string Function GetName()
	{
		return name;
	}

	int Function GetAge()
	{
		return age;
	}
}

Person person = new Person(""Alice"", 30);
return (person.GetName(), person.GetAge());
", @"(""Alice"", 30)", "Ошибок нет")]
	[DataRow(@"Class Person
{
	private string name;
	private int age;

	string Function GetName()
	{
		return name;
	}

	string Function GetAge()
	{
		return age;
	}
}

Person person = new Person(""Alice"", 30);
return (person.GetName(), person.GetAge());
", NullString, @"Error 402B in line 13 at position 9: incompatibility between the type of the returning value ""int"" and the function return type ""string"" - use an addition of zero-length string for this
")]
	[DataRow(@"Class Animal
{
	protected string species;

	string Function GetSpecies()
	{
		return species;
	}

	string Function Speak()
	{
		return ""Animal sound"";
	}
}

Class Dog : Animal
{
	Constructor()
	{
		species = ""Dog"";
	}

	string Function Bark()
	{
		return ""Woof!"";
	}
}

Dog dog = new Dog();
return (dog.GetSpecies(), dog.Speak(), dog.Bark());
", @"(""Dog"", ""Animal sound"", ""Woof!"")", "Ошибок нет")]
	[DataRow(@"Class Vehicle
{
	string Function Start()
	{
		return ""Vehicle starting"";
	}
}

Class Car : Vehicle
{
	string Function Start()
	{
		return ""Car starting"";
	}
}

Vehicle vehicle = new Car();
return vehicle.Start();
", "\"Car starting\"", "Ошибок нет")]
	[DataRow(@"Class Engine
{
	string Function Start()
	{
		return ""Engine starting"";
	}
}

Class Car
{
	private Engine engine = new Engine();

	string Function Start()
	{
		return engine.Start() + ""\r\nCar is now running"";
	}
}

Car car = new Car();
return car.Start();
", @"""Engine starting\r\nCar is now running""", "Ошибок нет")]
	[DataRow(@"Class BaseClass
{
	string Function Display()
	{
		return ""Display from BaseClass"";
	}

	string Function Info()
	{
		return ""Info from BaseClass"";
	}
}

Class DerivedClass : BaseClass
{
	string Function Display()
	{
		return ""Display from DerivedClass"";
	}

	new string Function Info()
	{
		return ""Info from DerivedClass"";
	}
}

BaseClass obj1 = new DerivedClass();
DerivedClass obj2 = new DerivedClass();
return (obj1.Display(), obj1.Info(), obj2.Display(), obj2.Info());
", """("Display from DerivedClass", "Info from BaseClass", "Display from DerivedClass", "Info from DerivedClass")""",
			"Ошибок нет")]
	[DataRow(@"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

Class Dog : Animal
{
	string Function Speak()
	{
		return ""Woof"";
	}

	string Function Eat()
	{
		return ""Dog is eating"";
	}
}

Class Cat : Animal
{
	string Function Speak()
	{
		return ""Meow"";
	}

	// Не переопределяем метод Eat, используем базовую реализацию
}

Animal myDog = new Dog();
Animal myCat = new Cat();
return (myDog.Speak(), myDog.Eat(), myCat.Speak(), myCat.Eat());
", """("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Ошибок нет")]
	[DataRow(@"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

Class Dog : Animal
{
	string Function Speak()
	{
		return ""Woof"";
	}

	string Function Eat()
	{
		return ""Dog is eating"";
	}
}

Class Cat : Animal
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myDog = new Dog();
Animal myCat = new Cat();
return (myDog.Speak(), myDog.Eat(), myCat.Speak(), myCat.Eat());
", """("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Warning 8008 in line 30 at position 1: the method \"Eat\"" +
			" has the same parameter types as its base method with the same name but it also" +
			" has the other significant differences such as the access modifier or the return type," +
			" so it cannot override that base method and creates a new one;" +
			" if this is intentional, add the \"new\" keyword, otherwise fix the differences\r\n")]
	[DataRow(@"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

sealed Class Cat : Animal
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", """("Meow", "Animal is eating")""", "Warning 8008 in line 17 at position 1: the method \"Eat\"" +
			" has the same parameter types as its base method with the same name but it also" +
			" has the other significant differences such as the access modifier or the return type," +
			" so it cannot override that base method and creates a new one;" +
			" if this is intentional, add the \"new\" keyword, otherwise fix the differences\r\n")]
	[DataRow(@"Class Cat : int
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", NullString, @"Error 2015 in line 1 at position 12: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: end of the line
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : long int
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", NullString, @"Error 2015 in line 1 at position 12: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: end of the line
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : System.RedStarLinqExtras
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", NullString, @"Error 2015 in line 1 at position 19: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: end of the line
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : System.Func[int]
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", NullString, @"Error 2015 in line 1 at position 19: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: end of the line
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class MyClass
{
	abstract string Function Go();
}
", NullString, @"Error 400A in line 3 at position 10: the abstract members can be located only inside the abstract classes
")]
	[DataRow(@"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(ref a);
F(ref a);
F(ref a);
return a;
", "8", "Ошибок нет")]
	[DataRow(@"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(a);
F(a);
F(a);
return a;
", NullString, @"Wreck 9013 in line 6 at position 2: this parameter must pass with the ""ref"" keyword
")]
	[DataRow(@"Class Person
{
	string Name { get, private set };
}
var p = new Person();
p.Name = ""Alice"";
return p;
", NullString, @"Error 4039 in line 6 at position 2: the property ""Person.Name"" cannot be set from here
")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
var c = new Config[100]();
c.Timeout = 200;
return c;
", NullString, @"Error 403A in line 6 at position 2: the property ""Config.Timeout"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
var c = new Config[100]();
return c;
", "new Config(100)", "Ошибок нет")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
var c = new Config[""100""]();
return c;
", NullString, @"Error 4014 in line 5 at position 19: cannot convert from the type ""string"" to the type ""int"" during the construction
")]
	[DataRow(@"Class Secret
{
	string Code { get, private init };
}
var s = new Secret[""AAA""]();
return s;
", NullString, @"Error 403F in line 5 at position 19: redundant property initializer - this class does not have so many open settable properties
")]
	[DataRow(@"Class Item
{
	string Label { get, init, set };
}
", NullString, @"Error 2008 in line 3 at position 25: expected: }
")]
	[DataRow(@"Class Record
{
	required string Title { get, init };
}
var r = new Record();
return r;
", NullString, @"Error 403C in line 5 at position 18: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Outer
{
	Inner Nested { get, init };
}

Class Inner
{
	required int Value { get, init };
}
var o = new Outer[new Inner()]();
return o;
", NullString, @"Error 403C in line 10 at position 27: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Outer
{
	Inner Nested { get, init };
}

Class Inner
{
	required int Value { get, init };
}
var o = new Outer[new Inner[123]()]();
return o;
", "new Outer(new Inner(123))", "Ошибок нет")]
	[DataRow(@"Class Record
{
	required string Title { get, init };
	required string Data { get, init };
}
var r = new Record[""MyRecord""]();
return r;
", NullString, @"Error 403D in line 6 at position 29: the required property ""Data"" must be set during the construction
")]
	[DataRow(@"Class User
{
	required string Name { get, init };
	required int Age { get, init };
	string Email { get, init }; // не required
}
var user = new User[""Alice""]();
return user;
", NullString, @"Error 403D in line 7 at position 27: the required property ""Age"" must be set during the construction
")]
	[DataRow(@"Class User
{
	required string Name { get, init };
	int Age { get, init };
	string Email { get, init }; // не required
}
var user = new User[""Alice"", 25, ""admin@example.com""]();
return user;
", @"new User(""Alice"", 25, ""admin@example.com"")", "Ошибок нет")]
	[DataRow(@"Class Product
{
	string Id { get, init };
	real Price { get, init };
}
var p = new Product[""P123"", 10.5, ""Electronics""]();
return p;
", NullString, @"Error 403F in line 6 at position 34: redundant property initializer - this class does not have so many open settable properties
")]
	[DataRow(@"Class Settings
{
	bool IsActive { get, init };
	int Timeout { get, init };
}
var settings = new Settings[true, 30]();
settings.IsActive = false;
return settings;
", NullString, @"Error 403A in line 7 at position 9: the property ""Settings.IsActive"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Account
{
	string Username { get, private set };
	real Balance { get, init };
	bool IsLocked { get, set }; // публичный set
}
var acc = new Account[""user1"", 100.0]();
return acc;
", NullString, @"Error 4014 in line 7 at position 22: cannot convert from the type ""string"" to the type ""real"" during the construction
")]
	[DataRow(@"Class Company
{
	required string Name { get, init };
	Address Location { get, init };
}

Class Address
{
	required string City { get, init };
	required string Street { get, init };
}

// Код, вызывающий ошибку:
var company = new Company[""Acme"", new Address[""New York""]()]();
return company;
", NullString, @"Error 403D in line 14 at position 56: the required property ""Street"" must be set during the construction
")]
	[DataRow(@"Class Batch
{
	() int Items { get, init };
	string Status { get, init };
}
var batch = new Batch[(1, 2, 3), ""Pending""]();
return batch;
", @"new Batch((1, 2, 3), ""Pending"")", "Ошибок нет")]
	[DataRow(@"Class Person
{
	string Name { get, private set };
}
Person p = new();
p.Name = ""Alice"";
return p;
", NullString, @"Error 4039 in line 6 at position 2: the property ""Person.Name"" cannot be set from here
")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
Config[100] c = new();
c.Timeout = 200;
return c;
", NullString, @"Error 403A in line 6 at position 2: the property ""Config.Timeout"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
Config[100] c = new();
return c;
", "new Config(100)", "Ошибок нет")]
	[DataRow(@"Class Config
{
	int Timeout { get, init };
}
Config[""100""] c = new();
return c;
", NullString, @"Error 4014 in line 5 at position 7: cannot convert from the type ""string"" to the type ""int"" during the construction
")]
	[DataRow(@"Class Secret
{
	string Code { get, private init };
}
Secret[""AAA""] s = new();
return s;
", NullString, @"Error 403F in line 5 at position 7: redundant property initializer - this class does not have so many open settable properties
")]
	[DataRow(@"Class Record
{
	required string Title { get, init };
}
Record r = new();
return r;
", NullString, @"Error 403C in line 5 at position 14: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Outer
{
	Inner Nested { get, init };
}

Class Inner
{
	required int Value { get, init };
}
Outer[new Inner()] o = new();
return o;
", NullString, @"Error 403C in line 10 at position 15: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Outer
{
	Inner Nested { get, init };
}

Class Inner
{
	required int Value { get, init };
}
Outer[new Inner[123]()] o = new();
return o;
", "new Outer(new Inner(123))", "Ошибок нет")]
	[DataRow(@"Class Record
{
	required string Title { get, init };
	required string Data { get, init };
}
Record[""MyRecord""] r = new();
return r;
", NullString, @"Error 403D in line 6 at position 17: the required property ""Data"" must be set during the construction
")]
	[DataRow(@"Class User
{
	required string Name { get, init };
	required int Age { get, init };
	string Email { get, init }; // не required
}
User[""Alice""] user = new();
return user;
", NullString, @"Error 403D in line 7 at position 12: the required property ""Age"" must be set during the construction
")]
	[DataRow(@"Class User
{
	required string Name { get, init };
	int Age { get, init };
	string Email { get, init }; // не required
}
User[""Alice"", 25, ""admin@example.com""] user = new();
return user;
", @"new User(""Alice"", 25, ""admin@example.com"")", "Ошибок нет")]
	[DataRow(@"Class Product
{
	string Id { get, init };
	real Price { get, init };
}
Product[""P123"", 10.5, ""Electronics""] p = new();
return p;
", NullString, @"Error 403F in line 6 at position 22: redundant property initializer - this class does not have so many open settable properties
")]
	[DataRow(@"Class Settings
{
	bool IsActive { get, init };
	int Timeout { get, init };
}
Settings[true, 30] settings = new();
settings.IsActive = false;
return settings;
", NullString, @"Error 403A in line 7 at position 9: the property ""Settings.IsActive"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Account
{
	string Username { get, private set };
	real Balance { get, init };
	bool IsLocked { get, set }; // публичный set
}
Account[""user1"", 100.0] acc = new();
return acc;
", NullString, @"Error 4014 in line 7 at position 8: cannot convert from the type ""string"" to the type ""real"" during the construction
")]
	[DataRow(@"Class Company
{
	required string Name { get, init };
	Address Location { get, init };
}

Class Address
{
	required string City { get, init };
	required string Street { get, init };
}
Company[""Acme"", new Address[""New York""]()] company = new();
return company;
", NullString, @"Error 403D in line 12 at position 38: the required property ""Street"" must be set during the construction
")]
	[DataRow(@"Class Batch
{
	() int Items { get, init };
	string Status { get, init };
}
Batch[(1, 2, 3), ""Pending""] batch = new();
return batch;
", @"new Batch((1, 2, 3), ""Pending"")", "Ошибок нет")]
	[DataRow(@"Class Person
{
	required int Age { get, init };
	required string Name { get, init };

	Constructor(string name)
	{
		Name = name;
	}
}
var p = new Person[30](""Alice"");
return p;
", @"new Person(30, ""Alice"")", "Ошибок нет")]
	[DataRow(@"Class Person
{
	required int Age { get, init };
	required string Name { get, init };

	Constructor(string name)
	{
		Name = name;
	}
}
var p = new Person(""Bob"");
return p;
", NullString, @"Error 403C in line 11 at position 18: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Secret
{
	bool IsActive { get, init };
	string Code { get, private init };

	Constructor(string code)
	{
		Code = code;
		IsActive = true;
	}
}
var s1 = new Secret(""123"");
var s2 = new Secret[true, ""789""](""456"");
var s3 = new Secret[false](""abc"");
return (s1, s2, s3);
", NullString, @"Error 403F in line 13 at position 26: redundant property initializer - this class does not have so many open settable properties
")]
	[DataRow(@"Class Person
{
	Constructor(string name)
	{
		Name = name;
	}

	required int Age { get, init };
	required string Name { get, init };
}
var p = new Person[30](""Alice"");
return p;
", @"new Person(30, ""Alice"")", "Ошибок нет")]
	[DataRow(@"Class Product
{
	required real Price { get, init };
	required string Id { get, init };
	string Category { get, set };

	Constructor()
	{
	}

	Constructor(string id)
	{
		Id = id;
	}
}
var p1 = new Product[10.5](""P123"");
var p2 = new Product(""P456"");
var p3 = new Product[20, ""P789"", ""Books""]();
return (p1, p2, p3);
", NullString, @"Error 403C in line 17 at position 20: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Product
{
	required real Price { get, init };
	required string Id { get, init };
	string Category { get, set };

	Constructor()
	{
	}

	Constructor(string id)
	{
		Id = id;
	}
}
Product[10.5] p1 = new(""P123"");
Product p2 = new(""P456"");
Product[20, ""P789"", ""Books""] p3 = new();
return (p1, p2, p3);
", NullString, @"Error 403C in line 17 at position 16: you must set the required properties - it is done with the square brackets
")]
	[DataRow(@"Class Parent
{
	protected string Secret { get, private init };
}

Class Child : Parent
{
	Constructor()
	{
		Secret = ""child-secret"";
	}
}
", NullString, @"Error 4039 in line 10 at position 2: the property ""Parent.Secret"" cannot be set from here
")]
	[DataRow(@"Class MyClass
{
	protected string Secret { get, private init };

	null Function Set(string value)
	{
		Secret = value;
	}
}
", NullString, @"Error 403A in line 7 at position 2: the property ""MyClass.Secret"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Parent
{
	protected string Secret { get, init };
}

Class Child : Parent
{
	null Function Set(string value)
	{
		Secret = ""child-secret"";
	}
}
", NullString, @"Error 403A in line 10 at position 2: the property ""Parent.Secret"" is declared with ""init"" modifier so it can be set only in the initializer or constructor
")]
	[DataRow(@"Class Parent
{
	string Secret { get } = ""parent-secret"";
}

Class Child : Parent
{
	null Function Set(string value)
	{
		Secret = ""child-secret"";
	}
}
return new Parent().Secret;
", NullString, @"Error 4070 in line 10 at position 2: the property ""Parent.Secret"" is get-only and cannot be set
")]
	[DataRow(@"Class MyClass
{
	string Secret { get } = ""my-secret"";

	Constructor()
	{
	}

	Constructor(string secret)
	{
		Secret = secret;
	}
}
return (new MyClass().Secret, new MyClass(""override-secret"").Secret);
", NullString, @"Error 4070 in line 11 at position 2: the property ""MyClass.Secret"" is get-only and cannot be set
")]
	[DataRow(@"const [typename, (abstract Class)] BaseStack = new(var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const [typename, (abstract Class)] BaseStack = new [typename, (abstract Class)](var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const var BaseStack = new [typename, (abstract Class)](var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = (var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new(var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const var BaseStack = new [typename T, (abstract Class)](var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new [typename T, (abstract Class)](var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"abstract Class BaseStack
{
	required typename T { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
}

const [typename T, (Class : BaseStack[T])] Stack = new(
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new(
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

{
	const [typename T, (Class : BaseStack[T])] Stack = new(
	{
		private () T list = new(32);
	
		T Function Peek()
		{
			return list[^1];
		}
	
		IO T Function Pop
		{
			return list.GetAndRemove(list.Length - 1);
		}
	
		null Function Push(T item)
		{
			list.Add(item);
		}
	});
	
	BaseStack[int] intStack = new Stack[int]();
	intStack.Push(5);
	intStack.Push(10);
	var x = (intStack.Pop(), intStack.Peek());
	BaseStack[string] stringStack = new Stack[string]();
	stringStack.Push(""A"");
	stringStack.Push(""B"");
	var y = (stringStack.Pop(), stringStack.Peek());
	return (x, y);
}
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new(
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const [typename T2, (Class : BaseStack[T2])] Stack = new(
{
	private () T2 list = new(32);

	T2 Function Peek()
	{
		return list[^1];
	}

	IO T2 Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T2 item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"abstract Class BaseStack
{
	required typename T { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
}

const var Stack = new [typename T, (Class : BaseStack[T])](
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const var BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

{
	const var Stack = new [typename T, (Class : BaseStack[T])](
	{
		private () T list = new(32);
	
		T Function Peek()
		{
			return list[^1];
		}
	
		IO T Function Pop
		{
			return list.GetAndRemove(list.Length - 1);
		}
	
		null Function Push(T item)
		{
			list.Add(item);
		}
	});
	
	BaseStack[int] intStack = new Stack[int]();
	intStack.Push(5);
	intStack.Push(10);
	var x = (intStack.Pop(), intStack.Peek());
	BaseStack[string] stringStack = new Stack[string]();
	stringStack.Push(""A"");
	stringStack.Push(""B"");
	var y = (stringStack.Pop(), stringStack.Peek());
	return (x, y);
}
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const var BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const var Stack = new [typename T2, (Class : BaseStack[T2])](
{
	private () T2 list = new(32);

	T2 Function Peek()
	{
		return list[^1];
	}

	IO T2 Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T2 item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"abstract Class BaseStack
{
	required typename T { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
}

const [typename T, (Class : BaseStack[T])] Stack = new [typename T, (Class : BaseStack[T])](
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

{
	const [typename T, (Class : BaseStack[T])] Stack = new [typename T, (Class : BaseStack[T])](
	{
		private () T list = new(32);
	
		T Function Peek()
		{
			return list[^1];
		}
	
		IO T Function Pop
		{
			return list.GetAndRemove(list.Length - 1);
		}
	
		null Function Push(T item)
		{
			list.Add(item);
		}
	});
	
	BaseStack[int] intStack = new Stack[int]();
	intStack.Push(5);
	intStack.Push(10);
	var x = (intStack.Pop(), intStack.Peek());
	BaseStack[string] stringStack = new Stack[string]();
	stringStack.Push(""A"");
	stringStack.Push(""B"");
	var y = (stringStack.Pop(), stringStack.Peek());
	return (x, y);
}
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const [typename T2, (Class : BaseStack[T2])] Stack = new [typename T2, (Class : BaseStack[T2])](
{
	private () T2 list = new(32);

	T2 Function Peek()
	{
		return list[^1];
	}

	IO T2 Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T2 item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new(
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const [typename T, (Class : BaseStack[T])] Stack = new(
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const var BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const var Stack = new [typename T, (Class : BaseStack[T])](
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [typename T, (abstract Class)] BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const [typename T, (Class : BaseStack[T])] Stack = new [typename T, (Class : BaseStack[T])](
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const [(typename T1, typename T2), (Class)] Pair = new(
{
	T1 First { get, set };
	T2 Second { get, set };

	Constructor(T1 first, T2 second)
	{
		First = first;
		Second = second;
	}
});

var pair = new Pair[int, string](100, ""Status"");
return (pair, pair.First, pair.Second);
", @"(new Pair(100, ""Status""), 100, ""Status"")", "Ошибок нет")]
	[DataRow(@"const var Pair = new [(typename T1, typename T2), (Class)](
{
	T1 First { get, set };
	T2 Second { get, set };

	Constructor(T1 first, T2 second)
	{
		First = first;
		Second = second;
	}
});

var pair = new Pair[int, string](100, ""Status"");
return (pair, pair.First, pair.Second);
", @"(new Pair(100, ""Status""), 100, ""Status"")", "Ошибок нет")]
	[DataRow(@"const [(typename T1, typename T2), (Class)] Pair = new [(typename T1, typename T2), (Class)](
{
	T1 First { get, set };
	T2 Second { get, set };

	Constructor(T1 first, T2 second)
	{
		First = first;
		Second = second;
	}
});

var pair = new Pair[int, string](100, ""Status"");
return (pair, pair.First, pair.Second);
", @"(new Pair(100, ""Status""), 100, ""Status"")", "Ошибок нет")]
	[DataRow(@"using System;
const [(typename T1, typename T2), (Class)] Pair = new(
{
	T1 First { get, set };
	T2 Second { get, set };
	() (T1, T2) List { get, set };

	Constructor(T1 first, T2 second, () (T1, T2) list)
	{
		First = first;
		Second = second;
		List = list;
	}
});

var pair = new Pair[int, string](100, ""Status"", ((5, ""A""), (10, ""B""), (15, ""C"")));
var x = pair.First + "": "" + pair.Second + "" - ""
x += string.Join("", "", RedStarLinq.Convert(pair.List, x => x[1] + "", "" + x[2]))
var pair2 = new Pair[string, string](""Name"", ""Status"", ((""A"", ""X""), (""B"", ""Y""), (""C"", ""Z"")));
var y = pair2.First + "": "" + pair2.Second + "" - ""
y += string.Join("", "", RedStarLinq.Convert(pair2.List, x => x[1] + "", "" + x[2]))
var pair3 = new Pair[int, int](100, 255, ((5, 8), (10, 16), (15, 24)));
var z = pair3.First + "": "" + pair3.Second + "" - ""
z += string.Join("", "", RedStarLinq.Convert(pair3.List, x => x[1] + "", "" + x[2]))
return (x, y, z);
", @"(""100: Status - 5, A, 10, B, 15, C"", ""Name: Status - A, X, B, Y, C, Z"", ""100: 255 - 5, 8, 10, 16, 15, 24"")", "Ошибок нет")]
	[DataRow(@"using System;
const var Pair = new [(typename T1, typename T2), (Class)](
{
	T1 First { get, set };
	T2 Second { get, set };
	() (T1, T2) List { get, set };

	Constructor(T1 first, T2 second, () (T1, T2) list)
	{
		First = first;
		Second = second;
		List = list;
	}
});

var pair = new Pair[int, string](100, ""Status"", ((5, ""A""), (10, ""B""), (15, ""C"")));
var x = pair.First + "": "" + pair.Second + "" - ""
x += string.Join("", "", RedStarLinq.Convert(pair.List, x => x[1] + "", "" + x[2]))
var pair2 = new Pair[string, string](""Name"", ""Status"", ((""A"", ""X""), (""B"", ""Y""), (""C"", ""Z"")));
var y = pair2.First + "": "" + pair2.Second + "" - ""
y += string.Join("", "", RedStarLinq.Convert(pair2.List, x => x[1] + "", "" + x[2]))
var pair3 = new Pair[int, int](100, 255, ((5, 8), (10, 16), (15, 24)));
var z = pair3.First + "": "" + pair3.Second + "" - ""
z += string.Join("", "", RedStarLinq.Convert(pair3.List, x => x[1] + "", "" + x[2]))
return (x, y, z);
", @"(""100: Status - 5, A, 10, B, 15, C"", ""Name: Status - A, X, B, Y, C, Z"", ""100: 255 - 5, 8, 10, 16, 15, 24"")", "Ошибок нет")]
	[DataRow(@"using System;
const [(typename T1, typename T2), (Class)] Pair = new [(typename T1, typename T2), (Class)](
{
	T1 First { get, set };
	T2 Second { get, set };
	() (T1, T2) List { get, set };

	Constructor(T1 first, T2 second, () (T1, T2) list)
	{
		First = first;
		Second = second;
		List = list;
	}
});

var pair = new Pair[int, string](100, ""Status"", ((5, ""A""), (10, ""B""), (15, ""C"")));
var x = pair.First + "": "" + pair.Second + "" - ""
x += string.Join("", "", RedStarLinq.Convert(pair.List, x => x[1] + "", "" + x[2]))
var pair2 = new Pair[string, string](""Name"", ""Status"", ((""A"", ""X""), (""B"", ""Y""), (""C"", ""Z"")));
var y = pair2.First + "": "" + pair2.Second + "" - ""
y += string.Join("", "", RedStarLinq.Convert(pair2.List, x => x[1] + "", "" + x[2]))
var pair3 = new Pair[int, int](100, 255, ((5, 8), (10, 16), (15, 24)));
var z = pair3.First + "": "" + pair3.Second + "" - ""
z += string.Join("", "", RedStarLinq.Convert(pair3.List, x => x[1] + "", "" + x[2]))
return (x, y, z);
", @"(""100: Status - 5, A, 10, B, 15, C"", ""Name: Status - A, X, B, Y, C, Z"", ""100: 255 - 5, 8, 10, 16, 15, 24"")", "Ошибок нет")]
	[DataRow(@"const BaseStack = new [typename T, (abstract Class)](var T:
{
	required typename T2 { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});
", NullString, "Ошибок нет")]
	[DataRow(@"const Dic = new [typename T, int](5);
return Dic[real];
", "5", "Ошибок нет")]
	[DataRow(@"abstract Class BaseStack
{
	required typename T { get, init };

	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
}

const Stack = new [typename T, (Class : BaseStack[T])](
{
	private () T list = new(32);

	T Function Peek()
	{
		return list[^1];
	}

	IO T Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

{
	const Stack = new [typename T, (Class : BaseStack[T])](
	{
		private () T list = new(32);
	
		T Function Peek()
		{
			return list[^1];
		}
	
		IO T Function Pop
		{
			return list.GetAndRemove(list.Length - 1);
		}
	
		null Function Push(T item)
		{
			list.Add(item);
		}
	});
	
	BaseStack[int] intStack = new Stack[int]();
	intStack.Push(5);
	intStack.Push(10);
	var x = (intStack.Pop(), intStack.Peek());
	BaseStack[string] stringStack = new Stack[string]();
	stringStack.Push(""A"");
	stringStack.Push(""B"");
	var y = (stringStack.Pop(), stringStack.Peek());
	return (x, y);
}
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"const BaseStack = new [typename T, (abstract Class)](
{
	abstract T Function Peek();
	abstract T Function Pop();
	abstract null Function Push(T item);
});

const Stack = new [typename T2, (Class : BaseStack[T2])](
{
	private () T2 list = new(32);

	T2 Function Peek()
	{
		return list[^1];
	}

	IO T2 Function Pop
	{
		return list.GetAndRemove(list.Length - 1);
	}

	null Function Push(T2 item)
	{
		list.Add(item);
	}
});

BaseStack[int] intStack = new Stack[int]();
intStack.Push(5);
intStack.Push(10);
var x = (intStack.Pop(), intStack.Peek());
BaseStack[string] stringStack = new Stack[string]();
stringStack.Push(""A"");
stringStack.Push(""B"");
var y = (stringStack.Pop(), stringStack.Peek());
return (x, y);
", @"((10, 5), (""B"", ""A""))", "Ошибок нет")]
	[DataRow(@"return ((56).ToChar(), 56u.ToChar(), 56L.ToChar(), 56uL.ToChar(), 56LL.ToChar())
", "('8', '8', '8', '8', '8')", "Ошибок нет")]
	[DataRow(@"bool[1000000] tuple = null
tuple[123456] = true
return (tuple[123455], tuple[123456], tuple[123457])
", "(false, true, false)", "Ошибок нет")]
	[DataRow(@"const typename T = int
T Function Func(T a, T b, T c, T d, T e, T f, T g, T h, T i, T j, T k, T l, T m, T n, T o, T p, T q, T r, T s, T t, T u, T v,"
+ @" T w, T x, T y, T z, T A, T B, T C, T D, T E, T F, T G, T H, T I, T J, T K, T L, T M, T N, T O, T P, T Q, T R, T S, T U,"
+ @" T V, T W, T X, T Y, T Z, T aa, T ab, T ac, T ad, T ae, T af, T ag, T ah, T ai, T aj, T ak, T al, T am, T an, T ao, T ap, T aq, T ar, T as)
{
	return a
}
", NullString, @"Warning 800F in line 2 at position 0: too long line (128 characters are supported, actually 384)
Wreck 9022 in line 2 at position 99: the function, constructor or extent cannot have more than 16 parameters
")]
	[DataRow(@"() byte bytes = (" + Five256 + @"5, 5, 5, 5)
return bytes;
", NullString, @"Warning 800F in line 1 at position 0: too long line (128 characters are supported, actually 796)
Error 401A in line 1 at position 785: the tuple literal cannot contain more than 256 items - use the external storage (such as the file) for the larger tuples (all the items have been set to null)
")]
	[DataRow(@"const typename T = byte;
(" + T255 + @"T) bytes = null;
return bytes[^1];
", "0", "Ошибок нет")]
	[DataRow(@"const typename T = byte;
(" + T255 + @"T, T) bytes = null;
return bytes[^1];
", NullString, @"Error 2041 in line 2 at position 772: the non-singular tuple cannot have more than 256 items; group them into several nested tuples or convert to one type and use singular tuple
")]
	[DataRow(@"bool[3] bools = (false, true, false)
return (bools[1], bools[2], bools[3])
", "(false, true, false)", "Ошибок нет")]
	[DataRow(@"bool[3] bools = (false, true, false)
return bools
", "(false, true, false)", "Ошибок нет")]
	[DataRow(@"bool[3] bools = (false, false, false)
bools[1] = true
bools[3] = true
return (bools[1], bools[2], bools[^1])
", "(true, false, true)", "Ошибок нет")]
	[DataRow(@"bool[3] bools = (false, false, false)
var len = bools.Length
for (var x in bools) { }
", NullString, @"Error 4033 in line 2 at position 16: the type ""tuple"" does not contain member ""Length""
")]
	[DataRow(@"const N = 4
bool[N] b = (true, false, true, false)
return b
", "(true, false, true, false)", "Ошибок нет")]
	[DataRow(@"bool[5] Function Invert(bool[5] values)
{
	for (i in..5)
		values[i]!!
	return values
}
return Invert((false, true, true, false, false))
", "(true, false, false, true, true)", "Ошибок нет")]
	[DataRow(@"var bools = (false, true, false)
() bool list = new()
for (var x in bools)
{
	list.Add(x)
}
return list
", "(false, true, false)", "Ошибок нет")]
	[DataRow(@"var n = 4
bool[n] bools = (true, false, true, false)
", NullString, @"Error 4050 in line 2 at position 5: this expression must be constant but it isn't
")]
	[DataRow(@"bool[2] Function Func(bool[2] bools)
{
	return bools
}
var a = true
return Func((a > 0) ? (true, false) : (false, true))
", "(true, false)", "Ошибок нет")]
	[DataRow(@"() bool[3] list = ((true, false, true), (false, false, true))
return list.ConvertAndJoin[bool[3], bool](x => x.ToList())
", "(true, false, true, false, false, true)", "Ошибок нет")]
	[DataRow(@"bool[100000000] bools = null
bools[99999999] = true
return bools[^2]
", "true", "Ошибок нет")]
	[DataRow(@"bool[1500_000_000] bools
int[100_000_000] ints
decimal[10_000_000] decimals
(bool[1000_000])[1000_000] bools2
(byte, byte)[50_000_000] bytes
(bool, bool)[50_000_000] items
([string, int])[10_000_000] dics
([string, int])[25_000_000] dics2
(System.Func[string, int])[5_000_000] funcs
(int[100_000], bool)[1000_000] items2
(int[1000], System.Collections.Chain)[1000_000] items3
System.Collections.Chain[10_000_000] chains
System.Collections.Chain[100_000_000] chains2
Struct MyStruct
{
	int[10] Items
}
(MyStruct[])[10] structs
", NullString, @"Error 2042 in line 1 at position 5: the singular tuple of type bool cannot have more than 1000000000 items; use the list for more ones
Error 2042 in line 2 at position 4: the singular tuple of type int cannot have more than 32000000 items; use the list for more ones
Error 2042 in line 3 at position 8: the singular tuple of type decimal cannot have more than 8000000 items; use the list for more ones
Error 2042 in line 4 at position 17: the singular tuple of type bool[1000000] cannot have more than 128 items; use the list for more ones
Error 2042 in line 6 at position 13: the singular tuple of type bool[2] cannot have more than 32000000 items; use the list for more ones
Error 2042 in line 8 at position 16: the singular tuple of type System.Collections.Dictionary[string, int] cannot have more than 16000000 items; use the list for more ones
Error 2042 in line 10 at position 21: the singular tuple of type (int[100000], bool) cannot have more than 319 items; use the list for more ones
Error 2042 in line 11 at position 38: the singular tuple of type (int[1000], System.Collections.Chain) cannot have more than 31936 items; use the list for more ones
Error 2042 in line 13 at position 25: the singular tuple of type System.Collections.Chain cannot have more than 16000000 items; use the list for more ones
Error 2043 in line 18 at position 13: the singular tuple of type MyStruct[(List :: list() typename)] is under development; use the list for that type
")]
	[DataRow(@"var a = 1234567890n
var b = typeof(a)
var c = 1uLL
var d = typeof(c)
return (a, b, c, d)
", "(1234567890, int, 1, unsigned long long)", "Ошибок нет")]
	[DataRow(@"var number = 12_345_678_901_234_567_890
var y = typeof(number % 256)
var y2 = typeof(number / 100_000_000_000_000_000)
var si = typeof(-(-number) % 32_768)
var usi = typeof(number % 65_536)
var usi2 = typeof(number / 300_000_000_000_000)
var i = typeof(-(-number) % 2_147_483_648)
var ui = typeof(number % 4_294_967_296)
var ui2 = typeof(number / 4_294_967_296)
var li = typeof(-(-number) % 9_223_372_036_854_775_808)
return (y, y2, si, usi, usi2, i, ui, ui2, li)
", "(byte, byte, short int, unsigned short int, unsigned short int, int, unsigned int, unsigned int, long int)", "Ошибок нет")]
	[DataRow(@"var a = 2345678901n
var b = 12345678901u
var c = 12345678901234567890L
var d = 23456789012345678901uL
", NullString, @"Error 0018 in line 1 at position 8: to large or too small literal for this type suffix; there is allowed from -2147483648 to 2147483647
Error 0018 in line 2 at position 8: to large or too small literal for this type suffix; there is allowed from 0 to 4294967295
Error 0018 in line 3 at position 8: to large or too small literal for this type suffix; there is allowed from -9223372036854775808 to 9223372036854775807
Error 0018 in line 4 at position 8: to large or too small literal for this type suffix; there is allowed from 0 to 18446744073709551615
")]
	[DataRow(@"var si = -12345
var i = -1234567890
var li = -123456789012345
var ll = -1234567890123456789012345
var items = (si.ToUnsigned(), i.ToUnsigned(), li.ToUnsigned(), ll.ToUnsigned())
var types = (typeof(si.ToUnsigned()), typeof(i.ToUnsigned()), typeof(li.ToUnsigned()), typeof(ll.ToUnsigned()))
return (items, types)
", "((12345, 1234567890, 123456789012345, 1234567890123456789012345),"
		+ " (unsigned short int, unsigned int, unsigned long int, unsigned long long))", "Ошибок нет")]
	[DataRow(@"return (Infty, -Infty, Uncty, Pi, E, real.Infty, -real.Infty, real.Uncty, real.Pi, real.E)
", "(Infty, -Infty, Uncty, 3.141592653589793, 2.718281828459045, Infty, -Infty, Uncty, 3.141592653589793, 2.718281828459045)", "Ошибок нет")]
	[DataRow(@"var r = Infty
var b = r == real.Infty // true
var r2 = Uncty
var b2 = real.IsUncertainty(r2) // true
return (b, b2)
", "(true, true)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = typeof(3 + 2)
Types.Add(typeof(10003 + 2))
Types.Add(typeof(3 + 10002))
Types.Add(typeof(10003 + 10002))
Types.Add(typeof(40003 + 2))
Types.Add(typeof(3 + 40002))
Types.Add(typeof(40003 + 10002))
Types.Add(typeof(10003 + 40002))
Types.Add(typeof(40003 + 40002))
Types.Add(typeof(1000000003 + 2))
Types.Add(typeof(3 + 1000000002))
Types.Add(typeof(1000000003 + 10002))
Types.Add(typeof(10003 + 1000000002))
Types.Add(typeof(1000000003 + 40002))
Types.Add(typeof(40003 + 1000000002))
Types.Add(typeof(1000000003 + 1000000002))
Types.Add(typeof(3000000003 + 2))
Types.Add(typeof(3 + 3000000002))
Types.Add(typeof(3000000003 + 10002))
Types.Add(typeof(10003 + 3000000002))
Types.Add(typeof(3000000003 + 40002))
Types.Add(typeof(40003 + 3000000002))
Types.Add(typeof(3000000003 + 1000000002))
Types.Add(typeof(1000000003 + 3000000002))
Types.Add(typeof(3000000003 + 3000000002))
Types.Add(typeof(5000000000000000003 + 2))
Types.Add(typeof(3 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 10002))
Types.Add(typeof(10003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 40002))
Types.Add(typeof(40003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000002))
Types.Add(typeof(1000000003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 3000000002))
Types.Add(typeof(3000000003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 5000000000000000002))
Types.Add(typeof(10000000000000000003 + 2))
Types.Add(typeof(3 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 10002))
Types.Add(typeof(10003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 40002))
Types.Add(typeof(40003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000002))
Types.Add(typeof(1000000003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 3000000002))
Types.Add(typeof(3000000003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 10000000000000000002))
Types.Add(typeof(1000000000000000000003 + 2))
Types.Add(typeof(3 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 10002))
Types.Add(typeof(10003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 40002))
Types.Add(typeof(40003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003LL + 2))
Types.Add(typeof(3 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 10002))
Types.Add(typeof(10003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 40002))
Types.Add(typeof(40003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002LL))
return Types
", "(byte, short int, short int, short int, unsigned short int, unsigned short int, unsigned short int, unsigned short int,"
		+ " int, int, int, int, int, int, int, int, unsigned int,"
		+ " unsigned int, unsigned int, unsigned int, unsigned int, unsigned int, unsigned int, unsigned int, int,"
		+ " long int, long int, long int, long int, long int, long int, long int, long int, long int, long int, long int,"
		+ " unsigned long int, unsigned long int, long long, long long, unsigned long int, unsigned long int,"
		+ " long long, long long, unsigned long int, unsigned long int, long long, long long, long int,"
		+ " unsigned long long, unsigned long long, long long, long long, unsigned long long, unsigned long long,"
		+ " long long, long long, unsigned long long, unsigned long long, long long, long long,"
		+ " unsigned long long, unsigned long long, unsigned long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long, long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = typeof(3 - 2)
Types.Add(typeof(10003 - 2))
Types.Add(typeof(3 - 10002))
Types.Add(typeof(10003 - 10002))
Types.Add(typeof(40003 - 2))
Types.Add(typeof(3 - 40002))
Types.Add(typeof(40003 - 10002))
Types.Add(typeof(10003 - 40002))
Types.Add(typeof(40003 - 40002))
Types.Add(typeof(1000000003 - 2))
Types.Add(typeof(3 - 1000000002))
Types.Add(typeof(1000000003 - 10002))
Types.Add(typeof(10003 - 1000000002))
Types.Add(typeof(1000000003 - 40002))
Types.Add(typeof(40003 - 1000000002))
Types.Add(typeof(1000000003 - 1000000002))
Types.Add(typeof(3000000003 - 2))
Types.Add(typeof(3 - 3000000002))
Types.Add(typeof(3000000003 - 10002))
Types.Add(typeof(10003 - 3000000002))
Types.Add(typeof(3000000003 - 40002))
Types.Add(typeof(40003 - 3000000002))
Types.Add(typeof(3000000003 - 1000000002))
Types.Add(typeof(1000000003 - 3000000002))
Types.Add(typeof(3000000003 - 3000000002))
Types.Add(typeof(5000000000000000003 - 2))
Types.Add(typeof(3 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 10002))
Types.Add(typeof(10003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 40002))
Types.Add(typeof(40003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 1000000002))
Types.Add(typeof(1000000003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 3000000002))
Types.Add(typeof(3000000003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 5000000000000000002))
Types.Add(typeof(10000000000000000003 - 2))
Types.Add(typeof(3 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 10002))
Types.Add(typeof(10003 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 40002))
Types.Add(typeof(40003 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 1000000002))
Types.Add(typeof(1000000003 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 3000000002))
Types.Add(typeof(3000000003 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 10000000000000000002))
Types.Add(typeof(1000000000000000000003 - 2))
Types.Add(typeof(1000000000000000000003 - 10002))
Types.Add(typeof(10003 - 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 - 40002))
Types.Add(typeof(1000000000000000000003 - 1000000002))
Types.Add(typeof(1000000003 - 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 - 3000000002))
Types.Add(typeof(1000000000000000000003 - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 - 10000000000000000002))
Types.Add(typeof(1000000000000000000003 - 1000000000000000000002))
Types.Add(typeof(1000000000000000000003LL - 2))
Types.Add(typeof(3 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 10002))
Types.Add(typeof(10003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 40002))
Types.Add(typeof(40003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 1000000002))
Types.Add(typeof(1000000003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 3000000002))
Types.Add(typeof(3000000003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 5000000000000000002))
Types.Add(typeof(5000000000000000003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 10000000000000000002))
Types.Add(typeof(10000000000000000003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 - 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL - 1000000000000000000002LL))
return Types
", "(byte, short int, short int, byte, unsigned short int, int, short int, short int, byte,"
		+ " int, int, int, int, int, int, byte, unsigned int, int, unsigned int, long int, unsigned int, int, int, int, byte,"
		+ " long int, long int, long int, long int, long int, long int, long int, long int, long int, long int, byte,"
		+ " unsigned long int, long int, long long, long long, unsigned long int, long int, long long, long long,"
		+ " unsigned long int, long int, long long, long long, byte, unsigned long long, long long, long long,"
		+ " unsigned long long, long long, long long, unsigned long long, long long, long long,"
		+ " unsigned long long, unsigned long long, long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long, long long,"
		+ " long long, long long, long long, long long, long long, long long, long long)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = typeof(3 * 2)
Types.Add(typeof(10003 * 2))
Types.Add(typeof(3 * 10002))
Types.Add(typeof(10003 * 10002))
Types.Add(typeof(40003 * 2))
Types.Add(typeof(3 * 40002))
Types.Add(typeof(40003 * 10002))
Types.Add(typeof(10003 * 40002))
Types.Add(typeof(40003 * 40002))
Types.Add(typeof(1000000003 * 2))
Types.Add(typeof(3 * 1000000002))
Types.Add(typeof(1000000003 * 10002))
Types.Add(typeof(10003 * 1000000002))
Types.Add(typeof(1000000003 * 40002))
Types.Add(typeof(40003 * 1000000002))
Types.Add(typeof(1000000003 * 1000000002))
Types.Add(typeof(3000000003 * 2))
Types.Add(typeof(3 * 3000000002))
Types.Add(typeof(3000000003 * 10002))
Types.Add(typeof(10003 * 3000000002))
Types.Add(typeof(3000000003 * 40002))
Types.Add(typeof(40003 * 3000000002))
Types.Add(typeof(3000000003 * 1000000002))
Types.Add(typeof(1000000003 * 3000000002))
Types.Add(typeof(3000000003 * 3000000002))
Types.Add(typeof(5000000000000000003 * 2))
Types.Add(typeof(3 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 10002))
Types.Add(typeof(10003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 40002))
Types.Add(typeof(40003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 1000000002))
Types.Add(typeof(1000000003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 3000000002))
Types.Add(typeof(3000000003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 5000000000000000002))
Types.Add(typeof(10000000000000000003 * 2))
Types.Add(typeof(3 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 10002))
Types.Add(typeof(10003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 40002))
Types.Add(typeof(40003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 1000000002))
Types.Add(typeof(1000000003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 3000000002))
Types.Add(typeof(3000000003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 10000000000000000002))
Types.Add(typeof(1000000000000000000003 * 2))
Types.Add(typeof(3 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 10002))
Types.Add(typeof(10003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 40002))
Types.Add(typeof(40003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 1000000002))
Types.Add(typeof(1000000003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 3000000002))
Types.Add(typeof(3000000003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003LL * 2))
Types.Add(typeof(3 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 10002))
Types.Add(typeof(10003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 40002))
Types.Add(typeof(40003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 1000000002))
Types.Add(typeof(1000000003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 3000000002))
Types.Add(typeof(3000000003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 5000000000000000002))
Types.Add(typeof(5000000000000000003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 10000000000000000002))
Types.Add(typeof(10000000000000000003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 * 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL * 1000000000000000000002LL))
return Types
", "(byte, short int, short int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, long int, long int,"
		+ " int, unsigned int, long int, long int, int, long int, long int, long int, long int, long int, long int, long int,"
		+ " long int, long int, long int, long int, long int, unsigned long int, long long, long long, long int,"
		+ " unsigned long int, long long, long long, unsigned long int, long int, long long, long long,"
		+ " unsigned long int, unsigned long long, unsigned long long, long long, long long,"
		+ " unsigned long long, unsigned long long, long long, long long, unsigned long long, unsigned long long,"
		+ " long long, long long, unsigned long long, unsigned long long, unsigned long long, long long, long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long, long long,"
		+ " long long, long long, long long, long long, long long, long long)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = typeof(3 / 2)
Types.Add(typeof(10003 / 2))
Types.Add(typeof(3 / 10002))
Types.Add(typeof(10003 / 10002))
Types.Add(typeof(40003 / 2))
Types.Add(typeof(3 / 40002))
Types.Add(typeof(40003 / 10002))
Types.Add(typeof(10003 / 40002))
Types.Add(typeof(40003 / 40002))
Types.Add(typeof(1000000003 / 2))
Types.Add(typeof(3 / 1000000002))
Types.Add(typeof(1000000003 / 10002))
Types.Add(typeof(10003 / 1000000002))
Types.Add(typeof(1000000003 / 40002))
Types.Add(typeof(40003 / 1000000002))
Types.Add(typeof(1000000003 / 1000000002))
Types.Add(typeof(3000000003 / 2))
Types.Add(typeof(3 / 3000000002))
Types.Add(typeof(3000000003 / 10002))
Types.Add(typeof(10003 / 3000000002))
Types.Add(typeof(3000000003 / 40002))
Types.Add(typeof(40003 / 3000000002))
Types.Add(typeof(3000000003 / 1000000002))
Types.Add(typeof(1000000003 / 3000000002))
Types.Add(typeof(3000000003 / 3000000002))
Types.Add(typeof(5000000000000000003 / 2))
Types.Add(typeof(3 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 10002))
Types.Add(typeof(10003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 40002))
Types.Add(typeof(40003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 1000000002))
Types.Add(typeof(1000000003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 3000000002))
Types.Add(typeof(3000000003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 5000000000000000002))
Types.Add(typeof(10000000000000000003 / 2))
Types.Add(typeof(3 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 10002))
Types.Add(typeof(10003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 40002))
Types.Add(typeof(40003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 1000000002))
Types.Add(typeof(1000000003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 3000000002))
Types.Add(typeof(3000000003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 10000000000000000002))
Types.Add(typeof(1000000000000000000003 / 2))
Types.Add(typeof(3 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 10002))
Types.Add(typeof(10003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 40002))
Types.Add(typeof(40003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 1000000002))
Types.Add(typeof(1000000003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 3000000002))
Types.Add(typeof(3000000003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003LL / 2))
Types.Add(typeof(3 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 10002))
Types.Add(typeof(10003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 40002))
Types.Add(typeof(40003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 1000000002))
Types.Add(typeof(1000000003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 3000000002))
Types.Add(typeof(3000000003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 5000000000000000002))
Types.Add(typeof(5000000000000000003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 10000000000000000002))
Types.Add(typeof(10000000000000000003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 / 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL / 1000000000000000000002LL))
return Types
", "(byte, short int, byte, byte, short int, byte, byte, byte, byte, int, byte, int, byte, short int, byte, byte, int, byte,"
		+ " int, byte, int, byte, byte, byte, byte, long int, byte, long int, byte, long int, byte, long int,"
		+ " byte, int, byte, byte, long int, byte, long long, long long, long int, byte, long long, long long,"
		+ " unsigned int, byte, long long, long long, byte, unsigned long long, byte, long long, byte,"
		+ " unsigned long long, byte, long long, short int, unsigned long long, byte, long long, byte,"
		+ " unsigned long long, unsigned long long, unsigned long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long, long long,"
		+ " long long, long long, long long, long long, long long, long long, long long, long long)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = typeof(3 % 2)
Types.Add(typeof(10003 % 2))
Types.Add(typeof(3 % 10002))
Types.Add(typeof(10003 % 10002))
Types.Add(typeof(40003 % 2))
Types.Add(typeof(3 % 40002))
Types.Add(typeof(40003 % 10002))
Types.Add(typeof(10003 % 40002))
Types.Add(typeof(40003 % 40002))
Types.Add(typeof(1000000003 % 2))
Types.Add(typeof(3 % 1000000002))
Types.Add(typeof(1000000003 % 10002))
Types.Add(typeof(10003 % 1000000002))
Types.Add(typeof(1000000003 % 40002))
Types.Add(typeof(40003 % 1000000002))
Types.Add(typeof(1000000003 % 1000000002))
Types.Add(typeof(3000000003 % 2))
Types.Add(typeof(3 % 3000000002))
Types.Add(typeof(3000000003 % 10002))
Types.Add(typeof(10003 % 3000000002))
Types.Add(typeof(3000000003 % 40002))
Types.Add(typeof(40003 % 3000000002))
Types.Add(typeof(3000000003 % 1000000002))
Types.Add(typeof(1000000003 % 3000000002))
Types.Add(typeof(3000000003 % 3000000002))
Types.Add(typeof(5000000000000000003 % 2))
Types.Add(typeof(3 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 10002))
Types.Add(typeof(10003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 40002))
Types.Add(typeof(40003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 1000000002))
Types.Add(typeof(1000000003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 3000000002))
Types.Add(typeof(3000000003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 5000000000000000002))
Types.Add(typeof(10000000000000000003 % 2))
Types.Add(typeof(3 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 10002))
Types.Add(typeof(10003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 40002))
Types.Add(typeof(40003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 1000000002))
Types.Add(typeof(1000000003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 3000000002))
Types.Add(typeof(3000000003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 10000000000000000002))
Types.Add(typeof(1000000000000000000003 % 2))
Types.Add(typeof(3 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 10002))
Types.Add(typeof(10003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 40002))
Types.Add(typeof(40003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 1000000002))
Types.Add(typeof(1000000003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 3000000002))
Types.Add(typeof(3000000003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003LL % 2))
Types.Add(typeof(3 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 10002))
Types.Add(typeof(10003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 40002))
Types.Add(typeof(40003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 1000000002))
Types.Add(typeof(1000000003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 3000000002))
Types.Add(typeof(3000000003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 5000000000000000002))
Types.Add(typeof(5000000000000000003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 10000000000000000002))
Types.Add(typeof(10000000000000000003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 % 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL % 1000000000000000000002LL))
return Types
", "(byte, byte, byte, byte, byte, byte, short int, short int, byte, byte, byte, byte, short int, short int,"
		+ " unsigned short int, byte, byte, byte, byte, short int, short int, unsigned short int, int, int, byte, byte, byte,"
		+ " short int, short int, short int, unsigned short int, byte, int, int, unsigned int, byte, byte, byte,"
		+ " short int, short int, short int, unsigned short int, byte, int, int, unsigned int, long long, long int,"
		+ " byte, byte, byte, short int, short int, short int, unsigned short int, short int, int, int, unsigned int,"
		+ " long int, long int, unsigned long long, unsigned long long, unsigned long long,"
		+ " byte, long long, short int, long long, short int, long long, short int, long long, int,"
		+ " long long, long long, long long, unsigned long int, long long, long long, long long, long long)", "Ошибок нет")]
	[DataRow(@"(() typename) Types = new()
Types.Add(typeof(1000000000000000000003uLr + 2))
Types.Add(typeof(3 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 10002))
Types.Add(typeof(10003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 40002))
Types.Add(typeof(40003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLm + 2))
Types.Add(typeof(3 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 10002))
Types.Add(typeof(10003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 40002))
Types.Add(typeof(40003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003.5 + 2))
Types.Add(typeof(3 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 10002))
Types.Add(typeof(10003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 40002))
Types.Add(typeof(40003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5m + 2))
Types.Add(typeof(3 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 10002))
Types.Add(typeof(10003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 40002))
Types.Add(typeof(40003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5Lr + 2))
Types.Add(typeof(3 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 10002))
Types.Add(typeof(10003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 40002))
Types.Add(typeof(40003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lm + 2))
Types.Add(typeof(3 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 10002))
Types.Add(typeof(10003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 40002))
Types.Add(typeof(40003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000002))
Types.Add(typeof(1000000003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 3000000002))
Types.Add(typeof(3000000003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 5000000000000000002))
Types.Add(typeof(5000000000000000003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 10000000000000000002))
Types.Add(typeof(10000000000000000003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002))
Types.Add(typeof(1000000000000000000003 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002LL))
Types.Add(typeof(1000000000000000000003LL + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002uLr))
Types.Add(typeof(1000000000000000000003uLr + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002uLm))
Types.Add(typeof(1000000000000000000003uLm + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002.5))
Types.Add(typeof(1000000000000000000003.5 + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002.5m))
Types.Add(typeof(1000000000000000000003.5m + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002.5Lr))
Types.Add(typeof(1000000000000000000003.5Lr + 1000000000000000000002.5Lm))
Types.Add(typeof(1000000000000000000003.5Lm + 1000000000000000000002.5Lm))
return Types
", "null", "Ошибок нет")]
	[DataRow(@"var x = 5
return ++x
", NullString, "Error 2039 in line 2 at position 7: the prefix increment/decrement operators (\"++x\") were removed from PL051"
		+ " because of their obscurity and ability to produce the \"dirty\" code;"
		+ " use either the postfix increment/decrement operators (\"x++\")"
		+ " or the composite assignment operators (\"x += 1\")\r\n")]
	[DataRow(@"var x = 5
return --x
", NullString, "Error 2039 in line 2 at position 7: the prefix increment/decrement operators (\"++x\") were removed from PL051"
		+ " because of their obscurity and ability to produce the \"dirty\" code;"
		+ " use either the postfix increment/decrement operators (\"x++\")"
		+ " or the composite assignment operators (\"x += 1\")\r\n")]
	[DataRow("""
		var s = /"var s = /""\;
		return s.Insert(10, s) + Q();"\;
		return s.Insert(10, s) + Q();
		""",
"""
		/"var s = /"var s = /""\;
		return s.Insert(10, s) + Q();"\;
		return s.Insert(10, s) + Q();var s = /"var s = /""\;
		return s.Insert(10, s) + Q();"\;
		return s.Insert(10, s) + Q();"\
		""",
			"Ошибок нет")]
	[DataRow(@"int x=null
return x*1
", "0", "Ошибок нет")]
	[DataRow(@"return куегкт
", NullString, @"Error 4001 in line 1 at position 7: the identifier ""куегкт"" is not defined in this location
")]
	[DataRow("""
real Function D(real[3] abc)
{
	return abc[2] * abc[2] - 4 * abc[1] * abc[3]
}
string Function DecomposeSquareTrinomial(real[3] abc)
{
	if (abc[1] == 0)
		return "Это не квадратный трехчлен"
	var d = D(abc)
	string first
	first = abc[1] switch
	{
		1 => "",
		-1 => "-",
		_ => abc[1] + "",
	}
	if (d < 0)
		return "Неразложимо"
	else if (d == 0)
		return first + Format(abc[2] / (2 * abc[1])) + '²'
	else
	{
		var sqrtOfD = Sqrt(d)
		return first + Format((abc[2] + sqrtOfD) / (2 * abc[1])) + Format((abc[2] - sqrtOfD) / (2 * abc[1]))
	}
}
string Function Format(real n)
{
	if (n == 0)
		return "x"
	else if (n < 0)
		return "(x - " + (-n) + ")"
	else
		return "(x + " + n + ")"
}
var a1 = DecomposeSquareTrinomial((3, 9, -30))
var a2 = DecomposeSquareTrinomial((1, 16, 64))
var a3 = DecomposeSquareTrinomial((-1, -1, -10))
var a4 = DecomposeSquareTrinomial((0, 11, 5))
var a5 = DecomposeSquareTrinomial((-1, 0, 0))
var a6 = DecomposeSquareTrinomial((2, -11, 0))
return (a1, a2, a3, a4, a5, a6)

""", """("3(x + 5)(x - 2)", "(x + 8)²", "Неразложимо", "Это не квадратный трехчлен", "-x²", "2x(x - 5.5)")""", "Ошибок нет")]
	public void Test(string Key, string TargetResult, string TargetErrors)
	{
		CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CodeStyleRules.TestEnvironment = true;
		TestInternal(Key, TargetResult, TargetErrors);
		Assert.ThrowsExactly<FormatException>(() => throw new FormatException());
	}

	private static void TestInternal(string Key, string TargetResult, string TargetErrors)
	{
		String result = default!, errors = default!;
		var thread = new System.Threading.Thread(() => result = ExecuteProgram(Key, [], out errors), int.MaxValue)
		{
			Name = "Program translation"
		};
		thread.Start();
		thread.IsBackground = true;
		thread.Join();
		if (result == TargetResult && (TargetErrors is null || errors == TargetErrors))
			return;
		String message = "Error: @\"" + Key.Replace("\"", "\"\"") + "\"";
		if (result != TargetResult)
		{
			var cpl = result.AsSpan().CommonPrefixLength(TargetResult);
			var split = TargetResult[..cpl].Split("\r\n");
			var line = split.Length;
			var position = split.Length == 0 ? 0 : split[^1].Length;
			message.AddRange(" returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\""
				+ TargetResult.Replace("\"", "\"\"") + "\" (difference in line " + line + " at position " + position + ")");
		}
		if (TargetErrors is not null && errors != TargetErrors)
		{
			var cpl = errors.AsSpan().CommonPrefixLength(TargetErrors);
			var split = TargetErrors[..cpl].Split("\r\n");
			var line = split.Length;
			var position = split.Length == 0 ? 0 : split[^1].Length;
			message.AddRange(" and produced errors @\"" + errors.Replace("\"", "\"\"")
				+ "\" instead of @\"" + TargetErrors.Replace("\"", "\"\"")
				+ "\" (difference in line " + line + " at position " + position + ")");
		}
		message.AddRange("!");
		throw new Exception(message.ToString());
	}
}

[TestClass]
public class FuncDictionaryTests
{
	[TestMethod]
	public void ComplexTest()
	{
		Random random = new(1234567890);
		Random random2 = new(1234567890);
		Random actionsRandom = new(1234567890);
		var counter = 0;
	l1:
		var arr = RedStarLinq.FillArray(random.Next(1, 100), _ => (Key: random.Next(100), Value: (Func<int, int>)(key => (int)Round(Sin(key) * 1000))));
		var arr2 = RedStarLinq.FillArray(random2.Next(1, 100), _ => (Key: random2.Next(100), Value: (Func<int, int>)(key => (int)Round(Sin(key) * 1000))));
		FuncDictionary<int, int> dic = new(new(arr2), []);
		var dic2 = E.ToDictionary(arr.RemoveDoubles(x => x.Key), x => x.Key, x => x.Value);
		var actions = new[] { () =>
		{
			var (index, n) = (random.Next(100), random.Next(100));
			_ = (random2.Next(100), random2.Next(100));
			switch (actionsRandom.Next(3))
			{
				case 0:
				if (dic.TryAdd(index, n))
					dic2.Add(index, key => n);
				break;
				case 1:
				if (dic.TryAdd(index, key => n))
					dic2.Add(index, key => n);
				break;
				case 2:
				if (dic2.Count == 0)
					return;
				index = dic2.Keys.ToList().Random(actionsRandom);
				n = dic2[index](index);
				dic.Remove(index);
				dic.Add(key => key == index, key => n);
				break;
				default:
					throw new InvalidOperationException();
			}
			Assert.HasCount(dic2.Count, dic);
			Assert.IsTrue(dic2.All(x => dic.TryGetValue(x.Key, out var value) && value.Equals(x.Value(x.Key))));
		}, () =>
		{
			if (dic.Length == 0)
				return;
			var (index, n) = (random.Next(100), random.Next(100));
			_ = (random2.Next(100), random2.Next(100));
			dic[index] = n;
			dic2[index] = key => n;
			Assert.HasCount(dic2.Count, dic);
			Assert.IsTrue(dic2.All(x => dic.TryGetValue(x.Key, out var value) && value.Equals(x.Value(x.Key))));
		}, () =>
		{
			if (dic.Length == 0)
				return;
			if (actionsRandom.Next(25) == 0)
			{
				dic.Clear();
				dic2.Clear();
			}
			else
			{
				var (n, _) = (random.Next(100), random.Next(100));
				_ = (random2.Next(100), random2.Next(100));
				var b = dic.TryGetValue(n, out _);
				if (!b)
					return;
				dic.Remove(n);
				dic2.Remove(n);
			}
			Assert.HasCount(dic2.Count, dic);
			Assert.IsTrue(dic2.All(x => dic.TryGetValue(x.Key, out var value) && value.Equals(x.Value(x.Key))));
		} };
		for (var i = 0; i < 1000; i++)
			actions.Random(actionsRandom)();
		if (counter++ < 1000)
			goto l1;
	}
}

[TestClass]
public class UtilityFunctionTests
{
	private readonly Random random = new(1234567890);

	[TestMethod]
	public void TestIsPrime()
	{
		for (var i = 0; i <= 10; i++)
		{
			var a = NStarUtilityFunctions.IsPrime(i);
			var b = ((MpzT)i).IsProbablyPrimeRabinMiller(100);
			Assert.AreEqual(b, a);
		}
		for (var i = 0; i < 10000; i++)
		{
			var n = random.Next();
			var a = NStarUtilityFunctions.IsPrime(n);
			var b = ((MpzT)n).IsProbablyPrimeRabinMiller(100);
			Assert.AreEqual(b, a);
		}
	}
}
