using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;

    //input
    Vector2 viewInput;

    //rotation
    float cameraRotationX = 0;
    float cameraRotationY = 0;

    //other components
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    public Camera localCamera;

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponentInParent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponent<Camera>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        cameraRotationX = GameManager.instance.cameraViewRotation.x;
        cameraRotationY = GameManager.instance.cameraViewRotation.y;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (cameraAnchorPoint == null)
            return;

        if (!localCamera.enabled)
            return;

        if (cinemachineVirtualCamera == null)
            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        else
        {
            if (NetworkPlayer.local.is3rdPersonCamera)
            {
                if (!cinemachineVirtualCamera.enabled)
                {

                    cinemachineVirtualCamera.enabled = true;
                }

                cinemachineVirtualCamera.Follow = NetworkPlayer.local.playerModel;
                cinemachineVirtualCamera.LookAt = NetworkPlayer.local.playerModel;

                //let the camera be handled by cinemachine
                return;
            }
            else
            {
                //do nothing, bool will not switch
            }
        }

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

    private void OnDestroy()
    {
        if (cameraRotationX != 0 && cameraRotationY != 0)
        {
            GameManager.instance.cameraViewRotation.x = cameraRotationX;
            GameManager.instance.cameraViewRotation.y = cameraRotationY;

        }
    }
}
