using UnityEngine;

public class Indicator : MonoBehaviour
{
    private void OnMouseDown()
    {
        Game.Instance.OnIndicatorClicked(gameObject);
    }
}
