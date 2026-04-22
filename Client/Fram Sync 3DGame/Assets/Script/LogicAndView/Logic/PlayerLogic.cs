using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerLogic
{


    public int playerId;
    public Vector3 LogicPos;
    public float MoveSpeed=2f;
    public int HP;

    public PlayerLogic(int playerId,Vector3 initPos,int hp)
    {
        
        LogicPos=initPos;
        HP=hp;
        this.playerId = playerId;
    }
    public void UpdateMove(Vector3 Dir,float FixedDeltaTime)
    {
        //Debug.Log($"玩家{playerId}移动方向：{Dir}");
        Vector3 newPos = LogicPos + Dir.normalized * FixedDeltaTime * MoveSpeed;
        LogicPos=FixFloat(newPos);
    }
    public Vector3 FixFloat(Vector3 pos)
    {
        int precision = 1000;
        return new Vector3(Mathf.Round(pos.x*precision)/precision,Mathf.Round(pos.y*precision)/precision, Mathf.Round(pos.z * precision) / precision);
    }
    public void TakeDamage(int damage)
    {
        HP=Mathf.Max(0,HP-damage);
    }
}
