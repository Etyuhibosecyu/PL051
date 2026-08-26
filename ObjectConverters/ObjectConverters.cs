global using NStar.Core;
global using NStar.Mpir;
global using RedStarMath;
global using System;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.TypeConverters;
global using Complex = RedStarMath.Complex;
global using String = NStar.Core.String;

namespace PL051.NStar;

public static class ObjectConverters
{
	private const string WrongOperation = "Ошибка, невозможно выполнить эту операцию с такими объектами.";

	public static ReadOnlySpan<char> GetBasicNStarType(this object? data)
	{
		if (data is null)
			return NullString.AsSpan();
		var type = TypeMappingBack(data.GetType(), [], [], []);
		if (!(TypeIsPrimitive(type.MainType) && type.MainType.TryPeek(out var block)))
			throw new InvalidOperationException(WrongOperation);
		return block.Name.AsSpan();
	}

	public static NStarType GetNStarType(this object? data) =>
		data is null ? NullType : TypeMappingBack(data.GetType(), [], [], []);

	public static bool ToBool(this object data)
	{
		var basicType = data.GetBasicNStarType();
		if (IsInteger(basicType) || IsReal(basicType))
			return data.ToNumber() >= 1;
		else if (IsComplex(basicType))
			return data.ToNumber().Real >= 1;
		return basicType switch
		{
			NullString => false,
			BoolTypeName when data is bool b => b,
			StringTypeName when data is String String => String != "",
			_ => false
		};
	}

	public static dynamic ToNumber(this object data)
	{
		var basicType = data.GetBasicNStarType();
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => data is not bool b || !b ? 0 : 1,
			ByteTypeName => data is not byte y ? (byte)0 : y,
			ShortIntTypeName => data is not short si ? (short)0 : si,
			UnsignedShortIntTypeName => data is not ushort usi ? (ushort)0 : usi,
			CharTypeName => data is not char c ? '\0' : c,
			IntTypeName => data is not int i ? 0 : i,
			UnsignedIntTypeName => data is not uint ui ? 0 : ui,
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

	public static double ToReal(this object data)
	{
		var basicType = data.GetBasicNStarType();
		return basicType switch
		{
			NullString => 0,
			BoolTypeName => data is not bool b || !b ? 0 : 1,
			ByteTypeName => data is not byte y ? 0 : y,
			ShortIntTypeName => data is not short si ? 0 : si,
			UnsignedShortIntTypeName => data is not ushort usi ? 0 : usi,
			CharTypeName => data is not char c ? 0 : c,
			IntTypeName => data is not int i ? 0 : i,
			UnsignedIntTypeName => data is not uint ui ? 0 : ui,
			LongIntTypeName => data is not long li ? 0 : li,
			nameof(DateTime) => data is not DateTime dt ? 0 : dt.Ticks,
			UnsignedLongIntTypeName => data is not ulong uli ? 0 : uli,
			UnsignedLongLongTypeName => (double)(data is not MpuT ull ? 0 : ull),
			LongLongTypeName => (double)(data is not MpzT ll ? 0 : ll),
			RealTypeName => (double)(data is not double r ? 0 : r),
			DecimalTypeName => RedStarMath.Math.ToReal(data is not decimal m ? 0 : m),
			UnsignedLongRealTypeName => (double)(data is not UnsignedLongReal ulr ? 0 : ulr),
			UnsignedLongDecimalTypeName => (double)(data is not UnsignedLongDecimal ulm ? 0 : ulm),
			LongRealTypeName => (double)(data is not LongReal lr ? 0 : lr),
			LongDecimalTypeName => (double)(data is not LongDecimal lm ? 0 : lm),
			StringTypeName => 0,
			_ => 0
		};
	}

	public static String ToString(this object data, bool takeIntoQuotes, bool addCasting = false)
	{
		var basicType = data.GetBasicNStarType();
		switch (basicType)
		{
			case NullString:
			return addCasting ? DefaultNull : NullString;
			case BoolTypeName when data is bool b:
			return b ? "true" : False;
			case ByteTypeName when data is byte y:
			return y.ToString();
			case ShortIntTypeName when data is short si:
			return si.ToString();
			case UnsignedShortIntTypeName when data is ushort usi:
			return usi.ToString();
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
			return i.ToString();
			case UnsignedIntTypeName when data is uint ui:
			return ui.ToString();
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
				double.NegativeZero => addCasting ? "0d" : "0",
				_ => r.ToString() + (addCasting ? "d" : "")
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
				return (addCasting ? "(" + nameof(LongReal) + ")" : "") + lr.ToString();
			case LongDecimalTypeName when data is LongDecimal lm:
			if (LongDecimal.IsPositiveInfinity(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.PositiveInfinity) : nameof(LongDecimal) + ".Infty";
			else if (LongDecimal.IsNegativeInfinity(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.NegativeInfinity) : "-" + nameof(LongDecimal) + ".Infty";
			else if (LongDecimal.IsNaN(lm))
				return addCasting ? nameof(LongDecimal) + "." + nameof(LongDecimal.NaN) : nameof(LongDecimal) + ".Uncty";
			else
				return (addCasting ? "(" + nameof(LongDecimal) + ")" : "") + lm.ToString();
			case ComplexTypeName when data is Complex c:
			return (addCasting ? "new Complex(" : "") + c.Real switch
			{
				double.PositiveInfinity => addCasting ? "double.PositiveInfinity" : "Infty",
				double.NegativeInfinity => addCasting ? "double.NegativeInfinity" : "-Infty",
				double.NaN => addCasting ? "double.NaN" : "Uncty",
				double.NegativeZero => "0",
				_ => c.Real.ToString()
			} + (addCasting ? ", " : c.Imaginary is double.NaN or >= 0 ? "+" : "") + c.Imaginary switch
			{
				double.PositiveInfinity => addCasting ? "double.PositiveInfinity" : "Infty",
				double.NegativeInfinity => addCasting ? "double.NegativeInfinity" : "-Infty",
				double.NaN => addCasting ? "double.NaN" : "Uncty",
				double.NegativeZero => "0",
				_ => c.Imaginary.ToString()
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
}
