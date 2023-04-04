using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keymapper : MonoBehaviour {
	//Alt. for the right click
	public KeyCode HideUI = KeyCode.Escape;

	//Alt. for Scroll wheel
	public KeyCode PrevFileInDir = KeyCode.F1;
	public KeyCode NextFileInDir = KeyCode.F2;

	//Not yet used
	public KeyCode PrevDir = KeyCode.F3;
	public KeyCode NextDir = KeyCode.F4;

	public static Keymapper Inst {get; private set;}

	void Start() {
		Inst = GameObject.Find("Keymapper").GetComponent<Keymapper>();
		if (Inst == null)
			Debug.Log("Keymapper.Start(): Couldn't find the Keymapper object.");
	}
}