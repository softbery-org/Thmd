// Version: 0.1.1.20
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
