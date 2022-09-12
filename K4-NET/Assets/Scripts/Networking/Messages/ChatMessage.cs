using Unity.Networking.Transport;

public class ChatMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.CHAT_MESSAGE;
		}
	}

	public NetworkMessageType messageType;
	public string message;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);
	
		writer.WriteFixedString128(message);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		message = reader.ReadFixedString128().ToString();
	}
}
