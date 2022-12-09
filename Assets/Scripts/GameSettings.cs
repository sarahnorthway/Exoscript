using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moved here from various places.
/// </summary>
public class GameSettings {
    public static bool debug = false;
    public static bool debugAllTextChunks = false;
    public static bool debugAllChoices = false;
	public static bool validateStories = false;
    public static Dictionary<string, string> customGender = new Dictionary<string, string>();
    public static int month = 15;
}
