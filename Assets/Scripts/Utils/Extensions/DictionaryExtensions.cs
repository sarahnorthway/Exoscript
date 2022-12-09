using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class DictionaryExtensions {
	
	/// <summary>
	/// Unique only. Creates new list if required.
	/// </summary>
	public static void AddToDictionaryList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value, bool allowDuplicates = false) {
		if (key == null || dict == null) return;

		List<TValue> list = null;
		if (dict.ContainsKey(key)) {
			list = dict[key];
		}
		if (list == null) {
			list = new List<TValue>();
		}
		list.Add(value);
		dict[key] = list;
	}

	public static void RemoveFromDictionaryList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value) {
		if (key == null || dict == null) return;
		if (!dict.ContainsKey(key)) return;

		List<TValue> list = dict[key];
		if (list == null) return;

		list.Remove(value);
	}
	
	public static TValue GetSafe<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default) {
		if (key == null || dict == null) return defaultValue;
		if (!dict.ContainsKey(key)) return defaultValue;
		return dict[key];
	}
}