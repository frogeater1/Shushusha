/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Lobby : GComponent
    {
        public struct Page
        {
            public const int 门外 = 0;
            public const int 房间 = 1;
        }
        public Controller m_Page;
        public GButton m_准备;
        public GList m_memberlist;
        public GTextField m_房间号;
        public GButton m_开始;
        public GButton m_创建;
        public GButton m_加入;
        public GTextInput m_房间号输入;
        public GTextField m_waiting;
        public const string URL = "ui://56i33xfjpoiun";

        public static UI_Lobby CreateInstance()
        {
            return (UI_Lobby)UIPackage.CreateObject("UI", "Lobby");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_Page = GetControllerAt(0);
            m_准备 = (GButton)GetChildAt(0);
            m_memberlist = (GList)GetChildAt(1);
            m_房间号 = (GTextField)GetChildAt(3);
            m_开始 = (GButton)GetChildAt(4);
            m_创建 = (GButton)GetChildAt(6);
            m_加入 = (GButton)GetChildAt(7);
            m_房间号输入 = (GTextInput)GetChildAt(9);
            m_waiting = (GTextField)GetChildAt(11);
            Init();
        }
        partial void Init();
    }
}