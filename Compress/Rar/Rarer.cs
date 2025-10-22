using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SharpCompress;
using SharpCompress.Archives;

using Thmd.Consolas;

namespace Thmd.Compress.Rar
{
    public class Rarer
    {
        Action<double> _progress;
        CancellationToken _cancellationToken;
        public void UnRar(string source_path, string target_directory)
        {
            try
            {
                if (SharpCompress.Archives.Rar.RarArchive.IsRarFile(source_path))
                {
                    var rar = SharpCompress.Archives.Rar.RarArchive.Open(source_path);
                    rar.ExtractToDirectory(target_directory, _progress, _cancellationToken);
                }
                else
                {
                    this.WriteLine("File is not a compressed rar file.");
                    
                    return;
                }
            }
            catch (Exception ex)
            {
                this.WriteLine(ex);
            }
            
        }
    }
}
// Version: 0.1.7.87
