﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sharp.AssetPipeline;

namespace Sharp.Editor.Attribs
{
	[AttributeUsage(AttributeTargets.Class)]
	public class SupportedFileFormatsAttribute: Attribute
	{
		public string[] supportedFileFormats;
		public static Dictionary<string, Pipeline> importers=new Dictionary<string, Pipeline>();

		static SupportedFileFormatsAttribute(){
			var type = typeof(Pipeline);
			var subclasses=type.Assembly.GetTypes ().Where (t => t.IsSubclassOf (type));

			foreach (var subclass in subclasses) {
				var attr=subclass.GetCustomAttributes (typeof(SupportedFileFormatsAttribute), false)[0] as SupportedFileFormatsAttribute;
				var importer = Activator.CreateInstance (subclass) as AssetPipeline.Pipeline;
				foreach(var format in attr.supportedFileFormats)
					importers.Add(format, importer);
			}
		}

		public SupportedFileFormatsAttribute (params string[] formats)
		{
				supportedFileFormats=formats;
		}
	}
}

