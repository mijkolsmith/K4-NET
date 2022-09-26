using Unity.Networking.Transport;

public class StartGameResponseMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.START_GAME_RESPONSE;
		}
	}

	public NetworkMessageType messageType;
	public uint startPlayer;
	public uint itemId;


	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(startPlayer);
		writer.WriteUInt(itemId);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		startPlayer = reader.ReadUInt();
		itemId = reader.ReadUInt();
	}
}