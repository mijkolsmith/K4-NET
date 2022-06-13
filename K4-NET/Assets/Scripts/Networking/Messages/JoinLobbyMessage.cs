using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class JoinLobbyMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.JOIN_LOBBY;
		}
	}

	public NetworkMessageType messageType;
	public string name;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// very important to call this first
		base.SerializeObject(ref writer);

		writer.WriteUInt((uint)messageType);
		writer.WriteFixedString128(name);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// very important to call this first
		base.DeserializeObject(ref reader);

		messageType = (NetworkMessageType)reader.ReadUInt();
		name = reader.ReadFixedString128().ToString();
	}
}
