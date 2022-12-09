using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum StoryReqType {
	none = 0,
	job = 1,
	location = 2,
	chara = 3,
	age = 4,
	season = 5,
	month = 6,
	skill = 7,
	love = 8,
	memory = 9,
	storyvar = 10,
	groundhog = 11,
	story = 12,
	or = 13,
	and = 14,
	mapSpot = 15,
	repeat = 16,
	call = 17,
	random = 18,
	biome = 19,
	status = 20,
}

public enum StoryReqCompare {
	none = 0,
	equal = 1,
	lessThan = 2,
	greaterThan = 3,
	notEqual = 4,
}

public enum RepeatType {
	none = 0,
	months = 1,
	seasons = 2,
	years = 3,
}

public enum Priority {
	none = 0,
	low = 1,
	average = 2,
	high = 3,
}

/// <summary>
/// Eg ~if love_mom > 3
/// </summary>
public class StoryReq {
	public StoryReqType type;
	public StoryReqCompare compare = StoryReqCompare.equal;
	public Story story;

	public string stringID;
	// even if this req fails, still show the choice - eg for failed skill checks
	public bool showDisabled = false;
	public int intValue = 0;
	// for ~if chara = _high_cal true means don't show speech bubble, for ~if random! it means truly random
	public bool? flagValue = null;
	// memory, storyvar, groundhog use stringValue whether bool, int, "null" or other string
	public string stringValue = null;
	public object objectValue = null;

	public RepeatType repeatType = RepeatType.none;
	public StoryCall call;
	// used for location, job, chara, mapspot
	public Priority priority = Priority.average;

	public string debugString = null;
	// for OR, AND and IF id requirements
	public List<StoryReq> subReqs = new List<StoryReq>();

	// ignored when double-checking that a story on the map is still valid
	public static readonly StoryReqType[] placementTypes = new[] {
		StoryReqType.job, StoryReqType.location, StoryReqType.chara, StoryReqType.mapSpot,
		StoryReqType.age, StoryReqType.season, StoryReqType.month, StoryReqType.biome
	};

	/// <summary>
	/// Match mem_whatever, or mem_whatever = thingy, but fail on !mem_whatever
	/// </summary>
	public bool isEqualTrue => compare == StoryReqCompare.equal && stringValue != "false"
		|| compare == StoryReqCompare.notEqual && stringValue == "false";

	public StoryReq(Story story, StoryReqType type = StoryReqType.none) {
		this.story = story;
		this.type = type;
	}

	public StoryReq(Story story, StoryReqTemplate template) {
		this.story = story;

		type = template.type;
		compare = template.compare;
		stringID = template.stringID;
		showDisabled = template.showDisabled;
		intValue = template.intValue;
		flagValue = template.flagValue;
		stringValue = template.stringValue;
		objectValue = template.objectValue is StoryCallTemplate callTemplate ? StoryCall.FromTemplate(callTemplate) : template.objectValue;
		repeatType = template.repeatType;
		priority = template.priority;
		debugString = template.debugString;

		if (template.call != null) {
			call = StoryCall.FromTemplate(template.call);
			if (call != null) {
				call.req = this;
			}
		}

		foreach (var subReqTemplate in template.subReqs) {
			subReqs.Add(new StoryReq(story, subReqTemplate));
		}
	}

	public StoryReqTemplate ToTemplate() {
		return new StoryReqTemplate {
			type = type,
			compare = compare,
			stringID = stringID,
			showDisabled = showDisabled,
			intValue = intValue,
			flagValue = flagValue,
			stringValue = stringValue,
			objectValue = objectValue is StoryCall callObject ? callObject.ToTemplate() : objectValue,
			repeatType = repeatType,
			call = call?.ToTemplate(),
			priority = priority,
			debugString = debugString,
			subReqs = subReqs.Select(r => r.ToTemplate()).ToList()
		};
	}

	/// <summary>
	/// Return true if the current game state meets this requirement.
	/// </summary>
	public bool Execute(Result result, bool isSetIf = false) {
		try {
			return ExecuteInner(result, isSetIf);
		} catch (Exception e) {
			Debug.LogError("StoryReq Execute fail " + e + ", " + this + ", " + story + ", " + result);
			return false;
		}
	}

	private bool ExecuteInner(Result result, bool isSetIf) {
		switch (type) {
			// case StoryReqType.job:
			// 	// if (string.IsNullOrEmpty(stringID)) return false;
			// 	if (string.IsNullOrEmpty(stringID)) {
			// 		// use stringValue = "true" or "false" instead to check for null job or any job
			// 		return CompareBool(result.job != null);
			// 	} else {
			// 		if (result.job == null) {
			// 			// "job != lookoutduty" returns true when there's no job at all, false when there is
			// 			return compare == StoryReqCompare.notEqual;
			// 		}
			// 		return CompareStringID(result.job.jobID);
			// 	}

			// case StoryReqType.location:
			// 	// none can never be picked, only executed directly
			// 	if (stringID == Location.locationIDNone) return false;
			// 	// all and priority happen anywhere
			// 	if (stringID == Location.locationIDAll) return true;
			// 	if (stringID == Location.locationIDPriority) return true;
			// 	if (result.location == null) return false;
			// 	return CompareStringID(result.location.locationID);

			// case StoryReqType.chara:
			// 	// none chara means all charas
			// 	//if (stringID == Chara.none.charaID) return true;
			// 	if (string.IsNullOrEmpty(stringID)) return true;
			// 	if (result.chara == null) return false;
			// 	return CompareStringID(result.chara.charaID);

			// case StoryReqType.age:
			// 	// match year
			// 	return CompareInt(GameManager.age);

			// case StoryReqType.season:
			// 	if (!stringValue.IsNullOrEmpty()) {
			// 		// only used in inspector, wet-2 in the stringValue is the same as monthOfYear in the intValue
			// 		intValue = Season.GetMonthOfYear(stringValue);
			// 		return CompareInt(GameManager.monthOfYear);
			//
			// 	} else if (intValue > 0) {
			// 		// match exact month of year
			// 		return CompareInt(GameManager.monthOfYear);
			// 	} else {
			// 		// match season
			// 		return CompareStringID(GameManager.season?.seasonID);
			// 	}

			// case StoryReqType.month:
			// 	return CompareInt(GameManager.monthOfGame);

			// case StoryReqType.skill:
			// 	// return CompareInt(Princess.GetSkill(stringID, true, true));
			// 	return CompareInt(GameManager.GetSkill(stringID));

			// case StoryReqType.love:
			// 	return CompareInt(Mathf.FloorToInt(GameManager.GetLove(stringID)));

			case StoryReqType.memory:
				return CompareStringDict(StoryManager.memories);

			case StoryReqType.storyvar:
				if (story == null) {
					Debug.LogError("StoryReq.storyvar needs a story");
					return false;
				}
				return CompareStringDict(story.vars);

			case StoryReqType.groundhog:
				if (StoryManager.hogsDisabled(stringID)) return false;
				return CompareStringDict(StoryManager.groundhogs);

			case StoryReqType.story:
				// use stringID because there may not actually be a real story (eg for once_today)
				int storyMonth = StoryManager.GetStoryMonth(stringID);

				if (compare == StoryReqCompare.lessThan && intValue > 0) {
					// ~if story_happenedRecently < 5
					// make sure it happened (can't == -1)
					if (storyMonth < 0) return false;
				}
				int monthDiff = StoryManager.currentGameMonth - storyMonth;
				if (monthDiff < 0) {
					// could occur with debugging; pretend it happened today
					Debug.LogWarning("Story occurred in the future? " + stringID);
					monthDiff = 0;
				}
				if (storyMonth < 0) {
					// use -1 to indicate it never happened, otherwise count days before today (today == 0)
					// ~if story_someStory -> greaterThan -1
					// ~if !story_someStory -> lessThan 0
					monthDiff = -1;
				}

				return CompareInt(monthDiff);

			case StoryReqType.or:
				foreach (StoryReq subReq in subReqs) {
					if (subReq.Execute(result)) {
						return true;
					}
				}
				return false;

			case StoryReqType.and:
				foreach (StoryReq subReq in subReqs) {
					if (!subReq.Execute(result)) {
						return false;
					}
				}
				return true;

			// case StoryReqType.mapSpot:
			// 	// type used when mapspot not available, but fallback to mapspot
			// 	if (result.mapSpotType != MapSpotType.none) return (int)result.mapSpotType == intValue;
			// 	if (result.mapSpot == null) return false;
			// 	return (int)result.mapSpot.type == intValue;

			case StoryReqType.repeat:
				// checked in Story.CanExecute not here
				return true;

			case StoryReqType.call:
				return CompareObject(call?.Execute());

			case StoryReqType.random:
				return RandomReq(result, isSetIf);

			// case StoryReqType.status:
			// 	if (compare == StoryReqCompare.notEqual) {
			// 		return !Princess.HasStatus(stringID.ParseEnum<StatusID>());
			// 	} else {
			// 		return Princess.HasStatus(stringID.ParseEnum<StatusID>());
			// 	}

			default:
				Debug.LogError("Can't execute unknown StoryReqType " + type);
				return false;
		}
	}

	/// <summary>
	/// Only for choices, not [if random] in text.
	/// If the choice has multiple siblings with Random StoryReqs, pseudo-randomly pick one to show.
	/// This will be run once for each choice with a random storyreq.
	/// Also see TextFilter.FilterIf which uses a different system but same TextFilter.GetRandomSeed
	/// </summary>
	private bool RandomReq(Result result, bool isSetIf) {
		// ~if random! (or chara = low_*) means immediately random, no seed, not based on month etc
		// string seed = TextFilter.GetRandomSeed(story, flagValue ?? false);
		string seed = TextFilter.GetRandomSeed(story, false);
		if (flagValue == true) {
			// truly random doesn't work on choices because this runs for each choice and needs the same randomWeight
			// use numLives (for intro) plus secondsPlayed (updates when game is saved)
			// seed += StoryManager.GetGroundhog("numLives") + Savegame.instance.secondsPlayed;
		}

		if (isSetIf) {
			// ~if random, var_outcome = good, var_outcome = bad
			// ~setif random = 99 ? var_wonLottery = false : var_wonLottery = true // 1 in 100 chance of true

			// random = 3 means 3x more likely to be true than false or 3/4 chance of true
			return NWUtils.RandomChance(intValue, intValue + 1, seed);
		}

		// result.choice is the parent of the choice containing this req
		if (result == null || result.choice == null) {
			Debug.LogError("StoryReq random executing without result or parent choice " + result);
			// ignore this StoryReq rather than failing it
			return true;
		}

		List<StoryChoice> choices = new List<StoryChoice>();
		List<float> weights = new List<float>();
		StoryChoice thisStoryChoice = null;

		foreach (StoryChoice siblingChoice in result.choice.choices) {
			// ignore invisible choices eg *= continue
			if (siblingChoice.buttonText.IsNullOrEmptyOrWhitespace()) continue;
			
			bool canExecute = true;
			int randomWeight = -1;
			
			foreach (StoryReq req in siblingChoice.requirements) {
				if (req.type == StoryReqType.random) {
					if (randomWeight >= 0) {
						Debug.LogWarning("StoryReq with multiple randoms " + req);
					}
					randomWeight = req.intValue;
					if (req == this) {
						thisStoryChoice = siblingChoice;
					}
				} else {
					if (req.HasSubReqType(StoryReqType.random)) {
						Debug.LogWarning("StoryReq has a subreq with random " + req);
						canExecute = false;
						break;
					}
					if (!req.Execute(result)) {
						canExecute = false;
						break;
					}
				}
			}

			if (!canExecute) continue;
			
			if (randomWeight < 0) {
				// this is fine, happens in forageLost
				// Debug.LogWarning("StoryReq Random has a non-random sibling " + this + ", " + result);
				continue;
			}

			choices.Add(siblingChoice);
			weights.Add(randomWeight);
		}

		if (thisStoryChoice == null) {
			Debug.LogError("StoryReq random executing without thisChoice " + result);
			// ignore this StoryReq rather than failing it
			return true;
		}

		// win the coin toss by default
		if (choices.Count < 2) {
			// can happen during intro the first time when there is only one valid random choice
			// Debug.LogWarning("StoryReq random has no siblingChoices with random reqs " + this + ", " + story);
			return true;
		}

		// this story will always have the same random choice available on this date
		StoryChoice randomStoryChoice = choices.PickRandomWeighted(weights, seed);
		return randomStoryChoice == thisStoryChoice;
	}

	/// <summary>
	/// Recursive check for AND > OR > AND > AND > someType.
	/// Used for validation.
	/// </summary>
	private bool HasSubReqType(StoryReqType someType) {
		if (subReqs == null) return false;
		foreach (StoryReq subReq in subReqs) {
			if (subReq.type == someType) return true;
			if (subReq.HasSubReqType(someType)) return true;
		}
		return false;
	}

	/// <summary>
	/// Return true if equals or not equals, depending on StoryReqCompare.
	/// Used for job, location, chara.
	/// </summary>
	public bool CompareStringID(string value) {
		if (compare == StoryReqCompare.notEqual) {
			return value != stringID;
		} else {
			return value == stringID;
		}
	}

	/// <summary>
	/// Requires stringValue to be "true" or "false" and compares against a value supporting negative compare.
	/// </summary>
	public bool CompareBool(bool value) {
		if (stringValue != "true" && stringValue != "false") {
			Debug.LogWarning("StoryReq.CompareBool but stringValue is not true or false " + this);
			return false;
		}
		bool targetValue = (compare == StoryReqCompare.equal && stringValue == "true")
			|| (compare == StoryReqCompare.notEqual && stringValue == "false");
		return value == targetValue;
	}
	
	/// <summary>
	/// Return true if greater or less than or equals, depending on StoryReqCompare.
	/// Used for love, skill, age.
	/// </summary>
	public bool CompareInt(int value) {
		int compareValue = intValue;
		
		// ~if skill_toughness >= call_BestSkillValue()
		if (objectValue != null && objectValue is StoryCall storyCall) {
			object callResults = storyCall.Execute();
			if (callResults != null && callResults is int callResultsInt) {
				compareValue = callResultsInt;
				// also replace the intValue with the current call results so button tooltips can pick it up
				intValue = callResultsInt;
			} else {
				Debug.LogError("StoryReq.CompareInt with null or non-int storyCall results " + this);
			}
		}
		
		if (compare == StoryReqCompare.greaterThan) {
			return value > compareValue;
		} else if (compare == StoryReqCompare.lessThan) {
			return value < compareValue;
		} else if (compare == StoryReqCompare.notEqual) {
			return value != compareValue;
		} else {
			return value == compareValue;
		}
	}

	/// <summary>
	/// Return true if greater or less than or equals, depending on StoryReqCompare.
	/// Used for calls where id may be string, int or bool.
	/// </summary>
	public bool CompareObject(object value) {
		if (value == null) {
			// this will probably never happen but misewell handle it well
			return objectValue == null;
		}
		object compareValue = objectValue;

		// ~if call_something() == call_somethingelse()
		if (compareValue is StoryCall storyCall) {
			compareValue = storyCall.Execute();
		}
		
		if (compareValue == null) {
			Debug.LogError("StoryReq.CompareObject with null objectValue " + this);
			compareValue = true;
		}
		
		if (compare == StoryReqCompare.greaterThan) {
			return value.ToString().ParseInt() > compareValue.ToString().ParseInt();
		} else if (compare == StoryReqCompare.lessThan) {
			return value.ToString().ParseInt() < compareValue.ToString().ParseInt();
		} else if (compare == StoryReqCompare.notEqual) {
			return value.ToString() != compareValue.ToString();
		} else {
			return value.ToString() == compareValue.ToString();
		}
	}

	/// <summary>
	/// Return true if requirement met based on value in the given StringDictionary.
	/// Value may be a bool, int, or string.
	/// Used for memories, storyvars, and groundhogs.
	/// </summary>
	private bool CompareStringDict(StringDictionary dict) {
		if (compare == StoryReqCompare.greaterThan) {
			return dict.GetInt(stringID) > intValue;
		} else if (compare == StoryReqCompare.lessThan) {
			return dict.GetInt(stringID) < intValue;
		} else if (stringValue == null) {
			// "5" or "0"
			if (compare == StoryReqCompare.notEqual) {
				return dict.GetInt(stringID) != intValue;
			} else {
				return dict.GetInt(stringID) == intValue;
			}
		} else {
			bool memBoolValue;
			if (stringValue.TryParseBool(out memBoolValue)) {
				// "true" or "false"
				if (compare == StoryReqCompare.notEqual) {
					return !memBoolValue ? dict.Has(stringID) : !dict.Has(stringID);
				} else {
					return memBoolValue ? dict.Has(stringID) : !dict.Has(stringID);
				}
			} else {
				// "sportsball" or "anemone"
				if (compare == StoryReqCompare.notEqual) {
					return !dict.Get(stringID).EqualsIgnoreCase(stringValue);
				} else {
					return dict.Get(stringID).EqualsIgnoreCase(stringValue);
				}
			}
		}
	}

	/// <summary>
	/// Has it been 1 month / 2 seasons / 4 years since the story was last played?
	/// </summary>
	public bool RepeatPassed(Result result) {
		if (type != StoryReqType.repeat || repeatType == RepeatType.none) {
			Debug.LogError("StoryReq.RepeatPassed on non repeat " + this);
			return false;
		}

		int storyMonth = StoryManager.GetStoryMonth(story.storyID);
		if (storyMonth < 1) {
			return true;
		}

		int monthsToWait = 0;
		if (repeatType == RepeatType.months) {
			monthsToWait = intValue;
		} else if (repeatType == RepeatType.seasons) {
			// 5 months per season (except glow)
			monthsToWait = intValue * 5;
		} else if (repeatType == RepeatType.years) {
			// 21 months per year
			monthsToWait = intValue * 21;
		}

		// monthsPassed could be negative if changing date for testing
		int monthsPassed = Mathf.Clamp(StoryManager.currentGameMonth - storyMonth, 0, int.MaxValue);
		return monthsPassed >= monthsToWait;
	}

	// other validation done in DataLoader when req created
	public bool ValidateAndFinish() {
		foreach (StoryReq subreq in subReqs) {
			subreq.ValidateAndFinish();
		}
		
		if (type == StoryReqType.call) {
			return call.Validate(this);
		} else if (type == StoryReqType.memory) {
			if (!Story.allMemories.Contains(stringID)) {
				Debug.LogWarning("Story contains req with no memory " + stringID + ", " + story);
				return false;
			}
		} else if (type == StoryReqType.groundhog) {
			if (!Story.allGroundhogs.Contains(stringID)) {
				Debug.LogWarning("Story contains req with no groundhog " + stringID + ", " + story);
				return false;
			}
		} else if (type == StoryReqType.storyvar) {
			if (!story.allVars.Contains(stringID)) {
				Debug.LogWarning("Story contains req with no storyvar " + stringID + ", " + story);
			}
		} else if (type == StoryReqType.story) {
			if (!Story.storiesByID.ContainsKey(stringID) && !Story.allSetStoryIDs.Contains(stringID)) {
				Debug.LogWarning("Story contains req with no story " + stringID + ", " + story);
			}
		// } else if (type == StoryReqType.repeat) {
		// 	if (intValue == 0 && story.chara == null && story.mapSpotType == MapSpotType.none) {
		// 		if (story.priority == Priority.high) {
		// 			// high priority will instantly happen again and again, no reason to allow that
		// 			Debug.LogWarning("High-priority Story repeat will fire forever, setting to 1 month " + story);
		// 			intValue = 1;
		// 		} else if (story.priority == Priority.average) {
		// 			// might be some reason to allow this?
		// 			Debug.LogWarning("Average-priority Story repeat will fire every month " + story);
		// 		}
		// 		// low priority can repeat every month that's fine
		// 	}
		}
		
		return true;
	}

	public bool IsDebugOnly() {
		return IsCall("debug") || IsCall("isDebug");
	}
	
	public bool IsCall(string callName = null) {
		if (type != StoryReqType.call) return false;
		if (call == null) return false;
		if (callName == null) return true;
		return callName.ToLower().Trim() == call.methodName;
	}

	/// <summary>
	/// Used for snippets which appear in multiple stories.
	/// Copy this requirement but change the story pointer.
	/// </summary>
	public StoryReq CloneReq(Story story) {
		// most values can be MemberwiseCloned and some can be pointers eg StoryCall
		StoryReq req = (StoryReq)MemberwiseClone();
		req.story = story;

		// subreqs need to be deeply cloned with a new story
		req.subReqs = new List<StoryReq>();
		foreach (StoryReq subReq in subReqs) {
			req.subReqs.Add(subReq.CloneReq(story));
		}

		return req;
	}

	/// <summary>
	/// ~if skill_bravery > 50
	/// becomes
	/// ~if skill_bravery < 51
	/// </summary>
	public StoryReq GetInverse() {
		StoryReq inverse = CloneReq(story);
		if (type == StoryReqType.and || type == StoryReqType.or || type == StoryReqType.repeat) {
			Debug.LogWarning("StoryReq.GetInverse can't invert type " + type);
			return inverse;
		}

		switch (compare) {
			case StoryReqCompare.equal:
				inverse.compare = StoryReqCompare.notEqual;
				break;
			case StoryReqCompare.lessThan:
				inverse.compare = StoryReqCompare.greaterThan;
				inverse.intValue--;
				break;
			case StoryReqCompare.greaterThan:
				inverse.compare = StoryReqCompare.lessThan;
				inverse.intValue++;
				break;
			case StoryReqCompare.notEqual:
				inverse.compare = StoryReqCompare.equal;
				break;
			default:
				Debug.LogWarning("StoryReq.GetInverse can't invert compare type " + compare);
				break;
		}

		return inverse;
	}

	public override string ToString() {
		return string.IsNullOrEmpty(debugString) ? type.ToString() : debugString;
	}
}
