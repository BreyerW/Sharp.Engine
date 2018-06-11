using System;

namespace Sharp
{
    internal interface IEngineObject/*:IDisposable*/
    {
        Guid Id { get; }

        void Destroy();
    }
}