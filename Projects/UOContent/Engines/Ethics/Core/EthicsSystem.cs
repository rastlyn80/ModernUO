using System;

namespace Server.Ethics;

public class EthicsSystem : GenericEntityPersistence<EthicsEntity>
{
    private static EthicsSystem _ethicSystem;

    public static bool Enabled { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetSetting("ethics.enabled", false);

        if (Enabled)
        {
            _ethicSystem = new EthicsSystem();
        }
    }

    public EthicsSystem() : base("Ethics", 10, 0x1, 0x7FFFFFFF)
    {
    }

    public static Serial NewEthicsEntity => _ethicSystem.NewEntity;

    public static void Add(EthicsEntity entity) => _ethicSystem.AddEntity(entity);

    public static void Remove(EthicsEntity entity) => _ethicSystem.AddEntity(entity);

    public static void Find<T>(Serial serial) where T : EthicsEntity => _ethicSystem.FindEntity<T>(serial);

    public static void Disable()
    {
        if (!Enabled)
        {
            return;
        }

        _ethicSystem.Unregister();
        Enabled = false;
        ServerConfiguration.SetSetting("ethics.enabled", false);
    }

    public static void Enable()
    {
        if (Enabled)
        {
            return;
        }

        _ethicSystem ??= new EthicsSystem();
        _ethicSystem.Register();
        Enabled = true;
        ServerConfiguration.SetSetting("ethics.enabled", true);
    }

    [ManualDirtyChecking]
    [TypeAlias("Server.Ethics.EthicsPersistance")]
    [Obsolete("Deprecated in favor of the static system. Only used for legacy deserialization")]
    public class EthicsPersistence : Item
    {
        [Constructible]
        public EthicsPersistence() : base(1)
        {
            Delete();
        }

        public EthicsPersistence(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            for (var i = 0; i < Ethic.Ethics.Length; ++i)
            {
                Ethic.Ethics[i].Deserialize(reader);
            }

            Timer.DelayCall(Delete);
        }
    }
}
