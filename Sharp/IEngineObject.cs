using System;

namespace Sharp
{
    public interface IEngineObject/*:IDisposable*/
    {
        Guid Id { get; }

        void Destroy();
    }
}