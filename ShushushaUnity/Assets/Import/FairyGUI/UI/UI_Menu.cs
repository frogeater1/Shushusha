/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_Menu : GComponent
    {
        public GList m_menu;
        public const string URL = "ui://56i33xfjv3snt";

        public static UI_Menu CreateInstance()
        {
            return (UI_Menu)UIPackage.CreateObject("UI", "Menu");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_menu = (GList)GetChildAt(0);
            Init();
        }
        partial void Init();
    }
}