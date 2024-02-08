using System.Threading.Tasks;

namespace DotNetReleaser.Runners;

public class DotNetRunner : DotNetRunnerBase
{
    public DotNetRunner(string command) : base(command)
    {
    }

    public async Task<CommandResulExtended> Run()
    {
        return await RunImpl();
    }
}