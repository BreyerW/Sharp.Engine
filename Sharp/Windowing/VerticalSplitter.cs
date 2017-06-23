using System;
using System.Collections.Generic;
using System.Linq;
using TupleExtensions;

namespace Sharp.Windowing
{
    internal class VerticalSplitter : ISplitter
    {
        public Window parent;
        private readonly List<Window> m_Sections = new List<Window>();
        private List<float> m_HVal = new List<float>();

        public VerticalSplitter(Window p)
        {
            parent = p;
        }

        private int count = 0;
        public int Count => count;

        public Window GetPanel(int index)
        {
            return m_Sections[index];
        }

        public void RecalculateLayout()
        {
            var evenSplit = 1f / m_Sections.Count;
            for (int i = 0; i < m_HVal.Count; i++)
                m_HVal[i] = evenSplit; //* (i + 1f);
        }

        public void SetPanel(int index, Window win)
        {
            if (index > m_Sections.Count - 1)
            {
                m_Sections.Add(win);
                count += 1;
            }
            else
                m_Sections[index] = win;
            m_HVal.Add(0.5f);
            RecalculateLayout();
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            var size = parent.Size;
            var pos = parent.Position;
            foreach (var (i, win) in m_Sections.WithIndexes())
            {
                m_Sections[i].Position = (pos.x + (int)(size.width * (i is 0 ? 0 : m_HVal[i - 1] * i)), pos.y + 100);
                m_Sections[i].Size = ((int)(size.width * m_HVal[i]), size.height - 100);
            }
        }
    }
}