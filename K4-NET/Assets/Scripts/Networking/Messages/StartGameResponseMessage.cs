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
	public uint activePlayer;
	public uint itemId;


	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(activePlayer);
		writer.WriteUInt(itemId);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		activePlayer = reader.ReadUInt();
		itemId = reader.ReadUInt();
	}
}