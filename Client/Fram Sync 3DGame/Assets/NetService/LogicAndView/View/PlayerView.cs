using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    
    public int HP;
    public PlayerLogic playerLogic;
    public Vector3 viewPos;
    private Vector3 preLogicPos;
    private float lerpSpeed;
    private Vector3 nowPredictPos;
    public void ConnectLogic(PlayerLogic logic)
    {
        playerLogic = logic;
        viewPos = playerLogic.LogicPos;
       
        SyncPosWithServer();
        SyncHP();
    }
    private void LateUpdate()
    {

        #region 尝试分开本地与网络
        //if (playerLogic.playerId==PlayerManager.LocalPlayerID)
        //{
        //    this.transform.position= playerLogic.LogicPos;
        //}
        //else
        //{
        //lerpSpeed = 0.1f * Time.deltaTime * 60;
        //viewPos = Vector3.Lerp(viewPos, playerLogic.LogicPos, lerpSpeed);
        //this.transform.position = Vector3.Lerp(this.transform.position, viewPos, lerpSpeed);
        //float Distance = Vector3.Distance(viewPos, playerLogic.LogicPos);
        //if (Distance > 1f)
        //{
        //    viewPos = playerLogic.LogicPos;
        //}
        //}
        #endregion
        lerpSpeed = 0.1f * Time.deltaTime * 60;
        #region 双重插值

        viewPos = Vector3.Lerp(viewPos, playerLogic.LogicPos, lerpSpeed);
        this.transform.position = Vector3.Lerp(this.transform.position, viewPos, lerpSpeed);
        float Distance = Vector3.Distance(viewPos, playerLogic.LogicPos);
        if (Distance > 1f)
        {
            viewPos = playerLogic.LogicPos;
        }
        #endregion
        #region  一重插值
        //viewPos = Vector3.Lerp(viewPos, playerLogic.LogicPos, lerpSpeed);

        //this.transform.position = viewPos;
        //float Distance = Vector3.Distance(viewPos, playerLogic.LogicPos);

        //if (Distance > 1f)
        //{
        //    viewPos = playerLogic.LogicPos;
        //}

        #endregion
        #region 无插值
        //this.transform.position = playerLogic.LogicPos;

        #endregion
    }
    public void SyncPosWithServer()
    {
        
        //float Distance = Vector3.Distance(viewPos, playerLogic.LogicPos);
        //if (Distance<0.1f)
        //{
        //    return;
        //}
        //if (Distance<=1f)
        //{
        //    viewPos = Vector3.Lerp(viewPos,playerLogic.LogicPos,lerpSpeed);
        //}
        //else
        //{
        //    viewPos=playerLogic.LogicPos;
        //}
    }
    public void SyncHP()
    {
        
            HP = playerLogic.HP;
    }
    public void UpdateView(Vector3 Dir, float FixedDeltaTime)
    {
        Vector3 dirNormalized = Dir.normalized;

        
        Vector3 newPos = viewPos + dirNormalized * playerLogic.MoveSpeed * FixedDeltaTime;


        viewPos = FixFloat(newPos);

        nowPredictPos = viewPos;
        
    }
    public Vector3 FixFloat(Vector3 pos)
    {
        int precision = 1000;
        return new Vector3(Mathf.Round(pos.x * precision) / precision, Mathf.Round(pos.y * precision) / precision, Mathf.Round(pos.z * precision) / precision);
    }
}
