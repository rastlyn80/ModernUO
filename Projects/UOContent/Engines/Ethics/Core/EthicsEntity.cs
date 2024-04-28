using System;
using ModernUO.Serialization;

namespace Server.Ethics;

[SerializationGenerator(0)]
public partial class EthicsEntity : ISerializable
{
    public DateTime Created { get; set; } = Core.Now;
    public long SavePosition { get; set; }
    public BufferWriter SaveBuffer { get; set; }
    public Serial Serial { get; }

    public bool Deleted { get; private set; }

    public EthicsEntity()
    {
        Serial = EthicsSystem.NewEthicsEntity;
        EthicsSystem.Add(this);
    }

    public void Delete()
    {
        Deleted = true;
    }
}
