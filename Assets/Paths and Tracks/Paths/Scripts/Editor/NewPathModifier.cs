using UnityEngine;
using System;
using System.Reflection;

using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{


	/// <summary>
	/// Used as a temporary placeholder IPathModifier instance in the list
	/// modifiers list. Whenever user selects the actual type, this instance
	/// will be replaced by the actual instance.
	/// </summary>
	/// 
	internal class NewPathModifier : AbstractPathModifier
	{

		public int pathModifierIndex = -1;
		public IPathModifier pathModifier;

		public override bool IsEnabled ()
		{
			return false;
		}
        
		public override PathPoint[] GetModifiedPoints (PathPoint[] points, PathModifierContext context)
		{
			throw new NotImplementedException ();
		}
        
		public override int GetRequiredInputFlags ()
		{
			return (null != pathModifier) ? pathModifier.GetRequiredInputFlags () : base.GetRequiredInputFlags ();
		}

		public override int GetProcessFlags (PathModifierContext context)
		{
			return (null != pathModifier) ? pathModifier.GetProcessFlags (context) : base.GetProcessFlags (context);
		}

		public override int GetPassthroughFlags (PathModifierContext context)
		{
			return (null != pathModifier) ? pathModifier.GetPassthroughFlags (context) : base.GetPassthroughFlags (context);
		}

		public override int GetGenerateFlags (PathModifierContext context)
		{
			return (null != pathModifier) ? pathModifier.GetGenerateFlags (context) : base.GetGenerateFlags (context);
		}
	}
}
