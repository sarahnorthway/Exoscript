using System.IO;
using UnityEngine;

public class StoryCallTemplate {
	private enum ParameterType : byte {
		Bool,
		Int,
		String
	}

	public string methodName;
	public object[] parameterArray;
	public string debugString;

	public static StoryCallTemplate Deserialize(BinaryReader reader) {
		bool notNull = reader.ReadBoolean();
		if (notNull) {
			return new StoryCallTemplate {
				methodName = reader.ReadStringSafe(),
				parameterArray = ReadParameters(reader),
				debugString = reader.ReadStringSafe()
			};
		} else {
			return null;
		}
	}

	public static void Serialize(BinaryWriter writer, StoryCallTemplate template) {
		if (template != null) {
			writer.Write(true);
			writer.WriteStringSafe(template.methodName);
			WriteParameters(writer, template.parameterArray);
			writer.WriteStringSafe(template.debugString);
		} else {
			writer.Write(false);
		}
	}

	private static object[] ReadParameters(BinaryReader reader) {
		int parameterCount = reader.ReadInt32();
		var parameters = new object[parameterCount];
		for (int i = 0; i < parameterCount; ++i) {
			parameters[i] = ReadParameter(reader);
		}

		return parameters;
	}

	private static object ReadParameter(BinaryReader reader) {
		var type = (ParameterType)reader.ReadByte();
		switch (type)
		{
			case ParameterType.Bool:
				return reader.ReadBoolean();

			case ParameterType.Int:
				return reader.ReadInt32();

			case ParameterType.String:
				return reader.ReadStringSafe();
		}

		Debug.LogError($"Unexpected type when reading parameter: {type}");
		return string.Empty;
	}

	private static void WriteParameters(BinaryWriter writer, object[] parameters) {
		writer.Write(parameters.Length);
		foreach (var parameter in parameters) {
			WriteParameter(writer, parameter);
		}
	}

	private static void WriteParameter(BinaryWriter writer, object parameter) {
		if (parameter is bool b) {
			writer.Write((byte)ParameterType.Bool);
			writer.Write(b);
		} else if (parameter is int i) {
			writer.Write((byte)ParameterType.Int);
			writer.Write(i);
		} else if (parameter is string s) {
			writer.Write((byte)ParameterType.String);
			writer.WriteStringSafe(s);
		} else {
			Debug.LogError($"Unexpected type when writing parameter: {parameter.GetType()}");
			writer.Write((byte)ParameterType.String);
			writer.WriteStringSafe(string.Empty);
		}
	}
}
