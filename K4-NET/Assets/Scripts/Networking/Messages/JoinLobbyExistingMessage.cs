using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class JoinLobbyExistingMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.JOIN_LOBBY_EXISTING;
		}
	}

	public NetworkMessageType messageType;
	public int score1;
	public int score2;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// very important to call this first
		base.SerializeObject(ref writer);

		writer.WriteUInt((uint)messageType);
		writer.WriteUInt((uint)score1);
		writer.WriteUInt((uint)score2);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// very important to call this first
		base.DeserializeObject(ref reader);

		messageType = (NetworkMessageType)reader.ReadUInt();
		score1 = (int) reader.ReadUInt();
		score2 = (int) reader.ReadUInt();
	}
}
