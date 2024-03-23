using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.Services
{
    public class PIDManager : IDVBTManager<long>
    {
        private bool _scanning = false;

        public PIDManager(ILoggingService loggingService, IDVBTDriverManager driver) :
            base(loggingService, driver)
        {


        }

        public async Task<List<long>> Scan(long programMapPID, int msTimeout = 2000)
        {
            var searchPIDsres = await _driver.SearchProgramPIDs(programMapPID, false);

            if (searchPIDsres.Result == SearchProgramResultEnum.OK)
            {
                AddItemsToDB(_driver.LastTunedFreq, programMapPID, searchPIDsres.PIDs);
            }

            return null;
        }

    }
}
