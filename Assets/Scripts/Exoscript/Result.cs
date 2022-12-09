using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public enum ResultStyle {
	normal = 0,
	fancyDark = 1,
	fancyLight = 2,
}

/// <summary>
/// Data to be displayed after a choice etc.
/// Created when a job is worked or chara is approached or a mapSpot is triggered.
/// Reused until end of any possible story dialog.
/// </summary>
public class Result {
	public string text; // set in Choice.Execute and includes fonts
	public StoryChoice choice;
	// public Job job; // either babysitting etc (no mapspot) or surveying (also has mapspot)
	// public readonly Location location; // clicked on a door to start this story (also has mapspot)
	// public readonly Chara chara; // clicked on a chara to start this story (also has mapspot)
	// public MapSpot mapSpot = null; // explore events have both this and job
	// public MapSpotType mapSpotType = MapSpotType.none; // used in place of mapSpot during some dummyResults
	
	public bool continueClicked = false; // was the last choice a continue?
	public bool afterJob = false;
	
	// whether to skip clearing the mapSpot after triggering it, or not log a chara story event as run, set to true using ~call keep()
	public bool keepStory = false;

	public string bgImage;
	// set the same background that was already showing, cancels fade effects
	public bool bgImageForcedToSame = false;

	// which charas are involved with this story, to know when you last encountered them
	// sets mem_met_chara for each one at end of MenuResult.FinishStory
	// see also Story.charas
	// public List<Chara> charas = new List<Chara>();
	// to avoid having same chara in multiple mapSpots while placing stories
	public List<string> usedMapSpotSpriteIDs = new List<string>();
	
	// only left, midleft, right, midright. Not none.
	public Dictionary<CharaImageLocation, string> charaImages = new Dictionary<CharaImageLocation, string>();
	// public Chara speakerChara = null;
	public string speakerSpriteName = null; // to bring xenos to the front instead of Charas

	// current cards and skill changes are removed after they are shown to player
	// public readonly List<CardData> currentCardDatas = new List<CardData>();
	// public readonly List<SkillChange> currentSkillChanges = new List<SkillChange>();
	
	// track of all cards or all skill changes during this story
	// public readonly List<CardData> allCardDatas = new List<CardData>();
	// public readonly List<SkillChange> allSkillChanges = new List<SkillChange>();
	
	// if true, card changes won't be shown eg for first intro
	public bool hideCards = false;
	public bool hideCardsFromButtons = false;
	
	// show a note if mem_job_* or mem_ultimate_* was set to true, clear after showing to user
	// public Job unlockedJob;
	// public Job unlockedUltimateJob;
	// public string unlockedCharaFact; // eg anemone_likes_egg or rex_enhancement
	// public Chara unlockedDatingChara;
	
	// use subtle black boxes for animation in gameStartIntro instead of hologrammy blue ones
	// public bool isFancyMode;
	public ResultStyle resultStyle = ResultStyle.normal;
	public bool isFancyMode => resultStyle != ResultStyle.normal;
	// pause before showing the next block of text, used for intro animation (multiplier, < 0 is disabled)
	public int delayTextSeconds = -1;
	// public bool delayHideMenus = false;

	// story is set during Story.Execute and might reuse one result for multiple stories.
	private Story _story;
	public Story story {
		get => _story;
		set {
			_story = value;
			// if (EndingMenu.isOpen) {
			// 	// match the background - white for age20 career endings, black for special endings
			// 	// this default can be overridden by ending events
			// 	resultStyle = EndingMenu.instance.useBlackEffects ? ResultStyle.fancyDark : ResultStyle.fancyLight;
			// 	bgImage = BackgroundMenu.instance.currentImageID;
			// } else {
				// can be changed in exoscript via ~call fancyMode(true) which defaults to fancyDark
				resultStyle = ResultStyle.normal;
				bgImage = null;
			// }
			keepStory = false;
		}
	}

	// /// <summary>
	// /// Charas, location doors, explore events, collectibles, set stories (eg go home) all have mapspots.
	// /// Explore events may also have a job. Babysitting etc events only have a job.
	// /// High priority events have neither job or mapspot.
	// /// </summary>
	// public Result(Job job = null, MapSpot mapSpot = null) {
	// 	this.job = job;
	//
	// 	if (mapSpot != null && !mapSpot.locationID.IsNullOrEmpty()) {
	// 		location = Location.FromID(mapSpot.locationID);
	// 	}
	// 	if (location == null && job != null) {
	// 		location = job.location;
	// 	}
	//
	// 	if (mapSpot != null && !mapSpot.charaID.IsNullOrEmpty()) {
	// 		chara = Chara.FromID(mapSpot.charaID);
	// 	}
	// 	
	// 	this.mapSpot = mapSpot;
	// 	this.mapSpotType = mapSpot?.type ?? MapSpotType.none;
	// 	
	// 	Reset();
	//
	// 	charas.AddSafe(chara);
	// }
	
	// public Result(MapSpot mapSpot) : this(null, mapSpot) { }

	/// <summary>
	/// Pick a default bg image for the given story, always called before bg or images are set.
	/// For charas, also add them in a random position. Chara will be replaced if background is set later.
	/// Both may be overridden by the first or any choice in a story.
	/// </summary>
	public void SetDefaultImages() {
		if (!string.IsNullOrEmpty(bgImage)) {
			return;
		}
		bgImage = null;

		// if (story == null) {
		// 	bgImage = BackgroundMenu.GetBackgroundForJob(job) ?? BackgroundMenu.randomBg;
		// 	return;
		// }
		//
		// // try to find a default background for the job
		// // use story.job because it may be null (for Location.priority) while this.job is babysitting
		// if (story.job != null) {
		// 	bgImage = BackgroundMenu.GetBackgroundForJob(job);
		// 	if (bgImage != null) return;
		// }
		//
		// // default background for location
		// // use story.location because it may be Location.priority while this.location = quarters because we were babysitting
		// if (story.location != null) {
		// 	Location bgLocation = story.location;
		// 	if (bgLocation == Location.none) {
		// 		// might be a location story
		// 		bgLocation = Location.FromID(story.storyID);
		// 		if (bgLocation == null) bgLocation = Location.none;
		// 	}
		//
		// 	string locationBg = bgLocation.defaultBackground;
		// 	if (BackgroundMenu.BgImageExists(locationBg)) {
		// 		bgImage = locationBg;
		// 		return;
		// 	}
		// 	string locationID = bgLocation.locationID;
		// 	if (BackgroundMenu.BgImageExists(locationID)) {
		// 		bgImage = locationID;
		// 		return;
		// 	}
		// }
		//
		// // set talking head against the chara's default background
		// if (story.chara != null) {
		// 	if (GameManager.season == Season.glow) {
		// 		bgImage = "glow";
		// 	} else if (!string.IsNullOrEmpty(story.chara.defaultBackground)) {
		// 		bgImage = story.chara.defaultBackground;
		// 	} else {
		// 		bgImage = BackgroundMenu.randomBg;
		// 	}
		//
		// 	if (charaImages.Count == 0) {
		// 		// SetDefaultImages usually gets called at the start of an event so this should be true
		// 		// except during ~call story() where we are shunted here from another event 
		// 		// show the character against the background by default for chara events
		// 		// chara will be cleared if bg is set in the event text
		// 		Sprite charaSprite = story.chara.GetStorySprite();
		// 		if (charaSprite != null) {
		// 			CharaImageLocation position = NWUtils.PickRandomEnum<CharaImageLocation>(true, "charaimage" + GameManager.month);
		// 			SetCharaImage(position, charaSprite.name);
		// 		}
		// 	}
		//
		// 	return;
		// }
		//
		// // darkness / error illustration
		// bgImage = BackgroundMenu.randomBg;
	}

	/// <summary>
	/// Prepare to show some chara or other subject in front of the background image.
	/// Will actually be set in MenuResults.ShowChara().
	/// </summary>
	public void SetCharaImage(CharaImageLocation location, string spriteName) {
		if (location == CharaImageLocation.none) {
			Debug.LogError("Result.SetCharaImage with none position");
			return;
		}
		
		// Chara spriteChara = Chara.FromCharaImageID(spriteName);
		//
		// // if (spriteChara != null && spriteChara.isDead && !GameSettings.debugAllTextChunks) {
		// if (spriteChara != null && spriteChara.isDead && !EditorUtils.isEditor) {
		// 	// if (!GameSettings.debug) {
		// 	// hide dead characters to be safe
		// 	Debug.LogWarning("Tried to show a dead chara " + spriteChara + " in " + GameManager.result?.story);
		// 	spriteName = null;
		// 	spriteChara = null;
		// 	// }
		// }
		//
		// if (spriteName.IsNullOrEmpty()) {
		// 	if (speakerChara != null && Chara.FromCharaImageID(charaImages.GetSafe(location)) == speakerChara) {
		// 		// the speaker left, so change the speaker to the leftmost remaining chara
		// 		// do nothing if a bushbub leaves
		// 		speakerChara = null;
		// 		foreach (CharaImageLocation loc in Enum.GetValues(typeof(CharaImageLocation))) {
		// 			if (loc == location || !charaImages.ContainsKey(loc)) continue;
		// 			Chara charaAtLoc = Chara.FromCharaImageID(charaImages.GetSafe(loc));
		// 			if (charaAtLoc != null) {
		// 				speakerChara = charaAtLoc;
		// 				break;
		// 			}
		// 		}
		// 	}
		// 	charaImages[location] = null;
		// 	return;
		// }
		//
		// // chara becomes the new speaker even if it's a bushbub or something that can't talk, it will be in the front
		// speakerSpriteName = spriteName;
		// if (spriteChara != null) {
		// 	// record that this chara was spoken to during this story
		// 	charas.AddSafe(spriteChara);
		// 	// even if the chara was already showing in the same place, they become the new speaker
		// 	speakerChara = spriteChara;
		// }
		//
		// // clear any existing references to this character in other locations so no dupes or jumping around
		// if (!BackgroundEffects.isEnding) {
		// 	foreach (CharaImageLocation existingLocation in charaImages.Keys) {
		// 		string existingSpriteName = charaImages.GetSafe(existingLocation);
		// 		Chara existingChara = Chara.FromCharaImageID(existingSpriteName);
		// 		if (existingSpriteName == spriteName || (existingChara != null && existingChara == spriteChara)) {
		// 			charaImages[existingLocation] = null;
		// 			// avoid InvalidOperationException since there can only be one anyway
		// 			break;
		// 		}
		// 	}
		// }

		charaImages[location] = spriteName;
	}

	public void SetSpeaker(string charaID) {
		// "none" etc will clear it
		// speakerChara = Chara.FromID(charaID, true);
		speakerSpriteName = charaID; // might be none, null, etc, or charaID or sprite name eg "bushbub2"
	}

	// public void AddCard(CardData cardData) {
	// 	if (hideCards) return;
	// 	currentCardDatas.Add(cardData);
	// 	allCardDatas.Add(cardData);
	// }

	// public void AddSkillChange(SkillChange skillChange) {
	// 	currentSkillChanges.Add(skillChange);
	// 	allSkillChanges.Add(skillChange);
	// }

	public void ClearCharaImages() {
		charaImages.Clear();
		speakerSpriteName = null;
	}
	
	/// <summary>
	/// After showing changes to the player after every choice.
	/// </summary>
	public void ClearChanges() {
		// currentSkillChanges.Clear();
		// currentCardDatas.Clear();
		// lastBgImage = bgImage;
	}

	// /// <summary>
	// /// Called by StoryCalls.finishMonth when needing to finish job and show MonthMenu halfway through an event.
	// /// </summary>
	// public void ClearJob() {
	// 	job = null;
	// }

	/// <summary>
	/// Note that we did this story.
	/// </summary>
	public void RecordStory() {
		// record the story was seen in the current month (unless a chara story with ~set keep)
		// if (!keepStory || mapSpot == null || mapSpot.type != MapSpotType.chara) {
			StoryManager.SetStory(story);
		// }

		// // record which charas were spoken to
		// foreach (Chara usedChara in charas) {
		// 	usedChara.hasMet = true;
		// 	// Princess.SetMemory(Princess.memCharaMetPrefix + usedChara.charaID, StoryManager.currentGameMonth.ToString());
		// }
	}

	/// <summary>
	/// Called when Princess.result will be cleared or changed.
	/// Record that the story was played, then possibly play another.
	/// Does not get called during ~call story() but RecordStory does.
	/// </summary>
	public void FinishStory() {
		if (story == null) {
			Debug.LogError("Result.FinishStory has no story");
			return;
		}

		RecordStory();
		
		// BackgroundMenu might not close between stories (or before job battle) but make sure any effects are cleared
		// BackgroundEffects.instance.HideEffects();

		// if (mapSpot != null && mapSpot.type == MapSpotType.chara) {
		//
		// 	if (!keepStory) {
		// 		// for charas, keepStory means pretend this story was never run (if first will still be true next time) 
		// 		
		// 		if (Story.storiesByCharaReg[chara].Contains(story) || Story.storiesByCharaHigh[chara].Contains(story)) {
		// 			// wait some months before choosing another regular-priority chara story
		// 			// usually 3
		// 			int eventMonthDelay = 3;
		// 			if (GameManager.age == 18) {
		// 				//but if we're getting near the end speed it up
		// 				eventMonthDelay = 2;
		// 			} else if (GameManager.age == 19) {
		// 				eventMonthDelay = 1;
		// 			}
		// 			GameManager.SetMemory(GameManager.memCharaStoryPrefix + chara.charaID, GameManager.month + eventMonthDelay);
		// 		}
		//
		// 		// set the chara to the next high-priority story or low story
		// 		mapSpot.SetOrPickCharaStory(null);
		// 	}
		//
		// } else if (mapSpot != null && mapSpot.type == MapSpotType.collectible) {
		// 	// collectible fades out / sinks into the ground after collecting
		// 	mapSpot.Remove(true);
		//
		// } else if (mapSpot != null && keepStory) {
		// 	// don't remove or give +15 stress if you called keep()
		//
		// } else if (mapSpot != null && !MapManager.isLoaded) {
		// 	// don't give +15 stress if you're going home, just remove it
		// 	mapSpot.Remove();
		//
		// } else if (mapSpot != null && mapSpot.type == MapSpotType.start) {
		// 	// don't give +15 for utopia start events, just remove them
		// 	mapSpot.Remove();
		//
		// } else if (mapSpot != null && mapSpot.isExploreType) {
		// 	// by default remove explore spots (start, bottleneck, miniboss, boss), unless ~call keep() prevents it
		// 	mapSpot.Remove();
		//
		// 	// stress goes up after every cleared event (not chara-sym or collectibles) in explore maps
		// 	Result dummyResult = new Result();
		// 	// int exploreStress = Princess.HasPerk(PerkID.reduceExploreStress)
		// 	// 	? 10
		// 	// 	: Variables.explore_stress_increment;
		// 	int exploreStress = Variables.explore_stress_increment;
		// 	if (GameManager.HasPerk(PerkID.reduceExploreStress)) {
		// 		exploreStress -= Variables.explore_reduceExploreStress;
		// 	}
		// 	GameManager.IncrementSkill(Skill.stress, exploreStress, dummyResult, true);
		// 	SkillChange change = dummyResult.allSkillChanges.GetValueSafe(0);
		// 	if (change != null && change.skill == Skill.stress) {
		// 		if (GameManager.HasPerk(PerkID.reduceExploreStress)) {
		// 			change.bonusValue -= Variables.explore_reduceExploreStress;
		// 			change.bonusText += (change.bonusText.IsNullOrEmptyOrWhitespace() ? "" : ";")
		// 				+ Perk.FromID(PerkID.reduceExploreStress).perkName;
		// 		}
		// 		// Time passes... +5 stress
		// 		PlayerText.ShowLocalized("player_explore_stress", change.GetFormattedString());
		// 	}	
		// 	
		// 	// other mapSpots like location and story are not cleared
		// 	
		// } else if (mapSpot == null && job != null) {
		// 	// finishing an event either before or after job battle (including returning from exploring)
		// 	if (afterJob) {
		// 		// may return here again if 2+ high-priority location/all/priority events
		// 		// if not this will advance the month after jobbing
		// 		job.AfterBattleStory(story.priority);
		// 	} else {
		// 		// may return here again if 2+ high-priority job events
		// 		job.Execute(story.priority);
		// 	}
		// }

		// maybe run stress events or fill mapspots unless we're in the process of changing maps
		// if (MapManager.isLoaded) {
		// 	if (Skill.stress.nearMax && !story.storyID.EqualsIgnoreCase("exploreStressMax")) {
		// 		// warn when stress is 80% or higher
		// 		if (MapManager.inColony) {
		// 			if (Skill.stress.isMaxed) {
		// 				PlayerText.ShowLocalized("player_colony_stress_max");
		// 			} else {
		// 				PlayerText.ShowLocalized("player_colony_stress_high");
		// 			}
		// 		} else {
		// 			if (Skill.stress.isMaxed) {
		// 				if (!EndingMenu.isOpen) {
		// 					// max stress while exploring triggers go home event
		// 					Story stressStory = Story.FromID("exploreStressMax");
		// 					if (stressStory == null) {
		// 						Debug.LogError("Result.FinishStory failed to find exploreStressMax story");
		// 						MapManager.instance.GoHome(true);
		// 					} else {
		// 						Result stressResult = new Result();
		// 						stressStory.Execute(stressResult);
		// 					}
		// 				}
		// 			} else {
		// 				PlayerText.ShowLocalized("player_explore_stress_high");
		// 			}
		// 		}
		// 	}
		//
		// 	// make sure all stories on the map are still valid after any story (except collecting)
		// 	if (story.collectible == null) {
		// 		// first turn on mapspots (eg day 2 tammy) that will next be filled
		// 		// triggers BillboardManager.InitAllBillboards and Toggler.Execute on everything, slow while exploring
		// 		RequirementToggler.ToggleAll();
		// 		
		// 		// avoid adding new collectibles until day changes
		// 		BillboardManager.FillMapspots(false, true);
		//
		// 	}
		// }
	}

	public void ResetStory() {
		if (story == null) {
			Debug.LogError("Can't ResetStory a result with no story.");
			return;
		}

		story.Reset();
		Reset();
	}

	/// <summary>
	/// Called when creating Result, and in rare cases when resetting the story (eg for Undo).
	/// Does not clear the job/location/chara/mapspot or story but rewinds as if it had not been run.
	/// </summary>
	public void Reset() {
		text = "";

		choice = null;
		ClearChanges();
		// allCardDatas.Clear();
		// allSkillChanges.Clear();
		hideCards = false;
		hideCardsFromButtons = false;
		bgImage = null;
		charaImages.Clear();
	}

	public override string ToString() {
		return "Result [" + story + ", choice=" + choice + "]";
	}
}
