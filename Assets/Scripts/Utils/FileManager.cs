using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

public static class FileManager {
	private const string storiesFolder = "Stories";
	public const string parserStoryFileExtension = "exo";
	public const string compiledStoryFileExtension = "exoc";
	
	/// <summary>
	/// Stories and other assets loaded from the filesystem, not packaged into the assets.
	/// eg D:\Work\GameName\Assets\StreamingAssets
	/// or E:\Steam\steamapps\common\GameName\GameName_Data\StreamingAssets
	/// </summary>
	private static string _streamingAssetsPath = null;
	private static string streamingAssetsPath {
		get {
			if (_streamingAssetsPath == null) {
				_streamingAssetsPath = FilePlatformPC.GetStreamingAssetsPath();
			}
			return _streamingAssetsPath;
		}
	}
	
	/// <summary>
	/// For event TXT files each containing multiple stories.
	/// </summary>
	public static string storiesPath {
		get {
			return PathCombine(streamingAssetsPath, storiesFolder);
		}
	}

	/// <summary>
	/// Where log files, settings, save games and other user-accessible files are saved.
	/// eg C:\Users\username\Documents\GameName\
	/// Android can't call GetDocumentsPath() on non-main threads while saving, so cache this on main thread.
	/// </summary>
	private static string _documentsPath = null;
	public static string documentsPath {
		get {
			if (_documentsPath == null) {
				_documentsPath = FilePlatformPC.GetDocumentsPath();
			}
			return _documentsPath;
		}
	}

	/// <summary>
	/// Used when refreshing spreadsheets or saving .exoc files,
	/// or for CopyFile when creating a savegame during a story event.
	/// </summary>
	public static void SaveFile(byte[] data, string filename, string path = null) {
		try {
			if (path == null) path = documentsPath;
			string filePath = PathCombine(path, filename);
			FilePlatformPC.SaveFile(data, filePath);
		} catch (Exception e) {
			Debug.LogError("FileManager.SaveFile failed. " + e);
		}
	}

	/// <summary>
	/// For loading compiled story files, or copying autosave when manually saving during a story event. 
	/// </summary>
	public static byte[] LoadFileBytes(string filename, string path = null) {
		try {
			if (string.IsNullOrEmpty(filename)) {
				Debug.LogError("Can't load null filename");
				return null;
			}
			if (string.IsNullOrEmpty(path)) {
				path = documentsPath;
			}
			string filePath = PathCombine(path, filename);
			if (FilePlatformPC.FileExists(filePath)) {
				return FilePlatformPC.ReadAllBytes(filePath);
			}

			if (File.Exists(filePath)) {
				return File.ReadAllBytes(filePath);
			} else {
				Debug.LogWarning ("File not found: " + filePath);
				return null;
			}
		} catch (Exception e) {
			Debug.LogError("FileManager.LoadFileBytes failed. " + e);
			return null;
		}
	}

	public static string LoadFileString(string filename, string path = null, bool warnFileMissing = true) {
		try {
			if (string.IsNullOrEmpty(filename)) {
				Debug.LogError("Can't load null filename");
				return null;
			}
			if (string.IsNullOrEmpty(path)) {
				path = documentsPath;
			}

			filename = PathCombine(path, filename);

			if (path.StartsWith(documentsPath)) {
				if (FilePlatformPC.FileExists(filename)) {
					return FilePlatformPC.ReadAllText(filename);
				}
			}

			if (File.Exists(filename)) {
				string text = File.ReadAllText(filename);
				return text;
			} else {
				if (warnFileMissing) {
					Debug.LogWarning("File not found: " + filename);
				}
				return null;
			}
		} catch (Exception e) {
			Debug.LogError("FileManager.LoadFileString failed. " + filename + ", " + path + ", " + e);
			return null;
		}
	}

	/// <summary>
	/// Defaults to using the datapath and convert /r and /r/n to /n.
	/// </summary>
	public static string[] LoadFileLines(string filename, string path = null, bool warnFileMissing = true) {
		string rawData = LoadFileString(filename, path, warnFileMissing);
		if (string.IsNullOrEmpty(rawData)) {
			return new string[0];
		}
		string[] lines = rawData.SplitLines();
		return lines;
	}

	/// <summary>
	/// Return a list of filenames with extensions in the given path.
	/// Path should NOT contain a period, eg "json".
	/// </summary>
	public static string[] GetFilenames(string path, string extension = null) {

		if (path.Equals(FilePlatformPC.GetSavePath())) {
			return FilePlatformPC.GetSaveFilenames();
		} else {
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				FileInfo[] fileInfos = directoryInfo.GetFiles();
				List<string> filenames = new List<string>();
				foreach (FileInfo fileInfo in fileInfos) {
					if (fileInfo.Extension.ToLower() == ".meta") continue;
					if (extension != null && fileInfo.Extension.ToLower() != "." + extension.ToLower()) continue;
					filenames.Add(fileInfo.Name);
				}
				string[] filenamesArray = filenames.ToArray();
				Array.Sort(filenamesArray);
				return filenamesArray;
			} catch (Exception e) {
				Debug.LogError("FileManager.FilesStartingWith failed. " + e);
				return new string[0];
			}
		}
	}

	public static bool DirectoryExists(string path) {
		return Directory.Exists(path);
	}

	/// <summary>
	/// Safely append two paths together using Path.Combine.
	/// Except even safer and more carefully and only use linux forward slashes.
	/// Switch uses forward slashes (like mac and linux) not blackslash (like windows)
	/// </summary>
	public static string PathCombine(string part1, string part2) {
		if (part1.IsNullOrEmptyOrWhitespace()) return part2;
		if (part2.IsNullOrEmptyOrWhitespace()) return part1;
		part1 = part1.ReplaceAll("\\", "/");
		part2 = part2.ReplaceAll("\\", "/");
		if (part1.EndsWith("/") && part2.StartsWith("/")) {
			part1 = part1.RemoveEnding("/");
		} else if (!part1.EndsWith("/") && !part2.StartsWith("/")) {
			part1 += "/";
		}
		string result = Path.Combine(part1, part2);
		result = result.ReplaceAll("\\", "/");
		return result;
	}
}
