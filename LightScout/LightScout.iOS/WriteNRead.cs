﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Foundation;
using LightScout.Models;
using Newtonsoft.Json;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(LightScout.iOS.WriteNRead))]
namespace LightScout.iOS
{
    public class WriteNRead : DataStore
    {
        public void SaveDummyData(string filename)
        {
            var docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                System.IO.Directory.CreateDirectory(Path.Combine(docpath, "FRCLightScout"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            docpath = Path.Combine(docpath, "FRCLightScout");
            var data = "";
            var tabletid = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).TabletIdentifier;
            var finalPath = Path.Combine(docpath, filename);
            var DummyDataMatches = new List<TeamMatch>();
            var match = new TeamMatch();
            match.MatchNumber = 1;
            match.TeamNumber = 1023;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            match = new TeamMatch();
            match.MatchNumber = 2;
            match.TeamNumber = 862;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            match = new TeamMatch();
            match.MatchNumber = 3;
            match.TeamNumber = 862;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            match = new TeamMatch();
            match.MatchNumber = 4;
            match.TeamNumber = 862;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            match = new TeamMatch();
            match.MatchNumber = 5;
            match.TeamNumber = 862;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            match = new TeamMatch();
            match.MatchNumber = 6;
            match.TeamNumber = 862;
            match.PowerCellInner = new int[21];
            match.PowerCellOuter = new int[21];
            match.PowerCellLower = new int[21];
            match.PowerCellMissed = new int[21];
            match.TabletId = tabletid;
            match.EventCode = JsonConvert.DeserializeObject<LSConfiguration>(LoadConfigFile()).CurrentEventCode;
            DummyDataMatches.Add(match);
            data = JsonConvert.SerializeObject(DummyDataMatches);
            try
            {
                System.IO.File.WriteAllText(finalPath, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void SaveData(string filename, TeamMatch modeldata)
        {
            //ADD FILE PARSING HERE
            var docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                System.IO.Directory.CreateDirectory(Path.Combine(docpath, "FRCLightScout"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            docpath = Path.Combine(docpath, "FRCLightScout");
            var data = "";
            var finalPath = Path.Combine(docpath, filename);
            var beforedata = "";
            try
            {
                beforedata = File.ReadAllText(finalPath);
                var modelstochange = JsonConvert.DeserializeObject<List<TeamMatch>>(beforedata);
                var specificmodeltochange = modelstochange.Where(x => x.TeamNumber == modeldata.TeamNumber && x.MatchNumber == modeldata.MatchNumber).FirstOrDefault();

                specificmodeltochange.MatchNumber = modeldata.MatchNumber;
                specificmodeltochange.TeamNumber = modeldata.TeamNumber;

                modelstochange.Remove(modelstochange.Where(x => x.TeamNumber == modeldata.TeamNumber && x.MatchNumber == modeldata.MatchNumber).FirstOrDefault());
                modelstochange.Add(modeldata);
                modelstochange = modelstochange.OrderBy(x => x.MatchNumber).ToList();

                data = JsonConvert.SerializeObject(modelstochange);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot find specified match in file system. Checking source of exception...");
                try
                {
                    var modelstochange = JsonConvert.DeserializeObject<List<TeamMatch>>(beforedata);
                    modelstochange.Add(modeldata);
                    modelstochange = modelstochange.OrderBy(x => x.MatchNumber).ToList();
                    data = JsonConvert.SerializeObject(modelstochange);
                }
                catch (Exception exjson)
                {
                    var result = LoadData("LSConfiguration.txt");
                    var createlistofthissize = JsonConvert.DeserializeObject<LSConfiguration>(result).MaxMatches;
                    List<TeamMatch> newTeamMatchList = new List<TeamMatch>();
                    newTeamMatchList.Add(modeldata);
                    data = JsonConvert.SerializeObject(newTeamMatchList);
                }

            }

            try
            {
                System.IO.File.WriteAllText(finalPath, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public string LoadData(string filename)
        {
            var docpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FRCLightScout");
            var finalPath = Path.Combine(docpath, filename);
            string result = null;
            try
            {
                result = File.ReadAllText(finalPath);
            }
            catch (Exception ex)
            {
                SaveDummyData("JacksonEvent2020.txt");
                result = File.ReadAllText(finalPath);
            }

            return result;
        }
        public void SaveDefaultData(string filename, List<TeamMatch> teamMatches)
        {
            var docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                System.IO.Directory.CreateDirectory(Path.Combine(docpath, "FRCLightScout"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            docpath = Path.Combine(docpath, "FRCLightScout");
            var finalPath = Path.Combine(docpath, filename);
            var data = JsonConvert.SerializeObject(teamMatches);
            try
            {
                System.IO.File.WriteAllText(finalPath, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public string LoadConfigFile()
        {
            var docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            string result = null;
            try
            {
                System.IO.Directory.CreateDirectory(Path.Combine(docpath, "FRCLightScout"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            docpath = Path.Combine(docpath, "FRCLightScout");
            var finalPath = Path.Combine(docpath, "LSConfiguration.txt");
            try
            {
                
                result = File.ReadAllText(finalPath);
                var samplewaytochange = JsonConvert.DeserializeObject<LSConfiguration>(result);
            }
            catch (Exception ex)
            {
                var newconfigfile = new LSConfiguration();
                newconfigfile.CurrentEventCode = "2020mijac";
                newconfigfile.ScouterNames = new string[2] { "John Doe", "Imaex Ample" };
                File.WriteAllText(finalPath, JsonConvert.SerializeObject(newconfigfile));
                result = File.ReadAllText(finalPath);
            }

            return result;
        }
        public void SaveConfigurationFile(string configtype, object newvalue)
        {
            var docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                System.IO.Directory.CreateDirectory(Path.Combine(docpath, "FRCLightScout"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            docpath = Path.Combine(docpath, "FRCLightScout");
            var finalPath = Path.Combine(docpath, "LSConfiguration.txt");
            var modeltochange = new LSConfiguration();
            try
            {
                var beforedata = File.ReadAllText(finalPath);
                modeltochange = JsonConvert.DeserializeObject<LSConfiguration>(beforedata);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot find specified match in file system. Creating configuration file...");
                modeltochange = new LSConfiguration();
                modeltochange.CurrentEventCode = "2020mijac";
                modeltochange.ScouterNames = new string[3] { "John Doe", "Imaex Ample", "Guest Scouter" };
            }
            switch (configtype)
            {
                case "numMatches":
                    modeltochange.NumberOfMatches = (int)newvalue;
                    break;
                case "tabletId":
                    modeltochange.TabletIdentifier = (string)newvalue;
                    break;
                case "maxMatches":
                    modeltochange.MaxMatches = (int)newvalue;
                    break;
                case "bluetoothStage":
                    modeltochange.BluetoothFailureStage = (int)newvalue;
                    break;
                case "scoutNames":
                    modeltochange.ScouterNames = (string[])newvalue;
                    break;
                case "ownerTeamChange":
                    modeltochange.TeamOfOwnership = (int)newvalue;
                    break;
                case "ownerScoutChange":
                    modeltochange.ScouterOfOwnership = (string)newvalue;
                    break;
                case "eventCode":
                    modeltochange.CurrentEventCode = (string)newvalue;
                    break;
                case "selectedMaster":
                    modeltochange.SelectedDeviceInformation = (DeviceInformation)newvalue;
                    break;
                case "scoutCode":
                    modeltochange.ScoutAuthCode = int.Parse((string)newvalue).ToString("0000");
                    break;
               
            }
            try
            {
                File.WriteAllText(finalPath, JsonConvert.SerializeObject(modeltochange));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}