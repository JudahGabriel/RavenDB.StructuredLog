using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;

namespace RavenDB.StructuredLog
{
    internal class RavenStructuredLogScope : IDisposable
    {
        private readonly Subject<Unit> disposedSignal = new Subject<Unit>();

        public RavenStructuredLogScope(object value)
        {
            this.Value = value;
        }

        public object Value { get; private set; }
        public IObservable<Unit> Disposed => this.disposedSignal;

        public void Dispose()
        {
            if (!disposedSignal.IsDisposed)
            {
                disposedSignal.OnNext(Unit.Default);
                disposedSignal.OnCompleted();
                disposedSignal.Dispose();
            }
        }
    }
}
