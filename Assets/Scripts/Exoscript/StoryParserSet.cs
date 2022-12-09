using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Statically parse homebrew scripting language (Exoscript) for dialog events.
/// 
/// Sets, Calls, and Jumps, eg:
///	    ~set love_anemone++
///	    ~call battle(social)
///	    [=call_charaAge(cal)]
///	    >start
/// </summary>
public class StoryParserSet {

	/// <summary>
	/// Return a StorySet with an If (StoryReq) clause, like:
	/// ~setif love_cal >= 3 ? skill_toughness++
	/// ~setif mem_dead_mom && mem_dead_dad ? mem_secondCultivator = player
	/// ~setif mem_something ? love_mars++ : love_mars--
	/// ~callif call_charaBirthday(tammy) ? charafact(tammy, birthday)
	/// ~callif season = pollen ? story(example_pollen) : story(example_not_pollen)
	/// </summary>
	public static StorySet ParseSetIf(string line, Story story, StoryChoice storyChoice) {
		try {
			return ParseSetIfInner(line, story, storyChoice);
		} catch (Exception e) {
			Debug.LogError("Invalid story setif " + line + ", " + e);
			return null;
		}
	}

	private static StorySet ParseSetIfInner(string line, Story story, StoryChoice storyChoice) {
		string[] parts = line.SplitSafe("?", ":");
		if (parts.Length < 2 || parts.Length > 3) {
			Debug.LogWarning("Invalid story setif parts (" + parts.Length + "), " + line + ", " + story);
			return null;
		}

		bool isCall = line.StartsWith("~callif");
		string setLine = (isCall ? "~call " : "~set ") + parts[1].Trim();
		StorySet set = ParseSet(setLine, story, storyChoice);
		if (set == null) {
			return null;
		}

		parts[0] = parts[0].RemoveStart("~setif").RemoveStart("~callif").Trim();
		string reqLine = "~if " + parts[0];
		StoryReq req = StoryParserReq.ParseReq(reqLine, story);
		if (req == null) {
			return null;
		}
		set.requirement = req;

		if (parts.Length == 3) {
			string elseLine = (isCall ? "~call " : "~set ") + parts[2].Trim();
			StorySet elseSet = ParseSet(elseLine, story, storyChoice);
			if (elseSet == null) {
				return null;
			}
			set.elseSet = elseSet;
		}

		return set;
	}

	/// <summary>
	/// Return a StorySet representing something like:
	/// ~set left = momHappy
	/// ~set rel_mom++
	/// ~set mem_metMom=true
	/// ~set skill_Toughness += 2
	/// ~set skill_toughness-2
	/// </summary>
	public static StorySet ParseSet(string line, Story story, StoryChoice storyChoice) {
		try {
			return ParseSetInner(line, story, storyChoice);
		} catch (Exception e) {
			Debug.LogError("Invalid story set " + line + ", " + e);
			return null;
		}
	}

	private static StorySet ParseSetInner(string line, Story story, StoryChoice storyChoice) {
		if (line.StartsWith("~call")) {
			return ParseCallSet(line, story, storyChoice);
		}
		
		// split into left, compare, right ignoring spaces, matching:
		// ~set left = momHappy
		// ~set rel_mom++
		// ~set mem_metMom= true
		// ~set skill_Toughness +=2
		// ~set skill_toughness-2
		// ~set mem_something
		// ~set !mem_something
		// ~set var_found = call_findCollectible(wood)
		// ~set title = Something with Spaces And Capital Letters (right == "Something", rightFull = "Something with...")
		// ~set mem_fact_rex_favorite = He feels awkward about Cool Blue Flowers.
		// https://regex101.com/
		string pattern = @"\~set\s+(!?\w+)\s*([=+-]+)\s*([\w\.]+|\+|\-)";
		Match match = Regex.Match(line, pattern);
		if (match == null || string.IsNullOrEmpty(match.Value)) {
			// try again with " = true" at the end
			// ~set mem_something
			match = Regex.Match(line + " = true", pattern);
			if (match == null || string.IsNullOrEmpty(match.Value)) {
				Debug.LogWarning("Invalid story set format " + line + ", " + story);
				return null;
			}
		}

		// match.Groups[0].Value is always the entire string
		// commands are lowercase
		string left = match.Groups[1].Value.Trim().ToLower();
		// should be addition symbols = + -
		string compare = match.Groups[2].Value.Trim().ToLower();
		// should be an integer, a boolean, or + or - (for ++ or --)
		string right = match.Groups[3].Value.Trim().ToLower();

		// eg ~set title = Something With Spaces And Capital Letters
		string rightFull = right;
		if (line.Contains("=")) {
			rightFull = line.Substring(line.LastIndexOf('=') + 1).Trim();
		}

		if (GameSettings.validateStories) {
			if (line.LastIndexOf('=') > 0) {
				if (!rightFull.EqualsIgnoreCase(right) && !right.StartsWith("call_")) {
					if (!left.StartsWith("mem_") || (!left.Contains("name") && !left.Contains("nick") && !left.Contains("_fact_") && !left.Contains("_dead_"))) {
						Debug.LogWarning("Longer rightFull (" + rightFull + ") than right (" + right + "), " + line + ", " + story);
					}
				}
			}
		}

		// ~set !mem_something becomes ~set mem_something = false
		if (left.StartsWith("!")) {
			if (compare != "=" || right != "true") {
				Debug.LogWarning("Invalid story set exclamation symbol " + line + ", " + story);
				return null;
			}
			left = left.Substring("!".Length);
			compare = "=";
			right = "false";
		}
		
		//Debug.Log("left:" + left + ", compare:" + compare + ", right:" + right);

		StorySet set = new StorySet(story, line);

		// ~set var_found = call_findCollectibles(wood))
		if (right.StartsWith("call_")) {
			// ~call findCollectibles(wood))
			string callLine = "~call " + rightFull.RemoveStart("call_");
			// will override the stringValue and possibly intValue and/or boolValue fields
			set.call = ParseCall(callLine);
			if (set.call == null) {
				Debug.LogWarning("Invalid set call " + line + ", " + story);
				return null;
			}
		}

		if (left == "bg" || left == "image") {
			// ~set bg = something.jpg
			// ~set bg = something
			// ~set bg = none
			set.type = StorySetType.background;

			foreach (StorySet existingSet in storyChoice.sets) {
				if (existingSet.type == StorySetType.background) {
					Debug.LogWarning("Choice already has a background image " + line + ", " + story);
					return null;
				}

				if (existingSet.type == StorySetType.charaImage) {
					Debug.LogWarning("Choice chara image would be cleared by bg " + line + ", " + story);
					return null;
				}
			}

			if (right == "null" || right == "false" || right == "blank" || right == "clear" || right == "none" ||
				right == "broken") {
				set.stringValue = "";
			} else {
				// if (!BackgroundMenu.BgImageExists(right)) {
				// 	Debug.LogWarning("Invalid background image " + line + ", " + story);
				// 	return null;
				// }

				set.stringValue = right;
			}
			
		} else if (left == "chara" || left == "charas" 
			|| left == "left" || left == "midleft" || left == "midright" || left == "right") {
			// ~set midleft = marz
			// ~set midleft = none
			// ~set charas = none
			set.type = StorySetType.charaImage;

			if (left == "chara" || left == "charas") {
				set.intValue = (int)CharaImageLocation.none;
			} else {
				set.intValue = (int)left.ParseEnum<CharaImageLocation>();
			}

			if (right == "null" || right == "false" || right == "blank" || right == "clear"
				|| right == "none" || right == "broken") {
				set.stringValue = "";
			} else {
				// ~set chara = marz not allowed
				if (left == "chara" || left == "charas") {
					Debug.LogWarning("Invalid story set charas to non-null " + line + ", " + story);
					return null;
				}
				
				// "anemone", "dys_sad", "marz3_happy", "hopeye", or "spacer"
				// set.stringValue = right;
				// if (!right.StartsWith("mem_") && !right.StartsWith("call_") && !CharaImage.SpriteExists(right)) {
				// 	Debug.LogWarning("Invalid story set unknown chara sprite " + line + ", " + story);
				// 	return null;
				// }	
			}

		} else if (left == "speaker") {
			// ~set speaker = tammy
			// ~set speaker = none
			set.type = StorySetType.speaker;
			if (right == "null" || right == "false" || right == "blank" || right == "clear"
				|| right == "none" || right == "broken") {
				set.stringValue = "";
			} else {
				set.stringValue = right;
			}

		} else if (left == "sprite") {
			// ~set sprite = sym
			set.type = StorySetType.billboardSprite;
			set.stringValue = right;

		} else if (left == "card" || left == "card_hidden") {
			// ~set card = tang1
			// ~set card = unique_dys1
			// ~set card = upgrade_tammy3
			// ~set card = unique_upgrade_vace3
			// ~set card_hidden = someCardNotShownInResultsMenu

			set.type = StorySetType.card;

			// if (left == "card_hidden") {
			// 	set.stringValue = "hidden";
			// }
			//
			// CardData card = CardData.FromID(right);
			// if (card == null) {
			// 	Debug.LogWarning("Invalid story set card " + line + ", " + story);
			// 	return null;
			// }
			// if (set.boolValue && card.upgradeFromCardID.IsNullOrEmpty()) {
			// 	// can't upgrade to this card because it is not part of an upgrade chain
			// 	Debug.LogWarning("Invalid story set card upgrade " + line + ", " + story);
			// 	return null;
			// }
			//
			// if (card.howGet == HowGet.unique) {
			// 	// cards flagged as unique don't need "unique_"
			// 	// player can only have one, including upgrades of the card (but ignoring downgrades)
			// 	set.intValue = 1;
			// }
			//
			// if (set.intValue == 1) {
			// 	if (card.upgradeFrom != null) {
			// 		// unique cards that are upgrades must try to upgrade
			// 		// if possible replace any lower order card with this one, otherwise grant this card
			// 		set.boolValue = true;
			// 	}
			// }
			//
			// set.stringID = card.cardID;

		} else if (left.StartsWith("skill_")) {
			// ~set skill_Toughness +=2
			// ~set skill_toughness-2
			set.type = StorySetType.skill;

			// // validate skill statusID eg "toughness"
			// string stringID = left.Substring("skill_".Length);
			// Skill skill = Skill.FromID(stringID);
			// if (skill == null) {
			// 	Debug.LogWarning("Invalid story set skill " + line + ", " + story);
			// 	return null;
			// }
			// set.stringID = skill.skillID;
			//
			// if (!ParseSetCompare(set, compare, right)) {
			// 	Debug.LogWarning("Invalid story set skill "+ line + ", " + story);
			// 	return null;
			// }

		} else if (left.StartsWith("love_")) {
			// ~set love_mom++
			// ~set love_rex - 1
			set.type = StorySetType.love;

			// // validate skill statusID eg "toughness"
			// string stringID = left.Substring("love_".Length);
			// Chara chara = Chara.FromID(stringID);
			// if (chara == null) {
			// 	Debug.LogWarning("Invalid story requirement love " + line + ", " + story);
			// 	return null;
			// }
			// if (!chara.canLove) {
			// 	Debug.LogWarning("Invalid story requirement love chara " + line + ", " + story);
			// 	return null;
			// }
			// set.stringID = chara.charaID;
			//
			// if (!ParseSetCompare(set, compare, right)) {
			// 	Debug.LogWarning("Invalid story set love " + line + ", " + story);
			// 	return null;
			// }

		} else if (left.StartsWith("mem_")) {
			// ~set mem_metMom = false
			set.type = StorySetType.memory;

			// there is no list of memoryIDs as they are defined when required (here) or set by stories
			string stringID = left.Substring("mem_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story set memory " + line + ", " + story);
				return null;
			}
			set.stringID = stringID;

			if (!ParseSetCompare(set, compare, right, true)) {
				Debug.LogWarning("Invalid story set memory " + line + ", " + story);
				return null;
			}

			//if (set.compare == StorySetCompare.equal && set.stringValue.ToLower() != rightFull.ToLower()) {
			if (set.compare == StorySetCompare.equal && !string.IsNullOrEmpty(set.stringValue)
				&& set.stringValue != "true" && set.stringValue != "false") {
				// ~set mem_anemoneNick = AnEmOnE
				// ~set mem_fact_rex_favorite = He feels awkward about Cool Blue Flowers.
				set.stringValue = rightFull;
			}

		} else if (left.StartsWith("var_")) {
			// ~set var_choseWho = anemone
			set.type = StorySetType.storyvar;

			// there is no list of storyvarIDs as they are defined when required (here) and only exist within one story
			string stringID = left.Substring("var_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story set storyvar " + line + ", " + story);
				return null;
			}
			set.stringID = stringID;

			if (!ParseSetCompare(set, compare, right, true)) {
				Debug.LogWarning("Invalid story set storyvar " + line + ", " + story);
				return null;
			}

		} else if (left.StartsWith("hog_")) {
			// ~set hog_foundSisterHideout
			// ~set hog_timesDied++
			set.type = StorySetType.groundhog;

			// there is no list of storyvarIDs as they are defined when required (here) and only exist within one story
			string stringID = left.Substring("hog_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story set groundhog " + line + ", " + story);
				return null;
			}
			set.stringID = stringID;
			if (!ParseSetCompare(set, compare, right, true)) {
				Debug.LogWarning("Invalid story set groundhog compare " + line + ", " + story);
				return null;
			}

		} else if (left.StartsWith("story_")) {
			// ~set story_something = false
			set.type = StorySetType.story;

			string stringID = left.Substring("story_".Length).Trim().ToLower();
			if (string.IsNullOrEmpty(stringID)) {
				Debug.LogWarning("Invalid story set story " + line + ", " + story);
				return null;
			}
			set.stringID = stringID;
			
			// don't include numbers, only "true" and "false"
			// if (!right.TryParseBoolSafe(out set.boolValue)) {
			// 	Debug.LogWarning("Invalid story set story value " + line + ", " + story);
			// 	return null;
			// }
			if (!right.TryParseBool(out set.boolValue)) {
				Debug.LogWarning("Invalid story set story value " + line + ", " + story);
				return null;
			}

		} else if (left == "next") {
			// ~set next = someStoryID
			Debug.LogError("ParserStorySet next tag deprecated " + line + ", " + story);
			return null;
			// set.type = StorySetType.nextStory;
			// set.stringID = right;

		} else if (left == "effect") {
			// ~set effect = screenShake
			set.type = StorySetType.effect;
			EffectType effectType = EffectType.none;
			if (right != "none" && right != "null") {
				effectType = right.ParseEnum<EffectType>();
				if (effectType == EffectType.none) {
					Debug.LogWarning("Invalid story set effect " + line + ", " + story);
					return null;
				}
			}
			set.stringID = effectType.ToString();

		} else if (left == "status") {
			// ~set status = grounded
			// ~set status = remove_starving

			set.type = StorySetType.status;
			
			string statusID = right;
			if (statusID.StartsWith("remove_") || statusID.EndsWith("_remove")) {
				statusID = statusID.RemoveStart("remove_").RemoveEnding("_remove");
				set.boolValue = true;
			}
				
			// Status status = Status.FromID(statusID);
			// if (status == null) {
			// 	Debug.LogWarning("Invalid story set statusID " + line + ", " + story);
			// 	return null;
			// }
			// set.stringID = status.statusID.ToString();

		} else {
			Debug.LogWarning("Invalid story set format " + line + ", left: " + left 
				+ ", compare: " + compare + ", right: " + right + ", " + story);
			return null;
		}

		return set;
	}

	/// <summary>
	/// Determine StorySet.compare, StorySet.stringValue and possibly StorySet.intValue.
	/// </summary>
	/// <param name="isString">Allow "thing = watermelons"</param>
	private static bool ParseSetCompare(StorySet set, string compare, string right, bool isString = false) {
		set.stringValue = right.Trim().ToLower();
		bool parsedInt = right.TryParseInt(out set.intValue);
		if (parsedInt && set.intValue.ToString() != right.Trim().ToLower()) {
			Debug.LogWarning("Right parses to int but has extra cruft: " + set.debugString + ", " + set.story?.storyID);
		}
		
		set.compare = StorySetCompare.increment;

		switch (compare) {
			case "=":
			case "==":
				set.compare = StorySetCompare.equal;
				if (isString) {
					// allowing "thing = watermelons" so ignore int parsing errors
					parsedInt = true;
				}
				break;
			case "+":
				if (right == "+") {
					// ~set skill_Toughness ++
					set.intValue = 1;
					// ignore right entirely, treat it as "thing + 1"
					parsedInt = true;
				}
				break;
			case "+=":
				break;
			case "-":
				if (right == "-") {
					set.intValue = -1;
					// ignore right entirely, treat it as "thing - 1"
					parsedInt = true;
				} else {
					set.intValue = -1 * set.intValue;
				}
				break;
			case "-=":
				set.intValue = -1 * set.intValue;
				break;
			default:
				return false;
		}

		if (!parsedInt) {
			// eg ~set age = watermelons or ~set mem_favorite + balloons
			return false;
		}

		return true;
	}

	/// <summary>
	/// Turn a ~call line into a StorySet encapsulating that call.
	/// </summary>
	public static StorySet ParseCallSet(string line, Story story, StoryChoice storyChoice) {
		StoryCall call = ParseCall(line);
		if (call == null) return null;
		
		StorySet set = new StorySet(story, line);
		set.type = StorySetType.call;
		set.call = call;
		call.set = set;
		
		// moved up
		// special case - battles should always have a page break above them
		// if (choice != null && line.StartsWith("~call battle")) {
		// 	if (!string.IsNullOrEmpty(choice.resultText)) {
		// 		choice = ParserStory.AddPageBreak(choice);
		// 	}
		// }

		//choice.AddSet(set); // done outside
		return set;
	}

	/// <summary>
	/// Call an arbitrary function from StoryCalls.cs
	/// </summary>
	public static StoryCall ParseCall(string line) {
		try {
			// ~call methodName
			// ~call methodName()
			// ~call methodName ( false )
			// ~call methodName(true, 15, Toughness)
			string pattern = @"\~call\s+(\w+)(\s*\(.+\s*\))?";
			Match match = Regex.Match(line, pattern);
			if (!match.Success) {
				Debug.LogWarning("Invalid story call format " + line);
				return null;
			}

			// eg "methodName"
			// lowercase enforced
			string methodName = match.Groups[1].Value.Trim().ToLower();

			// optional eg "(true)" or "(15)" or " ( Toughness ) " or "(true, 15, watermelon!)" or "(null)"
			// lowercase enforced
			object[] parameterArray = new object[0];
			if (match.Groups.Count == 3) {
				List<object> parameterList = new List<object>();
				//string parameterString = match.Groups[2].Value.Trim().Trim('(', ')').Trim().ToLower();
				// uppercase allowed for ~call setName(Solanaceae);
				string parameterString = match.Groups[2].Value.Trim().Trim('(', ')').Trim();
				if (!string.IsNullOrEmpty(parameterString)) {
					string[] parameterSplit = parameterString.Split(',');
					foreach (string parameter in parameterSplit) {
						parameterList.Add(parameter.ParseBoolIntString());
					}
					parameterArray = parameterList.ToArray();
				}
			}

			StoryCall call = StoryCall.CreateStoryCall(methodName, parameterArray, line);
			if (call == null) {
				Debug.LogWarning("Invalid story call " + line);
				return null;
			}
			
			return call;
		} catch (Exception e) {
			Debug.LogError("Invalid story call " + line + ", " + e);
			return null;
		}
	}

	/// <summary>
	/// Type of Set that moves to another choice in the story.
	/// Based on its choiceID or special labels "start", "back", "startonce", "backonce"
	/// > question2showtext
	/// >> question3ignoretext
	/// > snippet_doneboss
	/// >>>
	/// >> start
	/// >> back
	/// > backonce
	/// >> startonce
	/// >! nolinebreak
	/// >if skill_reasoning = 50 ? reasoning
	/// </summary>
	public static StorySet ParseJump(Story story, string line, StoryChoice storyChoice) {
		StorySet jump = new StorySet(story, line, StorySetType.jump);
		
		// a page break was added before this was called unless it started with >! >> or >>>

		if (line.StartsWith(">>")) {
			// >> ignores the text after the jump - only show the choices
			// > shows the text after the jump
			jump.boolValue = true;
		}

		// >> stringid becomes stringid
		string stringID = line.Trim('>').Trim('-').Trim('!').Trim();

		// jumpifs are buggy and not in use 
		// >>if mem_date_tammy ? label3
		// > if skill_rebellion >= 50 ? label1 : label2
		// add a requirement (like a setif) and possibly an else jump too
		if (line.Trim('>').Trim().StartsWith("if ")) {
			string[] parts = line.Trim('>').Trim().RemoveStart("if ").SplitSafe("?", ":");
			if (parts.Length < 2 || parts.Length > 3) {
				Debug.LogWarning("Invalid story jumpif parts (" + parts.Length + "), " + line + ", " + story);
				return null;
			}
			
			string reqLine = "~if " + parts[0].Trim();
			StoryReq req = StoryParserReq.ParseReq(reqLine, story);
			if (req == null) {
				return null;
			}
			jump.requirement = req;
			
			stringID = parts[1].Trim();
			
			if (parts.Length == 3) {
				string elseLine = (jump.boolValue ? ">> " : "> ") + parts[2].Trim();
				StorySet elseJump = ParseJump(story, elseLine, storyChoice);
				if (elseJump == null) {
					return null;
				}
				jump.elseSet = elseJump;
			}
		}
		
		if (line.StartsWith(">>>")) {
			// >>> is shortcut for ">>! backonce" and ignores text
			// which fully becomes:
			// > [parent-choiceID]
			// ~if var_once[randomid] = false
			// ~set var_once[randomid] = true
			stringID = "backonce";
		}

		if (stringID == "back" && storyChoice.parent == null) {
			stringID = "start";
		} else if (jump.stringID == "backonce" && storyChoice.parent == null) {
			stringID = "startonce";
		}

		bool once = false;
		if (stringID == "startonce") {
			once = true;
			stringID = "start";
		} else if (stringID == "backonce") {
			once = true;
			stringID = "back";
		}

		// start is entry choice of story
		if (stringID == "start") {
			if (string.IsNullOrEmpty(storyChoice.story.entryChoice.choiceID)) {
				// doesn't matter if these are different every time the game starts, just want them unique within story
				//storyChoice.story.EntryStoryChoice.choiceID = NWUtils.TrulyRandomInt().ToString();
				storyChoice.story.entryChoice.choiceID = story.GetUniqueChoiceId();
			}
			stringID = storyChoice.story.entryChoice.choiceID;
			
		} else if (stringID == "back") {
			// find the last parent where you made a choice
			StoryChoice backStoryChoice = storyChoice.parent;
			for (int i = 0; i < 100; i++) { // avoid loops
				if (backStoryChoice == null) break; // error
				if (backStoryChoice.choices.Count > 1) break; // multiple choices
				if (backStoryChoice.choices.Count > 0 && !backStoryChoice.choices[0].isContinue) break;
				backStoryChoice = backStoryChoice.parent;
			}
			if (backStoryChoice == null) {
				Debug.LogWarning("ParseStorySet.ParseJump failed to find non-continue parent");
				return null;
			}
			if (string.IsNullOrEmpty(backStoryChoice.choiceID)) {
				// doesn't matter if these are different every time the game starts, just want them unique within story
				// backStoryChoice.choiceID = NWUtils.TrulyRandomInt().ToString();
				backStoryChoice.choiceID = story.GetUniqueChoiceId();
			}
			stringID = backStoryChoice.choiceID;
		
		// clone choices from a globally-used snippet, add it as a hidden option to current story root
		} else if (stringID.StartsWith(Story.snippetPrefix)) {
			string snippetID = stringID.RemoveStart(Story.snippetPrefix).ToLower();
			if (!Story.snippetsByID.ContainsKey(snippetID)) {
				// snippets must be listed ABOVE where they are referenced
				Debug.LogWarning("Invalid story jump snippet ID - is it above all references? " + line);
				return null;
			}

			if (!story.HasChoiceById(stringID)) {
				Story snippetStory = Story.snippetsByID[snippetID];
				// snippet will be appended to the story with ID "snippet_snippetID"
				StoryChoice snippetStoryChoice = snippetStory.entryChoice.CloneChoice(story);
				// snippets need NULL button text, it's a hidden option at the top level
				snippetStoryChoice.buttonText = null;
				snippetStoryChoice.ReplaceChoiceID(snippetStory.entryChoice.choiceID, stringID);
				story.entryChoice.AddChoice(snippetStoryChoice);
				story.choicesByID[stringID] = snippetStoryChoice;
			}
		}

		jump.stringID = stringID;

		// use a variable to make this choice only appear once
		if (once) {
			AddOnce(storyChoice, false);
		}

		return jump;
	}

	/// <summary>
	/// both "~if once" and ">>>" prevent the given choice from being chosen again.
	/// If choice is under a pagebreak put the once-vars higher up where the user made a decision.
	/// Returns the req
	/// </summary>
	public static void AddOnce(StoryChoice storyChoice, bool onceToday, bool onceEver = false) {
		StoryChoice topStoryChoice = storyChoice.GetLastNonContinue();
		
		// must generate the same ID next time the game starts so base it on the storyID and choiceID
		int choiceIndex = storyChoice.story.allChoices.IndexOf(storyChoice);
		if (choiceIndex == -1) {
			Debug.LogWarning("ParserStorySet.AddOnce failed to find choice in Story.allChoices " + storyChoice.story.allChoices.Count);
			if (storyChoice.parent == null) {
				Debug.LogWarning("ParserStorySet.AddOnce found choice with no parent " + storyChoice);
			} else {
				choiceIndex = storyChoice.parent.choices.IndexOf(storyChoice);
				if (choiceIndex == -1) {
					Debug.LogWarning("ParserStorySet.AddOnce failed to find choice in parent.choices " + storyChoice);
				}
			}
		}
		
		string seed = "once" + storyChoice.story.storyID + "-" + choiceIndex;
		string varID = "once" + NWUtils.RandomRangeInt(0, 9999999, seed);

		if (onceEver) {
			// ~if !story_oncerandomid12345
			// ~set story_oncerandomid12345
			// custom stories stick around for the rest of the game
			StoryReq onceReq = StoryParserReq.ParseReq("~if !story_" + varID, storyChoice.story);
			topStoryChoice.AddRequirement(onceReq);
			StorySet onceSet = ParseSet("~set story_" + varID, storyChoice.story, topStoryChoice);
			topStoryChoice.AddSet(onceSet);
			
		} else if (onceToday) {
			// ~if story_oncerandomid12345 != 0
			// ~set story_oncerandomid12345
			// never happened (-1) or happened last month (1) or earlier (2+)
			// custom stories stick around for the rest of the game
			StoryReq onceReq = StoryParserReq.ParseReq("~if story_" + varID + " != 0", storyChoice.story);
			topStoryChoice.AddRequirement(onceReq);
			StorySet onceSet = ParseSet("~set story_" + varID, storyChoice.story, topStoryChoice);
			topStoryChoice.AddSet(onceSet);
			
		} else {
			// ~if !var_oncerandomid12345
			// ~set var_oncerandomid12345
			// vars are cleared after event ends
			StoryReq onceReq = StoryParserReq.ParseReq("~if !var_" + varID, storyChoice.story);
			topStoryChoice.AddRequirement(onceReq);
			StorySet onceSet = ParseSet("~set var_" + varID, storyChoice.story, topStoryChoice);
			topStoryChoice.AddSet(onceSet);
		}
	}
}
