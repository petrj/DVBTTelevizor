using DVBTTelevizor.Models;
using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.Services
{
    public class PIDManager : DBManager<ChannelPID>
    {
        public override string Key { get; set; } = "PID";
        public int ValidityDays { get; set; } = 14;

        public PIDManager(ILoggingService loggingService, IDVBTDriverManager driver) :
            base(loggingService, driver)
        {

        }

        public List<long> GetChannelPIDs(long freq, long programMapPID)
        {
            var res = new List<long>();

            var cachedPIDs = GetValues(freq, programMapPID);
            if (cachedPIDs == null || cachedPIDs.Count == 0)
                return res;

            if ((DateTime.Now-cachedPIDs[0].Time).TotalDays>ValidityDays)
            {
                return res;
            }

            foreach (var pid in cachedPIDs)
            {
                res.Add(pid.PID);
            }

            return res;
        }

        public void AddChannelPIDs(long freq, long programMapPID, List<long> PIDs)
        {
            var channelPIDS = new List<ChannelPID>();
            foreach (var pid in PIDs)
            {
                channelPIDS.Add(new ChannelPID()
                {
                    PID = pid,
                    Time = DateTime.Now
                });
            }

            AddItemsToDB(freq, programMapPID, channelPIDS);
        }

        public async Task<List<long>> Scan(long programMapPID, int msTimeout = 2000)
        {
            //var searchPIDsres = await _driver.SearchProgramPIDs(programMapPID, false);

            //if (searchPIDsres.Result == SearchProgramResultEnum.OK)
            //{
            //    AddItemsToDB(_driver.LastTunedFreq, programMapPID, searchPIDsres.PIDs);
            //}

            return null;
        }

    }
}
