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
	public uint x;
	public uint y;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteUInt(x);
		writer.WriteUInt(y);
}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		x = reader.ReadUInt();
		y = reader.ReadUInt();
	}
}