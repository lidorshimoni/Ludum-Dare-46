using UnityEngine;
using UnityEngine.UI;

public class StanceHUD : MonoBehaviour
{
    [Tooltip("Image component for the stance sprites")]
    public Image stanceImage;
    [Tooltip("Sprite to display when standing")]
    public Sprite standingSprite;
    [Tooltip("Sprite to display when crouching")]
    public Sprite crouchingSprite;
    [Tooltip("Sprite to display when sprinting")]
    public Sprite sprintingSprite;
    [Tooltip("Sprite to display when sliding")]
    public Sprite slidingSprite;
    [Tooltip("Sprite to display when wallrunning")]
    public Sprite wallrunSprite;
    [Tooltip("Sprite to display when jumping")]
    public Sprite jumpSprite;

    public enum Stance
    {
        Standing = 0,
        Sprinting,
        Crouching,
        Sliding,
        Wallrun,
        Jumping
    }

    private void Start()
    {
        //PlayerCharacterController character = FindObjectOfType<PlayerCharacterController>();
        //DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, StanceHUD>(character, this);
        //character.onStanceChanged += OnStanceChanged;

        //OnStanceChanged(character.isCrouching);
    }

    public void OnStanceChanged(int stance)
    {
        switch (stance)
        {
            case (int)Stance.Standing:
                stanceImage.sprite = standingSprite;
                break;
            case (int)Stance.Sprinting:
                stanceImage.sprite = sprintingSprite;
                break;
            case (int)Stance.Crouching:
                stanceImage.sprite = crouchingSprite;
                break;
            case (int)Stance.Sliding:
                stanceImage.sprite = slidingSprite;
                break;
            case (int)Stance.Wallrun:
                stanceImage.sprite = wallrunSprite;
                break;
            case (int)Stance.Jumping:
                stanceImage.sprite = jumpSprite;
                break;
        }
    }

}
