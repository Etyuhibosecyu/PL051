using Avalonia.Platform.Storage;

namespace PL051.NStar.ViewModels;

public class MainViewModel : ViewModelBase
{
	public static List<FilePickerFileType> NStarFileTypeFiter { get; } = [new("PL051 code files")
	{
		Patterns = ["*.n-star-alpha"], AppleUniformTypeIdentifiers = ["UTType.Item"],
		MimeTypes = ["multipart/mixed"]
	}];
}
