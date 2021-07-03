using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Naughty.Attributes.Test
{
	//[CreateAssetMenu(fileName = "NaughtyScriptableObject", menuName = "NaughtyAttributes/_NaughtyScriptableObject")]
	public class _NaughtyScriptableObject : ScriptableObject
	{
		[Expandable]
		public List<_TestScriptableObject> list;
	}
}
