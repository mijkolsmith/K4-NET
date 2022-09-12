using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Networking.Transport;

public abstract class MessageHeader
{
	private static uint nextID = 0;
	public static uint NextID => ++nextID;
	public abstract NetworkMessageType Type { get; }
	public uint ID { get; private set; } = NextID;

	public virtual void SerializeObject(ref DataStreamWriter writer)
	{
		// Write a UShort which is the message type
		writer.WriteUShort((ushort)Type);

		// Write a UInt which is the object ID
		writer.WriteUInt(ID);
	}

	public virtual void DeserializeObject(ref DataStreamReader reader)
	{
		// Read a UInt which is the object ID
		ID = reader.ReadUInt();
	}
}