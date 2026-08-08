global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using static PL051.NStar.NStarType;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NStar.BufferLib;
using NStar.MathLib.Extras;
using NStar.Mpir;
using NStar.ParallelHS;
using NStar.RemoveDoubles;
using NStar.SortedSets;
using NStar.SumCollections;
using NStar.TreeSets;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using ReactiveUI;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PL051.NStar;

public sealed class LexemTree(char @char, ImmutableArray<LexemTree> nextTree, bool allowAll = false, bool allowNone = true)
{
	public char Char { get; set; } = @char;
	public ImmutableArray<LexemTree> NextTree { get; set; } = nextTree;
	public bool AllowAll { get; set; } = allowAll;
	public bool AllowNone { get; set; } = allowNone;

	public LexemTree(char @char) : this(@char, [])
	{
	}

	public static implicit operator LexemTree(char x) => new(x);
}

public static class BuiltInMemberCollections
{
	public const string Abstract = "abstract";
	public const string ElseIf = "else if";
	public const string ElseIfNot = "else if!";
	public const string False = "false";
	public const string LoopWhile = "loop-while";
	public const string RepeatString = "repeat";
	public const string ReturnString = "return";
	public const string Sealed = "sealed";
	public const string Static = "static";
	public const string SystemCollections = "System.Collections";
	public const string SystemGUI = "System.GUI";
	public const string SystemGlobalization = "System.Globalization";
	public const string WhileString = "while";
	public const string WhileNot = "while!";
	private const string ImplicitConversion = "implicit conversion";
	private const string RealOrLongReal = "real or long real";
	private const string SystemIFloatNumber = "System.IFloatNumber";
	private const string SystemINumber = "System.INumber";
	private const string SystemIO = "System.IO";
	private const string SystemNet = "System.Net";
	private const string SystemText = "System.Text";
	private const string SystemThreading = "System.Threading";
	private const string SystemUnsafe = "System.Unsafe";
	private static readonly List<String> NoExtraTypes = [];
	private static readonly List<String> ExtraTypesT = ["T"];
	private static readonly BlockStack ExtendedTypeBool = new([new(BlockType.Primitive, BoolTypeName, 1)]);
	private static readonly BlockStack ExtendedTypeIFloatNumber = new([new(BlockType.Interface, "IFloatNumber", 1)]);
	private static readonly BlockStack ExtendedTypeIIncreasable = new([new(BlockType.Interface, "IIncreasable", 1)]);
	private static readonly BlockStack ExtendedTypeIIntegerNumber = new([new(BlockType.Interface, "IIntegerNumber", 1)]);
	private static readonly BlockStack ExtendedTypeINumber = new([new(BlockType.Interface, "INumber", 1)]);
	private static readonly BlockStack ExtendedTypeISignedIntegerNumber = new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]);
	private static readonly BlockStack ExtendedTypeInt = new([new(BlockType.Primitive, IntTypeName, 1)]);
	private static readonly BlockStack ExtendedTypeList = new([new(BlockType.Primitive, "list", 1)]);
	private static readonly BlockStack ExtendedTypeString = new([new(BlockType.Primitive, StringTypeName, 1)]);
	private static readonly NStarType NStarTypeT = new(new([new(BlockType.Extra, "T", 1)]), NoBranches);
	private static readonly BranchCollection BranchCollectionT = [new("type", 0, []) { Extra = NStarTypeT }];
	private static readonly NStarType CharListType = GetListType(CharType);
	private static readonly NStarType NStarTypeIFloatNumberT = new(ExtendedTypeIFloatNumber, BranchCollectionT);
	private static readonly NStarType NStarTypeIIncreasableT = new(ExtendedTypeIIncreasable, BranchCollectionT);
	private static readonly NStarType NStarTypeIIntegerNumberT = new(ExtendedTypeIIntegerNumber, BranchCollectionT);
	private static readonly NStarType NStarTypeINumberT = new(ExtendedTypeINumber, BranchCollectionT);
	private static readonly NStarType NStarTypeISignedIntegerNumberT = new(ExtendedTypeISignedIntegerNumber, BranchCollectionT);
	private static readonly ExtendedMethodParameter ExtendedParameterStringS = new(StringType, "s", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString1 = new(StringType, "string1", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString2 = new(StringType, "string2", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString3 = new(StringType, "string3", ParameterAttributes.None, []);

	public static SortedSet<String> Keywords { get; } = new(
		"_", Abstract, "break", "Class", "const", "Constructor", "continue",
		"Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern",
		False, "for", "Function", "if", "Interface", "internal", "lock", "loop",
		"Megaclass", "multiconst", "Namespace", "new", NullString, "Operator", "out",
		"params", "private", "protected", "public", "readonly", "ref", RepeatString, ReturnString,
		Sealed, Static, "Struct", "switch", "this", "throw", "true", "using", WhileString
	);

	public static SortedSet<String> EscapedKeywords { get; } = new(
		Abstract, "as", "base", BoolTypeName, "break", ByteTypeName,
		"case", "catch", CharTypeName, "checked", "class", "const", "continue",
		DecimalTypeName, DefaultConst, "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
		False, "finally", "fixed", "float", "for", "foreach", "goto",
		"if", "implicit", "in", IntTypeName, "interface", "internal", "is",
		"lock", "long", "namespace", "new", NullString, ObjectTypeName, "operator", "out", "override",
		"params", "private", "protected", "public", "readonly", "ref", ReturnString,
		"sbyte", Sealed, "short", "sizeof", "stackalloc", Static, StringTypeName, "struct", "switch",
		"this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
		"virtual", "void", "volatile", WhileString
	);

	public static ImmutableArray<string> AssignmentOperators { get; } = ImmutableArray.Create("=", "+=", "-=", "*=", "/=", "%=", "pow=", "&=", "|=", "^=", ">>=", "<<=");

	public static ImmutableArray<string> TernaryOperators { get; } = ImmutableArray.Create("?", "?=", "?>", "?<", "?>=", "?<=", "?!=");

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type and ExtraTypes.
	/// </summary>
	public static TypeSortedList<TypeVariables> Variables { get; } = [];

	public static SortedSet<String> Namespaces { get; } = new(SystemName, SystemCollections, SystemText, SystemThreading);

	public static SortedSet<String> IONamespaces { get; } = new(SystemGUI, SystemIO, SystemNet);

	public static G.HashSet<String> ImportedNamespaces { get; } = [];

	public static G.HashSet<String> UserDefinedNamespaces { get; } = [];

	public static SortedSet<String> ExplicitlyConnectedNamespaces { get; } = [];

	public static SortedDictionary<String, Type> PrimitiveTypes { get; } = new()
	{
		{ NullString, typeof(void) }, { ObjectTypeName, typeof(object) }, { BoolTypeName, typeof(bool) },
		{ ByteTypeName, typeof(byte) }, { ShortCharTypeName, typeof(byte) },
		{ ShortIntTypeName, typeof(short) }, { UnsignedShortIntTypeName, typeof(ushort) },
		{ CharTypeName, typeof(char) }, { IntTypeName, typeof(int) }, { UnsignedIntTypeName, typeof(uint) },
		{ LongCharTypeName, typeof(uint) }, { LongIntTypeName, typeof(long) },
		{ nameof(DateTime), typeof(DateTime) }, { nameof(TimeSpan), typeof(TimeSpan) },
		{ UnsignedLongIntTypeName, typeof(long) }, { RealTypeName, typeof(double) },
		{ LongLongTypeName, typeof(MpzT) }, { UnsignedLongLongTypeName, typeof(MpuT) },
		{ DecimalTypeName, typeof(decimal) }, { ComplexTypeName, typeof(RedStarMath.Complex) },
		{ RecursiveTypeName, typeof(Type) }, { StringTypeName, typeof(String) },
		{ "index", typeof(Index) }, { "range", typeof(Range) },
		{ "nint", typeof(nint) }, { "list", typeof(List<>) }, { "dynamic", typeof(void) }, { "var", typeof(void) },
	};

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static Mirror<(String Namespace, String Type), Type> ExtraTypes { get; } = new()
	{
		{ ([], nameof(IComparable<>)), typeof(IComparable<>) },
		{ ([], nameof(IEquatable<>)), typeof(IEquatable<>) },
		{ ("Environment", nameof(Environment.SpecialFolder)), typeof(Environment.SpecialFolder) },
		{ ("Environment", nameof(Environment.SpecialFolderOption)), typeof(Environment.SpecialFolderOption) },
		{ (SystemName, nameof(ArgumentException)), typeof(ArgumentException) },
		{ (SystemName, nameof(ArgumentNullException)), typeof(ArgumentNullException) },
		{ (SystemName, nameof(ArgumentOutOfRangeException)), typeof(ArgumentOutOfRangeException) },
		{ (SystemName, nameof(ArithmeticException)), typeof(ArithmeticException) },
		{ (SystemName, nameof(BitConverter)), typeof(BitConverter) },
		{ (SystemName, nameof(Convert)), typeof(Convert) },
		{ (SystemName, nameof(DateTimeKind)), typeof(DateTimeKind) },
		{ (SystemName, nameof(DayOfWeek)), typeof(DayOfWeek) },
		{ (SystemName, nameof(DivideByZeroException)), typeof(DivideByZeroException) },
		{ (SystemName, nameof(Environment)), typeof(Environment) },
		{ (SystemName, nameof(EventArgs)), typeof(EventArgs) },
		{ (SystemName, nameof(EventHandler)), typeof(EventHandler<>) },
		{ (SystemName, nameof(Exception)), typeof(Exception) },
		{ (SystemName, nameof(FormatException)), typeof(FormatException) },
		{ (SystemName, "IFloatNumber"), typeof(IFloatingPointConstants<>) },
		{ (SystemName, "IIntegerNumber"), typeof(IBinaryInteger<>) },
		{ (SystemName, nameof(IndexOutOfRangeException)), typeof(IndexOutOfRangeException) },
		{ (SystemName, nameof(INumber<>)), typeof(INumberBase<>) },
		{ (SystemName, nameof(InvalidCastException)), typeof(InvalidCastException) },
		{ (SystemName, nameof(InvalidOperationException)), typeof(InvalidOperationException) },
		{ (SystemName, nameof(ISignedNumber<>)), typeof(ISignedNumber<>) },
		{ (SystemName, nameof(IUnsignedNumber<>)), typeof(IUnsignedNumber<>) },
		{ (SystemName, nameof(RedStarMath.Math)), typeof(RedStarMath.Math) },
		{ (SystemName, nameof(NotImplementedException)), typeof(NotImplementedException) },
		{ (SystemName, nameof(NotSupportedException)), typeof(NotSupportedException) },
		{ (SystemName, nameof(NullReferenceException)), typeof(NullReferenceException) },
		{ (SystemName, nameof(ObjectDisposedException)), typeof(ObjectDisposedException) },
		{ (SystemName, nameof(OverflowException)), typeof(OverflowException) },
		{ (SystemName, nameof(PlatformNotSupportedException)), typeof(PlatformNotSupportedException) },
		{ (SystemName, nameof(Predicate<>)), typeof(Predicate<>) },
		{ (SystemName, nameof(ReadOnlySpan<>)), typeof(ReadOnlySpan<>) },
		{ (SystemName, nameof(RedStarLinq)), typeof(RedStarLinq) },
		{ (SystemName, nameof(RedStarLinqExtras)), typeof(RedStarLinqExtras) },
		{ (SystemName, nameof(RedStarLinqParallel)), typeof(RedStarLinqParallel) },
		{ (SystemName, nameof(RedStarLinqMath)), typeof(RedStarLinqMath) },
		{ (SystemName, nameof(RedStarLinqMathExtras)), typeof(RedStarLinqMathExtras) },
		{ (SystemName, nameof(RedStarLinqRemoveDoubles)), typeof(RedStarLinqRemoveDoubles) },
		{ (SystemName, nameof(Span<>)), typeof(Span<>) },
		{ (SystemName, nameof(SystemException)), typeof(SystemException) },
		{ (SystemCollections, nameof(Chain)), typeof(Chain) },
		{ (SystemCollections, nameof(Dictionary<,>)), typeof(Dictionary<,>) },
		{ (SystemCollections, nameof(G.KeyNotFoundException)), typeof(G.KeyNotFoundException) },
		{ (SystemCollections, nameof(ListHashSet<>)), typeof(ListHashSet<>) },
		{ (SystemCollections, nameof(Slice<>)), typeof(Slice<>) },
		{ (SystemCollections, nameof(ValueNotFoundException)), typeof(ValueNotFoundException) },
		{ (SystemGlobalization, nameof(IFormatProvider)), typeof(IFormatProvider) },
		{ (SystemGlobalization, nameof(NumberStyles)), typeof(NumberStyles) },
		{ (SystemIO, nameof(DirectoryNotFoundException)), typeof(DirectoryNotFoundException) },
		{ (SystemIO, nameof(DriveNotFoundException)), typeof(DriveNotFoundException) },
		{ (SystemIO, nameof(EndOfStreamException)), typeof(EndOfStreamException) },
		{ (SystemIO, nameof(FileLoadException)), typeof(FileLoadException) },
		{ (SystemIO, nameof(FileNotFoundException)), typeof(FileNotFoundException) },
		{ (SystemIO, nameof(IOException)), typeof(IOException) },
		{ (SystemIO, nameof(PathTooLongException)), typeof(PathTooLongException) },
		{ (SystemNet, nameof(SocketException)), typeof(SocketException) },
		{ (SystemNet, nameof(WebException)), typeof(WebException) },
		{ (SystemText, nameof(ASCIIEncoding)), typeof(ASCIIEncoding) },
		{ (SystemText, nameof(CodePagesEncodingProvider)), typeof(CodePagesEncodingProvider) },
		{ (SystemText, nameof(DecoderFallbackException)), typeof(DecoderFallbackException) },
		{ (SystemText, nameof(EncoderFallbackException)), typeof(EncoderFallbackException) },
		{ (SystemText, nameof(Encoding)), typeof(Encoding) },
		{ (SystemText, nameof(UnicodeEncoding)), typeof(UnicodeEncoding) },
		{ (SystemText, nameof(UTF32Encoding)), typeof(UTF32Encoding) },
		{ (SystemText, nameof(UTF7Encoding)), typeof(UTF7Encoding) },
		{ (SystemText, nameof(UTF8Encoding)), typeof(UTF8Encoding) },
		{ (SystemThreading, nameof(CancellationToken)), typeof(CancellationToken) },
		{ (SystemThreading, nameof(CancellationTokenSource)), typeof(CancellationTokenSource) },
		{ (SystemThreading, nameof(Parallel)), typeof(Parallel) },
		{ (SystemThreading, nameof(ParallelLoopResult)), typeof(ParallelLoopResult) },
		{ (SystemThreading, nameof(ParallelLoopState)), typeof(ParallelLoopState) },
		{ (SystemThreading, nameof(Task<>)), typeof(Task<>) },
		{ (SystemThreading, nameof(TaskAwaiter<>)), typeof(TaskAwaiter<>) },
		{ (SystemThreading, nameof(ValueTask<>)), typeof(ValueTask<>) },
		{ (SystemThreading, nameof(ValueTaskAwaiter<>)), typeof(ValueTaskAwaiter<>) },
		{ (SystemUnsafe, "EmptyTask"), typeof(Task) },
		{ (SystemUnsafe, nameof(FuncDictionary<,>)), typeof(FuncDictionary<,>) },
		{ (SystemUnsafe, "#Half#"), typeof(Half) },
		{ (SystemUnsafe, "#Int128#"), typeof(Int128) },
		{ (SystemUnsafe, nameof(Memory<>)), typeof(Memory<>) },
		{ (SystemUnsafe, nameof(ReadOnlyMemory<>)), typeof(ReadOnlyMemory<>) },
		{ (SystemUnsafe, "#Single#"), typeof(float) },
		{ (SystemUnsafe, "#UInt128#"), typeof(UInt128) },
		{ (SystemUnsafe, "UnsafeString"), typeof(string) },
		{ (SystemUnsafe, "ValueEmptyTask"), typeof(ValueTask) },
	};

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static Mirror<(String Namespace, String Type), Type> IOTypes { get; } = new()
	{
		{ (SystemName, nameof(RedStarLinqDictionaries)), typeof(RedStarLinqDictionaries) },
		{ (SystemCollections, nameof(BaseDictionary<,,>)), typeof(BaseDictionary<,,>) },
		{ (SystemCollections, nameof(BaseHashSet<,>)), typeof(BaseHashSet<,>) },
		{ (SystemCollections, nameof(BaseIndexable<,>)), typeof(BaseIndexable<,>) },
		{ (SystemCollections, nameof(BaseList<,>)), typeof(BaseList<,>) },
		{ (SystemCollections, nameof(BaseSet<,>)), typeof(BaseSet<,>) },
		{ (SystemCollections, nameof(BaseSortedSet<,>)), typeof(BaseSortedSet<,>) },
		{ (SystemCollections, nameof(BaseSumList<,>)), typeof(BaseSumList<int, SumList>) },
		{ (SystemCollections, nameof(Buffer)), typeof(Buffer<>) },
		{ (SystemCollections, nameof(Comparer<>)), typeof(Comparer<>) },
		{ (SystemCollections, nameof(EComparer<>)), typeof(EComparer<>) },
		{ (SystemCollections, nameof(Extents)), typeof(Extents) },
		{ (SystemCollections, nameof(FastDelHashSet<>)), typeof(FastDelHashSet<>) },
		{ (SystemCollections, nameof(Group<,>)), typeof(Group<,>) },
		{ (SystemCollections, nameof(G.LinkedList<>)), typeof(G.LinkedList<>) },
		{ (SystemCollections, nameof(G.LinkedListNode<>)), typeof(G.LinkedListNode<>) },
		{ (SystemCollections, nameof(LimitedQueue<>)), typeof(LimitedQueue<>) },
		{ (SystemCollections, nameof(ListEComparer<>)), typeof(ListEComparer<>) },
		{ (SystemCollections, nameof(ListOfBigSums)), typeof(ListOfBigSums) },
		{ (SystemCollections, nameof(Mirror<,>)), typeof(Mirror<,>) },
		{ (SystemCollections, nameof(ParallelHashSet<>)), typeof(ParallelHashSet<>) },
		{ (SystemCollections, nameof(Queue<>)), typeof(Queue<>) },
		{ (SystemCollections, nameof(SortedDictionary<,>)), typeof(SortedDictionary<,>) },
		{ (SystemCollections, nameof(SortedSet<>)), typeof(SortedSet<>) },
		{ (SystemCollections, nameof(Stack<>)), typeof(Stack<>) },
		{ (SystemCollections, nameof(SumList)), typeof(SumList) },
		{ (SystemCollections, nameof(SumSet<>)), typeof(SumSet<>) },
		{ (SystemCollections, nameof(TreeHashSet<>)), typeof(TreeHashSet<>) },
		{ (SystemCollections, nameof(TreeSet<>)), typeof(TreeSet<>) },
		{ (SystemGUI, nameof(Animatable)), typeof(Animatable) },
		{ (SystemGUI, nameof(AutoCompleteBox)), typeof(AutoCompleteBox) },
		{ (SystemGUI, nameof(AvaloniaObject)), typeof(AvaloniaObject) },
		{ (SystemGUI, nameof(Bitmap)), typeof(Bitmap) },
		{ (SystemGUI, nameof(Border)), typeof(Border) },
		{ (SystemGUI, nameof(Brush)), typeof(Brush) },
		{ (SystemGUI, nameof(Button)), typeof(Button) },
		{ (SystemGUI, nameof(Canvas)), typeof(Canvas) },
		{ (SystemGUI, nameof(CheckBox)), typeof(CheckBox) },
		{ (SystemGUI, nameof(Color)), typeof(Color) },
		{ (SystemGUI, nameof(ColumnDefinition)), typeof(ColumnDefinition) },
		{ (SystemGUI, nameof(ColumnDefinitions)), typeof(ColumnDefinitions) },
		{ (SystemGUI, nameof(ComboBox)), typeof(ComboBox) },
		{ (SystemGUI, nameof(Control)), typeof(Control) },
		{ (SystemGUI, nameof(Controls)), typeof(Controls) },
		{ (SystemGUI, nameof(Decorator)), typeof(Decorator) },
		{ (SystemGUI, nameof(DefinitionBase)), typeof(DefinitionBase) },
		{ (SystemGUI, nameof(DockPanel)), typeof(DockPanel) },
		{ (SystemGUI, nameof(Expander)), typeof(Expander) },
		{ (SystemGUI, nameof(Grid)), typeof(Grid) },
		{ (SystemGUI, nameof(GridLength)), typeof(GridLength) },
		{ (SystemGUI, nameof(GridSplitter)), typeof(GridSplitter) },
		{ (SystemGUI, nameof(GridUnitType)), typeof(GridUnitType) },
		{ (SystemGUI, nameof(GUIWindow)), typeof(GUIWindow) },
		{ (SystemGUI, nameof(HeaderedContentControl)), typeof(HeaderedContentControl) },
		{ (SystemGUI, nameof(HorizontalAlignment)), typeof(HorizontalAlignment) },
		{ (SystemGUI, nameof(IBrush)), typeof(IBrush) },
		{ (SystemGUI, nameof(IImage)), typeof(IImage) },
		{ (SystemGUI, nameof(Image)), typeof(Image) },
		{ (SystemGUI, nameof(ImageBrush)), typeof(ImageBrush) },
		{ (SystemGUI, nameof(IResourceDictionary)), typeof(IResourceDictionary) },
		{ (SystemGUI, nameof(ItemsControl)), typeof(ItemsControl) },
		{ (SystemGUI, nameof(KeyEventArgs)), typeof(KeyEventArgs) },
		{ (SystemGUI, nameof(Panel)), typeof(Panel) },
		{ (SystemGUI, nameof(PixelPoint)), typeof(PixelPoint) },
		{ (SystemGUI, nameof(Point)), typeof(Point) },
		{ (SystemGUI, nameof(PointerEventArgs)), typeof(PointerEventArgs) },
		{ (SystemGUI, nameof(PointerPressedEventArgs)), typeof(PointerPressedEventArgs) },
		{ (SystemGUI, nameof(PointerReleasedEventArgs)), typeof(PointerReleasedEventArgs) },
		{ (SystemGUI, nameof(RadioButton)), typeof(RadioButton) },
		{ (SystemGUI, nameof(RangeBase)), typeof(RangeBase) },
		{ (SystemGUI, nameof(RelativePanel)), typeof(RelativePanel) },
		{ (SystemGUI, nameof(RoutedEventArgs)), typeof(RoutedEventArgs) },
		{ (SystemGUI, nameof(RowDefinition)), typeof(RowDefinition) },
		{ (SystemGUI, nameof(RowDefinitions)), typeof(RowDefinitions) },
		{ (SystemGUI, nameof(ScrollViewer)), typeof(ScrollViewer) },
		{ (SystemGUI, nameof(SelectingItemsControl)), typeof(SelectingItemsControl) },
		{ (SystemGUI, nameof(Slider)), typeof(Slider) },
		{ (SystemGUI, nameof(SolidColorBrush)), typeof(SolidColorBrush) },
		{ (SystemGUI, nameof(SplitView)), typeof(SplitView) },
		{ (SystemGUI, nameof(StackPanel)), typeof(StackPanel) },
		{ (SystemGUI, nameof(TabControl)), typeof(TabControl) },
		{ (SystemGUI, nameof(TemplatedControl)), typeof(TemplatedControl) },
		{ (SystemGUI, nameof(TextBlock)), typeof(TextBlock) },
		{ (SystemGUI, nameof(TextBox)), typeof(TextBox) },
		{ (SystemGUI, nameof(Thumb)), typeof(Thumb) },
		{ (SystemGUI, nameof(TileBrush)), typeof(TileBrush) },
		{ (SystemGUI, nameof(ToggleButton)), typeof(ToggleButton) },
		{ (SystemGUI, nameof(ToolTip)), typeof(ToolTip) },
		{ (SystemGUI, nameof(UniformGrid)), typeof(UniformGrid) },
		{ (SystemGUI, nameof(VerticalAlignment)), typeof(VerticalAlignment) },
		{ (SystemGUI, nameof(Window)), typeof(Window) },
		{ (SystemGUI, nameof(WrapPanel)), typeof(WrapPanel) },
		{ (SystemGUI, nameof(ZoomBorder)), typeof(ZoomBorder) },
		{ (SystemIO, nameof(Directory)), typeof(Directory) },
		{ (SystemIO, nameof(DirectoryInfo)), typeof(DirectoryInfo) },
		{ (SystemIO, nameof(DriveInfo)), typeof(DriveInfo) },
		{ (SystemIO, nameof(DriveType)), typeof(DriveType) },
		{ (SystemIO, nameof(File)), typeof(File) },
		{ (SystemIO, nameof(FileAccess)), typeof(FileAccess) },
		{ (SystemIO, nameof(FileAttributes)), typeof(FileAttributes) },
		{ (SystemIO, nameof(FileInfo)), typeof(FileInfo) },
		{ (SystemIO, nameof(FileMode)), typeof(FileMode) },
		{ (SystemIO, nameof(FileOptions)), typeof(FileOptions) },
		{ (SystemIO, nameof(FileShare)), typeof(FileShare) },
		{ (SystemIO, nameof(FileStream)), typeof(FileStream) },
		{ (SystemIO, nameof(FileSystemInfo)), typeof(FileSystemInfo) },
		{ (SystemIO, nameof(MemoryStream)), typeof(MemoryStream) },
		{ (SystemIO, nameof(Path)), typeof(Path) },
		{ (SystemIO, nameof(Stream)), typeof(Stream) },
		{ (SystemIO, nameof(UnixFileMode)), typeof(UnixFileMode) },
		{ (SystemNet, nameof(AddressFamily)), typeof(AddressFamily) },
		{ (SystemNet, nameof(IPAddress)), typeof(IPAddress) },
		{ (SystemNet, nameof(IPEndPoint)), typeof(IPEndPoint) },
		{ (SystemNet, nameof(NetworkStream)), typeof(NetworkStream) },
		{ (SystemNet, nameof(TcpClient)), typeof(TcpClient) },
		{ (SystemNet, nameof(TcpListener)), typeof(TcpListener) },
	};

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static Mirror<(String Namespace, String Type), Type> ImportedTypes { get; } = [];

	/// <summary>
	/// Sorted by Container and Type, also contains RestrictionPackage modifiers, RestrictionTypes, RestrictionNames and Attributes.
	/// </summary>
	public static ExtendedTypesCollection ExtendedTypes { get; } = new(new BlockStackAndStringComparer())
	{
		{
			(new([new(BlockType.Namespace, SystemName, 1)]), nameof(Action)),
			([new(true, RecursiveType, "Types")], TypeAttributes.Delegate)
		},
		{
			(new([new(BlockType.Namespace, SystemName, 1)]), nameof(Func<>)),
			new([new(false, RecursiveType, "TReturn"), new(true, RecursiveType, "Types")], TypeAttributes.Delegate)
		}
	};

	/// <summary>
	/// Sorted by Container and Type, also contains Restrictions, Attributes, BaseType and Decomposition.
	/// </summary>
	public static Dictionary<(BlockStack Container, String Type), UserDefinedType> UserDefinedTypes { get; } = new(new BlockStackAndStringEComparer());

	/// <summary>
	/// Sorted by Container, then by Name, also contains Attributes, BaseType, StartPos and EndPos.
	/// </summary>
	public static TypeDictionary<ListHashSet<TempType>> TempTypes { get; } = [];

	public static TypeDictionary<ListHashSet<String>> UnnamedTypeStartIndexes { get; } = [];

	/// <summary>
	/// Sorted by tuple, contains Namespace, Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Interface), (List<String> ExtraTypes, Type DotNetType)> Interfaces { get; } = new()
	{
		{
			([], "IChar"), (ExtraTypesT, typeof(void))
		},
		{
			([], nameof(IComparable<>)), (ExtraTypesT, typeof(IComparable<>))
		},
		{
			([], "IComparableRaw"), (NoExtraTypes, typeof(IComparable))
		},
		{
			([], nameof(IConvertible)), (NoExtraTypes, typeof(IConvertible))
		},
		{
			([], nameof(IEquatable<>)), (ExtraTypesT, typeof(IEquatable<>))
		},
		{
			([], "IIncreasable"), (ExtraTypesT, typeof(IIncrementOperators<>))
		},
		{
			([], "IIntegerNumber"), (ExtraTypesT, typeof(IBinaryInteger<>))
		},
		{
			([], "INumber"), (ExtraTypesT, typeof(INumber<>))
		},
		{
			([], "IFloatNumber"), (ExtraTypesT, typeof(IFloatingPoint<>))
		},
		{
			([], "ISignedIntegerNumber"), (ExtraTypesT, typeof(ISignedNumber<>))
		},
		{
			([], "IUnsignedIntegerNumber"), (ExtraTypesT, typeof(IUnsignedNumber<>))
		},
		{
			(SystemCollections, nameof(ICollection)), (ExtraTypesT, typeof(ICollection<>))
		},
		{
			(SystemCollections, "ICollectionRaw"), (NoExtraTypes, typeof(System.Collections.ICollection))
		},
		{
			(SystemCollections, "IComparer"), (ExtraTypesT, typeof(G.IComparer<>))
		},
		{
			(SystemCollections, nameof(IDictionary)), (["TKey", "TValue"], typeof(G.IDictionary<,>))
		},
		{
			(SystemCollections, "IDictionaryRaw"), (NoExtraTypes, typeof(System.Collections.IDictionary))
		},
		{
			(SystemCollections, nameof(G.IEnumerable<>)), (ExtraTypesT, typeof(G.IEnumerable<>))
		},
		{
			(SystemCollections, "IEnumerableRaw"), (NoExtraTypes, typeof(System.Collections.IEnumerable))
		},
		{
			(SystemCollections, "IEqualityComparer"), (ExtraTypesT, typeof(G.IEqualityComparer<>))
		},
		{
			(SystemCollections, nameof(IList)), (ExtraTypesT, typeof(IList<>))
		},
		{
			(SystemCollections, "IListRaw"), (NoExtraTypes, typeof(G.IList<>))
		},
		{
			(SystemCollections, nameof(IReadOnlyList<>)), (ExtraTypesT, typeof(IReadOnlyList<>))
		},
		{
			(SystemCollections, "IReadOnlyListRaw"), (NoExtraTypes, typeof(G.IReadOnlyList<>))
		},
	};

	/// <summary>
	/// Sorted by Class, also contains Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String Interface, List<String> ExtraTypes)>> UserDefinedImplementedInterfaces { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeDictionary<UserDefinedTypeProperties> UserDefinedProperties { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains list of Names.
	/// </summary>
	public static TypeDictionary<List<String>> UserDefinedPropertiesOrder { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Index.
	/// </summary>
	public static TypeDictionary<Dictionary<String, int>> UserDefinedPropertiesMapping { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains IndexType, Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeSortedList<TypeIndexers> UserDefinedIndexers { get; } = [];

	/// <summary>
	/// Sorted by Name, also contains ExtraTypes, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterExtraTypes, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static FunctionsList PublicFunctions { get; } = new()
	{
		{
			"Abs", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Ceil", new(ExtraTypesT, IntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemIFloatNumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Chain", new(NoExtraTypes, "list", [IntTypeName], FunctionAttributes.Multiconst,
				[new(IntTypeName, "start", NoExtraTypes, ParameterAttributes.None, []), new(IntTypeName, "end", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Choose", new(NoExtraTypes, ObjectTypeName, NoExtraTypes, FunctionAttributes.None,
				[new(ObjectTypeName, "variants", NoExtraTypes, ParameterAttributes.Params, [])])
		},
		{
			"Clamp", new(ExtraTypesT, "INumber", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, []),
				new(SystemINumber, "min", ExtraTypesT, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MinValue;\")"),
				new(SystemINumber, "max", ExtraTypesT, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MaxValue;\")")])
		},
		{
			"Exp", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Fibonacci", new(NoExtraTypes, RealTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(IntTypeName, "n", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Fill", new(ExtraTypesT, "list", ExtraTypesT, FunctionAttributes.Multiconst,
				[new("T", "element", NoExtraTypes, ParameterAttributes.None, []), new(IntTypeName, "count", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Floor", new(ExtraTypesT, IntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Frac", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemIFloatNumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"IntRandom", new(NoExtraTypes, IntTypeName, NoExtraTypes, FunctionAttributes.None,
				[new(IntTypeName, "max", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"IntToReal", new(ExtraTypesT, RealTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IIntegerNumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"IsPrime", new(NoExtraTypes, BoolTypeName, NoExtraTypes, FunctionAttributes.None,
				[new(IntTypeName, "n", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Log", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(RealTypeName, "x", NoExtraTypes, ParameterAttributes.None, []),
				new(SystemINumber, "y", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Max", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("IComparable", "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Mean", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Min", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("IComparable", "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Q", new(NoExtraTypes, StringTypeName, NoExtraTypes, FunctionAttributes.None, [])
		},
		{
			"Random", new(NoExtraTypes, RealTypeName, NoExtraTypes, FunctionAttributes.None,
				[new(RealTypeName, "max", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"RealRemainder", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemIFloatNumber, "x", ExtraTypesT, ParameterAttributes.None, []),
				new(RealTypeName, "y", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"RGB", new(NoExtraTypes, IntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(ByteTypeName, "red", NoExtraTypes, ParameterAttributes.None, []),
				new(ByteTypeName, "green", NoExtraTypes, ParameterAttributes.None, []),
				new(ByteTypeName, "blue", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Round", new(ExtraTypesT, IntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemIFloatNumber, "x", ExtraTypesT, ParameterAttributes.None, []),
				new(IntTypeName, "digits_after_dot", NoExtraTypes, ParameterAttributes.Optional, "0")])
		},
		{
			"Sign", new(ExtraTypesT, ShortIntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Sqrt", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemINumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Truncate", new(ExtraTypesT, IntTypeName, NoExtraTypes, FunctionAttributes.Multiconst,
				[new(SystemIFloatNumber, "x", ExtraTypesT, ParameterAttributes.None, [])])
		}
	};

	/// <summary>
	/// Sorted by Container, then by Name, also contains Restrictions, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeSortedList<ExtendedMethods> ExtendedMethods { get; } = new()
	{
		{
			new(), new()
			{
				{ "ExecuteString", new() { new([], ObjectType, FunctionAttributes.Multiconst, [ExtendedParameterStringS, new(ObjectType, "parameters", ParameterAttributes.Params, [])]) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, then by Name, also contains Restrictions, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<UserDefinedMethods> UserDefinedMethods { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedFunctionIndexes { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<ConstructorOverloads> UserDefinedConstructors { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedConstructorIndexes { get; } = [];

	/// <summary>
	/// Sorted by Operator, also contains Postfix modifiers, ReturnTypes, ReturnNStarType.ExtraTypes, OpdTypes and OpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, UnaryOperatorClasses> UnaryOperators { get; } = new()
	{
		{
			"+", new(new BlockStackComparer())
			{
				{ ExtendedTypeINumber, new() { (false, NStarTypeT, NStarTypeINumberT) } }
			}
		},
		{
			"-", new(new BlockStackComparer())
			{
				{ ExtendedTypeISignedIntegerNumber, new() { (false, NStarTypeT, NStarTypeISignedIntegerNumberT) } },
				{ ExtendedTypeIFloatNumber, new() { (false, NStarTypeT, NStarTypeIFloatNumberT) } }
			}
		},
		{
			"++", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (false, NStarTypeT, NStarTypeIIncreasableT), (true, NStarTypeT, NStarTypeIIncreasableT) } }
			}
		},
		{
			"--", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (false, NStarTypeT, NStarTypeIIncreasableT), (true, NStarTypeT, NStarTypeIIncreasableT) } }
			}
		},
		{
			"!", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (false, BoolType, BoolType) } }
			}
		},
		{
			"!!", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack(BoolTypeName), new() { (true, BoolType, BoolType) } }
			}
		},
		{
			"~", new(new BlockStackComparer())
			{
				{ ExtendedTypeISignedIntegerNumber, new() { (false, NStarTypeT, NStarTypeISignedIntegerNumberT) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Operator, also contains ReturnTypes, ReturnNStarType.ExtraTypes, LeftOpdTypes and LeftOpdExtraTypes, RightOpdTypes and RightOpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, BinaryOperatorClasses> BinaryOperators { get; } = new()
	{
		{
			"+", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (NStarTypeT, NStarTypeIIncreasableT, IntType), (NStarTypeT, IntType, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } },
				{ ExtendedTypeString, new() { (StringType, CharType, StringType), (StringType, CharListType, StringType), (StringType, StringType, CharType), (StringType, StringType, CharListType), (StringType, StringType, StringType) } }
			}
		},
		{
			"-", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (NStarTypeT, NStarTypeIIncreasableT, IntType), (IntType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"*", new(new BlockStackComparer())
			{
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } },
				{ ExtendedTypeString, new() { (StringType, IntType, StringType), (StringType, StringType, IntType) } }
			}
		},
		{
			"/", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } },
				{ ExtendedTypeIFloatNumber, new() { (NStarTypeT, NStarTypeIFloatNumberT, NStarTypeIFloatNumberT) } }
			}
		},
		{
			"pow", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } },
				{ ExtendedTypeIFloatNumber, new() { (NStarTypeT, NStarTypeIFloatNumberT, NStarTypeIFloatNumberT) } } } },
		{
			"==", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack(ObjectTypeName), new() { (BoolType, ObjectType, ObjectType) } }
			}
		},
		{
			">", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"<", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			">=", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } } } },
		{
			"<=", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"!=", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack(ObjectTypeName), new() { (BoolType, ObjectType, ObjectType) } }
			}
		},
		{
			">>", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } }
			}
		},
		{
			"<<", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } }
			}
		},
		{
			"&", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"|", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"^", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"&&", new(new BlockStackComparer()) { { ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } } }
		},
		{
			"||", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } }
			}
		},
		{
			"^^", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, int Value)>> EnumConstants { get; } = new()
	{
		{
			new([new(BlockType.Namespace, SystemName, 1), new(BlockType.Enum, "DateTimeKind", 1)]), new()
			{
				("Local", (int)DateTimeKind.Local), ("Unspecified", (int)DateTimeKind.Unspecified),
				("UTC", (int)DateTimeKind.Utc)
			}
		},
		{
			new([new(BlockType.Namespace, SystemName, 1), new(BlockType.Enum, "DayOfWeek", 1)]), new()
			{
				("Friday", (int)DayOfWeek.Friday), ("Monday", (int)DayOfWeek.Monday), ("Saturday", (int)DayOfWeek.Saturday),
				("Sunday", (int)DayOfWeek.Sunday), ("Thursday", (int)DayOfWeek.Thursday),
				("Tuesday", (int)DayOfWeek.Tuesday), ("Wednesday", (int)DayOfWeek.Wednesday)
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<Dictionary<String, UserDefinedConstant>> UserDefinedConstants { get; } = [];

	/// <summary>
	/// Sorted by SrcType, also contains SrcNStarType.ExtraTypes, DestTypes and their DestNStarType.ExtraTypes.
	/// </summary>
	public static TypeSortedList<ImplicitConversions> ImplicitConversions { get; } = new()
	{
		{
			ExtendedTypeBool, new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedIntType, false), (IntType, false), (ShortIntType, false), (ByteType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(ByteTypeName), new()
			{
				{ NoBranches, new() { (UnsignedIntType, false), (IntType, false), (ShortIntType, false), (GetPrimitiveType(ShortCharTypeName), true), (BoolType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(CharTypeName), new() {
				{ NoBranches, new() { (UnsignedShortIntType, false), (StringType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(ComplexTypeName), new()
			{
				{ NoBranches, new() { } }
			}
		},
		{
			ExtendedTypeInt, new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (IndexType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true) } }
			}
		},
		{
			ExtendedTypeList, new()
			{
				{ [new("type", 0, []) { Extra = GetPrimitiveType(CharTypeName) }], new() { (StringType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(LongCharTypeName), new()
			{
				{ NoBranches, new() { (UnsignedIntType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(LongIntTypeName), new()
			{
				{ NoBranches, new() { (LongLongType, false), (UnsignedLongIntType, true), (BoolType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(LongLongTypeName), new()
			{
				{ NoBranches, new() { (BoolType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (UnsignedLongIntType, true), (UnsignedLongLongType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(UnsignedLongLongTypeName), new()
			{
				{ NoBranches, new() { (LongLongType, false), (BoolType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (UnsignedLongIntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(RealTypeName), new()
			{
				{ NoBranches, new() { (ComplexType, false), (BoolType, true), (UnsignedLongIntType, true), (LongIntType, true), (UnsignedIntType, true), (IntType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(ShortCharTypeName), new()
			{
				{ NoBranches, new() { (ByteType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(ShortIntTypeName), new()
			{
				{ NoBranches, new() { (LongIntType, false), (RealType, false), (IntType, false), (UnsignedShortIntType, false), (BoolType, true), (ByteType, true), (UnsignedIntType, true), (UnsignedLongIntType, true) } }
			}
		},
		{
			ExtendedTypeString, new()
			{
				{ NoBranches, new() { (CharListType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack(UnsignedIntTypeName), new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (IntType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(UnsignedLongIntTypeName), new()
			{
				{ NoBranches, new() { (UnsignedLongLongType, false), (BoolType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (LongIntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack(UnsignedShortIntTypeName), new()
			{
				{ NoBranches, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (BoolType, true), (ByteType, true), (ShortIntType, true) } }
			}
		},
	};

	/// <summary>
	/// Sorted by tuple, contains DestType and DestNStarType.ExtraTypes.
	/// </summary>
	public static List<NStarType> ImplicitConversionsFromAnything { get; } = [(GetPrimitiveBlockStack(ObjectTypeName), NoBranches), (GetPrimitiveBlockStack(NullString), NoBranches), GetListType(GetPrimitiveType("[this]"))];

	public static G.SortedSet<String> NotImplementedNamespaces { get; } = ["System.Diagnostics", "System.Globalization", "System.Runtime"];

	/// <summary>
	/// Sorted by Namespace, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedNamespaces { get; } = new()
	{
		{ "System.Collections.Generic", SystemCollections }, { "System.Linq", "RedStarLinq" },
		{ "System.Windows", SystemGUI }, { "System.Windows.Forms", SystemGUI }
	};

	public static G.SortedSet<String> ReservedNamespaces { get; } = [
		"Microsoft",
		"System.Activities", "System.AddIn", "System.CodeDom",
		"System.Collections.Concurrent", "System.Collections.ObjectModel", "System.Collections.Specialized",
		"System.ComponentModel", "System.Configuration",
		"System.Data", "System.Deployment", "System.Device",
		"System.Diagnostics.CodeAnalysis", "System.Diagnostics.Contracts", "System.Diagnostics.Design",
		"System.Diagnostics.Eventing", "System.Diagnostics.PerformanceData",
		"System.Diagnostics.SymbolStore", "System.Diagnostics.Tracing",
		"System.DirectoryServices", "System.Drawing", "System.Dynamic",
		"System.EnterpriseServices", "System.IdentityModel",
		"System.IO.Compression", "System.IO.IsolatedStorage", "System.IO.Log", "System.IO.MemoryMappedFiles",
		"System.IO.Packaging", "System.IO.Pipes", "System.IO.Ports",
		"System.Management", "System.Media", "System.Messaging", "System.Numerics",
		"System.Printing", "System.Reflection", "System.Resources",
		"System.Runtime.Caching", "System.Runtime.CompilerServices", "System.Runtime.ConstrainedExecution",
		"System.Runtime.DesignerServices", "System.Runtime.ExceptionServices", "System.Runtime.Hosting",
		"System.Runtime.InteropServices", "System.Runtime.Remoting",
		"System.Runtime.Serialization", "System.Runtime.Versioning",
		"System.Security", "System.ServiceModel", "System.ServiceProcess", "System.Speech", "System.StubHelpers",
		"System.Text.RegularExpressions", "System.Threading.Tasks",
		"System.Timers", "System.Transactions", "System.Web",
		"System.Windows.Annotations", "System.Windows.Automation", "System.Windows.Baml2006", "System.Windows.Controls",
		"System.Windows.Data", "System.Windows.Documents",
		"System.Windows.Forms.ComponentModel", "System.Windows.Forms.DataVisualization", "System.Windows.Forms.Design",
		"System.Windows.Forms.Interaction", "System.Windows.Forms.Layout",
		"System.Windows.Forms.PropertyGridInternal", "System.Windows.Forms.VisualStyles",
		"System.Windows.Ink", "System.Windows.Input", "System.Windows.Interop", "System.Windows.Markup", "System.Windows.Media",
		"System.Windows.Navigation", "System.Windows.Resources", "System.Windows.Shapes",
		"System.Windows.Threading", "System.Windows.Xps",
		"System.Workflow", "System.Xaml", "System.Xml", "Windows", "XamlGeneratedNamespace"
	];

	public static G.SortedSet<(String Namespace, String Type)> NotImplementedTypes { get; } = [
		([], LongComplexTypeName), ([], LongRealTypeName),
		(SystemName, "Delegate"), (SystemName, "Enum"), (SystemName, "Environment"), (SystemName, "OperatingSystem")
	];

	/// <summary>
	/// Sorted by Namespace and Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), String> OutdatedTypes { get; } = new()
	{
		{ ([], "*Exception"), "\"if error ...\"" },
		{ ([], "double"), RealOrLongReal }, { ([], "float"), RealOrLongReal },
		{ ([], "uint"), UnsignedIntTypeName }, { ([], "ulong"), UnsignedLongIntTypeName }, { ([], "ushort"), UnsignedShortIntTypeName },
		{ (SystemName, "Action"), "System.Func[null, ...]" }, { (SystemName, "Array"), "list" }, { (SystemName, "Boolean"), BoolTypeName },
		{ (SystemName, "Byte"), "byte (from the small letter)" },
		{ (SystemName, "Char"), "char (from the small letter), short char or long char" },
		{ (SystemName, "Console"), "labels and textboxes" },
		{ (SystemName, "ConsoleCancelEventArgs"), "TextBox.KeyDown, TextBox.KeyPress and TextBox.KeyUp" },
		{ (SystemName, "ConsoleCancelEventHandler"), "TextBox keyboard events" },
		{ (SystemName, "ConsoleColor"), "RichTextBox text color" }, { (SystemName, "ConsoleKey"), "other item enums" },
		{ (SystemName, "ConsoleKeyInfo"), "other item info classes" },
		{ (SystemName, "ConsoleModifiers"), "other item modifiers enums" },
		{ (SystemName, "ConsoleSpecialKey"), "other item enums" },
		{ (SystemName, "Double"), RealOrLongReal },
		{ (SystemName, "Int16"), ShortIntTypeName }, { (SystemName, "Int32"), IntTypeName }, { (SystemName, "Int64"), LongIntTypeName },
		{ (SystemName, "Object"), "object (from the small letter)" },
		{ (SystemName, "Random"), "Random(), IntRandom() etc." },
		{ (SystemName, "SByte"), "byte or short int" }, { (SystemName, "Single"), RealOrLongReal },
		{ (SystemName, "String"), "string (from the small letter)" },
		{ (SystemName, "Type"), RecursiveTypeName },
		{ (SystemName, "UInt16"), UnsignedShortIntTypeName }, { (SystemName, "UInt32"), UnsignedIntTypeName },
		{ (SystemName, "UInt64"), UnsignedLongIntTypeName }, { (SystemName, "Void"), NullString },
		{ (SystemCollections, "BitArray"), "BitList" },
		{ (SystemCollections, "HashSet"), "ListHashSet" }, { (SystemCollections, "Hashtable"), "Dictionary" },
		{ (SystemCollections, "KeyValuePair"), "tuples" }, { (SystemCollections, "SortedSet"), "SortedSet" }
	};

	public static G.SortedSet<(String Namespace, String Type)> ReservedTypes { get; } = [
		([], "*Attribute"), ([], "*Comparer"), ([], "*Enumerator"), ([], "*UriParser"), ([], DecimalTypeName),
		(SystemName, "ActivationContext"), (SystemName, "ActivationContext.ContextForm"), (SystemName, "Activator"),
		(SystemName, "AppContext"), (SystemName, "AppDomain"), (SystemName, "AppDomainInitializer"),
		(SystemName, "AppDomainManager"), (SystemName, "AppDomainManagerInitializationOptions"), (SystemName, "AppDomainSetup"),
		(SystemName, "ApplicationId"), (SystemName, "ApplicationIdentity"), (SystemName, "ArgIterator"), (SystemName, "ArraySegment"),
		(SystemName, "AssemblyLoadEventArgs"), (SystemName, "AsyncCallback"), (SystemName, "AttributeTargets"),
		(SystemName, "Base64FormattingOptions"), (SystemName, "BitConverter"), (SystemName, "Buffer"),
		(SystemName, "Comparison"), (SystemName, "ContextBoundObject"), (SystemName, "ContextStaticAttribute"),
		(SystemName, "Convert"), (SystemName, "Converter"), (SystemName, "CrossAppDomainDelegate"),
		(SystemName, "DateTimeOffset"), (SystemName, "DBNull"), (SystemName, "Decimal"),
		(SystemName, "EnvironmentVariableTarget"), (SystemName, "FormattableString"),
		(SystemName, "GC"), (SystemName, "GCCollectionMode"), (SystemName, "GCNotificationStatus"),
		(SystemName, "GenericUriParserOptions"), (SystemName, "Guid"),
		(SystemName, "IAppDomainSetup"), (SystemName, "IAsyncResult"), (SystemName, "ICloneable"), (SystemName, "ICustomFormattable"),
		(SystemName, "IDisposable"), (SystemName, "IFormatProvider"), (SystemName, "IFormattable"),
		(SystemName, "IObservable"), (SystemName, "IObserver"), (SystemName, "IProgress"), (SystemName, "IServiceProvider"),
		(SystemName, "Lazy"), (SystemName, "LoaderOptimization"), (SystemName, "LocalDataStoreSlot"),
		(SystemName, "MarshalByRefObject"), (SystemName, "Math"), (SystemName, "MidpointRounding"), (SystemName, "ModuleHandle"),
		(SystemName, "MulticastDelegate"), (SystemName, "Nullable"),
		(SystemName, "PlatformID"), (SystemName, "Progress"),
		(SystemName, "ResolveEventArgs"), (SystemName, "ResolveEventHandler"),
		(SystemName, "RuntimeArgumentHandle"), (SystemName, "RuntimeFieldHandle"),
		(SystemName, "RuntimeMethodHandle"), (SystemName, "RuntimeTypeHandle"),
		(SystemName, "StringComparer"), (SystemName, "StringComparison"), (SystemName, "StringSplitOptions"),
		(SystemName, "TimeZone"), (SystemName, "TimeZoneInfo"),
		(SystemName, "TimeZoneInfo.AdjustmentRule"), (SystemName, "TimeZoneInfo.TransitionTime"),
		(SystemName, "Tuple"), (SystemName, "TupleExtensions"), (SystemName, "TypeCode"), (SystemName, "TypedReference"),
		(SystemName, "UIntPtr"), (SystemName, "Uri"), (SystemName, "UriBuilder"), (SystemName, "UriComponents"),
		(SystemName, "UriFormat"), (SystemName, "UriHostNameType"), (SystemName, "UriIdnScope"),
		(SystemName, "UriKind"), (SystemName, "UriPartial"),
		(SystemName, "UriTemplate"), (SystemName, "UriTemplateEquivalenceComparer"),
		(SystemName, "UriTemplateMatch"), (SystemName, "UriTemplateTable"), (SystemName, "UriTypeConverter"),
		(SystemName, "ValueTuple"), (SystemName, "ValueType"), (SystemName, "Version"),
		(SystemName, "WeakReference"), (SystemName, "_AppDomain"),
		(SystemCollections, "ArrayList"), (SystemCollections, "CaseInsensitiveHashCodeProvider"),
		(SystemCollections, "CollectionBase"),
		(SystemCollections, "Dictionary.KeyCollection"), (SystemCollections, "Dictionary.ValueCollection"),
		(SystemCollections, "DictionaryBase"), (SystemCollections, "DictionaryEntry"),
		(SystemCollections, "IHashCodeProvider"), (SystemCollections, "IReadOnlyCollection"),
		(SystemCollections, "IReadOnlyDictionary"), (SystemCollections, "IReadOnlyList"), (SystemCollections, "ISet"),
		(SystemCollections, "IStructuralComparable"), (SystemCollections, "IStructuralEquatable"),
		(SystemCollections, "KeyedByTypeCollection"), (SystemCollections, "ReadOnlyCollectionBase"),
		(SystemCollections, "StructuralComparisons"), (SystemCollections, "SynchronizedCollection"),
		(SystemCollections, "SynchronizedKeyedCollection"), (SystemCollections, "SynchronizedReadOnlyCollection")
	];

	public static G.SortedSet<String> NotImplementedTypeEnds { get; } = [];

	/// <summary>
	/// Sorted by Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedTypeEnds { get; } = new() { { "Exception", "\"if error ...\"" } };

	public static G.SortedSet<String> ReservedTypeEnds { get; } = ["Attribute", "Comparer", "Enumerator", "UriParser"];

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> NotImplementedMembers { get; } = new() { { new([new(BlockType.Interface, nameof(DateTime), 1)]), new() { "AddRange", "Subtract" } } };

	/// <summary>
	/// Sorted by Container, then by Member, also contains UseInstead.
	/// </summary>
	public static TypeSortedList<SortedDictionary<String, String>> OutdatedMembers { get; } = new()
	{
		{
			ExtendedTypeBool, new()
			{
				{ "FalseString", "literal \"false\"" }, { "Parse", ImplicitConversion },
				{ "TrueString", "literal \"true\"" }, { "TryParse", ImplicitConversion }
			}
		},
		{
			GetPrimitiveBlockStack(nameof(DateTime)), new()
			{
				{ "IsDaylightSavingTime", "IsSummertime" },
				{ "Parse", ImplicitConversion }, { "TryParse", ImplicitConversion }
			}
		},
		{
			ExtendedTypeINumber, new()
			{
				{ "Parse", ImplicitConversion }, { "TryParse", ImplicitConversion }
			}
		},
		{
			ExtendedTypeList, new()
			{
				{ "Length", "Length" }
			}
		},
		{
			GetPrimitiveBlockStack(ObjectTypeName), new()
			{
				{ "Equals", "==" }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> ReservedMembers { get; } = new()
	{
		{
			GetPrimitiveBlockStack(nameof(DateTime)), new()
			{
				"FromBinary", "FromFileTime", "FromFileTimeUtc", "FromOADate", "GetDateTimeFormats", "ParseExact",
				"ToFileTime", "ToFileTimeUtc", "ToLongDateString", "ToLongTimeString", "ToOADate",
				"ToShortDateString", "ToShortTimeString", "TryParseExact"
			}
		},
		{
			FuncBlockStack, new() { "BeginInvoke", "EndInvoke", "Invoke" }
		},
		{
			new([new(BlockType.Interface, "IChar", 1)]), new()
			{
				"ConvertFromUtf32", "ConvertToUtf32", "GetNumericValue", "GetUnicodeCategory",
				"IsControl", "IsHighSurrogate", "IsLowSurrogate", "IsNumber", "IsPunctuation",
				"IsSurrogate", "IsSurrogatePair", "IsSymbol",
				"ToLowerInvariant", "ToUpperInvariant"
			}
		},
		{
			ExtendedTypeList, new()
			{
				"AsReadOnly", "ConvertAll", "GetEnumerator"
			}
		},
		{
			GetPrimitiveBlockStack(ObjectTypeName), new()
			{
				"GetType", "GetTypeCode", "ReferenceEquals"
			}
		},
		{
			new([new(BlockType.Namespace, SystemName, 1), new(BlockType.Class, "Predicate", 1)]), new()
			{
				"BeginInvoke", "EndInvoke", "Invoke"
			}
		},
		{
			ExtendedTypeString, new()
			{
				"Clone", "Copy", "CopyTo", "Empty", "Format", "GetEnumerator", "Intern",
				"IsInterned", "IsNormalized", "IsNullOrEmpty", "IsNullOrWhiteSpace",
				"Normalize", "ToLowerInvariant", "ToUpperInvariant"
			}
		}
	};

	public static OutdatedMethods OutdatedStringMethodOverloads { get; } = new()
	{
		{
			"Concat", new()
			{
				([
					ExtendedParameterString1, ExtendedParameterString2, ExtendedParameterString3,
					new(StringType, "string4", ParameterAttributes.None, [])
				], "concatenation in pairs, triples or in an array"),
				([
					new(ObjectType, "object1", ParameterAttributes.None, []),
					new(ObjectType, "object2", ParameterAttributes.None, []),
					new(ObjectType, "object3", ParameterAttributes.None, []),
					new(ObjectType, "object4", ParameterAttributes.None, [])
				], "concatenation in pairs, triples or in an array")
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes, ParameterDefaultValues and UseInstead suggestions.
	/// </summary>
	public static SortedDictionary<String, OutdatedMethodOverloads> OutdatedConstructors { get; } = [];

	public static G.SortedSet<String> NotImplementedOperators { get; } = [];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	/// <summary>
	/// Sorted by OperandsCount and Operator, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedOperators { get; } = [];

	public static G.SortedSet<String> ReservedOperators { get; } = ["#", "G", "K", "_", "g", "hexa", "hexa=", "penta", "penta=", "tetra", "tetra="];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	public static BlockStack GetBlockStack(String basic)
	{
		var typeName = basic.Copy();
		var namespace_ = typeName.GetBeforeSetAfterLast(".");
		var split = namespace_.Split('.');
		if (PrimitiveTypes.ContainsKey(basic))
			return GetPrimitiveBlockStack(basic);
		else if (ExtraTypes.TryGetValue((namespace_, typeName), out var netType)
			|| ImportedTypes.TryGetValue((namespace_, typeName), out netType)
			|| IOTypes.TryGetValue((namespace_, typeName), out netType))
		{
			var namespaces = split.Convert(x => new Block(BlockType.Namespace, x, 1));
			if (typeof(Delegate).IsAssignableFrom(netType))
				return new([.. namespaces, new(BlockType.Delegate, typeName, 1)]);
			else if (netType.IsInterface)
				return new([.. namespaces, new(BlockType.Interface, typeName, 1)]);
			else if (netType.IsClass)
				return new([.. namespaces, new(BlockType.Class, typeName, 1)]);
			else if (netType.IsValueType)
				return new([.. namespaces, new(BlockType.Struct, typeName, 1)]);
			else
				throw new InvalidOperationException();
		}
		else if (Interfaces.TryGetValue((namespace_, typeName), out var value) && value.DotNetType.IsInterface)
			return new([.. split.Convert(x => new Block(BlockType.Namespace, x, 1)),
				new(BlockType.Interface, typeName, 1)]);
		else if (basic.AsSpan() is nameof(Action) or nameof(Func<>))
			return new([new(BlockType.Delegate, basic, 1)]);
		else
			return new([new(BlockType.Extra, basic, 1)]);
	}

	public static async Task<List<string>> DownloadPackage(string packageId)
	{
		var downloadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloaded");
		var extractDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extracted");
		if (Directory.Exists(extractDir))
			Directory.Delete(extractDir, true);
		var source = new PackageSource("https://api.nuget.org/v3/index.json");
		var sourceRepository = new SourceRepository(source, Repository.Provider.GetCoreV3());
		var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
		var metadata = await metadataResource.GetMetadataAsync(packageId, includePrerelease: false, includeUnlisted: false,
			new(), new NullLogger(), CancellationToken.None);
		var maxVersion = metadata.Max(x => x.Identity.Version);
		var packageIdentity = metadata.FindAll(x => x.Identity.Version == maxVersion).Convert(x => x.Identity).FirstOrDefault()
			?? throw new NonExistentPackageException();
		var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>();
		var downloadResult = await downloadResource.GetDownloadResourceResultAsync(packageIdentity,
			new PackageDownloadContext(new SourceCacheContext()), downloadDir, new NullLogger(), CancellationToken.None);
		if (downloadResult.Status != DownloadResourceResultStatus.Available)
			throw new NonExistentPackageException();
		await VerifyPackageSignature(downloadResult.PackageStream);
		var nupkgStream = downloadResult.PackageStream;
		Directory.CreateDirectory(extractDir);
		await ZipFile.ExtractToDirectoryAsync(nupkgStream, extractDir, true);
		var dllFiles = Directory.GetFiles(extractDir, "*.dll", SearchOption.AllDirectories)
			.Filter(f => f.Contains("/lib/") || f.Contains(@"\lib\")).ToList();
		if (!dllFiles.Any())
			throw new NonExistentPackageException();
		var loadedAssemblies = new ParallelHashSet<Assembly>(new EComparer<Assembly>((x, y) => x.FullName == y.FullName,
			x => x.FullName?.GetHashCode() ?? 0));
		Parallel.ForEach(dllFiles, dllPath =>
		{
			try
			{
				var assembly = Assembly.LoadFrom(dllPath);
				loadedAssemblies.Add(assembly);
			}
			catch
			{
				// Just skip the assembly if it cannot be loaded
			}
		});
		foreach (var assembly in loadedAssemblies)
		{
			try
			{
				loadedAssemblies.Add(assembly);
				foreach (var type in assembly.GetTypes())
				{
					ImportedNamespaces.Add(type.Namespace);
					ImportedTypes.TryAdd((type.Namespace, ((String)type.Name).GetBefore('`')), type);
				}
			}
			catch
			{
				// Just skip the assembly if it cannot be loaded
			}
		}
		return loadedAssemblies.ToList(a => a.FullName ?? "netstandard");
	}

	private static async Task VerifyPackageSignature(Stream packageStream)
	{
		var package = new PackageArchiveReader(packageStream);
		var signaturePath = package.GetFiles()
			.Find(f => f.EndsWith(".signature.p7s"));
		if (string.IsNullOrEmpty(signaturePath))
			throw new WrongSignatureException();
		var signature = package.GetStream(signaturePath) ?? throw new WrongSignatureException();
		try
		{
			SignedCms signedCms = new();
			var signatureBytes = GC.AllocateUninitializedArray<byte>(checked((int)signature.Length));
			await signature.ReadExactlyAsync(signatureBytes);
			signedCms.Decode(signatureBytes);
			signedCms.CheckSignature(true);
			var certificates = signedCms.SignerInfos[0].Certificate;
			if (!(certificates?.Verify() ?? false))
				throw new WrongSignatureException();
		}
		catch (CryptographicException)
		{
			throw new WrongSignatureException();
		}
	}

	public static BlockStack GetNamespaceStack(String basic)
	{
		var split = basic.Split('.');
		var namespaces = split.Convert(x => new Block(BlockType.Namespace, x, 1));
		return new(namespaces);
	}

	public static Slice<ExtendedMethodParameter> ProperParameters(this UserDefinedMethodOverload function) =>
		function.Parameters.GetSlice((function.Attributes & FunctionAttributes.Extent) != 0 ? 1 : 0);
}

public class NonExistentPackageException : Exception
{
	public NonExistentPackageException() : base("Ошибка, такой NuGet-пакет не существует.") { }

	public NonExistentPackageException(string? message) : base(message) { }

	public NonExistentPackageException(string? message, Exception? innerException) : base(message, innerException) { }
}

public class WrongSignatureException : Exception
{
	public WrongSignatureException() : base("Ошибка, нельзя использовать этот пакет из-за неправильной подписи.") { }

	public WrongSignatureException(string? message) : base(message) { }

	public WrongSignatureException(string? message, Exception? innerException) : base(message, innerException) { }
}

public class InternalViewModel : ReactiveObject { }

public class GUIWindow : Window
{
	public GUIWindow() => InitializeComponent();

	public void InitializeComponent() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(this, $"""
		<Window xmlns="https://github.com/avaloniaui"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:vm="using:{nameof(PL051)}.{nameof(NStar)}"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				xmlns:views="clr-namespace:{nameof(PL051)}.{nameof(NStar)}"
				x:Class="PL051.NStar.{nameof(GUIWindow)}"
				x:DataType="vm:{nameof(InternalViewModel)}"
				Title="PL051.NStar"
				Width="1024"
				Height="768"
				MinWidth="1024"
				MinHeight="768">
			<Design.DataContext>
				<!-- This only sets the DataContext for the previewer in an IDE,
						to set the actual DataContext for runtime, set the DataContext property in code (look at App.axaml.cs) -->
				<vm:InternalViewModel />
			</Design.DataContext>

			<ContentControl
				x:Name="Content"
				HorizontalAlignment="Stretch"
				VerticalAlignment="Stretch" />
		</Window>

		"""));
}
