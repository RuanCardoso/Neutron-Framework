using System;

namespace NeutronNetwork.Naughty.Attributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class EnumFlagsAttribute : DrawerAttribute
	{
	}
}
