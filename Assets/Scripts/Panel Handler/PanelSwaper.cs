using System.Collections.Generic;
using UnityEngine;

namespace SteamLobbyN
{
    public class PanelSwaper : MonoBehaviour
    {
        public List<Panel> _Panels = new List<Panel>();

        public void SwapPanel(string _panelName)
        {
            foreach (Panel panel in _Panels)
            {
                if (panel._PanelName == _panelName)
                {
                    panel.gameObject.SetActive(true);
                }
                else
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }
    }
}