using System;
using Microsoft.Build.Framework;

namespace MsBuildPipeLogger
{
    public interface IPipeWriter : IDisposable
    {
        void Write(BuildEventArgs e);
    }
}
