using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class RegisterMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.REGISTER;
		}
	}

	public NetworkMessageType messageType;
	public string username;
	public string password;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// very important to call this first
		base.SerializeObject(ref writer);

		writer.WriteUInt((uint)messageType);
		writer.WriteFixedString128(username);
		writer.WriteFixedString128(password);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// very important to call this first
		base.DeserializeObject(ref reader);

		messageType = (NetworkMessageType)reader.ReadUInt();
		username = reader.ReadFixedString128().ToString();
		password = reader.ReadFixedString128().ToString();
	}
}
