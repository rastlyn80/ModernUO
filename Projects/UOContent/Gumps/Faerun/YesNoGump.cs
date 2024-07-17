using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Network;
using Server.Gumps;
using Server;
using Server.Commands;

namespace Server.Gumps.Faerun
{
    public class YesNoGump : Gump
    {
        private Mobile m_From;

        public YesNoGump(Mobile from) : base(0, 0)
        {

            m_From = from;

            Closable = true;
            Disposable = true;
            // Dragable = true;
            Resizable = false;

            AddPage(0);
            AddImage(210, 100, 11265);
            AddImage(410, 100, 11264);
            AddImage(1080, 100, 11265);
            AddLabel(620, 110, 0, "WELCOME ADVENTURER");
            AddBackground(530, 240, 429, 395, 9300);
            AddImage(1100, 130, 1634);
            AddImage(230, 130, 1632);
            AddLabel(640, 150, 5, "CHOOSE YOUR FATE");
            AddPage(1);
            AddLabel(660, 250, 36, "Choose your Race");
            AddLabel(610, 305, 56, "Dragonborn");
            AddLabel(610, 335, 56, "Dwarf");
            AddLabel(610, 365, 56, "Elf");
            AddBackground(770, 360, 150, 152, 1460);
            AddLabel(610, 395, 56, "Gnome");
            AddLabel(610, 425, 56, "Half-Elf");
            AddLabel(610, 455, 56, "Half-Orc");
            AddLabel(610, 485, 56, "Halfling");
            AddLabel(610, 515, 56, "Human");
            AddLabel(610, 545, 56, "Tiefling");
            AddRadio(580, 310, 30008, 30009, true, (int)Switches.RadioButton1);
            AddRadio(580, 340, 30008, 30009, false, (int)Switches.RadioButton2);
            AddRadio(580, 370, 30008, 30009, false, (int)Switches.RadioButton3);
            AddRadio(580, 400, 30008, 30009, false, (int)Switches.RadioButton4);
            AddRadio(580, 430, 30008, 30009, false, (int)Switches.RadioButton5);
            AddRadio(580, 460, 30008, 30009, false, (int)Switches.RadioButton6);
            AddRadio(580, 490, 30008, 30009, false, (int)Switches.RadioButton7);
            AddRadio(580, 520, 30008, 30009, false, (int)Switches.RadioButton8);
            AddRadio(580, 550, 30008, 30009, false, (int)Switches.RadioButton9999999999999999999999999999999999);
            AddImage(790, 380, 2731);
            AddButton(860, 600, 12009, 12010, (int)Buttons.RaceOK1, GumpButtonType.Reply, 0);



            /*

            AddPage(0);
            AddBackground(0, 0, 200, 150, 9270);
            AddAlphaRegion(10, 10, 180, 130);

            AddLabel(50, 20, 1152, "Would you like to proceed?");
            AddButton(50, 100, 247, 248, 1, GumpButtonType.Reply, 0); // Yes Button
            AddButton(100, 100, 241, 242, 2, GumpButtonType.Reply, 0); // No Button
            */
        }

        public enum Switches
        {
            RadioButton1 = 1,
            RadioButton2 = 2,
            RadioButton3 = 3,
            RadioButton4 = 4,
            RadioButton5 = 5,
            RadioButton6 = 6,
            RadioButton7 = 7,
            RadioButton8 = 8,
            RadioButton9999999999999999999999999999999999 = 9,
            RadioButton91910 = 10,
            RadioButton91911 = 11,
            RadioButton91912 = 12,
            RadioButton91913 = 13,
            RadioButton91914 = 14,
            RadioButton91915 = 15,
            RadioButton91916 = 16,
            RadioButton91917 = 17,
            RadioButton9999999191919191919191919191919191919191919191919191919191919 = 18,
            RadioButton91918 = 19,
            RadioButton920 = 20,
            RadioButton999999921 = 21,
        }

        public enum Buttons
        {
            RaceOK1 = 1,
            ClassOK2 = 2,
            ClassBACK4 = 3,
            Button6 = 4,
            Button7 = 5,
            Button8 = 6,
            Button910 = 7,
            Button911 = 8,
            Button9917 = 9,
            Button912 = 10,
            Button913 = 11,
            Button914 = 12,
            Button915 = 13,
            Button916 = 14,
            Button9 = 15,
            ClassOK3 = 16,
            ClassBACK5 = 17,
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            Mobile from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 1: // Yes
                    from.SendMessage("You chose Yes.");
                    break;
                case 2: // No
                    from.SendMessage("You chose No.");
                    break;
            }
        }
    }
}
