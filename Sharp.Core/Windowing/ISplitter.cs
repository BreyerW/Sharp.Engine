using System;
using System.Collections.Generic;

namespace Sharp.Windowing
{
    internal interface ISplitter
    {
        int Count { get; }

        void SetPanel(int index, Window win);

        Window GetPanel(int index);

        void RecalculateLayout();

        void UpdateLayout();
    }
}