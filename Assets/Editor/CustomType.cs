using System;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;

namespace ProfilerDatas
{
    public class CustomType
    {
        // 通过反射搜索指定成员和类型的标记
        // 公共字段
        private const BindingFlags PublicInstanceFieldFlag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField;
        // 私有字段
        private const BindingFlags PrivateInstanceFieldFlag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
        // 私有静态字段
        private const BindingFlags PrivateStaticFieldFlag = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField;
        // 公共实例方法
        private const BindingFlags PublicInstanceMethodFlag = BindingFlags.Public | BindingFlags.Instance;
        // 公共私有方法
        private const BindingFlags PrivateInstanceMethodFlag = BindingFlags.Instance | BindingFlags.NonPublic;


        public readonly Type InnerType;
        public object InnerObject { get; private set; }

        public CustomType(Type type)
        {
            InnerType = type;
        }


        public CustomType(object obj)
        {
            if (obj == null) return;
            InnerType = obj.GetType();
            InnerObject = obj;
        }

        /// <summary>
        /// 将 src 的 flags 数据复制到 dst 中，注意数据命名需要一样
        /// </summary>
        public static void CopeReflectFields(object dst, object src, BindingFlags flags)
        {
            Type dstType = dst.GetType();
            Type srcType = src.GetType();
            var dstFields = dstType.GetFields();
            foreach (var dstField in dstFields)
            {
                var srcFieldInfo = srcType.GetField(dstField.Name, flags);
                if (srcFieldInfo != null && srcFieldInfo.FieldType == dstField.FieldType)
                {
                    dstField.SetValue(dst, srcFieldInfo.GetValue(src));
                }
            }
        }




        /// <summary>
        /// 得到对应名字的公有字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object PublicInstanceField(string fieldName)
        {
            return GetField(fieldName, PublicInstanceFieldFlag);
        }

        public T PublicInstanceField<T>(string fieldName) where T : class
        {
            return PublicInstanceField(fieldName) as T;
        }

        /// <summary>
        /// 得到对应名字的私有字段
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object PrivateInstanceField(string fieldName)
        {
            return GetField(fieldName, PrivateInstanceFieldFlag);
        }

        public T PrivateInstanceField<T>(string fieldName) where T : class
        {
            return PrivateInstanceField(fieldName) as T;
        }

        public object PrivateStaticField(string fieldName)
        {
            return GetField(fieldName, PrivateStaticFieldFlag);
        }

        public T PrivateStaticField<T>(string fieldName) where T : class
        {
            return PrivateStaticField(fieldName) as T;
        }

        public void CallPrivateInstanceMethod(string methodName, params object[] args)
        {
            InvokeMethod(methodName, PrivateInstanceMethodFlag, args);
        }



        public void CallPublicInstanceMethod(string methodName, params object[] args)
        {
            InvokeMethod(methodName, PublicInstanceMethodFlag, args);
        }

        public void SetPrivateInstanceField(string fieldName, object value)
        {
            if (InnerType == null)
                return;

            var fieldInfo = InnerType.GetField(fieldName, PrivateInstanceFieldFlag);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(InnerObject, value);
            }
        }


        // 根据类型名和 BindingFlags 获取字段的值
        private object GetField(string fieldName, BindingFlags flags)
        {
            if (InnerType == null)
                return null;

            var fieldInfo = InnerType.GetField(fieldName, flags);
            // 仅 fieldInfo 对应的字段为 static 时，InnerObject 才能为空
            return fieldInfo != null ? fieldInfo.GetValue(InnerObject) : null;
        }


        // 根据类型名和 BindingFlags 调用对应方法
        private void InvokeMethod(string methodName, BindingFlags flags, params object[] args)
        {
            if (InnerType == null)
                return;

            var methodInfo = InnerType.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                UnityEngine.Debug.LogError("未获取到方法 " + methodName);
                return;
            }

            methodInfo.Invoke(InnerObject, args);
        }

    }
}

