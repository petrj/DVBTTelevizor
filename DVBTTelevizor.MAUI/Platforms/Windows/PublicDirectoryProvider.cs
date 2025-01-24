using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class PublicDirectoryProvider : IPublicDirectoryProvider
    {
        public string GetPublicDirectoryPath()
        {
            var dir = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "DVBTTelevizor");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }
}
