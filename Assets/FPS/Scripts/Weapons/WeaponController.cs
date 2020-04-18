using UnityEngine;
using UnityEngine.Events;
using weapon;


//[RequireComponent(typeof(AudioSource))]
public class WeaponController : BasicWeapon
{


    void Awake()
    {
        
        m_CurrentAmmo = maxAmmo;
        m_LastMuzzlePosition = weaponMuzzle.position;

        m_ShootAudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this, gameObject);
    }

    void Update()
    {

        UpdateAmmo();
        UpdateCharge();

        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity = (weaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = weaponMuzzle.position;
        }
    }

    protected void UpdateAmmo()
    {
        //if rocket remove rocket
        if (disappearingRocket != null && m_CurrentAmmo >= 1)
            disappearingRocket.SetActive(true);
        if (m_LastTimeShot + ammoReloadDelay < Time.time && m_CurrentAmmo < maxAmmo && !isCharging)
        {
            // reloads weapon over time
            m_CurrentAmmo += ammoReloadRate * Time.deltaTime;

            // limits ammo to max value
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, maxAmmo);

            isCooling = true;
        }
        else
        {
            isCooling = false;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = m_CurrentAmmo / maxAmmo;
        }
    }





    protected void UpdateCharge()
    {
        if (isCharging)
        {
            if (currentCharge < 1f)
            {
                float chargeLeft = 1f - currentCharge;

                // Calculate how much charge ratio to add this frame
                float chargeAdded = 0f;
                if (maxChargeDuration <= 0f)
                {
                    chargeAdded = chargeLeft;
                }
                else
                {
                    chargeAdded = (1f / maxChargeDuration) * Time.deltaTime;
                }

                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                // See if we can actually add this charge
                float ammoThisChargeWouldRequire = chargeAdded * ammoUsageRateWhileCharging;
                if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                {
                    // Use ammo based on charge added
                    UseAmmo(ammoThisChargeWouldRequire);

                    // set current charge ratio
                    currentCharge = Mathf.Clamp01(currentCharge + chargeAdded);
                }
            }
        }
    }


}
