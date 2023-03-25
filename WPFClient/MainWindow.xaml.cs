using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BigeumTalkClient
{
    public class Settings
    {
        public string IP { get; set; }
        public ushort PORT { get; set; }
    }
    public partial class MainWindow : Window
    {
        internal static MainWindow mainWindow;
        private bool versionFlag;
        private string versionString;
        private string versionDate;
        public Settings settings;
        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
            InitClient();
        }

        private void InitClient()
        {
            SizeToContent = SizeToContent.Manual;
            versionFlag = true;
            versionString = "버전: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            versionDate = "빌드된 날짜: " + Get_BuildDateTime(Assembly.GetExecutingAssembly().GetName().Version);
            versionLabel.Content = versionString;

            string path = @AppDomain.CurrentDomain.BaseDirectory + @"settings.json";
            bool fileExist = File.Exists(path);
            if (fileExist == false)
            {
                File.Create(path).Close();
                settings = new Settings();
                settings.IP = "127.0.0.1";
                settings.PORT = 3000;

                File.WriteAllText(path, JsonConvert.SerializeObject(settings));
            }
            else
            {
                using (StreamReader file = File.OpenText(path))
                {
                    try
                    {
                        string settingJson = file.ReadToEnd();
                        file.Close();
                        settings = JsonConvert.DeserializeObject<Settings>(settingJson);
                    }
                    catch (Exception)
                    {
                        File.Delete(path);
                        File.Create(path).Close();
                        settings = new Settings();
                        settings.IP = "127.0.0.1";
                        settings.PORT = 3000;

                        File.WriteAllText(path, JsonConvert.SerializeObject(settings));
                    }
                }
            }
        }

        // Page 마우스 XButton Block
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2)
            {
                e.Handled = true;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2)
            {
                e.Handled = true;
            }

            base.OnMouseUp(e);
        }

        public System.DateTime Get_BuildDateTime(System.Version? version = null)
        {
            // 주.부.빌드.수정
            // 주 버전    Major Number
            // 부 버전    Minor Number
            // 빌드 번호  Build Number
            // 수정 버전  Revision NUmber


            //매개 변수가 존재할 경우
            if (version == null)
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            //세번째 값(Build Number)은 2000년 1월 1일부터
            //Build된 날짜까지의 총 일(Days) 수 이다.
            int day = version.Build;
            System.DateTime dtBuild = (new System.DateTime(2000, 1, 1)).AddDays(day);

            //네번째 값(Revision NUmber)은 자정으로부터 Build된
            //시간까지의 지나간 초(Second) 값 이다.
            int intSeconds = version.Revision;
            intSeconds = intSeconds * 2;
            dtBuild = dtBuild.AddSeconds(intSeconds);

            //시차 보정
            System.Globalization.DaylightTime daylingTime = System.TimeZone.CurrentTimeZone
                    .GetDaylightChanges(dtBuild.Year);
            if (System.TimeZone.IsDaylightSavingTime(dtBuild, daylingTime))
                dtBuild = dtBuild.Add(daylingTime.Delta);

            return dtBuild;
        }

        // 버전-빌드시간 토글
        private void versionLabel_Click(object sender, RoutedEventArgs e)
        {
            if(versionFlag == false)
            {
                versionFlag = true;
                versionLabel.Content = versionString;
            }
            else
            {
                versionFlag = false;
                versionLabel.Content = versionDate;
            }
        }
    }
}
