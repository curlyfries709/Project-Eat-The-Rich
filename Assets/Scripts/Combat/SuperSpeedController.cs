using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SuperSpeedController : MonoBehaviour
{
    [Header("Timers")]
    [SerializeField] float speedBoostCompleteDepletionTime = 5f;
    [SerializeField] float speedBoostCompleteRegenerationTime = 5f;
    [Header("UI")]
    [SerializeField] Image superSpeerBar;
    [SerializeField] Transform superSpeedHeader;
    [SerializeField] Color superSpeedBarRegenColor;

    float speedBoostCurrentValue = 1f;
    bool coolingDown = false;

    //Cache
    BulletTime bulletTime;

    private void Start()
    {
        bulletTime = GetComponent<BulletTime>();
        superSpeedHeader.gameObject.SetActive(false);
    }

    public void Dash(bool canDash)
    {
       if(canDash && !coolingDown)
       {
            bulletTime.SetIsDashing(true);

            speedBoostCurrentValue = speedBoostCurrentValue - (Time.unscaledDeltaTime / speedBoostCompleteDepletionTime);
            speedBoostCurrentValue = Mathf.Max(speedBoostCurrentValue, 0);

            if (speedBoostCurrentValue == 0)
                coolingDown = true;
       }
       else if(!canDash)
       {
            bulletTime.SetIsDashing(false);

            speedBoostCurrentValue = speedBoostCurrentValue + (Time.unscaledDeltaTime / speedBoostCompleteRegenerationTime);
            speedBoostCurrentValue = Mathf.Min(speedBoostCurrentValue, 1);

            if (speedBoostCurrentValue == 1)
                coolingDown = false;
       }

        UpdateUI();
    }

    private void UpdateUI()
    {
        superSpeerBar.rectTransform.localScale = new Vector3(speedBoostCurrentValue, 1, 1);

        if (coolingDown)
        {
            superSpeerBar.color = superSpeedBarRegenColor;
        }
        else
        {
            superSpeerBar.color = Color.white;
        }

        superSpeedHeader.gameObject.SetActive(speedBoostCurrentValue != 1);
    }

    public bool IsSpeedBoostAvailable()
    {
        return !coolingDown;
    }

}
