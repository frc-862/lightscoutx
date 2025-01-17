﻿using LightScout.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LightScout
{
    public interface DataStore
    {
        void SaveData(string competitionfile, TeamMatch datatosave);
        void SaveDummyData(string competitionfile);
        string LoadData(string competitionfile);
        string LoadConfigFile();
        void SaveConfigurationFile(string configurationtype, object newvalue);
        void SaveDefaultData(string filename, List<TeamMatch> teamMatches);
    }
}
