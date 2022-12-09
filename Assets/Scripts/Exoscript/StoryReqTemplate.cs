using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StoryReqTemplate {
	private enum FlagValue : byte {
		False,
		True,
		Null
	}

	private enum ObjectType : byte {
		Int,
		Bool,
		String,
		Call,
		Null
	}

	public StoryReqType type;
	public StoryReqCompare compare;

	public string stringID;
	public bool showDisabled;
	public int intValue;
	public bool? flagValue;
	public string stringValue;
	public object objectValue;

	public RepeatType repeatType;
	public StoryCallTemplate call;
	public Priority priority;

	public string debugString;
	public List<StoryReqTemplate> subReqs = new List<StoryReqTemplate>();

	public static StoryReqTemplate Deserialize(BinaryReader reader) {
		bool notNull = reader.ReadBoolean();
		if (notNull) {
			var template = new StoryReqTemplate {
				type = (StoryReqType)reader.ReadInt32(),
				compare = (StoryReqCompare)reader.ReadInt32(),
				stringID = reader.ReadStringSafe(),
				showDisabled = reader.ReadBoolean(),
				intValue = reader.ReadInt32(),
				flagValue = ReadFlagValue(reader),
				stringValue = reader.ReadStringSafe(),
				objectValue = ReadObjectValue(reader),
				repeatType = (RepeatType)reader.ReadInt32(),
				call = StoryCallTemplate.Deserialize(reader),
				priority = (Priority)reader.ReadInt32(),
				debugString = reader.ReadStringSafe()
			};

			int subReqCount = reader.ReadInt32();
			for (int i = 0; i < subReqCount; ++i) {
				template.subReqs.Add(Deserialize(reader));
			}

			return template;
		} else {
			return null;
		}
	}

	public static void Serialize(BinaryWriter writer, StoryReqTemplate template) {
		if (template != null) {
			writer.Write(true);
			writer.Write((int)template.type);
			writer.Write((int)template.compare);
			writer.WriteStringSafe(template.stringID);
			writer.Write(template.showDisabled);
			writer.Write(template.intValue);
			WriteFlagValue(writer, template.flagValue);
			writer.WriteStringSafe(template.stringValue);
			WriteObjectValue(writer, template.objectValue);
			writer.Write((int)template.repeatType);
			StoryCallTemplate.Serialize(writer, template.call);
			writer.Write((int)template.priority);
			writer.WriteStringSafe(template.debugString);

			writer.Write(template.subReqs.Count);
			foreach (var subReq in template.subReqs) {
				Serialize(writer, subReq);
			}
		} else {
			writer.Write(false);
		}
	}

	private static bool? ReadFlagValue(BinaryReader reader) {
		var value = (FlagValue)reader.ReadByte();
		switch (value) {
			case FlagValue.False:
				return false;

			case FlagValue.True:
				return true;

			case FlagValue.Null:
				return null;

			default:
				Debug.LogError($"Unexpected value for optional bool: {value}");
				return null;
		}
	}

	private static void WriteFlagValue(BinaryWriter writer, bool? value) {
		FlagValue flagValue;
		if (value.HasValue) {
			if (value.Value) {
				flagValue = FlagValue.True;
			} else {
				flagValue = FlagValue.False;
			}
		} else {
			flagValue = FlagValue.Null;
		}

		writer.Write((byte)flagValue);
	}

	private static object ReadObjectValue(BinaryReader reader) {
		var type = (ObjectType)reader.ReadByte();
		switch (type) {
			case ObjectType.Bool:
				return reader.ReadBoolean();

			case ObjectType.Int:
				return reader.ReadInt32();

			case ObjectType.String:
				return reader.ReadStringSafe();

			case ObjectType.Call:
				return StoryCallTemplate.Deserialize(reader);

			case ObjectType.Null:
				return null;

			default:
				Debug.LogError($"Unexpected type when reading object value: {type}");
				return null;
		}
	}

	private static void WriteObjectValue(BinaryWriter writer, object objectValue) {
		if (objectValue == null) {
			writer.Write((byte)ObjectType.Null);
		} else if (objectValue is bool b) {
			writer.Write((byte)ObjectType.Bool);
			writer.Write(b);
		} else if (objectValue is int i) {
			writer.Write((byte)ObjectType.Int);
			writer.Write(i);
		} else if (objectValue is string s) {
			writer.Write((byte)ObjectType.String);
			writer.WriteStringSafe(s);
		} else if (objectValue is StoryCallTemplate c) {
			writer.Write((byte)ObjectType.Call);
			StoryCallTemplate.Serialize(writer, c);
		} else {
			if (objectValue != null) {
				Debug.LogError($"Unexpected type when writing object value: {objectValue.GetType()}");
			}

			writer.Write((byte)ObjectType.Null);
		}
	}
}
