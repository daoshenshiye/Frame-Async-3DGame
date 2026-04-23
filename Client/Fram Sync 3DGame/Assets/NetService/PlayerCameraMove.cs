using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraMove : MonoBehaviour
{
    public float RotateSpeed = 40f;
    public bool IsOneAspect = false;
    public Transform Target;
    public float dis=6f;
    public float minDis=3f;
    public float maxDis=10f;
    public float MouseScroSpeed = 20f;
    private RaycastHit hitInfo;
    public float SafeOffset =0.5f;
    public float HeightOffset = 1f;
    // Update is called once per frame
    void Update()
    {
        
      CameraMove();
        

    }
    public void CameraMove()
    {
        if (Target == null)
            return;
        if (IsOneAspect)
        {
            this.transform.Rotate(Vector3.up * RotateSpeed * Time.deltaTime * Input.GetAxis("Mouse X"));

            this.transform.Rotate(Vector3.right * RotateSpeed * Time.deltaTime * Input.GetAxis("Mouse Y"));
        }
        else
        {

            Vector3 nowTarget = Vector3.up * HeightOffset + Target.position;

            this.transform.RotateAround(nowTarget, Target.up, RotateSpeed * Time.deltaTime * Input.GetAxis("Mouse X"));
            this.transform.RotateAround(nowTarget, Target.right, RotateSpeed * Time.deltaTime * Input.GetAxis("Mouse Y"));

            transform.LookAt(nowTarget);


            Vector3 Direction = -(nowTarget - this.transform.position).normalized;

            if (Physics.Raycast(nowTarget, Direction, dis, 1 << LayerMask.NameToLayer("Environment")))
            {
                dis = dis - SafeOffset;

            }
            else
            {
                dis += -Input.GetAxis("Mouse ScrollWheel") * MouseScroSpeed * Time.deltaTime;
                dis = Mathf.Clamp(dis, minDis, maxDis);
            }

            this.transform.position = Direction * dis + nowTarget;
        }
    }
}
