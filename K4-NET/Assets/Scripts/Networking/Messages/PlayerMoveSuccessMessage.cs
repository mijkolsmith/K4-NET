using Unity.Networking.Transport;

public class PlayerMoveSuccessMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.PLAYER_MOVE_SUCCESS;
		}
	}

	public NetworkMessageType messageType;
	public uint activePlayer;
	public uint x;
	public uint y;
	public uint otherHealth;
	public uint playerToMove;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(activePlayer);
		writer.WriteUInt(x);
		writer.WriteUInt(y);
		writer.WriteUInt(otherHealth);
		writer.WriteUInt(playerToMove);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		activePlayer = reader.ReadUInt();
		x = reader.ReadUInt();
		y = reader.ReadUInt();
		otherHealth = reader.ReadUInt();
		playerToMove = reader.ReadUInt();
	}
}