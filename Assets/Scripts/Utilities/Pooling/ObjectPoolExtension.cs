using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Pooling
{
    internal static class ObjectPoolExtension
    {
        public static Transform GetTransform(this Object obj)
        {
            return obj switch
            {
                GameObject go => go.transform,
                Component co => co.transform,
                _ => throw new NotImplementedException($"{obj.name} is not GameObject or Component.")
            };
        }
        
        public static T GetComponent<T>(this Object obj) where T : Object
        {
            return obj switch
            {
                GameObject go => go.GetComponent<T>(),
                Component co => co.GetComponent<T>(),
                _ => throw new NotImplementedException($"{obj.name} is not GameObject or Component.")
            };
        }

        public static bool ActiveSelf(this Object obj)
        {
            return obj switch
            {
                GameObject go => go.activeSelf,
                Component co => co.gameObject.activeSelf,
                _ => throw new NotImplementedException($"{obj.name} is not GameObject or Component.")
            };
        }

        public static T SetActive<T>(this T obj, bool active) where T : Object
        {
            switch (obj)
            {
                case GameObject go:
                    go.SetActive(false);
                    return obj;
                case Component co:
                    co.gameObject.SetActive(active);
                    return obj;
                default:
                    throw new NotImplementedException($"{obj.name} is not GameObject or Component.");
            }
        }
        
        public static Transform Parental(this Transform src, Transform parent, bool worldPositionStays = false)
        {
            src.SetParent(parent, worldPositionStays);
            return src;
        }

        public static Transform SetPosition(this Transform src, Vector3 position)
        {
            src.position = position;
            return src;
        }
        
        public static Transform SetRotation(this Transform src, Quaternion rotation)
        {
            src.rotation = rotation;
            return src;
        }
        
        public static Transform SetLocalPosition(this Transform src, Vector3 position)
        {
            src.localPosition = position;
            return src;
        }
        
        public static Transform SetLocalRotation(this Transform src, Quaternion rotation)
        {
            src.localRotation = rotation;
            return src;
        }
        
        public static void SetActive(this Transform src, bool active) => src.gameObject.SetActive(active);
    }
}