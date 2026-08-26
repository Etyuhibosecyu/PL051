global using NStar.Mpir;
global using static PL051.NStar.NStarType;
global using String = NStar.Core.String;

namespace PL051.NStar;

public static class TypePromotionRules
{
	public static String GetQuotientType(String leftType, object right, String rightType)
	{
		if (ValidateRealType(leftType, rightType) is String s)
			return s;
		var rightConverted = MpzT.Abs(right.ToNumber());
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
			else if (rightConverted >= 65536)
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

	public static String GetRemainderType(String leftType, object right, String rightType)
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
			else if (rightConverted <= 32768)
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
}
