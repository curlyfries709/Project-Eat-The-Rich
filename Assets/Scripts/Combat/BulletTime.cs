using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BulletTime : MonoBehaviour
{
    [SerializeField] float bulletTimeScale = 0.5f;
    [Header("Timers")]
    [SerializeField] float bulletTimeCompleteDepletionTime = 5f;
    [SerializeField] float bulletTimeCompleteRegenerationTime = 2f;
    [Header("UI")]
    [SerializeField] Image bulletTimeBar;
    [SerializeField] Transform bulletTimeHeader;
    [SerializeField] Color bulletTimeRegenColor;

    float focusCurrentValue = 1f;
    float defaultFixedDeltaTime;

    bool coolingDown = false;
    bool enableFocus = false;
    bool isAiming = false;
    bool isDashing = false;

    private void Start()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        bulletTimeHeader.gameObject.SetActive(false);
    }

    private void Update()
    {
        if ((enableFocus || (isDashing && isAiming)) && !coolingDown)
        {
            //Change Time Scale
            Time.timeScale = bulletTimeScale;

            focusCurrentValue = focusCurrentValue - (Time.unscaledDeltaTime / bulletTimeCompleteDepletionTime);
            focusCurrentValue = Mathf.Max(focusCurrentValue, 0);

            if (focusCurrentValue == 0)
                coolingDown = true;
        }
        else if (coolingDown || !enableFocus)
        {
            //Change Time Scale
            Time.timeScale = 1f;

            focusCurrentValue = focusCurrentValue + (Time.unscaledDeltaTime / bulletTimeCompleteRegenerationTime);
            focusCurrentValue = Mathf.Min(focusCurrentValue, 1);

            if (focusCurrentValue == 1)
                coolingDown = false;
        }

        //Always Update Fixed Delta Time
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

        UpdateUI();
    }

    public void Focus(bool enable, bool isAiming)
    {
        enableFocus = enable;
        this.isAiming = isAiming;
    }

    public void SetIsDashing(bool isDashing)
    {
        this.isDashing = isDashing;
    }

    private void UpdateUI()
    {
        bulletTimeBar.fillAmount = focusCurrentValue;

        if (coolingDown)
        {
            bulletTimeBar.color = bulletTimeRegenColor;
        }
        else
        {
            bulletTimeBar.color = Color.white;
        }

        bulletTimeHeader.gameObject.SetActive(focusCurrentValue != 1 && isAiming);
    }
}
