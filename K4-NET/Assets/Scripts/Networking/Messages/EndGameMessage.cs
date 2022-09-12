using Unity.Networking.Transport;

public class EndGameMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.END_GAME;
		}
	}

	public NetworkMessageType messageType;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);
	}
}