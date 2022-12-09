using System.IO;

public class StorySetTemplate {
	public StorySetType type;
	public StorySetCompare compare;
	public string debugString;

	public string stringID;
	public int intValue;
	public bool boolValue;
	public string stringValue;

	public StoryCallTemplate call;
	public StoryReqTemplate requirement;
	public StorySetTemplate elseSet;

	public static StorySetTemplate Deserialize(BinaryReader reader) {
		bool notNull = reader.ReadBoolean();
		if (notNull) {
			return new StorySetTemplate {
				type = (StorySetType)reader.ReadInt32(),
				compare = (StorySetCompare)reader.ReadInt32(),
				debugString = reader.ReadStringSafe(),
				stringID = reader.ReadStringSafe(),
				intValue = reader.ReadInt32(),
				boolValue = reader.ReadBoolean(),
				stringValue = reader.ReadStringSafe(),
				call = StoryCallTemplate.Deserialize(reader),
				requirement = StoryReqTemplate.Deserialize(reader),
				elseSet = Deserialize(reader)
			};
		} else {
			return null;
		}
	}

	public static void Serialize(BinaryWriter writer, StorySetTemplate template) {
		if (template != null) {
			writer.Write(true);
			writer.Write((int)template.type);
			writer.Write((int)template.compare);
			writer.WriteStringSafe(template.debugString);
			writer.WriteStringSafe(template.stringID);
			writer.Write(template.intValue);
			writer.Write(template.boolValue);
			writer.WriteStringSafe(template.stringValue);
			StoryCallTemplate.Serialize(writer, template.call);
			StoryReqTemplate.Serialize(writer, template.requirement);
			Serialize(writer, template.elseSet);
		} else {
			writer.Write(false);
		}
	}
}
