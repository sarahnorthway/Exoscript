using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// Statically parse homebrew scripting language (Exoscript) for dialog events.
/// 
/// Requirements, eg:
///	    ~if age = 16
///	    [if !mem_mom_dead]
/// </summary>
public class StoryParserReq {
	/// <summary>
	/// Return a StoryReq representing something like:
	/// ~if job = shovel
	/// ~if location=geoponics
	/// ~if skill_Toughness >=10
	/// ~if mem_metMom = false
	/// ~if love_mom>50 || love_dad > 50
	/// ~if skill_toughness = 10 && !mem_work_defense || love_mars > 3 AND skill_combat = 20 &job= defense
	/// </summary>
	public static StoryReq ParseReq(string line, Story story, StoryChoice storyChoice = null) {
		try {
			return ParseReqInnerAndOr(line, story, storyChoice);
		} catch (Exception e) {
			Debug.LogError("Invalid story requirement " + line + ", " + e);
			return null;
		}
	}
	
	/// <summary>
	/// Look for AND, OR, ||, |, &&, & and call ParseReqInner on each part.
	/// mem1 and mem2 or mem3 and mem4 = (mem1 and mem2) or (mem3 and mem4)
	/// </summary>
	private static StoryReq ParseReqInnerAndOr(string line, Story story, StoryChoice storyChoice = null) {
		StoryReq req = new StoryReq(story);
		req.debugString = line;

		// line may be a fragment from the middle of an OR or AND statement
		if (line.StartsWith("~ifd")) {
			req.showDisabled = true;
			line = line.RemoveStart("~ifd");
		} else if (line.StartsWith("~if")) {
			line = line.RemoveStart("~if");
		}
		line = line.Trim();

		// OR is parsed first, nested AND will be evaluated first, no further nesting, no braces
		string[] orSplit = line.SplitSafe("||", " | ", " or ", " OR ");
		if (orSplit.Length > 1) {
			req.type = StoryReqType.or;
			foreach (string orString in orSplit) {
				// recursive!
				StoryReq subReq = ParseReq(orString, story);
				if (subReq != null) {
					req.subReqs.Add(subReq);
				}
			}
			if (req.subReqs.Count == 0) {
				Debug.LogWarning("No valid subReqs under OR " + line + ", " + story);
				return null;
			}
			return req;
		}

		// next look for AND
		string[] andSplit = line.SplitSafe("&&", " & ", " and ", " AND ");
		if (andSplit.Length > 1) {
			req.type = StoryReqType.and;
			foreach (string andString in andSplit) {
				// recursive!
				StoryReq subReq = ParseReq(andString, story);
				if (subReq != null) {
					req.subReqs.Add(subReq);
				}
			}
			if (req.subReqs.Count == 0) {
				Debug.LogWarning("No valid subReqs under AND " + line + ", " + story);
				return null;
			}
			return req;
		}

		// evaluate normally whether inside AND, OR or on its own
		// ~if or ~ifd has already been removed
		return ParseReqInner(req, line, story, storyChoice);
	}

	/// <summary>
	/// After "~if " has been removed from the beginning.
	/// </summary>
	private static StoryReq ParseReqInner(StoryReq req, string line, Story story, StoryChoice storyChoice = null) {
		if (req == null) {
			req = new StoryReq(story);
		}

		// split into left, compare, right ignoring spaces, matching:
		// ~if job = shovel
		// ~ifd location=geoponics
		// ~if skill_toughness >=10
		// ~if skill_touGHNEss=<10
		// ~ifd skill_Toughness==   10
		// ~if var_metMom = false
		// ~if mem_something
		// ~if season = pollen-end
		// ~if !hog_something
		// ~if repeat
		// ~if repeat!
		// ~if call_hasPet ( true, 15, Toughness)
		// ~if call_getLove(cal) > cal_getLove(tammy)

		// ~if or ~ifd has already been removed
		// https://regex101.com/
		string pattern = @"(!?\w+(?:\s*\(.*\s*\))?!?)\s*([!=<>]+)\s*([\w-]+(?:\s*\(.*\s*\))?)";
		Match match = Regex.Match(line, pattern);
		if (match == null || string.IsNullOrEmpty(match.Value)) {
			// try again with " = true" at the end
			// ~if mem_something
			match = Regex.Match(line + " = true", pattern);
			if (match == null || string.IsNullOrEmpty(match.Value)) {
				Debug.LogWarning("Invalid story requirement format: " + line + ", " + story);
				return null;
			}
		}

		// match.Groups[0].Value is always the entire string
		// commands are lowercase
		string left = match.Groups[1].Value.Trim().ToLower();
		// should be comparaison symbols = < >
		string compare = match.Groups[2].Value.Trim().ToLower();
		// ids are stored as lowercase even if written camelCase for ease of reading
		string right = match.Groups[3].Value.Trim().ToLower();

		if (GameSettings.validateStories) {
			if (line.LastIndexOf('=') > 0) {
				string rightFull = line.Substring(line.LastIndexOf('=') + 1).Trim();
				if (!rightFull.EqualsIgnoreCase(right)) {
					Debug.LogWarning("Longer rightFull (" + rightFull + ") than right (" + right + "), " + line + ", " + story);
				}
			}
		}
		
		// ~if !mem_something becomes ~if mem_something = false
		if (left.StartsWith("!")) {
			if (compare != "=" || right != "true") {
				Debug.LogWarning("Invalid story requirement exclamation symbol " + line + ", " + story);
				return null;
			}
			left = left.Substring("!".Length);
			right = "false";
		}

		if (left == "job") {
			// ~if job = shovel
			req.type = StoryReqType.job;

			// // low means default stories chosen only if nothing else matches at location
			// // ~if job = low_shovel
			// if (right.StartsWith("high_")) {
			// 	right = right.RemoveStart("high_");
			// 	req.priority = Priority.high;
			// } else if (right.StartsWith("low_")) {
			// 	right = right.RemoveStart("low_");
			// 	req.priority = Priority.low;
			// }
			//
			// // might be a bad idea... job is used to categorize events
			// // ~if job != null
			// if (right.ToLower() == "null" || right.ToLower() == "none" || right.ToLower() == "false") {
			// 	req.stringValue = "false";
			// } else if (right.ToLower() == "true") {
			// 	req.stringValue = "true";
			// } else {
			// 	Job job = Job.FromID(right);
			// 	if (job == null) {
			// 		Debug.LogWarning("Invalid story requirement job " + line + ", " + story);
			// 		return null;
			// 	}
			// 	req.stringID = job.jobID;
			// }
			//
			// if (!ParseReqCompareSimple(req, compare)) {
			// 	Debug.LogWarning("Invalid story requirement job compare " + line + ", " + story);
			// 	return null;
			// }

		} else if (left == "location") {
			// ~if location = geoponics
			req.type = StoryReqType.location;

			// // high location comes before average job
			// // ~if location = high_command
			// if (right.StartsWith("high_")) {
			// 	right = right.RemoveStart("high_");
			// 	req.priority = Priority.high;
			// } else if (right.StartsWith("low_")) {
			// 	Debug.LogWarning("Story location cannot be low priority, using avg " + line + ", " + story);
			// 	right = right.RemoveStart("low_");
			// 	req.priority = Priority.average;
			// 	
			// 	// if (story.storyID != "all") {
			// 	// 	// "all" is a special story name that is a catchall in case some job doesn't have a low_ repeater
			// 	// 	Debug.LogWarning("Story location with low priority will never be called " + line + ", " + story);
			// 	// }
			// 	// right = right.RemoveStart("low_");
			// 	// req.priority = Priority.low;
			// }
			//
			// Location location = Location.FromID(right);
			// if (location == null) {
			// 	Debug.LogWarning("Invalid story requirement location " + line + ", " + story);
			// 	return null;
			// }
			//
			// req.stringID = location.locationID;
			// if (!ParseReqCompareSimple(req, compare)) {
			// 	Debug.LogWarning("Invalid story requirement location compare " + line + ", " + story);
			// 	return null;
			// }

		} else if (left == "chara") {
			// ~if chara = tangent
			// ~if chara = low_mars
			// ~if chara = _high_anemone (no speech bubble)
			// ~if chara = _tammy (no speech bubble)
			// ~if chara = -high_mom (no speech bubble on repeat)
			req.type = StoryReqType.chara;

			// bool disableSpeechBubble = false;
			// if (right.StartsWith("_")) {
			// 	// hide speech bubble despite normal or high priority
			// 	// used for birthdays and other hidden events
			// 	right = right.RemoveStart("_");
			// 	disableSpeechBubble = true;
			// }
			//
			// // high stories are chosen first, low chosen last
			// if (right.StartsWith("high_")) {
			// 	right = right.RemoveStart("high_");
			// 	req.priority = Priority.high;
			// } else if (right.StartsWith("low_")) {
			// 	right = right.RemoveStart("low_");
			// 	req.priority = Priority.low;
			// }
			//
			// if (req.priority != Priority.low && !disableSpeechBubble) {
			// 	// show speech bubble for high or normal priority unless 
			// 	req.flagValue = true;
			// }
			//
			// Chara chara = Chara.FromID(right);
			// if (chara == null) {
			// 	Debug.LogWarning("Invalid story requirement chara " + line + ", " + story);
			// 	return null;
			// }
			//
			// req.stringID = chara.charaID;
			// if (!ParseReqCompareSimple(req, compare)) {
			// 	Debug.LogWarning("Invalid story requirement chara compare " + line + ", " + story);
			// 	return null;
			// }

		} else if (left == "age" || left == "year") {
			// ~if age == 12
			// ~if age < 12
			// ~if age <= 12
			// ~if age =< 12
			// ~if age >= 15-pollen (month >= 15-pollen-start)
			// ~if age == 15-pollen (age == 15 && season == pollen)
			// ~if age == 15-pollen-2
			// ~if age >= 15-dust-mid
			// ~if age < 17-wet-2
			
			if (compare == "=") {
				Debug.LogWarning("Age can't be = use == instead " + line + ", " + story);
				compare = "==";
			}

			string[] parts = right.Split('-');
			if (parts.Length == 3) {
				// ~if age = 10-pollen-3 (month >= 10-pollen-3)
				// ~if age == 15-dust-mid (month == 15-dust-mid)
				// ~if age < 15-dust-mid (month < 15-dust-mid)
				string monthCompare = compare == "=" ? ">=" : compare;
				string monthLine = "month " + monthCompare + " " + right;
				req = ParseReqInner(null, monthLine, story);
				if (req == null) {
					Debug.LogWarning("Invalid story requirement age " + line + ", " + story);
					return null;
				}
				return req;
			}

			if (parts.Length == 2) {
				if (compare == "==") {
					// ~if age == 15-pollen (age == 15 && season == pollen)
					string ageRight = parts[0];
					string seasonRight = parts[1];
					string andLine = "age == " + ageRight + " && season == " + seasonRight;
					req = ParseReq(andLine, story);
					if (req == null) {
						Debug.LogWarning("Invalid story requirement age " + line + ", " + story);
						return null;
					}
					return req;

				} else {
					// ~if age = 15-pollen (month >= 15-pollen-start)
					// ~if age >= 15-pollen (month >= 15-pollen-start)
					// ~if age < 15-pollen (month < 15-pollen-start)
					// ~if age <= 15-pollen (month <= 15-pollen-end)
					// ~if age > 15-pollen (month > 15-pollen-end)
					string monthRight = right + ((compare == "<=" || compare == "=<" || compare == ">") ? "-end" : "-start");
					string monthCompare = compare == "=" ? ">=" : compare;
					string monthLine = "month " + monthCompare + " " + monthRight;
					req = ParseReqInner(null, monthLine, story);
					if (req == null) {
						Debug.LogWarning("Invalid story requirement age " + line + ", " + story);
						return null;
					}
					return req;
				}
			}

			// ~if age <= 12
			req.type = StoryReqType.age;
			if (!ParseReqCompare(req, compare, right, false)) {
				Debug.LogWarning("Invalid story requirement age compare " + line + ", " + story);
				return null;
			}

		} else if (left == "season") {
			// ~if season = quiet (means == quiet)
			// ~if season != dust
			// ~if season = pollen-start
			// ~if season == pollen-end
			// ~if season = pollen-mid
			// ~if season = pollen-2
			// ~if season = dust-4
			// req.type = StoryReqType.season;
			//
			// string[] parts = right.Split('-');
			// if (parts.Length == 0 || parts.Length > 2) {
			// 	Debug.LogWarning("Invalid story requirement season parts " + line + ", " + story);
			// 	return null;
			// }
			//
			// Season season = Season.FromID(parts[0]);
			// if (season == null) {
			// 	Debug.LogWarning("Invalid story requirement season " + line + ", " + story);
			// 	return null;
			// }
			// req.stringID = season.seasonID;
			//
			// // does not support > or <
			// if (!ParseReqCompareSimple(req, compare)) {
			// 	Debug.LogWarning("Invalid story requirement chara compare " + line + ", " + story);
			// 	return null;
			// }
			//
			// // ~if season = pollen-2
			// // compare to exact monthOfYear
			// if (parts.Length > 1) {
			// 	req.intValue = Season.GetMonthOfYear(right);
			// 	if (req.intValue <= 0) {
			// 		Debug.LogWarning("Invalid story requirement season " + line + ", " + story);
			// 		return null;
			// 	}
			// 	// leave it there? does anything need "wet" in the stringID as well as MonthOfYear in the intValue?
			// 	// req.stringID = null;
			// } else {
			// 	req.intValue = 0;
			// }

		} else if (left.StartsWith("month") || left.StartsWith("week")) {
			// ~if month == 10-dust-3
			// ~if month >= 15-pollen-mid
			// ~if month == 16-quiet-end
			// ~if month == pollen-2  (handled by "season")
			// ~if month == 15-pollen  (handled by "age")
			// ~if week = 10-dust-3 (months used to be called weeks)
			req.type = StoryReqType.month;

			// ~if month == pollen-2  (handled by "season")
			// ~if month == 15-pollen  (handled by "age")
			// string[] parts = right.Split('-');
			// if (parts.Length == 2) {
			// 	if (Season.FromID(parts[0]) != null) {
			// 		return ParseReqInner(null, "season " + compare + " " + right, story);
			// 	} else {
			// 		return ParseReqInner(null, "age " + compare + " " + right, story);
			// 	}
			// }
			//
			// req.intValue = Season.GetMonthOfGame(right);
			// if (req.intValue < 0) {
			// 	Debug.LogWarning("Invalid story requirement month " + line + ", " + story);
			// 	return null;
			// }
			//
			// // assume we only want exactly on the date, not after
			// if (compare == "=") {
			// 	Debug.LogWarning("Month with = should be == " + line + ", " + story);
			// 	compare = "==";
			// }
			//
			// if (!ParseReqCompare(req, compare, req.intValue.ToString(), false)) {
			// 	Debug.LogWarning("Invalid story requirement month compare " + line + ", " + story);
			// 	return null;
			// }

		} else if (left.StartsWith("mem_")) {
			// ~if mem_metMom = false
			req.type = StoryReqType.memory;

			// there is no list of memoryIDs as they are defined when required (here) or set by stories
			string stringID = left.Substring("mem_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story requirement memory " + line + ", " + story);
				return null;
			}
			req.stringID = stringID;

			if (!ParseReqCompare(req, compare, right, true)) {
				Debug.LogWarning("Invalid story requirement memory compare " + line + ", " + story);
				return null;
			}

		} else if (left.StartsWith("var_")) {
			// ~if var_toy = sportsball
			req.type = StoryReqType.storyvar;

			// there is no list of storyvarIDs as they are defined when required (here)
			string stringID = left.Substring("var_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story requirement storyVar " + line + ", " + story);
				return null;
			}
			req.stringID = stringID;
			if (!ParseReqCompare(req, compare, right, true)) {
				Debug.LogWarning("Invalid story requirement storyVar compare " + line + ", " + story);
				return null;
			}

		} else if (left.StartsWith("hog_")) {
			// ~if hog_timesDied = 3 (means >= 3 like var_ and mem_)
			// ~if hog_foundSisterHideout
			req.type = StoryReqType.groundhog;

			// there is no list of groundhogIDs as they are defined when required (here)
			string stringID = left.Substring("hog_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story requirement groundhog " + line + ", " + story);
				return null;
			}
			req.stringID = stringID;
			if (!ParseReqCompare(req, compare, right, true)) {
				Debug.LogWarning("Invalid story requirement groundhog compare " + line + ", " + story);
				return null;
			}

		} else if (left.StartsWith("story_") || left.StartsWith("first")) {
			// ~if story_happened
			// ~if story_happened = true
			// ~if story_happened > -1
			// ~if story_threeMonthsPassed = 3
			// ~if story_threeMonthsPassed >= 3
			// ~if story_happenedRecently < 5
			// ~if !story_neverHappened
			// ~if story_neverHappened = false
			// ~if story_neverHappened == false
			// ~if story_neverHappened < 0

			// ~if firstTime
			// ~if first
			// equivalent to:
			// ~if story_thisStoryID == false

			if (left.StartsWith("first")) {
				left = "story_" + story.storyID;
				compare = "==";
				// !first was turned into first == false above, now make it story_X == true
				right = (right == "false") ? "true" : "false";
			}

			req.type = StoryReqType.story;

			// there is no list of storyvarIDs as they are defined when required (here)
			string stringID = left.Substring("story_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story requirement story " + line + ", " + story);
				return null;
			}
			req.stringID = stringID;

			// can be set to "true", "false", or an int, but not other strings
			if (!ParseReqCompare(req, compare, right, true)) {
				Debug.LogWarning("Invalid story requirement story compare " + line + ", " + story);
				return null;
			}

			// "true" means happened > -1 months ago (0 months ago means happened this month)
			// "false" means happened < 0 months ago (Princess.GetStoryMonth returns -1 for never)
			if (req.stringValue != null) {
				bool storyExistsBool;
				if (!right.TryParseBool(out storyExistsBool)) {
					Debug.LogWarning("Invalid story requirement story value " + line + ", " + story);
					return null;
				}
				if (storyExistsBool) {
					// happened at least 0 months ago (ever)
					req.intValue = -1;
					req.compare = StoryReqCompare.greaterThan;
				} else {
					// never happened (-1 indicates never)
					req.intValue = 0;
					req.compare = StoryReqCompare.lessThan;
				}
				req.stringValue = null;
			}

		} else if (left == "repeat_today") {
			if (story == null) {
				Debug.LogError("ParserStoryReq failed to parse repeat_today with no story " + line);
				return null;
			}
			
			// used for charas to repeat forever on whatever day this story played first, effectively:
			// ~if repeat && !story_STORYID || story_STORYID < 1
			string repeatLine = "repeat && !story_" + story.storyID + " || story_" + story.storyID + " < 1";
			return ParseReqInnerAndOr(repeatLine, story, storyChoice);

		} else if (left.StartsWith("repeat")) {
			// ~if repeat (means repeat_months = 0, repeat immediately)
			// ~if repeat_month (means repeat_months = 1, repeat next month but not today)
			// ~if repeat_months = 2
			// ~if repeat_week (old notation, months used to be called weeks
			// ~if repeat_season
			// ~if repeat_seasons = 3
			// ~if repeat_years = 4
			// ~if repeat_collectible = forage

			req.type = StoryReqType.repeat;
			// default to 0 months (effectively immediate) - required for anemoneRepeat and repeat_today
			req.intValue = 0;
			req.repeatType = RepeatType.months;

			if (left.StartsWith("repeat_")) {
				// ~if repeat_season defaults to 1 season
				req.intValue = 1;
				string repeatTypeString = left.Substring("repeat_".Length).Trim().ToLower();
				if (!repeatTypeString.EndsWith("s")) {
					repeatTypeString += "s";
				}
				if (repeatTypeString == "weeks") repeatTypeString = "months"; // renamed weeks to months Nov 2020
				RepeatType repeatType = repeatTypeString.ParseEnum<RepeatType>();
				if (repeatType == RepeatType.none) {
					Debug.LogWarning("Invalid story requirement repeat " + line + ", " + story);
					return null;
				}
				req.repeatType = repeatType;
			}

			if (!right.IsNullOrEmpty() && right != "true" && right != "false") {
				// right may be "true" eg for "~if repeat or ~if repeat_year"
				req.intValue = right.ParseInt(1);
				//if (req.repeatType == RepeatType.collectibles) {
				//	Job job = Job.FromID(right);
				//	if (job == null) {
				//		Debug.LogWarning("Invalid story requirement repeat jobID " + line + ", " + story);
				//		return null;
				//	}
				//	req.stringValue = job.jobID;
				//}
			}

		} else if (left.StartsWith("call_")) {

			// ~if call_methodName
			// ~if call_methodName()
			// ~if call_methodName ( false )
			// ~if call_methodName(true, 15, Toughness)
			// ~if call_mostlove == anemone
			// ~if call_mostlove() != mars
			// ~if call_daysSinceWar >= 30
			req.type = StoryReqType.call;
			
			string methodCall = left.Substring("call_".Length).Trim().ToLower();
			StoryCall call = StoryParserSet.ParseCall("~call " + methodCall);
			if (call == null) {
				Debug.LogWarning("Invalid story requirement call " + line + ", " + story);
				return null;
			}
			req.call = call;
			call.req = req;

			if (!ParseReqCompare(req, compare, right, true)) {
				Debug.LogWarning("Invalid story requirement call compare " + line + ", " + story);
				return null;
			}

			if (req.objectValue == null) {
				// calls use objectValue - bool, int, string, or another call producing one of those
				req.objectValue = right.ParseBoolIntString();
				if (req.objectValue is int) {
					// int may have been modified in ParseReqCompare (eg >= becomes > with intValue--)
					req.objectValue = req.intValue;
				}
			}
			
			// calls compare to bool, int, string, or even another call
			// if (right.StartsWith("call_")) {
			// 	string rightMethodCall = right.Substring("call_".Length).Trim().ToLower();
			// 	StoryCall rightCall = ParserStorySet.ParseCall("~call " + rightMethodCall);
			// 	if (rightCall == null) {
			// 		Debug.LogWarning("Invalid story requirement call_right " + line + ", " + story);
			// 		return null;
			// 	}
			// 	req.objectValue = rightCall;
			// } else {
			// 	req.objectValue = right.ParseBoolIntString();
			// 	// int may have been modified in ParseReqCompare (eg >= becomes > with intValue--)
			// 	if (req.objectValue is int) {
			// 		req.objectValue = req.intValue;
			// 	}
			// }

		} else if (left.Contains("random")) {
			// ~if random
			// ~if random = 3
			// ~if random!

			// pick between choices pseudo-randomly (based on month) weighting by the right side (default 1)
			// [if random] also used to pick random text variation; processed in TextFilter
			req.type = StoryReqType.random;
			// right may be "true" which parses to 0, otherwise right value is the weight eg = 3 means 3x more likely to be chosen
			req.intValue = right.ParseInt();
			if (req.intValue == 0) {
				req.intValue = 1;
			}
			if (left.Contains("!")){
				// means truly instantly random, not tied to the current month
				req.flagValue = true;
			}

		} else if (left == "biome") {
			// ~if biome = quiet (means == quiet)
			// ~if biome != nearby
			req.type = StoryReqType.biome;

			req.stringID = right.ToLower();

			// does not support > or <
			if (!ParseReqCompareSimple(req, compare)) {
				Debug.LogWarning("Invalid story requirement biome compare " + line + ", " + story);
				return null;
			}

		} else if (left == "status") {
			// ~if status = mourning
			// ~if status != stressed
			req.type = StoryReqType.status;

			// StatusID statusID = right.ParseEnum<StatusID>();
			// if (statusID == StatusID.none) {
			// 	Debug.LogWarning("Invalid story requirement status " + line + ", " + story);
			// 	return null;
			// }
			// req.stringID = statusID.ToString();

			// does not support > or <
			if (!ParseReqCompareSimple(req, compare)) {
				Debug.LogWarning("Invalid story requirement status compare " + line + ", " + story);
				return null;
			}

		} else {
			Debug.LogWarning("Invalid story requirement format " + line + ", left: " + left + ", compare: " + compare + ", right: " + right + ", story: " + story);
			return null;
		}

		return req;
	}

	/// <summary>
	/// Only =, ==, or != are allowed in simple string compares.
	/// Used for job, location, chara.
	/// </summary>
	private static bool ParseReqCompareSimple(StoryReq req, string compare) {
		switch (compare) {
			case "=":
			case "==":
				req.compare = StoryReqCompare.equal;
				break;
			case "!":
			case "!=":
			case "!==":
				req.compare = StoryReqCompare.notEqual;
				break;
			default:
				return false;
		}
		return true;
	}

	/// <summary>
	/// Right side may be an int, or bool/string.
	/// If an int, can do >= comparaisons as well as ==.
	/// For ints, "=" always means ">=". Use "==" for explicit equals.
	/// </summary>
	/// <param name="allowStringBool">Allow string or bool values as well as int; used for memories</param>
	private static bool ParseReqCompare(StoryReq req, string compare, string right, bool allowStringBool) {
		bool rightIsInt;
		bool rightIsCall = false;
		
		if (right.StartsWith("call_")) {
			rightIsCall = true;
			string methodCall = right.RemoveStart("call_").Trim().ToLower();
			StoryCall call = StoryParserSet.ParseCall("~call " + methodCall);
			if (call == null) {
				Debug.LogWarning("Invalid req call_right " + req.debugString);
				return false;
			}
			if (call.methodInfo.ReturnType != typeof(bool) && call.methodInfo.ReturnType != typeof(int)
				&& (!allowStringBool || call.methodInfo.ReturnType != typeof(string))) {
				// must return bool, int, or string if allowed
				Debug.LogWarning("Invalid req call_right return type " + call.methodInfo.ReturnType + ", " + req.debugString);
				return false;
			}
			
			req.objectValue = call;
			rightIsInt = call.methodInfo.ReturnType == typeof(int);

		} else {
			string stringValue = right.Trim().ToLower();
			
			rightIsInt = right.TryParseInt(out req.intValue);
			if (!rightIsInt) {
				if (!allowStringBool) {
					// invalid age or skill eg ~if age = watermelons
					Debug.LogWarning("Invalid req compare value must be int " + req.debugString);
					return false;
				}

				// ~if mem_bff = null means mem_bff has not been set
				if (stringValue == "null" || stringValue == "blank" || stringValue == "broken" || string.IsNullOrEmpty(stringValue)) {
					stringValue = "false";
				}
				req.stringValue = stringValue;
			} else if (req.intValue.ToString() != right.Trim().ToLower()) {
				Debug.LogWarning("Right parses to int but has extra cruft: " + req.debugString + ", " + req.story?.storyID);
			}
		}

		switch (compare) {
			case "==":
				// age == 12 means age is exactly 12
				req.compare = StoryReqCompare.equal;
				break;
			case "=":
				// age = 12 USED to mean age > 11 but now means nothing, use >= or == please
				if (rightIsInt) {
					Debug.LogWarning("No more = thank you, only == or >= " + req.debugString);
					return false;

					// req.compare = StoryReqCompare.greaterThan;
					// req.intValue--;
				} else {
					req.compare = StoryReqCompare.equal;
				}
				break;
			case "!":
			case "!=":
			case "!==":
				req.compare = StoryReqCompare.notEqual;
				break;
			case ">=":
			case "=>":
				if (rightIsCall) {
					Debug.LogWarning("Invalid requirement compare to call with >= use > " + req.debugString);
					return false;
				}
				req.intValue--;
				req.compare = StoryReqCompare.greaterThan;
				break;
			case ">":
				req.compare = StoryReqCompare.greaterThan;
				break;
			case "<=":
			case "=<":
				if (rightIsCall) {
					Debug.LogWarning("Invalid requirement compare to call with <= use < " + req.debugString);
					return false;
				}
				req.intValue++;
				req.compare = StoryReqCompare.lessThan;
				break;
			case "<":
				req.compare = StoryReqCompare.lessThan;
				break;
			default:
				return false;
		}
		
		if (allowStringBool && !rightIsInt && (req.compare != StoryReqCompare.equal && req.compare != StoryReqCompare.notEqual)) {
			// invalid memory eg ~if mem_favorite >= sportsball
			Debug.LogWarning("Invalid requirement compare to string or bool with > or < " + req.debugString);
			return false;
		}

		return true;
	}

}
