using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable IntroduceOptionalParameters.Global

/// <summary>
/// Static methods to be called by stories using reflection.
/// 
/// All method names are lowercase here, but may be camelCase in story text.
/// </summary>
public class StoryCalls {
	public static bool debug() {
		return isdebug();
	}
	public static bool isdebug() {
		return false;
	}

	public static bool isdemo() {
		return false;
	}
	

	/// <summary>
	/// Award the cards to the current result.
	/// And return 0, 1, 2, or more for now many of a given collectible you find.
	/// </summary>
	public static int findcollectibles(string cardID) {
		// CardData data = CardData.FromID(cardID);
		// Collectible collectible = data.collectible;
		// int numFound = collectible.GetNumFound();
		// for (int i = 1; i <= numFound; i++) {
		// 	PrincessCards.AddCard(collectible.cardData, Princess.result);
		// }
		// Princess.AddMemory("foundCollectible");
		// return numFound;
		return 1;
	}
	
	public static bool findcollectiblesValidate(string cardID) {
		// CardData data = CardData.FromID(cardID);
		// if (data == null) {
		// 	Debug.LogWarning("FindCollectible invalid collectible cardID " + cardID);
		// 	return false;
		// }
		// if (data.collectible == null) {
		// 	Debug.LogWarning("FindCollectible cardID is not a collectible " + cardID);
		// 	return false;
		// }
		return true;
	}
	
	

	/// <summary>
	/// Remember that we now know some fact about the given chara,
	/// to be shown on MenuCharas,
	/// and remembered as a groundhog.
	/// </summary>
	public static void charafact(string charaID, string factID) {
		// Chara chara = Chara.FromID(charaID);
		// chara.AddFact(factID);
	}

	public static bool charafactValidate(string charaID, string factID) {
		// Chara chara = Chara.FromID(charaID);
		// if (chara == null) {
		// 	Debug.LogWarning("CharaFact has invalid chara, " + charaID);
		// 	return false;
		// }
		//
		// // likes_egg -> likes, birthday -> birthday
		// string fullFactString = factID;
		// string factRight = null;
		// if (factID.Contains("_")) {
		// 	factRight = factID.Split('_')[1].ToLower();
		// 	factID = factID.Split('_')[0];
		// }
		//
		// CharaFact fact = factID.ParseEnum<CharaFact>();
		// if (fact == CharaFact.none) {
		// 	Debug.LogWarning("CharaFact has invalid fact, " + fullFactString);
		// 	return false;
		// }
		//
		// if (fact == CharaFact.date) {
		// 	Debug.LogWarning("CharaFact can't set date, set mem_date_* or mem_*_date instead, " + fullFactString);
		// 	return false;
		// }
		//
		// if (factRight != null) {
		// 	// some chara facts can have additional info
		// 	if (fact == CharaFact.likes || fact == CharaFact.dislikes) {
		// 		// defaults to everything if no right side provided
		// 		Collectible collectible = Collectible.FromID(factRight);
		// 		if (collectible == null) {
		// 			Debug.LogWarning("CharaFact has invalid fact collectible, " + fullFactString);
		// 			return false;
		// 		}
		// 	}
		// 	// else if (fact == CharaFact.date) {
		// 	// 	// defaults to none if no right side provided
		// 	// 	if (factRight != "player" && factRight != "none" && factRight != "null") {
		// 	// 		Chara dateChara = Chara.FromID(factRight);
		// 	// 		if (dateChara == null) {
		// 	// 			Debug.LogWarning("CharaFact has invalid date chara, " + fullFactString);
		// 	// 			return false;
		// 	// 		}
		// 	// 	}
		// 	// }
		// }
		
		return true;
	}
	
	/*

	/// <summary>
	/// Go find and trigger the animation clip by name in the Base Layer of the current animated background.
	/// </summary>
	public static void animation(string stateName) {
		// wait until end of frame for new background with new Animator to possibly load
		NWUtils.RunLateUpdate(() => animationLateUpdate(stateName));
	}

	private static void animationLateUpdate(string stateName) {
		if (Princess.result == null || !BackgroundMenu.isOpen) return;
		Animator anim = BackgroundMenu.instance.animatedBackgroundContainer.GetComponentInChildren<Animator>();
		if (anim == null) {
			// can happen when skiping into middle or something, whatever?
			// Debug.LogWarning("StoryCalls.animation but no Animator present in BackgroundMenu, " + stateName 
			// 	+ ", currentImageID " + BackgroundMenu.instance.currentImageID);
			return;
		}
		
		// crossfade might be skipping first frame causing nemmie and dog to coexist, try zero transition 
		anim.Play("Base Layer." + stateName, 0, 0f);
		
		// we can't get state names but we can tag them
		// bool isLoop = anim.GetCurrentAnimatorStateInfo(0).IsTag("loop");
		// Debug.Log("StoryCalls.animation calling " + stateName + " from loop? " + isLoop
		// 	+ ", currentImageID " + BackgroundMenu.instance.currentImageID);
		// if (isLoop) {
		// 	// great when moving from intro1fireLoop > intro2nemmie
		// 	anim.CrossFade("Base Layer." + stateName, 0.25f);
		// } else {
		// 	// better for intro2nemmie > (skipping intro2nemmieLoop) > intro3door
		// 	anim.Play("Base Layer." + stateName, 0, 0f);
		// }
	}

	public static void fancymode(bool value) {
		fancymode(value.ToString());
	}

	public static void fancymode(string value) {
		ResultStyle style = ResultStyle.normal;
		if (value.ToLower().Trim() == "true") {
			style = ResultStyle.fancyDark;
		} else if (value.ToLower().Trim() == "false") {
			style = ResultStyle.normal;
		} else {
			style = value.ParseEnum<ResultStyle>();
		}
		// ResultStyle style = value ? ResultStyle.fancyDark : ResultStyle.normal;
		if (Princess.result != null) Princess.result.resultStyle = style;
	}

	public static bool fancymodeValidate(string value) {
		if (value == "normal" || value == "true" || value == "false") return true;
		ResultStyle style = value.ParseEnum<ResultStyle>();
		if (style != ResultStyle.normal) return true;
		Debug.LogWarning("Invalid FancyMode value " + value);
		return false;
	}

	// public static void delaytext() {
	// 	delaytext(1);
	// }

	public static void delaytext(int seconds) {
		if (Princess.result == null) return;
		Princess.result.delayTextSeconds = seconds;
		// Princess.result.delayHideMenus = false;
	}

	// public static void delaytexthidemenus(int seconds) {
	// 	if (Princess.result == null) return;
	// 	Princess.result.delayTextSeconds = seconds;
	// 	Princess.result.delayHideMenus = true;
	// }

	// public static void autoadvance(int seconds) {
	// 	if (Princess.result != null) Princess.result.autoAdvanceSeconds = seconds;
	// }

	public static void cleartext() {
		ResultsMenu.instance.ClearContent();
	}

	public static void setgenderappearance(string value) {
		Princess.genderAppearance = value.ParseEnum<GenderID>();
	}

	public static void setgenderpronouns(string value) {
		// only used for debug quickstart in intro
		Princess.genderPronouns = value.ParseEnum<GenderID>();
	}

	public static string genderappearance() {
		return Princess.genderAppearance.ToString();
	}

	public static string genderpronouns() {
		return Princess.genderPronouns.ToString();
	}

	public static bool hasperk(string perkID) {
		return Perk.HasPerk(perkID.ParseEnum<PerkID>());
	}

	public static bool hasperkValidate(string perkID) {
		PerkID id = perkID.ParseEnum<PerkID>();
		if (id == PerkID.none) {
			Debug.LogWarning("Invalid hasPerk perkID " + perkID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// If they have the perk, and it's a skill perk, how many months since it was granted.
	/// Return -1 for don't have the perk, 0 for got the perk today, 1 for last month, 13 for a year ago
	/// </summary>
	public static int monthssinceperk(string perkID) {
		Perk perk = Perk.FromID(perkID);
		return perk.GetMonthsSinceSkillPerk();
	}

	public static bool monthssinceperkValidate(string perkID) {
		Perk perk = Perk.FromID(perkID);
		if (perk == null) {
			Debug.LogWarning("Invalid monthsSincePerk perkID " + perkID);
			return false;
		}
		if (perk.type != PerkType.skill) {
			Debug.LogWarning("Invalid monthsSincePerk non-skill perkID " + perkID);
			return false;
		}
		return true;
	}

	public static void setname(string choiceID) {
		PopupMenu.ShowInput("What is your name?", null, "Solanaceae", (value) => setname(choiceID, value), null);
		PopupMenu.instance.BlockScreen();
	}

	public static void setname(string choiceID, string value) {
		Princess.princessName = value.TrimSafe().CapitalizeWords();
		
		// default the gender based on the name
		value = value.TrimSafe().ToLower();
		if (value == "solanaceae") {
			Princess.SetBothGendersRandomly(GenderID.nonbinary);
		} else if (value == "solana") {
			Princess.SetBothGendersRandomly(GenderID.female);
		} else if (value == "solane") {
			Princess.SetBothGendersRandomly(GenderID.male);
		}
		PortraitMenu.instance.UpdatePortraitAndSkills();

		if (Princess.result?.story != null && !choiceID.IsNullOrEmpty() && choiceID != "none" && choiceID != "null") {
			Choice jumpChoice = Princess.result.story.GetChoiceById(choiceID);
			if (jumpChoice != null) {
				ResultsMenu.instance.ChoiceClicked(jumpChoice, null, true);
			}
		}
	}

	public static bool setnameValidate(string choiceID) {
		if (StoryCall.validationStory == null) return true;
		if (StoryCall.validationStory.GetChoiceById(choiceID) == null) {
			Debug.LogWarning("Invalid choiceID " + choiceID);
			return false;
		}
		return true;
	}
	
	/// <summary>
	/// Eg for changing Anemone's nickname and other names.
	/// Can't validate choiceID.
	/// </summary>
	public static void setinputmem(string memID, string choiceID) {
		setinputmem(memID, choiceID, "");
	}
	
	public static void setinputmem(string memID, string choiceID, string defaultValue) {
		// TODO localize and is it always a name?
		PopupMenu.ShowInput("What name?", null, defaultValue, (value) => setinputmemDone(memID, choiceID, value), null);
		PopupMenu.instance.BlockScreen();
	}

	public static void setinputmemDone(string memID, string choiceID, string value) {
		if (value.Length < 3) {
			// TODO localize
			PopupMenu.ShowInput("Name too short!", null, value, (newValue) => setinputmemDone(memID, choiceID, newValue), null);
			PopupMenu.instance.BlockScreen();
			return;
		}
		
		Princess.AddMemory(memID.ToLower(), value);

		if (Princess.result?.story != null && !choiceID.IsNullOrEmpty() && choiceID != "none" && choiceID != "null") {
			Choice jumpChoice = Princess.result.story.GetChoiceById(choiceID);
			if (choiceID != null) {
				ResultsMenu.instance.ChoiceClicked(jumpChoice, null, true);
			}
		}
	}
	
	public static bool setinputmemValidate(string memID, string choiceID) {
		return setinputmemValidate(memID, choiceID, "");
	}
	
	public static bool setinputmemValidate(string memID, string choiceID, string defaultValue) {
		// memID must be set elsewhere too, or this will fail AND read references will fail
		memID = memID.RemoveStart("mem_").ToLower();
		if (!Story.allMemories.Contains(memID)) {
			Debug.LogWarning("Invalid memory " + memID);
			return false;
		}
		if (StoryCall.validationStory == null) return true;
		if (StoryCall.validationStory.GetChoiceById(choiceID) == null) {
			Debug.LogWarning("Invalid choiceID " + choiceID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Eg for 7 food return 70%, for 12 defense return 120%.
	/// </summary>
	public static string memaspercent(string memID) {
		return memaspercent(memID, 0, false);
	}
	
	public static string memaspercent(string memID, int adjustment) {
		return memaspercent(memID, adjustment, false);
	}
	
	public static string memaspercent(string memID, int adjustment, bool clamp) {
		int value = Princess.GetMemoryInt(memID);
		value += adjustment;
		if (clamp) value = value.Clamp(0);
		return (value * 10) + "%";
	}
	
	public static bool memaspercentValidate(string memID) {
		memID = memID.RemoveStart("mem_").ToLower();
		if (!Story.allMemories.Contains(memID)) {
			Debug.LogWarning("Invalid memory " + memID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// A list of skill changes in the current story result so far.
	/// </summary>
	public static string storyskillchanges() {
		List<SkillChange> changes = new List<SkillChange>();
		foreach (SkillChange change in Princess.result.allSkillChanges) {
			if (change.isLove) continue;
			changes.Add(change);
		}
		return SkillChange.GetFormattedStringCompressed(changes, ", ");
	}

	/// <summary>
	/// Time to startgame after the intro.
	/// </summary>
	public static void introcomplete() {
		Debug.Log("calling introcomplete");
		Princess.IntroComplete();
	}
	
	public static string capitalize(string text){
		return text.CapitalizeWords();
	}
	
	public static string capitalizemem(string memID) {
		memID = memID.RemoveStart("mem_").ToLower();
		string text = Princess.GetMemory(memID);
		if (text.IsNullOrEmptyOrWhitespace()) return "";
		return text.CapitalizeWords();
	}

	public static bool capitalizememValidate(string memID) {
		memID = memID.RemoveStart("mem_").ToLower();
		if (!Story.allMemories.Contains(memID)) {
			Debug.LogWarning("Invalid memory " + memID);
			return false;
		}

		return true;
	}
	
	/// <summary>
	/// End the current story and start another
	/// </summary>
	public static void story(string storyID) {
		story(storyID, null);
	}

	/// <summary>
	/// End the current story and start another.
	/// Breaks undo, but that is a debug feature only.
	/// </summary>
	public static void story(string storyID, string choiceID) {
		Story story = Story.FromID(storyID);
		
		// continue with the current result, don't FinishStory but do RecordStory
		Result result = Princess.result;
		result.RecordStory();
		
		if (choiceID != null) {// start the story but don't execute the entry choice or show anything
			story.Execute(result, false, true);
			Choice jumpChoice = story.GetChoiceById(choiceID);
			// will point result to this new choice, append text and add stat changes
			ResultsMenu.instance.ChoiceClicked(jumpChoice, null, true);
 		} else {
			// start the story from the beginning and show it
			story.Execute(result);
		}
	}
	
	public static bool storyValidate(string storyID) {
		return storyValidate(storyID, null);
	}

	public static bool storyValidate(string storyID, string choiceID) {
		Story story = Story.FromID(storyID);
		if (story == null) {
			Debug.LogWarning("Story not found " + storyID);
			return false;
		}
		if (choiceID != null && story.GetChoiceById(choiceID) == null) {
			Debug.LogWarning("Story choiceID not found " + storyID + ", " + choiceID);
			return false;
		}
		return true;
	}
	
	/// <summary>
	/// Eg for [=call_snippet(chiefs)]
	/// Print statements are executed first, then contents (so [if] or other logic in the snippet supported).
	/// Assume snippet has no options, and just insert its text inline.
	/// Use jumps instead to execute snippets with options (eg > snippet_doneboss)
	/// </summary>
	public static string snippet(string snippetID) {
		return Story.snippetsByID[snippetID].entryChoice.resultText;
	}

	public static bool snippetValidate(string snippetID) {
		if (!Story.snippetsByID.ContainsKey(snippetID)) {
			Debug.LogWarning("Snippet not found " + snippetID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Use "default" ending for reaching age 20.
	/// All others are special deaths/wins eg dying of disease or famine, or destroying the gardeners.
	/// </summary>
	public static void endgame(string endingId) {
		Debug.Log("calling endgame " + endingId);

		if (DebugAutoplayer.running) {
			DebugAutoplayer.gameOver = true;
			return;
		}
		
		EndingMenu.instance.ShowEnding(endingId);
	}

	public static bool endgameValidate(string endingId) {
		if (endingId == "default") return true;
		Ending ending = Ending.FromID(endingId, true);
		if (ending == null) {
			ending = Ending.FromID("special_" + endingId, true);
		}
		if (ending == null) {
			Debug.LogWarning("StoryCalls.endgameValidate with unknown endingId " + endingId);
			return false;
		}
		return true;
	}
	
	// /// <summary>
	// /// Old Sol can send you back by reloading your last autosave from just before the event that ended the game.
	// /// </summary>
	// public static void undoendgame() {
	// 	SaveMenu.instance.LoadNewest();
	// }

	public static void enddemo() {
		if (Princess.HasGroundhog(Princess.hogLifeStarted) || !Princess.HasGroundhog(Princess.hogNumLives)) {
			// like ending the game, lets you unlock more complex intro the second time around in the demo
			Princess.IncrementGroundhog(Princess.hogNumLives);
			Princess.RemoveMemory(Princess.hogLifeStarted);
		}

		MainMenu.DemoEnded();
	}

	// /// <summary>
	// /// Should be followed by a jump to the beginning of the story, eg:
	// /// ~call restoreState()
	// /// > start
	// /// </summary>
	// public static void restorestate() {
	// 	Debug.Log("calling restorestate");
	// 	if (Princess.result == null || Princess.result.story == null) {
	// 		Debug.LogError("Princess has no current story in StoryCalls.restorestate " + Princess.result);
	// 		Princess.RestoreState();
	// 		return;
	// 	}
	//
	// 	Result result = Princess.result;
	// 	Princess.RestoreState();
	// 	Princess.SetResult(result);
	// 	result.ResetStory();
	// }
	
	public static void closeresults() {
		ResultsMenu.instance.DoneClicked();
	}

	public static void shop(string modeString) {
		ShopMenu.instance.OpenShop(modeString.ParseEnum<ShopMode>());
	}

	public static bool shopValidate(string modeString) {
		ShopMode mode = modeString.ParseEnum<ShopMode>();
		if (mode == ShopMode.none) {
			Debug.LogWarning("Invalid ShopMode " + modeString);
			return false;
		}
		return true;
	}

	public static void job(string jobID) {
		// if we are somehow executing a job from INSIDE a job, clear the first one
		// without this the first job will try to run an after job story
		Princess.result.ClearJob();
		
		Job job = Job.FromID(jobID);
		if (job.isExpedition) {
			MapManager.instance.LoadExploreMap(job);
			ResultsMenu.instance.DoneClicked();
		} else {
			job.Execute();
		}
	}

	public static bool jobValidate(string jobID) {
		Job job = Job.FromID(jobID);
		if (job == null) {
			Debug.LogWarning("Job not found " + jobID);
			return false;
		}
		return true;
	}

	public static void gohome() {
		MapManager.instance.GoHome(true);
	}

	/// <summary>
	/// Teleport the player to another position eg near the sneak out spot.
	/// newgame (in front of quarters), sneak (by dain pipe either in colony or nearby),
	/// exploreGlow (inside gates), or blank (default return from explore / start explore)
	/// </summary>
	public static void moveplayerto(string locationID) {
		locationID = locationID.ToLower();
		Player.instance.TeleportPlayerToEntrance(locationID);
	}
	
	public static bool moveplayertoValidate(string locationID) {
		locationID = locationID.ToLower();
		// blank is default return from explore
		if (locationID.IsNullOrEmptyOrWhitespace()) return true;
		if (locationID == "sneak" || locationID == "newgame" || locationID == "exploreglow") return true;
		Debug.LogWarning("Invalid MovePlayerTo locationID " + locationID);
		return false;
	}

	/// <summary>
	/// Return the locationID of the area where most work has happened
	/// </summary>
	public static string mostworklocation() {
		Location mostWorkedLocation = Location.allLocations.PickRandom();
		int mostTimesWorked = mostWorkedLocation?.GetTimesWorked() ?? 0;
		foreach (Location location in Location.allLocations.RandomClone()) {
			int timesWorked = location.GetTimesWorked();
			if (timesWorked > mostTimesWorked) {
				mostTimesWorked = timesWorked;
				mostWorkedLocation = location;
			}
		}
		return mostWorkedLocation?.locationID ?? "none";
	}

	/// <summary>
	/// Can't do [if call_timesrelaxed > call_timesregularjobbed] (one on left will error?)
	/// But can do [if relaxpercent > 50]
	/// </summary>
	public static int timesrelaxed() {
		int result = 0;
		foreach (Job job in Job.relaxJobs) {
			result += job.timesWorked;
		}
		return result;
	}

	public static int timesregularjobbed() {
		int result = 0;
		foreach (Job job in Job.regularJobs) {
			result += job.timesWorked;
		}
		return result;
	}

	// public static int monthssincejob(string jobID) {
	// 	// Princess.SetMemory(Princess.memJobStoryPrefix + result.job.jobID, timesWorked + storyPerTimesWorkedJob);
	// 	int nextStory = Princess.GetMemoryInt(Princess.memJobStoryPrefix + jobID.ToLower().Trim());
	// }
	
	// public static bool monthssincejobValidate(string jobID) {
	// 	Job job = Job.FromID(jobID);
	// 	if (job == null) {
	// 		Debug.LogWarning("Job not found " + jobID);
	// 		return false;
	// 	}
	// 	return true;
	// }

	/// <summary>
	/// How many monthly decisions (one per month including any skipped months) were relax jobs?
	/// </summary>
	public static int relaxpercent() {
		float percent = ((float) timesrelaxed()) / (StoryManager.currentGameMonthOfGame - 1).Clamp(1);
		return Mathf.RoundToInt(percent * 100);
	}

	/// <summary>
	/// Should only be used to load ColonyHelio halfway through the game.
	/// Loading explore maps this way (rather than via job(jobID)) may have unexpected behavior.
	/// </summary>
	public static void loadscene(string sceneID) {
		// will do nothing if we're already in the scene
		MapManager.instance.LoadMap(sceneID);
		closeresults();
	}

	public static bool loadsceneValidate(string sceneID) {
		if (!MapManager.SceneExists(sceneID.ToLower())) {
			Debug.LogWarning("SceneID not found " + sceneID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Can't use optional parameters for story calls because reflection must find 2 different methods.
	/// </summary>
	public static void battle(string battleID) {
		battle(battleID, Battle.defaultWinChoiceID, Battle.defaultLoseChoiceID);
	}
	public static bool battleValidate(string battleID) {
		return battleValidate(battleID, Battle.defaultWinChoiceID, Battle.defaultLoseChoiceID);
	}
	public static void battle(string battleID, string title) {
		battle(battleID, Battle.defaultWinChoiceID, Battle.defaultLoseChoiceID, title);
	}
	public static bool battleValidate(string battleID, string title) {
		return battleValidate(battleID, Battle.defaultWinChoiceID, Battle.defaultLoseChoiceID, title);
	}
	public static void battle(string battleID, string winChoiceID, string loseChoiceID) {
		battle(battleID, winChoiceID, loseChoiceID, null);
	}
	public static bool battleValidate(string battleID, string winChoiceID, string loseChoiceID) {
		return battleValidate(battleID, winChoiceID, loseChoiceID, null);
	}

	/// <summary>
	/// Starts a card battle.
	/// Some increase in difficulty based on the Princess' current age.
	/// Scaleable battles must have an age 10 even if it is not used until later years.
	/// Name format for scalable [battleID]_[age] eg anemone_10
	/// May skip ages and will fall back to easiest difficulty.
	/// </summary>
	public static void battle(string battleID, string winChoiceID, string loseChoiceID, string title) {
		Battle battle = new Battle(battleID, winChoiceID, loseChoiceID, title);
		BattleMenu.instance.StartBattle(battle);
	}

	public static bool battleValidate(string battleID, string winChoiceID, string loseChoiceID, string title) {
		if (!Battle.IsValidBattleID(battleID)) {
			Debug.LogWarning("Invalid battleID " + battleID);
			return false;
		}

		if (Story.validatingStory != null) {
			if (!string.IsNullOrEmpty(winChoiceID) && !Story.validatingStory.HasChoiceById(winChoiceID)) {
				Debug.LogWarning("Choice not found for battle win " + winChoiceID + ", " + Story.validatingStory);
				return false;
			}

			if (!string.IsNullOrEmpty(loseChoiceID) && !Story.validatingStory.HasChoiceById(loseChoiceID)) {
				Debug.LogWarning("Choice not found for battle lose " + loseChoiceID + ", " + Story.validatingStory);
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// After building a roguelike deck in a story, start the battle.
	/// </summary>
	public static void roguelike(int cpuLevel) {
		Debug.Log("calling roguelike " + cpuLevel);
		Battle battle = CardRoguelike.GenBattle(cpuLevel);
		BattleMenu.instance.StartBattle(battle, null, null);
		ResultsMenu.instance.DoneClicked();
	}

	/// <summary>
	/// Remove the interactive which triggered this story.
	/// </summary>
	public static void remove() {
		Debug.Log("removing last map interactive");
		// since this is never cleared it may not have actually spawned the story
		// but will work even if a card battle happened since clicking the interactive
		if (Princess.lastMapInteractive != null) {
			Princess.lastMapInteractive.Remove();
		}
	}

	/// <summary>
	/// Usually map interactives will be removed at the end of the story.
	/// This flags that they should be preserved.
	/// Only applies to explore mapspots (logged but stick around) and charas (NOT logged, as if they were never run)
	/// </summary>
	public static void keep() {
		if (Princess.result != null) {
			Princess.result.keepStory = true;
		}
	}

	/// <summary>
	/// Return bool if owning any pet card, even if it is not equipped.
	/// </summary>
	public static bool haspet() {
		return PrincessCards.HasPet();
	}

	/// <summary>
	/// Return true if owning a pet card even if is not equipped or is vriki2 or vriki3.
	/// </summary>
	public static bool haspet(string petID) {
		if (string.IsNullOrEmpty(petID)) {
			return !haspet();
		}
		return PrincessCards.HasPet(petID);
		// string currentPetCardID = PrincessCards.EquippedPetID();
		// if (string.IsNullOrEmpty(currentPetCardID)) {
		// 	return false;
		// }
		// return currentPetCardID.EqualsIgnoreCase(petID);
	}

	public static bool haspetValidate(string cardID) {
		return hascardValidate(cardID);
	}

	/// <summary>
	/// Player has collected the given card. Does not need to be equipped or in deck.
	/// </summary>
	public static bool hascard(string cardID) {
		return hascard(cardID, 1);
	}

	public static bool hascardValidate(string cardID) {
		return hascardValidate(cardID, 1);
	}

	/// <summary>
	/// Owns the card, not necessarily equipped / in deck.
	/// </summary>
	public static bool hascard(string cardID, int num) {
		return PrincessCards.HasCard(cardID, num);
	}

	public static bool hascardValidate(string cardID, int num) {
		CardData cardData = CardData.FromID(cardID);
		if (cardData == null) {
			Debug.LogWarning("Card not found for StoryCard.hasCard, " + cardID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Count of owned cards of type, not necessarily equipped / in deck.
	/// Ignoring any upgrades.
	/// </summary>
	public static int numcards(string cardID) {
		return PrincessCards.GetNumCards(cardID);
	}

	public static bool numcardsValidate(string cardID) {
		CardData cardData = CardData.FromID(cardID);
		if (cardData == null) {
			Debug.LogWarning("Card not found for StoryCard.numCards, " + cardID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Remove the given card from the princess' card pool.
	/// </summary>
	/// <param name="cardID"></param>
	public static void losecard(string cardID) {
		losecard(cardID, 1);
	}

	public static bool losecardValidate(string cardID) {
		return losecardValidate(cardID, 1);
	}

	public static void losecard(string cardID, int num) {
		PrincessCards.RemoveCard(cardID, num);
	}

	public static bool losecardValidate(string cardID, int num) {
		if (num < 0) {
			Debug.LogWarning("Invalid number for loseCard, " + num);
			return false;
		}
		CardData cardData = CardData.FromID(cardID);
		if (cardData == null) {
			Debug.LogWarning("Card not found for StoryCard.loseCard, " + cardID);
			return false;
		}
		return true;
	}

	public static bool equippedgear(string cardID) {
		return PrincessCards.HasEquippedGear(cardID);
	}

	public static bool equippedcardValidate(string cardID) {
		CardData cardData = CardData.FromID(cardID);
		if (cardData == null) {
			Debug.LogWarning("Card not found for StoryCard.equippedCard, " + cardID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Clear new cards from the current result so they won't be shown.
	/// </summary>
	public static void clearcards() {
		Princess.result?.currentCardDatas.Clear();
	}
	
	/// <summary>
	/// Hide any skill changes gotten above this line in the current choice.
	/// They've still been awarded but player will not see them in bubbles.
	/// </summary>
	public static void clearchanges() {
		Princess.result?.currentSkillChanges.Clear();
	}

	/// <summary>
	/// So we can learn facts etc but not show them.
	/// </summary>
	public static void clearunlock() {
		if (Princess.result == null) return;
		Princess.result.unlockedJob = null;
		Princess.result.unlockedUltimateJob = null;
		Princess.result.unlockedDatingChara = null;
		Princess.result.unlockedCharaFact = null;
	}

	/// <summary>
	/// For the rest of this story, do not show cards.
	/// </summary>
	public static void hideallcards() {
		if (Princess.result == null) return;
		clearcards();
		Princess.result.hideCards = true;
		Princess.result.hideCardsFromButtons = true;
	}
	
	public static void hidebuttoncards() {
		if (Princess.result == null) return;
		Princess.result.hideCardsFromButtons = true;
	}

	public static string storycards() {
		string text = "";
		foreach (CardData data in Princess.result.allCardDatas) {
			text += data.cardName + ", ";
		}
		text = text.RemoveEnding(", ");
		return text;
	}
	
	/// <summary>
	/// Check if we have ever in any life seen a background named pinup_pinupID.
	/// </summary>
	public static bool haspinup(string pinupID) {
		pinupID = pinupID.RemoveStart("pinup_").ToLower().Trim();
		// return Settings.instance.seenBackgrounds.ContainsSafe("pinup_" + pinupID);
		return Groundhogs.instance.seenBackgrounds.ContainsSafe("pinup_" + pinupID);
	}

	public static bool haspinupValidate(string pinupID) {
		pinupID = pinupID.RemoveStart("pinup_").ToLower().Trim();
		if (!AssetManager.instance.backgroundAndEndingNames.ContainsSafe("pinup_" + pinupID)) {
			Debug.LogWarning("HasPinup has invalid pinupID, " + pinupID);
			return false;
		}
		return true;
	}
	
	/// <summary>
	/// Get the current Princess age adjusted by some value.
	/// </summary>
	public static string ageplus(int value) {
		return (Princess.age + value).ToString();
	}

	public static int charaage(string charaID) {
		Chara chara = Chara.FromID(charaID);
		return chara.age;
	}

	public static bool charaageValidate(string charaID) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("CharaAge has invalid chara, " + charaID);
			return false;
		}
		return true;
	}

	// /// <summary>
	// /// Return 1, 2, or 3, depending on which age-stage art is showing for this chara.
	// /// Charas with only one stage always return 1.
	// /// </summary>
	// public static int charaart(string charaID) {
	// 	Chara chara = Chara.FromID(charaID);
	// 	return chara.currentArtStage;
	// }

	// public static bool charaartValidate(string charaID) {
	// 	Chara chara = Chara.FromID(charaID);
	// 	if (chara == null) {
	// 		Debug.LogWarning("CharaArt has invalid chara, " + charaID);
	// 		return false;
	// 	}
	// 	return true;
	// }

	/// <summary>
	/// Return the nickname of the character whose ID is in the given memory.
	/// Usage: "How did [= call_nameFromMem(bff) ] know?"
	/// Don't validate, too hard.
	/// </summary>
	public static string namefrommem(string memID) {
		string charaID = Princess.GetMemory(memID.RemoveStart("mem_").ToLower());
		Chara chara = Chara.FromID(charaID, true);
		if (chara != null) {
			return chara.nickname;
		}
		// if used in an [=inline] print statement, may be called during validation when mem is null 
		// Debug.LogWarning("StoryCalls.namefrommem has invalid memID or charaID " + memID + ", "  + charaID);
		return "they"; // TODO localize error fallback
	}

	/// <summary>
	/// True if it is currently the chara's birth month.
	/// </summary>
	public static bool charabirthday(string charaID) {
		Chara chara = Chara.FromID(charaID);
		return chara?.isBirthday ?? false;
	}

	public static bool charabirthdayValidate(string charaID) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("CharaBirthday has invalid chara, " + charaID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Return true if the given chara was not met this month.
	/// </summary>
	public static bool monthssincechara(string charaId) {
		return monthssincechara(charaId, 1);
	}
	
	/// <summary>
	/// Return true if it's been at least monthsSince months since you last spoke to or saw a chara.
	/// Also return true if you have never met them.
	/// </summary>
	public static bool monthssincechara(string charaID, int monthsSince) {
		Chara chara = Chara.FromID(charaID);
		string memoryID = Princess.memCharaMetPrefix + chara.charaID;
		if (!Princess.memories.Has(memoryID)) return true;
		int lastMetMonth = Princess.memories.GetInt(memoryID);
		return (lastMetMonth + monthsSince <= StoryManager.currentGameMonth);
	}

	public static bool monthssincecharaValidate(string charaID, int monthsSince) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("MonthsSinceChara has invalid chara, " + charaID);
			return false;
		}
		if (monthsSince <= 0) {
			Debug.LogWarning("MonthsSinceChara needs > 0 int value, " + monthsSince);
			return false;
		}
		return true;
	}

	/// <summary>
	/// How much the chara loves the princess, from 0 - 100.
	/// </summary>
	public static int getlove(string charaID) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) return 0;
		return Princess.GetLove(chara.charaID);
	}
	
	public static bool getloveValidate(string charaID) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("GetLove has invalid chara, " + charaID);
			return false;
		}
		if (!chara.canLove) {
			Debug.LogWarning("GetLove has invalid chara who can't love, " + charaID);
			return false;
		}
		return true;
	}
	
	/// <summary>
	/// Return the charaID of whichever loveable chara currently has the highest love value.
	/// May include characters with zero love. Does not include dead characters or adults or sym.
	/// </summary>
	public static string mostlove() {
		return mostlove(0);
	}
	
	public static string mostlove(int index) {
		return mostlove(index, 0, false);
	}
	
	public static string mostlove(int index, int minLove, bool flirtedOnly) {
		return mostlove(index, minLove, flirtedOnly, false);
	}
	
	/// <summary>
	/// Return the 1st, 2nd, 3rd etc most loved character. (index is 1-indexed and 0 == 1 == first character)
	/// Ties go to whoever, depends on how the sort does its thing.
	/// Exclude Sym and characters who can't love, and anyone who is dead.
	/// </summary>
	public static string mostlove(int index, int minLove, bool flirtedOnly, bool ignoreCouples) {
		return Chara.GetMostLoveChara(index, minLove, flirtedOnly, ignoreCouples)?.charaID ?? "";
	}

	/// <summary>
	/// Like mostlove but returns the character's proper name not ID.
	/// </summary>
	public static string mostlovename(){
		string charaID = mostlove();
		Chara chara = Chara.FromID(charaID);
		// doesn't account for mem_anemoneNick or tang if we will do the same thing there
		return chara == null ? "" : chara.nickname;
	}
	
	/// <summary>
	/// Return chara you've flirted with who has the highest love value, or just the chara
	/// with the highest love value.
	/// </summary>
	public static string getsecretadmirer() {
		// search for a love 3+ you've flirted with first
		string charaId = mostlove(0, 30, true, true);
		// fallback to whoever loves you most even if you're never flirted and they hate you
		if (charaId.IsNullOrEmpty()) charaId = mostlove(0, 0, false, true);
		// can return empty string
		return charaId;
	}

	//Return the difference love_charaID1 - love_charaID2 may be negative.
	public static int lovesubtract(string charaID1, string charaID2) {
		int love1 = Princess.GetLove(charaID1);
		int love2 = Princess.GetLove(charaID2);
		return love1 - love2;
	}
	
	public static bool lovesubtractValidate(string charaID1, string charaID2) {
		Chara chara1 = Chara.FromID(charaID1);
		Chara chara2 = Chara.FromID(charaID2);
		if (chara1 == null || chara2 == null) {
			Debug.LogWarning("LoveSubtract has invalid chara, " + charaID1 + ", " + charaID2);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Equivalent of:
	/// ~ifd call_hasCollectible
	/// ~ifd story_gift_tammy != 0
	/// ~ifd call_seasonsSinceStory(gift_tammy) != 0 || call_charaBirthday(tammy)
	/// </summary>
	public static bool cangift(string charaID) {
		// if (!hascollectible()) return false;
		
		// one gift per month even on birthday
		if (Princess.GetStoryMonth("gift_" + charaID) == StoryManager.currentGameMonth) return false;
		// once on their birthday even if you gave something to them last month
		if (charabirthday(charaID)) return true;
		
		// once per season: never happened (-1) or happened this season (0) or last season or earlier (1+)
		if (!Princess.HasPerk(PerkID.giftBetter)) {
			if (seasonssincestory("gift_" + charaID) == 0) return false;
		}
		return true;
	}

	public static bool cangiftValidate(string charaID) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("CanGift has invalid chara, " + charaID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Choose a collectible to give to a character.
	/// </summary>
	public static void choosegift(string charaID) {
		choosegift(charaID, true);
	}

	public static bool choosegiftValidate(string charaID) {
		return choosegiftValidate(charaID, true);
	}

	/// <summary>
	/// Choose a collectible to give to a character.
	/// </summary>
	public static void choosegift(string charaID, bool takeGift) {
		PickerMenu.instance.ShowCards(true, takeGift, 
			(CardData data) => choosegiftDone(charaID, data, takeGift));
	}

	public static bool choosegiftValidate(string charaID, bool takeGift) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null) {
			Debug.LogWarning("ChooseGift has invalid chara, " + charaID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// After a gift is chosen, record it was given.
	/// Unlock knowledge of likes/dislikes and birthdays.
	/// And increase/decrease love accordingly if tookGift is true.
	/// </summary>
	private static void choosegiftDone(string charaID, CardData data, bool tookGift) {
		Chara chara = Chara.FromID(charaID);
		if (chara == null){
			Debug.LogError("StoryCalls.ChooseGiftDone with invalid charaID: " + charaID);
			return;
		}
		
		bool isBirthday = charabirthday(charaID);
		if (isBirthday) {
			// you know their birthday now
			charafact(charaID, "birthday");
		}

		if (data.cardID.EqualsIgnoreCase("whiteFlower")) {
			// special flower increases love by 10 for everyone (except rex and sym sorry)
			if (tookGift) Princess.IncrementLove(charaID, 10, Princess.result);
			
		} else if (chara.likedCards.Contains(data)) {
			// fave items give +4 on birthdays and +2 normally (reduced from 6/4 because love from working has increased)
			if (tookGift) Princess.IncrementLove(charaID, isBirthday ? 4 : 2, Princess.result);
			charafact(charaID, "likes_" + data.cardID);
			
			// remember so you don't give another gift this month
			Princess.SetStory("gift_" + charaID);

		} else if (chara.dislikedCards.Contains(data)){
			// disliked items give -1 love (reduced from -2)
			if (tookGift) Princess.IncrementLove(charaID, -1, Princess.result);
			charafact(charaID, "dislikes_" + data.cardID);

			// hated items don't count towards daily gift limit

		} else {
			if (isBirthday) {
				if (data.cardID == "cake"){
					// cake gives +4 love on birthdays like a fav item
					if (tookGift) Princess.IncrementLove(charaID, 4, Princess.result);
				} else {
					// neutral items give +2 love on birthdays (reduced from +4)
					if (tookGift) Princess.IncrementLove(charaID, 2, Princess.result);
				}
			} else {
				
				// all neutral items give +1 on regular days (reduced from +2)
				if (tookGift) Princess.IncrementLove(charaID, 1, Princess.result);
			}
			
			// remember so you don't give another gift this month
			Princess.SetStory("gift_" + charaID);
		}
	}

	public static bool hascollectible() {
		foreach (string cardID in Princess.cards) {
			CardData data = CardData.FromID(cardID);
			if (data == null) continue;
			if (data.collectible == null) continue;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Choose and lose a collectible.
	/// Make sure there's an option named card_default in there.
	/// </summary>
	public static void choosecard(bool collectiblesOnly, bool destroyCard) {
		PickerMenu.instance.ShowCards(collectiblesOnly, destroyCard);
	}

	public static bool choosecardValidate(bool collectiblesOnly, bool destroyCard) {
		if (Princess.result == null || Princess.result.story == null) {
			Debug.LogError("Can't validate StoryCalls without Princess.result " + Princess.result);
			return false;
		}

		// after choosing a card, MenuPicker will fire choice called *=card_default
		// or *=card_[cardID] if it exists
		// or card_cancel if cancel is clicked
		Choice choice = Princess.result.story.GetChoiceById("card_default");
		if (choice == null) {
			Debug.LogWarning("ChooseCard requires choice labelled card_default");
		}

		choice = Princess.result.story.GetChoiceById("card_cancel");
		if (choice == null) {
			Debug.LogWarning("ChooseCard requires choice labelled card_cancel");
		}

		foreach (string choiceID in Princess.result.story.choicesByID.Keys) {
			if (choiceID.StartsWith("card_")) {
				if (choiceID == "card_default" || choiceID == "card_cancel") continue;
				string cardID = choiceID.Split('_')[1];
				CardData data = CardData.FromID(cardID);
				if (data == null) {
					Debug.LogWarning("ChooseCard found invalid cardID in choice label " + choiceID);
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Return true if any memory starting with the prefix exists (is true / non-zero / not null)
	/// ~if !call_memprefix(mem_date_).
	/// </summary>
	public static bool memprefix(string prefix) {
		// if the key exists then there is some value so return true
		foreach (string key in Princess.memories.Keys) {
			if (key.StartsWith(prefix)) return true;
		}
		return false;
	}

	/// <summary>
	/// Like call_memprefix(date_) except it excludes dead Dys or Sym.
	/// </summary>
	public static bool dating() {
		return !whodate().IsNullOrEmptyOrWhitespace();
	}

	/// <summary>
	/// Return the capitalized proper name of the first character you're dating, or empty.
	/// </summary>
	public static string whodate() {
		foreach (Chara chara in Chara.allCharas) {
			if (chara.isDead) continue;
			if (Princess.HasMemory("date_" + chara.charaID)) return chara.nickname;
		}
		return "";
	}

	// public static void hidespeechbubble(string charaID) {
	// 	Chara chara = Chara.FromID(charaID);
	// 	foreach (MapSpot mapspot in MapSpot.allMapSpots) {
	// 		if (mapspot.type == MapSpotType.chara && mapspot.charaID == chara.charaID) {
	// 			mapspot.showSpeechBubble = false;
	// 			break;
	// 		}
	// 	}
	// }
	//
	// public static bool hidespeechbubbleValidate(string charaID) {
	// 	Chara chara = Chara.FromID(charaID);
	// 	if (chara == null) {
	// 		Debug.LogWarning("NoSpeechBubble has invalid chara, " + charaID);
	// 		return false;
	// 	}
	// 	return true;
	// }

	/// <summary>
	/// For debugging.
	/// </summary>
	public static void setjob(string jobID) {
		Princess.result.job = Job.FromID(jobID);
	}

	/// <summary>
	/// Used for debugging.
	/// </summary>
	public static void incrementmonthsilent() {
		PrincessMonth.SetMonth(StoryManager.currentGameMonth + 1, true);
	}
	
	/// <summary>
	/// Show MonthMenu, advance time, and return to the first subchoice in current choice if no ID provided.
	/// If we're in the middle of a job, time will advance again when the event returns and ends.
	/// </summary>
	public static void incrementmonth() {
		if (Princess.result == null || Princess.result.choice == null || Princess.result.choice.choices.Count == 0) {
			incrementmonthsilent();
			return;
		}
		Choice choice = Princess.result.choice.choices[0];
		incrementmonth(choice);
	}
	
	public static void incrementmonth(string choiceID){
		Choice choice = Princess.result.story.choicesByID.GetSafe(choiceID);
		if (choice == null) {
			Debug.LogError("incrementmonth has invalid choiceID " + choiceID + ", result " + Princess.result);
			incrementmonthsilent();
			return;
		}
		incrementmonth(choice);
	}
	
	/// <summary>
	/// Show MonthMenu, increment month, then return to the given choice in the story in progress.
	/// </summary>
	public static void incrementmonth(Choice choice) {
		// keep the bg open but hide characters and results
		CharaImage.HideAllCharas();
		// hide results but keep bg visible, and don't nuke Princess.result so we can return later
		ResultsMenu.incrementingMonth = true;
		// MenuManager.CloseMenu(MenuType.results);
		// in case hiding results made this true, undo it
		ResultsMenu.showResultAfterChoiceExecution = false;
		MonthMenu.instance.StartRunner(choice);
	}
	
	public static void openmenu(string menuID){
		showmenu(menuID);
	}
	
	public static bool openmenuValidate(string menuID) {
		return showmenuValidate(menuID);
	}
	
	public static void showmenu(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		MenuManager.ShowMenu(type);
	}
	
	public static bool showmenuValidate(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		if (type == MenuType.none){
			Debug.LogWarning("ShowMenuValidate has invalid menuID, " + menuID);
			return false;
		}
		return true;
	}
	
	public static bool menuisopen(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		return MenuManager.GetMenu(type)?.isOpen ?? false;
	}
	
	public static bool menuisopenValidate(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		if (type == MenuType.none){
			Debug.LogWarning("MenuIsOpen has invalid menuID, " + menuID);
			return false;
		}
		return true;
	}
	
	/// <summary>
	/// Called during intro. Hide EVERYTHING including toolbars, except results.
	/// </summary>
	public static void hideallmenus() {
		MenuManager.CloseAllMenus(false, MenuType.results, MenuType.background);
	}
	
	public static void hidemenu(string menuID){
		closemenu(menuID);
	}
	
	public static bool hidemenuValidate(string menuID) {
		return closemenuValidate(menuID);
	}
	
	public static void closemenu(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		MenuManager.CloseMenu(type);
	}
	
	public static bool closemenuValidate(string menuID){
		MenuType type = menuID.ParseEnum<MenuType>();
		if (type == MenuType.none){
			Debug.LogWarning("CloseMenuValidate has invalid menuID, " + menuID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Colinist
	/// </summary>
	public static string getending() {
		return Ending.PickEnding().endingName;
	}

	/// <summary>
	/// You're good in a fight, and have a green thumb.
	/// </summary>
	public static string getabout() {
		Skill highestSkill = Skill.GetHighestSkill();
		return highestSkill.snippet1 + " " + Skill.GetHighestSkill(highestSkill).snippet2 + " ";
	}

	/// <summary>
	/// Return the ID of your highest regular skill.
	/// </summary>
	public static string bestskill() {
		Skill highestSkill = Skill.GetHighestSkill();
		return highestSkill.skillID;
	}

	/// <summary>
	/// Return the ID of your highest skill of the given suit.
	/// </summary>
	public static string bestskill(string suit) {
		CardSuit cardSuit = suit.ParseEnum<CardSuit>();
		Skill highestSkill = Skill.GetHighestSkill(null, cardSuit);
		return highestSkill.skillID;
	}

	public static bool bestskillValidate(string suit) {
		CardSuit cardSuit = suit.ParseEnum<CardSuit>();
		if (cardSuit == CardSuit.none) {
			Debug.LogWarning("BestSkill has invalid suit, " + suit);
			return false;
		}
		return true;
	}

	public static int bestskillvalue() {
		Skill highestSkill = Skill.GetHighestSkill();
		return highestSkill.value;
	}

	/// <summary>
	/// If best skill is 32, return 29 (stored as > 29 displayed as >= 30)
	/// </summary>
	public static int bestskillvaluerounddownminusone() {
		Skill highestSkill = Skill.GetHighestSkill();
		int value = highestSkill.value;
		value = (int) ((value / 10f).Floor() * 10f) - 1;
		return value;
	}

	public static void tutorial(string messageID) {
		TutorialMenu.ShowTutorial(messageID);
	}
	
	public static bool tutorialValidate(string messageID) {
		if (TutorialMenu.GetMessage(messageID).IsNullOrEmpty()) {
			Debug.LogWarning("TutorialValidate has invalid messageID, " + messageID);
			return false;
		}
		return true;
	}

	public static bool seentutorial(string messageID) {
		return TutorialMenu.HasSeenMessage(messageID);
	}
	
	public static bool seentutorialValidate(string messageID) {
		if (TutorialMenu.GetMessage(messageID).IsNullOrEmpty()) {
			Debug.LogWarning("TutorialValidate has invalid messageID, " + messageID);
			return false;
		}
		return true;
	}

	public static bool setting(string settingID) {
		return Settings.instance.GetSetting(settingID);
	}

	public static bool settingValidate(string settingID) {
		if (!Settings.instance.SettingExists(settingID)) {
			Debug.LogWarning("SettingValidate has invalid settingID, " + settingID);
			return false;
		}
		return true;
	}
	
	
	/// <summary>
	/// Compares the number of seasons the way this compares the number of months:
	/// "~if story_storyID < 5"
	/// If the story happened on Quiet-3 and it is now Pollen-1 (one month later), will return 1.
	/// If the story happened on Quiet-1 and it is now Pollen-1 (3 months later), still return 1.
	/// If the story happened on Quiet-3 and it is now Dust-1 (4 months later), will return 2.
	/// If the story never happened, will return -1.
	/// </summary>
	public static int seasonssincestory(string storyID) {
		return PrincessMonth.SeasonsSinceStory(storyID);
	}

	public static bool seasonssincestoryValidate(string storyID) {
		if (!Story.storiesByID.ContainsKey(storyID) && !Story.allSetStoryIDs.Contains(storyID)) {
			Debug.LogWarning("SeasonsSinceStoryValidate has invalid storyID, " + storyID);
			return false;
		}
		return true;
	}

	// public static void playambient(string typeID) {
	// 	AmbientType type = typeID.ParseEnum<AmbientType>();
	// 	SoundManager.SetAmbientOverride(type);
	// }
	//
	// public static bool playambientValidate(string typeID) {
	// 	if (typeID == "none" || typeID.IsNullOrEmptyOrWhitespace()) return true;
	// 	AmbientType type = typeID.ParseEnum<AmbientType>();
	// 	if (type == AmbientType.none) {
	// 		Debug.LogWarning("PlayAmbient has invalid typeID, " + typeID);
	// 		return false;
	// 	}
	// 	return true;
	// }

	public static void playmusic(string musicTypeID) {
		MusicType musicType = musicTypeID.ParseEnum<MusicType>();
		// prevent music from stopping when the event ends by pretending SoundManager started it
		string lastMusicLocation = MapManager.currentBiome?.biomeID;
		// during intro there's no map but it should be treated as colony so the end of intro music keeps playing
		if (!MapManager.isLoaded || lastMusicLocation.IsNullOrEmptyOrWhitespace()) lastMusicLocation = "colony";
		SoundManager.PlayMusic(musicType, true, lastMusicLocation);
	}
	
	public static bool playmusicValidate(string musicTypeID) {
		MusicType musicType = musicTypeID.ParseEnum<MusicType>();
		if (musicType == MusicType.none) {
			Debug.LogWarning("PlayMusic has invalid musicTypeID, " + musicTypeID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Stop a looping sound, especially "siren" which is started by effect = siren and plays for a long time.
	/// </summary>
	public static void stopsound(string soundID) {
		SoundType type = soundID.ParseEnum<SoundType>();
		if (soundID.EqualsIgnoreCase("siren")) {
			type = SoundType.sfx_story_effect_siren;
		}
		float fadeTime = type == SoundType.sfx_story_effect_siren ? 2f : 1f;
		SoundManager.StopLoopingOrCancellableSound(type, fadeTime);
	}
	
	public static bool stopsoundValidate(string soundID) {
		SoundType type = soundID.ParseEnum<SoundType>();
		if (soundID.EqualsIgnoreCase("siren")) {
			type = SoundType.sfx_story_effect_siren;
		}
		if (type == SoundType.none) {
			Debug.LogWarning("StopSound has invalid soundID, " + soundID);
			return false;
		}
		if (!SoundManager.IsLoopingOrCancellable2D(type)) {
			Debug.LogWarning("StopSound has un-cancellable soundID, " + soundID);
			return false;
		}
		return true;
	}

	/// <summary>
	/// To make sure that dog stops panting.
	/// </summary>
	public static void stopallloopingsounds() {
		SoundManager.StopAllLooping2DSounds();
	}

	//Return "social", "mental", or "physical" depending on the suit with the highest cumulative skills.
	public static string bestskillsuit() {
		return Princess.GetBestSkillSuit().ToString();
	}

	public static void cheevo(string cheevoID) {
		CheevoManager.MaybeAwardCheevo(cheevoID.ParseEnum<CheevoID>());
	}

	public static bool cheevoValidate(string cheevoID) {
		CheevoID id = cheevoID.ParseEnum<CheevoID>();
		if (id == CheevoID.none) {
			Debug.LogWarning("Cheevo has unrecognized cheevoID, " + cheevoID);
			return false;
		}
		return true;
	}
	
	public static void testrandomchara(bool onlyXeno = false) {
		if (Princess.result == null || Princess.result.choice == null) return;

		List<int> positions = new List<int> {1, 2, 3, 4};
		int pos1 = positions.PickRandom();
		string charaID = AssetManager.instance.charaSpriteNames.PickRandom();
		if (onlyXeno) {
			List<string> xenos = new List<string>();
			foreach (string spriteName in AssetManager.instance.charaSpriteNames) {
				if (Chara.FromCharaImageID(spriteName) != null) continue;
				xenos.Add(spriteName);
			}
			charaID = xenos.PickRandom();
		}
		
		if (pos1 == 1) {
			StorySet set = ParserStorySet.ParseSet("~set left = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("Left " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set left = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 2) {
			StorySet set = ParserStorySet.ParseSet("~set midleft = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("MidLeft " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set midleft = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 3) {
			StorySet set = ParserStorySet.ParseSet("~set midright = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("MidRight " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set midright = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 4) {
			StorySet set = ParserStorySet.ParseSet("~set right = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("Right " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set right = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}
	}
	
	
	public static void testrandom2charas(bool includeXeno = false) {
		if (Princess.result == null || Princess.result.choice == null) return;

		List<int> positions = new List<int> {1, 2, 3, 4};
		int pos1 = positions.PickRandom();
		positions.RemoveSafe(pos1);
		int pos2 = positions.PickRandom();
		
		List<string> charaIDs = new List<string>();
		charaIDs.AddSafe(AssetManager.instance.charaSpriteNames.PickRandom());
		if (includeXeno) {
			List<string> xenos = new List<string>();
			foreach (string spriteName in AssetManager.instance.charaSpriteNames) {
				if (Chara.FromCharaImageID(spriteName) != null) continue;
				xenos.Add(spriteName);
			}
			charaIDs.AddSafe(xenos.PickRandom());
		} else {
			charaIDs.AddSafe(AssetManager.instance.charaSpriteNames.PickRandom());
		}

		if (pos1 == 1 || pos2 == 1) {
			string charaID = charaIDs.Pop("anemone");
			StorySet set = ParserStorySet.ParseSet("~set left = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("Left " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set left = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 2 || pos2 == 2) {
			string charaID = charaIDs.Pop("anemone");
			StorySet set = ParserStorySet.ParseSet("~set midleft = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("MidLeft " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set midleft = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 3 || pos2 == 3) {
			string charaID = charaIDs.Pop("anemone");
			StorySet set = ParserStorySet.ParseSet("~set midright = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("MidRight " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set midright = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}

		if (pos1 == 4 || pos2 == 4) {
			string charaID = charaIDs.Pop("anemone");
			StorySet set = ParserStorySet.ParseSet("~set right = " + charaID, Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
			Debug.Log("Right " + charaID);
		} else {
			StorySet set = ParserStorySet.ParseSet("~set right = none", Princess.result.story, Princess.result.choice);
			set.Execute(Princess.result);
		}
	}

	 //Show 4 random chara images for testing.
	public static void testrandomcharas() {
		testrandomcharas(-1);
	}

	public static void testrandomcharas(int artStage) {
		if (Princess.result == null || Princess.result.choice == null) return;
		
		List<string> spriteIds = new List<string>(AssetManager.instance.charaSpriteNames);
		if (artStage > 3) {
			artStage = 3;
			// art stage 4 includes xenos and congruence, otherwise we'd have to load every Sprite to check pivots
			spriteIds = spriteIds.Where(s => s.Contains(artStage + "") || s.ParseInt() == 0).ToList();
			// spriteIds = spriteIds.Where(s => s.Contains(artStage + "") 
			// 	|| s.ParseInt() == 0 && !CharaImage.IsCongruenceOrXeno(CharaImage.charaSpritesByName[s])).ToList();
		} else if (artStage > 0) {
			// only kids in some stage
			spriteIds = spriteIds.Where(s => s.Contains(artStage + "") && Chara.FromCharaImageID(s) != null).ToList();
		}

		string charaID = spriteIds.PickRandom();
		StorySet set = ParserStorySet.ParseSet("~set left = " + charaID, Princess.result.story, Princess.result.choice);
		set.Execute(Princess.result);

		charaID = spriteIds.PickRandom();
		set = ParserStorySet.ParseSet("~set midleft = " + charaID, Princess.result.story, Princess.result.choice);
		set.Execute(Princess.result);

		charaID = spriteIds.PickRandom();
		set = ParserStorySet.ParseSet("~set midright = " + charaID, Princess.result.story, Princess.result.choice);
		set.Execute(Princess.result);

		charaID = spriteIds.PickRandom();
		set = ParserStorySet.ParseSet("~set right = " + charaID, Princess.result.story, Princess.result.choice);
		set.Execute(Princess.result);
	}
	
	public static void testrandomcharasless() {
		if (Princess.result == null || Princess.result.choice == null) return;

		CharaImage image = CharaImage.charaImages.PickRandom();
		if (image == null) return;
		
		StorySet set = ParserStorySet.ParseSet("~set " + image.location + " = none", Princess.result.story, Princess.result.choice);
		set.Execute(Princess.result);
	}
	
	public static void testrandomcharasmore() {
		if (Princess.result == null || Princess.result.choice == null) return;

		foreach (CharaImageLocation location in NWUtils.EnumValues<CharaImageLocation>()) {
			CharaImage image = CharaImage.GetImageAtIndex(location);
			if (image == null || image.sprite == null) {
				string charaID = AssetManager.instance.charaSpriteNames.PickRandom();
				StorySet set = ParserStorySet.ParseSet("~set " + location + " = " + charaID, Princess.result.story, Princess.result.choice);
				set.Execute(Princess.result);
				return;
			}
		}
	}
	
	*/
}
