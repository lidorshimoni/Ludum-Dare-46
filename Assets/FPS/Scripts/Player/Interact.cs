using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour
{
    [Header("References")]
    public PlayerInputHandler playerInputHandler;
    public GameObject cameraGameObject;

    [Header("preferences")]
    public float interactionMaxDistance = 3;
    public float throwForce = 3000;
    public float grabDelay = 0.2f;
    public float GrabbingDistance = 1f;

    public bool isGrabbed { get; private set; }
    public bool canShoot;

    public GameObject grabbedObject;
    public Transform grabbedObjectInitialPosition;


    // Start is called before the first frame update
    void Start()
    {
        canShoot = true;
        playerInputHandler = gameObject.GetComponent<PlayerInputHandler>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!isGrabbed && playerInputHandler.GetInteractInputHeld())
            TryToInteact();

        else if (isGrabbed)
        {
            //if want to throw
            if (playerInputHandler.GetFireInputDown())
            {
                grabbedObject.GetComponent<Rigidbody>().AddForce(cameraGameObject.transform.forward * throwForce);
                StartCoroutine(StopGrabbing());
            }

            //if want to let go
            else if (playerInputHandler.GetAimInputHeld())
                StartCoroutine(StopGrabbing());
            else
                Grab();
        }
    }

    Interactable IsInteractable(GameObject obj)
    {
        return obj.GetComponent<Interactable>();
    }

    bool IsGrabbable(GameObject obj)
    {
        return obj.GetComponent<Rigidbody>() != null && obj.GetComponent<Renderer>() != null;
    }

    void TryToInteact()
    {
        GameObject obj;
        if(null == (obj =  GetTargetedObject()))
            return;

        Interactable it;

        // try to do interaction
        if (null != (it = IsInteractable(obj)))
        {
            it.DoInteraction(gameObject);
            return; 
        }

        //try to grab
        else if(IsGrabbable(obj))
        {
            isGrabbed = true;
            canShoot = false;
            grabbedObject = obj;
            grabbedObjectInitialPosition = grabbedObject.transform;
            Grab();
        }
    }

    void DoInteraction()
    {

    }

    public GameObject GetTargetedObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraGameObject.transform.position, cameraGameObject.transform.forward, out hit, interactionMaxDistance))
            return hit.collider.gameObject;
        else
            return null;
    }

    void Grab()
    {
        if (grabbedObject == null)
            StartCoroutine(StopGrabbing());
        else
        {
            grabbedObject.GetComponent<Rigidbody>().useGravity = false;
            grabbedObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            float grabbedObjectSize = grabbedObject.GetComponent<Renderer>().bounds.size.magnitude;
            grabbedObject.transform.SetPositionAndRotation(cameraGameObject.transform.position + cameraGameObject.transform.forward * (grabbedObjectSize + GrabbingDistance), grabbedObjectInitialPosition.rotation);
        }
    }


    IEnumerator StopGrabbing()
    {
        grabbedObject.GetComponent<Rigidbody>().useGravity = true;
        isGrabbed = false;
        yield return new WaitForSeconds(grabDelay);
        canShoot = true;
    }
}
