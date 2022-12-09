using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Used for both StorySet and StoryReqs.
/// </summary>
public class StoryCall {
	public string methodName;
	public object[] parameterArray;
	public string debugString;

	public MethodInfo methodInfo;
	
	// may be encapsulated in either a StoryReq or StorySet
	public StoryReq req;
	public StorySet set;

	// set only while validating a call that might need access to the current story
	public static Story validationStory = null;

	public static StoryCall CreateStoryCall(string methodName, object[] parameterArray, string debugString = null) {
		// methodInfo may be null if parameters are wrong, if so Validate will fail later as well
		Type[] paramTypes = new Type[parameterArray.Length];
		for (int i = 0; i < parameterArray.Length; i++) {
			paramTypes[i] = parameterArray[i].GetType();
		}
		
		MethodInfo methodInfo = typeof(StoryCalls).GetMethod(methodName, paramTypes);
		
		if (methodInfo == null) {
			// look for name with any signature to know what went wrong
			try {
				methodInfo = typeof(StoryCalls).GetMethod(methodName);
				if (methodInfo == null) {
					Debug.LogWarning("Story call has unknown method name " + methodName);
				} else {
					Debug.LogWarning("Story call has wrong parameters " + methodName 
						+ ", params len " + paramTypes.Length);
				}
			} catch {
				// AmbiguousMatchException if 2 methods have that name but neither has the right params
				Debug.LogWarning("Story call has wrong parameters " + methodName);
			}
			return null;
		}

		StoryCall call = new StoryCall {
			methodName = methodName, parameterArray = parameterArray, debugString = debugString, methodInfo = methodInfo
		};
		return call;
	}

	public static StoryCall FromTemplate(StoryCallTemplate template) {
		return CreateStoryCall(template.methodName, template.parameterArray, template.debugString);
	}

	public StoryCallTemplate ToTemplate() {
		return new StoryCallTemplate {
			methodName = methodName,
			parameterArray = parameterArray,
			debugString = debugString,
		};
	}

	/// <summary>
	/// Validation step done in Story.ValidateData after all data files have been loaded.
	/// Turns stringValue + arrayValue into method call with parameters,
	/// if a method exists in StoryCalls with the right signature.
	/// May also be called and other weird times for StorySets created on the fly.
	/// </summary>
	/// <param name="returnObject">Provided if the method must return this id</param>
	public bool Validate(StoryReq req = null) {
		Type[] paramTypes = new Type[parameterArray.Length];
		for (int i = 0; i < parameterArray.Length; i++) {
			paramTypes[i] = parameterArray[i].GetType();
		}
		
		if (methodInfo == null) {
			// should have been caught earlier but check again just in case
			Debug.LogWarning("Story call has unknown method or wrong params " + methodName);
			return false;
		}
		
		// must match return id
		if (req != null) {
			if (req.objectValue is StoryCall call) {
				if (methodInfo.ReturnType != call.methodInfo.ReturnType) {
					Debug.LogWarning("Story call has wrong return return type " + methodName 
						+ ", " + methodInfo.ReturnType + ", must match other call: " + call.GetType() + ", " + req);
				} else if ((req.compare == StoryReqCompare.greaterThan || req.compare == StoryReqCompare.lessThan) 
					&& !methodInfo.ReturnType.IsNumericType()) {
					Debug.LogWarning("Story call has wrong return return type " + methodName 
						+ ", " + methodInfo.ReturnType + ", must be int for < > compare" + ", " + req);
				}
			} else {
				// comparing to int or string?
				if (methodInfo.ReturnType != req.objectValue.GetType()) {
					Debug.LogWarning("Story call has wrong return return type " + methodName 
						+ ", " + methodInfo.ReturnType + ", " + req.objectValue.GetType() + ", " + req);
					return false;
				}
			}
		}

		// some methods may have a way to validate the possible params
		MethodInfo validationMethod = typeof(StoryCalls).GetMethod(methodName + "Validate", paramTypes);
		if (validationMethod != null) {
			validationStory = this.req?.story ?? this.set?.story;
			try {
				bool methodResult = (bool)validationMethod.Invoke(null, parameterArray);
				validationStory = null;
				return methodResult;
			} catch (Exception e) {
				Debug.LogWarning("StoryCall failed to validate " + methodName + ", " + e + ", " + req);
				validationStory = null;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// For StorySet and MapInteractives with no return value.
	/// </summary>
	/// <returns>True if returnValue is returned by the method (including void for null)</returns>
	public object Execute() {
		if (methodInfo == null) {
			Debug.LogError("StoryCall.SetCall no methodInfo, was ValidateAndFinishCall called? " + this);
			Validate();
			if (methodInfo == null) {
				Debug.Log("Nope, still null.");
			}
			return null;
		}

		try {
			return methodInfo.Invoke(null, parameterArray);
		} catch (Exception e) {
			Debug.LogError("StorySet.DoCall failed " + methodName + ", " + e);
			return null;
		}
	}

	public override string ToString() {
		return "Call [" + debugString 
			+ (req != null ? (", req: " + req) : "")
			+ (set != null ? (", set: " + set) : "")
			+ "]";
	}
}
