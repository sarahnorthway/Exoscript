using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

/// <summary>
/// For Windows, Mac and Linux platforms, including WindowsEditor when in UNITY_SWITCH build mode.
/// </summary>
public static class FilePlatformPC {
	public const string backupExtension = ".bak";
	public const string backupExtension2 = ".bak2";

	/// <summary>
	/// Called the first time FileManager.streamingAssetsPath is requested.
	/// Stories and other assets loaded from the filesystem, not packaged into the assets.
	/// eg D:\Work\GameName\Assets\StreamingAssets
	/// or E:\Steam\steamapps\common\GameName\GameName_Data\StreamingAssets
	/// /Stories: chara_anemone.exo, location_priority.exo
	/// </summary>
	public static string GetStreamingAssetsPath() {
		string path = FileManager.PathCombine(Application.dataPath, "StreamingAssets");
		if (!Directory.Exists(path)) {
			Debug.Log("Failed to find StreamingAssets at " + path);

			if (Application.platform == RuntimePlatform.OSXPlayer) {
				// on mac Application.dataPath returns:
				// /Users/username/Library/Application Support/Steam/steamapps/common/GameName/GameName_Mac.app/Contents/StreamingAssets
				// but the files actually reside at:
				// /Users/username/Library/Application Support/Steam/steamapps/common/GameName/GameName_Mac.app/Contents/Resources/Data/StreamingAssets
				path = FileManager.PathCombine(Application.dataPath, "Resources/Data/StreamingAssets");
				if (!Directory.Exists(path)) {
					Debug.Log("STILL Failed to find StreamingAssets at " + path);
				}
			}
		}
		return path;
	}
	
	/// <summary>
	/// Called the first time FileManager.documentsPath is requested.
	/// Where log files, settings, save games and other user-accessible files are saved.
	/// eg C:\Users\username\Documents\GameName\
	/// Groundhogs.json, Settings.json, log.txt
	/// </summary>
	public static string GetDocumentsPath() {
		string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		string path = FileManager.PathCombine(myDocuments, "GameName");
		if (!Directory.Exists(path)) {
			Directory.CreateDirectory(path);
		}
		return path;
	}

	/// <summary>
	/// Called the first time FileManager.savePath is requested.
	/// Savegames under the documentsPath.
	/// eg C:\Users\username\Documents\GameName\Savegames\
	/// Autosave1_Autosave1_Solar_77_637766268208152109.json, Save_Everything_Solar_78_637763037231010084.json
	/// </summary>
	public static string GetSavePath() {
		string path = FileManager.PathCombine(GetDocumentsPath(), "Savegames");
		if (!Directory.Exists(path)) {
			Directory.CreateDirectory(path);
		}
		return path;
	}

	public static void SaveFile(byte[] data, string filePath) {
		SaveFileInner(data, null, filePath);
	}
	
	/// <summary>
	/// Data used when refreshing spreadsheets or saving .exoc files while game is running,
	/// or for CopyFile when creating a savegame during a story event.
	///
	/// DataString used when saving savegames, logs, groundhogs, settings, etc.
	/// Does a complicated backup-check-deletefile-save-check-deletebackup dance to avoid corrupting Groundhogs.json
	/// </summary>
	private static void SaveFileInner(byte[] data, string dataString, string filePath) {
		try { 
			if (!File.Exists(filePath)) {
				if (data != null) {
					File.WriteAllBytes(filePath, data);
					byte[] savedData = ReadAllBytes(filePath);
					if (!savedData.EqualsSafe(data)) {
						Debug.LogError("FilePlatformPC.SaveFile failed to save, " + filePath);
						return;
					}
				} else {
					File.WriteAllText(filePath, dataString);
					string savedString = ReadAllText(filePath);
					if (!savedString.Equals(dataString)) {
						Debug.LogError("FilePlatformPC.SaveFile failed to save, " + filePath);
						return;
					}
				}
				return;
			}

			// back up the existing file first (except when compiling story files)
			string backupFilePath = Path.ChangeExtension(filePath, backupExtension);
			if (!filePath.Contains(".exoc") && !filePath.Contains(".tsv")) {
				byte[] oldData = ReadAllBytes(filePath);
				if (oldData.Length == 0 || oldData[0] == '\0') {
					// corrupt files seem to be filled with null (NUL) characters
					// don't replace a clean Groundhogs.bak with a corrupt Groundhogs.json, instead create Groundhogs.bak2
					Debug.LogError("FilePlatformPC.SaveFile encountered corrupt old file, backing up to bak2, " + filePath);
					backupFilePath = Path.ChangeExtension(filePath, backupExtension2);
				}
				File.Copy(filePath, backupFilePath, true);
				byte[] backupData = ReadAllBytes(backupFilePath);
				if (!oldData.EqualsSafe(backupData)) {
					Debug.LogError("FilePlatformPC.SaveFile failed to backup, " + filePath);
					return;
				}
			}

			// delete the file
			File.Delete(filePath);
			
			// replace the file
			if (data != null) {
				File.WriteAllBytes(filePath, data);
			} else {
				File.WriteAllText(filePath, dataString);
			}
			
			// check the file
			if (data != null) {
				byte[] savedData = ReadAllBytes(filePath);
				if (!savedData.EqualsSafe(data)) {
					Debug.LogError("FilePlatformPC.SaveFile failed to overwrite, " + filePath);
					// try to copy the bak file back, leave the bak file in place
					if (File.Exists(backupFilePath)) File.Copy(backupFilePath, filePath, true);
					return;
				}
			} else {
				string savedData = ReadAllText(filePath);
				if (!savedData.Equals(dataString)) {
					Debug.LogError("FilePlatformPC.SaveFile failed to overwrite, " + filePath);
					// try to copy the bak file back, leave the bak file in place
					if (File.Exists(backupFilePath)) File.Copy(backupFilePath, filePath, true);
					return;
				}
			}
			
			// don't delete the backup, leave it around forever just in case
			
		} catch (Exception e) {
			Debug.LogError("FilePlatformPC.SaveFile failed, " + filePath + ", " + e);
		}
	}

	/// <summary>
	/// For loading compiled story files, or copying autosave when manually saving during a story event.
	/// </summary>
	public static byte[] ReadAllBytes(string filePath) {
		return File.ReadAllBytes(filePath);
	}

	/// <summary>
	/// For savegames, settings, groundhogs, logs, just about everything.
	/// </summary>
	public static string ReadAllText(string filePath) {
		string result = File.ReadAllText(filePath);
		if ((result.Length == 0 && FileExists(filePath)) || (result.Length > 0 && result[0] == '\0')) {
			// corrupt files seem to be filled with null (NUL) characters
			string backupFilePath = Path.ChangeExtension(filePath, backupExtension);
			string backupResult = File.ReadAllText(backupFilePath);
			if (backupResult.Length > 0) {
				Debug.LogError("FilePlatformPC.ReadAllText found corrupt file, backup recovered, " + filePath);
				return backupResult;
			} else {
				Debug.LogError("FilePlatformPC.ReadAllText found corrupt file, no backup, " + filePath);
			}
		}
		return result;
	}

	public static string[] GetSaveFilenames() {
		try {
			DirectoryInfo directoryInfo = new DirectoryInfo(GetSavePath());
			FileInfo[] fileInfos = directoryInfo.GetFiles();
			List<string> filenames = new List<string>();
			foreach (FileInfo fileInfo in fileInfos) {
				if (fileInfo.Extension.ToLower() == ".meta") continue;
				if (!fileInfo.Extension.ToLower().Equals(".json")) continue;
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

	public static bool FileExists(string fileName) {
		return File.Exists(fileName);
	}
}