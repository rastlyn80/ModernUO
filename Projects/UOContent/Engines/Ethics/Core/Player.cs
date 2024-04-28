using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Ethics;

[PropertyObject]
[SerializationGenerator(0)]
public partial class Player : EthicsEntity
{
    [SerializableField(0)]
    private Mobile _mobile;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private int _power;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private int _history;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private Mobile _steed;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private Mobile _familiar;

    [SerializableField(5)]
    private DateTime _shield;

    [SerializableField(6)]
    private Ethic _ethic;

    public Player(Ethic ethic, Mobile mobile)
    {
        Ethic = ethic;
        _mobile = mobile;

        Power = 5;
        History = 5;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsShielded
    {
        get
        {
            if (_shield == DateTime.MinValue)
            {
                return false;
            }

            if (Core.Now < _shield + TimeSpan.FromHours(1.0))
            {
                return true;
            }

            FinishShield();
            return false;
        }
    }

    public static Player Find(Mobile mob) => Find(mob, false);

    public static Player Find(Mobile mob, bool inherit)
    {
        var pm = mob as PlayerMobile;

        if (pm == null)
        {
            if (inherit && mob is BaseCreature bc)
            {
                if (bc.Controlled)
                {
                    pm = bc.ControlMaster as PlayerMobile;
                }
                else if (bc.Summoned)
                {
                    pm = bc.SummonMaster as PlayerMobile;
                }
            }

            if (pm == null)
            {
                return null;
            }
        }

        var pl = pm.EthicPlayer;

        if (pl?.Ethic.IsEligible(pl.Mobile) == false)
        {
            pm.EthicPlayer = pl = null;
        }

        return pl;
    }

    public void BeginShield() => _shield = Core.Now;

    public void FinishShield() => _shield = DateTime.MinValue;

    public void CheckAttach()
    {
        if (Ethic.IsEligible(Mobile))
        {
            Attach();
        }
    }

    public void Attach()
    {
        if (Mobile is PlayerMobile mobile)
        {
            mobile.EthicPlayer = this;
        }

        Ethic.AddToPlayers(this);
    }

    public void Detach()
    {
        if (Mobile is PlayerMobile mobile)
        {
            mobile.EthicPlayer = null;
        }

        Ethic.RemoveFromPlayers(this);
    }
}
