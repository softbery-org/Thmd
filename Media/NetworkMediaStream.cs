// Version: 0.1.0.74
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Thmd.Media;

public class NetworkMediaStream : IMediaStream, IDisposable
{
	private readonly string _url;

	public NetworkMediaStream(string url)
	{
		_url = url;
	}

	public async Task<Stream> GetStreamAsync()
	{
		HttpClient client = new HttpClient();
		try
		{
			return await client.GetStreamAsync(_url);
		}
		finally
		{
			((IDisposable)client)?.Dispose();
		}
	}

	public double GetDuration()
	{
		return 180.0;
	}

	public Task<Stream> GetStream()
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
