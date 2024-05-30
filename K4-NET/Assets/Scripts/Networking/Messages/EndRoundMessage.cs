using Unity.Networking.Transport;

public class EndRoundMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.END_ROUND;
		}
	}

	public NetworkMessageType messageType;
	public uint winnerId;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(winnerId);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		winnerId = reader.ReadUInt();
	}
}