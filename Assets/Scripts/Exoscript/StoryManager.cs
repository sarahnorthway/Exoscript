using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Static functions for handling memories, groundhogs etc.
/// </summary>
public static class StoryManager {

	public static int currentGameMonth = 15;
	
	// which stories have been seen, and what month they were LAST seen
	public static Dictionary<string, int> stories => new Dictionary<string, int>();

	// for debugging, what stories were seen each month
	public static Dictionary<int, List<string>> storiesLog => new Dictionary<int, List<string>>();

	// events of note eg important choices during stories, by month
	public static StringDictionary memories => new StringDictionary();
	
	// variables which persist across all savegames
	public static StringDictionary groundhogs => new StringDictionary();

	// set while executing a story, for StoryCalls to reference
	private static Result _result = null;
	public static Result result => StoryManager._result;

	public const string hogLastEnding = "lastending"; // used by StoryCalls.endGame
	public const string hogNumLives = "numlives"; // used by StoryCalls.endGame
	public const string hogLifeStarted = "lifestarted"; // used by StoryCalls.endGame

	public static bool isUndoing = false;

	public static void SetResult(Result result) {
		// usually changes to null before a new result
		// but could overwrite while debugging eg when force-closing the battle or results menu
		_result = result;
	}

	public static void AddMemory(string id, string value = "true") {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		memories.AddSafe(id, value.Trim());
	}

	public static void SetMemory(string id, object value = null) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		if (value == null) value = "true";
		AddMemory(id, value.ToString());
	}

	public static void IncrementMemory(string id, int value = 1) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		memories.Increment(id, value);
	}

	public static bool HasMemory(string id) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		return memories.Has(id);
	}

	public static string GetMemory(string id) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		if (!memories.ContainsKey(id)) return "";
		return memories[id];
	}

	public static int GetMemoryInt(string id) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		return memories.GetInt(id);
	}

	public static void RemoveMemory(string id) {
		id = id?.ToLower().RemoveStart("mem_").Trim() ?? "";
		memories.Remove(id);
	}

	public static void AddGroundhog(string id, string value = "true") {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		value = value.Trim();
		groundhogs.AddSafe(id, value);
	}

	public static void SetGroundhog(string id, object value = null) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		if (value == null) value = "true";
		AddGroundhog(id, value.ToString());
	}

	public static void IncrementGroundhog(string id, int value = 1) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		groundhogs.Increment(id, value);
	}

	/// <summary>
	/// allDelusionsEnding story turns off hogs for the rest of the playthrough
	/// </summary>
	public static bool hogsDisabled(string id) {
		if (!HasMemory("hogsdisabled")) return false;
		// exceptions for certain hogs we need to remember
		if (id == hogNumLives) return false;
		return true;
	}

	public static bool HasGroundhog(string id) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		if (hogsDisabled(id)) return false;
		return groundhogs.Has(id);
	}

	public static string GetGroundhog(string id) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		if (hogsDisabled(id)) return "";
		if (!groundhogs.ContainsKey(id)) return "";
		return groundhogs[id];
	}

	public static int GetGroundhogInt(string id) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		if (hogsDisabled(id)) return 0;
		return groundhogs.GetInt(id);
	}

	public static void RemoveGroundhog(string id) {
		id = id?.ToLower().RemoveStart("hog_").Trim() ?? "";
		groundhogs.Remove(id);
	}

	/// <summary>
	/// Have you played the given storyID.
	/// </summary>
	public static bool HasStory(string storyID) {
		storyID = storyID.ToLower().Trim();
		return stories.ContainsKey(storyID);
	}

	/// <summary>
	/// Have you played the given story.
	/// </summary>
	public static bool HasStory(Story story) {
		if (story == null) return false;
		return HasStory(story.storyID);
	}

	/// <summary>
	/// Same as above but may be used with fake set stories eg ~set story_madeUpId.
	/// Return -1 if story never run.
	/// </summary>
	public static int GetStoryMonth(string storyID) {
		storyID = storyID.ToLower().Trim();
		if (!stories.ContainsKey(storyID)) return -1;
		return stories[storyID];
	}

	/// <summary>
	/// Add or remove a story by setting it to a bool.
	/// Used for hackery with ~set story_whatever = false.
	/// </summary>
	public static void SetStory(Story story, bool value = true) {
		if (story == null) {
			Debug.LogWarning("Princess.SetStory with null story");
			return;
		}
		
		SetStory(story.storyID, value);
	}

	public static void SetStory(string storyID, bool value = true) {
		if (value) {
			stories[storyID] = StoryManager.currentGameMonth;
			storiesLog.AddToDictionaryList(StoryManager.currentGameMonth, storyID);
		} else if (stories.ContainsKey(storyID)) {
			stories.Remove(storyID);
			// remove from storiesLog only on same month it happened - for debugging
			storiesLog.RemoveFromDictionaryList(StoryManager.currentGameMonth, storyID);
		}
	}

	/// <summary>
	/// For debugging. Value is the month the story occured. 
	/// Month of 0 or lower removes the story.
	/// </summary>
	public static void SetStoryMonth(Story story, int month) {
		if (story == null) {
			Debug.LogWarning("Princess.SetStoryMonth with null story");
			return;
		}
		if (month > 0) {
			stories[story.storyID] = month;
		} else if (stories.ContainsKey(story.storyID)) {
			stories.Remove(story.storyID);
		}
	}
}