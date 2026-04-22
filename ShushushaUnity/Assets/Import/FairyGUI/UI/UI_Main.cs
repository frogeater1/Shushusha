/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Main : GComponent
    {
        public GLoader m_mouse_avatar;
        public GTextField m_name;
        public GTextField m_stage;
        public GTextField m_round;
        public GButton m_技能;
        public GButton m_确定;
        public UI_Button_开关 m_说;
        public UI_Button_开关 m_听;
        public GTextField m_倒计时;
        public const string URL = "ui://56i33xfjj62k0";

        public static UI_Main CreateInstance()
        {
            return (UI_Main)UIPackage.CreateObject("UI", "Main");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mouse_avatar = (GLoader)GetChildAt(0);
            m_name = (GTextField)GetChildAt(1);
            m_stage = (GTextField)GetChildAt(2);
            m_round = (GTextField)GetChildAt(3);
            m_技能 = (GButton)GetChildAt(4);
            m_确定 = (GButton)GetChildAt(5);
            m_说 = (UI_Button_开关)GetChildAt(6);
            m_听 = (UI_Button_开关)GetChildAt(7);
            m_倒计时 = (GTextField)GetChildAt(12);
            Init();
        }
        partial void Init();
    }
}