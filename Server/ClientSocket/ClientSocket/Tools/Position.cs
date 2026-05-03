using GamePlayer;
using System;
using System.Collections.Generic;
using System.Text;
using ClientSocket.Component;

namespace ClientSocket.Tools
{
    public class Position:BaseComponent
    {
        // 原有的私有字段（保留，兼容原有逻辑）
        private PlayerPosData playerPosData;

        // ✅ 修正：改为 float 类型（适配帧同步浮点坐标）
        public float x;
        public float y;
        public float z;

        #region 新增：x/y/z 构造函数（float 类型核心重载）
        // 1. 核心构造函数：直接传入 float 类型 x/y/z
        public Position(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            // 同步初始化 PlayerPosData（无缝兼容原有序列化逻辑）
            playerPosData = new PlayerPosData
            {
                x = x,
                y = y,
                z = z
            };
        }

        // 2. 兼容构造函数：传入 PlayerPosData（直接复用原有数据）
        public Position(PlayerPosData posData)
        {
            this.x = posData.x;
            this.y = posData.y;
            this.z = posData.z;
            this.playerPosData = posData;
        }

        // 3. 无参构造函数（默认初始化为 0.0f）
        public Position()
        {
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
            playerPosData = new PlayerPosData();
        }

        // 4. 简化构造函数：只传 x/z（适配 2D 平面移动，y 默认为 0）
        public Position(float x, float z) : this(x, 0.0f, z)
        {
        }
        #endregion

        #region 核心：运算符重载（+、*、==/!=，适配 float 计算）
        // 1. 重载 + 运算符：两个 Position 坐标相加（比如位移叠加）
        public static Position operator +(Position a, Position b)
        {
            // 空值安全校验（避免帧同步中空引用崩溃）
            if (a == null) return b ?? new Position();
            if (b == null) return a ?? new Position();

            return new Position(
                a.x + b.x,
                a.y + b.y,
                a.z + b.z
            );
        }

        // 2. 重载 * 运算符：Position × 浮点数值（缩放/移动距离计算）
        public static Position operator *(Position pos, float multiplier)
        {
            if (pos == null) return new Position();

            return new Position(
                pos.x * multiplier,
                pos.y * multiplier,
                pos.z * multiplier
            );
        }

        // 3. 重载 * 运算符：数值 × Position（交换律，方便计算）
        public static Position operator *(float multiplier, Position pos)
        {
            return pos * multiplier; // 复用已有逻辑，避免重复代码
        }

        // 4. 重载 == 运算符：判断两个坐标是否相等（浮点精度容错）
        public static bool operator ==(Position a, Position b)
        {
            // 处理 null 情况
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;

            // ✅ 浮点精度容错（帧同步必备，避免 0.1+0.2≠0.3 问题）
            const float epsilon = 1e-6f; // 0.000001，可根据需求调整
            return Math.Abs(a.x - b.x) < epsilon &&
                   Math.Abs(a.y - b.y) < epsilon &&
                   Math.Abs(a.z - b.z) < epsilon;
        }

        // 5. 重载 != 运算符（必须和 == 成对实现）
        public static bool operator !=(Position a, Position b)
        {
            return !(a == b);
        }
        #endregion

        #region 规范重写：Equals + GetHashCode（符合 C# 标准）
        public override bool Equals(object obj)
        {
            return obj is Position pos && this == pos;
        }

        public override int GetHashCode()
        {
            // 基于浮点坐标生成哈希值（适配字典/集合存储）
            return HashCode.Combine(x, y, z);
        }
        #endregion

        #region 实用方法：兼容原有 PlayerPosData 序列化逻辑
        // 转换为 PlayerPosData（直接用于 UDP 网络发送/序列化）
        public PlayerPosData ToPlayerPosData()
        {
            return new PlayerPosData
            {
                x = this.x,
                y = this.y,
                z = this.z
            };
        }

        // 从 PlayerPosData 初始化（接收网络数据后快速转换）
        public static Position FromPlayerPosData(PlayerPosData data)
        {
            return new Position(data);
        }
        #endregion

        #region 调试辅助：ToString 方法
        public override string ToString()
        {
            // 保留 3 位小数，方便调试帧同步坐标
            return $"Position(x:{x:F3}, y:{y:F3}, z:{z:F3})";
        }
        #endregion
    }
}