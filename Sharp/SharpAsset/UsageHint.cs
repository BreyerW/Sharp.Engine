using System;

namespace SharpAsset
{
	public enum UsageHint
	{
		StreamDraw = 35040,
		StreamRead,
		StreamCopy,
		StaticDraw = 35044,
		StaticRead,
		StaticCopy,
		DynamicDraw = 35048,
		DynamicRead,
		DynamicCopy
	}
}

