using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Server.Dal.Uri.Interfaces
{
    public interface ICloudUriHandler
    {
        Task<List<string>> ListFilesInFolder(string filter);
        Task<string> GetFileInFolder(string folderName, string fileName);
        Task WriteFileInFolder(string folderName, string fileName, string fileContent);
    }
}
