using NeutronNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionHelper
{
    public static T GetAttribute<T>(string methodName, object instance) where T : Attribute
    {
        var method = GetMethod(methodName, instance);
        if (method != null)
            return method.GetCustomAttribute<T>();
        else return null;
    }

    public static T[] GetAttributes<T>(object instance) where T : Attribute
    {
        var methods = GetMethods(instance);
        if (methods.Length > 0)
        {
            List<T> attributes = new List<T>();
            for (int i = 0; i < methods.Length; i++)
            {
                T attr = methods[i].GetCustomAttribute<T>();
                if (attr != null)
                    attributes.Add(attr);
                else
                    continue;
            }
            return attributes.ToArray();
        }
        else return null;
    }

    public static T[][] GetMultipleAttributes<T>(object instance) where T : Attribute
    {
        var methods = GetMethods(instance);
        if (methods.Length > 0)
        {
            List<T[]> attributes = new List<T[]>();
            for (int i = 0; i < methods.Length; i++)
            {
                T[] attrs = methods[i].GetCustomAttributes<T>().ToArray();
                if (attrs.Length > 0)
                    attributes.Add(attrs);
                else
                    continue;
            }
            return attributes.ToArray();
        }
        else return null;
    }

    public static (T[], MethodInfo)[] GetMultipleAttributesWithMethod<T>(object instance) where T : Attribute
    {
        var methods = GetMethods(instance);
        if (methods.Length > 0)
        {
            List<(T[], MethodInfo)> attributes = new List<(T[], MethodInfo)>();
            for (int i = 0; i < methods.Length; i++)
            {
                T[] attrs = methods[i].GetCustomAttributes<T>().ToArray();
                if (attrs.Length > 0)
                    attributes.Add((attrs, methods[i]));
                else
                    continue;
            }
            return attributes.ToArray();
        }
        else return null;
    }

    public static MethodInfo GetMethod(string name, object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    {
        return instance.GetType().GetMethod(name, flags);
    }

    public static MethodInfo[] GetMethods(object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    {
        return instance.GetType().GetMethods(flags);
    }
}