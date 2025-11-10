using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DayNightCycle))]
public class DayNightCycleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DayNightCycle cycle = (DayNightCycle)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "DAY/NIGHT CYCLE & BUILDING LIGHTS\n\n" +
            "• Auto-progresses time (disable for manual control)\n" +
            "• Rotates sun/moon light\n" +
            "• Turns building windows on/off automatically\n" +
            $"• Current time: {cycle.GetTimeString()}\n\n" +
            "Click buttons below for quick time changes!",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Time Controls:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Dawn (6 AM)", GUILayout.Height(30)))
        {
            cycle.SetTimeToDawn();
            EditorUtility.SetDirty(cycle);
        }

        if (GUILayout.Button("Noon (12 PM)", GUILayout.Height(30)))
        {
            cycle.SetTimeToDay();
            EditorUtility.SetDirty(cycle);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Dusk (6 PM)", GUILayout.Height(30)))
        {
            cycle.SetTimeToDusk();
            EditorUtility.SetDirty(cycle);
        }

        if (GUILayout.Button("Night (8 PM)", GUILayout.Height(30)))
        {
            cycle.SetTimeToNight();
            EditorUtility.SetDirty(cycle);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Refresh Building Materials", GUILayout.Height(35)))
        {
            cycle.RefreshBuildingMaterials();
            Debug.Log("Building materials refreshed!");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After building a city, click 'Refresh Building Materials'\n" +
            "to detect all the new buildings with emission maps!",
            MessageType.None);
    }
}
