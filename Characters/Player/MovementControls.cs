using UnityEngine;
using System.Collections.Generic;

public class MovementControls : MonoBehaviour
{
    // Parameters for player movement.
    public float turnSpeed = 3f;
    public float turnSmoothing = 1f;

    // Parameters for camera movement.
    public float smoothing = 1.5f;
    public float mouseSensitivity = 1f;
    public Vector3 startingPosition = new Vector3(999, 999, 999);

    // Public facing values for player movement.
    public float playerSpeed;
    public float playerRotation;
    public float angularSpeed;

    // Private variables for player movement.
    private float maxSpeed;
    private bool strafing = true;

    //Private values for camera movement.
    private float mouseTurnBuffer;
    private Vector2 mouseDir;
    private Quaternion cameraDir;
    private float cameraDist;

    private enum Action
    {
        None,
        StrafeLeft,
        StrafeRight,
        Crouch
    }
    private Action nextAction = Action.None;

    private Transform player;
    public Transform mainCam;
    private Animator playerAnimator;

    void Awake()
    {
        // Collect appropriate transforms.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        playerAnimator = player.GetComponent<Animator>();
        if(player != null)
            maxSpeed = player.gameObject.GetComponent<Player>().
            Stats.movementSpeed;

        // Set original player rotation.
        playerRotation = player.rotation.eulerAngles.y;

        if(startingPosition == Vector3.one * 999)
            startingPosition = mainCam.position - player.position + Vector3.up;

        // Set original camera distance and direction.
        cameraDir = Quaternion.LookRotation(startingPosition, Vector3.up);
        cameraDist = startingPosition.magnitude;
    }

    void LateUpdate()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if(player == null || mainCam == null || playerAnimator == null)
            Awake();

        AcceptInput();
        UpdateCameraPosition();

        // While dodging, cannot move.
        if(playerAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Dodge"))
            return;

        // Strafe movement is pretty simple.
        if(strafing)
        {
            float targetSpeed = Input.GetAxis("Move Y");
            targetSpeed *= (targetSpeed > 0 ? maxSpeed : maxSpeed / 2);
            playerSpeed = Mathf.Lerp(playerSpeed, targetSpeed, 
                                     3f * Time.deltaTime);
            playerAnimator.SetFloat("Speed", playerSpeed);
            if(playerSpeed < 0 && playerAnimator.speed > 0 ||
                playerSpeed > 0 && playerAnimator.speed < 0)
            {
                Debug.Log("Flipping");
                playerAnimator.speed *= -1;
                //playerAnimator.
            }
            Debug.Log(playerAnimator.speed);
            player.rotation = Quaternion.AngleAxis(
                mainCam.rotation.eulerAngles.y, Vector3.up);
            float strafeSpeed = 3f + playerSpeed / 2f;
            angularSpeed = Mathf.Lerp(angularSpeed, 
                                     Input.GetAxis("Move X") * strafeSpeed,
                                     3f * Time.deltaTime);
            playerAnimator.SetFloat("AngularSpeed", angularSpeed);
            return;
        }

        // Non-strafe movement is somewhat more advanced.
        float goalSpeed;
        float dir = GetMovementInput(out goalSpeed) * Mathf.Rad2Deg;

        if(goalSpeed > Mathf.Epsilon) // Update player rotation.
        {
            // Set dir to desired rotation.
            dir += mainCam.rotation.eulerAngles.y;
            dir -= playerRotation;
            if(dir < 180)
                dir += 360;
            while(dir > 180)
                dir -= 360;
            // Clamp it.
            dir = Mathf.Clamp(dir, -5f, 5f);
            if(Mathf.Abs(dir) < 0.01f)
                dir = 0f;
            float maxTurn = Time.deltaTime * 2f;
            if(Mathf.Abs(dir) < Mathf.Abs(angularSpeed))
                maxTurn *= 1;
            angularSpeed = Mathf.Lerp(angularSpeed, dir, maxTurn);
            playerRotation += dir;
            // Update player rotation.
            player.rotation = Quaternion.AngleAxis(playerRotation, Vector3.up);
            playerAnimator.SetFloat("AngularSpeed", angularSpeed);
        } else
            playerAnimator.SetFloat("AngularSpeed", 0f);


        // Update player speed.
        // Limit speed while crouched.
        if(playerAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Crouch"))
            goalSpeed = Mathf.Min(2f, goalSpeed);
        // Update speed and animator
        playerSpeed = Mathf.Lerp(playerSpeed, goalSpeed, 3f * Time.deltaTime);

        playerAnimator.SetFloat("Speed", playerSpeed);

        if(nextAction != Action.None)
            PerformAction(nextAction);
    }

    // Convert inputs to target dir/speed.
    private float GetMovementInput(out float speed)
    {
        float moveX = Input.GetAxis("Move X");
        float moveY = Input.GetAxis("Move Y");

        // Speed is simple enough.
        speed = Mathf.Sqrt(Mathf.Pow(moveX, 2) + Mathf.Pow(moveY, 2));
        speed *= maxSpeed;

        // Do not turn while not moving, to allow looking around.
        if(speed == 0)
            return 0f;

        // Direction is simple at the asymptote.
        if(Mathf.Abs(moveY) <= Mathf.Epsilon)
            return moveX >= 0f ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;

        // Otherwise, find the tangential value.
        return Mathf.Atan2(moveX, moveY);
    }

    // Camera movement.
    private void AcceptInput()
    {
        // Accept mouse input
        mouseDir.x += Input.GetAxis("Look X") * mouseSensitivity;
        mouseDir.y += Input.GetAxis("Look Y") * mouseSensitivity;
        mouseDir.y = Mathf.Clamp(mouseDir.y, -30f, 10f);
        cameraDist -= Input.GetAxis("Zoom");
        cameraDist = Mathf.Clamp(cameraDist, 2f, 5f);

        // If animation is playing and not nearly over, ignore inputs.
        if(!playerAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Ready"))
        {
            double time = playerAnimator.
                GetCurrentAnimatorStateInfo(0).normalizedTime;
            if(time - (long)time < 0.75)
                return;
        }

        // Else, prepare for next action.
        foreach(Action action in System.Enum.GetValues(typeof(Action)))
            if(action != Action.None && Input.GetButtonDown(action.ToString()))
                nextAction = action;
    }

    private void UpdateCameraPosition()
    {
        // Move to the desired position.
        var mouseRotation = Quaternion.Euler(mouseDir.y, mouseDir.x, 0);
        Quaternion targetDir = mouseRotation * cameraDir;
        Vector3 targetPos = targetDir * Vector3.forward * cameraDist;
        mainCam.position = targetPos + player.position + Vector3.up;
        
        // Look at the player.
        Vector3 dir = player.position - mainCam.position + Vector3.up * 1.1f;
        mainCam.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // Action handling.

    private void PerformAction(Action action)
    {
        nextAction = Action.None;
        switch(action)
        {
            case Action.Crouch:
                if(playerSpeed < 2)
                {
                    playerSpeed = 0;
                    playerAnimator.SetFloat("Speed", playerSpeed);
                }
                Strafe(0);
                return;
            case Action.StrafeLeft:
                Strafe(-1);
                return;
            case Action.StrafeRight:
                Strafe(1);
                return;
            default:
                return;
        }
    }

    private void Strafe(int direction)
    {
        playerAnimator.SetFloat("StrafeAngle", direction);
        playerAnimator.SetTrigger("Dodge");
        angularSpeed = 0;
    }
}