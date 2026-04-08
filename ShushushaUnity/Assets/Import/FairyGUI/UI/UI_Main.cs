/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Main : GComponent
    {
        public GLoader m_mouse_avatar;
        public GTextField m_name;
        public UI_Button_开关 m_说;
        public UI_Button_开关 m_听;
        public GImage m_场景1;
        public GImage m_场景2;
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
            m_说 = (UI_Button_开关)GetChildAt(6);
            m_听 = (UI_Button_开关)GetChildAt(7);
            m_场景1 = (GImage)GetChildAt(12);
            m_场景2 = (GImage)GetChildAt(13);
            Init();
        }
        partial void Init();
    }
}