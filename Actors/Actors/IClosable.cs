using System;
using System.Threading.Tasks;

namespace Actors
{
    public interface IClosable
    {
        Task Close();
    }

    public class RootClosable : IClosable
    {
        public async Task Close()
        {
            await Task.Yield();
        }
    }
}