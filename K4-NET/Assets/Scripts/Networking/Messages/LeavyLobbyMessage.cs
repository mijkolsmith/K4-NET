using Unity.Networking.Transport;

public class LeaveLobbyMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.LEAVE_LOBBY;
		}
	}

	public NetworkMessageType messageType;
	public string name;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteFixedString128(name);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		name = reader.ReadFixedString128().ToString();
	}
}