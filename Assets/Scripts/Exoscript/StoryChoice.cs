using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Stories are made up of choices and their resulting text.
/// The initial entry text of a story is also a choice with requirements and resultText, but no choiceText.
/// </summary>
public class StoryChoice {
	// this label is optional and only required when using StorySetType.jump to move to it after another choice
	public string choiceID;

	// only used during DataLoad; level 0 is entryChoice of a story; level is number of stars (**) before the choiceText
	public int level = 0;

	public StoryChoice parent = null;

	// raw text - displayed text produced via GetButtonText() and GetProcessedResultText()
	public string buttonText = null;
	private string _resultText = null;
	public string resultText => _resultText;
	public void SetResultText(string value) {
		_resultText = value;
	}

	public Story story;

	public List<StoryReq> requirements = new List<StoryReq>();
	public List<StorySet> sets = new List<StorySet>();
	public List<StoryChoice> choices = new List<StoryChoice>();

	public StoryChoice(Story story, string buttonText = null, string resultText = null) {
		this.story = story;
		// set temporarily to provide an ID for this choice, but will be cleared later
		story?.allChoices.Add(this);
		this.buttonText = buttonText;
		// this.resultText = resultText;
		SetResultText(resultText);
	}

	public StoryChoice(Story story, StoryChoiceTemplate template) {
		this.story = story;

		choiceID = template.choiceID;
		level = template.level;
		buttonText = template.buttonText;
		SetResultText(template.resultText);

		foreach (var reqTemplate in template.requirements) {
			AddRequirement(new StoryReq(story, reqTemplate));
		}

		foreach (var setTemplate in template.sets) {
			AddSet(new StorySet(story, setTemplate));
		}

		foreach (var choiceTemplate in template.choices) {
			var choice = new StoryChoice(story, choiceTemplate) {
				parent = this
			};
			AddChoice(choice);
		}
	}

	public StoryChoiceTemplate ToTemplate() {
		return new StoryChoiceTemplate {
			choiceID = choiceID,
			level = level,
			buttonText = buttonText,
			resultText = resultText,
			requirements = requirements.Select(r => r.ToTemplate()).ToList(),
			sets = sets.Select(s => s.ToTemplate()).ToList(),
			choices = choices.Select(c => c.ToTemplate()).ToList()
		};
	}

	/// <summary>
	/// If a choice has no text and no subchoices, then it should close the resultsmenu.
	/// Some sets are OK (just not battle or job) and will be executed during ResultsMenu.DoneClicked
	/// Having a label marked =end means every deadend choice will have one set (a jump to >end)
	/// </summary>
	public bool isDone => resultText.IsNullOrEmpty() && isEnd;
	// public bool isDone => resultText.IsNullOrEmpty() && sets.Count == 0 && choices.Count == 0;

	/// <summary>
	/// No choices under this, and nothing dramatic will happen to keep the story going.
	/// =end choices can be appended onto these dead-ends.
	/// </summary>
	public bool isEnd => choices.Count == 0
		&& !hasBattle && !hasJump && !hasStory && !hasIncrementMonth && !hasUserInput;

	/// <summary>
	/// Only let B button or ESC close the ResultsMenu:
	/// - in repeating chara events
	/// - in location events
	/// - in explore events that don't end with GoHome or EndGame
	/// These buttons still call DoneClicked, but can't be triggered via hotkeys.
	/// </summary>
	public bool isCancellableDone => isDone;
		// && (story.isExplore || story.isCharaLow || story.isLocation || HasCall("forceDoneCancel")) 
		// && !hasGoHome && !hasEndGame && !HasCall("preventDoneCancel");
	
	/// <summary>
	/// Just a page break with "...".
	/// </summary>
	public bool isContinue {
		get {
			if (hasBattle) return false;
			if (story.entryChoice == this) return false;
			return IsContinueText(buttonText);
		}
	}

	public static bool IsContinueText(string text) {
		if (GameSettings.debugAllChoices) {
			// eg "... (DS)"
			return text.StartsWith("...");
		}
		return text == "...";
	}
	
	// don't do the slow pinup reveal if we just did it last page, or during dream sequence flashbacks in intro
	// public bool isPinupReveal => isPinup && (parent == null || !parent.isPinup) && !BackgroundEffects.isDream;
	
	public bool isPinup {
		get {
			// this will be screwed up by jumps including *= end so don't pinup directly before a jump
			foreach (StorySet set in sets) {
				if (set.type == StorySetType.background && set.stringValue.StartsWith("pinup_")) return true;
			}
			return false;
		}
	}

	public bool hasCall {
		get {
			foreach (StorySet set in sets) {
				if (set.isCall) return true;
			}
			return false;
		}
	}
	
	public bool hasStory => HasCall("story");

	public bool hasGoHome => HasCall("goHome");
	public bool hasEndGame => HasCall("endGame");
	public bool hasPreventSeenSkip => HasCall("preventSeenSkip");

	/// <summary>
	/// Return true only if there's a battle in THIS set, ignoring children.
	/// NWButtonResults only looks at current choice, not children.
	/// </summary>
	public bool hasBattle => HasCall("battle");

	public bool hasUserInput => HasCall("setname") || HasCall("setinputmem");

	/// <summary>
	/// Like battle, this call will open another menu (MonthMenu) but keep the bg and result around to return to later
	/// </summary>
	public bool hasIncrementMonth => HasCall("incrementMonth");
	
	/// <summary>
	/// Not used. Only current choice battle is ever looked at.
	/// </summary>
	public bool hasBattleRecursive => GetBattleSet(true) != null;

	// /// <summary>
	// /// Return battle if there's a battle in THIS set, ignoring children.
	// /// </summary>
	// public Battle battle {
	// 	get {
	// 		StorySet battleSet = GetBattleSet();
	// 		return battleSet == null ? null : battleSet.CreateBattle();
	// 	}
	// }

	public bool hasJump => GetJumps().Count > 0;

	/// <summary>
	/// Charas come in faster if there's screenshake.
	/// </summary>
	public bool hasScreenshake {
		get {
			// foreach (StorySet set in sets) {
			// 	if (set.type != StorySetType.effect) continue;
			// 	return BackgroundEffects.IsScreenshake(set.stringID.ParseEnum<EffectType>());
			// }
			return false;
		}
	}
	
	public bool isDream {
		get {
			foreach (StorySet set in sets) {
				if (set.type != StorySetType.effect) continue;
				return set.stringID.ParseEnum<EffectType>() == EffectType.dream;
			}
			return false;
		}
	}

	public bool isGroundhog {
		get {
			foreach (StoryReq req in requirements) {
				if (req.type != StoryReqType.groundhog) continue;
				if (req.compare == StoryReqCompare.equal && req.stringValue == "true") return true;
				if (req.compare == StoryReqCompare.greaterThan) return true;
			}
			return false;
		}
	}

	public bool HasCall(string callName) {
		return GetCall(callName) != null;
	}

	private StorySet GetCall(string callName) {
		callName = callName.ToLower();
		foreach (StorySet set in sets) {
			if (set.IsCall(callName)) return set;
		}
		return null;
	}
	
	/// <summary>
	/// Check this choice and possibly its children for a battle call.
	/// checkPageChildren is never used, currently.
	/// </summary>
	private StorySet GetBattleSet(bool checkPageChildren = false) {
		foreach (StorySet set in sets) {
			if (set.IsCall("battle")) {
				return set;
			}
		}

		// recurse through child continue choice
		if (checkPageChildren) {
			StoryChoice pageChild = GetPageChild();
			if (pageChild != null) {
				return pageChild.GetBattleSet(true);
			}
		}

		return null;
	}

	/// <summary>
	/// For debugging show choice path.
	/// </summary>
	public string path {
		get {
			string result = buttonText;
			StoryChoice parent = this.parent;
			// prevent loops
			for (int i = 0; i < 100; i++) {
				if (parent == null) break;
				result = parent.buttonText + " > " + result;
				parent = parent.parent;
			}
			return result;
		}
	}

	/// <summary>
	/// Will this choice be visible?
	/// </summary>
	public bool CanShow(Result result, bool ignoreDebugOverride = false) {
		// missing buttonText means it can't be shown as a button, can only be jumped to
		if (string.IsNullOrEmpty(buttonText)) return false;

		if (!ignoreDebugOverride && GameSettings.debugAllChoices) {
			return true;
		}

		foreach (StoryReq req in requirements) {
			// only look at ones that would hide the choice
			if (req.showDisabled) continue;
			if (!req.Execute(result)) {
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Assuming the choice is visible (CanShow), is it also enabled?
	/// </summary>
	public bool CanExecuteShown(Result result, bool ignoreDebugOverride = false) {
		if (!ignoreDebugOverride && GameSettings.debugAllChoices) {
			return true;
		}

		foreach (StoryReq req in requirements) {
			// only look at ones that would show but disable the choice
			if (!req.showDisabled) continue;
			if (!req.Execute(result)) {
				return false;
			}
		}

		return true;
	}

	public List<StorySet> GetJumps() {
		// you could have more than one jump if there are multiple jumpifs with different requirements
		// which is not currently implemented...
		return sets.Where(set => set.isJump).ToList();
	}

	/// <summary>
	/// Eg for mem_food or mem_defense.
	/// Return 0 if there is no memory change.
	/// </summary>
	public int GetMemoryIncrement(string memID, bool includeNegative = false) {
		memID = memID.ToLower().Trim();
		foreach (StorySet set in sets) {
			if (set.type != StorySetType.memory) continue;
			if (set.stringID != memID) continue;
			if (set.compare == StorySetCompare.equal) {
				// setting memory equal to a thing rather than increment / decrement, ignore
				continue;
			}
			if (!includeNegative && set.intValue <= 0) continue;
			return set.intValue;
		}
		return 0;
	}

	/// <summary>
	/// Return true if this has no options except line breaks until the end.
	/// Was used for =end but whatever can allow stuff in there
	/// </summary>
	public bool IsDeadEnd() {
		if (!choices.Any()) return true;
		if (choices.Count > 1) return false;
		// page breaks don't have requirements
		if (choices[0].requirements.Any()) return false;
		// could use sets to change charas etc but no jumps!
		if (choices[0].GetJumps().Any()) return false;
		return choices[0].IsDeadEnd();
	}
	
	/// <summary>
	/// If a page break follows this choice, return the next one.
	/// Include any single result with no requirements, regardless of its button text.
	/// </summary>
	public StoryChoice GetPageChild(bool onlyContinueText = false) {
		if (choices.Count != 1) return null;
		if (onlyContinueText && !choices[0].isContinue) return null;
		if (!choices[0].requirements.Any()) return choices[0];
		return null;
	}
	
	// /// <summary>
	// /// Return a list of any card(s) on this option (which are not hidden via hidden_cardID).
	// /// </summary>
	// public List<CardData> GetCards() {
	// 	List<CardData> results = new List<CardData>();
	// 	foreach (StorySet set in sets) {
	// 		if (set.type != StorySetType.card || set.cardData == null) continue;
	// 		if (set.stringValue == "hidden") continue;
	// 		results.Add(set.cardData);
	// 	}
	// 	return results;
	// }

	// /// <summary>
	// /// If there are more than one card on this choice, only return the first one.
	// /// Does not include hidden_ cards.
	// /// </summary>
	// public CardData GetCard() {
	// 	List<CardData> cards = GetCards();
	// 	return cards.Count == 0 ? null : cards[0];
	// }
	
	public string GetButtonText(Result result = null) {
		if (string.IsNullOrEmpty(buttonText)) return "";

		// quotes are voiced by sol, and underlines italicized
		string text = TextFilter.FilterResultText(buttonText, null, story, true);

		if (GameSettings.debug && result != null) {
		
			if (GameSettings.debugAllChoices) {
				GameSettings.debugAllChoices = false;
				bool canReallyShow = CanShow(result);
				bool canReallyExecute = CanExecuteShown(result);
				GameSettings.debugAllChoices = true;
				if (!canReallyShow) {
					text += "(shw)";
				} else if (!canReallyExecute) {
					text += " (exe)";
				}
			}
			
			foreach (StoryReq req in requirements) {
				if (req.IsDebugOnly()) {
					text += " (dbg)";
					break;
				}
		
				if (req.type == StoryReqType.or) {
					bool isDebug = false;
					bool failedOr = false;
					foreach (StoryReq subreq in req.subReqs) {
						if (subreq.IsDebugOnly()) isDebug = true;
						if (!subreq.Execute(result)) failedOr = true;
					}
					if (isDebug && failedOr) {
						text += " (dbg)";
						break;
					}
				}
			}
		}

		return text;
	}

	/// <summary>
	/// Filter the choice resultText, replace [ge|nd|er] and [Name] variables,
	/// also [if mem_statements] and [=mem_printStatements].
	/// </summary>
	public string GetProcessedResultText(Result result) {
		return TextFilter.FilterResultText(resultText, result, story);
	}

	/// <summary>
	/// Determine the next chunk of text, and apply stat changes to the princess.
	/// Does not open the ResultsMenu or show the result.
	/// </summary>
	/// <param name="ignoreJumpAndBattle">For debugDisableBattlesChoose only run vanilla sets</param>
	public void Execute(Result result, StorySet prevJump = null, bool playerSelected = false, bool ignoreJumpAndBattle = false) {
		if (result != null) {
			result.choice = this;
		}

		// remember this was selected not auto-executed or jumped to
		if (playerSelected) {
			story.AddSelectedChoice(this);
		}

		// change skills, memories etc before calculating text
		foreach (StorySet set in sets) {
			if (set.isJump) continue;
			if (ignoreJumpAndBattle && set.IsCall("battle")) continue;
			set.Execute(result);
		}

		// calculate text before jump
		string text = result == null ? "" : result.text;

		// if we aren't jumping from somewhere, clear the result text
		if (prevJump == null) {
			text = "";
		}

		// title stays the same and text is appended
		// unless arriving here from a jumpskip which skips the resultText
		if (prevJump == null || prevJump.boolValue == false) {
			if (string.IsNullOrEmpty(text)) {
				text = GetProcessedResultText(result);
			} else {
				string postJumpText = GetProcessedResultText(result);
				if (!string.IsNullOrEmpty(postJumpText)) {
					text += "\n\n" + postJumpText;
				}
			}
		}
		if (result != null) {
			result.text = text;
		}
		
		// save jumps for last and move result to next choice if one succeeds
		if (!ignoreJumpAndBattle) {
			foreach (StorySet jump in GetJumps()) {
				if (prevJump != null && !jump.stringID.StartsWith(Story.snippetPrefix)) {
					if (jump.stringID == prevJump.stringID || jump.stringID == choiceID) {
						Debug.LogError("Jump to a jump in choice looks recursive! " + this + ", " + jump.stringID);
						return;
					}
				}

				if (jump.requirement != null && !jump.requirement.Execute(result, true)) {
					if (jump.elseSet != null && jump.elseSet.type == StorySetType.jump) {
						// jump to else tag instead
						StoryChoice jumpElseStoryChoice = story.GetChoiceById(jump.elseSet.stringID);
						// will point result to this new choice, append text and add stat changes
						jumpElseStoryChoice?.Execute(result, jump.elseSet);
						// stop after we execute a jump
						return;
					} else {
						// jump req failed, but maybe there is another jump after this one
						continue;
					}
				}
			
				StoryChoice jumpStoryChoice = story.GetChoiceById(jump.stringID);
				// will point result to this new choice, append text and add stat changes
				jumpStoryChoice?.Execute(result, jump);
				// stop after we execute a jump
				return;
			}
		}
	}

	/// <summary>
	/// Used during creating in ParserData.
	/// </summary>
	public void AddRequirement(StoryReq req) {
		if (req == null) return;
		requirements.Add(req);
	}

	/// <summary>
	/// Used when splitting choices by page during creation.
	/// </summary>
	public void RemoveRequirement(StoryReq req) {
		requirements.Remove(req);
	}

	/// <summary>
	/// Used during creating in ParserData.
	/// </summary>
	public void AddSet(StorySet set) {
		if (set == null) return;
		sets.Add(set);
		set.StoryChoice = this;
	}

	/// <summary>
	/// Used when splitting choices by page during creation.
	/// </summary>
	public void RemoveSet(StorySet set) {
		sets.Remove(set);
		set.StoryChoice = (set.StoryChoice == this) ? null : set.StoryChoice;
	}

	/// <summary>
	/// Used during creating in DataLoader.
	/// </summary>
	public void AddChoice(StoryChoice storyChoice) {
		choices.Add(storyChoice);
	}

	/// <summary>
	/// Used when splitting choices by page during creation.
	/// </summary>
	public void RemoveChoice(StoryChoice storyChoice) {
		choices.Remove(storyChoice);	
	}

	/// <summary>
	/// Make a deep clone of this choice and all its subchoices and attach them to the given story.
	/// Used for snippets which may be used in multiple stories.
	/// </summary>
	public StoryChoice CloneChoice(Story story) {
		// level, buttonText, etc all the same
		StoryChoice storyChoice = (StoryChoice)MemberwiseClone();

		// force new story and new choiceID
		storyChoice.story = story;

		// clone reqs and sets to change their story pointers
		storyChoice.requirements = new List<StoryReq>();
		foreach (StoryReq req in requirements) {
			storyChoice.AddRequirement(req.CloneReq(story));
		}
		storyChoice.sets = new List<StorySet>();
		foreach (StorySet set in sets) {
			StorySet clonedSet = set.CloneSet(story);
			storyChoice.AddSet(clonedSet);
		}

		// recurse subchoices and change their story and parent pointers
		storyChoice.choices = new List<StoryChoice>();
		foreach (StoryChoice subchoice in choices) {
			StoryChoice subchoiceClone = subchoice.CloneChoice(story);
			subchoiceClone.parent = storyChoice;
			storyChoice.AddChoice(subchoiceClone);
		}

		return storyChoice;
	}

	/// <summary>
	/// Used after CloneChoice when incorporating a snippet.
	/// Recursive. Only replaces first match, but continues to change all jumps to it.
	/// </summary>
	public void ReplaceChoiceID(string originalID, string newID, bool foundMatch = false) {
		if (!foundMatch && choiceID == originalID) {
			choiceID = newID;
			foundMatch = true;
		}

		foreach (StorySet set in sets) {
			// replace references to the ID
			if (set.isJump && set.stringID == originalID) {
				set.stringID = newID;
			}
		}

		foreach (StoryChoice subchoice in choices) {
			subchoice.ReplaceChoiceID(originalID, newID, foundMatch);
		}
	}

	/// <summary>
	/// Return this if it is not a continue, or recurse up the tree until the last choice which was
	/// not a continue (simple page break).
	/// </summary>
	public StoryChoice GetLastNonContinue() {
		if (!isContinue) return this;
		if (parent == null) return this;
		return parent.GetLastNonContinue();
	}
	
	/// <summary>
	/// Return true if the given choice contains a jump that could end up back at this choice.
	/// Recursively check all children.
	/// </summary>
	public bool IsAboveChoice(StoryChoice storyChoice, StoryChoice originalStoryChoice = null) {
		// loops may be on purpose just don't keep going around in a circle forever
		if (this == originalStoryChoice) return false;
		if (originalStoryChoice == null) originalStoryChoice = this;
		
		// check every choice
		foreach (StoryChoice childChoice in choices) {
			if (childChoice == storyChoice) return true;
			if (childChoice.IsAboveChoice(storyChoice, originalStoryChoice)) return true;
		}

		// check every jump
		foreach (StorySet jump in GetJumps()) {
			StoryChoice jumpStoryChoice = story.GetChoiceById(jump.stringID);
			if (jumpStoryChoice == null) continue;
			if (jumpStoryChoice == storyChoice) return true;
			if (jumpStoryChoice.IsAboveChoice(storyChoice, originalStoryChoice)) return true;
		}

		return false;
	}

	/// <summary>
	/// For testing.
	/// </summary>
	public string ToStringVerbose() {
		string text = "Choice ["
			+ (string.IsNullOrEmpty(choiceID) ? "" : "label:" + choiceID + ", ")
			+ "story: " + story.storyID + ", " 
			+ "button: " + buttonText + ", "
			+ "text: " + resultText + "\n";

		foreach (StoryChoice subchoice in choices) {
			text += "\tSubChoice " + subchoice + "\n";
		}
		
		text += "]";

		return text;
	}

	public override string ToString() {
		return "Choice ["
			+ (string.IsNullOrEmpty(choiceID) ? "" : "choiceID:" + choiceID + ", ")
			+ ", button: " + buttonText.StripLineBreaks()
			+ ", story: " + story.storyID + ", "
			+ ", text: " + resultText.StripLineBreaks()
			+ "]";
	}
}
