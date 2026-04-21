/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Tip : GComponent
    {
        public GButton m_close;
        public GButton m_confirm;
        public GTextField m_content;
        public const string URL = "ui://56i33xfjo6fsq";

        public static UI_Tip CreateInstance()
        {
            return (UI_Tip)UIPackage.CreateObject("UI", "Tip");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_close = (GButton)GetChildAt(1);
            m_confirm = (GButton)GetChildAt(2);
            m_content = (GTextField)GetChildAt(3);
            Init();
        }
        partial void Init();
    }
}