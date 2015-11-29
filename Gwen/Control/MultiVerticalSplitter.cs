using System;
using System.Windows.Forms;
using Gwen.ControlInternal;
using System.Collections.Generic;

namespace Gwen.Control
{
    public class MultiVerticalSplitter : Base
    {
		private readonly List<SplitterBar> m_HSplitter;
		private readonly List<Base> m_Sections;

		private List<float> m_HVal=new List<float>(); // 0-1
        private int m_BarSize; // pixels
        private int m_ZoomedSection; // 0-3

        /// <summary>
        /// Invoked when one of the panels has been zoomed (maximized).
        /// </summary>
		public event GwenEventHandler<EventArgs> PanelZoomed;

        /// <summary>
        /// Invoked when one of the panels has been unzoomed (restored).
        /// </summary>
		public event GwenEventHandler<EventArgs> PanelUnZoomed;

        /// <summary>
        /// Invoked when the zoomed panel has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ZoomChanged;

		public event GwenEventHandler<EventArgs> OnSplitMoved;
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSplitter"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public MultiVerticalSplitter(Base parent)
            : base(parent)
        {
			m_Sections = new List<Base> ();

			m_HSplitter = new List<SplitterBar>(){new SplitterBar(this)};
            m_HSplitter[0].SetPosition(128, 0);
            m_HSplitter[0].Dragged += OnHorizontalMoved;
            m_HSplitter[0].Cursor = Cursors.SizeWE;//remove winforms dependency

			m_HVal.Add(0.5f);

            SetPanel(0, null);
            SetPanel(1, null);

            SplitterSize = 5;
            SplittersVisible = false;

            m_ZoomedSection = -1;
        }

        /// <summary>
        /// Centers the panels so that they take even amount of space.
        /// </summary>
        public void CenterPanels()
        {
			var evenSplit =1f / m_Sections.Count;
			for (int i = 0; i < m_HSplitter.Count; i++)
				m_HVal[i] = evenSplit*(i+1f);
            Invalidate();
        }

        public void SetHValue(float value)
        {
            //if (value <= 1f || value >= 0)
              //  m_HVal = value;
        }

        /// <summary>
        /// Indicates whether any of the panels is zoomed.
        /// </summary>
        public bool IsZoomed { get { return m_ZoomedSection != -1; } }

        /// <summary>
        /// Gets or sets a value indicating whether splitters should be visible.
        /// </summary>
        public bool SplittersVisible
        {
            get { return m_HSplitter[0].ShouldDrawBackground; }
            set
            {
                m_HSplitter[0].ShouldDrawBackground = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the splitter.
        /// </summary>
        public int SplitterSize { get { return m_BarSize; } set { m_BarSize = value; } }

		private void UpdateHSplitter(SplitterBar split)
        {
			split.MoveTo((Width - split.Width) * (m_HVal[m_HSplitter.IndexOf(split)]), split.Y);
        }

		protected void OnHorizontalMoved(Base control, EventArgs args)
        {
			m_HVal[m_HSplitter.IndexOf(control as SplitterBar)] = CalculateValueHorizontal(control);
            Invalidate();
			if (OnSplitMoved != null)
				OnSplitMoved (control, EventArgs.Empty);
        }

		private float CalculateValueHorizontal(Base control)
        {
			var id = m_HSplitter.IndexOf (control as SplitterBar);
			if (id == 0) {
				if(control.X < MinimumSize.X)
				control.X = MinimumSize.X;
				else if (m_HSplitter [id + 1].X - control.X < MinimumSize.X)
					control.X = m_HSplitter [id + 1].X - MinimumSize.X;
			}
			else if (id == m_HSplitter.Count - 1) {
				if(control.X > Width - MinimumSize.X)
				control.X = Width - MinimumSize.X;
				else if (control.X - m_HSplitter [id - 1].X < MinimumSize.X)
					control.X = m_HSplitter [id - 1].X + MinimumSize.X;
			}
			else if(id>0 && id < m_HSplitter.Count - 1) {
				if (control.X - m_HSplitter [id - 1].X < MinimumSize.X)
					control.X = m_HSplitter [id - 1].X + MinimumSize.X;
				else if (m_HSplitter [id + 1].X - control.X < MinimumSize.X)
					control.X = m_HSplitter [id + 1].X - MinimumSize.X;
			}
			return control.X / (float)(Width - control.Width);
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.Base skin)
        {
			for (int i = 0; i < m_HSplitter.Count; i++) {
				m_HSplitter[i].SetSize (m_BarSize, Height);
				UpdateHSplitter (m_HSplitter[i]);
			}
            if (m_ZoomedSection == -1)
            {
				if (m_Sections != null) {
					m_Sections [0].SetBounds (0, 0, m_HSplitter[0].X, Height);

					if (m_Sections.Count > 1)
						for (int i = 1; i < m_Sections.Count; i++)
							if (m_Sections [i] != null)
								m_Sections [i].SetBounds (m_HSplitter [i - 1].X + m_BarSize, 0,(i== m_Sections.Count-1) ? Width - (m_HSplitter [i - 1].X + m_BarSize):m_HSplitter [i].X- m_HSplitter [i-1].X- m_BarSize, Height);
				}
			}
        }

        /// <summary>
        /// Assigns a control to the specific inner section.
        /// </summary>
        /// <param name="index">Section index (0-3).</param>
        /// <param name="panel">Control to assign.</param>
        public void SetPanel(int index, Base panel)
        {
			if (index > m_HSplitter.Count) {
				m_HSplitter.Add (new SplitterBar(this));
				m_HVal.Add (0.75f);
				m_HSplitter[index-1].SetPosition(128, 0);
				m_HSplitter[index-1].Dragged += OnHorizontalMoved;
				m_HSplitter[index-1].Cursor = Cursors.SizeWE;
				m_HSplitter[index-1].ShouldDrawBackground = false;

			}

			if (index>m_Sections.Count-1)
				m_Sections.Add (panel);
			else
				m_Sections [index] = panel;
				

            if (panel != null)
            {
                panel.Dock = Pos.None;
                panel.Parent = this;
            }
			CenterPanels ();
            Invalidate();
        }
        
        /// <summary>
        /// Gets the specific inner section.
        /// </summary>
        /// <param name="index">Section index (0-3).</param>
        /// <returns>Specified section.</returns>
        public Base GetPanel(int index)
        {
            return m_Sections[index];
        }

        /// <summary>
        /// Internal handler for the zoom changed event.
        /// </summary>
        protected void OnZoomChanged()
        {
            if (ZoomChanged != null)
				ZoomChanged.Invoke(this, EventArgs.Empty);

            if (m_ZoomedSection == -1)
            {
                if (PanelUnZoomed != null)
					PanelUnZoomed.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (PanelZoomed != null)
					PanelZoomed.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Maximizes the specified panel so it fills the entire control.
        /// </summary>
        /// <param name="section">Panel index (0-3).</param>
        public void Zoom(int section)
        {
            UnZoom();

            if (m_Sections[section] != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (i != section && m_Sections[i] != null)
                        m_Sections[i].IsHidden = true;
                }
                m_ZoomedSection = section;

                Invalidate();
            }
            OnZoomChanged();
        }

        /// <summary>
        /// Restores the control so all panels are visible.
        /// </summary>
        public void UnZoom()
        {
            m_ZoomedSection = -1;
            
            for (int i = 0; i < 2; i++)
            {
                if (m_Sections[i] != null)
                    m_Sections[i].IsHidden = false;
            }
            
            Invalidate();
            OnZoomChanged();
        }
    }
}
