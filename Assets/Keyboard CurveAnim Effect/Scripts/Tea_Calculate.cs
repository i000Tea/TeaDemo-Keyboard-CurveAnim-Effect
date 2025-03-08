using System.Collections.Generic;
using UnityEngine;
namespace Tea_Demos
{
   public delegate void TEvent();
   public delegate void TEvent<T>(T arg);
   public delegate void TEvent<T, T2>(T arg, T2 arg2);
   public delegate void TEvent<T, T2, T3>(T arg, T2 arg2, T3 arg3);
   public delegate void TEvent<T, T2, T3, T4>(T arg, T2 arg2, T3 arg3, T4 arg4);
   public class Tea_Calculate : MonoBehaviour
   {
      #region 参数变量

      #region 委托
      public static TEvent<Vector3, Vector2> mouseMpvement;
      public static TEvent<Transform, Transform> keyHover;
      public static TEvent<int, bool> keyClick;

      // 动画委托
      public static TEvent<int, bool> keyHoverAnim;
      public static TEvent<int, int, bool> keyAnim;
      public static TEvent<int, Vector3, bool, int> KeyboardAnim;
      public static TEvent<int> audioPlay;
      #endregion

      public static Transform[] key;
      public static Transform[] keyReplace;
      public static int drawDataIndex;
      public int[] keyValue;
      public bool[] keyDown;
      private int KeyboardValue = -1;
      #endregion

      #region Data
      public static InputData InputData;
      public InputData inputData;

      public static MouseAnimData MouseAnimData;
      public MouseAnimData mouseAnimData;
      #endregion

      private void Awake()
      {
         InputData = inputData;
         MouseAnimData = mouseAnimData;
         keyClick = KeyClick;
         keyHover = KeyHover;
      }
      private void Start()
      {
         StartCreateKeyReplace();
         keyValue = new int[key.Length];
         keyDown = new bool[key.Length];
      }
      private void KeyHover(Transform before, Transform nowSelect)
      {
         int beforeIndex = -1, nowIndex = -1;
         for (int i = 0; i < keyReplace.Length; i++)
         {
            if (keyReplace[i].Equals(before)) beforeIndex = i;
            else if (keyReplace[i].Equals(nowSelect)) nowIndex = i;
         }
         if (beforeIndex >= 0)
         {
            keyAnim?.Invoke(beforeIndex, -1, false);

            // audioPlay?.Invoke(1);
         }
         if (nowIndex >= 0)
         {
            keyAnim?.Invoke(nowIndex, -1, true);

            audioPlay?.Invoke(0);
         }
      }

      /// <summary>
      /// 按键点击事件处理方法
      /// </summary>
      /// <param name="index">被点击的按键索引</param>
      /// <param name="type">按键状态 - true:按下, false:抬起</param>
      private void KeyClick(int index, bool type)
      {
         if (index < 0)
         {
            for (int i = 0; i < keyReplace.Length; i++)
               if (keyReplace[i].Equals(InputData.hoverButton)) index = i;

            if (index < 0) return;
         }
         // 更新当前按键的按下状态
         keyDown[index] = type;
         // 用于存储键盘组合状态的值，-1表示无效组合
         var cacheKeyValue = -1;

         if (!type) // 按键抬起时执行
         {
            // 循环更新当前按键的外观序号
            keyValue[index]++;
            if (keyValue[index] >= drawDataIndex)
               keyValue[index] = 0;

            // 如果当前值与键盘已有值相同，直接跳过检测
            if (keyValue[index] == KeyboardValue) { cacheKeyValue = -1; }
            else
            {
               // 检查所有按键是否处于相同的外观序号状态
               cacheKeyValue = keyValue[index];
               for (int i = 0; i < keyValue.Length; i++)
               {
                  // 只要发现任何一个按键值不同，就设置为无效组合并退出循环
                  if (keyValue[i] != cacheKeyValue)
                  {
                     cacheKeyValue = -1;
                     break;
                  }
               }
               // 如果所有按键序号一致，更新键盘整体状态值
               if (cacheKeyValue != -1)
               {
                  KeyboardValue = cacheKeyValue;
               }
            }
         }

         // 计算当前按下的按键数量和旋转偏移量
         GetPressedKeyCountAndOffset(out var value, out var rotate);

         // 触发按键动画事件，传递当前按键索引、外观序号和按键状态
         keyAnim.Invoke(index, keyValue[index], type);
         // 触发键盘整体动画事件，传递按下数量、旋转偏移、按键状态和组合状态值
         KeyboardAnim.Invoke(value, rotate, type, cacheKeyValue);

         audioPlay?.Invoke(type ? 2 : 3);

         if (cacheKeyValue >= 0)
         {
            audioPlay?.Invoke(4);
         }
         //Debug.Log("a" + index + type + cacheKeyValue);
      }

      #region 辅助计算
      private void StartCreateKeyReplace()
      {
         var parent = new GameObject("keyReplaceParent").transform;
         List<Transform> cacheKeyReplace = new();
         for (int i = 0; i < key.Length; i++)
         {
            cacheKeyReplace.Add(Instantiate(key[i], parent.transform));

            key[i].GetComponent<Collider>().enabled = false;
            cacheKeyReplace[i].GetComponent<Collider>().enabled = true;
            cacheKeyReplace[i].GetComponent<MeshRenderer>().enabled = false;
         }
         keyReplace = cacheKeyReplace.ToArray();
      }
      /// <summary>
      /// 计算当前处于按下状态的按键数量
      /// 和所有已按下按键的平均旋转角度
      /// </summary>
      private void GetPressedKeyCountAndOffset(out int value, out Vector3 offset)
      {
         offset = Vector3.zero;
         value = 0;

         for (int i = 0; i < keyDown.Length; i++)
         {
            if (keyDown[i])
            {
               Vector3 buttonLocalPos = key[i].transform.localPosition.normalized;
               float rotateX = buttonLocalPos.z;
               float rotateZ = -buttonLocalPos.x;
               offset += new Vector3(rotateX, 0, rotateZ);
               value++;
            }
         }
         offset = offset.normalized;
      }
      #endregion
   }
}