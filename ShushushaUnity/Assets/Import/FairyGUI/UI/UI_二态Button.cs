/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_二态Button : GButton
    {
        public struct button
        {
            public const int up = 0;
            public const int down = 1;
            public const int over = 2;
            public const int selectedOver = 3;
        }
        public struct 状态
        {
        }
        public Controller m_状态;
        public const string URL = "ui://56i33xfjospas";

        public static UI_二态Button CreateInstance()
        {
            return (UI_二态Button)UIPackage.CreateObject("UI", "二态Button");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_状态 = GetControllerAt(1);
            Init();
        }
        partial void Init();
    }
}