// Version: 0.1.15.17
using System;
using System.IO;
using System.Threading.Tasks;

namespace Thmd.Media;

public interface IMediaStream : IDisposable
{
	Task<Stream> GetStreamAsync();

	Task<Stream> GetStream();

	double GetDuration();
}
