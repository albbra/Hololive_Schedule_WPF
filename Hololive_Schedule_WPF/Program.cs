using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Hololive_Schedule_WPF
{
    public class MainForm : Form
    {
        public static void Main()
        {
            Application.Run(new MainForm());
        }

        private List<string[]> listOfStreams = new List<string[]>();
        private int[] dateyposition = new int[4];
        private int currentScrollValue = 0;
        private int a = 0;

        public MainForm()
        {
            this.Text = "Hololive Schedule";
            this.Size = new Size(420, 1038);
           
            LoadDate();
            listOfStreams = LoadData();
            CreatePictureBoxesandLabels();
            CreateVScrollBar(10 + (listOfStreams.Count * 111));
            this.MouseWheel += new MouseEventHandler(MainForm_MouseWheel);
        }

        private void CreateVScrollBar(int max)
        {
            VScrollBar vScrollBar = new VScrollBar();
            vScrollBar.Location = new Point(395, 0);
            vScrollBar.Height = 1000;
            vScrollBar.Width = 10;
            vScrollBar.Maximum = max;
            vScrollBar.Name = "vScrollBar";

            // Add an event handler for scrolling if needed
            vScrollBar.Scroll += VScrollBar_Scroll;

            this.Controls.Add(vScrollBar);
        }

        private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            // Update the current scroll value
            currentScrollValue = e.NewValue;

            // Reposition the controls based on the scrollbar value
            RepositionControls();
        }

        private void RepositionControls()
        {
            int yOffset = 10 - currentScrollValue;

            for (int i = 0; i < listOfStreams.Count; i++)
            {
                string[] Stream = listOfStreams[i];

                PictureBox pictureBox = (PictureBox)this.Controls.Find("pictureBox" + i, true)[0];
                pictureBox.Location = new Point(20, yOffset + (i * 125));

                Label name = (Label)this.Controls.Find("name" + i, true)[0];
                name.Location = new Point(240, yOffset + (i * 125) + 35);

                Label time = (Label)this.Controls.Find("time" + i, true)[0];
                time.Location = new Point(240, yOffset + (i * 125) + 60);
            }
            
            for (int i = 0; i < a; i++)
            {
                Label streamdate = (Label)this.Controls.Find("streamdate" + i, true)[0];
                streamdate.Location = new Point(260, dateyposition[i] +  yOffset - 20);
            }
        }

        private void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            // Calculate the scroll amount
            int scrollAmount = -e.Delta / SystemInformation.MouseWheelScrollDelta;

            // Adjust the scroll speed by multiplying the scroll amount
            int scrollSpeed = 15;

            // Adjust the currentScrollValue based on the scroll amount
            currentScrollValue += scrollAmount * scrollSpeed;

            VScrollBar vScrollBar = (VScrollBar)this.Controls.Find("vScrollBar", true)[0];

            // Ensure that currentScrollValue stays within the valid range
            if (currentScrollValue < vScrollBar.Minimum)
            {
                currentScrollValue = vScrollBar.Minimum;
            }
            else if (currentScrollValue > vScrollBar.Maximum)
            {
                currentScrollValue = vScrollBar.Maximum;
            }

            // Set the scrollbar's Value property to update the visual position
            vScrollBar.Value = currentScrollValue;

            // Call the RepositionControls method to adjust control positions
            RepositionControls();
        }

        private void LoadDate()
        {
            // Specify the time zone for Japan (UTC+9)
            TimeZoneInfo japanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

            // Get the current time in the specified time zone
            DateTime japanTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, japanTimeZone);

            //Create a new label
            Label date = new Label();

            // Set properties for the label
            date.Location = new System.Drawing.Point(0, 0);
            date.AutoSize = true;
            date.Text = $"Current Japanese Time: {japanTime:yyyy-MM-dd HH:mm:ss}";

            // Add the label to the form's controls
            this.Controls.Add(date);
        }

        private List<string[]> LoadData()
        {
            List<string[]> data = new List<string[]>();

            // Define the URL of the webpage to fetch
            string url = "https://schedule.hololive.tv/lives";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = httpClient.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string htmlContent = response.Content.ReadAsStringAsync().Result;

                        var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                        htmlDocument.LoadHtml(htmlContent);

                        var thumbnailElements = htmlDocument.DocumentNode.SelectNodes("//a[contains(@class, 'thumbnail')]");

                        if (thumbnailElements != null)
                        {
                            string currentDate = "";

                            foreach (var element in thumbnailElements)
                            {
                                string date = GetDate(element);

                                if (!string.IsNullOrWhiteSpace(date))
                                {
                                    if (date != currentDate)
                                    {
                                            currentDate = date;
                                    }
                                }

                                string name = GetElementText(element, ".//div[contains(@class, 'name')]");
                                string time = GetElementText(element, ".//div[contains(@class, 'datetime')]");
                                string link = element.GetAttributeValue("href", "");

                                string piclink = "";

                                // Select all the image elements within the current thumbnail
                                var imageElements = element.SelectNodes(".//div[contains(@class, 'col-12 col-sm-12 col-md-12')]//img");

                                // Check if there are at least two image elements
                                if (imageElements != null && imageElements.Count >= 2)
                                {
                                    piclink = imageElements[1].GetAttributeValue("src", "");
                                }

                                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(time))
                                {
                                    if (IsJapanese(name))
                                    {
                                        name = TranslateJapaneseToEnglish(name);
                                    }
                                    data.Add(new string[] { currentDate, name, time, link, piclink });
                               
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("No elements with the class 'thumbnail' found.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to retrieve the webpage. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }

            return data;
        }

        private void CreatePictureBoxesandLabels()
        {
            string previousDate = null;

            for (int i = 0; i < listOfStreams.Count; i++) 
            {
                string[] Stream = listOfStreams[i];
                
                // Create a new PictureBox
                PictureBox newPictureBox = new PictureBox();
                newPictureBox.Size = new Size(216, 122);
                newPictureBox.Location = new Point(20, 10 + i * 125);
                newPictureBox.Name = "pictureBox" + i;

                // Load an image (replace with your URL)
                LoadImageFromUrl(newPictureBox, Stream[4]);

                // Set the Tag property to store Stream[3]
                newPictureBox.Tag = Stream[3];

                // Add the PictureBox to the form
                this.Controls.Add(newPictureBox);

                // Assign the Click event handler
                newPictureBox.Click += new EventHandler(PictureBox_Click);

                if (Stream[0] != previousDate)
                {
                    // Create a new streamdate label
                    Label streamdate = new Label();
                    streamdate.Location = new System.Drawing.Point(260, 10 + i * 125);
                    streamdate.AutoSize = true;
                    streamdate.Text = Stream[0];
                    streamdate.Font = new Font("Arial", 20);
                    streamdate.Name = "streamdate" + a;
                    dateyposition[a] = 10 + i * 125;
                    a++;
                    this.Controls.Add(streamdate);

                    // Update previousDate with the current date
                    previousDate = Stream[0];
                }

                //Create new Labels
                Label name = new Label();
                Label time = new Label();

                // Set properties for the labels
                name.Location = new System.Drawing.Point(240, 10 + i * 125 +35);
                name.AutoSize = true;
                name.Text = Stream[1];
                name.Font = new Font("Arial", 12);
                name.Name = "name" + i;
                time.Location = new System.Drawing.Point(240, 10 + i * 125 + 60);
                time.AutoSize = true;
                time.Text = Stream[2];
                time.Font = new Font("Arial", 12);
                time.Name = "time" + i;

                // Add the labels to the form's controls
                this.Controls.Add(name);
                this.Controls.Add(time);
            }
        }

        private void LoadImageFromUrl(PictureBox pictureBox, string url)
        {
            try
            {
                WebClient client = new WebClient();
                byte[] data = client.DownloadData(url);
                client.Dispose();

                using (MemoryStream mem = new MemoryStream(data))
                {
                    Image originalImage = Image.FromStream(mem);

                    // Define the new size
                    Size newSize = new Size(216, 122);

                    // Create a new bitmap with the desired dimensions
                    Bitmap scaledImage = new Bitmap(newSize.Width, newSize.Height);

                    using (Graphics g = Graphics.FromImage(scaledImage))
                    {
                        // Set the interpolation mode to high quality bicubic
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        // Draw the original image onto the new bitmap
                        g.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height);
                    }

                    pictureBox.Image = scaledImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message);
            }
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBox clickedPictureBox = (PictureBox)sender;

            // Retrieve the value of Stream[3] from the Tag property
            string url = clickedPictureBox.Tag.ToString();

            try
            {
                // Attempt to open the URL in the default web browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening URL: " + ex.Message);
            }
        }

        // Extract the date from the preceding holodule navbar-text element
        static string GetDate(HtmlNode element)
        {
            var dateElement = element.SelectSingleNode("preceding::div[contains(@class, 'holodule navbar-text')][1]");
            string date = dateElement?.InnerText.Trim() ?? "";
            date = (Regex.Replace(date, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}\(\)]", "")).Trim() ?? "";
            return date;
        }

        // Get the inner text of a specified element using XPath
        static string GetElementText(HtmlNode element, string xpath)
        {
            var subElement = element.SelectSingleNode(xpath);
            return subElement?.InnerText.Trim() ?? "";
        }

        static bool IsJapanese(string text)
        {
            // This pattern includes Hiragana, Katakana, and Kanji characters
            string japanesePattern = @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]";

            return Regex.IsMatch(text, japanesePattern);
        }

        static string TranslateJapaneseToEnglish(string japaneseName)
        {
            switch (japaneseName)
            {
                //Hololive
                case "ときのそら": return "Tokino Sora";
                case "ロボ子さん": return "Roboko-san";
                case "さくらみこ": return "Sakura Miko";
                case "星街すいせい": return "Hoshimachi Suisei";
                case "夜空メル": return "Yozora Mel";
                case "白上フブキ": return "Shirakami Fubuki";
                case "夏色まつり": return "Natsuiro Matsuri";
                case "赤井 はあと": return "Akai Haato";
                case "アキ・ローゼンタール": return "Aki Rosenthal";
                case "アキロゼ": return "Aki Rosenthal";
                case "湊あくあ": return "Minato Aqua";
                case "紫咲シオン": return "Murasaki Shion";
                case "百鬼あやめ": return "Nakiri Ayame";
                case "癒月ちょこ": return "Yuzuki Choco";
                case "大空スバル": return "Oozora Subaru";
                case "大神ミオ": return "Ookami Mio";
                case "猫又おかゆ": return "Nekomata Okayu";
                case "戌神ころね": return "Inugami Korone";
                case "兎田ぺこら": return "Usada Pekora";
                case "不知火フレア": return "Shiranui Flare";
                case "白銀ノエル": return "Shirogane Noel";
                case "宝鐘マリン": return "Houshou Marine";
                case "天音かなた": return "Amane Kanata";
                case "角巻わため": return "Tsunomaki Watame";
                case "常闇トワ": return "Tokoyami Towa";
                case "姫森ルーナ": return "Himemori Luna";
                case "雪花ラミィ": return "Yukihana Lamy";
                case "桃鈴ねね": return "Momosuzu Nene";
                case "獅白ぼたん": return "Shishiro Botan";
                case "尾丸ポルカ": return "Omaru Polka";
                case "ラプラス・ダークネス": return "La+ Darknesss";
                case "鷹嶺ルイ": return "Takane Lui";
                case "博衣こより": return "Hakui Koyori";
                case "沙花叉クロヱ": return "Sakamata Chloe";
                case "風真いろは": return "Kazama Iroha";
                case "ホロライブ": return "Hololive";
                case "火威青": return "Hiodoshi Ao";
                case "音乃瀬奏": return "Otonose Kanade";
                case "一条莉々華": return "Ichijou Ririka";
                case "儒烏風亭らでん": return "Juufuutei Raden";
                case "轟はじめ": return "Todoroki Hajime";
                //HOLOSTARS
                case "花咲みやび": return "Hanasaki Miyabi";
                case "奏手イヅル": return "Kanade Izuru";
                case "アルランディス": return "Arurandeisu";
                case "律可": return "Rikka";
                case "アステル・レダ": return "Astel Leda";
                case "岸堂天真": return "Kishido Temma";
                case "夕刻ロベル": return "Yukoku Roberu";
                case "影山シエン": return "Kageyama Shien";
                case "荒咬オウガ": return "Aragami Oga";
                case "夜十神封魔": return "Yatogami Fuma";
                case "羽継烏有": return "Utsugi Uyu";
                case "緋崎ガンマ": return "Hizaki Gamma";
                case "水無世燐央": return "Minase Rio";
                //Indonesian 
                case "アユンダ・リス": return "Ayunda Risu";
                case "ムーナ・ホシノヴァ": return "Moona Hoshinova";
                case "アイラニ・イオフィフティーン": return "Airani Iofifteen";
                case "クレイジー・オリー": return "Kureiji Ollie";
                case "アーニャ・メルフィッサ": return "Anya Melfissa";
                case "パヴォリア・レイネ": return "Pavolia Reine";

                default: return japaneseName; // Return the original name if not found
            }
        }
    }
}