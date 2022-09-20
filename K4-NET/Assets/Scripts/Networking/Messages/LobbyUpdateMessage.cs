using Unity.Networking.Transport;

public class LobbyUpdateMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.LOBBY_UPDATE;
		}
	}

	public NetworkMessageType messageType;
	public uint score1;
	public uint score2;
	public string name;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(score1);
		writer.WriteUInt(score2);
		writer.WriteFixedString128(name);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		score1 = reader.ReadUInt();
		score2 = reader.ReadUInt();
		name = reader.ReadFixedString128().ToString();
	}
}