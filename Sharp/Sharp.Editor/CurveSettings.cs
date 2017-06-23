using System;
using System.Collections.Generic;

namespace Sharp.Editor
{
    public class CurveSettings
    {
        //
        // Fields
        //
        private TickStyle m_HTickStyle = new TickStyle();

        private bool m_VSlider = true;

        private bool m_HSlider = true;

        public bool allowDeleteLastKeyInCurve;

        public bool allowDraggingCurvesAndRegions = true;

        public bool showAxisLabels = true;

        public bool useFocusColors;

        public Color wrapColor = new Color(1, 1, 1, 0.5f);

        private bool m_ScaleWithWindow = true;

        private float m_VRangeMax = float.PositiveInfinity;

        private float m_VRangeMin = -float.PositiveInfinity;

        private float m_HRangeMax = float.PositiveInfinity;

        private float m_HRangeMin = -float.PositiveInfinity;

        private bool m_VRangeLocked;

        private bool m_HRangeLocked;

        private TickStyle m_VTickStyle = new TickStyle();

        public float hTickLabelOffset = 4;

        //
        // Properties
        //
        public bool hasUnboundedRanges
        {
            get
            {
                return this.m_HRangeMin == -float.PositiveInfinity || this.m_HRangeMax == float.PositiveInfinity || this.m_VRangeMin == -float.PositiveInfinity || this.m_VRangeMax == float.PositiveInfinity;
            }
        }

        internal bool hRangeLocked
        {
            get
            {
                return this.m_HRangeLocked;
            }
            set
            {
                this.m_HRangeLocked = value;
            }
        }

        public float hRangeMax
        {
            get
            {
                return this.m_HRangeMax;
            }
            set
            {
                this.m_HRangeMax = value;
            }
        }

        public float hRangeMin
        {
            get
            {
                return this.m_HRangeMin;
            }
            set
            {
                this.m_HRangeMin = value;
            }
        }

        public bool hSlider
        {
            get
            {
                return this.m_HSlider;
            }
            set
            {
                this.m_HSlider = value;
            }
        }

        public TickStyle hTickStyle
        {
            get
            {
                return this.m_HTickStyle;
            }
            set
            {
                this.m_HTickStyle = value;
            }
        }

        internal bool scaleWithWindow
        {
            get
            {
                return this.m_ScaleWithWindow;
            }
            set
            {
                this.m_ScaleWithWindow = value;
            }
        }

        internal bool vRangeLocked
        {
            get
            {
                return this.m_VRangeLocked;
            }
            set
            {
                this.m_VRangeLocked = value;
            }
        }

        public float vRangeMax
        {
            get
            {
                return this.m_VRangeMax;
            }
            set
            {
                this.m_VRangeMax = value;
            }
        }

        public float vRangeMin
        {
            get
            {
                return this.m_VRangeMin;
            }
            set
            {
                this.m_VRangeMin = value;
            }
        }

        public bool vSlider
        {
            get
            {
                return this.m_VSlider;
            }
            set
            {
                this.m_VSlider = value;
            }
        }

        public TickStyle vTickStyle
        {
            get
            {
                return this.m_VTickStyle;
            }
            set
            {
                this.m_VTickStyle = value;
            }
        }

        //
        // Constructors
        //
        public CurveSettings()
        {
            if (false /* isDarkSkin*/)
            {
                this.wrapColor = new Color(0.65f, 0.65f, 0.65f, 0.5f);
            }
            else
            {
                this.wrapColor = new Color(1, 1, 1, 0.5f);
            }
        }
    }

    public class TickStyle
    {
        //
        // Fields
        //
        public Color color = new Color(0, 0, 0, 0.2f);

        public Color labelColor = new Color(0, 0, 0, 1f);

        public int distMin = 10;

        public int distFull = 80;

        public int distLabel = 50;

        public bool stubs;

        public bool centerLabel;

        public string unit = string.Empty;

        //
        // Constructors
        //
        public TickStyle()
        {
            if (false/*isDarkSkin*/)
            {
                this.color = new Color(0.45f, 0.45f, 0.45f, 0.2f);
                this.labelColor = new Color(0.8f, 0.8f, 0.8f, 0.32f);
            }
            else
            {
                this.color = new Color(0, 0, 0, 0.2f);
                this.labelColor = new Color(0, 0, 0, 0.32f);
            }
        }
    }
}