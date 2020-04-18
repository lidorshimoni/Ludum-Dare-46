using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace weapon
{

    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,

    }

    [System.Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite crosshairSprite;
        [Tooltip("The size of the crosshair image")]
        public int crosshairSize;
        [Tooltip("The color of the crosshair image")]
        public Color crosshairColor;
    }

    public class BasicWeapon : MonoBehaviour
    {

        [Header("Information")]
        [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string weaponName;
        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite weaponIcon;


        [Tooltip("Default data for the crosshair")]
        public CrosshairData crosshairDataDefault;
        [Tooltip("Data for the crosshair when targeting an enemy")]
        public CrosshairData crosshairDataTargetInSight;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject weaponRoot;
        [Tooltip("Tip of the weapon, where the projectiles are shot")]
        public Transform weaponMuzzle;

        [Header("Shoot Parameters")]
        [Tooltip("The type of weapon wil affect how it shoots")]
        public WeaponShootType shootType;
        [Tooltip("The projectile prefab")]
        public ProjectileBase projectilePrefab;
        [Tooltip("Minimum duration between two shots")]
        public float delayBetweenShots = 0.5f;
        [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
        public float bulletSpreadAngle = 0f;
        [Tooltip("Amount of bullets per shot")]
        public int bulletsPerShot = 1;
        [Tooltip("Force that will push back the weapon after each shot")]
        [Range(0f, 2f)]
        public float recoilForce = 1;
        [Tooltip("Ratio of the default FOV that this weapon applies while aiming")]
        [Range(0f, 1f)]
        public float aimZoomRatio = 1f;
        [Tooltip("Translation to apply to weapon arm when aiming with this weapon")]
        public Vector3 aimOffset;

        [Header("Ammo Parameters")]
        [Tooltip("Amount of ammo reloaded per second")]
        public float ammoReloadRate = 1f;

        [Tooltip("wheter press R to reload or aut oreload")]
        public bool AutomaticReload = true;

        [Tooltip("Delay after the last shot before starting to reload")]
        public float ammoReloadDelay = 2f;
        [Tooltip("Maximum amount of ammo in the gun")]
        public float maxAmmo = 8;

        [Header("Charging parameters (charging weapons only)")]
        [Tooltip("Trigger a shot when maximum charge is reached")]
        public bool automaticReleaseOnCharged;
        [Tooltip("Duration to reach maximum charge")]
        public float maxChargeDuration = 2f;
        [Tooltip("Initial ammo used when starting to charge")]
        public float ammoUsedOnStartCharge = 1f;
        [Tooltip("Additional ammo used when charge reaches its maximum")]
        public float ammoUsageRateWhileCharging = 1f;

        [Header("Audio & Visual")]
        [Tooltip("Optional weapon animator for OnShoot animations")]
        public Animator weaponAnimator;
        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip changeWeaponSFX;
        [Tooltip("Prefab of the muzzle flash")]
        public GameObject muzzleFlashPrefab;
        [Tooltip("Unparent the muzzle flash instance on spawn")]
        public bool unparentMuzzleFlash;
        [Tooltip("sound played when shooting")]
        public AudioClip shootSFX;
        public Transform SpawnPositionCardridge;
        public GameObject Cardridge;
        public Vector3 cardridgeScale;

        public GameObject disappearingRocket;

        public UnityAction onShoot;

        protected float m_CurrentAmmo;
        protected float m_LastTimeShot = Mathf.NegativeInfinity;
        protected float m_TimeBeginCharge;
        protected Vector3 m_LastMuzzlePosition;

        public GameObject owner { get; set; }
        public GameObject sourcePrefab { get; set; }
        public bool isCharging { get; protected set; }
        public float currentAmmoRatio { get; protected set; }
        public bool isWeaponActive { get; protected set; }
        public bool isCooling { get; protected set; }
        public float currentCharge { get; protected set; }
        public Vector3 muzzleWorldVelocity { get; protected set; }
        public float GetAmmoNeededToShoot() => (shootType != WeaponShootType.Charge ? 1 : ammoUsedOnStartCharge) / maxAmmo;

        protected AudioSource m_ShootAudioSource;

        const string k_AnimAttackParameter = "Attack";



        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);

            if (show && changeWeaponSFX)
            {
                m_ShootAudioSource.PlayOneShot(changeWeaponSFX);
            }

            isWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
            m_LastTimeShot = Time.time;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {

            switch (shootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    return false;

                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }
                    return false;

                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }
                    // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                    if (inputUp || (automaticReleaseOnCharged && currentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }
                    return false;

                default:
                    return false;
            }
        }
        bool TryBeginCharge()
        {
            if (!isCharging
                && m_CurrentAmmo >= ammoUsedOnStartCharge
                && m_LastTimeShot + delayBetweenShots < Time.time)
            {
                UseAmmo(ammoUsedOnStartCharge);
                isCharging = true;

                return true;
            }

            return false;
        }

        bool TryReleaseCharge()
        {
            if (isCharging)
            {
                HandleShoot();

                currentCharge = 0f;
                isCharging = false;

                return true;
            }
            return false;
        }
        bool TryShoot()
        {
            if (m_CurrentAmmo >= 1f && m_LastTimeShot + delayBetweenShots < Time.time)
            {
                HandleShoot();
                m_CurrentAmmo -= 1;

                return true;
            }

            return false;
        }

        protected void HandleShoot()
        {

            //if rocket remove rocket
            if (disappearingRocket != null)
                disappearingRocket.SetActive(false);


            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(weaponMuzzle);
                ProjectileBase newProjectile = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // muzzle flash
            if (muzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(muzzleFlashPrefab, weaponMuzzle.position, weaponMuzzle.rotation, weaponMuzzle.transform);
                // Unparent the muzzleFlashInstance
                if (unparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            //cartridge vfx
            if (Cardridge && SpawnPositionCardridge != null)
            {

                GameObject cardiridgeInstance = Instantiate(Cardridge, SpawnPositionCardridge.position, SpawnPositionCardridge.rotation, SpawnPositionCardridge.transform);
                cardiridgeInstance.transform.localScale = cardridgeScale;
                Destroy(cardiridgeInstance, 2f);
            }

            m_LastTimeShot = Time.time;

            // play shoot SFX
            if (shootSFX)
            {
                m_ShootAudioSource.PlayOneShot(shootSFX);
            }

            // Trigger attack animation if there is any
            if (weaponAnimator)
            {
                weaponAnimator.SetTrigger(k_AnimAttackParameter);
            }

            // Callback on shoot
            if (onShoot != null)
            {
                onShoot();
            }
        }



        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
}
