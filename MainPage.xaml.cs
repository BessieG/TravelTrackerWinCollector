using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Graphics.Display;




namespace Travel_Tracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Accelerometer accelerometer;
        AccelerometerReading accelerometerReading;

        //variables

        string sas = "https://ucltt.blob.core.windows.net/collector/?sv=2015-04-05&sr=c&sig=E3KK%2BaWJVw8vemkDM8%2BsV9n7K5SLdgstXX1RuSTvBsc%3D&st=2015-12-14T11%3A30%3A06Z&se=2016-06-14T10%3A30%3A06Z&sp=rwdl";
        StringBuilder builder = new StringBuilder();
        DateTime starttime;
        DateTime stoptime;
        double[] max = new double[4];
        double[] min = new double[4];
        double[] avg = new double[4];
        double[] sum = { 0.0, 0.0, 0.0, 0.0 };
        double[] reading = new double[4];
        //max[0]is max for resultant, [1] for x,[2] for y, [3]for z; same as min,avg,sum，reading
        int count = -1;
        string[] items = { "Car", "Bus", "Train", "Metro", "Walk", "Bike" };
        uint reportInterval = 60;
        string ModeStr;


        public MainPage()
        {
            InitializeComponent();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {

            if (comboBox.SelectedItem == null)
            {
                MessageDialog messageDialog = new MessageDialog("Please choose a mode.", "Error!");
                await messageDialog.ShowAsync();
            }
            else
            {
                starttime = DateTime.Now;
                builder.Append("{\"Type\":\"Trainning Data\",")
                       .Append(" \"StartTime\": \"" + starttime.ToString() + "\",")
                       .Append("\"Record\":[");

                accelerometer = Accelerometer.GetDefault();
                if (accelerometer == null)
                {
                    await new MessageDialog("Error when getting accelerometer").ShowAsync();
                    return;
                }
                comboBox.IsEnabled = false;
                StopButton.IsEnabled = true;
                StartButton.IsEnabled = false;
                //The current report interval(设置读数时间间隔)
                //Debug.WriteLine(accelerometer.MinimumReportInterval);
                accelerometer.ReportInterval = reportInterval;//60毫秒
                accelerometer.ReadingChanged += accelerometer_ReadingChanged;
                //accelerometer.Shaken += accelerometer_Shaken;
                accelerometerReading = accelerometer.GetCurrentReading();
                ShowData();
            }
        }

        //Accelerometer change(加速度变化事件处理程序)
        async void accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                accelerometerReading = args.Reading;
                if (StartButton.IsEnabled == false) ShowData();
            });

        }

        void ShowData()
        {

            int i;

            reading[1] = accelerometerReading.AccelerationX * 10;
            reading[2] = accelerometerReading.AccelerationY * 10;
            reading[3] = accelerometerReading.AccelerationZ * 10;

            xTextBlock.Text = "X: " + accelerometerReading.AccelerationX.ToString("0.0000");
            yTextBlock.Text = "Y: " + accelerometerReading.AccelerationY.ToString("0.0000");
            zTextBlock.Text = "Z: " + accelerometerReading.AccelerationZ.ToString("0.0000");

            reading[0] = reading[1] * reading[1] + reading[2] * reading[2] + reading[3] * reading[3];
            if (count == -1)
            {
                for (i = 0; i < 4; i++)
                {
                    max[i] = reading[i];
                    min[i] = reading[i];
                }
                count++;
            }
            if (count == 90)
            {
                //calculate resultant acceleration
                max[0] = Math.Sqrt(max[0]);
                min[0] = Math.Sqrt(min[0]);
                avg[0] = sum[0] / 90;
                avg[0] = Math.Sqrt(avg[0]);
                for (i = 1; i < 4; i++)
                {
                    avg[i] = sum[i] / 90;
                }
                //write json
                builder.Append("{\"max_resultant\":" + max[0].ToString() + ",")
                       .Append("\"min_resultant\":" + min[0].ToString() + ",")
                       .Append("\"avg_resultant\":" + avg[0].ToString() + ",")
                       .Append("\"max_x\":" + max[1].ToString() + ",")
                       .Append("\"min_x\":" + min[1].ToString() + ",")
                       .Append("\"avg_x\":" + avg[1].ToString() + ",")
                       .Append("\"max_y\":" + max[2].ToString() + ",")
                       .Append("\"min_y\":" + min[2].ToString() + ",")
                       .Append("\"avg_y\":" + avg[2].ToString() + ",")
                       .Append("\"max_z\":" + max[3].ToString() + ",")
                       .Append("\"min_z\":" + min[3].ToString() + ",")
                       .Append("\"avg_z\":" + avg[3].ToString() + "},");
                //renew max min sum
                for (i = 0; i < 4; i++)
                {
                    max[i] = reading[i];
                    min[i] = reading[i];
                    sum[i] = 0.0;
                }
                count = 0;
            }
            for (i = 0; i < 4; i++)
            {
                if (max[i] < reading[i]) max[i] = reading[i];
                if (min[i] > reading[i]) min[i] = reading[i];
                sum[i] += reading[i];
            }
            count++;


            //draw the gragh
            //xLine.X2 = xLine.X1 + accelerometerReading.AccelerationX * 100;
            //yLine.Y2 = yLine.Y1 - accelerometerReading.AccelerationY * 100;
            //zLine.X2 = zLine.X1 - accelerometerReading.AccelerationZ * 50;
            //zLine.Y2 = zLine.Y1 + accelerometerReading.AccelerationZ * 50;
        }

 //       private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
 //       {

 //       }

        static async Task UseContainerSAS(string sas, string mode, string json)
        {
            //Try performing container operations with the SAS provided.

            //break a reference to the container using the SAS URI.
            CloudBlobContainer container = new CloudBlobContainer(new Uri(sas));
            string date = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try
            {
                //Write operation: write a new blob to the container.
                CloudBlockBlob blob = container.GetBlockBlobReference("Win_" + mode + date + ".json");

                string blobContent = json;
                MemoryStream msWrite = new
                MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    await blob.UploadFromStreamAsync(msWrite.AsInputStream());
                }
                //Console.WriteLine("Write operation succeeded for SAS " + sas);
                //Console.WriteLine();
            }
            catch (Exception e)
            {
                //Console.WriteLine("Write operation failed for SAS " + sas);
                //Console.WriteLine("Additional error information: " + e.Message);
                //Console.WriteLine();
            }
        }



        private void comboBox_DropDownClosed(object sender, object e)
        {
            if (comboBox.SelectedItem != null)
            {
                ModeStr = comboBox.SelectedItem as string;
                //Info.Text = ModeStr;

            }

        }


        public async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            comboBox.IsEnabled = true;
            comboBox.SelectedItem = null;
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            xTextBlock.Text = "X:0.00";
            yTextBlock.Text = "Y:0.00";
            zTextBlock.Text = "Z:0.00";
            accelerometer.ReportInterval = 0;
            ContentDialog dialog = new ContentDialog()
            {
                Title = "DATA TRANSMISSION CONFIRM", //标题
                Content = "Do you want to upload the data?",//内容
                FullSizeDesired = false,  //是否全屏展示
                PrimaryButtonText = "YES",//第一个按钮内容
                SecondaryButtonText = "NO"
            };
            dialog.SecondaryButtonClick += dialog_SecondaryButtonClick;//第二个按钮单击事件
            dialog.PrimaryButtonClick += dialog_PrimaryButtonClick;

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) { } //处理第一个按钮的返回
            else if (result == ContentDialogResult.Secondary) { }//处理第二个按钮的返回

            /*
            builder.Append("\"Mode\":\""+ModeStr+"\"}");
            await UseContainerSAS(sas, ModeStr, builder.ToString());
            builder.Length = 0;
            comboBox.IsEnabled = true;
            comboBox.SelectedItem = null;
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;

            MessageDialog messageDialog = new MessageDialog("Data uploaded!","Well Done");
            await messageDialog.ShowAsync();
            */
        }
        async void dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            stoptime = DateTime.Now;
            builder.Length--;
            builder.Append("],");
            builder.Append("\"EndTime\":\"" + stoptime.ToString() + "\",");
            builder.Append("\"Mode\":\"" + ModeStr + "\"}");
            await UseContainerSAS(sas, ModeStr, builder.ToString());
            builder.Length = 0;

            await new MessageDialog("Data uploaded!").ShowAsync();
        }

        async void dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            builder.Length = 0;
            await new MessageDialog("Transmission canceled").ShowAsync();
        }


        
    }
}
