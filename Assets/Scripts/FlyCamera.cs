using UnityEngine;
using System.Collections;

//
// Quick and hacky flycam controller.
//


public class FlyCamera : MonoBehaviour {


    public float cameraSensitivity = 90;
    public float climbSpeed = 4;
    public float normalMoveSpeed = 10;
    public float slowMoveFactor = 0.25f;
    public float fastMoveFactor = 3;

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    bool hold1, hold2;
    GameObject cursor;
    Vector3 crossHair;
    float chSize = 1f;
    bool mousemode = false;

    Vector3 startHold;

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        //Screen.lockCursor = true;
        cursor = GameObject.Find("cursor");
        cursor.transform.localScale = new Vector3(chSize, chSize, chSize);
    }

    void Update() {
        if (Input.GetKey(KeyCode.C)) {

            if (mousemode) {
                mousemode = false;
                
                Cursor.lockState = CursorLockMode.Locked;
            } else {
                Cursor.lockState = CursorLockMode.None;
                mousemode = true;
            }
        }

        if (mousemode) return;
        
        UpdateCrosshairDistance();
        UpdateMouseControl();
        //UpdateAngles();

        rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);

        if (!CollisionDetect() || Input.GetAxis("Vertical") <= 0f) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            } else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            } else {
                transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
        }



        if (Input.GetKey(KeyCode.E)) { transform.position += transform.up * climbSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.Q)) { transform.position -= transform.up * climbSpeed * Time.deltaTime; }


    }
    private void FixedUpdate() {
        cursor.transform.position = crossHair;
        cursor.transform.rotation = Camera.main.transform.rotation;
        cursor.transform.localScale = new Vector3(chSize, chSize, 0f);
    }

    void UpdateMouseControl() {
        if (Input.GetMouseButtonDown(0)) { hold1 = true; startHold = crossHair; }
        if (Input.GetMouseButtonUp(0)) { hold1 = false;  }
        if (Input.GetMouseButtonDown(1)) hold2 = true;
        if (Input.GetMouseButtonUp(1)) hold2 = false;

        if (Input.GetAxis("Mouse ScrollWheel") > 0) chSize = Mathf.Clamp(chSize + 1, 1, 10);
        if (Input.GetAxis("Mouse ScrollWheel") < 0) chSize = Mathf.Clamp(chSize - 1, 1, 10);


        if (Controller.simMode == SimMode.fluid) {
            //when Stable-Fluids mode we can add fluid or velocity
            if (hold1 && !isOutsideCanvas(crossHair)) Controller.myFluidCube.AddFluid(crossHair);
            if (hold2 && !isOutsideCanvas(crossHair)) Controller.myFluidCube.AddVelo(crossHair);
        } else {
            // Add and Remove "matter" to the world.
            // i use the function Random.insideUnitSphere vs. an actual sphere.
            // not yet sure whats better.

            //if (hold1 && !isOutsideCanvas(crossHair)) MC_Canvas.SetPointCloudInWorld(HelperTools.GetRandomSphere(crossHair, 100 * (int)chSize, chSize), 2);
            //if (hold2 && !isOutsideCanvas(crossHair)) MC_Canvas.SetPointCloudInWorld(HelperTools.GetRandomSphere(crossHair, 100 * (int)chSize, chSize), 2, true,null,0.8f);

            if (hold1 && !isOutsideCanvas(crossHair)) Controller.myCanvas.SetPointCloudInWorld(HelperTools.GetSphere(crossHair,  chSize), 2,false,null,0.25f);
            if (hold2 && !isOutsideCanvas(crossHair)) Controller.myCanvas.SetPointCloudInWorld(HelperTools.GetSphere(crossHair,  chSize), 2, true,null,0.5f);
        }


    }

    void UpdateCrosshairDistance() {

        float dist = 16f;

        RaycastHit hit;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);


        // Cast a ray straight downwards.
        if (Physics.Raycast(ray, out hit)) {
            //Debug.Log("hit " + hit.transform.gameObject.name);

            if (hit.transform.gameObject != null) {
                if (hit.distance < dist && !hold1) {
                    dist = hit.distance;
                }

            }

        }

        crossHair = (Camera.main.transform.position + Camera.main.transform.forward * dist);
    }

    public bool isOutsideCanvas(Vector3 where) {
        MC_Point p = Controller.myCanvas.GetWorldCoordByRealPos(where);
        if (p == null) {
            Debug.Log("outisde?");
            return true;
        }
        return false;
    }


    bool CollisionDetect(float dist = 6f) {

        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out hit)) {
            //Debug.Log("hit " + hit.transform.gameObject.name);

            if (hit.transform.gameObject != null) if (hit.distance < dist ) return true;
                            

        }
        return false;
    }


}