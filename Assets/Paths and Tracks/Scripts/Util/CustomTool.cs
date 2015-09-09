using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CustomTool : System.Attribute
	{
        
		public string name;
		public string description;
        
		public CustomTool () : this(null)
		{
		}

		public CustomTool (string name)
		{
			this.name = name;
		}
        
		public static string GetToolName (Type toolType)
		{
            
			object[] attrs = toolType.GetCustomAttributes (typeof(CustomTool), false);
			if (attrs.Length == 0) {
				attrs = toolType.GetCustomAttributes (typeof(CustomTool), true);
			}
			foreach (object attr in attrs) {
				CustomTool toolAttr = (CustomTool)attr;
				if (!StringUtil.IsEmpty (toolAttr.name)) {
					return toolAttr.name;
				}
			}
			return null;
		}
	}
    
}
