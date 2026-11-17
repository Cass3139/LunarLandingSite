using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    public static CameraControl Instance;
    [SerializeField] private float mouseSensivity = 30f;
    [SerializeField] private float yCenterPosOffset = 10f;
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float moveSpeed;

    
    private static float _yRotation;
    private static float _xRotation;
    public static bool isCameraLocked = true;
    
    private InputAction _lookDeltaInput, _rotateCameraInput, _scrollMovementInput, _wasdMovementInput, _jumpInput,_crouchInput;




    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _lookDeltaInput = InputSystem.actions.FindAction("Look");
        _rotateCameraInput = InputSystem.actions.FindAction("RightClick");
        _scrollMovementInput = InputSystem.actions.FindAction("Scroll");
        _wasdMovementInput = InputSystem.actions.FindAction("Move");
        _jumpInput = InputSystem.actions.FindAction("Jump");
        _crouchInput = InputSystem.actions.FindAction("Crouch");

        _xRotation = transform.rotation.eulerAngles.x;
        _yRotation = transform.rotation.eulerAngles.y;
        
    }

    void Update()
    {
        
        if(isCameraLocked) return;
        
        //Camera movement : Scroll
        float scroll =_scrollMovementInput.ReadValue<float>();
        transform.Translate(scroll*zoomSpeed*Time.deltaTime*Vector3.forward);

        //Camera movement : WASD
        Vector2 wasdInput = _wasdMovementInput.ReadValue<Vector2>();
        Vector3 movement = wasdInput.y*Vector3.forward - wasdInput.x*Vector3.left;
        transform.Translate(moveSpeed*Time.deltaTime*movement);

        float verticalInput = _jumpInput.ReadValue<float>() - _crouchInput.ReadValue<float>();
        transform.Translate(moveSpeed*Time.deltaTime*verticalInput*Vector3.up, Space.World);





        if (!_rotateCameraInput.IsPressed())
        {
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        float mouseX = _lookDeltaInput.ReadValue<Vector2>().x * mouseSensivity * Time.deltaTime;
        float mouseY = _lookDeltaInput.ReadValue<Vector2>().y  * mouseSensivity * Time.deltaTime;
        
        _xRotation += mouseX;
        _yRotation -= mouseY;

        transform.rotation = Quaternion.Euler(_yRotation, _xRotation, 0f);
    }

    public void SetCenterLanding(TerrainData landingTerrain)
    {
        Vector3 centerPos = new Vector3(
            landingTerrain.size.x/2,
            landingTerrain.size.y + yCenterPosOffset,
            landingTerrain.size.z/2
        );

        transform.position = centerPos;
    }
}
