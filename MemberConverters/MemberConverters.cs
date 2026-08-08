global using NStar.Core;
global using NStar.MathLib;
global using System;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.NStarUtilityFunctions;
global using static PL051.NStar.TypeConverters;
global using static System.Math;
global using String = NStar.Core.String;
using Avalonia.Controls;
using System.IO;
using System.Threading.Tasks;

namespace PL051.NStar;

public static class MemberConverters
{
	public static String FunctionMapping(String function, List<NStarType> parameterTypes, List<String>? parameters,
		String? typeParametersCache)
	{
		var result = function.ToString() switch
		{
			"Add" => parameterTypes.Length != 0 && GetSubtype(parameterTypes[0]) == NullType
				&& !TypeEqualsToPrimitive(parameterTypes[0], TupleName, false)
				? nameof(function.Add) : nameof(function.AddRange),
			"Ceil" => "(int)" + nameof(Ceiling),
			nameof(Ceiling) => [],
			"Chain" => ((String)nameof(NStarUtilityFunctions)).Add('.').AddRange(nameof(Chain)),
			nameof(BitConverter.DoubleToInt64Bits) => [],
			nameof(BitConverter.DoubleToUInt64Bits) => [],
			nameof(RedStarLinq.Fill) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)),
			"FillList" => [],
			nameof(Floor) => "(int)" + nameof(Floor),
			nameof(BitConverter.HalfToInt16Bits) => [],
			nameof(BitConverter.HalfToUInt16Bits) => [],
			nameof(BitConverter.Int16BitsToHalf) => [],
			nameof(BitConverter.Int32BitsToSingle) => [],
			nameof(BitConverter.Int64BitsToDouble) => [],
			"IntBitsToReal" => nameof(BitConverter.Int64BitsToDouble),
			"IntRandom" => nameof(IntRandomNumber),
			nameof(IntRandomNumber) => [],
			"IntToReal" => "(double)",
			"IsSummertime" => nameof(DateTime.IsDaylightSavingTime),
			nameof(DateTime.IsDaylightSavingTime) => [],
			nameof(RedStarLinqMath.Max) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Max)),
			"Max3" => [],
			nameof(RedStarLinqMath.Mean) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Mean)),
			"Mean3" => [],
			nameof(RedStarLinqMath.Min) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Min)),
			"Min3" => [],
			"Random" => nameof(RandomNumber),
			nameof(RandomNumber) => [],
			nameof(File.ReadAllBytes) => nameof(File.ReadAllBytesAsync),
			nameof(File.ReadAllLines) => nameof(File.ReadAllLinesAsync),
			nameof(File.ReadAllText) => nameof(File.ReadAllTextAsync),
			nameof(File.ReadLines) => nameof(File.ReadLinesAsync),
			"RealToIntBits" => nameof(BitConverter.DoubleToInt64Bits),
			"RealToUnsignedIntBits" => nameof(BitConverter.DoubleToUInt64Bits),
			nameof(Round) => "(int)" + nameof(Round),
			nameof(BitConverter.SingleToInt32Bits) => [],
			nameof(BitConverter.SingleToUInt32Bits) => [],
			"ToBool" => nameof(BitConverter.ToBoolean),
			nameof(BitConverter.ToBoolean) => [],
			nameof(BitConverter.ToDouble) => [],
			nameof(BitConverter.ToHalf) => [],
			"ToInt" => nameof(BitConverter.ToInt32),
			nameof(BitConverter.ToInt128) => [],
			nameof(BitConverter.ToInt16) => [],
			nameof(BitConverter.ToInt32) => [],
			nameof(BitConverter.ToInt64) => [],
			"ToLongInt" => nameof(BitConverter.ToInt64),
			"ToReal" => nameof(BitConverter.ToDouble),
			"ToShortInt" => nameof(BitConverter.ToInt16),
			nameof(BitConverter.ToSingle) => [],
			nameof(ToString) => nameof(RedStarLinq.ToNString),
			nameof(BitConverter.ToUInt128) => [],
			nameof(BitConverter.ToUInt16) => [],
			nameof(BitConverter.ToUInt32) => [],
			nameof(BitConverter.ToUInt64) => [],
			"ToUnsafeString" => nameof(ToString),
			"ToUnsignedInt" => nameof(BitConverter.ToUInt32),
			"ToUnsignedLongInt" => nameof(BitConverter.ToUInt64),
			"ToUnsignedShortInt" => nameof(BitConverter.ToUInt16),
			nameof(Truncate) => "(int)" + nameof(Truncate),
			nameof(BitConverter.UInt16BitsToHalf) => [],
			nameof(BitConverter.UInt32BitsToSingle) => [],
			nameof(BitConverter.UInt64BitsToDouble) => [],
			"UnsignedIntBitsToReal" => nameof(BitConverter.UInt64BitsToDouble),
			nameof(File.WriteAllBytes) => nameof(File.WriteAllBytesAsync),
			nameof(File.WriteAllLines) => nameof(File.WriteAllLinesAsync),
			nameof(File.WriteAllText) => nameof(File.WriteAllTextAsync),
			_ => function.Copy(),
		};
		if (parameters is null)
			return result;
		if (typeParametersCache is not null)
			result.AddRange(typeParametersCache);
		result.Add('(');
		if (function.AsSpan() is nameof(parameters.RemoveAt)
			or nameof(parameters.RemoveEnd) or nameof(parameters.Reverse) && parameters.Length >= 1
			|| function.AsSpan() is nameof(parameters.GetRange) or nameof(parameters.Remove) && parameters.Length == 2)
			parameters[0].Insert(0, '(').AddRange(") - 1");
		if (function.AsSpan() is nameof(parameters.IndexOf) or nameof(parameters.LastIndexOf)
			or nameof(Grid.SetColumn) or nameof(Grid.SetRow) && parameters.Length >= 2)
			parameters[1].Insert(0, '(').AddRange(") - 1");
		if (function.AsSpan() is nameof(Parallel.For) && parameters.Length >= 2)
			parameters[1].Insert(0, '(').AddRange(") + 1");
		result.AddRange(String.Join(", ", parameters)).Add(')');
		if (function.AsSpan() is nameof(parameters.IndexOf) or nameof(parameters.LastIndexOf))
			result.Insert(0, '(').AddRange(") + 1");
		return result;
	}

	public static String PropertyMapping(String property) => property.ToString() switch
	{
		"UTCNow" => nameof(DateTime.UtcNow),
		nameof(DateTime.UtcNow) => [],
		_ => property.Copy(),
	};
}
