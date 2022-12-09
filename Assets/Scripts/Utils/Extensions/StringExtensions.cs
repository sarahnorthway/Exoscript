using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StringExtensions {
	private static readonly string[] newLineCharacters = new[] {"\r\n", "\r", "\n"};

	/// <summary>
	/// More thorough than string.Replace, deals correctly with nested results,
	/// eg ReplaceAll("aaaa", "aaa", "aa") will return "aa" not "aaa".
	/// </summary>
	public static string ReplaceAll(this string value, string find, string replace) {
		if (value == null || find == null || replace == null) return value;
		value = value.Replace(find, replace);
		if (!replace.Contains(find) && value.Contains(find)) {
			value = ReplaceAll(value, find, replace);
		}
		return value;
	}

	/// <summary>
	/// string.Replace replaces all instances, this one stops after the first.
	/// </summary>
	public static string ReplaceFirst(this string value, string find, string replace) {
		if (value == null || find == null || replace == null) return value;
		int index = value.IndexOf(find, StringComparison.Ordinal);
		if (index < 0) return value;
		// return value.Substring(0, index) + replace + value.Substring(index + find.Length);
		return value.Remove(index, find.Length).Insert(index, replace);
	}
	
	public static string ReplaceLast(this string value, string find, string replace) {
		if (value == null || find == null || replace == null) return value;
		int index = value.LastIndexOf(find, StringComparison.Ordinal);
		if (index < 0) return value;
		return value.Remove(index, find.Length).Insert(index, replace);
	}

	/// <summary>
	/// Checks for null is all.
	/// </summary>
	public static string TrimSafe(this string value) {
		if (value == null) return "";
		return value.Trim();
	}

	public static string RemoveStartEnd(this string value, string ending, bool removeAll = false) {
		value = RemoveStart(value, ending, removeAll);
		value = RemoveEnding(value, ending, removeAll);
		return value;
	}


	/// <summary>
	/// Remove start if it is any of startArray values.
	/// </summary>
	public static string RemoveStart(this string value, string[] startArray) {
		foreach (string start in startArray) {
			value = RemoveStart(value, start);
		}
		return value;
	}

	/// <summary>
	/// Remove a specific string from the start of value.
	/// </summary>
	public static string RemoveStart(this string value, string start, bool removeAll = false) {
		if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(start)) return "";
		if (!value.StartsWith(start)) {
			return value;
		}
		value = value.Substring(start.Length);
		if (removeAll) {
			return RemoveStart(value, start, removeAll);
		} else {
			return value;
		}
	}

	/// <summary>
	/// Remove a specific string from the end of value.
	/// </summary>
	public static string RemoveEnding(this string value, string ending, bool removeAll = false) {
		if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(ending)) return "";
		if (!value.EndsWith(ending)) {
			return value;
		}
		value = value.Substring(0, value.Length - ending.Length);
		if (removeAll) {
			return RemoveEnding(value, ending, removeAll);
		} else {
			return value;
		}
	}
	
	/// <summary>
	/// Capitalize the first letter of the first word.
	/// With the blinding speed of input switches, apparently
	/// https://stackoverflow.com/questions/4135317/make-first-letter-of-a-string-upper-case-with-maximum-performance?rq=1
	/// </summary>
	public static string CapitalizeFirstChar(this string input) => input switch {
		null => string.Empty,
		"" => string.Empty,
		_ => input[0].ToString().ToUpper() + input.Substring(1)
	};

	/// <summary>
	/// Support dos/windows (\r\n), linux (\n), mac (\r) line endings.
	/// Don't trim lines (leave leading tabs for spreadsheets).
	/// Don't skip blank lines (completely empty lines needed for story paragraph spacing)
	/// </summary>
	public static string[] SplitLines(this string value) {
		return value.Split(newLineCharacters, StringSplitOptions.None);
	}

	/// <summary>
	/// Helper function for splitting one string by another string.
	/// </summary>
	public static string[] SplitSafe(this string value, string splitter = ",", string splitter2 = null, string splitter3 = null, string splitter4 = null) {
		if (value == null) return new string[] { "" };
		List<string> splitterList = new List<string>();
		splitterList.Add(splitter);
		if (splitter2 != null) splitterList.Add(splitter2);
		if (splitter3 != null) splitterList.Add(splitter3);
		if (splitter4 != null) splitterList.Add(splitter4);
		string[] splitterArray = splitterList.ToArray();
		return value.Split(splitterArray, StringSplitOptions.None);
	}
	
	public static bool TryParseInt(this string value, out int result) {
		return int.TryParse(value, NumberStyles.Integer, 
			CultureInfo.InvariantCulture, out result);
	}
	
	public static bool TryParseBool(this string value, out bool result) {
		return bool.TryParse(value, out result);
	}

	/// <summary>
	/// Strips non-numeric. Returns defaultValue for invalid values (default 0)
	/// </summary>
	public static int ParseInt(this string value, int defaultValue = 0) {
		if (string.IsNullOrEmpty(value)) return defaultValue;
		value = StripNonNumeric(value);
		int result;
		// make sure we use InvariantInfo which is basically en-US
		// the default is CurrentInfo which could be wacky in some countries eg 1000,00 instead of 100,000
		if (int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result)) {
			return result;
		}
		return defaultValue;
	}

	public static long ParseLong(this string value) {
		if (string.IsNullOrEmpty(value)) return 0;
		value = StripNonNumeric(value);
		long result;
		if (long.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result)) {
			return result;
		}
		return 0;
	}

	/// <summary>
	/// The given string may be a boolean, an integer, or failing those a trimmed lowercase string.
	/// Used when parsing story scripts using reflection.
	/// </summary>
	public static object ParseBoolIntString(this string value) {
		if (string.IsNullOrEmpty(value)) return "";

		value = value.Trim().ToLower();

		if (value == "null") {
			return "";
		}

		bool boolValue;
		bool success = bool.TryParse(value, out boolValue);
		if (success) {
			return boolValue;
		}

		int intValue;
		success = int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out intValue);
		if (success) {
			return intValue;
		}

		return value;
	}

	public static string StripNonNumeric(this string value) {
		return Regex.Replace(value, "[^0-9.-]", "");
	}
	
	/// <summary>
	/// Replace them with spaces, then trim any extra from the start and end.
	/// </summary>
	public static string StripLineBreaks(this string value) {
		return value.ReplaceAll("\r\n", " ").ReplaceAll("\r", " ").ReplaceAll("\n", " ").Trim();
	}

	/// <summary>
	/// Extensions are null-proof, eg this will work, won't throw an NPE:
	/// string nullString = null;
	/// bool isEmpty = nullString.IsEmpty();
	/// Spaces " " will return true.
	/// </summary>
	public static bool IsNullOrEmptyOrWhitespace(this string value) {
		// return string.IsNullOrEmpty(value);
		return string.IsNullOrWhiteSpace(value);
	}
	
	/// <summary>
	/// Spaces " " will return false.
	/// </summary>
	public static bool IsNullOrEmpty(this string value) {
		return string.IsNullOrEmpty(value);
	}
	
	/// <summary>
	/// Return true if both are null/empty/spaces, or if they match ignoring case and trimming spaces.
	/// </summary>
	public static bool EqualsIgnoreCase(this string value, string otherValue) {
		if (value.IsNullOrEmpty() && otherValue.IsNullOrEmpty()) return true;
		if (value.IsNullOrEmpty() || otherValue.IsNullOrEmpty()) return false;
		return string.Equals(value.Trim(), otherValue.Trim(), StringComparison.CurrentCultureIgnoreCase);
	}

	/// <summary>
	/// Given a string which may either be the name of an Enum or its int value, return that Enum value.
	/// Return Enum default on no match.
	/// </summary>
	public static T ParseEnum<T>(this string nameOrValue, T? defaultValue = null) where T : struct, System.IConvertible {
		if (string.IsNullOrEmpty(nameOrValue)) nameOrValue = "";
		nameOrValue = nameOrValue.ToLower().Trim();

		int valueInt = -1;
		if (!nameOrValue.TryParseInt(out valueInt)) {
			valueInt = -1;
		}

		foreach (T possibleValue in System.Enum.GetValues(typeof(T))) {
			if (valueInt >= 0) {
				int possibleIndex = System.Convert.ToInt32(possibleValue);
				if (possibleIndex == valueInt) {
					return possibleValue;
				}
			} else if (possibleValue.ToString().ToLower() == nameOrValue) {
				return possibleValue;
			}
		}

		// return default value
		if (defaultValue != null) {
			return (T)defaultValue;
		}
		return (T)Activator.CreateInstance(typeof(T));
	}
	
	public static bool ContainsAny(this string text, params char[] matches) {
		if (text.IsNullOrEmptyOrWhitespace() || matches == null) {
			return false;
		}
		foreach (char match in matches) {
			if (text.Contains(match)) return true;
		}
		return false;
	}
}