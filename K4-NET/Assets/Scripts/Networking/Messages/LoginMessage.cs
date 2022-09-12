using Unity.Networking.Transport;

public class LoginMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.LOGIN;
		}
	}

	public NetworkMessageType messageType;
	public string username;
	public string password;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// Write message type & object ID
		base.SerializeObject(ref writer);

		writer.WriteFixedString128(username);
		writer.WriteFixedString128(password);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// Read message type & object ID
		base.DeserializeObject(ref reader);

		username = reader.ReadFixedString128().ToString();
		password = reader.ReadFixedString128().ToString();
	}
}