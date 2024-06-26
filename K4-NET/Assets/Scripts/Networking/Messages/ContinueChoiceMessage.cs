using Unity.Networking.Transport;

public class ContinueChoiceMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.CONTINUE_CHOICE;
		}
	}

	public NetworkMessageType messageType;
	public string name;
	public bool choice;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteFixedString128(name);
		writer.WriteByte((byte)(choice ?  1 : 0));
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		name = reader.ReadFixedString128().ToString();
		choice = reader.ReadByte() == 1;
	}
}
