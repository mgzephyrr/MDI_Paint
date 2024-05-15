using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PluginInterface
{
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; }

        public ProgressEventArgs(int progress)
        {
            Progress = progress;
        }
    }

    public interface IPlugin
    {
        event EventHandler<ProgressEventArgs> ProgressChanged;
        string Name { get; }
        string ButtonName { get; }
        string Author { get; }
        Task<InkCanvas> Transform(InkCanvas app, CancellationTokenSource cts);
    }
}
