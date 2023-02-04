using System;

namespace Witcher3UpdateHelper
{
    internal class Disposable : IDisposable
    {
        private readonly Func<string> p;

        public Disposable(Func<string> p)
        {
            this.p = p;
        }

        public void Dispose()
        {
            this.p();
        }

        internal static IDisposable Create(Func<string> p)
        {
            return new Disposable(p);
        }
    }
}