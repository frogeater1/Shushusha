using UnityEngine;

namespace UI
{
    public partial class UI_Main
    {
        partial void Init()
        {
            Game.Instance.uiMain = this;
            visible = false;
            Debug.Log("main init");
            m_听.Bind();
            m_说.Bind();
        }
    }
}