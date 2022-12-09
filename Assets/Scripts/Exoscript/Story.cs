using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A fancy container for the entryChoice of a dialog tree.
/// </summary>
public class Story {
	public string storyID;
	public StoryChoice entryChoice;
	public Priority priority;

	// for debugging
	public string storyIDCamelcase;
	
	// temporarily set while choosing stories for a map, to avoid picking the same one twice
	public bool onMapDuringFill = false;
	
	// while executing, remembers what choices were made
	public readonly List<StoryChoice> selectedChoices = new List<StoryChoice>();

	// while executing the story, holds results of ~set var_varname = somevalue 
	public readonly StringDictionary vars = new StringDictionary();

	// includes any nested choices labelled with (lowercase) IDs for jumping - does not include non-labelled choices
	public readonly Dictionary<string, StoryChoice> choicesByID = new Dictionary<string, StoryChoice>();
	// includes all choices; used for both validation and random seeding
	// set during parse then cleared and set again during finish after jumps might have changed choices
	public readonly List<StoryChoice> allChoices = new List<StoryChoice>();

	// used for validation and debug settings
	private bool locationSetOnce = false;
	private bool mapspotSetOnce = false;
	private int nextGeneratedChoiceID = 0;
	// set during ValidateAndFinish
	public List<string> allVars = new List<string>();

	public static List<Story> allStories = new List<Story>();
	public static Dictionary<string, Story> storiesByID = new Dictionary<string, Story>();

	// stories starting with this are actually text snippets inserted to other stories
	public static Dictionary<string, Story> snippetsByID = new Dictionary<string, Story>();

	// set during ValidateAndFinish
	public readonly List<string> debugSetMemories = new List<string>(); // memories set in this story
	public static List<string> allMemories = new List<string>(); // memories set in any story
	public static List<string> allGroundhogs = new List<string>();
	public static List<string> allSetStoryIDs = new List<string>();
	public static Story validatingStory = null;

	public const string snippetPrefix = "snippet_";

	private StoryChoice EndStoryChoice => choicesByID.ContainsKey("end") ? choicesByID["end"] : null;
	public bool hasExecuted => StoryManager.HasStory(this);

	public Story() {
		entryChoice = new StoryChoice(this);
		// do not add to lists yet
	}

	public Story(StoryTemplate template) {
		storyID = template.storyID;
		storyIDCamelcase = template.storyIDCamelcase;
		entryChoice = new StoryChoice(this, template.entryChoice);
	}

	public StoryTemplate ToTemplate() {
		return new StoryTemplate {
			storyID = storyID,
			storyIDCamelcase = storyIDCamelcase,
			entryChoice = entryChoice.ToTemplate(),
			// collectibleID = collectible?.collectibleID
		};
	}

	public void SetID(string idString) {
		// ids are lower case
		storyID = idString.Trim().ToLower();
		if (storiesByID.ContainsKey(storyID)) {
			Debug.LogWarning("Story already exists with this key, may cause problems " + this.storyID);
		}
		storyIDCamelcase = idString;
	}

	public string GetUniqueChoiceId() {
		return $"choice_{nextGeneratedChoiceID++}";
	}

	public void Reset() {
		selectedChoices.Clear();
		vars.Clear();
	}

	/// <summary>
	/// Return true if the story can be run or placed on the map.
	/// alreadyOnMap used when double-checking existing mapspot stories (skip onMapDuringFill and repeat) 
	/// </summary>
	public bool CanExecute(Result result, bool alreadyOnMap = false) {
		// when called before placing it on the map, avoid duplicates if it's already in another mapspot
		// alreadyOnMap = spawned way earlier, onMapDuringFill = in the middle of story spawning loop
		// if (!alreadyOnMap && result.mapSpot != null && onMapDuringFill) return false;

		// if story has run once, it needs a repeat req to ever run again
		// exception is if it's already sitting on the map due to an earlier keep() call
		// exception to the exception: exploreSymMeet etc which can appear in multiple scenes (and can't use keep)
		if (!alreadyOnMap) {
			if (StoryManager.HasStory(this)) {
				StoryReq repeatReq = GetRepeatReq();
				// no repeat means we can only play it once and we've already played it, so nope can't execute again
				if (repeatReq == null) return false;
				// eg repeat_season but a season hasn't passed yet
				if (!repeatReq.RepeatPassed(result)) {
					return false;
				}
			}
		}
		
		StoryReq req = GetExecuteBlockingReq(result, alreadyOnMap);
		if (req != null) return false;
		
		// avoid duplicate utopias etc
		// if (!alreadyOnMap && result.mapSpot != null && result.mapSpot.type != MapSpotType.boss) {
		// 	// bosses always filled last and always allowed with dupes
		// 	string spriteID = GetSpriteID();
		// 	if (result.usedMapSpotSpriteIDs.Contains(spriteID)) {
		// 		if (spriteID == "utopia" || spriteID == "dys" || spriteID == "sym" || spriteID == "nomi") {
		// 			if (BillboardManager.instance.logDebugInfo) Debug.Log("Not adding story " + storyID + " to map because sprite already used: " + spriteID);
		// 			return false;
		// 		}
		// 	}
		// }
		
		return true;
	}
	
	/// <summary>
	/// Return the first requirement which prevents this story from executing.
	/// </summary>
	public StoryReq GetExecuteBlockingReq(Result result, bool alreadyOnMap = false) {
		// entryChoice requirements must all be met
		foreach (StoryReq req in entryChoice.requirements) {
			// DO check if job has changed for exploreNearby vs sneakGlow in nearby maps
			if (alreadyOnMap && (req.type != StoryReqType.job && StoryReq.placementTypes.Contains(req.type))) {
				// ignore location, chara etc when double-checking a story already on the map
				continue;
			}

			if (!req.Execute(result)) {
				return req;
			}
		}

		return null;
	}

	/// <summary>
	/// Start playing through a story from the beginning.
	/// Probably won't clear the old story because we might be jumping to a second story from in a first one.
	/// </summary>
	/// <param name="result"></param>
	/// <param name="undoing"></param>
	/// <param name="startStoryOnly">Used by ~call story(choiceID) if we're about to jump to a choice</param>
	public void Execute(Result result, bool undoing = false, bool startStoryOnly = false) {
		Debug.Log("Executing story " + this + "\n");

		if (result == null) {
			Debug.LogError("Can't ExecuteStory without a result");
			return;
		}

		Reset();
		result.story = this;
		StoryManager.SetResult(result);

		// pick a default bg image and possibly set the chara against it
		result.SetDefaultImages();
		
		// eg if jumping into the middle of a story, don't execute the entry choice or show anything
		if (!startStoryOnly) {
			// may show characters etc in ResultsManager
			entryChoice.Execute(result);
			// TODO show the result ResultsMenu.instance.ShowResult();
		}
	}

	public void AddSelectedChoice(StoryChoice storyChoice) {
		selectedChoices.Add(storyChoice);
	}

	public bool HasChoiceById(string choiceID) {
		choiceID = choiceID.Trim().ToLower();
		return choicesByID.ContainsKey(choiceID);
	}

	public StoryChoice GetChoiceById(string choiceID, bool warnInvalid = true, bool checkNonLabelChoices = false) {
		choiceID = choiceID.Trim().ToLower();
		if (choicesByID.ContainsKey(choiceID)) return choicesByID[choiceID];

		if (checkNonLabelChoices) {
			// used for debugging when reloading scripts to jump to a particular choice by numerical ID
			foreach (StoryChoice choice in allChoices) {
				if (choice.choiceID == choiceID) return choice;
			}
		}
		
		if (warnInvalid) {
			Debug.LogError("Can't jump to unknown choiceID " + choiceID);
		}
		return null;
	}

	/// <summary>
	/// Return the first set image or set sprite in the entry choice.
	/// Technically this is a prefabID as it includes hitbox and shader info.
	/// </summary>
	public string GetSpriteID() {
		foreach (StorySet set in entryChoice.sets) {
			if (set.type == StorySetType.billboardSprite) {
				return set.stringValue;
			}
		}
		return null;
	}
	
	/// <summary>
	/// For debugging, get calls to launch another story.
	/// </summary>
	public List<Story> GetStoryCalls() {
		List<Story> nextStories = new List<Story>();
		foreach (StoryChoice choice in allChoices) {
			foreach (StorySet set in choice.sets) {
				if (set.type == StorySetType.call) {
					if (set.debugString.ToLower().Contains("story(") && set.call.parameterArray.Length > 0) {
						string callStoryID = set.call.parameterArray[0] as string;
						Story callStory = FromID(callStoryID);
						if (callStory != null) {
							nextStories.Add(callStory);
						}
					}
				}
			}
		}
		return nextStories;
	}
	
	public List<StoryReq> GetStoryCallReqs(string callStoryIDToFind) {
		List<Story> nextStories = new List<Story>();
		foreach (StoryChoice choice in allChoices) {
			foreach (StorySet set in choice.sets) {
				if (set.type == StorySetType.call) {
					if (set.debugString.ToLower().Contains("story(") && set.call.parameterArray.Length > 0) {
						string callStoryID = set.call.parameterArray[0] as string;
						if (callStoryID == callStoryIDToFind) {
							return choice.requirements;
						}
					}
				}
			}
		}
		return new List<StoryReq>();
	}

	public StoryReq GetReqOrSubreqOfType(StoryReq req, StoryReqType type) {
		if (req == null) return null;
		if (req.type == type) return req;
		if (req.type == StoryReqType.and || req.type == StoryReqType.or) {
			foreach (StoryReq subReq in req.subReqs) {
				// recurse through parts not caring if they're AND or OR
				StoryReq found = GetReqOrSubreqOfType(subReq, type);
				if (found != null) return found;
			}
		}
		return null;
	}

	/// <summary>
	/// recursively look through reqs for ones in format:
	/// ~if repeat
	/// ~if repeat_years = 4
	/// ~if repeat && mem_memName
	/// ~if repeat && !story_storyName || story_storyName > 1 (aka repeat_today)
	/// ~if repeat || story_storyName (not sure what this would do, but it would find it here)
	/// ~if mem_memName && repeat_season || story_storyName (also not advised)
	/// </summary>
	public StoryReq GetRepeatReq() {
		foreach (StoryReq req in entryChoice.requirements) {
			StoryReq found = GetReqOrSubreqOfType(req, StoryReqType.repeat);
			if (found != null) return found;
		}
		return null;
	}

	public bool GetShowSpeechBubble() {
		StoryReq charaReq = null;
		foreach (StoryReq req in entryChoice.requirements) {
			if (req.type == StoryReqType.chara) {
				charaReq = req;
				break;
			}
		}
		if (charaReq != null) {
			return charaReq.flagValue == true;
		} else {
			// no chara means it's an explore or location event
			return false;
		}
	}

	/// <summary>
	/// When DataLoader is finished loading this story.
	/// Add to lists, cut up Choices by page.
	/// Final validation is done once all stories are added, in ValidateAndFinish.
	/// </summary>
	public void FinishLoading() {
		if (string.IsNullOrEmpty(storyID)) {
			Debug.LogError("Story found with null ID " + this);
			storyID = "none";
		}

		// text snippet used in multiple stories
		if (storyID.StartsWith(snippetPrefix)) {
			string snippetID = storyID.Substring(snippetPrefix.Length);
			this.entryChoice.buttonText = "Continue";

			if (snippetsByID.ContainsKey(snippetID)) {
				Debug.LogWarning("Duplicate snippet ID, overwriting " + snippetID);
			}
			snippetsByID[snippetID] = this;
			
			// forget this story, store only the snippet text
			return;
		}

		storiesByID[storyID] = this;
		allStories.Add(this);

		// reqs for two jobs means it can happen during either job
		locationSetOnce = false;
		mapspotSetOnce = false;

		// find location, chara, mapspot and/or job and pull the story's priority out of it
		foreach (StoryReq req in entryChoice.requirements) {
			FinishFindPriority(req);
		}

		foreach (StoryReq req in entryChoice.requirements) {
			// may set locationSetOnce or mapspotSetOnce
			FinishFindLocation(req);
		}

		// some stories can happen anywhere, but should explicitely set location = all
		if (!locationSetOnce && !mapspotSetOnce) {
			// Debug.LogWarning("Story has no location/job/chara/mapspot, using location all, " + this);
			// storiesByLocationReg.AddToDictionaryList(Location.all, this);
		}

		// validate mapspot sprite art
		if (mapspotSetOnce) {
			string spriteID = GetSpriteID();
			if (string.IsNullOrEmpty(spriteID)) {
				Debug.LogWarning("Story has no mapspot sprite, " + this);
			}
		}

		// Recursively add labelled choices to choicesByID
		allChoices.Clear();
		choicesByID.Clear();
		RegisterChoices(entryChoice);

		// assign choiceIDs to un-labelled choices for undoing and saving
		int choiceIDInt = 0;
		foreach (StoryChoice choice in allChoices) {
			choiceIDInt++;
			if (string.IsNullOrEmpty(choice.choiceID)) {
				choice.choiceID = storyID + "_" + choiceIDInt;
			}
		}
		
		// jump every deadend choice to endChoice if it exists
		if (EndStoryChoice != null) {
			// end will always have a break before it, nothing for it
			foreach (StoryChoice choice in allChoices) {
				if (choice == EndStoryChoice) continue;
				if (!choice.isEnd) continue;
				if (EndStoryChoice.IsAboveChoice(choice)) continue;
				StorySet jump = StoryParserSet.ParseJump(this, "> end", choice);
				choice.AddSet(jump);
			}
		}
	}

	/// <summary>
	/// Set the priority variable based on chara, mapSpot, job or location.
	/// If both mapspot and job are set, but one is high_ or low_ use that one.
	/// </summary>
	private void FinishFindPriority(StoryReq req, bool ignoreDoubleLocationWarnings = false) {
		if (req.type == StoryReqType.or || req.type == StoryReqType.and) {
			foreach (StoryReq subreq in req.subReqs) FinishFindPriority(subreq, true);
			return;
		}
		if (req.compare != StoryReqCompare.equal) return;
		if (req.type == StoryReqType.chara || req.type == StoryReqType.mapSpot || req.type == StoryReqType.job ||
			req.type == StoryReqType.location) {
			if (priority == Priority.none || priority == Priority.average) {
				priority = req.priority;
			}
		}

		if (priority == Priority.none) {
			priority = req.priority;
		}
	}

	/// <summary>
	/// Put the story in a dictionary based on where/when it occurs.
	/// </summary>
	private void FinishFindLocation(StoryReq req, bool ignoreDoubleLocationWarnings = false) {
		if (priority == Priority.none) {
			Debug.LogWarning("FinishFindLocation but failed to find any priority earlier. " + this);
			return;
		}
		if (req.type == StoryReqType.or || req.type == StoryReqType.and) {
			foreach (StoryReq subreq in req.subReqs) {
				// "or" clause means add to both lists
				// "and" is same but both will be required at runtime
				FinishFindLocation(subreq, true);
			}
			return;
		}
		if (req.compare != StoryReqCompare.equal) return;

		bool locationSet = false;

		if (req.type == StoryReqType.chara) {
			// Chara chara = Chara.FromID(req.stringID);
			// if (chara != null) {
			// 	this.chara = chara;
			// 	locationSet = true;
			// 	// bool set means low_jobname; lowest priority after general to location or anywhere
			// 	if (priority == Priority.high) {
			// 		storiesByCharaHigh.AddToDictionaryList(chara, this);
			// 	} else if (priority == Priority.low) {
			// 		storiesByCharaLow.AddToDictionaryList(chara, this);
			// 	} else {
			// 		storiesByCharaReg.AddToDictionaryList(chara, this);
			// 	}
			// }

		} else if (req.type == StoryReqType.mapSpot) {
			// MapSpotType mapSpotType = (MapSpotType)req.intValue;
			// if (mapSpotType != MapSpotType.none) {
			// 	// can have both mapspot and job
			// 	this.mapSpotType = mapSpotType;
			// 	this.location = Location.FromID("expeditions");
			// 	
			// 	if (mapspotSetOnce && !ignoreDoubleLocationWarnings) {
			// 		Debug.LogWarning("Story.FinishReq mapspot set twice, use OR clause instead " + this);
			// 	}
			// 	mapspotSetOnce = true;
			// 	
			// 	// bool set means high_mapspottype; highest priority, explore equivalent of Location.priority
			// 	if (priority == Priority.high) {
			// 		storiesByMapSpotHigh.AddToDictionaryList(mapSpotType, this);
			// 	} else if (priority == Priority.low) {
			// 		storiesByMapSpotLow.AddToDictionaryList(mapSpotType, this);
			// 	} else {
			// 		storiesByMapSpotReg.AddToDictionaryList(mapSpotType, this);
			// 	}
			// }

		} else if (req.type == StoryReqType.job) {
			// Job job = Job.FromID(req.stringID);
			// if (job != null) {
			// 	this.job = job;
			// 	this.location = job.location;
			// 	locationSet = true;
			// 	// bool set means low_jobname; lowest priority after general to location or anywhere
			// 	if (priority == Priority.high) {
			// 		storiesByJobHigh.AddToDictionaryList(job, this);
			// 	} else if (priority == Priority.low) {
			// 		storiesByJobLow.AddToDictionaryList(job, this);
			// 	} else {
			// 		storiesByJobReg.AddToDictionaryList(job, this);
			// 	}
			// }

		} else if (req.type == StoryReqType.location) {
			// // may be real location or none, all, priority
			// Location location = Location.FromID(req.stringID);
			// if (location != null) {
			// 	this.location = location;
			// 	locationSet = true;
			// 	if (location == Location.priority) {
			// 		// most important plot events above everything else
			// 		storiesByLocationPriority.AddToDictionaryList(location, this);
			// 	} else if (priority == Priority.high) {
			// 		storiesByLocationHigh.AddToDictionaryList(location, this);
			// 	} else if (priority == Priority.low) {
			// 		storiesByLocationLow.AddToDictionaryList(location, this);
			// 	} else {
			// 		storiesByLocationReg.AddToDictionaryList(location, this);
			// 	}
			// }
		}

		if (locationSet) {
			if (locationSetOnce && !ignoreDoubleLocationWarnings) {
				Debug.LogWarning("Story.FinishReq location/job/chara set twice, use OR clause instead " + this);
			}
			locationSetOnce = true;
		}
	}

	/// <summary>
	/// Recursively add labelled choices to choicesByID.
	/// Called during FinishLoading.
	/// </summary>
	private void RegisterChoices(StoryChoice storyChoice) {
		allChoices.Add(storyChoice);
		if (!string.IsNullOrEmpty(storyChoice.choiceID)) {
			string choiceID = storyChoice.choiceID.Trim().ToLower();
			// don't bitch about overrides; snippets might have been added to this dict earlier
			if (choicesByID.ContainsKey(choiceID) && !choiceID.StartsWith("snippet_")) {
				Debug.LogWarning("Story.RegisterChoices overriding choice label " + choiceID 
					+ ", orig: " + choicesByID[choiceID]
					+ ", new: "+ storyChoice + ", story: " + this);
			}
			choicesByID[choiceID] = storyChoice;
		}
		foreach (StoryChoice subChoice in storyChoice.choices) {
			RegisterChoices(subChoice);
		}
	}

	/// <summary>
	/// Called once at end of DataLoader.
	/// Some validation must wait until all stories are loaded.
	/// </summary>
	public static void ValidateAllStories() {
		// // grab all memories from stories, also special ones from jobs and locations
		// allMemories = new List<string>();
		// allGroundhogs = new List<string>();
		// foreach (Skill skill in Skill.allSkills) {
		// 	// skill perk level reached
		// 	allMemories.AddSafe(Fort.memSkillPerkPrefix + skill.skillID + 1);
		// 	allMemories.AddSafe(Fort.memSkillPerkPrefix + skill.skillID + 2);
		// 	allMemories.AddSafe(Fort.memSkillPerkPrefix + skill.skillID + 3);
		// }
		// foreach (Job job in Job.allJobs) {
		// 	// job is available
		// 	allMemories.AddSafe(Fort.memJobUnlockPrefix + job.jobID);
		// 	// incremented when you work a job
		// 	allMemories.AddSafe(Fort.memJobWorkedPrefix + job.jobID);
		// }
		// foreach (Location location in Location.allLocations) {
		// 	// incremented when you work a job in this location (same prefix as working the job itself)
		// 	allMemories.AddSafe(Fort.memLocWorkedPrefix + location.locationID);
		// }
		// foreach (Chara chara in Chara.allCharas) {
		// 	// set to current month when you see or speak to a chara
		// 	allMemories.AddSafe(Fort.memCharaMetPrefix + chara.charaID);
		// }
		//
		// // StoryCalls.endGame() sets various mems and hogs
		// foreach (Ending ending in Ending.allEndings) {
		// 	allMemories.AddSafe(Fort.memEndingPrefix + ending.endingID);
		// 	allGroundhogs.AddSafe(Fort.memEndingPrefix + ending.endingID);
		// }
		// allMemories.AddSafe(Fort.memEndingPrefix + "default");
		// allGroundhogs.AddSafe(Fort.memEndingPrefix + "default");
		// allGroundhogs.AddSafe(Fort.hogLastEnding);
		// allGroundhogs.AddSafe(Fort.hogNumLives);
		//
		// // grab all memories from all stories
		// foreach (Story story in allStories) {
		// 	foreach (StoryChoice choice in story.allChoices) {
		// 		foreach (StorySet set in choice.sets) {
		// 			if (set.type == StorySetType.memory) {
		// 				allMemories.AddSafe(set.stringID);
		// 				story.debugSetMemories.AddSafe(set.stringID);
		// 			} else if (set.type == StorySetType.groundhog) {
		// 				allGroundhogs.AddSafe(set.stringID);
		// 			}
		// 			if (set.elseSet != null) {
		// 				if (set.elseSet.type == StorySetType.memory) {
		// 					allMemories.AddSafe(set.elseSet.stringID);
		// 					story.debugSetMemories.AddSafe(set.elseSet.stringID);
		// 				} else if (set.elseSet.type == StorySetType.groundhog) {
		// 					allGroundhogs.AddSafe(set.elseSet.stringID);
		// 				}
		// 			}
		// 		}
		// 	}
		// }
		//
		// // need a dummy princess for validation
		// bool createdDummyPrincess = false;
		// if (!Fort.isLoaded) {
		// 	createdDummyPrincess = true;
		// 	Fort.CreateBlankFort(true);
		// }
		//
		// foreach (Story story in allStories) {
		// 	story.ValidateAndFinish();
		// }
		//
		// validatingStory = null;
		// Fort.SetResult(null); // clear the dummy result
		//
		// // clear the dummy princess created for validation
		// if (createdDummyPrincess) {
		// 	Fort.isLoaded = false;
		// }
	}

	/// <summary>
	/// Called once at end of DataLoader.
	/// Validation must be performed after all stories are loaded.
	/// </summary>
	public void ValidateAndFinish() {
		validatingStory = this;

		if (entryChoice == null) {
			Debug.LogError("Story with null entryChoice " + this);
			return;
		}

		// grab all storyvars for this one story
		allVars = new List<string>();
		List<StorySet> badPageBreakJumps = new List<StorySet>();
		foreach (StoryChoice choice in allChoices) {
			foreach (StorySet set in choice.sets) {
				if (set.type == StorySetType.storyvar) {
					allVars.Add(set.stringID);
				} else if (set.type == StorySetType.story) {
					// ~set story_gift_rex may use a fake storyID to mark time
					allSetStoryIDs.Add(set.stringID);
				} else if (set.isJump && choice.isContinue && choice.resultText == null) {
					// jumps from no text TO no text need line break before them removed
					StoryChoice jumpStoryChoice = GetChoiceById(set.stringID, false);
					if (jumpStoryChoice != null && jumpStoryChoice.resultText == null) {
						badPageBreakJumps.Add(set);
					}
				}
			}
		}

		// retroactively remove page breaks from regular jumps to choices with no text
		foreach (StorySet jump in badPageBreakJumps) {
			// Debug.Log("StorySet jump from no text TO no text (" + jump.stringID + "), " 
			// 	+ storyID + ", " + jump.choice.path + ", " + this.storyID);
			StoryParser.RemovePageBreak(jump.StoryChoice);
		}

		// need a dummy result for the story, as if it is being run now
		Result dummyResult = new Result();
		dummyResult.story = this;
		StoryManager.SetResult(dummyResult);

		foreach (StoryChoice choice in allChoices) {
			// // allocates mem and takes time, so skip this step in prod
			// // validate text for broken inline print statements, inline if statements etc
			// dummyResult.choice = choice;
			// string resultText = choice.GetProcessedResultText(dummyResult);

			// jumps must have a valid stringID
			foreach (StorySet set in choice.sets) {
				if (!set.ValidateAndFinish()) {
					Debug.LogWarning("Story contains invalid set " + storyID + " - " + set);
				}
			}

			// requirements must have a valid story stringID
			foreach (StoryReq req in choice.requirements) {
				req.ValidateAndFinish();
			}
		}

		// clear the dummy result
		StoryManager.SetResult(null);
	}

	/// <summary>
	/// Called before the job battle. Return only average or high priority stories with the right job, or null.
	/// If HighPriorityOnly, we already had one high priority and can only show another.
	/// </summary>
	public static Story PickBeforeJobStory(Result result, bool highPriorityOnly = false) {
		/*
		if (result.job == null) return null;
		
		// not even high priority job events while mourning, except rebuild/mourn jobs
		// if (Status.inMourning && (result.job == null || !result.job.isRebuilding)) return null;

		// high priority job (always fires first)
		Story story = PickStoryFromDict(result.job, storiesByJobHigh, result);
		if (story != null) return story;

		// high priority location (fires after high priority job if both exist)
		story = PickStoryFromDict(result.location, storiesByLocationHigh, result);
		if (story != null) return story;

		// replace regular job events with geoponicsGlowRepeat etc during glow
		if (GameManager.season == Season.glow) {
			string glowStoryID = result.location.locationID.ToLower().Trim() + "glowrepeat";
			Story glowStory = Story.FromID(glowStoryID);
			if (glowStory != null) {
				// already saw the glow story
				if (highPriorityOnly) return null;
				return glowStory;
			}
			// Debug.LogWarning("Story.PickBeforeJobStory failed to find glow story for location " + result.location);
			// fall through to regular job story
		}

		// only if we haven't run one event yet
		if (!highPriorityOnly) {
			
			// alternate job events with location events
			
			int timesWorkedLoc = GameManager.GetMemoryInt(GameManager.memLocWorkedPrefix + result.location.locationID);
			
			if (timesWorkedLoc % 2 == 0) {
				// look for a job event, then fallback to repeat
				story = PickStoryFromDict(result.job, storiesByJobReg, result);
				if (story != null) return story;
				if (GameManager.age >= 19) {
					// in the final year of the game increase the frequency of events by checking the other type too
					story = PickStoryFromDict(result.location, storiesByLocationReg, result);
					if (story != null) return story;
				}
			} else {
				// look for a location event, then fallback to repeat
				story = PickStoryFromDict(result.location, storiesByLocationReg, result);
				if (story != null) return story;
				if (GameManager.age >= 19) {
					// in the final year of the game increase the frequency of events by checking the other type too
					story = PickStoryFromDict(result.job, storiesByJobReg, result);
					if (story != null) return story;
				}
			}

			// fallback to low priority repeating job event eg sportsballRepeat
			story = PickStoryFromDict(result.job, storiesByJobLow, result);
			if (story != null) return story;
			
			// // avg priority job (fires every 3 months)
			// // int currentMonth = Princess.GetMemoryInt(Princess.memJobWorkedPrefix + result.job.jobID);
			// int currentMonth = StoryManager.currentGameMonth;
			// int nextStory = Princess.GetMemoryInt(Princess.memJobStoryPrefix + result.job.jobID);
			// if (currentMonth >= nextStory) {
			// 	story = PickStoryFromDict(result.job, storiesByJobReg, result);
			// 	if (story != null) {
			// 		// events closer together near the end of the game when pacing is less important than completion
			// 		int eventMonthDelay = Princess.age == 19 ? 1 : Princess.age == 18 ? 2 : storyPerTimesWorkedJob;
			// 		Princess.SetMemory(Princess.memJobStoryPrefix + result.job.jobID, currentMonth + eventMonthDelay);
			// 		return story;
			// 	}
			// }
		}
		*/
		return null;
	}

	/// <summary>
	/// Called after the job battle. Try to find a location or priority story.
	/// If HighPriorityOnly, we already had one story (either before battle or after) and can only show high priority.
	/// After returning from exploring, highPriorityOnly will be false so we can run sneakEnd
	/// If nothing else happens today, return a low priority job story.
	/// </summary>
	public static Story PickAfterJobStory(Result result, bool highPriorityOnly = false) {
		/*
		if (result.job == null || result.location == null) {
			Debug.LogError("Story.PickAfterJobStory needs to know the job + location!");
			return null;
		}

		// if (Status.inMourning) {
		// 	// only priority events while mourning, not even high priority location/all
		// 	return PickStoryFromDict(Location.priority, storiesByLocationPriority, result);
		// }

		// high priority all-location (fires before high priority location stories)
		Story story = PickStoryFromDict(Location.all, storiesByLocationHigh, result);
		if (story != null) return story;
		
		// priority location stories (attack/vertumnalia/birthday will be last)
		story = PickStoryFromDict(Location.priority, storiesByLocationPriority, result);
		if (story != null) return story;
		
		if (!highPriorityOnly) {
			// all-location events last, if no other regular or high priority events happened (including job ones)
			story = PickStoryFromDict(Location.all, storiesByLocationReg, result);
			if (story != null) return story;
		}
		*/

		return null;
	}

	
	// /// <summary>
	// /// Pick a story for a boss, miniboss, creche etc.
	// /// High priority happen in order. Regular and low priority are randomized based on month of game.
	// /// </summary>
	// public static Story PickMapSpotStory(Result result, int dateSeed, bool highPriorityOnly = false, bool noFallback = false) {
	// 	if (result.mapSpot == null && result.mapSpotType == MapSpotType.none) return null;
	// 	MapSpotType type = (result.mapSpotType == MapSpotType.none) ? result.mapSpot.type : result.mapSpotType;
	//
	// 	// high priority picked first in order
	// 	Story story = PickStoryFromDict(type, storiesByMapSpotHigh, result);
	// 	if (story != null) return story;
	//
	// 	// average priority events are scrambled up so eg all dys events don't happen in a row
	// 	if (!highPriorityOnly) {
	// 		string randomSeedReg = "mapSpotReg" + dateSeed;
	// 		story = PickStoryFromDict(type, storiesByMapSpotReg, result, randomSeedReg);
	// 		if (story != null) return story;
	//
	// 		// low priority (usually repeating) events
	// 		string randomSeedLow = "mapSpotLow" + dateSeed;
	// 		story = PickStoryFromDict(type, storiesByMapSpotLow, result, randomSeedLow);
	// 		if (story != null) return story;
	// 	}
	//
	// 	return null;
	// }

	// public static Story PickStoryFromDict<TKey>(TKey key, IDictionary<TKey, List<Story>> dict, Result result, string randomSeed = null) {
	// 	if (key == null) return null;
	// 	if (!dict.ContainsKey(key)) return null;
	// 	List<Story> stories = dict.GetList(key);
	// 	if (randomSeed != null) {
	// 		stories = stories.CloneSafe();
	// 		stories.RandomizeInPlace(randomSeed);
	// 	}
	// 	foreach (Story story in stories) {
	// 		if (!story.CanExecute(result)) {
	// 			continue;
	// 		}
	// 		return story;
	// 	}
	// 	return null;
	// }

	/// <summary>
	/// May return null for invalid ID.
	/// </summary>
	public static Story FromID(string storyID) {
		if (storyID == null) return null;
		storyID = storyID.Trim().ToLower();
		if (storiesByID.ContainsKey(storyID)) {
			return storiesByID[storyID];
		}
		return null;
	}

	/// <summary>
	/// For testing.
	/// </summary>
	public string ToStringVerbose() {
		string text = "Story [id=" + (storyIDCamelcase ?? "NO_ID") + "\n";
		foreach (StoryChoice choice in entryChoice.choices) {
			text += ChoiceToStringRecursive(choice, 1);
		}
		text += "]";
		return text;
	}

	/// <summary>
	/// For testing.
	/// * ButtonText (buttonID) - first 50 charas
	///     ** ButtonText (buttonID) - first 50 charas
	///     ** ButtonText (buttonID) (jump to buttonID) - first 50 charas
	///         *** ButtonText (buttonID) - first 50 charas
	/// </summary>
	private string ChoiceToStringRecursive(StoryChoice storyChoice, int level) {
		if (storyChoice == null) return "NULL_CHOICE\n";
		string text = "";
		for (int i = 1; i < level; i++) {
			text += "\t";
		}
		for (int i = 0; i < level; i++) {
			text += "*";
		}

		text += storyChoice.buttonText + " (" + storyChoice.choiceID + ") ";
		foreach (StorySet jump in storyChoice.GetJumps()) {
			text += "(jump to " + jump.stringID + ") ";
		}
		text += "- " + storyChoice.resultText + "\n";

		foreach (StoryChoice subchoice in storyChoice.choices) {
			text += ChoiceToStringRecursive(subchoice, level + 1);
		}

		return text;
	}

	/// <summary>
	/// Called when reparsing stories on the fly for debugging.
	/// </summary>
	public static void ClearAllStories() {
	
		allStories = new List<Story>();
		storiesByID = new Dictionary<string, Story>();
		// storiesByLocationPriority = new Dictionary<Location, List<Story>>();
		//
		// storiesByJobHigh = new Dictionary<Job, List<Story>>();
		// storiesByLocationHigh = new Dictionary<Location, List<Story>>();
		// storiesByCharaHigh = new Dictionary<Chara, List<Story>>();
		// storiesByMapSpotHigh = new Dictionary<MapSpotType, List<Story>>();
		//
		// storiesByJobReg = new Dictionary<Job, List<Story>>();
		// storiesByLocationReg = new Dictionary<Location, List<Story>>();
		// storiesByCharaReg = new Dictionary<Chara, List<Story>>();
		// storiesByMapSpotReg = new Dictionary<MapSpotType, List<Story>>();
		//
		// storiesByJobLow = new Dictionary<Job, List<Story>>();
		// storiesByLocationLow = new Dictionary<Location, List<Story>>();
		// storiesByCharaLow = new Dictionary<Chara, List<Story>>();
		// storiesByMapSpotLow = new Dictionary<MapSpotType, List<Story>>();

		snippetsByID = new Dictionary<string, Story>();
		allMemories = new List<string>();
		allGroundhogs = new List<string>();
		allSetStoryIDs = new List<string>();
		validatingStory = null;
	}

	public override string ToString() {
		return storyIDCamelcase ?? "UNNAMED_STORY";
	}
}
