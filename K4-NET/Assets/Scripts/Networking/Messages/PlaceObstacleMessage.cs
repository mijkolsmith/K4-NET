using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class PlaceObstacleMessage : MessageHeader
{
	public override NetworkMessageType Type
	{
		get
		{
			return NetworkMessageType.PLACE_OBSTACLE;
		}
	}

	public NetworkMessageType messageType;
	public string message;

	public override void SerializeObject(ref DataStreamWriter writer)
	{
		// very important to call this first
		base.SerializeObject(ref writer);

		writer.WriteUInt((uint)messageType);
		writer.WriteFixedString128(message);
	}

	public override void DeserializeObject(ref DataStreamReader reader)
	{
		// very important to call this first
		base.DeserializeObject(ref reader);

		messageType = (NetworkMessageType)reader.ReadUInt();
		message = reader.ReadFixedString128().ToString();
	}
}
