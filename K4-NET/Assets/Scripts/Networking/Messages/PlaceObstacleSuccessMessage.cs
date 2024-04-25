using Unity.Networking.Transport;

public class PlaceObstacleSuccessMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.PLACE_OBSTACLE_SUCCESS;
		}
	}

	public NetworkMessageType messageType;
	public uint removal;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(removal);
}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		removal = reader.ReadUInt();
	}
}