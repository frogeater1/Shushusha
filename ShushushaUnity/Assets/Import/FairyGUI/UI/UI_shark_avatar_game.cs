/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_shark_avatar_game : GButton
    {
        public GImage m_喇叭;
        public const string URL = "ui://56i33xfjpoiul";

        public static UI_shark_avatar_game CreateInstance()
        {
            return (UI_shark_avatar_game)UIPackage.CreateObject("UI", "shark_avatar_game");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_喇叭 = (GImage)GetChildAt(1);
            Init();
        }
        partial void Init();
    }
}