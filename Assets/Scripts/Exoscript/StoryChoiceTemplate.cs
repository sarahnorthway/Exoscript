using System.Collections.Generic;
using System.IO;

public class StoryChoiceTemplate {
	public string choiceID;
	public int level;

	public string buttonText;
	public string resultText;

	public List<StoryReqTemplate> requirements = new List<StoryReqTemplate>();
	public List<StorySetTemplate> sets = new List<StorySetTemplate>();
	public List<StoryChoiceTemplate> choices = new List<StoryChoiceTemplate>();

	public static StoryChoiceTemplate Deserialize(BinaryReader reader) {
		var template = new StoryChoiceTemplate {
			choiceID = reader.ReadStringSafe(),
			level = reader.ReadInt32(),
			buttonText = reader.ReadStringSafe(),
			resultText = reader.ReadStringSafe(),
		};

		int requirementCount = reader.ReadInt32();
		for (int i = 0; i < requirementCount; ++i) {
			template.requirements.Add(StoryReqTemplate.Deserialize(reader));
		}

		int setCount = reader.ReadInt32();
		for (int i = 0; i < setCount; ++i) {
			template.sets.Add(StorySetTemplate.Deserialize(reader));
		}

		int choiceCount = reader.ReadInt32();
		for (int i = 0; i < choiceCount; ++i) {
			template.choices.Add(Deserialize(reader));
		}

		return template;
	}

	public static void Serialize(BinaryWriter writer, StoryChoiceTemplate template) {
		writer.WriteStringSafe(template.choiceID);
		writer.Write(template.level);
		writer.WriteStringSafe(template.buttonText);
		writer.WriteStringSafe(template.resultText);

		writer.Write(template.requirements.Count);
		foreach (var requirement in template.requirements) {
			StoryReqTemplate.Serialize(writer, requirement);
		}

		writer.Write(template.sets.Count);
		foreach (var set in template.sets) {
			StorySetTemplate.Serialize(writer, set);
		}

		writer.Write(template.choices.Count);
		foreach (var choice in template.choices) {
			Serialize(writer, choice);
		}
	}
}
