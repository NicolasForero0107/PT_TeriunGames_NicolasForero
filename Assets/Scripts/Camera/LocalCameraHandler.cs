using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;

    //input
    Vector2 viewInput;

    //rotation
    float cameraRotationX = 0;
    float cameraRotationY = 0;

    //other components
    //Cinemachine.CinemachineVirtualCamera localCamera; will be used later
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    Camera localCamera;

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponent<Camera>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        //detach camera if enabled
        if (localCamera.enabled)
            localCamera.transform.parent = null;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (cameraAnchorPoint == null)
            return;

        if (!localCamera.enabled)
            return;

        //move cam to position of the player
        localCamera.transform.position = cameraAnchorPoint.position;

        //calculate rotation
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -40, 40);

        cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCustom.rotationSpeed;

        //apply rotation
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }
}
