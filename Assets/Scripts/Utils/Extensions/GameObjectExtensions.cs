using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Also some for UnityEngine.Object, regular Object, and Component.
/// </summary>
public static class GameObjectExtensions
{

	public static string GetPath(this UnityEngine.Object obj) {
		if (obj == null) return null;
		if (obj is GameObject gameObject) return gameObject.GetPath();
		if (obj is Component component) return component.GetPath();
		return "Unknown/Path/" + obj.GetType().ToString();
	}

	public static string GetPath(this GameObject gameObject) {
		if (gameObject == null) return null;
		try {
			return gameObject.transform.GetPath() + "/" + gameObject.GetType().ToString();
		} catch {
			return "[path undetermined. Object null?]";
		}
	}

	/// <summary>
	/// Makes a copy of the object, copying values (float, string) 
	/// and maintaining references for other types of variables (string[], SomeClass).
	/// </summary>
	public static T ShallowClone<T>(this T obj) {
		MethodInfo cloneMethod = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
		if (cloneMethod != null) {
			return (T)cloneMethod.Invoke(obj, null);
		}
		return default(T);
	}
	
	public static bool IsNumericType(this Type type){
		switch (Type.GetTypeCode(type)){
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				return true;
			default:
				return false;
		}
	}
}