using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to an object in the scene to load the .exo story files.
/// Will then print an example to console, press 1-9 to ineract, 0 to reset.
/// </summary>
public class ExoscriptTest : MonoBehaviour {
	private const string exampleStoryID = "simpleExample";
	private static List<StoryChoice> availableChoices = new List<StoryChoice>();
	private static Result result;

	/// <summary>
	/// Pull stories from .exo or compiled .exoc files in StreamingAssets.
	/// </summary>
    void Awake() {
		// this also compiles .exo files to .exoc if they're missing or out of date
		// these load much faster at game start especially on consoles
        StoryParser.LoadStories(); 
        // fills Story.allMemories and story.allVars, log warnings
        Story.ValidateAllStories();
    }
    
	public void OnEnable() {
		PrintSimpleExample();
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Alpha0)) PrintSimpleExample();
		if (Input.GetKeyDown(KeyCode.Alpha1)) ChoicePicked(1);
		if (Input.GetKeyDown(KeyCode.Alpha2)) ChoicePicked(2);
		if (Input.GetKeyDown(KeyCode.Alpha3)) ChoicePicked(3);
		if (Input.GetKeyDown(KeyCode.Alpha4)) ChoicePicked(4);
		if (Input.GetKeyDown(KeyCode.Alpha5)) ChoicePicked(5);
		if (Input.GetKeyDown(KeyCode.Alpha6)) ChoicePicked(6);
		if (Input.GetKeyDown(KeyCode.Alpha7)) ChoicePicked(7);
		if (Input.GetKeyDown(KeyCode.Alpha8)) ChoicePicked(8);
		if (Input.GetKeyDown(KeyCode.Alpha9)) ChoicePicked(9);
	}

	private void ChoicePicked(int index) {
		if (availableChoices.Count < index - 1 || availableChoices[index - 1] ==  null) {
			Debug.Log("Unknown choice: " + index);
			return;
		}
		Debug.Log("----------- Choice selected: " + index);
		PrintSimpleExample(availableChoices[index - 1]);
	}

	/// <summary>
	/// Called both when opening the menu and again if a choice is chosen.
	/// Budget version of ResultsMenu that lets players drill down through the content warning spoilers.
	/// </summary>
	private void PrintSimpleExample(StoryChoice choice = null) {
		if (choice == null) {
			Debug.Log("----------- Executing story: " + exampleStoryID);
			Story story = Story.FromID(exampleStoryID);
			if (story == null) {
				Debug.LogError("ExoscriptTest failed to find story: " + exampleStoryID);
				return;
			}
			choice = story.entryChoice;
			
			// container to execute choices into and save our story state
			result = new Result();
		}
		
		// execute any SET, CALL, etc tags in the selected choice, process the text, and fill a result
		choice.Execute(result);
		
		// result.text may differ from choice.resultText after being processed
		Debug.Log(result.text.ReplaceAll("\n", " "));

		availableChoices.Clear();
		int numChoices = 0;
		
		// result.choice may differ from choice if we jumped
		foreach (StoryChoice subChoice in result.choice.choices) {
			// hide choices which fail an IF statement, or can only be jumped to 
			if (!subChoice.CanShow(result)) continue;

			// display but disable choices which fail an IFD statement
			if (!subChoice.CanExecuteShown(result)) {
				numChoices++;
				Debug.Log("Disabled choice -" + numChoices + "-: " + subChoice.GetButtonText());
				availableChoices.Add(null);
				continue;
			}
			
			// show and enable executable choices
			numChoices++;
			Debug.Log("Press " + numChoices + ": " + subChoice.GetButtonText());
			availableChoices.Add(subChoice);
		}

		if (numChoices == 0) {
			Debug.Log("Reached an end. Press 0 to start over.");
		}
	}

}
