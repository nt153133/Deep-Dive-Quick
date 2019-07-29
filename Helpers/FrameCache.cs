﻿/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */
using System;
using ff14bot;

namespace Deep2.Helpers
{
    internal class FrameCache<T>
    {
        private Func<T> _producer;
        private uint _lastFrame = uint.MaxValue;
        private T _cached;

        public FrameCache(Func<T> producer)
        {
            _producer = producer ?? throw new ArgumentNullException("producer");
        }

        public T Value
        {
            get
            {
                var frameCount = Core.Memory.Executor.FrameCount;
                if (_lastFrame != frameCount)
                {
                    _cached = _producer();
                    _lastFrame = frameCount;
                }
                return _cached;
            }
        }

        internal bool NeedsUpdating => Core.Memory.Executor.FrameCount != _lastFrame;

        public static implicit operator T(FrameCache<T> pfcv)
        {
            return pfcv.Value;
        }

    }
}