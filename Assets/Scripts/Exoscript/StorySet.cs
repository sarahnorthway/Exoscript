using System;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public enum StorySetType {
	none = 0,
	skill = 1,
	love = 2,
	memory = 3, 
	storyvar = 4,
	groundhog = 5,
	jump = 6,
	// 7,
	call = 8,
	background = 9,
	charaImage = 10,
	speaker = 11,
	billboardSprite = 12,
	card = 13,
	story = 14,
	// 15,
	nextStory = 16,
	effect = 17,
	status = 18,
}

public enum StorySetCompare {
	none = 0,
	equal = 1, // may be int (age, skill) or string (memory, storyvar, groundhog) value
	increment = 2, // always int value
}

public enum CharaImageLocation {
	// corresponds to array index of ResultsManager.charaImages; stored in intValue
	none = 0,
	left = 1,
	midleft = 2,
	midright = 3,
	right = 4,
}

public enum EffectType {
	// don't mess with these numbers, needed for Background Camera BackgroundEffects list
	
	none = 0,
	screenshake = 1,
	dream = 2, // bloomy dream sequence for groundhogs
	fadesexy = 3, // heart shaped fade to black
	holopalm = 4, // old photos on a holopalm
	silence = 5, // stop music for dramatic events (in case music is happy)
	dim = 6, // eg a dim room, dark and less saturated
	pollen = 7, // clouds of pollen lower part of screen
	fadetoblack = 8, // like fadesexy but with no heart, used to transition between backgrounds
	night = 9, // dark blue at edges and bottom, better than dim for outdoors or with people
	fire = 10, // flickering
	siren = 11, // sound for attacks
	festival = 12, // sound for vertumnalia
	heathaze = 13, // for empty scenes only
	screenshakefire = 14, // for empty scenes only
	emergency = 15, // red at top, dark at bottom
	wormhole = 16, // wacktastic version of dream for intro
	screenshakewormhole = 17, // intense!
	solold = 18, // chiller dream for intro
	screenshakeflashdim = 19, // lights go out!
	inkbleed_white = 20, // for endings, requires a renderTexture set to the correct threshold map first
	inkbleedprep_white = 21, // high contrast blurry for inkbleed RenderTexture, used by EndingMenu
	inkbleed_black = 22, // instead of ink on a page it's an inverted swirly kind of effect
	inkbleedprep_black = 23, // still uses a RenderTexture but combined with a swirly texture
	slowtransition = 24, // change backgrounds more slowly than usual, like fadetoblack with no black
	screenshakedream = 25, // ends back on dream
	ending = 26, // for chara scenes during EndingMenu
	screenshakeemergency = 27, // for intro
	screenshakenight = 28, // for walls_night
}

public class StorySet {
	public StorySetType type;
	public StorySetCompare compare = StorySetCompare.equal;
	public Story story;
	public StoryChoice StoryChoice; // backlink from Choice.AddSet
	public readonly string debugString;

	public string stringID;
	public int intValue;
	public bool boolValue;
	public string stringValue;
	// public CardData cardData;
	public StoryCall call;

	// for setif; must return true or this set will not run
	public StoryReq requirement;
	public StorySet elseSet;

	public bool isJump => type == StorySetType.jump;
	public bool isCall => type == StorySetType.call;

	public StorySet(Story story, string debugString = null, StorySetType type = StorySetType.none) {
		this.story = story;
		this.debugString = debugString;
		this.type = type;
	}

	public StorySet(Story story, StorySetTemplate template) {
		this.story = story;

		type = template.type;
		compare = template.compare;
		debugString = template.debugString;
		stringID = template.stringID;
		intValue = template.intValue;
		boolValue = template.boolValue;
		stringValue = template.stringValue;

		if (template.call != null) {
			call = StoryCall.FromTemplate(template.call);
			if (call != null) {
				call.set = this;
			}
		}

		if (template.requirement != null) {
			requirement = new StoryReq(story, template.requirement);
		}

		if (template.elseSet != null) {
			elseSet = new StorySet(story, template.elseSet);
		}
	}

	public StorySetTemplate ToTemplate() {
		return new StorySetTemplate {
			type = type,
			compare = compare,
			debugString = debugString,
			stringID = stringID,
			intValue = intValue,
			boolValue = boolValue,
			stringValue = stringValue,
			call = call?.ToTemplate(),
			requirement = requirement?.ToTemplate(),
			elseSet = elseSet?.ToTemplate()
		};
	}

	public void Execute(Result result = null) {
		try {
			ExecuteInner(result);
		} catch (Exception e) {
			Debug.LogError("Failed to execute StorySetType, " + this + ", " + e);
		}
	}

	private void ExecuteInner(Result result = null) {
		if (requirement != null) {
			if (!requirement.Execute(result, true)) {
				elseSet?.Execute(result);
				// skip this setif because the requirement returned false
				return;
			}
		}

		// ~set hog_permanent = mem_temporary
		// ~set left = var_calculatedchara
		string stringValue = this.stringValue;
		if (stringValue != null) {
			if (stringValue.StartsWith("mem_")) {
				stringValue = StoryManager.GetMemory(stringValue.RemoveStart("mem_"));
			} else if (stringValue.StartsWith("hog_")) {
				stringValue = StoryManager.GetGroundhog(stringValue.RemoveStart("hog_"));
			} else if (stringValue.StartsWith("var_")) {
				if (result != null && result.story != null) {
					stringValue = result.story.vars.Get(stringValue.RemoveStart("var_"));
				}
			}
		}
		
		// resolve dynamic values for "~set var_bff = call_mostLove()" or "~set left = call_mostLove()"
		if (type != StorySetType.call && call != null) {
			stringValue = call.Execute().ToString();
			//stringID = stringValue; // stringID would be bff in first case so no, only set stringValue
			//intValue = stringValue.ParseInt(); // only override if stringValue is "2" not "marz2"
			//boolValue = stringValue.ParseBool(); // only accept "true" or "false"
			if (stringValue.TryParseInt(out int intOverrideValue)) intValue = intOverrideValue;
			if (stringValue.TryParseBool(out bool boolOverrideValue)) boolValue = boolOverrideValue;
		}

		switch (type) {
			case StorySetType.skill:
				// if (compare == StorySetCompare.equal) {
				// 	// skill can only be set to integers
				// 	Princess.SetSkill(stringID, intValue, result);
				// } else {
				// 	Princess.IncrementSkill(Skill.FromID(stringID), intValue, result, true);
				// }
				break;

			case StorySetType.love:
				// Chara chara = Chara.FromID(stringID);
				// if (chara == null || !chara.canLove) break;
				// if (chara.isDead || !chara.onMap) break;
				// // for debugging show hearts for dead charas eg tammy
				// if (!Settings.debugAllChoices && !chara.onMap) break;
				//
				// if (compare == StorySetCompare.equal) {
				// 	// love can be set for sym or people hating you
				// 	Princess.SetLove(stringID, intValue, result);
				// } else {
				// 	//try out doubleing the amount of love from events
				// 	Princess.IncrementLove(stringID, intValue * 2, result);
				// }
				//
				// // record that the chara was spoken to during this story
				// result?.charas.AddSafe(chara);
				break;
			
			case StorySetType.memory:
				if (compare == StorySetCompare.equal) {
					// mem could be set to "sportsball" or "5" or "true"
					StoryManager.SetMemory(stringID, stringValue);
				} else {
					StoryManager.IncrementMemory(stringID, intValue);
				}
				
				break;

			case StorySetType.storyvar:
				if (compare == StorySetCompare.equal) {
					story.vars.Set(stringID, stringValue);
				} else {
					story.vars.Increment(stringID, intValue);
				}
				break;

			case StorySetType.groundhog:
				if (compare == StorySetCompare.equal) {
					StoryManager.SetGroundhog(stringID, stringValue);
				} else {
					StoryManager.IncrementGroundhog(stringID, intValue);
				}
				break;

			case StorySetType.background:
				// if (BackgroundMenu.instance.logDebugInfo) Debug.Log("StorySet background: " + stringValue + " for " + StoryChoice);
				if (result != null) {
					if (!result.bgImage.IsNullOrEmptyOrWhitespace()) {
						// clear characters when the background changes (or is set again to the same thing)
						result.ClearCharaImages();
						// ~set effect = none
						// if (!BackgroundEffects.fadingToBlack && !BackgroundEffects.fadedToBlack) {
						// 	// don't clear black transition yet, BackgroundMenu.Update will do this when it swaps bgs
						// 	BackgroundEffects.instance.HideEffects();
						// }
					}
					result.bgImageForcedToSame = !result.bgImage.IsNullOrEmptyOrWhitespace();
					result.bgImage = stringValue;
				}
				break;

			case StorySetType.charaImage:
				// if (result != null) {
				// 	// intValue is 1-indexed from CharaImagePosition; 0 means NONE / clear all
				// 	CharaImageLocation position = intValue.ParseEnum<CharaImageLocation>();
				// 	if (position == CharaImageLocation.none) {
				// 		// ~set charas = none
				// 		result.ClearCharaImages();
				// 	} else if (stringValue?.StartsWith("mem_") ?? false) {
				// 		string charaImageID = StoryManager.GetMemory(stringValue);
				// 		if (!charaImageID.IsNullOrEmpty()) result.SetCharaImage(position, charaImageID);
				// 	} else {
				// 		// ~set left = tammy
				// 		// ~set midright = tammy_sad
				// 		// ~set right = hopeye
				// 		result.SetCharaImage(position, stringValue);
				// 	}
				// }
				break;
			
			case StorySetType.speaker:
				result?.SetSpeaker(stringValue);
				break;

			case StorySetType.billboardSprite:
				// do nothing, billboard area sprites are set before story starts
				break;

			case StorySetType.jump:
				Debug.LogError("Can't execute jump StorySet " + this);
				break;

			//case StorySetType.jumpSkip:
			//	Debug.LogError("Can't execute jumpSkip StorySet " + this);
			//	break;

			case StorySetType.call:
				call?.Execute();
				break;

			case StorySetType.card:
			// 	if (intValue == 1) {
			// 		// player can only have one of this card or its upgrades
			// 		if (PrincessCards.HasCard(cardData.cardID, 1, true)) {
			// 			if (GameSettings.debugAllTextChunks || GameSettings.debugAllChoices) {
			// 				// for debugging show card even though you didn't get another one
			// 				result?.AddCard(cardData);
			// 			}
			// 			break;
			// 		}
			// 	}
			// 	if (boolValue) {
			// 		// fallback to adding a new card if the upgrade fails
			// 		bool success = PrincessCards.UpgradeToCard(cardData, true, result);
			// 		if (success) break;
			// 	}
			// 	
			// 	// if card is to be hidden (during intro) don't add it to results
			// 	PrincessCards.AddCard(cardData, stringValue == "hidden" ? null : result);
				break;

			case StorySetType.story:
				// stringID may be a dummy with no actual Story associated with it
				// either way it's been validated already
				StoryManager.SetStory(stringID, boolValue);
				break;

			//case StorySetType.title:
			//	if (result != null) {
			//		result.title = stringValue;
			//	}
			//	break;

			case StorySetType.nextStory:
				Debug.LogError("Next Story disabled! " + this);
				// // the first time this is run, will enable the mapSpot for the next story
				// Story nextStory = Story.FromID(stringID);
				// if (nextStory != null) {
				// 	BillboardManager.ShowNextStory(nextStory);
				// }
				break;

			case StorySetType.effect:
				EffectType effectType = stringID.ParseEnum<EffectType>();
				// EffectType.none means hide all effets
				// BackgroundEffects.instance.SetEffect(effectType);
				break;

			case StorySetType.status:
			// 	Status status = Status.FromID(stringID);
			// 	if (boolValue) {
			// 		Princess.RemoveStatus(status, true);
			// 	} else {
			// 		Princess.AddStatus(status, true);
			// 	}
				break;

			default:
				Debug.LogError("Can't execute unknown StorySetType " + this);
				break;
		}
	}

	/// <summary>
	/// Called via Story.Validate after all stories have been parsed.
	/// </summary>
	public bool ValidateAndFinish() {
		requirement?.ValidateAndFinish();
		elseSet?.ValidateAndFinish();

		if (isJump) {
			StoryChoice jumpStoryChoice = story.GetChoiceById(stringID, false);
			if (jumpStoryChoice == null) {
				Debug.LogWarning("StorySet jump to nowhere (" + stringID + "), " + story.storyID + ", " + StoryChoice.path);
				return false;
			}

			if (StoryChoice.story.storyID == "test" && StoryChoice.resultText.IsNullOrEmpty() && jumpStoryChoice.resultText.IsNullOrEmpty() && StoryChoice.isContinue) {
				// we couldn't look ahead before and can't retroactively remove unecessary page breaks, so just warn
				Debug.LogWarning("StorySet jump from no text (" + StoryChoice.buttonText + ") TO no text (" + stringID + "), " + story.storyID + ", " + StoryChoice.path);
			}

		} else if (call != null) {
			// can't also validate stringID because it will be generated dynamically
			return call.Validate();
			
		// } else if (type == StorySetType.card) {
		// 	cardData = CardData.FromID(stringID);
		// 	if (cardData == null) {
		// 		Debug.LogWarning("StorySet Card not found " + stringID + ", " + debugString);
		// 		return false;
		// 	}
			
		} else if (type == StorySetType.nextStory) {
			Story nextStory = Story.FromID(stringID);
			if (nextStory == null) {
				Debug.LogWarning("StorySet NextStory not found " + stringID + ", " + debugString);
				return false;
			}
			
		} else if (type == StorySetType.charaImage) {
			// charaImage validation done earlier in parser

		} else if (type == StorySetType.billboardSprite) {
			// sprite image validation handled earlier in Story.FinishLoading()
			if (StoryChoice?.story?.entryChoice != StoryChoice) {
				Debug.LogWarning("StorySet Sprite not in entry choice: " + debugString + ", " + story);
				return false;
			}

		} else if (type == StorySetType.background) {
			// background image validation done earlier in parser
		}
		
		return true;
	}

	public bool IsCall(string callName = null) {
		if (type != StorySetType.call) return false;
		if (call == null) return false;
		if (callName == null) return true;
		return callName.ToLower().Trim() == call.methodName;
	}

	// /// <summary>
	// /// Hacky way to get a battle from a call without executing the call.
	// /// Used to display battle info in choice buttons.
	// /// </summary>
	// public string GetBattleID() {
	// 	if (!IsCall("battle") || call.parameterArray.Length == 0 || !(call.parameterArray[0] is string)) return null;
	// 	string battleID = call.parameterArray[0] as string;
	// 	return battleID;
	// }

	// /// <summary>
	// /// Hacky way to get a battle from a call without executing the call.
	// /// Used to display battle info in choice buttons.
	// /// </summary>
	// public Battle CreateBattle() {
	// 	if (!IsCall("battle") || call.parameterArray.Length == 0 || !(call.parameterArray[0] is string)) return null;
	// 	string battleID = call.parameterArray[0] as string;
	// 	if (call.parameterArray.Length >= 3) {
	// 		string winChoiceID = call.parameterArray[1] as string;
	// 		string loseChoiceID = call.parameterArray[2] as string;
	// 		return new Battle(battleID, winChoiceID, loseChoiceID);
	// 	} else {
	// 		return new Battle(battleID, Battle.defaultWinChoiceID, Battle.defaultLoseChoiceID);
	// 	}
	// }
	
	// /// <summary>
	// /// Can't get difficulty from the battle because a Hard battle at age 15 == Normal battle at age 17,
	// /// and they'll be clamped by max princess age, so Impossible at age 19 == Normal at age 19.
	// /// </summary>
	// public BattleDifficulty GetBattleDifficulty() {
	// 	if (!IsCall("battle") || call.parameterArray.Length == 0 || !(call.parameterArray[0] is string)) return BattleDifficulty.normal;
	// 	string battleID = call.parameterArray[0] as string;
	// 	
	// 	string[] parts = battleID.Split('_');
	// 	if (parts.Length < 2) {
	// 		return BattleDifficulty.normal;
	// 	}
	//
	// 	BattleDifficulty difficulty = parts[1].ParseEnum<BattleDifficulty>();
	// 	if (difficulty == BattleDifficulty.none) return BattleDifficulty.normal;
	// 	return difficulty;
	// }
	
	/// <summary>
	/// Used for snippets which appear in multiple stories.
	/// Copy this set but change the story pointer.
	/// </summary>
	public StorySet CloneSet(Story story) {
		// most values can be MemberwiseCloned and some can be pointers eg StoryCall
		StorySet set = (StorySet)MemberwiseClone();
		set.story = story;

		// sub req or set must be deep cloned
		if (requirement != null) {
			set.requirement = requirement.CloneReq(story);
		}
		if (elseSet != null) {
			set.elseSet = elseSet.CloneSet(story);
		}

		return set;
	}

	public override string ToString() {
		return string.IsNullOrEmpty(debugString) ? type.ToString() : debugString;
	}
}
