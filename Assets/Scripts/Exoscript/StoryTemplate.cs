using System.IO;

public class StoryTemplate {
	public string storyID;
	public string storyIDCamelcase;

	public StoryChoiceTemplate entryChoice;
	public string collectibleID;

	public static StoryTemplate Deserialize(BinaryReader reader) {
		return new StoryTemplate {
			storyID = reader.ReadStringSafe(),
			storyIDCamelcase = reader.ReadStringSafe(),
			entryChoice = StoryChoiceTemplate.Deserialize(reader),
			collectibleID = reader.ReadStringSafe(),
		};
	}

	public static void Serialize(BinaryWriter writer, StoryTemplate template) {
		writer.WriteStringSafe(template.storyID);
		writer.WriteStringSafe(template.storyIDCamelcase);
		StoryChoiceTemplate.Serialize(writer, template.entryChoice);
		writer.WriteStringSafe(template.collectibleID);
	}
}
