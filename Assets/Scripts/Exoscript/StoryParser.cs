using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Statically parse homebrew Exoscript scripting language for dialog events.
/// Top-level story parsing line by line, including comments, choice depth, page breaks, etc.
/// </summary>
public static class StoryParser {
		
	// button font will shrink when it's too big, but validation warns when it reaches this many characters
	private const int maxWarnButtonLength = 45; // after about 40~45 font starts to shrink

	private static int wordCount = 0;
	private static readonly Char[] invalidChars = new[] {'“', '”', '’', '\t', '—'};
	private static readonly StringBuilder resultTextStringBuilder = new StringBuilder();
	
	public static void LoadStories() {
		wordCount = 0;
		
		// first try loading from external StreamingAssets/Stories folder for overrides
		string storiesPath = FileManager.storiesPath;
		string[] filenames = new string[0];
		if (FileManager.DirectoryExists(storiesPath)) {
			filenames = FileManager.GetFilenames(storiesPath, FileManager.parserStoryFileExtension);
		}
		if (filenames.Length == 0) {
			Debug.LogError("ParserStory failed to find StreamingAssets: " + storiesPath);
			return;
		}

		foreach (string filename in filenames) {
			string compiledFileName = Path.ChangeExtension(filename, FileManager.compiledStoryFileExtension);
			bool loadCompiled = false;
			// In editor or on standalone, load whichever file is more recent
			DateTime lastTextStoryWriteTime = File.GetLastWriteTime(Path.Combine(storiesPath, filename));
			DateTime lastCompiledStoryWriteTime = File.GetLastWriteTime(Path.Combine(storiesPath, compiledFileName));
			loadCompiled = lastCompiledStoryWriteTime > lastTextStoryWriteTime;

			if (loadCompiled) {
				byte[] data = FileManager.LoadFileBytes(compiledFileName, storiesPath);
				using (MemoryStream memoryStream = new MemoryStream(data))
					using (BinaryReader reader = new BinaryReader(memoryStream)) {
						int templateCount = reader.ReadInt32();
						for (int i = 0; i < templateCount; ++i) {
							StoryTemplate template = StoryTemplate.Deserialize(reader);
							Story story = new Story(template);
							story.FinishLoading();
						}
					}
			} else {
				if (Application.isPlaying) {
					Debug.LogWarning($"Loading uncompiled story file: [{filename}]. Stories should be compiled for faster loading.");
				} else {
					Debug.Log($"Compiling story file [{filename}]");
				}

				List<StoryTemplate> storyTemplates = LoadStoriesFile(filename, storiesPath);
				// Save the compiled stories for faster loading next time
				using (MemoryStream memoryStream = new MemoryStream()) {
					using (BinaryWriter writer = new BinaryWriter(memoryStream)) {
						writer.Write(storyTemplates.Count);
						foreach (StoryTemplate template in storyTemplates) {
							StoryTemplate.Serialize(writer, template);
						}

						FileManager.SaveFile(memoryStream.ToArray(), compiledFileName, storiesPath);
					}
				}
			}
		}

		if (GameSettings.validateStories) {
			Debug.Log("Loaded " + Story.allStories.Count + " stories from " + filenames.Length + " files.");
			Debug.Log("Total word count: " + wordCount);
			DebugLogStoryStats();
			// DebugMultiExploreKeep();
			// DebugLogDoneButtons();
			// DebugLogStories();
			// DebugListSwearStories();
			// DebugListCardStories();
			// DebugListDemoStories();
			// DebugCountBarks();
		}
	}

	/// <summary>
	/// For debugging print how many stories in each job etc.
	/// </summary>
	private static void DebugLogStoryStats() {
		// int numAllStories = 0;
		// numAllStories += Story.storiesByLocationHigh.GetList(Location.all).Count;
		// numAllStories += Story.storiesByLocationReg.GetList(Location.all).Count;
		// numAllStories += Story.storiesByLocationLow.GetList(Location.all).Count;
		// Debug.Log("\tlocation - " + Location.all.locationName + " - " + numAllStories);
		//
		// Debug.Log("\tpriority - priority - " + Story.storiesByLocationPriority.GetList(Location.priority).Count);
		//
		// foreach (Chara chara in Chara.allCharas) {
		// 	if (!chara.canLove) continue;
		// 	int numStories = 0;
		// 	numStories += Story.storiesByCharaHigh.GetList(chara).Count;
		// 	numStories += Story.storiesByCharaReg.GetList(chara).Count;
		// 	numStories += Story.storiesByCharaLow.GetList(chara).Count;
		// 	Debug.Log("\tchara - " + chara.nickname + " - " + numStories);
		// }
		//
		// foreach (Location location in Location.allLocations) {
		// 	int numStories = 0;
		// 	numStories += Story.storiesByLocationHigh.GetList(location).Count;
		// 	numStories += Story.storiesByLocationReg.GetList(location).Count;
		// 	numStories += Story.storiesByLocationLow.GetList(location).Count;
		// 	Debug.Log("\tlocation - " + location.locationName + " - " + numStories);
		// }
		//
		// List<Story> jobStories = new List<Story>();
		// foreach (Job job in Job.allJobs) {
		// 	jobStories.Clear();
		// 	jobStories.ConcatSafe(Story.storiesByJobHigh.GetList(job), true);
		// 	jobStories.ConcatSafe(Story.storiesByJobReg.GetList(job), true);
		// 	jobStories.ConcatSafe(Story.storiesByJobLow.GetList(job), true);
		//
		// 	Debug.Log("\tjob - " + job.jobName + " - " + jobStories.Count());
		//
		// 	// if (job.isExpedition) {
		// 	// 	string expedition = "";
		// 	// 	foreach (MapSpotType type in MapSpot.exploreTypes) {
		// 	// 		int numMapSpotStories = jobStories.Count(story => story.mapSpotType == type);
		// 	// 		expedition += type + ": "+ numMapSpotStories + "\t";
		// 	// 	}
		// 	// 	Debug.Log("\t\t" + expedition);
		// 	// }
		// 	//
		// 	// if (job.jobID == "explorenearby") {
		// 	// 	Job sneak = Job.FromID("sneak");
		// 	// 	jobStories.ConcatSafe(Story.storiesByJobHigh.GetList(sneak), true);
		// 	// 	jobStories.ConcatSafe(Story.storiesByJobReg.GetList(sneak), true);
		// 	// 	jobStories.ConcatSafe(Story.storiesByJobLow.GetList(sneak), true);
		// 	// 	Debug.Log("\tjob - sneak OR exploreNearby - " + jobStories.Count());
		// 	// 	string expedition = "";
		// 	// 	foreach (MapSpotType type in MapSpot.exploreTypes) {
		// 	// 		int numMapSpotStories = jobStories.Count(story => story.mapSpotType == type);
		// 	// 		expedition += type + ": "+ numMapSpotStories + "\t";
		// 	// 	}
		// 	// 	Debug.Log("\t\t" + expedition);
		// 	// }
		// }
		//
		// // count and output exploration job stats
		// // Debug.Log("\n");
		// // foreach (Job job in Job.allJobs) {
		// // 	if (job.location.locationID != "expeditions" || job.jobID == "relaxpet") continue;
		// //
		// // 	// List<Story> storiesForJob = DebugAllStoriesForMapJob(job);
		// //
		// // 	int totalExclusive = 0;
		// // 	int semiExclusive = 0;
		// // 	foreach (Story story in storiesForJob) {
		// // 		if (story.DebugStoryJobs().Count == 1) totalExclusive++;
		// // 		if (story.DebugStoryJobs().Count == 2) semiExclusive++;
		// // 		if (story.DebugStoryJobs().Count == 3) semiExclusive++;
		// // 	}			
		// // 	
		// // 	Debug.Log("\tjob - " + job.jobName + " (" + storiesForJob.Count 
		// // 		+ ") (Exclusive " + totalExclusive + ", Semi-exclusive " + semiExclusive + ")");
		// // }
	}

	// /// <summary>
	// /// For debugging print how many stories in each job etc.
	// /// </summary>
	// private static void DebugLogStories() {
	// 	
	// 	int storyInt = 1;
	// 	foreach (Story story in Story.allStories) {
	// 		string text = storyInt++ + "\t" + story.storyIDCamelcase + "\t";
	// 	
	// 		// if (story.location?.locationID == "expeditions") {
	// 		// 	string mapSpotType = story.mapSpotType.ToString();
	// 		// 	if (story.mapSpotType == MapSpotType.bottleneck) mapSpotType = "bott";
	// 		// 	if (story.mapSpotType == MapSpotType.miniboss) mapSpotType = "mini";
	// 		// 	Season storySeason = story.DebugGetSeason();
	// 		// 	if (storySeason != null) {
	// 		// 		mapSpotType += " " + storySeason.seasonID;
	// 		// 	}
	// 		// 	if (story.GetRepeatReq() != null) {
	// 		// 		mapSpotType += " repeat";
	// 		// 	}
	// 		// 	text += mapSpotType + "\t";
	// 		// } else {
	// 		// 	text += ((story.location == null) ? "" : story.location.locationID) + "\t";
	// 		// }
	// 		
	// 		if (story.DebugStoryJobs().Count < 10) {
	// 			foreach (Job job in story.DebugStoryJobs()) {
	// 				text += job.jobID + ", ";
	// 			}
	// 		}
	// 		text = text.RemoveEnding(", ") + "\t";
	// 		
	// 		foreach (Chara chara in story.charas) {
	// 			text += chara.charaID + ", ";
	// 		}
	// 		text = text.RemoveEnding(", ") + "\t";
	// 	
	// 		List<int> ages = story.DebugGetAges();
	// 		if (ages.Count == 0) {
	// 			text += "n/a\t"; // n/a
	// 		} else if (ages.Count == 1) {
	// 			text += ages[0] + "\t"; // 14
	// 		} else {
	// 			int firstAge = ages[0];
	// 			string ageString = firstAge + "+"; // 13+
	// 			for (int age = firstAge; age < 20; age++) {
	// 				if (!ages.Contains(age)) {
	// 					int lastAge = ages[ages.Count - 1]; 
	// 					if (lastAge == (age - 1)) {
	// 						ageString = firstAge + "-" + lastAge; // 13-15
	// 					} else {
	// 						ageString = ages.JoinSafe(", "); // 13, 14, 18
	// 					}
	// 					break;
	// 				}
	// 			}
	// 			text += ageString + "\t";
	// 		}
	// 		// if (story.DebugGetAges().Count < 10) {
	// 		// 	foreach (int age in story.DebugGetAges()) {
	// 		// 		text += age + ", ";
	// 		// 	}
	// 		// }
	// 		// text = text.RemoveEnding(", ") + "\t";
	// 	
	// 		// if (story.DebugGetSeasons().Count < 5) {
	// 		// 	foreach (Season season in story.DebugGetSeasons()) {
	// 		// 		text += season.seasonID + ", ";
	// 		// 	}
	// 		// }
	// 		// text = text.RemoveEnding(", ") + "\t";
	// 	
	// 		text += story.debugSetMemories.JoinSafe(", ") + "\t";
	// 		
	// 		Debug.Log(text);
	// 	}
	// }

	public static List<StoryTemplate> LoadStoriesFile(string filename, string storiesPath) {
		var storyTemplates = new List<StoryTemplate>();
		string[] lines = FileManager.LoadFileLines(filename, storiesPath, true);
		if (lines.Length < 5) {
			Debug.LogError("Invalid events rawData lines " + lines.Length + ", " + filename);
			return storyTemplates;
		}

		if (lines[0].Trim() == "~disabled"){
			//Debug.Log("Ignoring stories file because ~disabled " + filename);
			return storyTemplates; 
		}

		Story story = new Story();
		StoryChoice storyChoice = story.entryChoice;
		resultTextStringBuilder.Clear();
		bool inComment = false;
		bool inText = false;

		for (int i = 0; i < lines.Length; i++) {
			// remove tabs and whitespace
			string line = lines[i].Trim();

			// ignore blank lines or add them to the text
			if (line.IsNullOrEmptyOrWhitespace()) {
				// if (inText && !inComment && !choice.resultText.IsEmpty()) {
				if (inText && !inComment && resultTextStringBuilder.Length > 0) {
					// treat empty lines as line breaks in text - \n\n\n wil be replaced with \n later
					// choice.resultText += "\n";
					resultTextStringBuilder.Append("\n");
				}
				// no further parsing for broken line
				continue;
			}

			bool wasInText = inText;
			inText = false;

			// ignore multi-line comments
			if (inComment) {
				if (line.Contains("*/")) {
					inComment = false;
				}
				inText = wasInText;
				continue;
			}
			
			// remove comments from the end of lines
			if (!line.StartsWith("//") && line.Contains("//")) {
				line = line.Substring(0, line.IndexOf("//", StringComparison.Ordinal)).Trim();
			}
			
			// check for invalid characters (ContainsAny is expensive so skip in prod)
			if (GameSettings.validateStories && !inComment && !line.StartsWith("//")) {
				// warn and replace smart quotes etc
				
				if (line.ContainsAny(invalidChars)) {
					Debug.LogWarning("Line contains invalid char " + line + ", " + story);
					line = line.ReplaceAll("“", "\"");
					line = line.ReplaceAll("”", "\"");
					line = line.ReplaceAll("’", "'");
					line = line.ReplaceAll("\t", " ");
					line = line.ReplaceAll("—", "-");
				}

				if (!line.StartsWith("~") && line.Contains("~")) {
					Debug.LogWarning("Line contains invalid char " + line + ", " + story);
					line = line.ReplaceAll("~", "-");
				}
			}
			
			char firstChar = line[0];
			switch (firstChar) {
				// ignore single-line comments
				case '#':
					inText = wasInText;
					continue;

				// new storyID or choice label
				case '=':
					if (line.StartsWith("===")) {
						// eg "======= shovelIntro ====" -> "shovelIntro"
						string storyID = line.Trim('=').Trim();
						if (!string.IsNullOrEmpty(story.storyID)) {
							storyChoice = FinishChoice(storyChoice);
							storyTemplates.Add(story.ToTemplate());
							story.FinishLoading();
							story = new Story();
							storyChoice = story.entryChoice;
						}
						story.SetID(storyID);

					} else {
						// eg "= firstChoice" -> "firstChoice"
						string choiceID = line.Trim('=').Trim();
						if (!string.IsNullOrEmpty(storyChoice.choiceID)) {
							Debug.LogWarning("Choice already has a label! Overwriting " + storyChoice.choiceID + " with " + choiceID + ", " + story);
						}
						storyChoice.choiceID = choiceID;
						// moved to FinishChoice
						// if (choice.choiceID == "end") {
						// 	// Story.FinishLoading will further validate and add jumps to this from all deadend choices
						// 	// end choice starts blank with a page break
						// 	// it and its children will have no reqs, jumps, or sub choices other than page breaks
						// 	choice = AddPageBreak(choice, true);
						// }
					}
					continue;

				// requirement, setting, function
				case '~':
					if (line.StartsWith("~ ")) {
						// remove spaces between ~ set etc
						line = "~" + line.RemoveStart("~").Trim();
					}

					if (line.StartsWith("~if once") || line.StartsWith("~set once") || line.StartsWith("~once")) {
						if (line.EndsWith("once_today") || line.EndsWith("once_week") || line.EndsWith("once_month")) {
							// shortcut for
							// ~if story_oncerandomid12345 != 0
							// ~set story_oncerandomid12345
							// never happened (-1) or happened last month (1) or earlier (2+)
							StoryParserSet.AddOnce(storyChoice, true);
							
						} else if (line.EndsWith("once_ever")) {
								// shortcut for
								// ~if !story_oncerandomid12345
								// ~set story_oncerandomid12345
								// never happened (-1) or happened last month (1) or earlier (2+)
								StoryParserSet.AddOnce(storyChoice, false, true);
						} else {
							// shortcut for
							// ~if !var_randomid12345
							// ~set var_randomid12345
							StoryParserSet.AddOnce(storyChoice, false);
						}

					} else  if (line.StartsWith("~if")) {
						// ~if or ~ifd
						StoryReq req = StoryParserReq.ParseReq(line, story, storyChoice);
						if (req != null) {
							storyChoice.AddRequirement(req);
							if (story.entryChoice == storyChoice && req.showDisabled) {
								Debug.LogWarning("Entry choice can't use ~ifd, making ~if " + line + ", " + story);
								req.showDisabled = false;
							}
						}

					} else if (line.StartsWith("~set ") || line.StartsWith("~call ")) {
						StorySet set = StoryParserSet.ParseSet(line, story, storyChoice);
						if (set == null) continue;

						// if ((set.IsCall("battle") || set.IsCall("goHome") || set.IsCall("incrementMonth")) 
						// 	&& !choice.resultText.IsNullOrEmpty()) {
						if ((set.IsCall("battle") || set.IsCall("goHome") || set.IsCall("incrementMonth")) 
							&& resultTextStringBuilder.Length > 0) {
							// add a page break before battle or go home so text can be read (text after ignored)
							storyChoice = AddPageBreak(storyChoice);
							inText = false;
							if (set.IsCall("goHome")) {
								// "Done" if the choice will send you home
								storyChoice.buttonText = "Done";
							} else if (set.IsCall("battle")) {
								// "Challenge!" if the battle isn't in a choice with button text
								storyChoice.buttonText = "Challenge!";
							} else {
								storyChoice.buttonText = "Time Passes";
							}
						}
						
						storyChoice.AddSet(set);

					} else if (line.StartsWith("~setif ") || line.StartsWith("~callif ")) {
						StorySet set = StoryParserSet.ParseSetIf(line, story, storyChoice);
						if (set != null) {
							storyChoice.AddSet(set);
						}
						
					} else {
						Debug.LogWarning("Invalid story line " + line + ", " + story);
					}
					continue;

				// jump to another choice
				case '>':
					if (storyChoice.GetJumps().Any() && storyChoice.GetJumps()[0].requirement == null) {
						Debug.LogWarning("Choice already has a non-conditional jump " 
							+ storyChoice.GetJumps()[0] + ", ignoring new " + line + ", " + story);
						continue;
					}

					if (line.Trim('>').Trim('!').Trim().ToLower() == "end") {
						Debug.LogWarning("Choice has a jump to end label but will go there anyway, ignoring. " 
							+ line + ", " + story);
						continue;
					}

					StorySet jump = null;

					// ">" defaults to adding a page break, but ">!" or ">>" or ">>>" or ">if" prevents it
					// if (!line.StartsWith(">!") && !line.StartsWith(">>!") && !line.StartsWith(">>>!") 
					// if (!line.StartsWith(">!") && !line.StartsWith(">>") 
					// 	&& !line.Trim('>').Trim().StartsWith("if ") && !choice.resultText.IsNullOrEmpty()){
					if (!line.StartsWith(">!") && !line.StartsWith(">>") 
						&& !line.Trim('>').Trim().StartsWith("if ") && resultTextStringBuilder.Length > 0){
						// if it turns out that the jump choice has no text, this page break will be removed
						// retroactively during Story.Validate
						storyChoice = AddPageBreak(storyChoice);
						inText = false;
					}
					
					jump = StoryParserSet.ParseJump(story, line, storyChoice);

					if (jump == null) {
						Debug.LogWarning("Failed to parse jump " + line);
						continue;
					}
					
					// for if with no else, need a line break immediately after them
					if (jump.requirement != null && jump.elseSet == null) {
						// add page break choice.AddSet so it won't move jump down
						StoryChoice afterJumpStoryChoice = AddPageBreak(storyChoice); 
						inText = false;
						// jump is on the original choice, and else after the pagebreak
						storyChoice.AddSet(jump);
						storyChoice = afterJumpStoryChoice;
					} else {
						storyChoice.AddSet(jump);
					}
					
					continue;

				// new choiceText
				case '*':
					storyChoice = FinishChoice(storyChoice);
					StoryChoice newStoryChoice = ParseChoiceText(line, storyChoice);
					if (newStoryChoice != null) {
						storyChoice = newStoryChoice;
					}
					continue;

				default:
					// ignore single-line comments
					if (line.StartsWith("//")) {
						inText = wasInText;
						continue;
					}

					// ignore multi-line comments
					if (line.StartsWith("/*")) {
						if (!line.Contains("*/")) {
							inComment = true;
						}
						inText = wasInText;
						continue;
					}

					// page breaks in text break up the text into multiple choices
					if (line.Trim() ==  "-") {
						storyChoice = AddPageBreak(storyChoice);
						continue;
					}

					// resultText
					inText = true;
					
					if (resultTextStringBuilder.Length == 0) {
						resultTextStringBuilder.Append(line);
					} else {
						if (!wasInText) {
							Debug.LogWarning("Choice has previous resultText " + resultTextStringBuilder
								+ ", appending " + line + ", " + story);
						}
						resultTextStringBuilder.Append(line);
					}
					
					if (GameSettings.validateStories) wordCount += line.Split(' ').Length;
					
					if ((line.StartsWith("[if") || line.StartsWith("[else")) && line.EndsWith("]") && !line.EndsWith("[end]")) {
						// no line break at the end of [if] or [else] lines but all other dynamic stuff ok
					} else {
						resultTextStringBuilder.Append("\n");
					}

					break;
			}
		}

		if (string.IsNullOrEmpty(story.storyID)) {
			Debug.LogWarning("No stories found in file or last one has no storyID: " + filename);
		} else {
			// finish the final story
			storyChoice = FinishChoice(storyChoice);
			storyTemplates.Add(story.ToTemplate());
			story.FinishLoading();
		}

		return storyTemplates;
	}

	/// <summary>
	/// When a "-" is reached, create a new "..." choice under the current one and move jumps down to it.
	/// Return the (possibly new) child choice.
	/// </summary>
	public static StoryChoice AddPageBreak(StoryChoice storyChoice, bool before = false) {
		// if (!choice.resultText.IsNullOrEmpty()) {
		// 	// expected to be in the middle of StringBuilding the resultText, it should be null
		// 	Debug.LogWarning("ParserStory.AddPageBreak with non-null resultText " + choice);
		// }
		
		// if (!before && choice.resultText.IsEmpty()) {
		if (!before && resultTextStringBuilder.Length == 0) {
			if (storyChoice.hasJump) {
				// an option whose only contents are a jump does not need an extra page break before the jump
				return storyChoice;
			} else if (storyChoice.hasIncrementMonth) {
				// other things might need to be alone between page breaks, like incrementMonth
			} else {
				Debug.LogWarning("ParserStory.AddPageBreak with no text, jump, or incrementMonth " + storyChoice.story);
			}
		}

		if (!before) {
			// add the break AFTER the text content in choice (normal usage)
			StoryChoice nextStoryChoice = new StoryChoice(storyChoice.story);
			nextStoryChoice.buttonText = "...";
			nextStoryChoice.level = storyChoice.level; // considered same level eg ** = level 2
			storyChoice.AddChoice(nextStoryChoice);
			nextStoryChoice.parent = storyChoice;

			// jumps shuffle down to the bottom, other ifs and sets and label stay at the top
			foreach (StorySet prevJump in storyChoice.GetJumps()) {
				storyChoice.RemoveSet(prevJump);
				nextStoryChoice.AddSet(prevJump);
			}

			FinishChoice(storyChoice);
			return nextStoryChoice;
			
		} else {
			// add a blank page break BEFORE the text content in choice (used for =end)
			
			StoryChoice prevStoryChoice = new StoryChoice(storyChoice.story);
			prevStoryChoice.choiceID = storyChoice.choiceID;
			prevStoryChoice.buttonText = storyChoice.buttonText;
			prevStoryChoice.level = storyChoice.level; // considered same level eg ** = level 2
			prevStoryChoice.AddChoice(storyChoice);
			prevStoryChoice.parent = storyChoice.parent;
			
			storyChoice.parent?.RemoveChoice(storyChoice);
			storyChoice.parent?.AddChoice(prevStoryChoice);
			if (storyChoice.story.entryChoice == storyChoice) {
				storyChoice.story.entryChoice = prevStoryChoice;
			}

			storyChoice.choiceID = null;
			storyChoice.buttonText = "...";
			storyChoice.parent = prevStoryChoice;
			
			// move ifs up, but keep sets, jumps and text down
			foreach (StoryReq req in storyChoice.requirements) {
				storyChoice.RemoveRequirement(req);
				prevStoryChoice.AddRequirement(req);
			}

			// avoid looping back here for =end
			FinishChoice(prevStoryChoice, true);
			return storyChoice;
		}
	}

	/// <summary>
	/// Retroactively remove page breaks from regular jumps to choices with no text.
	/// Should only be called on Continue choices.
	/// </summary>
	public static StoryChoice RemovePageBreak(StoryChoice child) {
		if (child.parent == null) {
			Debug.LogError("ParserStory.RemovePageBreak child has no parent? " + child + ", " + child.story.storyID);
			return child;
		}
		if (child.parent.choices.Count != 1) {
			Debug.LogError("ParserStory.RemovePageBreak parent has multiple choices " + child + ", " + child.story.storyID);
			return child;
		}
		StoryChoice parent = child.parent;
		
		// move everything from child to parent
		foreach (StoryReq req in child.requirements) {
			Debug.LogError("ParserStory.RemovePageBreak child has requirement? " + req + ", " + child.story.storyID);
			parent.AddRequirement(req);
		}
		foreach (StorySet set in child.sets) {
			parent.AddSet(set);
		}
		parent.SetResultText(parent.resultText + child.resultText);
		// parent.resultText += child.resultText;
		
		// clear connection from parent to child
		parent.choices.Clear();
		// connect to grandchildren instead
		foreach (StoryChoice choice in child.choices) {
			parent.AddChoice(choice);
		}
		// child is no more
		child.story.allChoices.Remove(child);
		if (child.story.choicesByID.GetSafe(child.choiceID) == child) {
			Debug.LogWarning("ParserStory.RemovePageBreak removed label " + child.choiceID + ", " + child.story);
			child.story.choicesByID.Remove(child.choiceID);
		}
		return parent;
	}

	// public static Choice RemovePageBreak(Choice child) {
	// 	if (child.parent == null) {
	// 		Debug.LogError("ParserStory.RemovePageBreak child has no parent? " + child + ", " + child.story.storyID);
	// 		return child;
	// 	}
	// 	if (child.parent.choices.Count != 1) {
	// 		Debug.LogError("ParserStory.RemovePageBreak parent has multiple choices " + child + ", " + child.story.storyID);
	// 		return child;
	// 	}
	// 	Choice parent = child.parent;
	// 	
	// 	// move everything from child to parent
	// 	foreach (StoryReq req in child.requirements) {
	// 		Debug.LogError("ParserStory.RemovePageBreak child has requirement? " + req + ", " + child.story.storyID);
	// 		parent.AddRequirement(req);
	// 	}
	// 	foreach (StorySet set in child.sets) {
	// 		parent.AddSet(set);
	// 	}
	// 	parent.resultText += child.resultText;
	// 	
	// 	// clear connection from parent to child
	// 	parent.choices.Clear();
	// 	// connect to grandchildren instead
	// 	foreach (Choice choice in child.choices) {
	// 		parent.AddChoice(choice);
	// 	}
	// 	// child is no more
	// 	child.story.allChoices.RemoveSafe(child);
	// 	return parent;
	// }

	/// <summary>
	/// Create a new choice based on the choiceText line starting with one or more stars (***)
	/// </summary>
	private static StoryChoice ParseChoiceText(string line, StoryChoice prevStoryChoice) {
		//try {
		StoryChoice storyChoice = new StoryChoice(prevStoryChoice.story);

		// * = 1st level, ** = 2, *** = 3
		storyChoice.level = line.Length - line.Trim('*').Length;

		if (storyChoice.level >= prevStoryChoice.level + 1) {
			// * prevChoice
			// ** choice
			storyChoice.parent = prevStoryChoice;
			storyChoice.level = prevStoryChoice.level + 1;

			if (storyChoice.level > prevStoryChoice.level + 1) {
				// * prevChoice
				// **** choice
				Debug.LogWarning("ParseChoice can't jump ahead more than one choice level deep " + line + ", " + prevStoryChoice.story);
			}

		} else {
			// *** prevChoice
			// * choice
			// or
			// * prevChoice
			// * choice

			storyChoice.parent = prevStoryChoice;
			while (storyChoice.parent.parent != null) {
				if (storyChoice.level == storyChoice.parent.level + 1) {
					break;
				}
				storyChoice.parent = storyChoice.parent.parent;
			}
			if (storyChoice.level != storyChoice.parent.level + 1) {
				Debug.LogWarning("ParseChoice failed to find the correct parent choice for " + line + ", " + prevStoryChoice.story);
				storyChoice.level = storyChoice.parent.level + 1;
			}
		}

		storyChoice.parent.AddChoice(storyChoice);
		
		// "** Why so scared?" -> "Why so scared?"
		// may also be blank for continuing to the next page
		storyChoice.buttonText = line.Trim('*').Trim();

		// "* = question3" is actually a blank / jump-only choice with a choiceID of "question3"
		if (storyChoice.buttonText.StartsWith("=")) {
			string choiceID = storyChoice.buttonText.Trim('=').Trim();
			if (!string.IsNullOrEmpty(storyChoice.choiceID)) {
				Debug.LogWarning("Choice already has a choiceID! Overwriting " + storyChoice.choiceID + " with " + choiceID + ", " + prevStoryChoice.story);
			}
			storyChoice.choiceID = choiceID;
			storyChoice.buttonText = "";

			// moved to FinishChoice
			// if (choice.choiceID == "end") {
			// 	// Story.FinishLoading will further validate and add jumps to this from all deadend choices
			// 	// end choice starts blank with a page break
			// 	// it and its children will have no reqs, jumps, or sub choices other than page breaks
			// 	// return child after page break
			// 	// this will be removed again later if end has no text
			// 	choice = AddPageBreak(choice, true);
			// 	choice.choiceID = "end-after-break";
			// }
		} else if (storyChoice.buttonText.Length > maxWarnButtonLength) {
			Debug.LogWarning("Long button text: " + storyChoice.buttonText + ", " + storyChoice.story);
		}

		return storyChoice;
	}

	/// <summary>
	/// Just before moving on to the next sub-choice or finishing the story.
	/// May be also be called when adding a page break, AFTER the sub-choice was added.
	/// </summary>
	private static StoryChoice FinishChoice(StoryChoice storyChoice, bool noPageBreaks = false) {
		if (!storyChoice.resultText.IsNullOrEmpty()) {
			// expected to be in the middle of StringBuilding the resultText, it should be null
			Debug.LogWarning("ParserStory.FinishChoice for the second time... " + resultTextStringBuilder.Length + ", " + storyChoice);
			storyChoice.SetResultText(storyChoice.resultText + resultTextStringBuilder.ToString());
		} else {
			storyChoice.SetResultText(resultTextStringBuilder.ToString());
		}
		resultTextStringBuilder.Clear();
		
		if (storyChoice.resultText.IsNullOrEmpty()) {
			// if (choice.choiceID == "end-after-break") {
			// 	Debug.Log("end with no text! " + choice.story.storyID);
			// 	return RemovePageBreak(choice);
			// }
			
			// some choices just have a line break because something jumps TO them which already has text
			// choice.resultText = "";
			storyChoice.SetResultText("");
			
		} else if (storyChoice.choiceID == "end") {
			// Story.FinishLoading will further validate and add jumps to this from all deadend choices
			// end choice starts blank with a page break
			// it and its children will have no reqs, jumps, or sub choices other than page breaks
			if (!noPageBreaks) storyChoice = AddPageBreak(storyChoice, true);
		}
		
		return storyChoice;
	}
}
