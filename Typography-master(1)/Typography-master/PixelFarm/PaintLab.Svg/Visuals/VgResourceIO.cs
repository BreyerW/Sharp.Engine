﻿//MIT, 2014-present, WinterDev

using System;
namespace PaintLab.Svg
{ 
    public static class VgResourceIO
    {
        //IO 
        [System.ThreadStatic]
        static Action<LayoutFarm.ImageBinder, VgVisualElement, object> s_vgIO;
        public static Action<LayoutFarm.ImageBinder, VgVisualElement, object> VgImgIOHandler
        {
            get
            {
                return s_vgIO;
            }
            set
            {
                if (value == null)
                {
                    //clear existing value
                    s_vgIO = null;
                }
                else
                {
                    if (s_vgIO == null)
                    {
                        //please note that if the system already has one=>
                        //we do not replace it
                        s_vgIO = value;
                    }
                }
            }
        }
    }

}