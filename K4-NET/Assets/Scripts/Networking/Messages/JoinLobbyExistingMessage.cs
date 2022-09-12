using Unity.Networking.Transport;

public class JoinLobbyExistingMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.JOIN_LOBBY_EXISTING;
		}
	}

	public NetworkMessageType messageType;
	public uint score1;
	public uint score2;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(score1);
		writer.WriteUInt(score2);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		score1 = reader.ReadUInt();
		score2 = reader.ReadUInt();
	}
}