/////////////////////////////////////////////////////////////////////////////////
// Paint.NET (MIT,from version 3.36.7, see=> https://github.com/rivy/OpenPDN   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

//MIT, 2017-present, WinterDev

namespace PaintFx
{
    /// <summary>
    /// Abstract base class that all "user" blend ops derive from.
    /// These ops are available in the UI for a user to choose from
    /// in order to configure the blending properties of a Layer.
    /// 
    /// See UserBlendOps.cs for guidelines on implementation.
    /// </summary>
    
    public abstract class UserBlendOp
        : BinaryPixelOp
    {
        public virtual UserBlendOp CreateWithOpacity(int opacity)
        {
            return this;
        }

        public override string ToString()
        {
            return "";
            //return Utility.GetStaticName(this.GetType());
        }
    }
}
