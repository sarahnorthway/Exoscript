using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public static class ArrayExtensions {
	
	/// <summary>
	/// Helper between int[].IndexOf and Array.IndexOf.
	/// </summary>
	public static int IndexOf<T>(this T[] array, T obj) {
		if (array == null) return -1;
		return Array.IndexOf(array, obj);
	}

	public static T GetSafe<T>(this T[] array, int index) {
		if (array == null) return default(T);
		if (index < 0 || index > array.Length - 1) return default(T);
		return array[index];
	}

	public static T PickRandom<T>(this IList<T> list, string seed = null) {
		if (list == null || list.Count == 0) {
			return default(T);
		}

		if (seed != null) NWUtils.SetRandomSeed(seed);
		// Random.Range int is inclusive to EXCLUSIVE so add one
		int index = UnityEngine.Random.Range(0, list.Count);
		T randomValue = list[index];
		if (seed != null) NWUtils.ResetRandomSeed();

		return randomValue;
	}
	
	public static T PickRandomWeighted<T>(this IList<T> list, IList<float> weights, string seed = null) {
		if (list == null || list.Count == 0) {
			return default(T);
		}
		if (weights == null || list.Count != weights.Count) {
			Debug.LogWarning("PickRandomWeighted invalid weights length. Array len: " 
				+ list.Count() + ", weights len: " + (weights == null ? "null" : weights.Count.ToString()));
			return list.PickRandom();
		}

		float totalWeight = 0;
		foreach (float weight in weights) {
			totalWeight += weight;
		}
		if (totalWeight == 0) {
			Debug.LogWarning("PickRandomWeighted zero total weight");
			return default(T);
		}

		if (seed != null) NWUtils.SetRandomSeed(seed);
		// Random.Range float is inclusive to inclusive
		float randomWeight = UnityEngine.Random.Range(0, totalWeight);
		if (seed != null) NWUtils.ResetRandomSeed();

		float runningWeight = 0;
		for (int i = 0; i < list.Count(); i++) {
			runningWeight += weights[i];
			if (weights[i] > 0 && runningWeight >= randomWeight) {
				return list[i];
			}
		}

		Debug.LogError("PickRandomWeighted failed to find item");
		
		return list[list.Count() - 1];
	}

	public static bool EqualsSafe(this byte[] bytes, byte[] otherBytes) {
		if (bytes == null || otherBytes == null) return false;
		if (bytes.Length != otherBytes.Length) return false;
		for (int i = 0; i < bytes.Length; i++) {
			if (bytes[i] != otherBytes[i]) return false;
		}
		return true;
	}
}