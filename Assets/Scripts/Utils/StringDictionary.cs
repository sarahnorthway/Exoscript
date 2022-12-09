using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for mem, var etc. Has helpers for prefxies and values which are actually bool or int but stored as string.
/// </summary>
public class StringDictionary : Dictionary<string, string> {
	public StringDictionary() : base() {
		// flow through to base and do nothing more
	}
	public StringDictionary(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) {
		// flow through to base and do nothing more
	}

	/// <summary>
	/// Return true if memory was never set or was set to "false" or "0".
	/// </summary>
	public bool Has(string stringID) {
		return ContainsKey(stringID);
	}

	/// <summary>
	/// Return key value (usually "true") or null if it doesn't exist.
	/// </summary>
	public string Get(string stringID) {
		if (!ContainsKey(stringID)) return null;
		return this[stringID];
	}

	/// <summary>
	/// Return all values for all keys starting with a given prefix.
	/// Unoptimized.
	/// </summary>
	public string[] GetPrefix(string prefix) {
		List<string> values = new List<string>();
		foreach (string key in Keys) {
			if (key.StartsWith(prefix)) {
				values.Add(this[key]);
			}
		}
		return values.ToArray();
	}

	/// <summary>
	/// Return an int value for the given key, or 0 if something goes wrong.
	/// </summary>
	public int GetInt(string stringID) {
		if (!ContainsKey(stringID)) return 0;
		string stringValue = this[stringID];
		int intValue;
		bool success = stringValue.TryParseInt(out intValue);
		if (!success) {
			// try bool just in case; false = 0, true = 1
			bool boolValue;
			success = stringValue.TryParseBool(out boolValue);
			if (!success) {
				Debug.LogWarning("StringDictionary failed to GetInt " + stringID + ", value " + stringValue + ", setting to 0");
				return 0;
			}
			intValue = boolValue ? 1 : 0;
		}
		return intValue;
	}

	/// <summary>
	/// Value is always lowercase and trimmed.
	/// Overwrites if already exists.
	/// Removes key if setting to null, "", "false" or "0".
	/// </summary>
	public void Set(string stringID, string stringValue) {
		if (string.IsNullOrEmpty(stringValue)) {
			if (ContainsKey(stringID)) {
				Remove(stringID);
			}
		} else {
			// do not lower case for ~set mem_anemoneNick = Anemone
			// other dictionaries should already have their values lowercased before here
			stringValue = stringValue.Trim();
			//stringValue = stringValue.Trim().ToLower();
			if (stringValue == "false" || stringValue == "0") {
				if (ContainsKey(stringID)) {
					Remove(stringID);
				}
			} else {
				this[stringID] = stringValue;
			}
		}
	}

	public void Set(string stringID, int value) {
		Set(stringID, value.ToString());
	}

	/// <summary>
	/// Overwrite if there's already something there.
	/// </summary>
	public void AddSafe(string key, string value = "true") {
		if (key.IsNullOrEmpty()) return;
		Set(key, value);
	}

	/// <summary>
	/// Set memory to "true".
	/// </summary>
	public void Add(string stringID) {
		AddSafe(stringID);
	}

	/// <summary>
	/// Add incrementValue to the stored integer for a memory.
	/// If memory does not exist, add it with incrementValue.
	/// If memory is stored as boolean ("true"/"false") treat those as 1 and 0.
	/// </summary>
	public void Increment(string stringID, int incrementValue = 1) {
		string stringValue = Get(stringID);
		if (string.IsNullOrEmpty(stringValue)) {
			Set(stringID, incrementValue.ToString());
			return;
		}
		int newValue = GetInt(stringID) + incrementValue;
		Set(stringID, newValue.ToString());
	}
}
