using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Most of these have been moved to Extensions.
/// Static functions dealing with randomness, reflection, running later and drawing debug objects.
/// </summary>
public class NWUtils {
	private static UnityEngine.Random.State prevRandomState = default(UnityEngine.Random.State);
	private static float numRandomSeeds = 0; // for TrulyRandomSeed
	
	/// <summary>
	/// Return a number from 0 to 1 based roughly on the given text string, used for pseudo-random numbers.
	/// </summary>
	public static float RandomFromString(string text) {
		if (string.IsNullOrEmpty(text)) {
			return 0.5f;
		}

		// sum the values of the characters
		float hashNumber = 0;
		for (int i = 0; i < text.Length; i++) {
			// square and multiply by the position so 1salt2 is different than 2salt1
			hashNumber += (int)text[i] * (int)text[i] * i;
		}

		// wrap the int around a couple times and pseudo-ranomize by multiplying by primes
		float hashPrimed = hashNumber * 9343 * 12157 * 15307;
		float hashPercent = (hashPrimed % 2953) / 2953;

		return hashPercent;
	}

	public static int RandomRangeInt(int startInclusive, int endInclusive, string seed = null) {
		if (seed == null) {
			// Random.Range int is inclusive to EXCLUSIVE so add one
			return UnityEngine.Random.Range(startInclusive, endInclusive + 1);
		} else {
			float randomNumber = RandomFromString(seed); // between 0 and 1 inclusive
			int value = Mathf.FloorToInt(randomNumber * (endInclusive - startInclusive + 1) + startInclusive);
			if (value < startInclusive || value > endInclusive) {
				// could happen if the range is from 0 to int.MaxValue beacause of that +1
				Debug.LogWarning("RandomRangeInt outside range! " + value + " (" + startInclusive + " - " + endInclusive + ")");
			}
			return value;
		}
	}

	/// <summary>
	/// Return true numerator/denominator percent of the time.
	/// Use seed = ("eventname" + month) to defeat save scrubbing
	/// </summary>
	public static bool RandomChance(float numerator, float denominator, string seed = null) {
		if (float.IsNaN(numerator) || float.IsNaN(denominator)) {
			Debug.LogError("NaN in RandomChance, num: " + numerator + ", denom: " + denominator);
			return false;
		}

		if (denominator <= 0) {
			Debug.LogError("Denominator is zero in RandomChance");
			return false;
		}

		if (numerator > denominator) return true;

		float randomNumber = seed == null ? UnityEngine.Random.Range(0f, 1f) : RandomFromString(seed);
		float chance = numerator / denominator; // between 0 and very large
		return randomNumber < chance;
	}
	
	/// <summary>
	/// Used to break out of a set day seed for one action. Generates a seed that can be used in
	/// other random functions.
	/// </summary>
	public static string TrulyRandomSeed() {
		// Time.realtimeSinceStartup or System.DateTime.Now.Ticks might not change in the middle of a frame
		// on some platforms, so add a counter for good measure
		return DateTime.Now.Ticks + " " + Time.realtimeSinceStartup + " " + (numRandomSeeds++);
	}

	/// <summary>
	/// Set the random seed from a string.
	/// </summary>
	public static void SetRandomSeed(string randomString) {
		// whether or not the seed changes, remember the previous state so we can ResetRandomSeed
		prevRandomState = UnityEngine.Random.state;
		// if (debugOnly && !GameSettings.debug) return;
		// if (GameSettings.debug) return;
		int randomSeed = (int)(RandomFromString(randomString) * int.MaxValue);
		UnityEngine.Random.InitState(randomSeed);
	}

	/// <summary>
	/// Set the random seed back to its previous state before the last SetRandomSeed.
	/// </summary>
	public static void ResetRandomSeed() {
		UnityEngine.Random.state = prevRandomState;
	}
}
 