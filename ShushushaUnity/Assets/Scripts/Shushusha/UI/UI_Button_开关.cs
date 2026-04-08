using UnityEngine;

namespace UI
{
    public partial class UI_Button_开关
    {
        public void Bind()
        {
            onClick.Set(() => { m_开关.selectedIndex = m_开关.selectedIndex == 开关.关 ? 开关.开 : 开关.关; });
        }
    }
}