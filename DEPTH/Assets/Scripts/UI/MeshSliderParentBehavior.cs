using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using IngameDebugConsole;

public static class MeshSliderParents {
	private static Dictionary<string, MeshSliderParentBehavior> _dict;

	private static string MinMaxPath {get {return $"{DepthFileUtils.SaveDir}/minmax.txt";}}

	static MeshSliderParents() {
		_dict = new Dictionary<string, MeshSliderParentBehavior>();
	}

	public static void Add(string paramname, MeshSliderParentBehavior behav) =>
		_dict.Add(paramname, behav);

	public static MeshSliderParentBehavior Get(string paramname) =>
		_dict[paramname];

	[ConsoleMethod("minmax_export", "Export current min/max values for the mesh sliders")]
	public static void ExportMinMax() {
		/*
		param1 min max
		param2 min max
		...
		*/

		StringBuilder output = new StringBuilder();

		foreach (var item in _dict) {
			string paramname = item.Key;
			
			Slider targetSlider = item.Value.Slider;
			float minValue = targetSlider.minValue;
			float maxValue = targetSlider.maxValue;

			output.Append($"{paramname} {minValue} {maxValue}\n");
		}

		File.WriteAllText(MinMaxPath, output.ToString());
	}

	[ConsoleMethod("minmax_import", "Import the min/max values for the mesh sliders")]
	public static void ImportMinMax() {
		if (!File.Exists(MinMaxPath)) return;
		string input = File.ReadAllText(MinMaxPath);

		ImportMinMax(input);
	}

	public static void ImportMinMax(string input) {
		foreach (string line in input.Split('\n')) {
			string[] tokens = line.Split(' ');
			if (tokens.Length < 3)
				continue;

			string paramname = tokens[0].Trim();
			float minValue, maxValue;
			try {
				minValue = float.Parse(tokens[1]);
				maxValue = float.Parse(tokens[2]);
			}
			catch (System.FormatException exc) {
				Debug.LogWarning($"ImportMinMax(): Failed to parse: `{line}`, {exc}");
				continue;
			}

			if (!_dict.ContainsKey(paramname))
				continue;

			Slider target = _dict[paramname].Slider;
			target.minValue = minValue;
			target.maxValue = maxValue;
		}
	}

	[ConsoleMethod("minmax_reset", "Reset the min/max values for the mesh sliders")]
	public static void ResetMinMax() {
		string input = @"
			Scale 0.5 1.5
			Beta 0.25 0.75
			Alpha 0.01 2
			MeshY -10 10
			MeshLoc -50 50
			MeshX -10 10
			DepthMult 0 100
		";

		ImportMinMax(input);
	}
}

public class MeshSliderParentBehavior : SliderParentBehavior {
	private IDepthMesh _dmesh;
	private string _paramname;

	void Start() {
		_dmesh = GameObject.Find("DepthPlane").GetComponent<MeshBehavior>();
		_paramname = LabelText.text;

		Slider.onValueChanged.AddListener(delegate {ChangeValueText();});
		Slider.onValueChanged.AddListener(delegate {OnValueChanged();});

		ChangeValueText();

		//Add itself to MeshSlidersParents
		MeshSliderParents.Add(_paramname, this);

		//Add itself to mesh's ParamChanged event
		_dmesh.ParamChanged += OnParamChanged;
	}

	private void OnValueChanged() =>
		_dmesh.SetParam(_paramname, Slider.value);

	private void OnParamChanged(string paramname, float value) {
		/* This method is invoked when the mesh's property is changed, either by this slider or by external actor */

		if (paramname != _paramname)
			return;

		if (value == Slider.value)
			return;
		
		//Value changed by other object
		//Check if it is in the range of the slider
		if (Slider.minValue <= value && value <= Slider.maxValue)
			Slider.value = value;
		else
			//otherwise just change the label
			ValueText.text = value.ToString("0.##") + "*";
	}
}