using Unity.Networking.Transport;

public class RegisterSuccessMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.REGISTER_SUCCESS;
		}
	}

	public NetworkMessageType messageType;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);
	}
}