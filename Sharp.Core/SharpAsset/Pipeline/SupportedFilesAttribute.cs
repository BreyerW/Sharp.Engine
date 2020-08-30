﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpAsset.Pipeline
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedFilesAttribute : Attribute
    {
        public string[] supportedFileFormats;

        public SupportedFilesAttribute(params string[] formats)
        {
            supportedFileFormats = formats;
        }
    }
}