global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using System.Reflection;
global using static NStar.Core.Extents;
global using static System.Math;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.TypeChecks;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using NStar.EasyEvalLib;
using NStar.Mpir;
using NStar.ParallelHS;
using NStar.SortedSets;
using NStar.TreeSets;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PL051.NStar;

public static class TypeConverters
{
	public static readonly ImmutableArray<String> CollectionTypesList = [nameof(Buffer), nameof(Dictionary<,>),
		nameof(FastDelHashSet<>), nameof(FuncDictionary<,>), "HashTable",
		nameof(ICollection), nameof(G.IEnumerable<>), nameof(IList), nameof(IReadOnlyCollection<>), nameof(IReadOnlyList<>),
		nameof(LimitedQueue<>), nameof(G.LinkedList<>), nameof(G.LinkedListNode<>), nameof(ListHashSet<>),
		nameof(Memory<>), nameof(Mirror<,>),
		nameof(Queue<>), nameof(ParallelHashSet<>), nameof(ReadOnlyMemory<>), nameof(ReadOnlySpan<>),
		nameof(Slice<>), nameof(SortedDictionary<,>), nameof(SortedSet<>), nameof(Span<>), nameof(Stack<>),
		nameof(TreeHashSet<>), nameof(TreeSet<>)];
	private static readonly Random random = new();
	private static readonly String DateTime = nameof(DateTime);
	private static readonly Dictionary<Type, bool> memoizedTypes = [];
	private static readonly Dictionary<String, Type> memoizedExtraTypes = [];

	public static NStarType BasicTypeToExtendedType(String mainType, List<String> extraTypes) =>
		(GetBlockStack(mainType), GetBranchCollection(extraTypes));

	public static Type ConstructFuncType(Type returnType)
	{
		if (returnType == typeof(void))
			return typeof(Action);
		return typeof(Func<>).MakeGenericType(returnType);
	}

	public static Type ConstructFuncType(Type returnType, Type paramType)
	{
		if (returnType == typeof(void))
			return typeof(Action<>).MakeGenericType(paramType);
		return typeof(Func<,>).MakeGenericType(paramType, returnType);
	}

	public static Type ConstructFuncType(Type returnType, Slice<Type> netTypes)
	{
		if (returnType == typeof(void))
			return netTypes.Length switch
			{
				0 => typeof(Action),
				1 => typeof(Action<>).MakeGenericType(netTypes[0]),
				2 => typeof(Action<,>).MakeGenericType(netTypes[0], netTypes[1]),
				3 => typeof(Action<,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2]),
				4 => typeof(Action<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3]),
				5 => typeof(Action<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4]),
				6 => typeof(Action<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5]),
				7 => typeof(Action<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5], netTypes[6]),
				8 => typeof(Action<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5], netTypes[6], netTypes[7]),
				_ => throw new InvalidOperationException(),
			};
		return netTypes.Length switch
		{
			0 => typeof(Func<>).MakeGenericType(returnType),
			1 => typeof(Func<,>).MakeGenericType(netTypes[0], returnType),
			2 => typeof(Func<,,>).MakeGenericType(netTypes[0], netTypes[1], returnType),
			3 => typeof(Func<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], returnType),
			4 => typeof(Func<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], returnType),
			5 => typeof(Func<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				returnType),
			6 => typeof(Func<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], returnType),
			7 => typeof(Func<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], netTypes[6], returnType),
			8 => typeof(Func<,,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], netTypes[6], netTypes[7], returnType),
			_ => throw new InvalidOperationException(),
		};
	}

	public static Type ConstructListType(Type netType)
	{
		if (netType == typeof(bool))
			return typeof(BitList);
		else
			return typeof(List<>).MakeGenericType(netType);
	}

	public static Type ConstructTupleType(Slice<Type> netTypes) => netTypes.Length switch
	{
		0 => throw new InvalidOperationException(),
		1 => typeof(ValueTuple<>).MakeGenericType(netTypes[0]),
		2 => typeof(ValueTuple<,>).MakeGenericType(netTypes[0], netTypes[1]),
		3 => typeof(ValueTuple<,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2]),
		4 => typeof(ValueTuple<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3]),
		5 => typeof(ValueTuple<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4]),
		6 => typeof(ValueTuple<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5]),
		7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5], netTypes[6]),
		_ => typeof(ValueTuple<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5], netTypes[6], ConstructTupleType(netTypes[7..])),
	};

	private static BranchCollection GetBranchCollection(List<String> partialBlockStack) =>
		new(partialBlockStack.Convert(x => new TreeBranch("type", 0, []) { Extra
			= new NStarType(new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoBranches) }));

	private static List<(NStarType Type, bool Warning)> GetCompatibleTypes((NStarType Type, bool Warning) source,
		List<(NStarType Type, bool Warning)> blackList)
	{
		List<(NStarType Type, bool Warning)> compatibleTypes = new(16);
		compatibleTypes.AddRange(ImplicitConversionsFromAnything.Convert(x => (x, source.Warning))
			.Filter(x => !blackList.Contains(item: x)));
		if (BuiltInMemberCollections.ImplicitConversions.TryGetValue(source.Type.MainType, out var containerConversions)
			&& containerConversions.TryGetValue(source.Type.ExtraTypes, out var typeConversions))
			compatibleTypes.AddRange(typeConversions.Convert(x => (x.DestType, x.Warning || source.Warning))
				.Filter(x => !blackList.Contains(item: x)));
		return compatibleTypes;
	}

	private static NStarType GetListResultType(BuiltInMemberCollections C, NStarType leftType, NStarType rightType,
		String leftTypeString, String rightTypeString, String leftValue, String rightValue)
	{
		if (CollectionTypesList.Contains(item: leftTypeString) || CollectionTypesList.Contains(item: rightTypeString))
			return GetListType(GetResultType(C, GetSubtype(C, leftType), GetSubtype(C, rightType), leftValue, rightValue));
		else if (leftTypeString == "list")
			return GetListType(GetResultType(C, GetSubtype(C, leftType), (rightTypeString == "list")
				? GetSubtype(C, rightType) : rightType, leftValue, rightValue));
		else
			return GetListType(GetResultType(C, leftType, GetSubtype(C, rightType), leftValue, rightValue));
	}

	private static NStarType GetListSubtype(NStarType type)
	{
		if (type.ExtraTypes.Length == 1
				&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType Subtype)
			return Subtype;
		else if (!(type.ExtraTypes[0].Name != "type" && int.TryParse(type.ExtraTypes[0].Name.ToString(), out var n)))
			return NullType;
		else if (n <= 1 && type.ExtraTypes[1].Name == "type" && type.ExtraTypes[1].Extra is NStarType Subtype2)
			return Subtype2;
		else if (n == 2)
			return GetListType(type.ExtraTypes[1]);
		else
			return (ListBlockStack, new BranchCollection { new((n - 1).ToString(), 0, []), type.ExtraTypes[1] });
	}

	private static String GetPrimitiveResultType(String leftType, String rightType, String leftValue, String rightValue)
	{
		if (leftType == BoolTypeName && IsNumeric(rightType.AsSpan()))
			leftValue.Insert(0, '(').AddRange(" ? 1 : 0)");
		else if (rightType == BoolTypeName && IsNumeric(leftType.AsSpan()))
			rightValue.Insert(0, '(').AddRange(" ? 1 : 0)");
		string t;
		if (leftType == (t = "dynamic") || rightType == t)
			return t;
		else if (leftType == (t = StringTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongDeccomplexTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongComplexTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName or LongDecimalTypeName
				|| rightType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName or LongDecimalTypeName)
				return LongDeccomplexTypeName;
			else
				return t;
		}
		else if (leftType == (t = DeccomplexTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName
				or RealTypeName or UnsignedLongRealTypeName or UnsignedLongDecimalTypeName
				or LongRealTypeName or LongDecimalTypeName or ComplexTypeName
				|| rightType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName
				or RealTypeName or UnsignedLongRealTypeName or UnsignedLongDecimalTypeName
				or LongRealTypeName or LongDecimalTypeName or ComplexTypeName)
				return LongDeccomplexTypeName;
			else
				return t;
		}
		else if (leftType == (t = ComplexTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName or LongDecimalTypeName
				|| rightType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName or LongDecimalTypeName)
				return LongDeccomplexTypeName;
			else if (leftType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName
				or UnsignedLongRealTypeName or LongRealTypeName
				|| rightType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName
				or UnsignedLongRealTypeName or LongRealTypeName)
				return LongComplexTypeName;
			else
				return t;
		}
		else if (leftType == (t = LongDecimalTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongRealTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName
				|| rightType.AsSpan() is DecimalTypeName or UnsignedLongDecimalTypeName)
				return LongDecimalTypeName;
			else
				return t;
		}
		else if (leftType == (t = UnsignedLongDecimalTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName
				or RealTypeName or DecimalTypeName or UnsignedLongRealTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName
				or RealTypeName or DecimalTypeName or UnsignedLongRealTypeName)
				return LongDecimalTypeName;
			else
				return t;
		}
		else if (leftType == (t = UnsignedLongRealTypeName) || rightType == t)
		{
			if (leftType == DecimalTypeName || rightType == DecimalTypeName)
				return LongDecimalTypeName;
			else if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName
				or RealTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or LongLongTypeName
				or RealTypeName)
				return LongRealTypeName;
			else
				return t;
		}
		else if (leftType == (t = DecimalTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName or RealTypeName
				|| rightType.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName or RealTypeName)
				return LongDecimalTypeName;
			else
				return t;
		}
		else if (leftType == (t = RealTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is LongIntTypeName or UnsignedLongIntTypeName
				or LongLongTypeName or UnsignedLongLongTypeName
				|| rightType.AsSpan() is LongIntTypeName or UnsignedLongIntTypeName
				or LongLongTypeName or UnsignedLongLongTypeName)
				return LongRealTypeName;
			else
				return t;
		}
		else if (leftType == (t = LongLongTypeName) || rightType == t)
			return t;
		else if (leftType == (t = UnsignedLongLongTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName)
				return LongLongTypeName;
			else
				return t;
		}
		else if (leftType == (t = UnsignedLongIntTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or nameof(DateTime) or "TimeSpan"
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName or LongIntTypeName or nameof(DateTime) or "TimeSpan")
				return LongLongTypeName;
			else
				return t;
		}
		else if (leftType == (t = "TimeSpan") || rightType == t
			|| leftType == (t = nameof(DateTime)) || rightType == t)
			return t;
		else if (leftType == (t = nameof(DateTime)) || rightType == t)
			return t;
		else if (leftType == (t = LongIntTypeName) || rightType == t)
			return t;
		else if (leftType == (t = LongCharTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return t;
		}
		else if (leftType == (t = UnsignedIntTypeName) || rightType == t)
		{
			if (leftType.AsSpan() is ShortIntTypeName or IntTypeName
				|| rightType.AsSpan() is ShortIntTypeName or IntTypeName)
				return LongIntTypeName;
			else
				return t;
		}
		else if (leftType == (t = IntTypeName) || rightType == t)
			return t;
		else if (leftType == (t = CharTypeName) || rightType == t)
		{
			if (leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return t;
		}
		else if (leftType == (t = UnsignedShortIntTypeName) || rightType == t)
		{
			if (leftType == ShortIntTypeName || rightType == ShortIntTypeName)
				return IntTypeName;
			else
				return t;
		}
		else if (leftType == (t = ShortIntTypeName) || rightType == t)
			return t;
		else if (leftType == (t = ShortCharTypeName) || rightType == t)
			return t;
		else if (leftType == (t = ByteTypeName) || rightType == t)
			return t;
		else if (leftType == (t = BoolTypeName) || rightType == t)
			return t;
		else if (leftType == (t = "BaseClass") || rightType == t)
			return t;
		else
			return NullString;
	}

	public static List<(Type ExtraType, Type TypeToInsert)> GetReplacementPatterns(Type[] genericArguments,
		Type[] parameterTypes)
	{
		var length = Min(genericArguments.Length, parameterTypes.Length);
		List<(Type ExtraType, Type TypeToInsert)> result = [];
		for (var i = 0; i < length; i++)
		{
			var genericArgument = genericArguments[i];
			var parameterType = parameterTypes[i];
			if (parameterType == typeof(String))
			{
				result.Add((genericArgument, /*typeof(G.IEnumerable<>).IsAssignableFrom(genericArgument)
					? */typeof(char)/* : typeof(String)*/));
				continue;
			}
			if (!parameterType.IsGenericType)
			{
				if (parameterType != typeof(void))
					result.Add((genericArgument, parameterType));
				continue;
			}
			var parameterGenericArguments = parameterType.GetGenericTypeDefinition().GetGenericArguments();
			var index = parameterGenericArguments.FindIndex(x => x.Name == genericArgument.Name);
			if (index != -1)
			{
				result.Add((genericArgument, parameterType.GetGenericArguments()[index]));
				continue;
			}
			result.AddRange(GetReplacementPatterns(genericArgument.GetGenericArguments(),
				parameterType.GetGenericArguments()));
		}
		return result;
	}

	public static NStarType GetResultType(BuiltInMemberCollections C, NStarType leftType, NStarType rightType,
		String leftValue, String rightValue)
	{
		try
		{
			if (leftType.Equals(rightType) && !leftType.Equals(GetPrimitiveType(DateTime)))
				return leftType;
			if (TypeIsPrimitive(leftType.MainType) && TypeIsPrimitive(rightType.MainType))
			{
				var leftTypeName = leftType.MainType.Peek().Name;
				var rightTypeName = rightType.MainType.Peek().Name;
				if (leftType.ExtraTypes.Length == 0 && rightType.ExtraTypes.Length == 0)
					return GetPrimitiveType(GetPrimitiveResultType(leftTypeName, rightTypeName, leftValue, rightValue));
				else if (leftTypeName == "list" || rightTypeName == "list")
					return GetListResultType(C, leftType, rightType, leftTypeName, rightTypeName, leftValue, rightValue);
				else
					return NullType;
			}
			else
				return NullType;
		}
		catch (StackOverflowException)
		{
			return NullType;
		}
	}

	public static NStarType GetSubtype(BuiltInMemberCollections C, NStarType type, int levels = 1)
	{
		if (levels <= 0)
			return type;
		else if (levels == 1)
		{
			if (type.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive)
			{
				if (block.Name == "list")
					return GetListSubtype(type);
				else if (block.Name == "range")
					return IntType;
				else if (block.Name == TupleName
					&& (type.ExtraTypes.AllEqual() || type.ExtraTypes.Length == 2
					&& type.ExtraTypes[1].Length == 0 && int.TryParse(type.ExtraTypes[1].Name.AsSpan(), out _))
					&& type.ExtraTypes[0].Extra is NStarType Subtype)
					return Subtype;
				else
					return NullType;
			}
			else if (type.ExtraTypes.Length == 1 && TypesAreCompatible(C, type, new(IEnumerableBlockStack, type.ExtraTypes),
				out var warning, null, out _, out _) && !warning
				&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType Subtype)
				return Subtype;
			else if (type.Equals(ChainType))
				return IntType;
			else
				return NullType;
		}
		else
		{
			var t = type;
			for (var i = 0; i < levels; i++)
				t = GetSubtype(C, t);
			return t;
		}
	}

	private static (int Depth, NStarType LeafType) GetTypeDepthAndLeafType(NStarType type)
	{
		var Depth = 0;
		var LeafType = type;
		while (true)
		{
			if (TypeEqualsToPrimitive(LeafType, "list", false))
			{
				if (LeafType.ExtraTypes.Length == 1
					&& LeafType.ExtraTypes[0].Name == "type" && LeafType.ExtraTypes[0].Extra is NStarType Subtype)
				{
					Depth++;
					LeafType = Subtype;
				}
				else if (LeafType.ExtraTypes[0].Name != "type"
					&& int.TryParse(LeafType.ExtraTypes[0].Name.ToString(), out var n)
					&& LeafType.ExtraTypes[1].Name == "type" && LeafType.ExtraTypes[1].Extra is NStarType Subtype2)
				{
					Depth += n;
					LeafType = Subtype2;
				}
				else if (LeafType.ExtraTypes[1].Name == "type" && LeafType.ExtraTypes[1].Extra is NStarType Subtype3)
				{
					Depth++;
					LeafType = Subtype3;
				}
				else
					return (Depth, LeafType);
			}
			else if (LeafType.MainType.TryPeek(out var block)
				&& block.BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
				&& CollectionTypesList.Contains(item: LeafType.MainType.ToString().ToNString().GetAfterLast("."))
					&& LeafType.ExtraTypes[^1].Name == "type" && LeafType.ExtraTypes[^1].Extra is NStarType Subtype)
			{
				Depth++;
				LeafType = Subtype;
			}
			else if (LeafType.Equals(ChainType))
				return (Depth + 1, IntType);
			else
				return (Depth, LeafType);
		}
	}

	public static bool IsAssignableFromExt(this Type destination, Type source) =>
		destination.IsAssignableFrom(source) || destination == typeof(MpzT) && new[] { typeof(byte), typeof(short),
		typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(MpzT) }.Contains(source)
		|| destination == typeof(long) && new[] { typeof(byte),
		typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long) }.Contains(source)
		|| destination == typeof(int) && new[] { typeof(byte), typeof(short), typeof(ushort), typeof(int) }.Contains(source)
		|| destination == typeof(short) && new[] { typeof(byte), typeof(short) }.Contains(source)
		|| destination == typeof(ulong) && new[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) }.Contains(source)
		|| destination == typeof(uint) && new[] { typeof(byte), typeof(ushort), typeof(uint) }.Contains(source)
		|| destination == typeof(ushort) && new[] { typeof(byte), typeof(ushort) }.Contains(source);

	public static bool IsUnmanaged(this Type netType)
	{
		if (!memoizedTypes.TryGetValue(netType, out var answer))
		{
			if (!netType.IsValueType)
				answer = false;
			else if (netType.IsPrimitive || netType.IsPointer || netType.IsEnum)
				answer = true;
			else
				answer = netType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.All(f => IsUnmanaged(f.FieldType));
			memoizedTypes[netType] = answer;
		}
		return answer;
	}

	public static dynamic ListWithSingle<T>(T item)
	{
		if (item is bool b)
			return new BitList([b]);
		else
			return new List<T>(item);
	}

	public static List<char> RandomVarName() => RedStarLinq.Fill(32, _ => (char)(random.Next(2) == 1
		? random.Next('A', 'Z' + 1) : random.Next('a', 'z' + 1)));

	public static Type ReplaceExtraNetType(Type originalType, (Type ExtraType, Type TypeToInsert) pattern)
	{
		if (originalType.Name.Equals(pattern.ExtraType.Name))
			return pattern.ExtraType.IsGenericMethodParameter || pattern.ExtraType.IsGenericParameter
				? pattern.TypeToInsert : pattern.ExtraType;
		else
		{
			var genericArguments = originalType.GetGenericArguments();
			if (genericArguments.Length == 0)
				return originalType;
			return originalType.GetGenericTypeDefinition().MakeGenericType([.. genericArguments.Convert(x =>
				ReplaceExtraNetType(x, pattern))]);
		}
	}

	public static NStarType ReplaceExtraType(NStarType originalType, (String ExtraType, NStarType TypeToInsert) pattern)
	{
		if (originalType.MainType.Length == 1 && originalType.MainType.Peek().BlockType == BlockType.Extra
			&& originalType.MainType.Peek().Name == pattern.ExtraType && originalType.ExtraTypes.Length == 0)
			return pattern.TypeToInsert;
		else
		{
			return new(originalType.MainType, new(originalType.ExtraTypes
				.ToList((x, index) => new G.KeyValuePair<String?, TreeBranch>(originalType.ExtraTypes.Keys
				.TryGetKey(index, out var key) ? key : null, x.Name != "type"
				|| x.Extra is not NStarType InnerNStarType ? new TreeBranch(x.Name, x.Pos, x.Container)
				: new TreeBranch("type", x.Pos, x.Container)
				{
					Extra = ReplaceExtraType(InnerNStarType, pattern)
				}))));
		}
	}

	public static Type TypeMapping(BuiltInMemberCollections C, NStarType NStarType)
	{
		if (TypeEqualsToPrimitive(NStarType, "list", false))
		{
			if (NStarType.ExtraTypes.Length == 1)
			{
				if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				var netType = TypeMapping(C, InnerNStarType);
				return ConstructListType(netType);
			}
			else
			{
				if (NStarType.ExtraTypes[0].Name == "type"
					|| !int.TryParse(NStarType.ExtraTypes[0].Name.ToString(), out var levelsCount) || levelsCount < 1
					|| NStarType.ExtraTypes[^1].Name != "type"
					|| NStarType.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				var netType = TypeMapping(C, InnerNStarType);
				Type outputType;
				if (netType == typeof(bool))
					outputType = typeof(BitList);
				else
					outputType = typeof(List<>).MakeGenericType(netType);
				for (var i = 1; i < levelsCount; i++)
					outputType = typeof(List<>).MakeGenericType(outputType);
				return outputType;
			}
		}
		if (NStarType.MainType.Equals(FuncBlockStack))
		{
			List<Type> funcComponents = [];
			if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType)
				throw new InvalidOperationException();
			var returnType = TypeMapping(C, InnerNStarType);
			for (var i = 1; i < NStarType.ExtraTypes.Length; i++)
			{
				if (NStarType.ExtraTypes[i].Name != "type" || NStarType.ExtraTypes[i].Extra is not NStarType InnerNStarType2)
					throw new InvalidOperationException();
				funcComponents.Add(TypeMapping(C, InnerNStarType2));
			}
			return ConstructFuncType(returnType, funcComponents.GetSlice());
		}
		if (!TypeEqualsToPrimitive(NStarType, TupleName, false))
		{
			var split = SplitType(NStarType.MainType);
			if (C.TypeExists(split, out var netType))
			{
				if (netType == typeof(Task<>)
					&& (NStarType.ExtraTypes.Length == 0
					|| NStarType.ExtraTypes.Length == 1 && NStarType.ExtraTypes[0].Name == "type"
					&& NStarType.ExtraTypes[0].Extra is NStarType InnerNStarType && InnerNStarType.Equals(NullType)))
					return typeof(Task);
				else if (netType == typeof(ValueTask<>)
					&& (NStarType.ExtraTypes.Length == 0
					|| NStarType.ExtraTypes.Length == 1 && NStarType.ExtraTypes[0].Name == "type"
					&& NStarType.ExtraTypes[0].Extra is NStarType ValueInnerNStarType && ValueInnerNStarType.Equals(NullType)))
					return typeof(ValueTask);
				else if (netType.ContainsGenericParameters)
					return netType.MakeGenericType(NStarType.ExtraTypes.ToArray(x => TypeMapping(C, x)));
				else
					return netType;
			}
			else if (Interfaces.TryGetValue((split.Container.ToString(), split.Type), out var @interface))
			{
				netType = @interface.DotNetType;
				if (netType.ContainsGenericParameters)
					return netType.MakeGenericType(NStarType.ExtraTypes.ToArray(x => TypeMapping(C, x)));
				else
					return netType;
			}
			else if (NStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra)
			{
				if (memoizedExtraTypes.TryGetValue(block.Name, out var memoized))
					return memoized;
				var assembly = EasyEval.CompileAndGetAssembly("class C<" + block.Name + ">{}class P{static void Main(){}}",
					[], out var errors);
				if (assembly is null || errors != "Compilation done without any error.\r\n")
					throw new InvalidOperationException();
				return memoizedExtraTypes[block.Name] = assembly.DefinedTypes.First().GetGenericArguments().First();
			}
			else
				throw new InvalidOperationException();
		}
		if (NStarType.ExtraTypes.Length == 0)
			return typeof(void);
		List<Type> tupleComponents = [];
		if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType3)
			throw new InvalidOperationException();
		var first = TypeMapping(C, InnerNStarType3);
		if (NStarType.ExtraTypes.Length == 1)
			return first;
		var innerResult = first;
		if (NStarType.ExtraTypes.Length == 2 && NStarType.ExtraTypes[1].Length == 0
			&& int.TryParse(NStarType.ExtraTypes[1].Name.AsSpan(), out var tupleLength))
			return ConstructTupleType(RedStarLinq.FillArray(innerResult, tupleLength).GetSlice());
		for (var i = 1; i < NStarType.ExtraTypes.Length; i++)
		{
			if (NStarType.ExtraTypes[i].Name == "type" && NStarType.ExtraTypes[i].Extra is NStarType InnerNStarType2)
			{
				tupleComponents.Add(innerResult);
				first = TypeMapping(C, InnerNStarType2);
				innerResult = first;
				continue;
			}
			innerResult = ConstructTupleType(RedStarLinq.FillArray(innerResult,
				int.TryParse(NStarType.ExtraTypes[i].Name.ToString(), out var n) ? n : 1).GetSlice());
		}
		return ConstructTupleType(tupleComponents.Add(innerResult).GetSlice());
	}

	private static Type TypeMapping(BuiltInMemberCollections C, TreeBranch branch)
	{
		if (branch.Name != "type" || branch.Extra is not NStarType NStarType)
			throw new InvalidOperationException();
		return TypeMapping(C, NStarType);
	}

	public static NStarType TypeMappingBack(Type netType, Type[] genericArguments, BranchCollection extraTypes,
		Type[] netTypes)
	{
		if (netType.IsGenericParameter)
		{
			var genericArgumentsIndex = Array.IndexOf(genericArguments, netType);
			if (genericArgumentsIndex < 0 || extraTypes.Length <= genericArgumentsIndex)
				return new(new(new Block(BlockType.Extra, netType.Name, 1)), []);
			else if (extraTypes[genericArgumentsIndex].Name != "type"
				|| extraTypes[genericArgumentsIndex].Extra is not NStarType InnerNStarType)
				throw new InvalidOperationException();
			else
				return InnerNStarType;
		}
		if (netType.IsSZArray || netType.IsPointer)
			netType = typeof(List<>).MakeGenericType(netType.GetElementType() ?? throw new InvalidOperationException());
		else if (netType == typeof(System.Collections.BitArray))
			netType = typeof(BitList);
		else if (netType == typeof(Index))
			netType = typeof(int);
		var typeGenericArguments = netType.GetGenericArguments();
		if (netType.Name.Contains("Action"))
		{
			return new(FuncBlockStack, new([new TreeBranch("type", 0, []) { Extra = NullType },
				.. typeGenericArguments.Convert((x, index) =>
				new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes, netTypes) })]));
		}
		if (netType.Name.Contains(nameof(Func<>)))
		{
			return new(FuncBlockStack, new([typeGenericArguments[^1].Wrap(x =>
				new TreeBranch("type", 0, [])
				{
					Extra = TypeMappingBack(x.GetGenericArguments().Length != 0
					&& x.GetGenericTypeDefinition() == typeof(ValueTask<>)
					? x.GetGenericArguments()[0] : x, genericArguments, extraTypes, netTypes)
				}), .. typeGenericArguments.GetSlice(..^1).Convert((x, index) =>
				new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes, netTypes) })]));
		}
		if (netType.FullName is not null && netType.GetGenericArguments().Length == 1
			&& typeof(G.IEnumerable<>).MakeGenericType(netType.GetGenericArguments()).IsAssignableFrom(netType)
			&& extraTypes.Length == 2 && extraTypes[0].Name != "type" && extraTypes[0].Extra is null
			&& int.TryParse(extraTypes[0].Name.AsSpan(), out var n))
			return new(ListBlockStack, extraTypes);
		int foundIndex;
		if ((foundIndex = genericArguments.FindIndex(x => x.Name == netType.Name)) >= 0)
			netType = netTypes[foundIndex];
		List<Type> innerTypes = [];
		foreach (var genericArgument in netType.GenericTypeArguments)
		{
			if ((foundIndex = genericArguments.IndexOf(genericArgument)) < 0)
				continue;
			innerTypes.Add(netTypes[foundIndex]);
		}
		var oldNetType = netType;
		if (netType.IsGenericType)
			netType = netType.GetGenericTypeDefinition();
		while (true)
		{
			if (CreateVar(PrimitiveTypes.Find(x => x.Value == netType).Key, out var typename) is not null)
				return typename == "list" ? GetListType(TypeMappingBack(typeGenericArguments[0], genericArguments,
					new(extraTypes.TakeLast(genericArguments.Length)), netTypes))
					: GetPrimitiveType(typename);
			else if (netType == typeof(Task))
				return new(TaskBlockStack, [new("type", 0, []) { Extra = NullType }]);
			else if (netType == typeof(ValueTask))
				return new(ValueTaskBlockStack, [new("type", 0, []) { Extra = NullType }]);
			else if (ExtraTypes.TryGetKey(netType, out var type2) || ImportedTypes.TryGetKey(netType, out type2)
				|| IOTypes.TryGetKey(netType, out type2))
				return new(GetBlockStack(type2.Namespace + "." + type2.Type),
					new([.. typeGenericArguments.Convert((x, index) =>
					new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes, netTypes) })]));
			else if (CreateVar(Interfaces.Find(x => x.Value.DotNetType == netType), out var type3).Key != default)
				return new(GetBlockStack(type3.Key.Namespace + "." + type3.Key.Interface),
					new([.. typeGenericArguments.Convert((x, index) =>
					new TreeBranch("type", 0,[]) { Extra = TypeMappingBack(x, genericArguments, extraTypes, netTypes) })]));
			else if (netType == typeof(string))
				return StringType;
			else if (netType == typeof(BitList))
				return GetListType(BoolType);
			else if (innerTypes.Length != 0)
			{
				netType = netType.MakeGenericType([.. innerTypes]);
				if (!netType.Name.Contains("Tuple") && !netType.Name.Contains("KeyValuePair"))
				{
					innerTypes.Clear();
					continue;
				}
				return new(TupleBlockStack, new(netType.GenericTypeArguments.ToList(x =>
					new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes, netTypes) })));
			}
			else if (!typeof(ITuple).IsAssignableFrom(oldNetType))
				throw new InvalidOperationException();
			break;
		}
		BranchCollection result = [];
		var tupleTypes = new Queue<Type>();
		tupleTypes.Enqueue(oldNetType);
		while (tupleTypes.Length != 0 && tupleTypes.Dequeue() is Type tupleType)
			foreach (var field in tupleType.GetFields())
			{
				if (field.Name == "Rest")
				{
					tupleTypes.Enqueue(tupleType);
					continue;
				}
				result.Add(new("type", 0, [])
				{
					Extra = TypeMappingBack(field.FieldType, genericArguments, extraTypes, netTypes)
				});
			}
		return new(TupleBlockStack, result);
	}

	public static bool TypesAreCompatible(BuiltInMemberCollections C, NStarType sourceType, NStarType destinationType,
		out bool warning, String? srcExpr, out String? destExpr, out String? extraMessage)
	{
		warning = false;
		extraMessage = null;
		while (TypeEqualsToPrimitive(sourceType, TupleName, false) && sourceType.ExtraTypes.Length == 1
			&& sourceType.ExtraTypes[0].Name == "type" && sourceType.ExtraTypes[0].Extra is NStarType SourceSubtype)
			sourceType = SourceSubtype;
		while (TypeEqualsToPrimitive(destinationType, TupleName, false) && destinationType.ExtraTypes.Length == 1
			&& destinationType.ExtraTypes[0].Name == "type"
			&& destinationType.ExtraTypes[0].Extra is NStarType DestinationSubtype)
			destinationType = DestinationSubtype;
		if (C.IsEqualOrDerived(sourceType, destinationType) && (sourceType.ExtraTypes, destinationType.ExtraTypes).Combine()
			.All(x => x.Item1.Equals(x.Item2)))
		{
			destExpr = srcExpr;
			return true;
		}
		if (sourceType.Equals(StringType) && destinationType.Equals(UnsafeStringType))
		{
			destExpr = srcExpr?.Insert(0, '(').AddRange(").ToString()");
			return true;
		}
		if (sourceType.Equals(UnsafeStringType) && destinationType.Equals(StringType))
		{
			destExpr = srcExpr?.Insert(0, "((String)").Add(')');
			return true;
		}
		if (TypeEqualsToPrimitive(sourceType, NullString, false))
		{
			destExpr = DefaultNull;
			return true;
		}
		if (ImplicitConversionsFromAnything.Contains(destinationType))
		{
			if (srcExpr is null)
				destExpr = null;
			else if (TypeEqualsToPrimitive(destinationType, StringTypeName))
				destExpr = ((String)"(").AddRange(srcExpr).AddRange(").ToString()");
			else if (TypeEqualsToPrimitive(destinationType, "list", false))
				destExpr = ((String)nameof(ListWithSingle)).Add('(').AddRange(srcExpr).Add(')');
			else
				destExpr = srcExpr;
			return true;
		}
		if (TypeEqualsToPrimitive(destinationType, TupleName, false))
		{
			if (!TypeEqualsToPrimitive(sourceType, TupleName, false))
			{
				destExpr = DefaultNull;
				return false;
			}
			if (sourceType.ExtraTypes.Length >= 2 && sourceType.ExtraTypes.AllEqual())
			{
				sourceType.ExtraTypes[1].Replace(new(sourceType.ExtraTypes.Length.ToString(),
					sourceType.ExtraTypes[1].Pos, sourceType.ExtraTypes[1].Container));
				sourceType.ExtraTypes.RemoveEnd(2);
			}
			if (destinationType.ExtraTypes.Length >= 2 && destinationType.ExtraTypes.AllEqual())
			{
				destinationType.ExtraTypes[1].Replace(new(destinationType.ExtraTypes.Length.ToString(),
					destinationType.ExtraTypes[1].Pos, destinationType.ExtraTypes[1].Container));
				destinationType.ExtraTypes.RemoveEnd(2);
			}
			if (sourceType.ExtraTypes.Length != destinationType.ExtraTypes.Length
				&& sourceType.ExtraTypes.All(x => x.Name == "type") && destinationType.ExtraTypes.All(x => x.Name == "type"))
			{
				destExpr = DefaultNull;
				return false;
			}
			destExpr = srcExpr;
			return sourceType.ExtraTypes.Combine(destinationType.ExtraTypes)
				.All(x => x.Item1.Name == "type" && x.Item1.Extra is NStarType LeftType
				&& x.Item2.Name == "type" && x.Item2.Extra is NStarType RightType
				&& TypesAreCompatible(C, LeftType, RightType, out var innerWarning, null, out _, out _) && !innerWarning
				|| x.Item1.Length == 0 && int.TryParse(x.Item1.Name.AsSpan(), out var leftLength)
				&& x.Item2.Length == 0 && int.TryParse(x.Item2.Name.AsSpan(), out var rightLength)
				&& leftLength == rightLength)
				|| destinationType.ExtraTypes.Length == 2
				&& destinationType.ExtraTypes[0].Name == "type" && destinationType.ExtraTypes[0].Extra is NStarType RightType
				&& destinationType.ExtraTypes[1].Length == 0
				&& int.TryParse(destinationType.ExtraTypes[1].Name.AsSpan(), out var rightLength)
				&& sourceType.ExtraTypes.All(x => x.Name == "type" && x.Extra is NStarType LeftType
				&& TypesAreCompatible(C, LeftType, RightType, out var innerWarning, null, out _, out _) && !innerWarning);
		}
		var destinationTypeString = destinationType.MainType.ToString();
		if (TypeEqualsToPrimitive(destinationType, "list", false) || destinationType.MainType.Length != 0
			&& destinationType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
			&& CollectionTypesList.Contains(item: destinationTypeString.ToNString().GetAfterLast(".")))
		{
			if (TypeEqualsToPrimitive(sourceType, TupleName, false))
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
				else if (!sourceType.ExtraTypes.All(x => x.Name == "type" && x.Extra is NStarType ValueType
					&& TypesAreCompatible(C, ValueType, subtype, out var innerWarning, null, out _, out _) && !innerWarning))
				{
					destExpr = DefaultNull;
					return false;
				}
				else
				{
					destExpr = srcExpr;
					return true;
				}
			}
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			if (SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, StringTypeName))
			{
				if (srcExpr is null)
					destExpr = null;
				else if (DestinationDepth == 0)
					destExpr = (String?)((String)"(").AddRange(srcExpr).AddRange(").ToString()");
				else
					destExpr = (String?)srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth
				&& TypesAreCompatible(C, SourceLeafType, DestinationLeafType, out warning, null, out _, out _) && !warning)
			{
				var toInsert = ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(')
					.Repeat(DestinationDepth - SourceDepth);
				if (srcExpr is null)
				{
					destExpr = null;
					return true;
				}
				else if (!SourceLeafType.Equals(DestinationLeafType) && TypeIsPrimitive(SourceLeafType.MainType)
					&& TypeIsPrimitive(DestinationLeafType.MainType) && SourceLeafType.MainType.Peek().Name != StringTypeName
					&& DestinationLeafType.MainType.Peek().Name != StringTypeName)
				{
					srcExpr.Replace(AdaptTerminalType(srcExpr, SourceLeafType, DestinationLeafType));
					srcExpr.Insert(0, toInsert);
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				else
				{
					srcExpr.Insert(0, toInsert);
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				if (destinationTypeString is "System."
					+ nameof(ReadOnlySpan<>) or "System." + nameof(Span<>))
					srcExpr.Add('.').AddRange(nameof(srcExpr.AsSpan)).AddRange("()");
				if (destinationTypeString is "System.Unsafe."
					+ nameof(ReadOnlyMemory<>) or "System.Unsafe." + nameof(Memory<>))
					srcExpr.Add('.').AddRange(nameof(srcExpr.AsMemory)).AddRange("()");
				return true;
			}
			else if (SourceDepth <= DestinationDepth + 1
				&& (SourceLeafType.Equals(StringType) || SourceLeafType.Equals(UnsafeStringType))
				&& DestinationLeafType.Equals(CharType)
				|| SourceDepth <= DestinationDepth + 1 && SourceLeafType.Equals(RangeType)
				&& DestinationLeafType.Equals(IntType) && TypesAreCompatible(C, ChainType,
				new(IEnumerableBlockStack, [new("type", 0, []) { Extra = DestinationLeafType }]), out warning,
				srcExpr?.Insert(0, nameof(Chain) + '(').Add(')'), out destExpr, out extraMessage))
			{
				var toInsert = ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(')
					.Repeat(DestinationDepth - SourceDepth - 1);
				if (srcExpr is null)
					destExpr = null;
				else
				{
					srcExpr.Insert(0, toInsert);
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth - 1));
					destExpr = srcExpr;
				}
				return true;
			}
			else
			{
				destExpr = DefaultNull;
				return false;
			}
		}
		if (sourceType.MainType.Equals(FuncBlockStack) && sourceType.ExtraTypes.Length == 3
			&& destinationType.MainType.Equals(EventHandlerBlockStack) && destinationType.ExtraTypes.Length == 1
			&& sourceType.ExtraTypes[0].Length == 0 && sourceType.ExtraTypes[0].Name == "type"
			&& (sourceType.ExtraTypes[0].Extra?.Equals(NullType) ?? false)
			&& sourceType.ExtraTypes[1].Length == 0 && sourceType.ExtraTypes[1].Name == "type"
			&& (sourceType.ExtraTypes[1].Extra?.Equals(ObjectType) ?? false)
			&& sourceType.ExtraTypes[2].Length == 0 && sourceType.ExtraTypes[2].Name == "type"
			&& destinationType.ExtraTypes[0].Length == 0 && destinationType.ExtraTypes[0].Name == "type"
			&& (sourceType.ExtraTypes[2].Extra?.Equals(destinationType.ExtraTypes[0].Extra) ?? false))
		{
			destExpr = srcExpr;
			return true;
		}
		if (sourceType.MainType.Equals(FuncBlockStack) && destinationType.MainType.Equals(FuncBlockStack))
		{
			destExpr = srcExpr;
			try
			{
				var warning2 = false;
				if (!(sourceType.ExtraTypes.Length >= destinationType.ExtraTypes.Length
					&& destinationType.ExtraTypes.Length >= 1
					&& sourceType.ExtraTypes[0].Name == "type" && sourceType.ExtraTypes[0].Extra is NStarType SourceSubtype
					&& destinationType.ExtraTypes[0].Name == "type"
					&& destinationType.ExtraTypes[0].Extra is NStarType DestinationSubtype
					&& TypesAreCompatible(C, SourceSubtype, DestinationSubtype,
					out warning, null, out _, out _)))
					return false;
				if (destinationType.ExtraTypes.Skip(1).Combine(sourceType.ExtraTypes.Skip(1), (x, y) =>
				{
					var warning3 = false;
					var b = x.Name == "type" && x.Extra is NStarType LeftType
					&& y.Name == "type" && y.Extra is NStarType RightType
					&& TypesAreCompatible(C, LeftType, RightType, out warning3, null, out _, out _);
					warning2 |= warning3;
					return b;
				}).All(x => x))
				{
					warning |= warning2;
					return true;
				}
				else
					return false;
			}
			catch (StackOverflowException)
			{
				return false;
			}
		}
		if (destinationTypeString is "System." + nameof(ReadOnlySpan<>) or "System." + nameof(Span<>))
		{
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			if (SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, StringTypeName))
			{
				if (srcExpr is null)
					destExpr = null;
				else if (DestinationDepth == 0)
					destExpr = (String?)((String)"(").AddRange(srcExpr).AddRange(").ToString()");
				else
					destExpr = (String?)srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth && TypesAreCompatible(C, SourceLeafType, DestinationLeafType,
				out warning, null, out _, out _) && !warning)
			{
				destExpr = srcExpr ?? null;
				return true;
			}
			else
			{
				destExpr = DefaultNull;
				return false;
			}
		}
		if (sourceType.Equals(RangeType) && destinationType.Equals(ChainType))
		{
			destExpr = srcExpr?.Insert(0, nameof(Chain) + '(').Add(')');
			return true;
		}
		if (TaskBlockStacks.Contains(destinationType.MainType) && destinationType.ExtraTypes.Length == 1
			&& destinationType.ExtraTypes[0].Name == "type" && destinationType.ExtraTypes[0].Extra is NStarType TaskNStarType
			&& (TaskNStarType.Equals(sourceType) || sourceType.MainType.Equals(EmptyTaskBlockStack)))
		{
			destExpr = srcExpr;
			return true;
		}
		if (C.UserDefinedTypes.TryGetValue(SplitType(sourceType.MainType), out var userDefinedType)
			&& userDefinedType.BaseType != NullType && TypesAreCompatible(C, userDefinedType.BaseType, destinationType,
			out warning, srcExpr, out destExpr, out extraMessage))
			return true;
		if (!BuiltInMemberCollections.ImplicitConversions.TryGetValue(sourceType.MainType, out var containerConversions))
		{
			destExpr = DefaultNull;
			return false;
		}
		if (!containerConversions.TryGetValue(sourceType.ExtraTypes, out var typeConversions))
		{
			destExpr = DefaultNull;
			return false;
		}
		var foundIndex = typeConversions.FindIndex(x => x.DestType.Equals(destinationType));
		if (foundIndex != -1)
		{
			warning = typeConversions[foundIndex].Warning;
			if (srcExpr is null)
				destExpr = null;
			else if (!(warning || sourceType.Equals(BoolType)
				|| destinationType.MainType.Length == 1 && destinationType.ExtraTypes.Length == 0
				&& destinationType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Primitive
				&& block.Name.AsSpan() is LongLongTypeName or UnsignedLongLongTypeName))
				destExpr = srcExpr;
			else
				destExpr = AdaptTerminalType(srcExpr, sourceType, destinationType);
			return true;
		}
		List<(NStarType Type, bool Warning)> types_list = [(sourceType, false)];
		List<(NStarType Type, bool Warning)> new_types_list = [(sourceType, false)];
		while (true)
		{
			List<(NStarType Type, bool Warning)> new_types2_list = new(16);
			for (var i = 0; i < new_types_list.Length; i++)
			{
				var new_types3_list = GetCompatibleTypes(new_types_list[i], types_list);
				foundIndex = new_types3_list.FindIndex(x => x.Type.Equals(destinationType));
				if (foundIndex == -1)
				{
					new_types2_list.AddRange(new_types3_list);
					continue;
				}
				warning = new_types3_list[foundIndex].Warning;
				if (srcExpr is null)
					destExpr = null;
				else if (!warning)
					destExpr = srcExpr;
				else
					destExpr = AdaptTerminalType(srcExpr, sourceType, destinationType);
				return true;
			}
			new_types_list = [.. new_types2_list];
			types_list.AddRange(new_types2_list);
			if (new_types2_list.Length == 0)
				break;
		}
		destExpr = null;
		return false;
	}

	private static String AdaptTerminalType(String source, NStarType srcType, NStarType destType)
	{
		Debug.Assert(TypeIsPrimitive(srcType.MainType));
		Debug.Assert(TypeIsPrimitive(destType.MainType));
		var srcTypeBlockName = srcType.MainType.Peek().Name.ToString();
		var destTypeBlockName = destType.MainType.Peek().Name.ToString();
		Debug.Assert(destTypeBlockName != StringTypeName);
		var destTypeConverter = destTypeBlockName switch
		{
			NullString => "void",
			ShortCharTypeName => ByteTypeName,
			ShortIntTypeName => "short",
			UnsignedShortIntTypeName => "ushort",
			UnsignedIntTypeName => "uint",
			LongCharTypeName => "(char, char)",
			LongIntTypeName => "long",
			UnsignedLongIntTypeName => "ulong",
			LongLongTypeName => nameof(MpzT),
			UnsignedLongLongTypeName => nameof(MpuT),
			"unsigned long real" => "UnsignedLongReal",
			RealTypeName => "double",
			ComplexTypeName => "Complex",
			StringTypeName => nameof(String),
			RecursiveTypeName => "Type",
			"universal" => ObjectTypeName,
			_ => destTypeBlockName,
		};
		if (srcTypeBlockName == StringTypeName)
		{
			Debug.Assert(destTypeBlockName != StringTypeName);
			if (destTypeBlockName is BoolTypeName or ByteTypeName or CharTypeName or "short" or "ushort"
				or IntTypeName or "uint" or "long" or "ulong" or nameof(MpuT) or nameof(MpzT) or "double")
			{
				var result = ((String)"(").AddRange(destTypeConverter).Add('.').AddRange(nameof(int.TryParse)).Add('(');
				var varName = RedStarLinq.Fill(32, _ =>
					(char)(random.Next(2) == 1 ? random.Next('A', 'Z' + 1) : random.Next('a', 'z' + 1)));
				result.AddRange(source).AddRange(", out var ").AddRange(varName).AddRange(") ? ").AddRange(varName);
				return result.AddRange(" : ").AddRange(destTypeBlockName == BoolTypeName ? "false)" : "0)");
			}
			else
				return ((String)"(").AddRange(destTypeConverter).AddRange(")(").AddRange(source).Add(')');
		}
		else if (destTypeBlockName == BoolTypeName)
		{
			Debug.Assert(srcTypeBlockName != BoolTypeName);
			return ((String)"(").AddRange(source).AddRange(") >= 1");
		}
		else if (srcTypeBlockName == BoolTypeName)
		{
			Debug.Assert(destTypeBlockName != BoolTypeName);
			return ((String)"(").AddRange(source).AddRange(") ? 1 : 0");
		}
		else if (srcTypeBlockName is RealTypeName or DecimalTypeName)
		{
			Debug.Assert(destTypeBlockName != srcTypeBlockName);
			return ((String)"(").AddRange(destTypeConverter).Add(')')
				.AddRange(nameof(Truncate)).Add('(').AddRange(source).Add(')');
		}
		else
			return ((String)"unchecked((").AddRange(destTypeConverter).AddRange(")(").AddRange(source).AddRange("))");
	}
}
