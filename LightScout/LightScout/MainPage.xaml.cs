﻿using LightScout.Models;
using Newtonsoft.Json;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace LightScout
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private bool MenuAnimationActive = false;
        private bool MenuOpen = false;
        private bool NotificationActive = false;
        private int NextMatchIndex = -1;
        private static bool[] ControlPanel = new bool[2];
        private static IBluetoothLE ble = CrossBluetoothLE.Current;
        private static IAdapter adapter = CrossBluetoothLE.Current.Adapter;
        private static IDevice deviceIWant;
        private static ObservableCollection<IDevice> Devices = new ObservableCollection<IDevice>();
        private static bool Balanced;
        private List<TeamMatch> listofmatches = new List<TeamMatch>();
        private List<TeamMatchViewItem> listofviewmatches = new List<TeamMatchViewItem>();
        private List<string> MatchNames = new List<string>();
        private static int BluetoothDevices = 0;
        private static bool TimerAlreadyCreated = false;
        private static int timesalive = 0;
        private string currentCodeString = "";
        private CodeReason currentCodeReason;
        private static SubmitVIABluetooth submitVIABluetoothInstance = new SubmitVIABluetooth();
        private List<string> tabletlist = new List<string>();

        public enum CodeReason
        {
            DeleteMatch,
            EditMatch,
            CreateMatch
        }

        public MainPage()
        {

            InitializeComponent();
            ControlPanel[0] = false;
            ControlPanel[1] = false;
            adapter.DeviceDiscovered += async (s, a) =>
            {

                if (a.Device.Name != null)
                {
                    Devices.Add(a.Device);
                }

            };
            adapter.DeviceConnected += async (s, a) =>
            {
                Console.WriteLine("Connected to: " + a.Device.Name.ToString());
                //status.Text = "Connected to: " + a.Device.Name.ToString();
                deviceIWant = a.Device;
            };
            adapter.DeviceConnectionLost += (s, a) =>
            {
                Console.WriteLine("Lost connection to: " + a.Device.Name.ToString());
                //status.Text = "Disconnected from: " + a.Device.Name.ToString();
                Devices.Clear();
            };
            adapter.DeviceDisconnected += (s, a) =>
            {
                Console.WriteLine("Lost connection to: " + a.Device.Name.ToString());
                //status.Text = "Disconnected from: " + a.Device.Name.ToString();
                Devices.Clear();
            };

            if (!TimerAlreadyCreated)
            {
                Console.WriteLine("Test started :)");
                Device.StartTimer(TimeSpan.FromMinutes(1), () =>
                {
                    timesalive++;
                    Console.WriteLine("This message has appeared " + timesalive.ToString() + " times. Last ping at " + DateTime.Now.ToShortTimeString());
                    TimerAlreadyCreated = true;
                    return true;
                });
            }

            /*Device.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                if(deviceIWant != null)
                {
                    bluetoothHandler.SubmitBluetooth(adapter, deviceIWant);
                }
                
                return true;
            });*/
            try
            {
                var allmatchesraw = DependencyService.Get<DataStore>().LoadData("JacksonEvent2020.txt");
                listofmatches = JsonConvert.DeserializeObject<List<TeamMatch>>(allmatchesraw);
                var upnext = false;
                TeamMatchViewItem selectedItem = null;
                var upnextselected = false;
                int i = 0;
                foreach (var match in listofmatches)
                {

                    var newmatchviewitem = new TeamMatchViewItem();
                    upnext = false;
                    if (!match.ClientSubmitted)
                    {
                        if (!upnextselected)
                        {
                            upnext = true;
                            upnextselected = true;
                        }
                    }
                    newmatchviewitem.Completed = match.ClientSubmitted;
                    if (match.TabletId != null)
                    {
                        newmatchviewitem.IsRed = match.TabletId.StartsWith("R");
                        newmatchviewitem.IsBlue = match.TabletId.StartsWith("B");
                    }

                    newmatchviewitem.IsUpNext = upnext;
                    newmatchviewitem.TeamName = match.TeamName;
                    if (match.TeamName == null)
                    {
                        newmatchviewitem.TeamName = "FRC Team " + match.TeamNumber.ToString();
                    }
                    newmatchviewitem.TeamNumber = match.TeamNumber;
                    newmatchviewitem.NewPlaceholder = false;
                    newmatchviewitem.ActualMatch = true;
                    newmatchviewitem.MatchNumber = match.MatchNumber;
                    newmatchviewitem.TabletName = match.TabletId;
                    newmatchviewitem.teamIcon = ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String("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")));
                    if (upnext)
                    {
                        selectedItem = newmatchviewitem;
                        NextMatchIndex = i;
                    }
                    listofviewmatches.Add(newmatchviewitem);
                    i++;
                }
                var placeholdermatch = new TeamMatchViewItem();
                placeholdermatch.ActualMatch = false;
                placeholdermatch.NewPlaceholder = true;
                listofviewmatches.Add(placeholdermatch);
                listOfMatches.ItemsSource = listofviewmatches;
                carouseluwu.ItemsSource = listofviewmatches;
                if (selectedItem != null)
                {
                    carouseluwu.CurrentItem = selectedItem;
                }
                else
                {
                    carouseluwu.CurrentItem = listofviewmatches.First();
                }
            }catch(Exception ex)
            {

            }
            

            tabletlist = new string[6] { "R1", "R2", "R3", "B1", "B2", "B3" }.ToList();
            tabletPicker.ItemsSource = tabletlist;
            MatchProgressList.Progress = (float)((float)1 / (float)listofviewmatches.Count);
            SetMenuItems();
        }
        private async void SetMenuItems()
        {
            settingsButton.Opacity = 0;
            showUsb.TranslationX = 0;
            showSettings.TranslationX = 0;
            showUsb.TranslationX = (showMenu.X - showUsb.X);
            showUsb.IsVisible = true;
            showUsb.TranslateTo(showUsb.TranslationX - (showMenu.X - showUsb.X), showUsb.TranslationY, 10, Easing.CubicInOut);
            showAbout.TranslationX = (showMenu.X - showAbout.X);
            showAbout.IsVisible = true;
            showAbout.TranslateTo(showAbout.TranslationX - (showMenu.X - showAbout.X), showAbout.TranslationY, 10, Easing.CubicInOut);
            showSettings.TranslationX = (showMenu.X - showSettings.X);
            showSettings.IsVisible = true;
            await showSettings.TranslateTo(showSettings.TranslationX - (showMenu.X - showSettings.X), showSettings.TranslationY, 10, Easing.CubicInOut);
            await Task.Delay(10);
            showUsb.TranslateTo((showMenu.X - showUsb.X), showUsb.TranslationY, 10);
            showAbout.TranslateTo((showMenu.X - showAbout.X), showAbout.TranslationY, 10);
            await showSettings.TranslateTo((showMenu.X - showSettings.X), showSettings.TranslationY, 10);
            showUsb.IsVisible = false;
            showSettings.IsVisible = false;
            settingsButton.Opacity = 1;
        }

        private void FTCShow_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new FTCMain());
        }

        private void FRCShow_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new FRCMain(new TeamMatch() { PowerCellInner = new int[21], PowerCellOuter = new int[21], PowerCellLower = new int[21], PowerCellMissed = new int[21], MatchNumber = 1, TeamNumber = 862 }));


        }
        private async void CreateNewScoutNames(object sender, EventArgs e)
        {
            DependencyService.Get<DataStore>().SaveConfigurationFile("scoutNames", new string[3] { "John Doe", "Imaex Ample", "Guest Scouter" });
            Console.WriteLine(DependencyService.Get<DataStore>().LoadConfigFile());
            await DismissNotification();
            await NewNotification("Scout Names Reset!");
        }
        private async void SendDummyData(object sender, EventArgs e)
        {
            DependencyService.Get<DataStore>().SaveDummyData("JacksonEvent2020.txt");
            await DismissNotification();
            await NewNotification("Data Reset!");
        }
        private void ReloadScreen(object sender, EventArgs e)
        {
            Navigation.PushAsync(new MainPage());
        }
        private void commscheck_Clicked(object sender, EventArgs e)
        {

        }
        private async void CheckBluetooth(object sender, EventArgs e)
        {
            //BindingContext = new BluetoothDeviceViewModel();

            Devices.Clear();

            await adapter.StartScanningForDevicesAsync();

        }

        private async void listofdevices_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            IDevice selectedDevice = e.Item as IDevice;

            if (deviceIWant != null)
            {
                await adapter.DisconnectDeviceAsync(deviceIWant);
                await adapter.ConnectToDeviceAsync(selectedDevice);
                deviceIWant = selectedDevice;
            }
            else
            {
                await adapter.ConnectToDeviceAsync(selectedDevice);
                deviceIWant = selectedDevice;
            }
        }

        private async void sendDataToBT_Clicked(object sender, EventArgs e)
        {
            /*var servicetosend = await deviceIWant.GetServiceAsync(Guid.Parse("50dae772-d8aa-4378-9602-792b3e4c198d"));
            var characteristictosend = await servicetosend.GetCharacteristicAsync(Guid.Parse("50dae772-d8aa-4378-9602-792b3e4c198e"));
            var stringtoconvert = "Test!";
            var bytestotransmit = Encoding.ASCII.GetBytes(stringtoconvert);
            await characteristictosend.WriteAsync(bytestotransmit);
            Console.WriteLine(bytestotransmit);*/
        }

        private void dcFromBT_Clicked(object sender, EventArgs e)
        {
            adapter.DisconnectDeviceAsync(deviceIWant);
        }

        private async void listOfMatches_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            Console.WriteLine(listofmatches[e.ItemIndex].TeamNumber.ToString() + "'s match at match #" + listofmatches[e.ItemIndex].MatchNumber.ToString());
            bool answer = true;
            if (listofmatches[e.ItemIndex].ClientSubmitted)
            {
                answer = await DisplayAlert("Match Completed", "This match has already been completed by someone using this tablet, would you still like to continue?", "Continue", "Cancel");
            }
            if (answer)
            {
                Navigation.PushAsync(new FRCMain(listofmatches[e.ItemIndex]));
            }
            else
            {
                var listobject = sender as ListView;
                listobject.SelectedItem = null;
            }

        }

        private void MenuItem_Clicked(object sender, EventArgs e)
        {
            moreinfoMenu.IsVisible = true;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            moreinfoMenu.IsVisible = false;
        }

        private void tabletPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var list = sender as Picker;
            DependencyService.Get<DataStore>().SaveConfigurationFile("tabletId", tabletlist[list.SelectedIndex]);
        }
        private async void GoToFRCPage(object sender, EventArgs e)
        {
            var currentindex = listofviewmatches.FindIndex(a => a == carouseluwu.CurrentItem);
            Console.WriteLine(listofmatches[currentindex].TeamNumber.ToString() + "'s match at match #" + listofmatches[currentindex].MatchNumber.ToString());
            bool answer = true;
            if (listofmatches[currentindex].ClientSubmitted)
            {
                answer = await DisplayAlert("Match Completed", "This match has already been completed by someone using this tablet, would you still like to continue?", "Continue", "Cancel");
            }
            else if (currentindex != NextMatchIndex)
            {
                if(NextMatchIndex != -1)
                {
                    answer = await DisplayAlert("Match Not Next", "This match is not up next! Would you still like to continue?", "Continue", "Cancel");
                }
                
            }
            if (answer)
            {
                Navigation.PushAsync(new FRCMain(listofmatches[currentindex]));
            }
            else
            {
                //var listobject = sender as ListView;
                //listobject.SelectedItem = null;
            }
        }
        

        private void carouseluwu_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var currentindex = listofviewmatches.FindIndex(a => a == carouseluwu.CurrentItem);
            if(((TeamMatchViewItem)carouseluwu.CurrentItem).ActualMatch == true)
            {
                MatchProgressList.ProgressTo((double)((float)(currentindex + 1) / (float)(listofviewmatches.Count - 1)), 250, Easing.CubicInOut);
            }
            else
            {
                MatchProgressList.ProgressTo(1, 250, Easing.CubicInOut);
            }
            
        }

        private async void getDataFromServer_Clicked(object sender, EventArgs e)
        {

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            MessagingCenter.Subscribe<SubmitVIABluetooth, int>(this, "receivedata", async (messagesender, value) => {
                switch (value)
                {
                    case 1:
                        getDataFromServer.Text = "Process in Progress";
                        break;
                    case 2:
                        getDataFromServer.Text = "Process in Progress";
                        break;
                    case 3:
                        ReloadMatches();
                        cancellationTokenSource.Cancel();
                        MessagingCenter.Unsubscribe<SubmitVIABluetooth, int>(this, "receivedata");
                        await DismissNotification();
                        await NewNotification("GET Succeeded!");
                        break;
                    case -1:
                        getDataFromServer.Text = "Process Failed";
                        break;
                }
            });
            await submitVIABluetoothInstance.GetDefaultData(token);
            var i = 0;
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                i++;
                if (i < 8)
                {
                    return true;
                }
                else
                {
                    cancellationTokenSource.Cancel();
                    return false;
                }
            });
        }

        private async void resetBTLock_Clicked(object sender, EventArgs e)
        {
            //DependencyService.Get<DataStore>().SaveConfigurationFile("bluetoothStage", 0);
            //resetBTLock.Text = "Reset!!";
            MessagingCenter.Subscribe<object, int>(this, "USBResponse", async (mssender,value) =>
            {
                switch (value)
                {
                    case 1:
                        await DismissNotification();
                        await NewNotification("USB: Handshake Started");
                        break;
                    case 2:
                        await DismissNotification();
                        await NewNotification("USB: Response Gotten");
                        break;
                    case 3:
                        await DismissNotification();
                        await NewNotification("USB: Completed");
                        break;
                    case -1:
                        await DismissNotification();
                        await NewNotification("USB: Failed");
                        DisplayAlert("Not Available", "The USB socket is currently in use from a previous request. We closed it for you. Please try again!", "Ok!");
                        MessagingCenter.Unsubscribe<object, int>(this, "USBResponse");
                        break;
                }
            });
            if(Battery.PowerSource == BatteryPowerSource.Usb || Battery.PowerSource == BatteryPowerSource.AC)
            {
                var jsondata = DependencyService.Get<DataStore>().LoadData("JacksonEvent2020.txt");
                DependencyService.Get<USBCommunication>().SendData(jsondata);
            }
            else
            {
                await DismissNotification();
                await NewNotification("USB Not Connected!");
            }
            

        }
        private async Task NewNotification(string NotificationText)
        {
            NotificationActive = true;
            NotificationLabel.Text = NotificationText;
            notificationContainer.TranslationY = 150;
            notificationMedium.IsVisible = true;
            await notificationContainer.TranslateTo(notificationContainer.TranslationX, notificationContainer.TranslationY - 150, 500, Easing.CubicInOut);
            await NotificationIcon.RotateTo(25, 100, Easing.CubicIn);
            await NotificationIcon.RotateTo(0, 100, Easing.CubicIn);
            await NotificationIcon.RotateTo(25, 100, Easing.CubicIn);
            await NotificationIcon.RotateTo(0, 100, Easing.CubicIn);
            await NotificationIcon.RotateTo(25, 100, Easing.CubicIn);
            await NotificationIcon.RotateTo(0, 100, Easing.CubicIn);
        }
        private async Task DismissNotification()
        {
            if (NotificationActive)
            {
                NotificationActive = false;
                await notificationContainer.TranslateTo(notificationContainer.TranslationX, notificationContainer.TranslationY + 150, 500, Easing.CubicInOut);

                notificationMedium.IsVisible = false;
                notificationContainer.TranslationY = 0;
            }
            

        }
        private async void sendNotification_Clicked(object sender, EventArgs e)
        {
            await DismissNotification();
            await NewNotification("Test Notification");
        }
        private async void dismissNotification_Clicked(object sender, EventArgs e)
        {
            await DismissNotification();

        }
        
        private async void AddCodeNumber(object sender, EventArgs e)
        {
            var converter = new ColorTypeConverter();
            var codebutton = (Button)sender as Button;
            switch (codebutton.Text)
            {
                case "0":
                    currentCodeString = currentCodeString + "0";
                    break;
                case "1":
                    currentCodeString = currentCodeString + "1";
                    break;
                case "2":
                    currentCodeString = currentCodeString + "2";
                    break;
                case "3":
                    currentCodeString = currentCodeString + "3";
                    break;
                case "4":
                    currentCodeString = currentCodeString + "4";
                    break;
                case "5":
                    currentCodeString = currentCodeString + "5";
                    break;
                case "6":
                    currentCodeString = currentCodeString + "6";
                    break;
                case "7":
                    currentCodeString = currentCodeString + "7";
                    break;
                case "8":
                    currentCodeString = currentCodeString + "8";
                    break;
                case "9":
                    currentCodeString = currentCodeString + "9";
                    break;
                default:
                    currentCodeString = "";
                    codeButtonCancel.FadeTo(0, 150, Easing.SinIn);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    break;
            }
            if(currentCodeString.Length == 1)
            {
                codeButtonCancel.FadeTo(1, 150, Easing.SinIn);
                codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            }
            else if (currentCodeString.Length == 2)
            {
                codeButtonCancel.FadeTo(1, 150, Easing.SinIn);
                codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            }
            else if (currentCodeString.Length == 3)
            {
                codeButtonCancel.FadeTo(1, 150, Easing.SinIn);
                codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            }
            else if (currentCodeString.Length == 4)
            {
                codeButtonCancel.FadeTo(0, 150, Easing.SinIn);
                codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("#2a7afa");
                if (currentCodeString == "0000")
                {
                    codeButton0.IsEnabled = false;
                    codeButton1.IsEnabled = false;
                    codeButton2.IsEnabled = false;
                    codeButton3.IsEnabled = false;
                    codeButton4.IsEnabled = false;
                    codeButton5.IsEnabled = false;
                    codeButton6.IsEnabled = false;
                    codeButton7.IsEnabled = false;
                    codeButton8.IsEnabled = false;
                    codeButton9.IsEnabled = false;
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    await Task.Delay(100);
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Green");
                    
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeButton0.IsEnabled = true;
                    codeButton1.IsEnabled = true;
                    codeButton2.IsEnabled = true;
                    codeButton3.IsEnabled = true;
                    codeButton4.IsEnabled = true;
                    codeButton5.IsEnabled = true;
                    codeButton6.IsEnabled = true;
                    codeButton7.IsEnabled = true;
                    codeButton8.IsEnabled = true;
                    codeButton9.IsEnabled = true;
                    switch (currentCodeReason)
                    {
                        case CodeReason.DeleteMatch:
                            var currentindex = listofviewmatches.FindIndex(a => a == carouseluwu.CurrentItem);
                            listofmatches.RemoveAt(currentindex);
                            DependencyService.Get<DataStore>().SaveDefaultData("JacksonEvent2020.txt", listofmatches);
                            await Task.Delay(2000);
                            ReloadMatches();
                            break;
                        case CodeReason.EditMatch:
                            var currentindexedit = listofviewmatches.FindIndex(a => a == carouseluwu.CurrentItem);
                            var currentmatch = listofmatches.ToArray()[currentindexedit];
                            coreEditMatchNumberLabel.Text = "#" + currentmatch.MatchNumber.ToString();
                            coreEditMatchNumberStepper.Value = currentmatch.MatchNumber;
                            if (currentmatch.TeamName != null)
                            {
                                coreEditTeamName.Text = currentmatch.TeamName;
                            }
                            coreEditTeamNumber.Text = currentmatch.TeamNumber.ToString();
                            editCoreInfoPanel.TranslationX = 600;
                            await Task.Delay(50);
                            await strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX - 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
                            strategyCodeInterface.IsVisible = false;
                            strategyCodePanel.TranslationX = 0;
                            editCoreInfoInterface.IsVisible = true;
                            editCoreInfoPanel.TranslateTo(editCoreInfoPanel.TranslationX - 600, editCoreInfoPanel.TranslationY, 500, Easing.CubicInOut);
                            await Task.Delay(2000);
                            ReloadMatches();
                            break;
                        case CodeReason.CreateMatch:
                            createNewMatchNumberStepper.Value = 1;
                            createNewMatchNumberLabel.Text = "#1";
                            createMatchInfoPanel.TranslationX = 600;
                            await Task.Delay(50);
                            await strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX - 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
                            strategyCodeInterface.IsVisible = false;
                            strategyCodePanel.TranslationX = 0;
                            createNewMatchInterface.IsVisible = true;
                            createMatchInfoPanel.TranslateTo(createMatchInfoPanel.TranslationX - 600, createMatchInfoPanel.TranslationY, 500, Easing.CubicInOut);

                            break;
                    }
                    
                }
                else
                {
                    currentCodeString = "";
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Red");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Red");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Red");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.Red");
                    codeButton0.IsEnabled = false;
                    codeButton1.IsEnabled = false;
                    codeButton2.IsEnabled = false;
                    codeButton3.IsEnabled = false;
                    codeButton4.IsEnabled = false;
                    codeButton5.IsEnabled = false;
                    codeButton6.IsEnabled = false;
                    codeButton7.IsEnabled = false;
                    codeButton8.IsEnabled = false;
                    codeButton9.IsEnabled = false;
                    try
                    {
                        Vibration.Vibrate();
                    }
                    catch(Exception ex)
                    {

                    }
                    
                    await codeProgressContainer.TranslateTo(codeProgressContainer.TranslationX - 10, codeProgressContainer.TranslationY, 75, Easing.SinIn);
                    await codeProgressContainer.TranslateTo(codeProgressContainer.TranslationX + 20, codeProgressContainer.TranslationY, 75, Easing.SinIn);
                    await codeProgressContainer.TranslateTo(codeProgressContainer.TranslationX - 20, codeProgressContainer.TranslationY, 75, Easing.SinIn);
                    await codeProgressContainer.TranslateTo(codeProgressContainer.TranslationX + 20, codeProgressContainer.TranslationY, 75, Easing.SinIn);
                    await codeProgressContainer.TranslateTo(codeProgressContainer.TranslationX - 10, codeProgressContainer.TranslationY, 75, Easing.SinIn);
                    codeButton0.IsEnabled = true;
                    codeButton1.IsEnabled = true;
                    codeButton2.IsEnabled = true;
                    codeButton3.IsEnabled = true;
                    codeButton4.IsEnabled = true;
                    codeButton5.IsEnabled = true;
                    codeButton6.IsEnabled = true;
                    codeButton7.IsEnabled = true;
                    codeButton8.IsEnabled = true;
                    codeButton9.IsEnabled = true;
                    codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                    codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
                }
            }
        }

        private async void SeeSettingsPage(object sender, EventArgs e)
        {
            if (!MenuAnimationActive)
            {
                mainInterface.TranslateTo(mainInterface.TranslationX - 600, mainInterface.TranslationY, 500, Easing.SinIn);
                if (MenuOpen)
                {
                    showUsb.TranslateTo((showMenu.X - showUsb.X), showUsb.TranslationY, 350, Easing.CubicIn);
                    showAbout.TranslateTo((showMenu.X - showAbout.X), showAbout.TranslationY, 350, Easing.CubicIn);
                    showSettings.TranslateTo((showMenu.X - showSettings.X), showSettings.TranslationY, 350, Easing.CubicIn);
                    showMenu.Focus();
                    pureblueOverButton.IsVisible = true;
                    pureblueOverButton.FadeTo(1, 200);
                    await Task.Delay(200);
                    await showMenu.RotateTo(0, easing: Easing.CubicInOut);
                    showUsb.IsVisible = false;
                    showAbout.IsVisible = false;
                    showSettings.IsVisible = false;

                    MenuOpen = false;
                }
                MenuAnimationActive = true;
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 10, 75, Easing.CubicOut);
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 110, 250, Easing.CubicOut);
                MenuAnimationActive = false;
                settingsButton.IsVisible = false;
                settingsButton.TranslationY = 0;
                await Task.Delay(100);
                settingsInterface.TranslationY = 1200;
                await Task.Delay(100);
                settingsInterface.IsVisible = true;
                settingsInterface.TranslateTo(settingsInterface.TranslationX, settingsInterface.TranslationY - 1200, 500, Easing.SinOut);
            }
            
        }
        private async void ToggleMenuItems(object sender, EventArgs e)
        {
            if (!MenuAnimationActive)
            {
                if (!MenuOpen)
                {
                    MenuAnimationActive = true;
                    showMenu.RotateTo(90, easing: Easing.CubicInOut);
                    await Task.Delay(150);
                    showUsb.TranslationX = 0;
                    showSettings.TranslationX = 0;
                    showUsb.TranslationX = (showMenu.X - showUsb.X);
                    showUsb.IsVisible = true;
                    showUsb.TranslateTo(showUsb.TranslationX - (showMenu.X - showUsb.X), showUsb.TranslationY, 600, Easing.CubicOut);
                    showSettings.TranslationX = (showMenu.X - showSettings.X);
                    showSettings.IsVisible = true;
                    showSettings.TranslateTo(showSettings.TranslationX - (showMenu.X - showSettings.X), showSettings.TranslationY, 600, Easing.CubicOut);
                    showAbout.TranslationX = (showMenu.X - showAbout.X);
                    showAbout.IsVisible = true;
                    await showAbout.TranslateTo(showAbout.TranslationX - (showMenu.X - showAbout.X), showAbout.TranslationY, 600, Easing.CubicOut);
                    MenuOpen = true;
                    MenuAnimationActive = false;
                }
                else
                {
                    MenuAnimationActive = true;
                    showUsb.TranslateTo((showMenu.X - showUsb.X), showUsb.TranslationY, 350, Easing.CubicIn);
                    showSettings.TranslateTo((showMenu.X - showSettings.X), showSettings.TranslationY, 350, Easing.CubicIn);
                    showAbout.TranslateTo((showMenu.X - showAbout.X), showAbout.TranslationY, 350, Easing.CubicIn);
                    await Task.Delay(200);
                    await showMenu.RotateTo(0, easing: Easing.CubicInOut);
                    showUsb.IsVisible = false;
                    showSettings.IsVisible = false;
                    showAbout.IsVisible = false;

                    MenuOpen = false;
                    MenuAnimationActive = false;
                }
            }
            
            

            
        }
        private async void CancelSettingsPage(object sender, EventArgs e)
        {
            settingsInterface.TranslateTo(settingsInterface.TranslationX, settingsInterface.TranslationY + 1200, 500, Easing.SinIn);
            await Task.Delay(350);
            settingsInterface.IsVisible = false;
            settingsInterface.TranslationY = 0;
            pureblueOverButton.IsVisible = false;
            pureblueOverButton.Opacity = 0;
            mainInterface.TranslationX = -600;
            mainInterface.TranslateTo(mainInterface.TranslationX + 600, mainInterface.TranslationY, 500, Easing.SinOut);
            settingsButton.TranslationY = 100;
            settingsButton.IsVisible = true;
            MenuAnimationActive = true;
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 110, 400, Easing.CubicIn);
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 10, 100, Easing.CubicIn);
            MenuAnimationActive = false;


        }
        private async void SeeUsbPage(object sender, EventArgs e)
        {
            if (!MenuAnimationActive)
            {
                mainInterface.TranslateTo(mainInterface.TranslationX - 600, mainInterface.TranslationY, 500, Easing.SinIn);
                if (MenuOpen)
                {
                    showUsb.TranslateTo((showMenu.X - showUsb.X), showUsb.TranslationY, 350, Easing.CubicIn);
                    showAbout.TranslateTo((showMenu.X - showAbout.X), showAbout.TranslationY, 350, Easing.CubicIn);
                    showSettings.TranslateTo((showMenu.X - showSettings.X), showSettings.TranslationY, 350, Easing.CubicIn);
                    showMenu.Focus();
                    pureblueOverButton.IsVisible = true;
                    pureblueOverButton.FadeTo(1, 200);
                    await Task.Delay(200);
                    await showMenu.RotateTo(0, easing: Easing.CubicInOut);
                    showUsb.IsVisible = false;
                    showAbout.IsVisible = false;
                    showSettings.IsVisible = false;

                    MenuOpen = false;
                }
                MenuAnimationActive = true;
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 10, 75, Easing.CubicOut);
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 110, 250, Easing.CubicOut);
                MenuAnimationActive = false;
                settingsButton.IsVisible = false;
                settingsButton.TranslationY = 0;
                await Task.Delay(100);
                usbInterface.TranslationY = 1200;
                await Task.Delay(100);
                usbInterface.IsVisible = true;
                usbInterface.TranslateTo(usbInterface.TranslationX, usbInterface.TranslationY - 1200, 500, Easing.SinOut);
            }
            
        }
        private async void CancelUsbPage(object sender, EventArgs e)
        {
            usbInterface.TranslateTo(usbInterface.TranslationX, usbInterface.TranslationY + 1200, 500, Easing.SinIn);
            await Task.Delay(350);
            usbInterface.IsVisible = false;
            usbInterface.TranslationY = 0;
            pureblueOverButton.IsVisible = false;
            pureblueOverButton.Opacity = 0;
            mainInterface.TranslationX = -600;
            mainInterface.TranslateTo(mainInterface.TranslationX + 600, mainInterface.TranslationY, 500, Easing.SinOut);
            settingsButton.TranslationY = 100;
            settingsButton.IsVisible = true;
            MenuAnimationActive = true;
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 110, 400, Easing.CubicIn);
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 10, 100, Easing.CubicIn);
            MenuAnimationActive = false;

        }
        private async void SeeAboutPage(object sender, EventArgs e)
        {
            if (!MenuAnimationActive)
            {
                mainInterface.TranslateTo(mainInterface.TranslationX - 600, mainInterface.TranslationY, 500, Easing.SinIn);
                if (MenuOpen)
                {
                    showUsb.TranslateTo((showMenu.X - showUsb.X), showUsb.TranslationY, 350, Easing.CubicIn);
                    showAbout.TranslateTo((showMenu.X - showAbout.X), showAbout.TranslationY, 350, Easing.CubicIn);
                    showSettings.TranslateTo((showMenu.X - showSettings.X), showSettings.TranslationY, 350, Easing.CubicIn);
                    showMenu.Focus();
                    pureblueOverButton.IsVisible = true;
                    pureblueOverButton.FadeTo(1, 200);
                    await Task.Delay(200);
                    await showMenu.RotateTo(0, easing: Easing.CubicInOut);
                    showUsb.IsVisible = false;
                    showAbout.IsVisible = false;
                    showSettings.IsVisible = false;

                    MenuOpen = false;
                }
                MenuAnimationActive = true;
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 10, 75, Easing.CubicOut);
                await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 110, 250, Easing.CubicOut);
                MenuAnimationActive = false;
                settingsButton.IsVisible = false;
                settingsButton.TranslationY = 0;
                await Task.Delay(100);
                aboutInterface.TranslationY = 1200;
                await Task.Delay(100);
                aboutInterface.IsVisible = true;
                aboutInterface.TranslateTo(aboutInterface.TranslationX, aboutInterface.TranslationY - 1200, 500, Easing.SinOut);
            }

        }
        private async void CancelAboutPage(object sender, EventArgs e)
        {
            aboutInterface.TranslateTo(aboutInterface.TranslationX, aboutInterface.TranslationY + 1200, 500, Easing.SinIn);
            await Task.Delay(350);
            aboutInterface.IsVisible = false;
            aboutInterface.TranslationY = 0;
            pureblueOverButton.IsVisible = false;
            pureblueOverButton.Opacity = 0;
            mainInterface.TranslationX = -600;
            mainInterface.TranslateTo(mainInterface.TranslationX + 600, mainInterface.TranslationY, 500, Easing.SinOut);
            settingsButton.TranslationY = 100;
            settingsButton.IsVisible = true;
            MenuAnimationActive = true;
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY - 110, 400, Easing.CubicIn);
            await settingsButton.TranslateTo(settingsButton.TranslationX, settingsButton.TranslationY + 10, 100, Easing.CubicIn);
            MenuAnimationActive = false;

        }
        private async void CancelCodePanel(object sender, EventArgs e)
        {
            await strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX + 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
            strategyCodeInterface.IsVisible = false;
            strategyCodePanel.TranslationX = 0;
            
            
            currentCodeString = "";
        }
        private async void CancelCoreInfoEdit(object sender, EventArgs e)
        {
            DependencyService.Get<IKeyboardHelper>().HideKeyboard();
            await editCoreInfoPanel.TranslateTo(editCoreInfoPanel.TranslationX + 600, editCoreInfoPanel.TranslationY, 500, Easing.CubicInOut);
            editCoreInfoInterface.IsVisible = false;
            editCoreInfoPanel.TranslationX = 0;

        }
        private async void SaveCoreInfoEdit(object sender, EventArgs e)
        {
            var currentindex = listofviewmatches.FindIndex(a => a == carouseluwu.CurrentItem);
            var currentmatch = listofmatches.ToArray()[currentindex];
            currentmatch.TeamName = coreEditTeamName.Text;
            currentmatch.TeamNumber = int.Parse(coreEditTeamNumber.Text);
            currentmatch.MatchNumber = (int)coreEditMatchNumberStepper.Value;
            listofmatches.ToArray()[currentindex] = currentmatch;
            DependencyService.Get<DataStore>().SaveDefaultData("JacksonEvent2020.txt", listofmatches);
            DependencyService.Get<IKeyboardHelper>().HideKeyboard();
            await editCoreInfoPanel.TranslateTo(editCoreInfoPanel.TranslationX - 600, editCoreInfoPanel.TranslationY, 500, Easing.CubicInOut);
            editCoreInfoInterface.IsVisible = false;
            editCoreInfoPanel.TranslationX = 0;
            await Task.Delay(2000);
            ReloadMatches();
        }
        private async void CancelCreateMatch(object sender, EventArgs e)
        {
            DependencyService.Get<IKeyboardHelper>().HideKeyboard();
            await createMatchInfoPanel.TranslateTo(createMatchInfoPanel.TranslationX + 600, createMatchInfoPanel.TranslationY, 500, Easing.CubicInOut);
            createNewMatchInterface.IsVisible = false;
            createMatchInfoPanel.TranslationX = 0;

        }
        private async void SaveCreateMatch(object sender, EventArgs e)
        {
            

            var currentmatch = new TeamMatch() { PowerCellInner = new int[20], PowerCellOuter = new int[20], PowerCellLower = new int[20], PowerCellMissed = new int[20], TabletId = JsonConvert.DeserializeObject<LSConfiguration>(DependencyService.Get<DataStore>().LoadConfigFile()).TabletIdentifier };
            currentmatch.TeamName = createNewTeamName.Text;
            currentmatch.TeamNumber = int.Parse(createNewTeamNumber.Text);
            currentmatch.MatchNumber = (int)createNewMatchNumberStepper.Value;
            listofmatches.Add(currentmatch);
            DependencyService.Get<DataStore>().SaveDefaultData("JacksonEvent2020.txt", listofmatches);
            DependencyService.Get<IKeyboardHelper>().HideKeyboard();
            await createMatchInfoPanel.TranslateTo(createMatchInfoPanel.TranslationX - 600, createMatchInfoPanel.TranslationY, 500, Easing.CubicInOut);
            createNewMatchInterface.IsVisible = false;
            createMatchInfoPanel.TranslationX = 0;
            await Task.Delay(2000);
            ReloadMatches();
        }
        private void coreEditMatchNumberStepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            coreEditMatchNumberLabel.Text = "#" + e.NewValue.ToString();
        }
        private void createMatchNumberStepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            createNewMatchNumberLabel.Text = "#" + e.NewValue.ToString();
        }
        private async void CreateNewMatch(object sender, EventArgs e)
        {
            var converter = new ColorTypeConverter();
            codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            strategyCodePanel.TranslationX = 600;
            await Task.Delay(50);
            strategyCodeInterface.IsVisible = true;
            strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX - 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
            currentCodeString = "";
            currentCodeReason = CodeReason.CreateMatch;
        }
        private async void DeleteEntry(object sender, EventArgs e)
        {
            var converter = new ColorTypeConverter();
            codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            strategyCodePanel.TranslationX = 600;
            await Task.Delay(50);
            strategyCodeInterface.IsVisible = true;
            strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX - 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
            currentCodeString = "";
            currentCodeReason = CodeReason.DeleteMatch;
        }
        private async void EditEntry(object sender, EventArgs e)
        {
            var converter = new ColorTypeConverter();
            codeProgress1.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress2.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress3.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            codeProgress4.BackgroundColor = (Color)converter.ConvertFromInvariantString("Color.LightGray");
            strategyCodePanel.TranslationX = 600;
            await Task.Delay(50);
            strategyCodeInterface.IsVisible = true;
            strategyCodePanel.TranslateTo(strategyCodePanel.TranslationX - 600, strategyCodePanel.TranslationY, 500, Easing.CubicInOut);
            currentCodeString = "";
            currentCodeReason = CodeReason.EditMatch;
        }
        private void ReloadMatches()
        {
            try
            {
                listofviewmatches = new List<TeamMatchViewItem>();
                var allmatchesraw = DependencyService.Get<DataStore>().LoadData("JacksonEvent2020.txt");
                listofmatches = JsonConvert.DeserializeObject<List<TeamMatch>>(allmatchesraw);
                var upnext = false;
                TeamMatchViewItem selectedItem = null;
                var upnextselected = false;
                int i2 = 0;
                foreach (var match in listofmatches)
                {

                    var newmatchviewitem = new TeamMatchViewItem();
                    upnext = false;
                    if (!match.ClientSubmitted)
                    {
                        if (!upnextselected)
                        {
                            upnext = true;
                            upnextselected = true;
                        }
                    }
                    newmatchviewitem.Completed = match.ClientSubmitted;
                    if (match.TabletId != null)
                    {
                        newmatchviewitem.IsRed = match.TabletId.StartsWith("R");
                        newmatchviewitem.IsBlue = match.TabletId.StartsWith("B");
                    }

                    newmatchviewitem.IsUpNext = upnext;
                    newmatchviewitem.TeamName = match.TeamName;
                    if (match.TeamName == null)
                    {
                        newmatchviewitem.TeamName = "FRC Team " + match.TeamNumber.ToString();
                    }
                    newmatchviewitem.TeamNumber = match.TeamNumber;
                    newmatchviewitem.MatchNumber = match.MatchNumber;
                    newmatchviewitem.TabletName = match.TabletId;
                    newmatchviewitem.ActualMatch = true;
                    newmatchviewitem.NewPlaceholder = false;
                    newmatchviewitem.teamIcon = ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String("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")));
                    if (upnext)
                    {
                        selectedItem = newmatchviewitem;
                        NextMatchIndex = i2;
                    }
                    listofviewmatches.Add(newmatchviewitem);
                    i2++;
                }
                if (selectedItem != null)
                {
                    carouseluwu.CurrentItem = selectedItem;
                }
                else
                {
                    carouseluwu.CurrentItem = listofviewmatches.First();
                }
            }
            catch (Exception ex)
            {

            }
            var placeholdermatch = new TeamMatchViewItem();
            placeholdermatch.ActualMatch = false;
            placeholdermatch.NewPlaceholder = true;
            listofviewmatches.Add(placeholdermatch);
            listOfMatches.ItemsSource = listofviewmatches;
            carouseluwu.ItemsSource = listofviewmatches;
            MatchProgressList.Progress = (float)((float)1 / (float)(listofviewmatches.Count - 1));
        }

        private void OverlayEntryUnfocused(object sender, FocusEventArgs e)
        {
            DependencyService.Get<IKeyboardHelper>().HideKeyboard();
        }
    }
}
