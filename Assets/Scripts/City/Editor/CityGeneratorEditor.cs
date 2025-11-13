using UnityEditor;
using UnityEngine;

namespace City.Editor
{
	[CustomEditor(typeof(CityGenerator))]
	public class CityGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var generator = (CityGenerator)target;

			GUILayout.Space(8);
			if (GUILayout.Button("Generate City"))
			{
				generator.Generate();
			}

			if (GUILayout.Button("Clear City"))
			{
				generator.ClearCity();
			}
		}
	}
}


