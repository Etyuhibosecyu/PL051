global using NStar.Mpir;
global using RedStarMath;
global using static PL051.NStar.NStarType;

namespace PL051.NStar;

public static class TranslateTimeOperations
{
	public static object BitwiseAnd(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (leftType is NullString)
			leftType = rightType;
		else if (rightType is NullString)
			rightType = leftType;
		if (leftType is "bool" & rightType is "bool")
			return left.ToBool() & right.ToBool();
		return left.ToNumber() & right.ToNumber();
	}

	public static object BitwiseOr(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (leftType is NullString)
			leftType = rightType;
		else if (rightType is NullString)
			rightType = leftType;
		if (leftType is "bool" & rightType is "bool")
			return left.ToBool() | right.ToBool();
		return left.ToNumber() | right.ToNumber();
	}

	public static object BitwiseXor(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (leftType is NullString)
			leftType = rightType;
		else if (rightType is NullString)
			rightType = leftType;
		if (leftType is "bool" & rightType is "bool")
			return left.ToBool() ^ right.ToBool();
		return left.ToNumber() ^ right.ToNumber();
	}

	public static object CyclicShift(this object data, int shiftAmount)
	{
		var basicType = data.GetBasicNStarType();
		if (basicType is ByteTypeName)
			return (byte)((byte)data.ToNumber() is var y
				? y << (int)unchecked((uint)shiftAmount % (sizeof(byte) * 8))
				| y >>> (int)unchecked((uint)-shiftAmount % (sizeof(byte) * 8)) : 0);
		else if (basicType is ShortIntTypeName)
			return (short)((short)data.ToNumber() is var si
				? si << (int)unchecked((uint)shiftAmount % (sizeof(short) * 8))
				| si >>> (int)unchecked((uint)-shiftAmount % (sizeof(short) * 8)) : 0);
		else if (basicType is UnsignedShortIntTypeName)
			return (ushort)((ushort)data.ToNumber() is var usi
				? usi << (int)unchecked((uint)shiftAmount % (sizeof(ushort) * 8))
				| usi >>> (int)unchecked((uint)-shiftAmount % (sizeof(ushort) * 8)) : 0);
		else if (basicType is IntTypeName)
			return (int)data.ToNumber() is var i
				? i << (int)unchecked((uint)shiftAmount % (sizeof(int) * 8))
				| i >>> (int)unchecked((uint)-shiftAmount % (sizeof(int) * 8)) : 0;
		else if (basicType is UnsignedIntTypeName)
			return (uint)data.ToNumber() is var ui
				? ui << (int)unchecked((uint)shiftAmount % (sizeof(uint) * 8))
				| ui >>> (int)unchecked((uint)-shiftAmount % (sizeof(uint) * 8)) : 0;
		else if (basicType is LongIntTypeName)
			return (long)data.ToNumber() is var li
				? li << (int)unchecked((uint)shiftAmount % (sizeof(long) * 8))
				| li >>> (int)unchecked((uint)-shiftAmount % (sizeof(long) * 8)) : 0;
		else if (basicType is UnsignedLongIntTypeName)
			return (ulong)data.ToNumber() is var uli
				? uli << (int)unchecked((uint)shiftAmount % (sizeof(ulong) * 8))
				| uli >>> (int)unchecked((uint)-shiftAmount % (sizeof(ulong) * 8)) : 0;
		else if (basicType is RealTypeName)
			return data.ToReal().Shift(shiftAmount);
		else if (IsNumeric(basicType))
			return data.ToNumber() << shiftAmount;
		else
			return 0;
	}

	public static object Divide(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (NeedsMpzT(leftType, rightType))
			return (MpzT)left.ToNumber() / right.ToNumber();
		else
			return left.ToNumber() / right.ToNumber();
	}

	public static object Minus(this object data)
	{
		var basicType = data.GetBasicNStarType();
		if (basicType is ByteTypeName or ShortIntTypeName)
			return (short)-data.ToNumber();
		else
			return -data.ToNumber();
	}

	public static object Modulo(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (NeedsMpzT(leftType, rightType))
			return (MpzT)left.ToNumber() % right.ToNumber();
		else
			return left.ToNumber() % right.ToNumber();
	}

	public static object Multiply(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (leftType is NullString)
			leftType = rightType;
		else if (rightType is NullString)
			rightType = leftType;
		if (leftType is StringTypeName && rightType is StringTypeName)
			return 0;
		if (leftType is StringTypeName)
			return left.ToString(false).Repeat((int)(uint)right.ToNumber());
		else if (rightType is StringTypeName)
			return right.ToString(false).Repeat((int)(uint)left.ToNumber());
		else if (NeedsMpzT(leftType, rightType))
			return (MpzT)left.ToNumber() * right.ToNumber();
		else
			return left.ToNumber() * right.ToNumber();
	}

	private static bool NeedsMpzT(System.ReadOnlySpan<char> leftType, System.ReadOnlySpan<char> rightType) =>
		leftType is ShortIntTypeName or IntTypeName or LongIntTypeName && rightType is UnsignedLongIntTypeName
				|| leftType is UnsignedLongIntTypeName && rightType is ShortIntTypeName or IntTypeName or LongIntTypeName;

	public static object Not(this object data)
	{
		var basicType = data.GetBasicNStarType();
		return !data.ToBool() && basicType is BoolTypeName;
	}

	public static object Plus(this object data)
	{
		var basicType = data.GetBasicNStarType();
		if (basicType is ByteTypeName)
			return (byte)+data.ToNumber();
		else if (basicType is ShortIntTypeName)
			return (short)+data.ToNumber();
		else if (basicType is UnsignedShortIntTypeName)
			return (ushort)+data.ToNumber();
		else
			return +data.ToNumber();
	}

	public static object Plus(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (leftType is NullString)
			leftType = rightType;
		else if (rightType is NullString)
			rightType = leftType;
		if (leftType is NullString)
			return null!;
		else if (leftType is StringTypeName || rightType is StringTypeName)
			return left.ToString(false).Concat(right.ToString(false));
		else if (NeedsMpzT(leftType, rightType))
			return (MpzT)left.ToNumber() + right.ToNumber();
		else
			return left.ToNumber() + right.ToNumber();
	}

	public static object ShiftLeft(this object left, int right)
	{
		var basicType = left.GetBasicNStarType();
		return basicType switch
		{
			DecimalTypeName => ((decimal)left.ToNumber()).ShiftDec(right),
			RealTypeName => left.ToReal().Shift(right),
			_ => left.ToNumber() << right
		};
	}

	public static object ShiftRight(this object left, int right)
	{
		var basicType = left.GetBasicNStarType();
		return basicType switch
		{
			DecimalTypeName => ((decimal)left.ToNumber()).ShiftDec(-right),
			RealTypeName => left.ToReal().Shift(-right),
			_ => left.ToNumber() >> right
		};
	}

	public static object ShiftRightUnsigned(this object left, int right)
	{
		var basicType = left.GetBasicNStarType();
		return basicType switch
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
			IntTypeName => (dynamic)((int)left.ToNumber() >>> right),
			UnsignedShortIntTypeName => (ushort)left.ToNumber() >>> right,
			ShortIntTypeName => (short)left.ToNumber() >>> right,
			ByteTypeName => (byte)left.ToNumber() >>> right,
			_ => new object()
		};
	}

	public static object Subtract(this object left, object right)
	{
		var leftType = left.GetBasicNStarType();
		var rightType = right.GetBasicNStarType();
		if (NeedsMpzT(leftType, rightType))
			return (MpzT)left.ToNumber() - right.ToNumber();
		else
			return left.ToNumber() - right.ToNumber();
	}

	public static object Tilde(this object data)
	{
		var basicType = data.GetBasicNStarType();
		return basicType switch
		{
			ByteTypeName => (byte)~data.ToNumber(),
			ShortIntTypeName => (short)~data.ToNumber(),
			UnsignedShortIntTypeName => (ushort)~data.ToNumber(),
			DecimalTypeName or RealTypeName => -1 - data.ToNumber(),
			_ => ~data.ToNumber()
		};
	}
}
