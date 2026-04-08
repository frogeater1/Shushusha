/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_shark_avatar_lobby : GButton
    {
        public GTextField m_准备;
        public GTextField m_id_in_room;
        public const string URL = "ui://56i33xfjpoiuo";

        public static UI_shark_avatar_lobby CreateInstance()
        {
            return (UI_shark_avatar_lobby)UIPackage.CreateObject("UI", "shark_avatar_lobby");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_准备 = (GTextField)GetChildAt(1);
            m_id_in_room = (GTextField)GetChildAt(2);
            Init();
        }
        partial void Init();
    }
}