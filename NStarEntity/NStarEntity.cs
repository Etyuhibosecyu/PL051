global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.TypeConverters;
global using static System.Math;
global using Complex = RedStarMath.Complex;
global using String = NStar.Core.String;
using NStar.Mpir;
using RedStarMath;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace PL051.NStar;

[DebuggerDisplay("{ToString(true)}")]
public struct NStarEntity
{
	private readonly object? data;
	public NStarType Type { get; set; }
	public static NStarEntity Infinity => double.PositiveInfinity;
	public static NStarEntity MinusInfinity => double.NegativeInfinity;
	public static NStarEntity Uncertainty => double.NaN;

	private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

	public NStarEntity()
	{
		data = null;
		Type = NullType;
	}

	private NStarEntity(object @object, NStarType type)
	{
		if (@object is NStarEntity unv)
			data = unv.data;
		else
			data = @object;
		Type = type;
	}

	public readonly NStarEntity CyclicShift(int shiftAmount)
	{
		if (TypeIsPrimitive(Type.MainType))
		{
			var basicType = Type.MainType.Peek().Name;
			if (basicType.AsSpan() is LongDeccomplexTypeName or LongComplexTypeName or DeccomplexTypeName or ComplexTypeName
				or LongDecimalTypeName or LongRealTypeName or UnsignedLongDecimalTypeName or UnsignedLongRealTypeName
				or DecimalTypeName or RealTypeName or UnsignedLongLongTypeName or LongLongTypeName)
				return ToNumber() << shiftAmount;
			else if (basicType == RealTypeName)
				return ToReal().Shift(shiftAmount);
			else if (basicType == UnsignedLongIntTypeName)
				return (ulong)ToNumber() is var uli
					? uli << (int)unchecked((uint)shiftAmount % (sizeof(ulong) * 8))
					| uli >>> (int)unchecked((uint)-shiftAmount % (sizeof(ulong) * 8)) : 0;
			else if (basicType == LongIntTypeName)
				return (long)ToNumber() is var li
					? li << (int)unchecked((uint)shiftAmount % (sizeof(long) * 8))
					| li >>> (int)unchecked((uint)-shiftAmount % (sizeof(long) * 8)) : 0;
			else if (basicType == UnsignedIntTypeName)
				return (uint)ToNumber() is var ui
					? ui << (int)unchecked((uint)shiftAmount % (sizeof(uint) * 8))
					| ui >>> (int)unchecked((uint)-shiftAmount % (sizeof(uint) * 8)) : 0;
			else if (basicType == IntTypeName)
				return ToInt() is var i
					? i << (int)unchecked((uint)shiftAmount % (sizeof(int) * 8))
					| i >>> (int)unchecked((uint)-shiftAmount % (sizeof(int) * 8)) : 0;
			else if (basicType == UnsignedShortIntTypeName)
				return (ushort)((ushort)ToInt() is var usi
					? usi << (int)unchecked((uint)shiftAmount % (sizeof(ushort) * 8))
					| usi >>> (int)unchecked((uint)-shiftAmount % (sizeof(ushort) * 8)) : 0);
			else if (basicType == ShortIntTypeName)
				return (short)((short)ToInt() is var si
					? si << (int)unchecked((uint)shiftAmount % (sizeof(short) * 8))
					| si >>> (int)unchecked((uint)-shiftAmount % (sizeof(short) * 8)) : 0);
			else if (basicType == ByteTypeName)
				return (byte)((byte)ToInt() is var y
					? y << (int)unchecked((uint)shiftAmount % (sizeof(byte) * 8))
					| y >>> (int)unchecked((uint)-shiftAmount % (sizeof(byte) * 8)) : 0);
			else
				return new();
		}
		else
			return new();
	}

	public override readonly bool Equals(object? obj) => obj is not null
		&& obj is NStarEntity m && ToBool() == m.ToBool() && ToReal() == m.ToReal() && ToString() == m.ToString();

	public override readonly int GetHashCode()
	{
		if (TypeIsPrimitive(Type.MainType))
		{
			var s = Type.MainType.Peek().Name;
			if (s == NullString)
				return 0;
			else if (s == BoolTypeName && data is bool b)
				return b.GetHashCode();
			else if (s.AsSpan() is ByteTypeName or ShortIntTypeName or UnsignedShortIntTypeName
				or IntTypeName or UnsignedIntTypeName or LongIntTypeName or UnsignedLongIntTypeName
				or LongLongTypeName or UnsignedLongLongTypeName or RealTypeName or DecimalTypeName
				or UnsignedLongRealTypeName or UnsignedLongDecimalTypeName or LongRealTypeName or LongDecimalTypeName)
				return ToNumber().GetHashCode();
			else if (s == CharTypeName && data is char @char)
				return @char.GetHashCode();
			else if (s == LongIntTypeName && data is long li)
				return li.GetHashCode();
			else if (s == nameof(DateTime) && data is DateTime dt)
				return dt.GetHashCode();
			else if (s == UnsignedLongIntTypeName && data is ulong uli)
				return uli.GetHashCode();
			else if (s == UnsignedLongLongTypeName && data is ulong ull)
				return ull.GetHashCode();
			else if (s == LongLongTypeName && data is MpzT ll)
				return ll.GetHashCode();
			else if (s == ComplexTypeName && data is Complex c)
				return c.GetHashCode();
			else if (s == StringTypeName && data is String String)
				return String.GetHashCode();
		}
		return 0;
	}

	public static String GetQuotientType(String leftType, NStarEntity right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		var rightConverted = (MpzT)right.ToNumber();
		string t;
		if (leftType == LongLongTypeName)
			return LongLongTypeName;
		else if (leftType == UnsignedLongLongTypeName)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongLongTypeName;
		}
		else if (leftType == UnsignedLongIntTypeName)
		{
			if (rightConverted >= 1uL << 56)
				return ByteTypeName;
			else if (rightConverted >= 1uL << 48)
				return UnsignedShortIntTypeName;
			else if (rightConverted >= 4294967296)
				return UnsignedIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongIntTypeName;
		}
		else if (leftType == LongIntTypeName)
		{
			if (rightConverted >= 1L << 48)
				return ShortIntTypeName;
			else if (rightConverted >= 4294967296)
				return IntTypeName;
			else if (rightType == UnsignedLongIntTypeName)
				return LongLongTypeName;
			else
				return LongIntTypeName;
		}
		else if (leftType == (t = LongCharTypeName) || rightType == t)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return LongCharTypeName;
		}
		else if (leftType == UnsignedIntTypeName)
		{
			if (rightConverted >= 16777216)
				return ByteTypeName;
			else if (rightConverted >= 65536)
				return UnsignedShortIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return UnsignedIntTypeName;
		}
		else if (leftType == IntTypeName)
		{
			if (rightType == UnsignedIntTypeName)
				return LongIntTypeName;
			else if (right.ToInt() >= 65536)
				return ShortIntTypeName;
			else
				return IntTypeName;
		}
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return CharTypeName;
		}
		else if (leftType == UnsignedShortIntTypeName)
			return ValidateUnsignedShortIntType(rightConverted >= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	public static String GetRemainderType(String leftType, NStarEntity right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		var rightConverted = (MpzT)right.ToNumber();
		string t;
		if (leftType == LongLongTypeName)
		{
			if (rightConverted <= 32768)
				return ShortIntTypeName;
			else if (rightConverted <= 2147483648)
				return IntTypeName;
			else if (rightConverted <= 9223372036854775808)
				return LongIntTypeName;
			else
				return LongLongTypeName;
		}
		else if (leftType == UnsignedLongLongTypeName)
		{
			if (rightConverted <= 256)
				return ByteTypeName;
			else if (rightConverted <= 65536)
				return UnsignedShortIntTypeName;
			else if (rightConverted <= 4294967296)
				return UnsignedIntTypeName;
			else if (rightConverted <= MpuT.One << 64)
				return UnsignedLongIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongLongTypeName;
		}
		else if (leftType == UnsignedLongIntTypeName)
		{
			if (rightConverted <= 256)
				return ByteTypeName;
			else if (rightConverted <= 65536)
				return UnsignedShortIntTypeName;
			else if (rightConverted <= 4294967296)
				return UnsignedIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return UnsignedLongIntTypeName;
		}
		else if (leftType == LongIntTypeName)
		{
			if (rightConverted <= 32768)
				return ShortIntTypeName;
			else if (rightConverted <= 2147483648)
				return IntTypeName;
			else if (rightType == UnsignedLongIntTypeName)
				return LongLongTypeName;
			else
				return LongIntTypeName;
		}
		else if (leftType == (t = LongCharTypeName) || rightType == t)
		{
			if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return LongCharTypeName;
		}
		else if (leftType == UnsignedIntTypeName)
		{
			if (rightConverted <= 256)
				return ByteTypeName;
			else if (rightConverted <= 65536)
				return UnsignedShortIntTypeName;
			else if (rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return UnsignedIntTypeName;
		}
		else if (leftType == IntTypeName)
		{
			if (rightType == UnsignedIntTypeName)
				return LongIntTypeName;
			else if (right.ToInt() <= 32768)
				return ShortIntTypeName;
			else
				return IntTypeName;
		}
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return CharTypeName;
		}
		else if (leftType == UnsignedShortIntTypeName)
			return ValidateUnsignedShortIntType(rightConverted <= 256, rightType);
		else
			return ValidatePostUSIType(leftType, rightType);
	}

	public readonly bool ToBool()
	{
		if (!TypeIsPrimitive(Type.MainType))
			return false;
		var basicType = Type.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => false,
			BoolTypeName when data is bool b => b,
			ByteTypeName or ShortCharTypeName or ShortIntTypeName or UnsignedShortIntTypeName
				or CharTypeName or IntTypeName or UnsignedIntTypeName
				or LongCharTypeName or LongIntTypeName or UnsignedLongIntTypeName
				or LongLongTypeName or UnsignedLongLongTypeName or RealTypeName or DecimalTypeName
				or UnsignedLongRealTypeName or UnsignedLongDecimalTypeName or LongRealTypeName or LongDecimalTypeName =>
				ToNumber() >= 1,
			ComplexTypeName or DeccomplexTypeName or LongComplexTypeName or LongDeccomplexTypeName =>
			ToNumber().Real >= 1,
			StringTypeName when data is String String => String != "",
			_ => false
		};
	}

	public readonly int ToInt()
	{
		if (!TypeIsPrimitive(Type.MainType))
			return 0;
		var basicType = Type.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => data is not bool b || !b ? 0 : 1,
			ByteTypeName => data is not byte r ? 0 : r,
			ShortIntTypeName => data is not short r ? 0 : r,
			UnsignedShortIntTypeName => data is not ushort r ? 0 : r,
			CharTypeName => data is not char r ? 0 : r,
			IntTypeName => data is not int r ? 0 : r,
			UnsignedIntTypeName => (int)(data is not uint ui || ui > 2147483647 ? 0 : ui),
			LongIntTypeName => (int)(data is not long li || li is < -2147483648 or > 2147483647 ? 0 : li),
			nameof(DateTime) => (int)(data is not DateTime dt || dt.Ticks > 2147483647 ? 0 : dt.Ticks),
			UnsignedLongIntTypeName => (int)(data is not ulong uli || uli > 2147483647 ? 0 : uli),
			UnsignedLongLongTypeName => (int)(data is not MpuT ull || ull > 2147483647 ? 0 : ull),
#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
			LongLongTypeName => (int)(data is not MpzT ll ? 0 : ll < -2147483648 || ll > 2147483647 ? 0 : ll),
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
			RealTypeName => (int)(data is not double r || r is < -2147483648 or > 2147483647 ? 0 : Truncate(r)),
			DecimalTypeName => (int)(data is not decimal m || m is < -2147483648 or > 2147483647 ? 0 : Truncate(m)),
			UnsignedLongRealTypeName => (int)(data is not UnsignedLongReal ulr || ulr > 2147483647 ? 0 : ulr),
			UnsignedLongDecimalTypeName => (int)(data is not UnsignedLongDecimal ulm || ulm > 2147483647 ? 0 : ulm),
			LongRealTypeName => (int)(data is not LongReal lr || lr < -2147483648 || lr > 2147483647 ? 0 : lr.Truncate()),
			LongDecimalTypeName => (int)(data is not LongDecimal lm || lm < -2147483648 || lm > 2147483647 ? 0 : lm.Truncate()),
			StringTypeName => 0,
			_ => 0
		};
	}

	public readonly dynamic ToNumber()
	{
		if (!TypeIsPrimitive(Type.MainType))
			return 0;
		var basicType = Type.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => data is not bool b || !b ? 0 : 1,
			ByteTypeName => data is not byte r ? 0 : r,
			ShortIntTypeName => data is not short r ? 0 : r,
			UnsignedShortIntTypeName => data is not ushort r ? 0 : r,
			CharTypeName => data is not char r ? 0 : r,
			IntTypeName => data is not int r ? 0 : r,
			UnsignedIntTypeName => data is not uint r ? 0 : r,
			LongIntTypeName => data is not long li ? 0 : li,
			nameof(DateTime) => data is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => data is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => data is not MpuT ull ? 0 : ull,
			LongLongTypeName => data is not MpzT ll ? 0 : ll,
			RealTypeName => data is not double r ? 0 : r,
			DecimalTypeName => data is not decimal m ? 0 : m,
			UnsignedLongRealTypeName => data is not UnsignedLongReal ulr ? 0 : ulr,
			UnsignedLongDecimalTypeName => data is not UnsignedLongDecimal ulm ? 0 : ulm,
			LongRealTypeName => data is not LongReal lr ? 0 : lr,
			LongDecimalTypeName => data is not LongDecimal lm ? 0 : lm,
			ComplexTypeName => data is not Complex c ? 0 : c,
			_ => 0,
		};
	}

	public readonly double ToReal()
	{
		if (!TypeIsPrimitive(Type.MainType))
			return 0;
		var basicType = Type.MainType.Peek().Name.ToString() ?? NullString;
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => data is not bool b || !b ? 0 : 1,
			ByteTypeName => data is not byte r ? 0 : r,
			ShortIntTypeName => data is not short r ? 0 : r,
			UnsignedShortIntTypeName => data is not ushort r ? 0 : r,
			CharTypeName => data is not char r ? 0 : r,
			IntTypeName => data is not int r ? 0 : r,
			UnsignedIntTypeName => data is not uint r ? 0 : r,
			LongIntTypeName => data is not long li ? 0 : li,
			nameof(DateTime) => data is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => data is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (double)(data is not MpuT ull ? 0 : ull),
			LongLongTypeName => (double)(data is not MpzT ll ? 0 : ll),
			RealTypeName => (double)(data is not double r ? 0 : r),
			DecimalTypeName => (data is not decimal m ? 0 : m).ToReal(),
			UnsignedLongRealTypeName => (double)(data is not UnsignedLongReal ulr ? 0 : ulr),
			UnsignedLongDecimalTypeName => (double)(data is not UnsignedLongDecimal ulm ? 0 : ulm),
			LongRealTypeName => (double)(data is not LongReal lr ? 0 : lr),
			LongDecimalTypeName => (double)(data is not LongDecimal lm ? 0 : lm),
			StringTypeName => 0,
			_ => 0
		};
	}

	public readonly String ToString(bool takeIntoQuotes = false, bool addCasting = false)
	{
		if (!TypeIsPrimitive(Type.MainType))
			return takeIntoQuotes ? "Unknown Object" : "";
		var basicType = Type.MainType.Peek().Name ?? NullString;
		switch (basicType.AsSpan())
		{
			case NullString:
			return addCasting ? DefaultNull : NullString;
			case BoolTypeName when data is bool b:
			return b ? "true" : False;
			case ByteTypeName when data is byte y:
			return y.ToString(InvariantCulture);
			case ShortIntTypeName when data is short si:
			return si.ToString(InvariantCulture);
			case UnsignedShortIntTypeName when data is ushort usi:
			return usi.ToString(InvariantCulture);
			case CharTypeName when data is char c:
			return takeIntoQuotes ? "'" + c switch
			{
				'\0' => @"\0",
				'\a' => @"\a",
				'\b' => @"\b",
				'\f' => @"\f",
				'\n' => @"\n",
				'\r' => @"\r",
				'\t' => @"\t",
				'\v' => @"\v",
				'\'' => @"\'",
				'\"' => @"\q",
				'\\' => @"\!",
				_ => c,
			} + "'" : "" + c;
			case IntTypeName when data is int i:
			return i.ToString(InvariantCulture);
			case UnsignedIntTypeName when data is uint ui:
			return ui.ToString(InvariantCulture);
			case LongIntTypeName:
			return data is null ? "" : data is long li ? li.ToString() : "0";
			case nameof(DateTime):
			return data is null ? "" : data is DateTime dt ? dt.ToString() : new DateTime(0).ToString();
			case UnsignedLongIntTypeName:
			return data is null ? "" : data is ulong uli ? uli.ToString() : "0";
			case UnsignedLongLongTypeName when data is MpuT ull:
			if (!addCasting)
				return ull.ToString();
			if (ull <= ulong.MaxValue)
				return "new " + nameof(MpuT) + '(' + ull.ToString() + ')';
			return "new " + nameof(MpuT) + "(\"" + ull.ToString() + "\")";
			case LongLongTypeName when data is MpzT ll:
			if (!addCasting)
				return ll.ToString();
#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
			if (ll >= long.MinValue && ll <= ulong.MaxValue)
				return "new " + nameof(MpzT) + '(' + ll.ToString() + ')';
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
			return "new " + nameof(MpzT) + "(\"" + ll.ToString() + "\")";
			case RealTypeName when data is double r:
			return r switch
			{
				double.PositiveInfinity => addCasting ? "double.PositiveInfinity" : "Infty",
				double.NegativeInfinity => addCasting ? "double.NegativeInfinity" : "-Infty",
				double.NaN => addCasting ? "double.NaN" : "Uncty",
				-0d => addCasting ? "0d" : "0",
				_ => r.ToString(InvariantCulture) + (addCasting ? "d" : "")
			};
			case DecimalTypeName when data is decimal m:
			return m.ToString();
			case UnsignedLongRealTypeName when data is UnsignedLongReal ulr:
			if (!addCasting)
				return ulr.ToString();
			if (ulr <= ulong.MaxValue)
				return "new " + nameof(UnsignedLongReal) + '(' + ulr.ToString() + ')';
			return "new " + nameof(UnsignedLongReal) + "(\"" + ulr.ToString() + "\")";
			case UnsignedLongDecimalTypeName when data is UnsignedLongDecimal ulm:
			if (!addCasting)
				return ulm.ToString();
			if (ulm <= ulong.MaxValue)
				return "new " + nameof(UnsignedLongDecimal) + '(' + ulm.ToString() + ')';
			return "new " + nameof(UnsignedLongDecimal) + "(\"" + ulm.ToString() + "\")";
			case LongRealTypeName when data is LongReal lr:
			if (LongReal.IsPositiveInfinity(lr))
				return addCasting ? nameof(LongReal) + "." + nameof(LongReal.PositiveInfinity) : nameof(LongReal) + ".Infty";
			else if (LongReal.IsNegativeInfinity(lr))
				return addCasting ? nameof(LongReal) + "." + nameof(LongReal.NegativeInfinity) : "-" + nameof(LongReal) + ".Infty";
			else if (LongReal.IsNaN(lr))
				return addCasting ? nameof(LongReal) + "." + nameof(LongReal.NaN) : nameof(LongReal) + ".Uncty";
			else
				return (addCasting ? "(" + nameof(LongReal) + ")" : "") + lr.ToString(InvariantCulture);
			case LongDecimalTypeName when data is LongDecimal lm:
			if (LongDecimal.IsPositiveInfinity(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.PositiveInfinity) : nameof(LongDecimal) + ".Infty";
			else if (LongDecimal.IsNegativeInfinity(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.NegativeInfinity) : "-" + nameof(LongDecimal) + ".Infty";
			else if (LongDecimal.IsNaN(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.NaN) : nameof(LongDecimal) + ".Uncty";
			else
				return (addCasting ? "(" + nameof(LongDecimal) + ")" : "") + lm.ToString(InvariantCulture);
			case ComplexTypeName when data is Complex c:
			return (addCasting ? "new Complex(" : "") + c.Real switch
			{
				double.PositiveInfinity => addCasting ? "double.PositiveInfinity" : "Infty",
				double.NegativeInfinity => addCasting ? "double.NegativeInfinity" : "-Infty",
				double.NaN => addCasting ? "double.NaN" : "Uncty",
				-0d => "0",
				_ => c.Real.ToString(InvariantCulture)
			} + (addCasting ? ", " : c.Imaginary is double.NaN or >= 0 ? "+" : "") + c.Imaginary switch
			{
				double.PositiveInfinity => addCasting ? "double.PositiveInfinity" : "Infty",
				double.NegativeInfinity => addCasting ? "double.NegativeInfinity" : "-Infty",
				double.NaN => addCasting ? "double.NaN" : "Uncty",
				-0d => "0",
				_ => c.Imaginary.ToString(InvariantCulture)
			} + (addCasting ? ")" : "i");
			case RecursiveTypeName:
			return data is null ? "" : data is NStarType NStarType ? NStarType.ToString() : NullType.ToString();
			case StringTypeName when data is String String:
			if (!takeIntoQuotes)
				return String;
			else if (addCasting)
				return ((String)"((").AddRange(nameof(String)).Add(')').AddRange(String.TakeIntoQuotes(true)).Add(')');
			else
				return String.TakeIntoQuotes();
			case "list":
			throw new InvalidOperationException();
			default:
			return takeIntoQuotes ? "Unknown Object" : "";
		}
	}

	public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out NStarEntity result)
	{
		result = default;
		ReadOnlySpan<char> s2;
		if (s.Length == 0)
			return false;
		else if (s is NullString)
			result = new();
		else if (s is "true" or False)
			result = s is "true";
		else if (s is "Infty")
			result = Infinity;
		else if (s is "-Infty")
			result = MinusInfinity;
		else if (s is "Uncty")
			result = Uncertainty;
		else if (s is "Pi")
			result = PI;
		else if (s is "E")
			result = E;
		else if (s[0] is not (>= '0' and <= '9' or '+' or '-') && s[^1] is not ('\"' or '\'' or '\\'))
			return false;
		else if (s[^1] == 'n')
		{
			if (byte.TryParse(s[..^1], InvariantCulture, out var y))
				result = y;
			else if (short.TryParse(s[..^1], InvariantCulture, out var si))
				result = si;
			else if (ushort.TryParse(s[..^1], InvariantCulture, out var usi))
				result = usi;
			else if (int.TryParse(s[..^1], InvariantCulture, out var i))
				result = i;
			else
				return false;
		}
		else if (s[^1] == 'u')
		{
			if (uint.TryParse(s[..^1], InvariantCulture, out var ui))
				result = ui;
			else
				return false;
		}
		else if (s[^1] == 'L')
		{
			s2 = s[..^1];
			var @double = false;
			if (s2.EndsWith('L'))
			{
				@double = true;
				s2 = s2[..^1];
			}
			if (s2.EndsWith('u'))
			{
				s2 = s2[..^1];
				if (!@double && ulong.TryParse(s2, out var ul))
					result = new(ul, UnsignedLongIntType);
				else if (MpuT.TryParse(s2, out var ull))
					result = new(ull, UnsignedLongLongType);
				else
					return false;
				return true;
			}
			else
			{
				if (!@double && long.TryParse(s2, out var l))
					result = new(l, LongIntType);
				else if (MpzT.TryParse(s2, out var ll))
					result = new(ll, LongLongType);
				else
					return false;
				return true;
			}
		}
		else if (s[^1] == 'r')
		{
			s2 = s[..^1];
			var modifier = 0;
			if (s2.EndsWith('L'))
			{
				modifier++;
				s2 = s2[..^1];
				if (s2.EndsWith('u'))
				{
					modifier++;
					s2 = s2[..^1];
				}
			}
			if (!s2.All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (modifier == 0 && double.TryParse(s2, out var r))
			{
				double n;
				if (int.TryParse(s2, InvariantCulture, out var i))
					n = i;
				else if (!double.TryParse(s2, InvariantCulture, out n))
					return false;
				result = n;
				result.Type = RealType;
			}
			else if (modifier == 2 && UnsignedLongReal.TryParse(s2, null, out var ulr))
				result = new(ulr, UnsignedLongRealType);
			else if (modifier == 1 && LongReal.TryParse(s2, null, out var lr))
				result = new(lr, LongRealType);
			else
				return false;
		}
		else if (s[^1] == 'm')
		{
			s2 = s[..^1];
			var modifier = 0;
			if (s2.EndsWith('L'))
			{
				modifier++;
				s2 = s2[..^1];
				if (s2.EndsWith('u'))
				{
					modifier++;
					s2 = s2[..^1];
				}
			}
			if (!s2.All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (modifier == 0 && double.TryParse(s2, out var r))
			{
				decimal n;
				if (int.TryParse(s2, InvariantCulture, out var i))
					n = i;
				else if (!decimal.TryParse(s2, InvariantCulture, out n))
					return false;
				result = new(n, DecimalType);
			}
			else if (modifier == 2 && UnsignedLongDecimal.TryParse(s2, null, out var ulm))
				result = new(ulm, UnsignedLongDecimalType);
			else if (modifier == 1 && LongDecimal.TryParse(s2, null, out var lr))
				result = new(lr, LongDecimalType);
			else
				return false;
		}
		else if (s[^1] == 'c')
		{
			s2 = s[..^1];
			if (!s2.All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (!double.TryParse(s2, InvariantCulture, out var n))
				return false;
			result = new(new Complex(n, 0), ComplexType);
		}
		else if (s[^1] == 'i')
		{
			s2 = s[..^1];
			if (!s2.All(x => (uint)(x - '0') <= 9 || ".Ee+-".Contains(x)))
				return false;
			if (!double.TryParse(s2, InvariantCulture, out var n))
				return false;
			result = new(new Complex(0, n), ComplexType);
		}
		else if (s[0] == '\"' && s[^1] == '\"')
			result = ((String)s).RemoveQuotes();
		else if (s[0] == '\'' && s[^1] == '\'')
			result = s.Length <= 2 ? (NStarEntity)'\0' : (NStarEntity)((String)s).RemoveQuotes()[0];
		else if (s.Length >= 3 && s[0] == '@' && s[1] == '\"' && s[^1] == '\"')
			result = ((String)s)[2..^1].Replace("\"\"", "\"");
		else if (Quotes.IsRawString(s, out var output))
			result = output;
		else if (byte.TryParse(s, NumberStyles.Integer, InvariantCulture, out var y))
			result = y;
		else if (short.TryParse(s, NumberStyles.Integer, InvariantCulture, out var si))
			result = si;
		else if (ushort.TryParse(s, NumberStyles.Integer, InvariantCulture, out var usi))
			result = usi;
		else if (int.TryParse(s, NumberStyles.Integer, InvariantCulture, out var i))
			result = i;
		else if (uint.TryParse(s, NumberStyles.Integer, InvariantCulture, out var ui))
			result = ui;
		else if (long.TryParse(s, NumberStyles.Integer, InvariantCulture, out var l))
			result = (NStarEntity)l;
		else if (ulong.TryParse(s, NumberStyles.Integer, InvariantCulture, out var ul))
			result = (NStarEntity)ul;
		else if (double.TryParse(s, NumberStyles.Float, InvariantCulture, out var d))
			result = d;
		else
			return false;
		return true;
	}

	private static String ValidatePostUSIType(String leftType, String rightType)
	{
		string t;
		if (leftType == ShortIntTypeName)
		{
			if (rightType == UnsignedShortIntTypeName)
				return IntTypeName;
			else
				return ShortIntTypeName;
		}
		else if (leftType == (t = ShortCharTypeName) || rightType == t)
			return ShortCharTypeName;
		else if (leftType.AsSpan() is ByteTypeName or BoolTypeName)
			return ByteTypeName;
		else
			return NullString;
	}

	private static String? ValidateRealType(String leftType, String rightType)
	{
		string t;
		if (leftType == (t = LongComplexTypeName) || rightType == t || leftType == (t = LongRealTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongLongTypeName) || rightType == t)
		{
			if (leftType == (t = RealTypeName) || rightType == t)
				return LongRealTypeName;
			else
				return null;
		}
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or RealTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or RealTypeName)
				return LongRealTypeName;
			else
				return null;
		}
		else if (leftType == (t = ComplexTypeName) || rightType == t || leftType == (t = RealTypeName) || rightType == t)
			return t;
		else if (rightType == BoolTypeName)
			return ByteTypeName;
		return null;
	}

	private static String ValidateUnsignedShortIntType(bool condition, String rightType)
	{
		if (condition)
			return ByteTypeName;
		else if (rightType == ShortIntTypeName)
			return IntTypeName;
		else
			return UnsignedShortIntTypeName;
	}

	public static implicit operator NStarEntity(bool x) => new(x, BoolType);

	public static implicit operator NStarEntity(byte x) => new(x, ByteType);

	public static implicit operator NStarEntity(short x) => new(x, ShortIntType);

	public static implicit operator NStarEntity(ushort x) => new(x, UnsignedShortIntType);

	public static implicit operator NStarEntity(char x) => new(x, CharType);

	public static implicit operator NStarEntity(int x) => new(x, IntType);

	public static implicit operator NStarEntity(uint x) => new(x, UnsignedIntType);

	public static implicit operator NStarEntity(long x) => new(x, LongIntType);

	public static implicit operator NStarEntity(ulong x) => new(x, UnsignedLongIntType);

	public static implicit operator NStarEntity(MpzT x) => new(x, LongLongType);

	public static implicit operator NStarEntity(MpuT x) => new(x, UnsignedLongLongType);

	public static implicit operator NStarEntity(DateTime x) => new(x, DateTimeType);

	public static implicit operator NStarEntity(double x) => new(x, RealType);

	public static implicit operator NStarEntity(decimal x) => new(x, DecimalType);

	public static implicit operator NStarEntity(UnsignedLongReal x) => new(x, UnsignedLongRealType);

	public static implicit operator NStarEntity(UnsignedLongDecimal x) => new(x, UnsignedLongDecimalType);

	public static implicit operator NStarEntity(LongReal x) => new(x, LongRealType);

	public static implicit operator NStarEntity(LongDecimal x) => new(x, LongDecimalType);

	public static implicit operator NStarEntity(Complex x) => new(x, ComplexType);

	public static implicit operator NStarEntity(string x) => new((String)x, StringType);

	public static implicit operator NStarEntity(String x) => new(x, StringType);

	public static NStarEntity operator +(NStarEntity x)
	{
		if (!TypeIsPrimitive(x.Type.MainType))
			return new();
		var basicType = x.Type.MainType.Peek().Name;
		if (basicType.AsSpan() is ByteTypeName)
			return (byte)+x.ToNumber();
		else if (basicType.AsSpan() is ShortIntTypeName)
			return (short)+x.ToNumber();
		else if (basicType.AsSpan() is UnsignedShortIntTypeName)
			return (ushort)+x.ToNumber();
		else
			return +x.ToNumber();
	}

	public static NStarEntity operator -(NStarEntity x)
	{
		if (!TypeIsPrimitive(x.Type.MainType))
			return new();
		var basicType = x.Type.MainType.Peek().Name;
		if (basicType.AsSpan() is ByteTypeName or ShortIntTypeName)
			return (short)-x.ToNumber();
		else
			return -x.ToNumber();
	}

	public static NStarEntity operator !(NStarEntity x) =>
		!x.ToBool() && TypeIsPrimitive(x.Type.MainType) && x.Type.MainType.Peek().Name == BoolTypeName;

	public static NStarEntity operator ~(NStarEntity x)
	{
		if (!TypeIsPrimitive(x.Type.MainType))
			return new();
		var basicType = x.Type.MainType.Peek().Name;
		if (basicType.AsSpan() is ByteTypeName)
			return (byte)~x.ToNumber();
		else if (basicType.AsSpan() is ShortIntTypeName)
			return (short)~x.ToNumber();
		else if (basicType.AsSpan() is UnsignedShortIntTypeName)
			return (ushort)~x.ToNumber();
		else if (basicType.AsSpan() is DecimalTypeName or RealTypeName)
			return -1 - x.ToNumber();
		else
			return ~x.ToNumber();
	}

	public static NStarEntity operator +(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		var leftType = left.Type.MainType.Peek().Name;
		var rightType = right.Type.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		string t;
		if (leftType == NullString)
			return new();
		else if (leftType == (t = StringTypeName) || rightType == t)
			return left.ToString().Concat(right.ToString());
		return left.ToNumber() + right.ToNumber();
	}

	public static NStarEntity operator -(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		return left.ToNumber() - right.ToNumber();
	}

	public static NStarEntity operator *(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		var leftType = left.Type.MainType.Peek().Name;
		var rightType = right.Type.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		if (leftType == StringTypeName && rightType == StringTypeName)
			throw new InvalidOperationException();
		if (leftType == StringTypeName)
			return left.ToString().Repeat((int)(uint)right.ToNumber());
		else if (rightType == StringTypeName)
			return right.ToString().Repeat((int)(uint)left.ToNumber());
		return left.ToNumber() * right.ToNumber();
	}

	public static NStarEntity operator /(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		return left.ToNumber() / right.ToNumber();
	}

	public static NStarEntity operator %(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		return left.ToNumber() % right.ToNumber();
	}

	public static NStarEntity operator &(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		var leftType = left.Type.MainType.Peek().Name;
		var rightType = right.Type.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		if (leftType == "bool" & rightType == "bool")
			return left.ToBool() & right.ToBool();
		return left.ToNumber() & right.ToNumber();
	}

	public static NStarEntity operator |(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		var leftType = left.Type.MainType.Peek().Name;
		var rightType = right.Type.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		if (leftType == "bool" & rightType == "bool")
			return left.ToBool() | right.ToBool();
		return left.ToNumber() | right.ToNumber();
	}

	public static NStarEntity operator ^(NStarEntity left, NStarEntity right)
	{
		if (!TypeIsPrimitive(left.Type.MainType) || !TypeIsPrimitive(right.Type.MainType))
			return new();
		var leftType = left.Type.MainType.Peek().Name;
		var rightType = right.Type.MainType.Peek().Name;
		if (leftType == NullString)
			leftType = rightType;
		else if (rightType == NullString)
			rightType = leftType;
		if (leftType == "bool" & rightType == "bool")
			return left.ToBool() ^ right.ToBool();
		return left.ToNumber() ^ right.ToNumber();
	}

	public static NStarEntity operator <<(NStarEntity left, int right)
	{
		if (!TypeIsPrimitive(left.Type.MainType))
			return new();
		var basicType = left.Type.MainType.Peek().Name;
		if (basicType == DecimalTypeName)
			return ((decimal)left.ToNumber()).ShiftDec(right);
		else if (basicType == RealTypeName)
			return left.ToReal().Shift(right);
		return left.ToNumber() << right;
	}

	public static NStarEntity operator >>(NStarEntity left, int right)
	{
		if (!TypeIsPrimitive(left.Type.MainType))
			return new();
		var basicType = left.Type.MainType.Peek().Name;
		return basicType.AsSpan() switch
		{
			DecimalTypeName => ((decimal)left.ToNumber()).ShiftDec(-right),
			RealTypeName => left.ToReal().Shift(-right),
			_ => left.ToNumber() >> right
		};
	}

	public static NStarEntity operator >>>(NStarEntity left, int right)
	{
		if (!TypeIsPrimitive(left.Type.MainType))
			return new();
		var basicType = left.Type.MainType.Peek().Name;
		return basicType.AsSpan() switch
		{
			LongDecimalTypeName => (LongDecimal)left.ToNumber() >> right,
			LongRealTypeName => (LongReal)left.ToNumber() >> right,
			UnsignedLongDecimalTypeName => (UnsignedLongDecimal)left.ToNumber() >>> right,
			UnsignedLongRealTypeName => (UnsignedLongReal)left.ToNumber() >>> right,
			DecimalTypeName => ((decimal)left.ToNumber()).ShiftDec(-right),
			RealTypeName => left.ToReal().Shift(-right),
			LongLongTypeName => (MpzT)left.ToNumber() >>> right,
			UnsignedLongLongTypeName => (MpuT)left.ToNumber() >>> right,
			UnsignedLongIntTypeName => (ulong)left.ToNumber() >>> right,
			LongIntTypeName => (long)left.ToNumber() >>> right,
			UnsignedIntTypeName => (uint)left.ToNumber() >>> right,
			IntTypeName => (dynamic)(left.ToInt() >>> right),
			UnsignedShortIntTypeName => (ushort)left.ToNumber() >>> right,
			ShortIntTypeName => (short)left.ToNumber() >>> right,
			ByteTypeName => (byte)left.ToNumber() >>> right,
			_ => new NStarEntity()
		};
	}

	public static bool operator ==(NStarEntity left, NStarEntity right) =>
		left.ToBool() == right.ToBool() && left.ToReal() == right.ToReal() && left.ToString() == right.ToString();

	public static bool operator !=(NStarEntity left, NStarEntity right) => !(left == right);
}
