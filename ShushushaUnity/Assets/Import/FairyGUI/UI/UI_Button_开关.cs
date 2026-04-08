/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Button_开关 : GButton
    {
        public struct button
        {
            public const int up = 0;
            public const int down = 1;
            public const int over = 2;
            public const int selectedOver = 3;
        }
        public struct 开关
        {
            public const int 开 = 0;
            public const int 关 = 1;
        }
        public Controller m_开关;
        public const string URL = "ui://56i33xfjpoiuh";

        public static UI_Button_开关 CreateInstance()
        {
            return (UI_Button_开关)UIPackage.CreateObject("UI", "Button_开关");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_开关 = GetControllerAt(1);
            Init();
        }
        partial void Init();
    }
}