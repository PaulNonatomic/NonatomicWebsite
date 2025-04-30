using UnityEngine;

namespace Game.Intro
{
	public class SetFPS : MonoBehaviour
	{
		public void Start()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
		}
	}
}