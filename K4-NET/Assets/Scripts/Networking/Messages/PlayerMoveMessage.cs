using Unity.Networking.Transport;

public class PlayerMoveMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.PLAYER_MOVE;
		}
	}

	public NetworkMessageType messageType;
	public string name;
	public uint x;
	public uint y;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteFixedString128(name);
		writer.WriteUInt(x);
		writer.WriteUInt(y);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		name = reader.ReadFixedString128().ToString();
		x = reader.ReadUInt();
		y = reader.ReadUInt();
	}
}