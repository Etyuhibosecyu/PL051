global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using static NStar.Core.Extents;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using String = NStar.Core.String;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PL051.NStar;

public static class TypeChecks
{
	public static bool CheckContainer(BlockStack container, Func<BlockStack, bool> check, out BlockStack matchingContainer)
	{
		if (check(container))
		{
			matchingContainer = container;
			return true;
		}
		var containerPart = container.ToList().GetSlice();
		BlockStack stack;
		while (containerPart.Any())
		{
			containerPart = containerPart.SkipLast(1);
			if (check(stack = new(containerPart)))
			{
				matchingContainer = stack;
				return true;
			}
		}
		matchingContainer = new();
		return false;
	}

	public static bool ExtraTypeExists(BlockStack container, String name, out bool @class)
	{
		@class = false;
		if (container.Length != 0 && UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType)
			&& (userDefinedType.Restrictions?.Exists(x => x.Name == name) ?? false))
			return true;
		if (UserDefinedConstants.TryGetValue(container, out var containerConstants)
			&& containerConstants.TryGetValue(name, out var constant))
		{
			if (TypeIsPrimitive(constant.NStarType.MainType) && constant.NStarType.MainType.TryPeek(out var block)
				&& block.Name == RecursiveTypeName && constant.NStarType.ExtraTypes.Length == 0)
				return true;
			if (constant.NStarType.MainType.Equals(DictionaryBlockStack) && constant.NStarType.ExtraTypes.Length == 2
				&& constant.NStarType.ExtraTypes[1].Name == "type"
				&& constant.NStarType.ExtraTypes[1].Extra is NStarType ValueNStarType
				&& ValueNStarType.MainType.TryPeek(out block) && block.BlockType == BlockType.Other && block.Name == "Class")
			{
				@class = true;
				return true;
			}
		}
		if (Variables.TryGetValue(container, out var containerVariables)
			&& containerVariables.TryGetValue(name, out var variableType))
			return TypeIsPrimitive(variableType.MainType) && variableType.MainType.Peek().Name == RecursiveTypeName;
		return UserDefinedProperties.TryGetValue(container, out var containerProperties)
			&& containerProperties.TryGetValue(name, out var a)
			&& TypeIsPrimitive(a.NStarType.MainType) && a.NStarType.MainType.Peek().Name == RecursiveTypeName
			&& a.NStarType.ExtraTypes.Length == 0;
	}

	public static int GetTypeSize(Type type, int depth = 1)
	{
		if (depth > 10)
			return -1;
		if (type.IsPrimitive || type.IsEnum)
			return (int?)typeof(Marshal).GetMethod(nameof(Marshal.SizeOf), BindingFlags.Static | BindingFlags.Public, [])
				?.MakeGenericMethod([type])?.Invoke(null, []) ?? throw new InvalidOperationException(PropertyNameError);
		if (type.IsClass)
			return 8;
		long total = 0;
		var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (fields.Length == 1 && type.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute attribute)
		{
			var length = type.GetGenericArguments().Length == 0
				? (int?)type.GetField("_size", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null)
				?? throw new InvalidOperationException(PropertyNameError) : attribute.Length;
			if (fields[0].FieldType == typeof(bool))
				total = GetArrayLength(length, 32) * 4;
			else
				total = (long)GetTypeSize(fields[0].FieldType) * length;
			if (total > int.MaxValue)
				return -1;
			return (int)total;
		}
		foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			var fieldSize = GetTypeSize(field.FieldType, depth + 1);
			if (fieldSize < 0)
				return -1; // Неизвестный размер внутри
			total += fieldSize;
		}
		if (total > int.MaxValue)
			return -1;
		return (int)total;
	}


	public static bool IsEqualOrDerived(NStarType derived, NStarType @base)
	{
		if (derived.Equals(@base) || @base.Equals(ObjectType))
			return true;
		String foundName = default!;
		if (@base.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
			&& CheckContainer(@base.MainType, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
			&& containerTempTypes.Find(x => x.Name == block.Name) is var found && (foundName = found.Name) is not null, out _)
			&& derived.MainType.TryPeek(out block) && block.BlockType == BlockType.Extra
			&& CheckContainer(derived.MainType, stack => TempTypes.TryGetValue(stack, out var containerTempTypes)
			&& containerTempTypes.Any(x => x.Name == block.Name && Variables.TryGetValue(stack, out var containerVariables)
			&& containerVariables.TryGetValue(x.Name, out var VariableNStarType)
			&& VariableNStarType.MainType.Equals(RecursiveBlockStack) && VariableNStarType.ExtraTypes.Length == 1
			&& VariableNStarType.ExtraTypes[0].Name == "type"
			&& VariableNStarType.ExtraTypes[0].Extra is NStarType BaseNStarType
			&& BaseNStarType.Equals(@base)), out _))
			return true;
		if (IsEqualOrDerivedNetType(derived, @base))
			return true;
		var type = derived;
		while (!type.Equals(NullType))
		{
			if (!UserDefinedTypes.TryGetValue(SplitType(derived.MainType), out var userDefinedType))
				return false;
			type = userDefinedType.BaseType;
			if (type.MainType.Equals(@base.MainType))
				return true;
		}
		return false;
	}

	private static bool IsEqualOrDerivedNetType(NStarType sourceType, NStarType destinationType)
	{
		if (sourceType.MainType.TryPeek(out var sourceBlock))
		{
			if (!(PrimitiveTypes.TryGetValue(sourceBlock.Name, out var sourceNetType)
				|| ExtraTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
				sourceBlock.Name), out sourceNetType)
				|| IOTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
				sourceBlock.Name), out sourceNetType)
				|| ImportedTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
				sourceBlock.Name), out sourceNetType)))
				return false;
			if (sourceNetType == typeof(void))
				return false;
			if (sourceNetType.GetGenericArguments().Length != 0)
				return false;
			if (!destinationType.MainType.TryPeek(out var destinationBlock))
				return false;
			if (!(PrimitiveTypes.TryGetValue(destinationBlock.Name, out var destinationNetType)
				|| ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				destinationBlock.Name), out destinationNetType)
				|| IOTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				destinationBlock.Name), out destinationNetType)
				|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				destinationBlock.Name), out destinationNetType)))
				return false;
			if (destinationNetType.GetGenericArguments().Length == 0
				&& destinationNetType.IsAssignableFrom(sourceNetType))
				return true;
			if (destinationNetType.GetGenericArguments().Length == 1
				&& destinationNetType.GetGenericArguments()[0].Name is "T" or "TSelf"
				&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
				&& genericType.IsAssignableFrom(sourceNetType))
				return true;
			return false;
		}
		foreach (var x in ExplicitlyConnectedNamespaces)
		{
			if (!(ExtraTypes.TryGetValue((x,
				sourceType.MainType.TryPeek(out sourceBlock) ? sourceBlock.Name : ""), out var sourceNetType)
				|| IOTypes.TryGetValue((x,
				sourceType.MainType.TryPeek(out sourceBlock) ? sourceBlock.Name : ""), out sourceNetType)
				|| ImportedTypes.TryGetValue((x,
				sourceType.MainType.TryPeek(out sourceBlock) ? sourceBlock.Name : ""), out sourceNetType)))
				continue;
			if (sourceNetType.GetGenericArguments().Length != 0)
				continue;
			if (!(ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				sourceType.MainType.TryPeek(out var destinationBlock) ? destinationBlock.Name : ""), out var destinationNetType)
				|| IOTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				sourceType.MainType.TryPeek(out destinationBlock) ? destinationBlock.Name : ""), out destinationNetType)
				|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
				sourceType.MainType.TryPeek(out destinationBlock) ? destinationBlock.Name : ""), out destinationNetType)))
				continue;
			if (destinationNetType.GetGenericArguments().Length == 0
				&& destinationNetType.IsAssignableFrom(sourceNetType))
				return true;
			if (destinationNetType.GetGenericArguments()[0].Name is "T" or "TSelf"
				&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
				&& genericType.IsAssignableFrom(sourceNetType))
				return true;
		}
		if (!destinationType.MainType.TryPeek(out var destinationBlock2))
			return false;
		if (!(PrimitiveTypes.TryGetValue(destinationBlock2.Name, out var destinationNetType2)
			|| ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
			destinationBlock2.Name), out destinationNetType2)
			|| IOTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
			destinationBlock2.Name), out destinationNetType2)
			|| ImportedTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
			destinationBlock2.Name), out destinationNetType2)))
			return false;
		if (destinationNetType2.GetGenericArguments().Length != 1)
			return false;
		if (destinationNetType2.GetGenericArguments()[0].Name is "T"
			&& destinationType.ExtraTypes.Length == 1 && destinationType.ExtraTypes[0].Name == "type"
			&& destinationType.ExtraTypes[0].Extra is NStarType ClosureNStarType && ClosureNStarType.Equals(sourceType))
			return true;
		return false;
	}

	public static bool IsGUIType(NStarType container)
	{
		var mainType = container.MainType;
		if (mainType.Length == 0)
			return false;
		var split = SplitType(mainType);
		if (split.Container.StartsWith(GetNamespaceStack(SystemGUI))
			&& IOTypes.TryGetValue((split.Container.ToString(), split.Type), out _))
			return true;
		else if (UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType))
		{
			if (userDefinedType.BaseType.Equals(container))
				return false;
			if (IsGUIType(userDefinedType.BaseType))
				return true;
		}
		return false;
	}

	public static bool IsNotImplementedNamespace(String @namespace) => NotImplementedNamespaces.Contains(@namespace);

	public static bool IsOutdatedNamespace(String @namespace, out String? useInstead)
	{
		if (OutdatedNamespaces.TryGetValue(@namespace, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedNamespace(String @namespace) => ReservedNamespaces.Contains(@namespace);

	public static bool IsIONamespace(String @namespace) =>
		IONamespaces.Contains(@namespace) || ImportedNamespaces.Contains(@namespace);

	public static bool IsNotImplementedType(String @namespace, String typeName) =>
		NotImplementedTypes.Contains((@namespace, typeName));

	public static bool IsOutdatedType(String @namespace, String typeName, out String? useInstead)
	{
		if (OutdatedTypes.TryGetValue((@namespace, typeName), out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String typeName) => ReservedTypes.Contains((@namespace, typeName));

	public static bool IsIOType(String @namespace, String typeName) =>
		IOTypes.ContainsKey((@namespace, typeName)) || ImportedTypes.ContainsKey((@namespace, typeName));

	public static bool IsNotImplementedEndOfIdentifier(String identifier, out String wrongEnd)
	{
		foreach (var typeEnd in NotImplementedTypeEnds)
		{
			if (identifier.EndsWith(typeEnd))
			{
				wrongEnd = typeEnd;
				return true;
			}
		}
		wrongEnd = [];
		return false;
	}

	public static bool IsOutdatedEndOfIdentifier(String identifier, out String wrongEnd, out String useInstead)
	{
		foreach (var typeEnd in OutdatedTypeEnds)
		{
			if (identifier.EndsWith(typeEnd.Key))
			{
				useInstead = typeEnd.Value;
				wrongEnd = typeEnd.Key;
				return true;
			}
		}
		useInstead = [];
		wrongEnd = [];
		return false;
	}

	public static bool IsReservedEndOfIdentifier(String identifier, out String wrongEnd)
	{
		foreach (var typeEnd in ReservedTypeEnds)
		{
			if (identifier.EndsWith(typeEnd))
			{
				wrongEnd = typeEnd;
				return true;
			}
		}
		wrongEnd = [];
		return false;
	}

	public static bool IsNotImplementedMember(BlockStack type, String member) => NotImplementedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member);

	public static bool IsOutdatedMember(BlockStack type, String member, out String? useInstead)
	{
		if (OutdatedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.TryGetValue(member, out useInstead))
			return true;
		useInstead = [];
		return false;
	}

	public static bool IsReservedMember(BlockStack type, String member) => ReservedMembers.TryGetValue(type, out var containerMembers)
			&& containerMembers.Contains(member);

	public static bool TryGetSingularTupleSizeLimit(NStarType type, [MaybeNullWhen(false)] out int limit)
	{
		if (type.Equals(BoolType))
		{
			limit = 1000_000_000;
			return true;
		}
		else if (TryGetTypeSize(type, out var size))
		{
			limit = 128_000_000 / size;
			return true;
		}
		else
		{
			limit = -1;
			return false;
		}
	}

	public static bool TryGetTypeSize(NStarType type, [MaybeNullWhen(false)] out int size)
	{
		if (!type.MainType.TryPeek(out var block))
		{
			size = default;
			return false;
		}
		else if (block.BlockType == BlockType.Class
			&& UserDefinedTypes.TryGetValue(SplitType(type.MainType), out var userDefinedType)
			&& (userDefinedType.Attributes & TypeAttributes.Delegate)
			is 0 or TypeAttributes.Sealed or TypeAttributes.Abstract or TypeAttributes.Static)
		{
			size = 8;
			return true;
		}
		else if (block.BlockType == BlockType.Enum
			&& UserDefinedTypes.TryGetValue(SplitType(type.MainType), out userDefinedType)
			&& !userDefinedType.BaseType.Equals(NullType))
			return TryGetTypeSize(userDefinedType.BaseType, out size);
		else if (block.BlockType != BlockType.Primitive)
		{
			if (TypeExists(SplitType(type.MainType), out var netType) && GetTypeSize(netType) is var netSize && netSize > 0)
			{
				size = netSize;
				return true;
			}
			else
			{
				size = default;
				return false;
			}
		}
		else if (block.Name == "list")
		{
			size = 8;
			return true;
		}
		else if (block.Name != TupleName)
		{
			size = block.Name.AsSpan() switch
			{
				BoolTypeName or ByteTypeName or ShortCharTypeName => 1,
				ShortIntTypeName or UnsignedShortIntTypeName or CharTypeName => 2,
				IntTypeName or UnsignedIntTypeName or LongCharTypeName or "index" => 4,
				LongIntTypeName or nameof(DateTime) or nameof(TimeSpan) or UnsignedLongIntTypeName
					or RealTypeName or LongLongTypeName or UnsignedLongLongTypeName
					or RecursiveTypeName or StringTypeName or "range" => 8,
				DecimalTypeName or ComplexTypeName => 16,
				_ => default,
			};
			return size != default;
		}
		else if (type.ExtraTypes.Length == 2
			&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType ItemNStarType
			&& type.ExtraTypes[1].Length == 0 && int.TryParse(type.ExtraTypes[1].Name.AsSpan(), out var number)
			&& TryGetTypeSize(ItemNStarType, out var itemSize))
		{
			size = itemSize * number;
			if (ItemNStarType.Equals(BoolType))
				size = GetArrayLength(size, 4) * 4;
			return size != default;
		}
		else
		{
			size = 0;
			foreach (var item in type.ExtraTypes)
			{
				if (item.Name == "type" && item.Extra is NStarType NStarType && TryGetTypeSize(NStarType, out itemSize))
					size += itemSize;
				else
					size += 128;
			}
			return true;
		}
	}

	public static bool TypeExists((BlockStack Container, String Type) containerType, [MaybeNullWhen(false)] out Type netType)
	{
		if (PrimitiveTypes.TryGetValue(containerType.Type, out netType))
			return true;
		if (ExtraTypes.TryGetValue((containerType.Container.ToString(), containerType.Type), out netType)
			|| ImportedTypes.TryGetValue((containerType.Container.ToString(), containerType.Type), out netType)
			|| IOTypes.TryGetValue((containerType.Container.ToString(), containerType.Type), out netType))
			return true;
		if (Interfaces.TryGetValue((containerType.Container.ToString(), containerType.Type), out var @interface))
		{
			netType = @interface.DotNetType;
			return true;
		}
		if (ExtendedTypes.TryGetValue((containerType.Container, containerType.Type), out var extendedType))
		{
			netType = containerType.Type.ToString() switch
			{
				nameof(Action) => typeof(Action),
				nameof(Func<>) => typeof(Func<>),
				_ => throw new InvalidOperationException(),
			};
			return true;
		}
		Type? preservedNetType = null;
		if (containerType.Container.Length != 0)
			return false;
		if (ExplicitlyConnectedNamespaces
			.FindIndex(x => ExtraTypes.TryGetValue((x, containerType.Type), out preservedNetType)) < 0
			&& ExplicitlyConnectedNamespaces
			.FindIndex(x => ImportedTypes.TryGetValue((x, containerType.Type), out preservedNetType)) < 0)
			return false;
		if (preservedNetType is null)
			return false;
		netType = preservedNetType;
		return true;
	}
}
