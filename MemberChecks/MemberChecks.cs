global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib.Extras;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Reflection;
global using static NStar.Core.Extents;
global using static PL051.NStar.BuiltInMemberCollections;
global using static PL051.NStar.NStarType;
global using static PL051.NStar.TypeChecks;
global using static PL051.NStar.TypeConverters;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PL051.NStar;

public static class MemberChecks
{
	public static bool PropertyExists(this BuiltInMemberCollections C, NStarType container, String name, bool @static,
		[MaybeNullWhen(false)]
		out UserDefinedProperty? property)
	{
		if (C.UserDefinedProperties.TryGetValue(container.MainType, out var containerProperties)
			&& containerProperties.TryGetValue(name, out var a))
		{
			property = a;
			return ProcessProperty(C, container, ref property);
		}
		else if (C.UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& PropertyExists(C, userDefinedType.BaseType, name, @static, out property))
			return ProcessProperty(C, container, ref property);
		var containerType = SplitType(container.MainType);
		if (!C.TypeExists(containerType, out var netType))
		{
			property = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetProperty(name.ToString(),
			(@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public), out var netProperty))
			netProperty = netType.GetProperties((@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public)
				.Find(x => x.Name == name.ToString());
		if (netProperty is not null)
		{
			property = new(TypeMappingBack(netProperty.PropertyType, netType.GetGenericArguments(), container.ExtraTypes,
				container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))),
				@static ? PropertyAttributes.Static : PropertyAttributes.None, NullString);
			return true;
		}
		if (!netType.TryWrap(x => x.GetField(name.ToString(),
			(@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public), out var netField))
			netField = netType.GetFields((@static ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public)
				.Find(x => x.Name == name.ToString());
		if (netField is not null)
		{
			property = new(TypeMappingBack(netField.FieldType, netType.GetGenericArguments(), container.ExtraTypes,
				container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))),
				@static ? PropertyAttributes.Static : PropertyAttributes.None, NullString);
			return true;
		}
		if (@static)
		{
			property = null;
			return false;
		}
		if (!netType.TryWrap(x => x.GetEvent(name.ToString()), out var netEvent))
			netEvent = netType.GetEvents().Find(x => x.Name == name.ToString());
		if (netEvent is not null)
		{
			var handlerType = netEvent.EventHandlerType;
			if (handlerType is not null)
			{
				property = new(TypeMappingBack(handlerType, netType.GetGenericArguments(), container.ExtraTypes,
				container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))),
					PropertyAttributes.None, NullString);
				return true;
			}
		}
		property = null;
		return false;
	}

	public static bool UserDefinedPropertyExists(this BuiltInMemberCollections C, BlockStack container, String name,
		bool @static, [MaybeNullWhen(false)] out UserDefinedProperty? property,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool inBase, out BlockStack actualContainer)
	{
		UserDefinedType userDefinedType = default;
		if (CheckContainer(container, C.UserDefinedProperties.ContainsKey, out matchingContainer)
			&& C.UserDefinedProperties[matchingContainer].TryGetValue(name, out var value))
		{
			property = value;
			inBase = false;
			actualContainer = matchingContainer;
			return true;
		}
		else if (CheckContainer(container, x => C.UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && PropertyExists(C, userDefinedType.BaseType, name, @static, out property))
		{
			inBase = true;
			actualContainer = userDefinedType.BaseType.MainType;
			return true;
		}
		property = null;
		inBase = false;
		actualContainer = default;
		return false;
	}

	private static bool ProcessProperty(this BuiltInMemberCollections C, NStarType container, ref UserDefinedProperty? property)
	{
		Debug.Assert(property is not null);
		(BlockStack Container, String Type) matchingType = default;
		if (!CheckContainer(container.MainType, x => C.UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = C.UserDefinedTypes[matchingType].Restrictions;
		if (restrictions is null || restrictions.Length == 0)
			return true;
		var sourceTypes = restrictions.ToList(x => new NStarType(new(new Block(BlockType.Extra, x.Name, 1)), NoBranches));
		var destinationTypes = container.ExtraTypes
			.ToList(x => x.Name == "type" && x.Extra is NStarType NStarType ? NStarType : NullType);
		if (container.ExtraTypes.Length == 1 && container.ExtraTypes[0].Name == "List")
			destinationTypes.AddRange(container.ExtraTypes[0].Elements.Convert(GetBranchType));
		destinationTypes.FilterInPlace(x => !x.Equals(NullType));
		var patterns = GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
			destinationTypes, sourceTypes)
			.AddRange(GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
			sourceTypes, destinationTypes));
		var returnType = property.Value.NStarType;
		for (var j = 0; j < patterns.Length; j++)
		{
			for (var k = 0; k < sourceTypes.Length; k++)
				returnType = ReplaceExtraType(returnType, patterns[j]);
		}
		property = new(returnType, property.Value.Attributes, property.Value.DefaultValue);
		return true;
		static NStarType GetBranchType(TreeBranch x)
		{
			if (x.Name == "type" && x.Extra is NStarType NStarType)
				return NStarType;
			else if (x.Name == "Hypername" && x.Length == 1 && x[0].Name == "type" && x[0].Extra is NStarType NStarType2)
				return NStarType2;
			else
				return NullType;
		}
	}

	public static List<G.KeyValuePair<String, UserDefinedProperty>> GetAllProperties(this BuiltInMemberCollections C,
		BlockStack container)
	{
		List<G.KeyValuePair<String, UserDefinedProperty>> result = [];
		if (C.UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
			result.AddRange(GetAllProperties(C, userDefinedType.BaseType.MainType));
		if (C.UserDefinedProperties.TryGetValue(container, out var containerProperties))
			foreach (var containerProperty in containerProperties)
				result.Add(containerProperty);
		return result;
	}

	public static bool ConstantExists(this BuiltInMemberCollections C, NStarType container, String name,
		[MaybeNullWhen(false)] out UserDefinedConstant? constant)
	{
		if (name.Length == 0)
		{
			constant = null;
			return false;
		}
		if (C.UserDefinedConstants.TryGetValue(container.MainType, out var containerConstants)
			&& containerConstants.TryGetValue(name, out var a))
		{
			constant = a;
			return true;
		}
		else if (C.UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType))
		{
			if (userDefinedType.BaseType.Equals(container))
			{
				constant = null;
				return false;
			}
			if (ConstantExists(C, userDefinedType.BaseType, name, out constant))
				return true;
		}
		var containerType = SplitType(container.MainType);
		if (!C.TypeExists(containerType, out var netType))
		{
			constant = null;
			return false;
		}
		var netProperty = netType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
			.Find(x => (x.IsInitOnly || x.IsLiteral && !netType.IsEnum) && x.Name == name.ToString());
		if (netProperty is null)
		{
			constant = null;
			return false;
		}
		constant = new(TypeMappingBack(netProperty.FieldType, netType.GetGenericArguments(), container.ExtraTypes,
			container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
			? TypeMapping(C, NStarType) : typeof(void))),
			ConstantAttributes.None,
			new("#value", 0, []) { Extra = netProperty.GetValue(null)?.ToNString() ?? NullString });
		return true;
	}

	public static bool UserDefinedConstantExists(this BuiltInMemberCollections C, BlockStack container, String name,
		[MaybeNullWhen(false)] out UserDefinedConstant? constant, [MaybeNullWhen(false)] out BlockStack matchingContainer,
		[MaybeNullWhen(false)] out bool inBase)
	{
		UserDefinedType userDefinedType = default;
		if (CheckContainer(container, C.UserDefinedConstants.ContainsKey, out matchingContainer)
			&& C.UserDefinedConstants[matchingContainer].TryGetValue(name, out var value))
		{
			constant = value;
			inBase = false;
			return true;
		}
		else if (CheckContainer(container, x => C.UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer) && ConstantExists(C, userDefinedType.BaseType, name, out constant))
		{
			inBase = true;
			return true;
		}
		constant = null;
		inBase = false;
		return false;
	}

	public static bool UserDefinedPolymorphTypeExists(this BuiltInMemberCollections C, BlockStack container, String name,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		UserDefinedType userDefinedType = default;
		if (CheckContainer(container, x => C.UserDefinedTypes.TryGetValue(SplitType(x), out userDefinedType),
			out matchingContainer))
		{
			var foundIndex = userDefinedType.Restrictions
				?.FindIndex(x => x.RestrictionType.MainType.Equals(RecursiveBlockStack) && x.Name == name) ?? -1;
			if (foundIndex >= 0)
				return true;
		}
		return false;
	}

	public static bool MethodExists(this BuiltInMemberCollections C, NStarType container, String name)
	{
		var containerType = SplitType(container.MainType);
		if (!C.TypeExists(containerType, out var netType))
			return false;
		if (!netType.TryWrap(x => x.GetMethod(name.ToString()), out var method))
			method = netType.GetMethods().Find(x => x.Name == name.ToString());
		if (method is null)
			return false;
		return true;
	}

	public static bool MethodExists(this BuiltInMemberCollections C, NStarType container, String name,
		List<NStarType> callParameterTypes, List<NStarType> typeParameters,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions)
	{
		if (C.UserDefinedTypes.TryGetValue(SplitType(container.MainType), out _))
		{
			functions = default;
			return false;
		}
		if (name == "ToList" && TypeEqualsToPrimitive(container, TupleName, false)
			&& (container.ExtraTypes.AllEqual() || container.ExtraTypes.Length == 2
			&& container.ExtraTypes[1].Length == 0 && int.TryParse(container.ExtraTypes[1].Name.AsSpan(), out _))
			&& container.ExtraTypes[0].Name == "type" && container.ExtraTypes[0].Extra is NStarType TupleItemNStarType)
		{
			functions = [new(name, [], GetListType(TupleItemNStarType), FunctionAttributes.None, [], null)];
			return true;
		}
		var netType = TypeMapping(C, container);
		if (container.ExtraTypes.Length == 0)
		{
			if (netType == typeof(Task<>))
				netType = typeof(Task);
			else if (netType == typeof(ValueTask<>))
				netType = typeof(ValueTask);
		}
		var callParameterNetTypes = callParameterTypes.ToArray(x => TypeMapping(C, x));
		var validity = int.MinValue;
		var methods = netType.GetMethods().Filter(x => x.Name == name).Concat(ExtraTypes.Values
			.ConvertAndJoin(x => x.IsAbstract && x.IsSealed && x.GetGenericArguments().Length == 0 ? x.GetMethods()
			.Filter(x => x.Name == name && x.GetParameters().Length != 0 && x.IsDefined(typeof(ExtensionAttribute), true))
			: [])).FindAllMax(x =>
		{
			var currentValidity = GetMethodValidity(name, netType, x, callParameterNetTypes);
			if (currentValidity > validity)
				validity = currentValidity;
			return currentValidity;
		});
		functions = [];
		if (validity < 0)
			return false;
		var noArrayFunction = false;
		foreach (var method in methods)
		{
			if (Attribute.IsDefined(method, typeof(ObsoleteAttribute)))
				continue;
			var genericArguments = method.GetGenericArguments();
			var extentOffset = (method.DeclaringType is null
				|| method.DeclaringType.IsAbstract && method.DeclaringType.IsSealed)
				&& method.DeclaringType != netType ? 1 : 0;
			var patterns = genericArguments.Combine(typeParameters.Convert(x => TypeMapping(C, x))).ToList()
				.AddRange(GetReplacementPatterns(genericArguments,
				extentOffset == 1 ? [.. callParameterNetTypes.Prepend(netType)] : callParameterNetTypes));
			var returnNetType = method.ReturnType;
			var parameters = method.GetParameters();
			var functionParameterTypes = parameters.ToArray(x => x.ParameterType);
			for (var i = 0; i < patterns.Length; i++)
			{
				returnNetType = ReplaceExtraNetType(returnNetType, patterns[i]);
				for (var j = 0; j < functionParameterTypes.Length; j++)
					functionParameterTypes[j] = ReplaceExtraNetType(functionParameterTypes[j], patterns[i]);
			}
			if (extentOffset == 1 && functionParameterTypes[0] != typeof(void)
				&& !functionParameterTypes[0].IsAssignableFrom(netType))
				continue;
			var goodIndex = parameters.FindIndex(x => (x.ParameterType.Name.Contains(nameof(List<>))
				|| x.ParameterType.Name.Contains(nameof(G.IEnumerable<>)))
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)));
			var badIndex = parameters.FindIndex(x => x.ParameterType.IsSZArray
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)) || x.ParameterType.Name.Contains("Span"));
			if (goodIndex >= 0 && badIndex < 0)
				noArrayFunction = true;
			else if (noArrayFunction && badIndex >= 0)
				continue;
			functions.Add(new(name, [], TypeMappingBack(returnNetType, netType.GetGenericArguments(), container.ExtraTypes,
				container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))),
				(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0)
				| (method.ReturnType.FullName is not null
				&& (method.ReturnType.FullName.StartsWith("System.Threading.Tasks.Task")
				|| method.ReturnType.FullName.StartsWith("System.Threading.Tasks.ValueTask")) ? FunctionAttributes.Async : 0)
				| (extentOffset == 1 ? FunctionAttributes.Extent : 0),
				new(functionParameterTypes.ToList((x, index) => new ExtendedMethodParameter(TypeMappingBack(x,
				netType.GetGenericArguments(), container.ExtraTypes,
				container.ExtraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))), parameters[index].Name ?? "x",
				(parameters[index].IsOptional ? ParameterAttributes.Optional : 0)
				| (parameters[index].ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
				| (parameters[index].IsOut ? ParameterAttributes.Out : 0)
				| (Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? ParameterAttributes.Params : 0),
				parameters[index].DefaultValue?.ToString() ?? NullString))), null));
		}
		return true;
	}

	private static int GetMethodValidity(String? name, Type netType, MethodBase method, Type[] callParameterNetTypes)
	{
		if (name is not null && method.Name != name.ToString())
			return int.MinValue;
		var obsolete = method.GetCustomAttribute<ObsoleteAttribute>(false);
		if (obsolete is not null && obsolete.IsError)
			return 0;
		var extentOffset = (method.DeclaringType is null
			|| method.DeclaringType.IsAbstract && method.DeclaringType.IsSealed)
			&& method.DeclaringType != netType ? 1 : 0;
		if (CreateVar(method.GetParameters(), out var functionParameters).Length < callParameterNetTypes.Length + extentOffset)
			return 0;
		if (!functionParameters.Skip(callParameterNetTypes.Length + extentOffset).All(y => y.IsOptional))
			return 0;
		if (method.Name == nameof(name.AddRange) && functionParameters.Length == 1 && extentOffset == 0)
		{
			if (functionParameters[0].ParameterType.Name != "List`1")
				return 0;
			var genericArguments = functionParameters[0].ParameterType.GetGenericArguments();
			if (genericArguments.Length != 1)
				return 0;
			var listType = typeof(List<>).MakeGenericType(genericArguments);
			if (!functionParameters[0].ParameterType.Equals(listType))
				return 0;
			return functionParameters.Length;
		}
		var index = (functionParameters,
				extentOffset == 1 ? [.. callParameterNetTypes.Prepend(netType)] : callParameterNetTypes)
			.Combine().FindIndex(x => !IsValidParameter(x));
		return index >= 0 ? index : functionParameters.Length;
	}

	private static bool IsValidParameter((ParameterInfo, Type) x)
	{
		var genericArguments = x.Item2.GetGenericArguments();
		Type destType;
		if (x.Item1.ParameterType.IsGenericParameter)
		{
			if (genericArguments.Length != 0)
				destType = genericArguments[0];
			else if (x.Item2 == typeof(void))
				return true;
			else
				destType = x.Item2;
		}
		else if (x.Item1.ParameterType.IsSZArray)
		{
			if (genericArguments.Length != 0)
				destType = genericArguments[0].MakeArrayType();
			else if (x.Item2 == typeof(void))
				return true;
			else
				destType = x.Item2.MakeArrayType();
		}
		else if (x.Item1.ParameterType.ContainsGenericParameters)
		{
			if (x.Item2 == typeof(void))
				return true;
			else if (genericArguments.Length == 0 || typeof(ITuple).IsAssignableFrom(x.Item2))
				genericArguments = [x.Item2];
			if (x.Item1.ParameterType.GetGenericArguments().Length != genericArguments.Length)
				return false;
			destType = x.Item1.ParameterType.GetGenericTypeDefinition().MakeGenericType(genericArguments);
		}
		else
			destType = x.Item1.ParameterType;
		if (x.Item2 == typeof(void))
			return true;
		if (destType.IsAssignableFromExt(x.Item2))
			return true;
		return false;
	}

	public static bool ExtendedMethodExists(this BuiltInMemberCollections C, BlockStack container, String name,
		List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions, out bool user)
	{
		if (PublicFunctions.TryGetValue(name, out var functionOverload))
		{
			BlockStack mainType;
			if (functionOverload.ExtraTypes.Contains(item: functionOverload.ReturnType))
				mainType = FindParameter(functionOverload.ReturnType).MainType;
			else
				mainType = GetBlockStack(functionOverload.ReturnType);
			BranchCollection extraTypes = new(functionOverload.ReturnExtraTypes.ToList(GetTypeAsBranch));
			NStarType ReturnNStarType = (mainType, extraTypes);
			ExtendedMethodParameters parameters = [.. functionOverload.Parameters.Convert((x, index) =>
			{
				NStarType NStarType;
				if (functionOverload.ExtraTypes.Contains(item: x.Type))
					NStarType = FindParameter(x.Type);
				else
					NStarType = new(GetBlockStack(x.Type), new(x.ExtraTypes.Convert(GetTypeAsBranch)));
				return new ExtendedMethodParameter(NStarType, x.Name, x.Attributes, x.DefaultValue);
			})];
			var functionParameterTypes = parameters.ToList((x, index) => index == callParameterTypes.Length - 1
				&& (x.Attributes & ParameterAttributes.Params) == ParameterAttributes.Params ? GetListType(x.Type) : x.Type);
			if (parameters.Length != 0 && (parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callParameterTypes.Length > functionParameterTypes.Length)
			{
				functionParameterTypes.RemoveAt(^1);
				functionParameterTypes.AddSeries(parameters[^1].Type,
					callParameterTypes.Length - functionParameterTypes.Length);
			}
#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable S2234
			var patterns = GetNStarReplacementPatterns(C, functionOverload.ExtraTypes,
				callParameterTypes, functionParameterTypes)
				.AddRange(GetNStarReplacementPatterns(C, functionOverload.ExtraTypes,
				functionParameterTypes, callParameterTypes))
				.FilterInPlace(x => !x.TypeToInsert.ExtraTypes
				.Any(y => y.Name == "type" && y.Extra is NStarType NStarType && NStarType.MainType.TryPeek(out var block)
				&& block.Name.Equals(x.ExtraType)));
#pragma warning restore S2234
#pragma warning restore IDE0079 // Удалить ненужное подавление
			for (var j = 0; j < patterns.Length; j++)
			{
				ReturnNStarType = ReplaceExtraType(ReturnNStarType, patterns[j]);
				for (var k = 0; k < functionParameterTypes.Length; k++)
					functionParameterTypes[k] = ReplaceExtraType(functionParameterTypes[k], patterns[j]);
				parameters[j] = new(functionParameterTypes[j], parameters[j].Name, parameters[j].Attributes,
					parameters[j].DefaultValue);
			}
			functions = [new(name, [], ReturnNStarType, functionOverload.Attributes, parameters, null)];
			user = false;
			return true;
		}
		if (!(C.UserDefinedMethods.TryGetValue(container, out var methods)
			&& methods.TryGetValue(name, out var overloads)))
		{
			if (BuiltInMemberCollections.ExtendedMethods.TryGetValue(container, out var builtInMethods)
				&& builtInMethods.TryGetValue(name, out var builtInOverloads))
			{
				functions = [.. builtInOverloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0).ToList(x =>
					new UserDefinedMethodOverload(name, x.Restrictions, x.ReturnNStarType, x.Attributes, x.Parameters, null))];
				user = false;
				return true;
			}
			functions = null;
			user = false;
			return false;
		}
		functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		for (var i = 0; i < functions.Length; i++)
		{
			var arrayParameters = functions[i].Restrictions;
			for (var j = 0; j < arrayParameters.Length; j++)
			{
				var x = arrayParameters[j];
				if (!(!x.Package && x.RestrictionType.ExtraTypes.Length == 0
					&& x.RestrictionType.MainType.Length == 1
					&& x.RestrictionType.MainType.Peek().BlockType == BlockType.Extra && callParameterTypes.Length > j))
					continue;
				functions[i] = new(functions[i].RealName, [], ReplaceExtraType(functions[i].ReturnNStarType,
					(x.RestrictionType.MainType.Peek().Name, callParameterTypes[j])), functions[i].Attributes,
					[.. functions[i].Parameters.Convert(y => new ExtendedMethodParameter(ReplaceExtraType(y.Type,
					(x.RestrictionType.MainType.Peek().Name, callParameterTypes[j])), y.Name, y.Attributes, y.DefaultValue))],
					null);
			}
		}
		user = true;
		return true;
		NStarType FindParameter(String typeName)
		{
			var foundIndex = functionOverload.Parameters
				.FindIndex(x => typeName == x.Type || x.ExtraTypes.Contains(item: typeName));
			if (foundIndex != callParameterTypes.Length - 1
				|| (functionOverload.Parameters[foundIndex].Attributes & ParameterAttributes.Params)
				!= ParameterAttributes.Params)
				return callParameterTypes[foundIndex];
			else if (GetSubtype(C, callParameterTypes[foundIndex]) is var subtype && !subtype.Equals(NullType))
				return subtype;
			else
				return callParameterTypes[foundIndex];
		}
		TreeBranch GetTypeAsBranch(String typeName) => new("type", 0, [])
		{
			Extra = functionOverload.ExtraTypes.Contains(item: typeName)
			? FindParameter(typeName) : new NStarType(GetBlockStack(typeName), [])
		};
	}

	public static bool UserDefinedFunctionExists(this BuiltInMemberCollections C, BlockStack container, String name)
	{
		if (CheckContainer(container, C.UserDefinedMethods.ContainsKey, out var matchingContainer)
			&& C.UserDefinedMethods[matchingContainer].TryGetValue(name, out var method_overloads))
			return true;
		else if (C.UserDefinedTypes.TryGetValue(SplitType(container), out var userDefinedType))
		{
			if (MethodExists(C, userDefinedType.BaseType, name))
				return true;
			else if (UserDefinedFunctionExists(C, userDefinedType.BaseType.MainType, name))
				return true;
		}
		return false;
	}

	public static bool UserDefinedFunctionExists(this BuiltInMemberCollections C, NStarType container, String name,
		List<NStarType> parameterTypes, List<NStarType> typeParameters,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions) =>
		UserDefinedFunctionExists(C, container, name, parameterTypes, typeParameters, out functions, out _, out _);

	public static bool UserDefinedFunctionExists(this BuiltInMemberCollections C, NStarType container, String name, List<NStarType> callParameterTypes,
		List<NStarType> typeParameters, [MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out bool derived)
	{
		var mainType = container.MainType;
		if (!(CheckContainer(mainType, C.UserDefinedMethods.ContainsKey, out matchingContainer)
			&& C.UserDefinedMethods[matchingContainer].TryGetValue(name, out functions)))
		{
			if (C.UserDefinedTypes.TryGetValue(SplitType(mainType), out var userDefinedType))
			{
				if (userDefinedType.BaseType.Equals(default))
				{
					functions = null;
					derived = false;
					return false;
				}
				else if (MethodExists(C, userDefinedType.BaseType, name, callParameterTypes, typeParameters, out functions))
				{
					derived = true;
					return true;
				}
				else if (UserDefinedFunctionExists(C, userDefinedType.BaseType, name, callParameterTypes, typeParameters,
					out functions, out matchingContainer, out derived))
					return ProcessUserDefinedMethod(C, container, callParameterTypes, functions);
			}
			functions = null;
			derived = false;
			return false;
		}
		functions = [.. functions.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		derived = false;
		return ProcessUserDefinedMethod(C, container, callParameterTypes, functions);
	}

	private static bool ProcessUserDefinedMethod(this BuiltInMemberCollections C, NStarType container, List<NStarType> callParameterTypes,
		UserDefinedMethodOverloads functions)
	{
		var mainType = container.MainType;
		(BlockStack Container, String Type) matchingType = default;
		if (!CheckContainer(mainType, x => C.UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = C.UserDefinedTypes[matchingType].Restrictions;
		if (restrictions is null || restrictions.Length == 0)
			return true;
		for (var i = 0; i < functions.Length; i++)
		{
			var function = functions[i];
			var ReturnNStarType = function.ReturnNStarType;
			ExtendedMethodParameters parameters = [.. function.Parameters.Convert(x =>
				new ExtendedMethodParameter(x.Type, x.Name, x.Attributes, x.DefaultValue))];
			callParameterTypes = callParameterTypes.Copy();
			var extraTypes = container.ExtraTypes;
			if (extraTypes.Length == 1 && extraTypes.First() is var extraType && extraType.Name == "List")
				extraTypes = [.. extraType.Elements];
			foreach (var x in extraTypes)
			{
				TreeBranch branch;
				if (x.Name == "Hypername" && x.Length == 1)
					branch = x[0];
				else
					branch = x;
				if (branch.Name == "type" && branch.Extra is NStarType NStarType)
					callParameterTypes.Add(NStarType);
			}
			var functionParameterTypes = parameters.ToList(x => x.Type)
				.Concat(restrictions.ToList(x => new NStarType(new(new Block(BlockType.Extra, x.Name, 1)), NoBranches)));
#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable S2234
			var patterns = GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
				callParameterTypes, functionParameterTypes)
				.AddRange(GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
				functionParameterTypes, callParameterTypes));
#pragma warning restore S2234
#pragma warning restore IDE0079 // Удалить ненужное подавление
			for (var j = 0; j < patterns.Length; j++)
			{
				ReturnNStarType = ReplaceExtraType(ReturnNStarType, patterns[j]);
				for (var k = 0; k < parameters.Length; k++)
				{
					functionParameterTypes[k] = ReplaceExtraType(functionParameterTypes[k], patterns[j]);
					parameters[k] = new(functionParameterTypes[k], parameters[k].Name, parameters[k].Attributes,
						parameters[k].DefaultValue);
				}
			}
			functions[i] = new(function.RealName, function.Restrictions, ReturnNStarType, function.Attributes, parameters,
				null);
		}
		return true;
	}

	public static bool UserDefinedNonDerivedFunctionExists(this BuiltInMemberCollections C, BlockStack container, String name,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer)
	{
		if (!(CheckContainer(container, C.UserDefinedMethods.ContainsKey, out matchingContainer)
			&& C.UserDefinedMethods[matchingContainer].TryGetValue(name, out var overloads)))
		{
			functions = null;
			return false;
		}
		functions = [.. overloads.Filter(x => (x.Attributes & FunctionAttributes.Wrong) == 0)];
		return true;
	}

	public static ListHashSet<(String ExtraType, NStarType TypeToInsert)>
		GetNStarReplacementPatterns(this BuiltInMemberCollections C, List<String> genericArguments,
		List<NStarType> callParameterTypes, List<NStarType> functionParameterTypes)
	{
		var length = Min(callParameterTypes.Length, functionParameterTypes.Length);
		ListHashSet<(String ExtraType, NStarType TypeToInsert)> result = [];
		for (var i = 0; i < genericArguments.Length; i++)
		{
			var genericArgument = genericArguments[i];
			for (var j = 0; j < length; j++)
			{
				var callParameterType = callParameterTypes[j];
				var functionParameterType = functionParameterTypes[j];
				if (TypeIsFullySpecified(C, callParameterType, []))
					continue;
				if (callParameterType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra
					&& block.Name == genericArgument)
				{
					result.Add((genericArgument, functionParameterType));
					continue;
				}
				result.AddRange(GetNStarReplacementPatterns(C, genericArguments,
					callParameterType.ExtraTypes.ToList(x => x.Name == "type" && x.Extra is NStarType NStarType
					? NStarType : NullType),
					functionParameterType.ExtraTypes.ToList(x => x.Name == "type" && x.Extra is NStarType NStarType
					? NStarType : NullType)));
			}
		}
		return result;
	}

	public static bool ConstructorsExist(this BuiltInMemberCollections C, NStarType container, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!C.TypeExists(containerType, out var netType))
		{
			constructors = [];
			return false;
		}
		var callParameterNetTypes = callParameterTypes.ToArray(x => TypeMapping(C, x));
		var validity = int.MinValue;
		var methods = netType.GetConstructors().FindAllMax(x =>
		{
			var currentValidity = GetMethodValidity(null, netType, x, callParameterNetTypes);
			if (currentValidity > validity)
				validity = currentValidity;
			return currentValidity;
		});
		constructors = [];
		if (validity < 0)
			return false;
		var noArrayConstructor = false;
		foreach (var method in methods)
		{
			var genericArguments = netType.GetGenericArguments();
			var patterns = GetReplacementPatterns(genericArguments, callParameterNetTypes);
			var parameters = method.GetParameters();
			var constructorParameterTypes = parameters.ToArray(x => x.ParameterType);
			for (var i = 0; i < patterns.Length; i++)
			{
				for (var j = 0; j < constructorParameterTypes.Length; j++)
					constructorParameterTypes[j] = ReplaceExtraNetType(constructorParameterTypes[j], patterns[i]);
			}
			var goodIndex = parameters.FindIndex(x => (x.ParameterType.Name.Contains(nameof(List<>))
				|| x.ParameterType.Name.Contains(nameof(G.IEnumerable<>)))
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)));
			var badIndex = parameters.FindIndex(x => x.ParameterType.IsSZArray
				&& !Attribute.IsDefined(x, typeof(ParamArrayAttribute)) || x.ParameterType.Name.Contains("Span"));
			if (goodIndex >= 0 && badIndex < 0)
				noArrayConstructor = true;
			else if (noArrayConstructor && badIndex >= 0)
				continue;
			constructors.Add(new((method.IsAbstract ? ConstructorAttributes.Abstract : 0)
				| (method.IsStatic ? ConstructorAttributes.Static : 0),
				new(constructorParameterTypes.ToList((x, index) => new ExtendedMethodParameter(TypeMappingBack(x,
				netType.GetGenericArguments(), [.. CreateVar(container.ExtraTypes.SkipWhile(x =>
				x.Name != "type" || x.Extra is not NStarType), out var extraTypes)],
				extraTypes.ToArray(x => x.Name == "type" && x.Extra is NStarType NStarType
				? TypeMapping(C, NStarType) : typeof(void))).Wrap(y =>
				Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? GetSubtype(C, y) : y),
				parameters[index].Name ?? "x",
				(parameters[index].IsOptional ? ParameterAttributes.Optional : 0)
				| (parameters[index].ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
				| (parameters[index].IsOut ? ParameterAttributes.Out : 0)
				| (Attribute.IsDefined(parameters[index], typeof(ParamArrayAttribute)) ? ParameterAttributes.Params : 0),
				parameters[index].DefaultValue?.ToString() ?? NullString))), [], null));
		}
		return true;
	}

	public static bool UserDefinedConstructorsExist(this BuiltInMemberCollections C, NStarType container, List<NStarType> callParameterTypes,
		[MaybeNullWhen(false)] out ConstructorOverloads constructors)
	{
		var mainType = container.MainType;
		if (!C.UserDefinedConstructors.TryGetValue(container.MainType, out var temp_constructors)
			|| C.UserDefinedTypes.TryGetValue(SplitType(container.MainType), out var userDefinedType)
			&& (userDefinedType.Attributes & (TypeAttributes.Struct | TypeAttributes.Static))
			is not (0 or TypeAttributes.Sealed or TypeAttributes.Struct))
		{
			constructors = null;
			return false;
		}
		constructors = [.. temp_constructors,
			.. ConstructorsExist(C, userDefinedType.BaseType, callParameterTypes, out var baseConstructors)
			? baseConstructors : [],
			.. UserDefinedConstructorsExist(C, userDefinedType.BaseType, callParameterTypes, out baseConstructors)
			? baseConstructors : []];
		(BlockStack Container, String Type) matchingType = default;
		if (!CheckContainer(mainType, x => C.UserDefinedTypes.ContainsKey(matchingType = SplitType(x)), out _))
			return true;
		var restrictions = C.UserDefinedTypes[matchingType].Restrictions;
		if (restrictions is null || restrictions.Length == 0)
			return true;
		for (var i = 0; i < constructors.Length; i++)
		{
			var (Attributes, Parameters, UnsetRequiredProperties, _) = constructors[i];
			ExtendedMethodParameters parameters = [.. Parameters.Convert(x =>
				new ExtendedMethodParameter(x.Type, x.Name, x.Attributes, x.DefaultValue))];
			var constructorParameterTypes = parameters.ToList(x => x.Type);
			var typeParameters = container.ExtraTypes.ConvertAndJoin(x => x.Length == 0 ? [x] : x.Elements)
				.ConvertAndJoin(x =>
			{
				if (x.Name == "Hypername" && x.Length == 1)
					x = x[0];
				if (x.Length == 0 && x.Name == "type" && x.Extra is NStarType NStarType)
					return new[] { NStarType };
				else
					return [];
			});
			var patterns = GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
				restrictions.Convert(x => BasicTypeToExtendedType(x.Name, [])).Take(typeParameters.Length())
				.Concat(callParameterTypes),
				typeParameters.Concat(constructorParameterTypes))
				.AddRange(GetNStarReplacementPatterns(C, restrictions.ToList(x => x.Name),
				constructorParameterTypes, callParameterTypes));
			for (var j = 0; j < patterns.Length; j++)
			{
				for (var k = 0; k < constructorParameterTypes.Length; k++)
				{
					constructorParameterTypes[k] = ReplaceExtraType(constructorParameterTypes[k], patterns[j]);
					parameters[k] = new(constructorParameterTypes[k], parameters[k].Name, parameters[k].Attributes,
						parameters[k].DefaultValue);
				}
			}
			constructors[i] = new(Attributes, parameters, UnsetRequiredProperties, null);
		}
		return true;
	}

	public static bool TypeIsFullySpecified(this BuiltInMemberCollections C, NStarType type, BlockStack container)
	{
		BlockStack partialContainer;
		String name;
		if (type.MainType.Length == 0 || type.MainType.Peek().BlockType == BlockType.Extra
			&& !(UserDefinedPolymorphTypeExists(C, partialContainer = new(type.MainType.SkipLast(1)),
			name = type.MainType.Peek().Name, out _) || ConstantExists(C, new(partialContainer, NoBranches), name, out _)
			|| type.MainType.Length == 1
			&& UserDefinedConstantExists(C, container, type.MainType.Peek().Name, out _, out _, out _)))
			return false;
		if (type.ExtraTypes.Length != 0
			&& (type.ExtraTypes.AllEqual() || type.ExtraTypes.Length == 2
			&& type.ExtraTypes[1].Length == 0 && int.TryParse(type.ExtraTypes[1].Name.AsSpan(), out _))
			&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType ItemNStarType
			&& TypeIsFullySpecified(C, ItemNStarType, container))
			return true;
		foreach (var x in type.ExtraTypes)
			if (x.Name == "type" && x.Extra is NStarType InnerNStarType
				&& !TypeIsFullySpecified(C, InnerNStarType, container))
				return false;
		return true;
	}
}
