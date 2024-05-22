using Unity.Networking.Transport;

public class PlayerMoveFailMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.PLAYER_MOVE_FAIL;
		}
	}

	public NetworkMessageType messageType;
	public uint x;
	public uint y;
	public uint moveFailReason;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(x);
		writer.WriteUInt(y);
		writer.WriteUInt(moveFailReason);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		x = reader.ReadUInt();
		y = reader.ReadUInt();
		moveFailReason = reader.ReadUInt();
	}
}