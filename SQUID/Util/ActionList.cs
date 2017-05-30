﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Squid
{
    public abstract class GuiAction
    {
        internal Control _control;
        internal GuiActionList _list;

        protected GuiActionList Actions { get { return _list; } }
        protected Control Control { get { return _control; } }

        public event VoidEvent Finished;
        public event VoidEvent Started;
        public event VoidEvent Updated;

        //public int Lanes;
        public bool IsBlocking = true;

        protected bool _isFinished;
        public bool IsFinished
        {
            get { return _isFinished; }
            protected set
            {
                _isFinished = value;

                if (_isFinished)
                {
                    End();

                    if (Finished != null)
                        Finished(_control);
                }
            }
        }

        private bool firstRun = true;
        internal void InternalUpdate(float dt)
        {
            if (firstRun)
            {
                firstRun = false;

                Start();

                if (Started != null)
                    Started(_control);
            }

            Update(dt);

            if (Updated != null)
                Updated(_control);
        }

        public virtual void Start() { }
        public virtual void Update(float dt) { }
        public virtual void End() { }
    }

    public class GuiActionList
    {
        internal bool IsUpdating;

        private int index;
        private Control owner;
        private List<GuiAction> Actions = new List<GuiAction>();

        public GuiAction First { get { return Actions.Count > 0 ? Actions[0] : null; } }
        public GuiAction Last { get { return Actions.Count > 0 ? Actions[Actions.Count - 1] : null; } }

        public GuiActionList(Control owner)
        {
            this.owner = owner;
        }

        public void Clear()
        {
            Actions.Clear();
        }

        public GuiAction Add(GuiAction action)
        {
            action._list = this;
            action._control = owner;
            Actions.Add(action);
            return action;
        }

        public void InsertBefore(GuiAction action)
        {
            action._list = this;
            action._control = owner;
            Actions.Insert(index - 1, action);
        }

        public void InsertAfter(GuiAction action)
        {
            action._list = this;
            action._control = owner;
            Actions.Insert(index + 1, action);
        }

        public void Remove(GuiAction action)
        {
            Actions.Remove(action);
        }

        public void Update(float dt)
        {
            IsUpdating = true;

            index = 0;
            int i = index;
            //int lanes = 0;

            while (i < Actions.Count)
            {
                GuiAction action = Actions[i];

                //if ((lanes & action.Lanes) == 0)
                //    continue;

                action.InternalUpdate(dt);

                //if (action.IsBlocking)
                //    lanes |= action.Lanes;

                if (action.IsFinished)
                {
                    // action->OnEnd();
                    Actions.Remove(action);
                }

                i++;
                index = i;

                if (action.IsBlocking)
                    break;
            }

            IsUpdating = false;
        }
    }
}
