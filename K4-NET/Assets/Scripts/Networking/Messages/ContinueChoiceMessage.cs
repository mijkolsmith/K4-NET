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
	public bool choice;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteByte(choice ? (byte) 1 : (byte) 0);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		choice = reader.ReadByte() == 1 ? true : false;
	}
}
