using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Static methods for parsing choice text.
/// </summary>
public class TextFilter {
	
	/// <summary>
	/// Convert dynamic text like [Name] to current values.
	/// Also convert _ to italics, and color dialog
	/// </summary>
	public static string FilterResultText(string text, Result result, Story story, bool resultButton = false) {
		if (text == null) {
			//Debug.Log("FilterResultText is null");
			return "";
		}

		if (text.Contains("[")) {
			// replace "[=var_something]" etc with the variable
			text = FilterPrint(text, result, story);

			// refer to player character by name
			// text = text.ReplaceAll("[Name]", GameManager.princessName);

			// replace "[them]" with "her" eg if two character are talking about you
			text = FilterGender(text);

			// replace inline [if something ? value1 : value2] 
			text = FilterIfSimple(text, result, story);
			// and full form [if]...[else]...[endif]
			text = FilterIfFull(text, result, story);

			// missed one
			if (text.Contains("[")) {
				Debug.LogWarning("Unknown dynamic resultText rule, " + text);
			}
		}

		// remove leading whitespace and extra line breaks
		// line breaks forced to \n during FileManager.LoadFileLines
		text = text.TrimSafe();
		text = text.RemoveStart("\n", true);
		text = text.RemoveEnding("\n", true);
		text = text.ReplaceAll("\n\n\n", "\n\n");
		text = text.ReplaceAll("\n\n\n", "\n\n");
		text = text.ReplaceAll("  ", " ");

		if (resultButton && !StoryChoice.IsContinueText(text) && !text.EndsWith("...") && !text.EndsWith("...\"")) {
			// remove periods from the end of button text but keep exclamation and question marks
			text = text.RemoveEnding(".");
			text = text.ReplaceLast(".\"", "\"");
			text = text.ReplaceLast("._", "_");
		}

		// turn _ into <i> last in case it would affect trimming
		text = FilterEmphasis(text, result, resultButton);

		return text;
	}

	/// <summary>
	/// [=var_veskudos]
	/// [=mem_something]
	/// [=hog_somethingelse]
	/// [=call_mostlove]
	/// </summary>
	private static string FilterPrint(string text, Result result, Story story) {
		string pattern = @"\[=([^\]]*)\]";
		MatchCollection matches = Regex.Matches(text, pattern);
		string replace = "";
		foreach (Match match in matches) {
			string fullMatch = match.Groups[0].Value;
			string variableID = match.Groups[1].Value.Trim().ToLower();
			if (variableID.StartsWith("var_")) {
				variableID = variableID.RemoveStart("var_");
				replace = story.vars.Get(variableID);

			} else if (variableID.StartsWith("mem_")) {

				variableID = variableID.RemoveStart("mem_");
				replace = StoryManager.GetMemory(variableID);

			} else if (variableID.StartsWith("hog_")) {
				variableID = variableID.RemoveStart("hog_");
				replace = StoryManager.GetGroundhog(variableID);

			} else if (variableID.StartsWith("call_")) {
				variableID = variableID.RemoveStart("call_");
				StoryCall call = StoryParserSet.ParseCall("~call " + variableID);
				if (call == null) {
					Debug.LogWarning("Invalid dynamic resultText FilterPrint call, " + text);
				} else {
					call.Validate();
					replace = call.Execute()?.ToString() ?? "";
				}

			} else {
				Debug.LogWarning("Invalid dynamic resultText FilterPrint, " + text);
			}

			// during validation, and could be cases where we expect it to be broken
			if (string.IsNullOrEmpty(replace)) {
				replace = "";
			}

			// replace may contain further [dynamic tags] so filter those too
			replace = FilterResultText(replace, result, story);

			text = text.ReplaceAll(fullMatch, replace);
		}

		return text;
	}

	/// <summary>
	/// [nonbinary|female|male] - can't have if or [ or ] inside the three parts
	/// </summary>
	public static string FilterGender(string text) {
		string pattern = @"\[(?!if )([^\[\]\|]+)\|([^\[\]\|]+)\|([^\[\]\|]+)\]";
		for (int i = 0; i < 100; i++) {
			Match match = Regex.Match(text, pattern);
			if (string.IsNullOrEmpty(match.Value)) {
				break;
			}

			// [nonbinary|female|male]
			// string fullMatch = match.Groups[0].Value;
			// string nonbinary = match.Groups[1].Value.Trim();
			// string female = match.Groups[2].Value;
			// string male = match.Groups[3].Value;
			//
			// string custom = GetCustomGenderString(fullMatch);

			// string genderedText = nonbinary;
			// if (GameManager.genderPronouns == GenderID.female) {
			// 	genderedText = female;
			// } else if (GameManager.genderPronouns == GenderID.male) {
			// 	genderedText = male;
			// } else {
			// 	// this replaces They/Them with custom values if they've been defined
			// 	if (!custom.IsNullOrEmptyOrWhitespace()) genderedText = custom;
			// }
			// text = text.ReplaceAll(fullMatch, genderedText);
		}

		return text;
	}

	public static string GetCustomGenderString(string customKey) {
		customKey = customKey.RemoveStart("[").RemoveEnding("]");
		string nonbinary = customKey.Split('|').GetSafe(0);
		
		// no strings at all, return they
		if (GameSettings.customGender.Count == 0) {
			return nonbinary;
		}
		
		// custom value for they|he|she
		if (GameSettings.customGender.ContainsKey(customKey)) {
			return GameSettings.customGender.GetSafe(customKey);
		}
		
		// They|He|She --> they|he|she
		if (GameSettings.customGender.ContainsKey(customKey.ToLower())) {
			// must be They --> they so we can reverse it, not SomeThing eLsE Weird
			if (nonbinary.ToLower().CapitalizeFirstChar() == nonbinary) {
				string lowercase = GameSettings.customGender.GetSafe(customKey.ToLower());
				return lowercase.CapitalizeFirstChar();
			} else {
				// too fancy to capitalize
			}
		}
		
		// no custom entry, return they
		return nonbinary;
	}

	/// <summary>
	/// Seed is based on current month and story, so it's the same for every requirement in this story.
	/// TrulyRandom is based on Time.realtimeSinceStartup.1
	/// </summary>
	public static string GetRandomSeed(Story story, bool trulyRandom) {
		string value;
		if (trulyRandom) {
			// different if you save and reload or come back during the same month
			value = NWUtils.TrulyRandomSeed();
		} else {
			// pinned to the event so a random choice will be the same choice every time in this story
			value = "randomReq" + (story == null ? "" : story.storyID) + GameSettings.month;
		}
		return value;
	}

	/// <summary>
	/// [if reqText : outputText]
	/// [if reqText ? outputText]
	/// [if reqText ? outputText : outputTextElse]
	/// </summary>
	private static string FilterIfSimple(string text, Result result, Story story) {
		// https://regex101.com/
		string pattern = @"\[if ([^\[^\]^:^?]*)[\?:]([^\[^\]^:]*):?([^\[^\]]*)\]";
		// process one match at a time to allow for nested [if] statements
		// [if reqText : outputText]
		for (int i = 0; i < 100; i++) {
			Match match = Regex.Match(text, pattern);
			if (string.IsNullOrEmpty(match.Value)) {
				break;
			}

			string fullMatch = match.Groups[0].Value;
			string reqText = match.Groups[1].Value.Trim().ToLower();
			// keep spaces eg [if mem_whatever?    Spaced!   ]
			string outputText = match.Groups[2].Value;
			// will be blank if no else portion eg [if reqText : outputText]
			string outputTextElse = match.Groups[3].Value;

			// random keyword picks one possible output value
			// [if random : hunting feral dogs|trying to find some clean drinking water|looking for a working sparkplug]
			// [if random! : more random one|more random two]
			if (reqText == "random" || reqText == "random!") {
				// if there are 2 random blocks, pick the same option for every random block in this story on this month
				// for random! pick a different option every time
				string[] randomValues = outputText.Split('|');
				// use PickRandomWeighted so algorithm is the same as ~if random and [if random] blocks
				List<float> weights = randomValues.Select(value => 1).Select(dummy => (float) dummy).ToList();
				string seed = GetRandomSeed(story, reqText == "random!");
				string randomText = randomValues.PickRandomWeighted(weights, seed);
				if (GameSettings.debugAllTextChunks) {
					string debugOutput = "";
					foreach (string possibleText in randomValues) {
						if (possibleText == randomText) {
							debugOutput += debugVisibleTextChunk(possibleText, true);
						} else {
							debugOutput += debugHiddenTextChunk(possibleText, true);
						}
					}
					text = text.ReplaceFirst(fullMatch, debugOutput.RemoveEnding(" "));
				} else {
					text = text.ReplaceFirst(fullMatch, randomText.TrimSafe());
				}
				continue;
			}

			// validate immediately because all stories and other data is already parsed
			StoryReq req = StoryParserReq.ParseReq("~if " + reqText, story);
			if (req == null) {
				Debug.LogWarning("Choice resultText contains invalid [if], " + fullMatch);
				text = text.ReplaceFirst(fullMatch, "");
				continue;
			} else {
				req.ValidateAndFinish();
			}

			if (GameSettings.debugAllTextChunks) {
				if (req.Execute(result)) {
					string debugOutput = debugVisibleTextChunk(outputText, true) + debugHiddenTextChunk(outputTextElse, true);
					text = text.ReplaceFirst(fullMatch, debugOutput);
				} else {
					string debugOutput = debugHiddenTextChunk(outputText, true) + debugVisibleTextChunk(outputTextElse, true);
					text = text.ReplaceFirst(fullMatch, debugOutput);
				}
			} else {
				if (req.Execute(result)) {
					text = text.ReplaceFirst(fullMatch, outputText);
				} else {
					text = text.ReplaceFirst(fullMatch, outputTextElse);
				}
			}

		}
		return text;
	}
	
	
	/// <summary>
	/// [if mem_metNomi = false] .... [endif]
	/// .... may contain additional [elseif thing = stuff] or [else] statements
	/// nested [if]...[endif] statements will be processed first
	/// </summary>
	private static string FilterIfFull(string text, Result result, Story story) {
		// https://regex101.com/
		string pattern = @"\[if ([^\[^\]^:^?]*)\]((?s:(?!\[endif\]|\[end\])(?!\[if).)*)(?:\[endif\]|\[end\])";
		// safer than while:true
		for (int i = 0; i < 100; i++) {
			// process one match at a time to allow for nested [if] statements
			Match match = Regex.Match(text, pattern);
			if (match == null || string.IsNullOrEmpty(match.Value)) {
				break;
			}

			string fullMatch = match.Groups[0].Value;
			string reqText = match.Groups[1].Value.Trim().ToLower();
			string insideText = match.Groups[2].Value;

			// random keyword picks one possible output value
			// [if random] some text [or] other text [else random = 3] 3x more likely [elseif season = pollen] maybe in pollen [end]
			if (reqText.Contains("random")) {
				string randomPattern = @"\[([^\]]*)\]([^\[]*)";
				// else if and elseif must be first because they contain if and else
				string[] randomSplitters = { "or if", "else if", "elseif", "if", "or", "else", "|", "||", };
				MatchCollection randomMatches = Regex.Matches(fullMatch, randomPattern);
				List<string> randomValues = new List<string>();
				List<float> randomWeights = new List<float>();
				bool ignoreSeed = false;

				foreach (Match randomMatch in randomMatches) {
					string randomReqText = randomMatch.Groups[1].Value.Trim().ToLower();
					string randomValue = randomMatch.Groups[2].Value;

					if (string.IsNullOrEmpty(randomValue)) continue;
				
					// eg exactly "or" or "else"
					if (randomSplitters.Contains(randomReqText)) {
						randomValues.Add(randomValue);
						randomWeights.Add(1);
						continue;
					}

					// eg "if random" -> "random"
					// eg "if random!" -> "random" but with no seed (always for chara low repeat events)
					// eg "random = 3" -> "random = 3"
					// eg "elseif random = 5" -> "random = 5"
					// eg "else if season = pollen" -> "season = pollen"
					randomReqText = randomReqText.RemoveStart(randomSplitters).Trim();
					StoryReq randomReq = StoryParserReq.ParseReq("~if " + randomReqText, story);
					if (randomReq == null) {
						Debug.LogWarning("Choice resultText contains invalid random [else], " + fullMatch);
						continue;
					} else {
						randomReq.ValidateAndFinish();
						// random! means use a totally random seed every time (including every choice)
						if (randomReq.flagValue == true) ignoreSeed = true;
						// don't make nomiRepeat etc truly random because their dates require less random random
						// random! or low priority chara events (eg repeating one) are truly random
						// if (randomReq.flagValue == true || story.isCharaLow) ignoreSeed = true;
					}

					// eg "season = quiet && random = 10"
					if (randomReq.type == StoryReqType.and) {
						int weight = 1;
						bool executePassed = true;
						foreach (StoryReq subReq in randomReq.subReqs) {
							if (subReq.type == StoryReqType.random) {
								weight = subReq.intValue;
								continue;
							}
							if (!subReq.Execute(result)) {
								executePassed = false;
								break;
							}
						}
						if (executePassed) {
							randomValues.Add(randomValue);
							randomWeights.Add(weight);
						}
						continue;
					}

					// eg "random = 3"
					if (randomReq.type == StoryReqType.random) {
						randomValues.Add(randomValue);
						randomWeights.Add(randomReq.intValue);
						continue;
					}

					// eg "season = pollen"
					if (randomReq.Execute(result)) {
						randomValues.Add(randomValue);
						randomWeights.Add(1);
						continue;
					}
				}
				
				// random! (or chara low) is truly random, random is pinned to storyID and month 
				string seed = GetRandomSeed(story, ignoreSeed);
				// if there is more than one random within the text block, each will be different
				// first one will be the same as other first ones within the event
				// first one is the most nested one, because nested is processed first
				seed += i;
				string outputText = randomValues.PickRandomWeighted(randomWeights, seed);
				
				if (GameSettings.debugAllTextChunks) {
					string debugOutput = "";
					foreach (string possibleText in randomValues) {
						if (possibleText == outputText) {
							debugOutput += debugVisibleTextChunk(possibleText);
						} else {
							debugOutput += debugHiddenTextChunk(possibleText);
						}
					}
					text = text.ReplaceFirst(fullMatch, debugOutput.RemoveEnding("\n\n"));
				} else {
					text = text.ReplaceFirst(fullMatch, outputText);
				}

				continue;
			}

			// validate immediately because all stories and other data is already parsed
			StoryReq req = StoryParserReq.ParseReq("~if " + reqText, story);
			if (req == null) {
				Debug.LogWarning("Choice resultText contains invalid [if], " + fullMatch);
				text = text.ReplaceFirst(fullMatch, "");
				continue;
			} else {
				req.ValidateAndFinish();
			}

			// [elseif reqTextInner] outputText
			// [else] outputText
			string patternInner = @"\[else([^\[]*)\]([^\[]+)";
			MatchCollection innerMatches = Regex.Matches(insideText, patternInner);
			if (innerMatches.Count == 0) {
				// simple case, no [elseif] or [else]
				if (GameSettings.debugAllTextChunks) {
					if (req.Execute(result)) {
						text = text.ReplaceFirst(fullMatch, debugVisibleTextChunk(insideText));
					} else {
						text = text.ReplaceFirst(fullMatch, debugHiddenTextChunk(insideText));
					}
				} else {
					if (req.Execute(result)) {
						text = text.ReplaceFirst(fullMatch, insideText);
					} else {
						text = text.ReplaceFirst(fullMatch, "");
					}
				}
				continue;
			}

			bool foundMatch = false;
			string debugOutputText = ""; // for debugging show all text chunks
			
			// if first req executes true, replace with text before the first [else... match
			// [if reqText] outputText [else...
			if (GameSettings.debugAllTextChunks) {
				string outputText = insideText.Substring(0, insideText.IndexOf("[else"));
				if (req.Execute(result)) {
					foundMatch = true;
					debugOutputText += debugVisibleTextChunk(outputText);
				} else {
					debugOutputText += debugHiddenTextChunk(outputText);
				}
				// keep going to print them all for debugging
			} else {
				if (req.Execute(result)) {
					foundMatch = true;
					string outputText = insideText.Substring(0, insideText.IndexOf("[else"));
					text = text.ReplaceFirst(fullMatch, outputText);
					continue;
				}
			}

			foreach (Match innerMatch in innerMatches) {
				// [elseif reqText] outputText
				// [else] outputText
				reqText = innerMatch.Groups[1].Value.Trim().ToLower();
				string outputText = innerMatch.Groups[2].Value;

				// stop when reaching [else]
				if (string.IsNullOrEmpty(reqText)) {
					if (GameSettings.debugAllTextChunks) {
						if (foundMatch) {
							debugOutputText += debugHiddenTextChunk(outputText);
						} else {
							foundMatch = true;
							debugOutputText += debugVisibleTextChunk(outputText);
						}
						break;
					} else {
						foundMatch = true;
						text = text.ReplaceFirst(fullMatch, outputText);
						break;
					}
				}

				// stop when reaching any true [elseif]
				// validate immediately because all stories and other data is already parsed
				req = StoryParserReq.ParseReq("~" + reqText, story);
				if (req == null) {
					Debug.LogWarning("Choice resultText contains invalid [elseif], " + fullMatch);
					text = text.ReplaceFirst(fullMatch, "");
					break;
				} else {
					req.ValidateAndFinish();
				}
				
				if (GameSettings.debugAllTextChunks) {
					if (!foundMatch && req.Execute(result)) {
						foundMatch = true;
						debugOutputText += debugVisibleTextChunk(outputText);
					} else {
						debugOutputText += debugHiddenTextChunk(outputText);
					}
					// keep going to print them all for debugging
				} else {
					if (req.Execute(result)) {
						foundMatch = true;
						text = text.ReplaceFirst(fullMatch, outputText);
						break;
					}
				}
			}

			if (GameSettings.debugAllTextChunks) {
				text = text.ReplaceFirst(fullMatch, debugOutputText.TrimSafe());
			} else {
				// if we never found a match, syntax must be
				// [if] ... [elseif] ... [end] with no default catchall [else]; replace with blank
				if (!foundMatch) {
					text = text.ReplaceFirst(fullMatch, "");
				}
			}
		}

		return text;
	}

	/// <summary>
	/// Show all text chunks but make usually-hidden ones dark grey if GameSettings.debugAllTextChunks
	/// </summary>
	private static string debugVisibleTextChunk(string text, bool spaceNotLineBreaks = false) {
		return "{" + text.TrimSafe() + "}" + (spaceNotLineBreaks ? " " : "\n\n");
	}

	/// <summary>
	/// Show all text chunks but make usually-hidden ones dark grey if GameSettings.debugAllTextChunks
	/// </summary>
	private static string debugHiddenTextChunk(string text, bool spaceNotLineBreaks = false) {
		return "<color=#CCCCCC>{" + text.TrimSafe() + "}</color>" + (spaceNotLineBreaks ? " " : "\n\n");
	}

	/// <summary>
	/// Set html tags for _italics_ and "spoken dialog text".
	/// http://digitalnativestudios.com/textmeshpro/docs/rich-text/
	/// Result is optional for speaker dialog color.
	/// </summary>
	public static string FilterEmphasis(string text, Result result, bool resultButton = false) {
		if (text.IsNullOrEmpty()) return "";
		
		// https://regex101.com/
		string dialogPattern = @"""([^""]*)""";
		string italicsPattern = @"_([^_]*)_";
		
		// find dialog
		MatchCollection matches = Regex.Matches(text, dialogPattern);
		foreach (Match match in matches) {
			string fullMatch = match.Groups[0].Value;
			string insideText = match.Groups[1].Value;
			insideText = insideText.RemoveStartEnd("\n");
			
			// inner italics become non-italics
			// MatchCollection insideMatches = Regex.Matches(insideText, italicsPattern);
			// foreach (Match insideMatch in insideMatches) {
			// 	string fullInsideMatch = insideMatch.Groups[0].Value;
			// 	string insideInsideText = insideMatch.Groups[1].Value;
			// 	insideInsideText = insideInsideText.RemoveStartEnd("\n");
			// 	insideText = insideText.ReplaceAll(fullInsideMatch, "</i>" + insideInsideText + "<i>");
			// }
			
			// string dialogColor = result?.speakerChara?.dialogColor ?? Chara.defaultDialogColor;
			// if (resultButton) {
			// 	// results buttons show quotes
			// 	//text = text.ReplaceAll(fullMatch, insideText);
			// } else if (ResultsMenu.isFancyWhite) {
			// 	// only bold text for endings, not pale since we couldn't read them
			// 	text = text.ReplaceAll(fullMatch, "\"<b>" + insideText + "</b>\"");
			// 	
			// } else {
			// 	text = text.ReplaceAll(fullMatch, 
			// 		"\"<color=#" + dialogColor + "><b>" + insideText + "</b></color>\"");
			// 	
			// 	// highlight spoken text using mark tag
			// 	// periods will be covered by the mark color and act as forced spacers
			// 	// bold tag is required to make the text appear over the mark... might not be on purpose?
			// 	//text = text.ReplaceAll(fullMatch, 
			// 	//	"<mark=#" + dialogColor + ">..<color=#000000><b>\"" + insideText + "\"</b></color>..</mark>");
			// }
		}
		
		// Shortcut for _italic text_ but <i>HTML tags</i> also work.
		matches = Regex.Matches(text, italicsPattern);
		foreach (Match match in matches) {
			string fullMatch = match.Groups[0].Value;
			string insideText = match.Groups[1].Value;
			insideText = insideText.RemoveStartEnd("\n");
			text = text.ReplaceAll(fullMatch, "<i>" + insideText + "</i>");
		}

		return text;
	}

	// public static string Genderize(string nonbinary, string female, string male, string custom) {
	// 	if (Princess.genderPronouns == GenderID.female) {
	// 		return female;
	// 	} else if (Princess.genderPronouns == GenderID.male) {
	// 		return male;
	// 	} else {
	// 		// TODO custom or nonbinary
	// 		if (!custom.IsNullOrEmptyOrWhitespace()) return custom;
	// 		
	// 		return nonbinary;
	// 	}
	// }
	
	// public static void ReplaceNonbinary() {
	// 	// list of pipe-delimeted "neutral|female|male"
	// 	List<string> genderStrings = new List<string>();
	// 	foreach (Story story in Story.allStories) {
	// 		foreach (Choice choice in story.allChoices) {
	// 			AddGenderStrings(choice.buttonText, genderStrings);
	// 			AddGenderStrings(choice.resultText, genderStrings);
	// 		}
	// 	}
	//
	// 	genderStrings = genderStrings.OrderBy(x => x.Length).ToList();
	// 		
	// 	Debug.Log("All genderStrings: " + genderStrings.ToStringSafe());
	//
	// 	SettingsMenu.instance.OpenGroup("gender");
	// // }
	//
	// public static void AddGenderStrings(string text, List<string> genderStrings) {
	// 	if (text.IsNullOrEmptyOrWhitespace()) return;
	// 	const string pattern = @"\[(?!if )([^\[\]\|]+)\|([^\[\]\|]+)\|([^\[\]\|]+)\]";
	// 	foreach (Match match in Regex.Matches(text, pattern)) {
	// 		// [neutral|female|male]
	// 		string fullMatch = match.Groups[0].Value;
	// 		fullMatch = fullMatch.RemoveStart("[").RemoveEnding("]");
	// 		
	// 		// if (genderStrings.ContainsSafe(fullMatch.ToLower())) {
	// 		// 	string nonbinary = fullMatch.Split('|').GetSafe(0);
	// 		// 	// must be They --> they so we can reverse it, not SomeThing eLsE Weird
	// 		// 	if (nonbinary.ToLower().CapitalizeFirstChar() == nonbinary) {
	// 		// 		// don't add capitalized version to the list
	// 		// 		continue;
	// 		// 	} else {
	// 		// 		Debug.Log("======= lowercase matches but caps do not (AddGenderStrings): " + fullMatch);
	// 		// 	}
	// 		// }
	// 		
	// 		genderStrings.AddSafe(fullMatch);
	// 	}
	// }
}
