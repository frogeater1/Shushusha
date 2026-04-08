/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace UI
{
    public partial class UI_魔力值条 : GComponent
    {
        public struct 魔力值
        {
        }
        public Controller m_魔力值;
        public const string URL = "ui://56i33xfjpoiuk";

        public static UI_魔力值条 CreateInstance()
        {
            return (UI_魔力值条)UIPackage.CreateObject("UI", "魔力值条");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_魔力值 = GetControllerAt(0);
            Init();
        }
        partial void Init();
    }
}