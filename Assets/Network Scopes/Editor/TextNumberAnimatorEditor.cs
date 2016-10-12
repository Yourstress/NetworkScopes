using UnityEditor;

[CustomEditor(typeof(TextNumberAnimator))]
public class TextNumberAnimatorEditor : Editor
{
	const float scaleUpDuration = 0.06f;
	const float scaleDownDuration = 0.06f;

	const float scaleUpFactor = 1.25f;

	new public TextNumberAnimator target { get { return (TextNumberAnimator)base.target; } }

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();
	}
}
