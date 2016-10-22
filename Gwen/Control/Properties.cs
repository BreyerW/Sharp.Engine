using System;
using System.Windows.Forms;
using Gwen.ControlInternal;

namespace Gwen.Control
{
	/// <summary>
	/// Properties table.
	/// </summary>
	public class Properties : Base
	{
		//private readonly SplitterBar m_SplitterBar;

		/// <summary>
		/// Returns the width of the first column (property names).
		/// </summary>
		//public int SplitWidth { get { return m_SplitterBar.X; } } // todo: rename?

		/// <summary>
		/// Invoked when a property value has been changed.
		/// </summary>
		public event GwenEventHandler<EventArgs> ValueChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="Properties"/> class.
		/// </summary>
		/// <param name="parent">Parent control.</param>
		public Properties(Base parent)
			: base(parent)
		{
			//m_SplitterBar = new SplitterBar(this);
			// m_SplitterBar.SetPosition(80, 0);
			// m_SplitterBar.Cursor = Cursors.SizeWE;
			//m_SplitterBar.Dragged += OnSplitterMoved;
			// m_SplitterBar.ShouldDrawBackground = false;
		}

		/// <summary>
		/// Function invoked after layout.
		/// </summary>
		/// <param name="skin">Skin to use.</param>
		protected override void PostLayout(Skin.Base skin)
		{
			// m_SplitterBar.Height = 0;

			if (SizeToChildren(false, true))
			{
				InvalidateParent();
			}

			// m_SplitterBar.SetSize(3, Height);
		}

		/// <summary>
		/// Handles the splitter moved event.
		/// </summary>
		/// <param name="control">Event source.</param>
		protected virtual void OnSplitterMoved(Base control, EventArgs args)
		{
			InvalidateChildren();
		}

		/// <summary>
		/// Adds a new text property row.
		/// </summary>
		/// <param name="label">Property name.</param>
		/// <param name="value">Initial value.</param>
		/// <returns>Newly created row.</returns>
		public PropertyRow<string> Add(string label, string value = "")
		{
			return Add<string>(label, new Property.Text(this), value);
		}
		/*public Base Add(string label, object value)
		{
			switch (value.GetType()) {
			//case typeof()
			}
			PropertyRow<T> row = new PropertyRow<T>(this, prop);
			row.Dock = Pos.Top;
			row.Label = label;
			row.ValueChanged += OnRowValueChanged;

			prop.SetValue(value, true);

			// m_SplitterBar.BringToFront();
			return row;
		}*/

		/// <summary>
		/// Adds a new property row.
		/// </summary>
		/// <param name="label">Property name.</param>
		/// <param name="prop">Property control.</param>
		/// <param name="value">Initial value.</param>
		/// <returns>Newly created row.</returns>
		public PropertyRow<T> Add<T>(string label, Property.Base<T> prop, T value = default(T))
		{
			PropertyRow<T> row = new PropertyRow<T>(this, prop);
			row.Dock = Pos.Top;
			row.Label = label;
			row.Name = label;
			row.ValueChanged += OnRowValueChanged;

			prop.SetValue(value, true);

			// m_SplitterBar.BringToFront();
			return row;
		}
		public PropertyRow<T> Get<T>(string label)
		{
			return FindChildByName(label) as PropertyRow<T>;
		}
		private void OnRowValueChanged(Base control, EventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged.Invoke(control, EventArgs.Empty);
		}

		/// <summary>
		/// Deletes all rows.
		/// </summary>
		public void DeleteAll()
		{
			m_InnerPanel.DeleteAllChildren();
		}
	}
}
