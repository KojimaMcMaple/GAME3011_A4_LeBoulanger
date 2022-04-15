using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalUtils
{
	public static int GetAnimClipIdxByName(Animator anim, string name)
	{
		AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
		for (int i = 0; i < clips.Length; i++)
		{
			if (clips[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	public static void AddRuntimeAnimEvent(Animator anim, int clip_idx, float time, string functionName, float floatParameter)
	{
		AnimationEvent animationEvent = new AnimationEvent();
		animationEvent.functionName = functionName;
		animationEvent.floatParameter = floatParameter;
		animationEvent.time = time;
		AnimationClip clip = anim.runtimeAnimatorController.animationClips[clip_idx];
		clip.AddEvent(animationEvent);
	}
}
