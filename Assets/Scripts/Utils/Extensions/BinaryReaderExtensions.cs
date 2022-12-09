using System.IO;

public static class SerializationUtilities {
	public static string ReadStringSafe(this BinaryReader reader) {
		bool notNull = reader.ReadBoolean();
		if (notNull) {
			return reader.ReadString();
		} else {
			return null;
		}
	}

	public static void WriteStringSafe(this BinaryWriter writer, string s) {
		if (s != null) {
			writer.Write(true);
			writer.Write(s);
		} else {
			writer.Write(false);
		}
	}
}
