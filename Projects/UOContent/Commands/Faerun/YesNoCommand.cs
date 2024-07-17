using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Commands;
using Server.Gumps.Faerun;



namespace Server.Commands.Faerun
{
    public class YesNoCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("YesNo", AccessLevel.Player, new CommandEventHandler(YesNo_OnCommand));
        }

        [Usage("YesNo")]
        [Description("Opens an empty Yes/No gump.")]
        public static void YesNo_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            from.SendGump(new YesNoGump(from));
        }
    }
}
